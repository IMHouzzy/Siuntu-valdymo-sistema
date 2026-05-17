using Bakalauras.API.Models;
using Bakalauras.API.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Bakalauras.API.Services;
[ApiController]
[Route("api/client")]
[Authorize]
public class ClientOrdersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly INotificationService _notif;

    public ClientOrdersController(AppDbContext db, IWebHostEnvironment env, INotificationService notif)
    {
        _db = db;
        _env = env;
        _notif = notif;
    }

    private int GetUserId() => User.GetUserId();

    private int GetCompanyId()
    {
        var id = User.GetCompanyId();
        if (id <= 0) throw new UnauthorizedAccessException("No active company.");
        return id;
    }

    // GET /api/client/orders
    [HttpGet("orders")]
    public async Task<IActionResult> ListOrders()
    {
        int companyId;
        try { companyId = GetCompanyId(); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        var userId = GetUserId();

        // Verify the user is actually a client of this specific company
        var isClient = await _db.client_companies.AnyAsync(cc =>
            cc.fk_Companyid_Company == companyId && cc.fk_Clientid_Users == userId);
        if (!isClient) return Forbid("You are not a client of this company.");

        // KEY FIX: filter by BOTH companyId AND userId
        var orders = await _db.orders
            .AsNoTracking()
            .Where(o => o.fk_Companyid_Company == companyId && o.fk_Clientid_Users == userId)
            .OrderByDescending(o => o.id_Orders)
            .Select(o => new
            {
                o.id_Orders,
                o.OrdersDate,
                o.totalAmount,
                o.paymentMethod,
                o.deliveryPrice,
                o.status,
                statusName = o.statusNavigation.name,
                o.externalDocumentId,
                o.snapshotDeliveryAddress,
                o.snapshotCity,
                o.snapshotCountry,
                o.snapshotPhone,
                hasShipment = _db.shipments.Any(s => s.fk_Ordersid_Orders == o.id_Orders),
                hasLabels = _db.packages.Any(p =>
                    p.labelFile != null &&
                    _db.shipments.Any(s =>
                        s.id_Shipment == p.fk_Shipmentid_Shipment &&
                        s.fk_Ordersid_Orders == o.id_Orders)),
                hasReturn = _db.product_returns.Any(r =>
                    r.fk_ordersid_orders == o.id_Orders &&
                    r.fk_Companyid_Company == companyId),
                itemCount = (int)o.ordersproducts.Sum(op => op.quantity),
            })
            .ToListAsync();

        return Ok(orders);
    }

    // GET /api/client/orders/{id}
    [HttpGet("orders/{id:int}")]
    public async Task<IActionResult> GetOrder(int id)
    {
        int companyId;
        try { companyId = GetCompanyId(); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        var userId = GetUserId();

        var order = await _db.orders
            .AsNoTracking()
            .Where(o => o.id_Orders == id
                     && o.fk_Companyid_Company == companyId
                     && o.fk_Clientid_Users == userId)
            .Select(o => new
            {
                o.id_Orders,
                o.OrdersDate,
                o.totalAmount,
                o.paymentMethod,
                o.deliveryPrice,
                o.status,
                statusName = o.statusNavigation.name,
                o.externalDocumentId,
                o.snapshotDeliveryAddress,
                o.snapshotCity,
                o.snapshotCountry,
                o.snapshotPhone,
                o.snapshotDeliveryMethod,
                o.snapshotCourierId,
                o.snapshotLockerId,
                o.snapshotLockerName,
                o.snapshotLockerAddress,
                o.snapshotLat,
                o.snapshotLng,
                // canChange: true only before labels exist

                // Expose companyId so frontend can use it to load couriers
                companyId = o.fk_Companyid_Company,
            })
            .FirstOrDefaultAsync();

        if (order == null) return NotFound("Order not found.");

        var products = await _db.ordersproducts
            .AsNoTracking()
            .Where(op => op.fk_Ordersid_Orders == id)
            .Select(op => new
            {
                op.id_OrdersProduct,
                op.quantity,
                op.unitPrice,
                op.vatValue,
                product = new
                {
                    id = op.fk_Productid_Product,
                    name = op.fk_Productid_ProductNavigation.name,
                    unit = op.fk_Productid_ProductNavigation.unit,
                    externalCode = op.fk_Productid_ProductNavigation.externalCode,
                    canReturn = op.fk_Productid_ProductNavigation.canTheProductBeProductReturned,
                    imageUrl = op.fk_Productid_ProductNavigation.product_images
                                    .OrderBy(pi => pi.sortOrder)
                                    .Select(pi => pi.url)
                                    .FirstOrDefault()
                }
            })
            .ToListAsync();

        var shipment = await _db.shipments
            .AsNoTracking()
            .Where(s => s.fk_Ordersid_Orders == id)
            .Select(s => new
            {
                s.id_Shipment,
                s.trackingNumber,
                s.shippingDate,
                s.estimatedDeliveryDate,
                s.providerLockerId,
                courierName = s.fk_Courierid_CourierNavigation == null
                    ? null : s.fk_Courierid_CourierNavigation.name,
                latestStatus = s.shipment_statuses
                    .OrderByDescending(ss => ss.date)
                    .Select(ss => new
                    {
                        ss.date,
                        typeName = ss.fk_ShipmentStatusTypeid_ShipmentStatusTypeNavigation.name
                    })
                    .FirstOrDefault(),
                packages = s.packages
                    .OrderBy(p => p.id_Package)
                    .Select(p => new
                    {
                        p.id_Package,
                        p.trackingNumber,
                        p.weight,
                        p.labelFile,
                        p.creationDate
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        var existingReturn = await _db.product_returns
            .AsNoTracking()
            .Where(r => r.fk_ordersid_orders == id && r.fk_Companyid_Company == companyId)
            .Select(r => new
            {
                r.id_Returns,
                r.date,
                r.fk_ReturnStatusTypeid_ReturnStatusType,
                statusName = r.fk_ReturnStatusTypeid_ReturnStatusTypeNavigation.name,
                r.returnMethod,
                r.clientNote,
                r.employeeNote
            })
            .FirstOrDefaultAsync();

        var hasLabels = await _db.packages.AnyAsync(p =>
            p.labelFile != null &&
            _db.shipments.Any(s =>
                s.id_Shipment == p.fk_Shipmentid_Shipment &&
                s.fk_Ordersid_Orders == id));

        var canReturn = (order.status == 3 || order.status == 5) && existingReturn == null;

        return Ok(new
        {
            order.id_Orders,
            order.OrdersDate,
            order.totalAmount,
            order.deliveryPrice,
            order.paymentMethod,
            order.status,
            order.statusName,
            order.externalDocumentId,
            order.snapshotDeliveryAddress,
            order.snapshotCity,
            order.snapshotCountry,
            order.snapshotPhone,
            order.snapshotDeliveryMethod,
            order.snapshotCourierId,
            order.snapshotLockerId,
            order.snapshotLockerName,
            order.snapshotLockerAddress,
            companyId = order.companyId,
            hasLabels,
            canReturn,
            products,
            shipment,
            existingReturn
        });
    }

    // PUT /api/client/orders/{id}/delivery
    [HttpPut("orders/{id:int}/delivery")]
    public async Task<IActionResult> UpdateDelivery(int id, [FromBody] ClientDeliveryUpdateDto dto)
    {
        int companyId;
        try { companyId = GetCompanyId(); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        var userId = GetUserId();

        var order = await _db.orders.FirstOrDefaultAsync(o =>
            o.id_Orders == id &&
            o.fk_Companyid_Company == companyId &&
            o.fk_Clientid_Users == userId);
        if (order == null) return NotFound("Order not found.");

        // Block changes once labels are generated
        var hasLabels = await _db.packages.AnyAsync(p =>
            p.labelFile != null &&
            _db.shipments.Any(s =>
                s.id_Shipment == p.fk_Shipmentid_Shipment &&
                s.fk_Ordersid_Orders == id));
        if (hasLabels)
            return Conflict("Cannot edit — shipping labels have already been generated.");

        var method = (dto.DeliveryMethod ?? order.snapshotDeliveryMethod ?? "HOME")
                        .Trim().ToUpperInvariant();

        if (method == "LOCKER")
        {
            order.snapshotDeliveryMethod = "LOCKER";
            order.snapshotCourierId = dto.CourierId ?? order.snapshotCourierId;
            order.snapshotLockerId = dto.LockerId;
            order.snapshotLockerName = dto.LockerName;
            order.snapshotLockerAddress = dto.LockerAddress;
            order.snapshotLat = dto.DeliveryLat;
            order.snapshotLng = dto.DeliveryLng;
            order.snapshotPhone = dto.Phone?.Trim() ?? order.snapshotPhone;
        }
        else
        {
            order.snapshotDeliveryMethod = "HOME";
            order.snapshotDeliveryAddress = dto.DeliveryAddress?.Trim();
            order.snapshotCity = dto.City?.Trim();
            order.snapshotCountry = dto.Country?.Trim();
            order.snapshotPhone = dto.Phone?.Trim();
            order.snapshotCourierId = dto.CourierId ?? order.snapshotCourierId;
            order.snapshotLockerId = null;
            order.snapshotLockerName = null;
            order.snapshotLockerAddress = null;
            order.snapshotLat = null;
            order.snapshotLng = null;
        }

        if (order.status == 1)
        {
            order.status = 4; // Vykdomas
        }
        await _notif.NotifyOrderStatusAsync(order.id_Orders, 4, companyId);
        await _db.SaveChangesAsync();
        return Ok();
    }

    //  GET /api/client/returns 
    [HttpGet("returns")]
    public async Task<IActionResult> ListReturns()
    {
        int companyId;
        try { companyId = GetCompanyId(); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        var userId = GetUserId();

        var returns = await _db.product_returns
            .AsNoTracking()
            .Where(r => r.fk_Companyid_Company == companyId && r.fk_Clientid_Users == userId)
            .OrderByDescending(r => r.id_Returns)
            .Select(r => new
            {
                r.id_Returns,
                r.date,
                r.fk_ReturnStatusTypeid_ReturnStatusType,
                statusName = r.fk_ReturnStatusTypeid_ReturnStatusTypeNavigation.name,
                r.returnMethod,
                r.clientNote,
                r.employeeNote,
                orderId = r.fk_ordersid_orders,
                itemCount = r.return_items.Count,
                // Show return shipment labels if they exist
                returnShipment = _db.shipments.AsNoTracking()
                    .Where(s => s.fk_Returnsid_Returns == r.id_Returns)
                    .OrderByDescending(s => s.id_Shipment)
                    .Select(s => new
                    {
                        s.id_Shipment,
                        s.trackingNumber,
                        packages = s.packages.Select(p => new
                        {
                            p.id_Package,
                            p.trackingNumber,
                            p.labelFile
                        }).ToList()
                    }).FirstOrDefault()
            })
            .ToListAsync();

        return Ok(returns);
    }

    //  GET /api/client/returns/{returnId} 
    [HttpGet("returns/{returnId:int}")]
    public async Task<IActionResult> GetReturn(int returnId)
    {
        int companyId;
        try { companyId = GetCompanyId(); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        var userId = GetUserId();

        var ret = await _db.product_returns
            .AsNoTracking()
            .Where(r => r.id_Returns == returnId
                     && r.fk_Companyid_Company == companyId
                     && r.fk_Clientid_Users == userId)
            .Select(r => new
            {
                r.id_Returns,
                r.date,
                r.fk_ReturnStatusTypeid_ReturnStatusType,
                statusName = r.fk_ReturnStatusTypeid_ReturnStatusTypeNavigation.name,
                r.returnMethod,
                r.clientNote,
                r.employeeNote,
                r.returnStreet,
                r.returnCity,
                r.returnPostalCode,
                r.returnCountry,
                orderId = r.fk_ordersid_orders,
                items = r.return_items.Select(ri => new
                {
                    ri.id_ReturnItem,
                    ri.quantity,
                    ri.evaluationComment,
                    ri.evaluation,
                    ri.returnSubTotal,
                    ri.evaluationDate,
                    ri.imageUrls,
                    reason = ri.reasonId != null && ri.reasonNavigation != null
                             ? ri.reasonNavigation.name : ri.reason,
                    product = new
                    {
                        id = ri.fk_OrdersProductid_OrdersProductNavigation.fk_Productid_Product,
                        name = ri.fk_OrdersProductid_OrdersProductNavigation.fk_Productid_ProductNavigation.name,
                        unit = ri.fk_OrdersProductid_OrdersProductNavigation.fk_Productid_ProductNavigation.unit,
                        imageUrl = ri.fk_OrdersProductid_OrdersProductNavigation.fk_Productid_ProductNavigation.product_images
                                      .OrderBy(pi => pi.sortOrder)
                                      .Select(pi => pi.url)
                                      .FirstOrDefault()
                    }
                }).ToList(),
                returnShipment = _db.shipments.AsNoTracking()
                    .Where(s => s.fk_Returnsid_Returns == r.id_Returns)
                    .OrderByDescending(s => s.id_Shipment)
                    .Select(s => new
                    {
                        s.id_Shipment,
                        s.trackingNumber,
                        packages = s.packages.Select(p => new
                        {
                            p.id_Package,
                            p.trackingNumber,
                            p.labelFile
                        }).ToList()
                    }).FirstOrDefault()
            })
            .FirstOrDefaultAsync();

        if (ret == null) return NotFound("Return not found.");
        return Ok(ret);
    }

    // POST /api/client/returns/upload-images
    // Accepts multipart files, saves to wwwroot/uploads/returns/temp/{userId}/
    // Returns list of relative URLs to store alongside the return item.
    [HttpPost("returns/upload-images")]
    public async Task<IActionResult> UploadReturnImages([FromForm] IFormFileCollection files)
    {
        var userId = GetUserId();

        if (files == null || files.Count == 0)
            return BadRequest("No files provided.");

        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var dir = Path.Combine(webRoot, "uploads", "returns", "temp", userId.ToString());
        Directory.CreateDirectory(dir);

        var urls = new List<string>();
        foreach (var file in files)
        {
            if (file.Length == 0) continue;
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext)) continue;

            var fileName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(dir, fileName);
            await using var stream = System.IO.File.Create(fullPath);
            await file.CopyToAsync(stream);
            urls.Add($"/uploads/returns/temp/{userId}/{fileName}");
        }

        return Ok(new { urls });
    }

    // POST /api/client/orders/{id}/returns
    // UPDATED: accepts CourierId, LockerId, locker address fields
    // Does NOT create shipment or labels — those are added by employee after confirmation
    [HttpPost("orders/{id:int}/returns")]
    public async Task<IActionResult> CreateReturn(int id, [FromBody] CreateReturnDto dto)
    {
        int companyId;
        try { companyId = GetCompanyId(); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        var userId = GetUserId();

        var order = await _db.orders.AsNoTracking().FirstOrDefaultAsync(o =>
            o.id_Orders == id && o.fk_Companyid_Company == companyId && o.fk_Clientid_Users == userId);
        if (order == null) return NotFound("Order not found.");

        if (order.status != 3 && order.status != 5)
            return Conflict("Returns are only allowed for completed or sent orders.");

        var alreadyHasReturn = await _db.product_returns.AnyAsync(r =>
            r.fk_ordersid_orders == id && r.fk_Companyid_Company == companyId);
        if (alreadyHasReturn)
            return Conflict("A return already exists for this order.");

        if (dto.Items == null || dto.Items.Count == 0)
            return BadRequest("At least one item is required.");

        // Validate return method
        var method = (dto.ReturnMethod ?? "CUSTOM").Trim().ToUpperInvariant();
        if (method is not ("CUSTOM" or "DPD" or "LP_EXPRESS" or "OMNIVA"))
            return BadRequest("Invalid returnMethod. Allowed: CUSTOM, DPD, LP_EXPRESS, OMNIVA.");

        // Validate items belong to this order
        var opIds = dto.Items.Select(i => i.OrdersProductId).ToList();
        var validOps = await _db.ordersproducts.AsNoTracking()
            .Where(op => opIds.Contains(op.id_OrdersProduct) && op.fk_Ordersid_Orders == id)
            .ToListAsync();
        if (validOps.Count != opIds.Count)
            return BadRequest("One or more items do not belong to this order.");

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var ret = new product_return
            {
                date = DateTime.UtcNow,
                fk_ReturnStatusTypeid_ReturnStatusType = 1, // "Sukurtas" / pending
                fk_Clientid_Users = userId,
                fk_Adminid_Users = null,
                fk_Companyid_Company = companyId,
                fk_ordersid_orders = id,
                returnMethod = method,
                clientNote = dto.ClientNote?.Trim(),
                returnStreet = dto.ReturnStreet?.Trim(),
                returnCity = dto.ReturnCity?.Trim(),
                returnPostalCode = dto.ReturnPostalCode?.Trim(),
                returnCountry = string.IsNullOrWhiteSpace(dto.ReturnCountry)
                                                           ? "LT" : dto.ReturnCountry.Trim().ToUpper(),
                // Store courier + locker info for when employee generates labels
                fk_Courierid_Courier = dto.CourierId,
                returnLockerId = dto.ReturnLockerId,
                returnLockerName = dto.ReturnLockerName,
                returnLockerAddress = dto.ReturnLockerAddress,
                returnLat = dto.ReturnLat,
                returnLng = dto.ReturnLng,
            };

            _db.product_returns.Add(ret);
            await _db.SaveChangesAsync();

            foreach (var item in dto.Items)
            {
                var op = validOps.First(x => x.id_OrdersProduct == item.OrdersProductId);

                if (item.Quantity < 1 || item.Quantity > (int)op.quantity)
                    return BadRequest($"Invalid quantity for item {item.OrdersProductId}.");

                var finalImageUrls = new List<string>();
                if (item.ImageUrls != null)
                {
                    var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    var destDir = Path.Combine(webRoot, "uploads", "returns", ret.id_Returns.ToString());
                    Directory.CreateDirectory(destDir);

                    foreach (var tempUrl in item.ImageUrls)
                    {
                        var tempPath = Path.Combine(webRoot, tempUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                        if (!System.IO.File.Exists(tempPath)) { finalImageUrls.Add(tempUrl); continue; }

                        var fileName = Path.GetFileName(tempPath);
                        var destPath = Path.Combine(destDir, fileName);
                        System.IO.File.Move(tempPath, destPath, overwrite: true);
                        finalImageUrls.Add($"/uploads/returns/{ret.id_Returns}/{fileName}");
                    }
                }

                _db.return_items.Add(new return_item
                {
                    fk_Returnsid_Returns = ret.id_Returns,
                    fk_OrdersProductid_OrdersProduct = item.OrdersProductId,
                    quantity = item.Quantity,
                    reason = null,
                    reasonId = item.ReasonId,
                    returnSubTotal = op.unitPrice * item.Quantity,
                    // Store image URLs as JSON array string
                    imageUrls = finalImageUrls.Count > 0
                        ? System.Text.Json.JsonSerializer.Serialize(finalImageUrls) : null,
                });
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return Ok(new { returnId = ret.id_Returns });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return StatusCode(500, ex.InnerException?.Message ?? ex.Message);
        }

    }

    [HttpGet("couriers")]
    public async Task<IActionResult> GetClientCouriers()
    {
        int companyId;
        try { companyId = GetCompanyId(); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }

        // Get enabled integration keys for this company
        var integrations = await _db.company_integrations
            .AsNoTracking()
            .Where(i => i.fk_Companyid_Company == companyId && i.enabled == true)
            .Select(i => i.encryptedSecrets)
            .ToListAsync();

        var couriers = await _db.couriers
            .AsNoTracking()
            .Where(c =>
                (c.fk_Companyid_Company == companyId) ||
                (c.fk_Companyid_Company == null && (
                    c.type == "CUSTOM" ||
                    integrations.Contains(
                        c.type == "DPD_PARCEL" || c.type == "DPD_HOME" ? "DPD" :
                        c.type == "LP_EXPRESS_PARCEL" || c.type == "LP_EXPRESS_HOME" ? "LP_EXPRESS" :
                        c.type == "OMNIVA_PARCEL" ? "OMNIVA" : ""
                    )
                ))
            )
            .OrderBy(c => c.name)
            .Select(c => new
            {
                c.id_Courier,
                c.name,
                c.type,
                c.deliveryPrice,
                c.deliveryTermDays,
                supportsLockers = c.type.EndsWith("_PARCEL"),
            })
            .ToListAsync();

        return Ok(couriers);
    }

}
