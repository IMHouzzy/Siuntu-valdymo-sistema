using Bakalauras.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Bakalauras.API.Dtos;
using Bakalauras.API.Services;

[ApiController]
[Route("api/orders/")]
[Authorize]
public class OrderController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notif;

    public OrderController(AppDbContext db, INotificationService notif)
    {
        _db = db;
        _notif = notif;
    }

    // Helpers

    private int GetRequiredCompanyId()
    {
        var companyId = User.GetCompanyId();
        if (companyId <= 0)
            throw new UnauthorizedAccessException("No active company selected.");
        return companyId;
    }

    // READ (LIST) 

    [HttpGet("allOrders")]
    public async Task<IActionResult> GetAllOrders()
    {
        int companyId;
        try { companyId = GetRequiredCompanyId(); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }

        var orders = await _db.orders
            .AsNoTracking()
            .Where(o => o.fk_Companyid_Company == companyId)
            .Select(o => new
            {
                o.id_Orders,
                o.OrdersDate,
                o.totalAmount,
                o.paymentMethod,
                o.deliveryPrice,
                o.status,
                o.fk_Clientid_Users,
                o.externalDocumentId,
                o.fk_Companyid_Company
            })
            .ToListAsync();

        return Ok(orders);
    }

    [HttpGet("allOrdersFullInfo")]
    public async Task<IActionResult> GetAllOrdersFullInfo()
    {
        int companyId;
        try { companyId = GetRequiredCompanyId(); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }

        var orders = await _db.orders
            .AsNoTracking()
            .Where(o => o.fk_Companyid_Company == companyId)
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
                o.fk_Companyid_Company,

                hasShipment = _db.shipments.Any(s => s.fk_Ordersid_Orders == o.id_Orders),

                // Delivery snapshot — what the client actually chose for this order
                o.snapshotDeliveryMethod,
                o.snapshotDeliveryAddress,
                o.snapshotCity,
                o.snapshotCountry,
                o.snapshotPhone,
                o.snapshotLockerId,
                o.snapshotLockerName,
                o.snapshotLockerAddress,
                o.snapshotCourierId,

                client = new
                {
                    id_Users = o.fk_Clientid_Users,
                    name = o.fk_Clientid_UsersNavigation.name,
                    surname = o.fk_Clientid_UsersNavigation.surname,
                    email = o.fk_Clientid_UsersNavigation.email,

                    companyData = _db.client_companies
                        .AsNoTracking()
                        .Where(cc =>
                            cc.fk_Companyid_Company == companyId &&
                            cc.fk_Clientid_Users == o.fk_Clientid_Users)
                        .Select(cc => new
                        {
                            cc.vat,
                            cc.bankCode,
                            cc.externalClientId
                        })
                        .FirstOrDefault()
                },

                products = o.ordersproducts.Select(op => new
                {
                    op.quantity,
                    op.unitPrice,
                    op.vatValue,
                    productId = op.fk_Productid_Product,
                    name = op.fk_Productid_ProductNavigation.name,
                    price = op.fk_Productid_ProductNavigation.price,
                    unit = op.fk_Productid_ProductNavigation.unit,
                    externalCode = op.fk_Productid_ProductNavigation.externalCode,
                    imageUrl = op.fk_Productid_ProductNavigation.product_images
                                      .Select(pi => pi.url).FirstOrDefault()
                }).ToList()
            })
            .OrderByDescending(x => x.id_Orders)
            .ToListAsync();

        return Ok(orders);
    }

    // READ (FULL DETAIL)
    // GET /api/orders/order/{id}/full

    [HttpGet("order/{id:int}/full")]
    public async Task<IActionResult> GetOrderFull(int id)
    {
        int companyId;
        try { companyId = GetRequiredCompanyId(); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }

        var order = await _db.orders
            .AsNoTracking()
            .Where(o => o.id_Orders == id && o.fk_Companyid_Company == companyId)
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

                // Full delivery snapshot for this specific order
                o.snapshotDeliveryMethod,
                o.snapshotDeliveryAddress,
                o.snapshotCity,
                o.snapshotCountry,
                o.snapshotPhone,
                o.snapshotCourierId,
                o.snapshotLockerId,
                o.snapshotLockerName,
                o.snapshotLockerAddress,
                o.snapshotLat,
                o.snapshotLng,

                client = new
                {
                    id = o.fk_Clientid_Users,
                    name = o.fk_Clientid_UsersNavigation.name,
                    surname = o.fk_Clientid_UsersNavigation.surname,
                    email = o.fk_Clientid_UsersNavigation.email,
                    phone = o.fk_Clientid_UsersNavigation.phoneNumber,
                    companyData = _db.client_companies
                        .AsNoTracking()
                        .Where(cc =>
                            cc.fk_Companyid_Company == companyId &&
                            cc.fk_Clientid_Users == o.fk_Clientid_Users)
                        .Select(cc => new
                        {
                            cc.vat,
                            cc.bankCode,
                            cc.externalClientId
                        })
                        .FirstOrDefault()
                },

                products = o.ordersproducts.Select(op => new
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
                        imageUrl = op.fk_Productid_ProductNavigation.product_images
                                          .OrderBy(pi => pi.sortOrder)
                                          .Select(pi => pi.url)
                                          .FirstOrDefault()
                    }
                }).ToList(),

                shipment = _db.shipments
                    .AsNoTracking()
                    .Where(s => s.fk_Ordersid_Orders == o.id_Orders)
                    .Select(s => new
                    {
                        s.id_Shipment,
                        s.trackingNumber,
                        s.shippingDate,
                        s.estimatedDeliveryDate,
                        s.DeliveryLat,
                        s.DeliveryLng,
                        s.providerShipmentId,
                        s.providerParcelNumber,
                        s.providerLockerId,
                        courierName = s.fk_Courierid_CourierNavigation == null
                                        ? null : s.fk_Courierid_CourierNavigation.name,
                        courierType = s.fk_Courierid_CourierNavigation == null
                                        ? null : s.fk_Courierid_CourierNavigation.type,
                        courierPrice = s.fk_Courierid_CourierNavigation == null
                                        ? (double?)null : s.fk_Courierid_CourierNavigation.deliveryPrice,
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
                                p.labelFile,
                                p.weight,
                                p.trackingNumber,
                                p.creationDate
                            })
                            .ToList()
                    })
                    .FirstOrDefault(),

                // Returns for this order
                returns = _db.product_returns
                    .AsNoTracking()
                    .Where(r => r.fk_ordersid_orders == o.id_Orders)
                    .OrderByDescending(r => r.date)
                    .Select(r => new
                    {
                        r.id_Returns,
                        r.date,
                        statusId = r.fk_ReturnStatusTypeid_ReturnStatusType,
                        statusName = r.fk_ReturnStatusTypeid_ReturnStatusTypeNavigation.name,
                        r.returnMethod,
                        r.clientNote,
                        r.employeeNote,
                        r.returnStreet,
                        r.returnCity,
                        r.returnPostalCode,
                        r.returnCountry,
                        r.returnLockerId,
                        r.returnLockerName,
                        r.returnLockerAddress,
                        r.returnLat,
                        r.returnLng,
                        courierName = r.fk_Courierid_CourierNavigation == null
                                        ? null : r.fk_Courierid_CourierNavigation.name,

                        // Return items with product details
                        items = r.return_items.Select(ri => new
                        {
                            ri.id_ReturnItem,
                            ri.quantity,
                            ri.reason,
                            ri.reasonId,
                            reasonName = ri.reasonNavigation == null ? null : ri.reasonNavigation.name,
                            ri.evaluationComment,
                            ri.evaluation,
                            ri.evaluationDate,
                            ri.returnSubTotal,
                            ri.imageUrls,

                            // Get the product info from the OrdersProduct
                            product = _db.ordersproducts
                                .Where(op => op.id_OrdersProduct == ri.fk_OrdersProductid_OrdersProduct)
                                .Select(op => new
                                {
                                    op.id_OrdersProduct,
                                    name = op.fk_Productid_ProductNavigation.name,
                                    unit = op.fk_Productid_ProductNavigation.unit,
                                    imageUrl = op.fk_Productid_ProductNavigation.product_images
                                        .OrderBy(pi => pi.sortOrder)
                                        .Select(pi => pi.url)
                                        .FirstOrDefault()
                                })
                                .FirstOrDefault()
                        }).ToList(),

                        // Return shipment with packages/labels
                        returnShipment = _db.shipments
                            .AsNoTracking()
                            .Where(s => s.fk_Returnsid_Returns == r.id_Returns)
                            .Select(s => new
                            {
                                s.id_Shipment,
                                s.trackingNumber,
                                packages = s.packages
                                    .OrderBy(p => p.id_Package)
                                    .Select(p => new
                                    {
                                        p.id_Package,
                                        p.labelFile,
                                        p.trackingNumber
                                    })
                                    .ToList()
                            })
                            .FirstOrDefault()
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (order == null)
            return NotFound("Order not found or does not belong to your company.");

        return Ok(order);
    }

    // LOOKUPS

    [AllowAnonymous]
    [HttpGet("order-statuses")]
    public async Task<IActionResult> GetOrderStatuses()
    {
        var statuses = await _db.orderstatuses
            .Select(s => new { s.id_OrderStatus, s.name })
            .ToListAsync();

        return Ok(statuses);
    }

    // Returns client_company data (billing/VAT only) for a given user within the active company
    [HttpGet("clientInfo/{userId:int}")]
    public async Task<IActionResult> GetClientInfo(int userId)
    {
        int companyId;
        try { companyId = GetRequiredCompanyId(); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }

        var cc = await _db.client_companies
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.fk_Companyid_Company == companyId &&
                x.fk_Clientid_Users == userId);

        if (cc == null)
            return StatusCode(403, "Client is not in your company.");

        // Also return the current profile delivery address so staff can
        // pre-fill the order form — but this is just a suggestion, not locked in.
        return Ok(new
        {
            cc.deliveryAddress,
            cc.city,
            cc.country,
            cc.vat,
            cc.bankCode,
            cc.externalClientId
        });
    }

    [HttpGet("products")]
    public async Task<IActionResult> SearchProducts([FromQuery] string? q)
    {
        int companyId;
        try { companyId = GetRequiredCompanyId(); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }

        q = (q ?? "").Trim();

        var products = await _db.products
            .AsNoTracking()
            .Where(p => p.fk_Companyid_Company == companyId)
            .Where(p => q == "" || p.name.Contains(q))
            .Select(p => new { p.id_Product, p.name, p.price })
            .ToListAsync();

        return Ok(products);
    }

    // CREATE

    [HttpPost("createOrder")]
    public async Task<IActionResult> CreateOrder([FromBody] OrderUpsertDto dto)
    {
        int companyId;
        try { companyId = GetRequiredCompanyId(); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }

        if (dto.Items == null || dto.Items.Count == 0)
            return BadRequest("Order must have at least 1 product.");

        // Client must have a client_company row for this company
        var cc = await _db.client_companies
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.fk_Companyid_Company == companyId &&
                x.fk_Clientid_Users == dto.ClientUserId);

        if (cc == null)
            return StatusCode(403, "Client is not in your company.");

        // All products must belong to this company
        var productIds = dto.Items.Select(i => i.ProductId).Distinct().ToList();
        var okCount = await _db.products.AsNoTracking()
            .CountAsync(p => productIds.Contains(p.id_Product) && p.fk_Companyid_Company == companyId);

        if (okCount != productIds.Count)
            return StatusCode(403, "One or more products are not in your company.");

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // Snapshot the client's current profile address at order creation time.
            // After this point the order's delivery address is independent of client_companies.
            // If dto.DeliveryMethod/LockerId etc. are provided they take priority (locker order).
            var snapshotAddress = dto.DeliveryMethod?.ToUpperInvariant() == "LOCKER"
                ? null   // locker orders don't need a home address
                : (dto.ClientInfo?.DeliveryAddress ?? cc.deliveryAddress);

            var snapshotCity = dto.DeliveryMethod?.ToUpperInvariant() == "LOCKER"
                ? null
                : (dto.ClientInfo?.City ?? cc.city);

            var snapshotCountry = dto.DeliveryMethod?.ToUpperInvariant() == "LOCKER"
                ? null
                : (dto.ClientInfo?.Country ?? cc.country);

            var clientPhone = await _db.users.AsNoTracking()
                .Where(u => u.id_Users == dto.ClientUserId)
                .Select(u => u.phoneNumber)
                .FirstOrDefaultAsync();

            var order = new order
            {
                fk_Companyid_Company = companyId,
                OrdersDate = dto.OrdersDate,
                totalAmount = dto.TotalAmount,
                paymentMethod = dto.PaymentMethod,
                deliveryPrice = dto.DeliveryPrice,
                status = dto.Status,
                fk_Clientid_Users = dto.ClientUserId,
                externalDocumentId = dto.ExternalDocumentId,

                // Address snapshot — independent of client_companies from this point on.
                // Never updated unless client explicitly changes THIS order's delivery.
                snapshotDeliveryAddress = snapshotAddress,
                snapshotCity = snapshotCity,
                snapshotCountry = snapshotCountry,
                snapshotPhone = clientPhone,

                // Delivery method snapshot
                snapshotCourierId = dto.CourierId,
                snapshotDeliveryMethod = dto.DeliveryMethod?.ToUpperInvariant() ?? "HOME",
                snapshotLockerId = dto.LockerId,
                snapshotLockerName = dto.LockerName,
                snapshotLockerAddress = dto.LockerAddress,
                snapshotLat = dto.DeliveryLat,
                snapshotLng = dto.DeliveryLng,
            };

            _db.orders.Add(order);
            await _db.SaveChangesAsync();

            foreach (var it in dto.Items)
            {
                _db.ordersproducts.Add(new ordersproduct
                {
                    fk_Ordersid_Orders = order.id_Orders,
                    fk_Productid_Product = it.ProductId,
                    quantity = it.Quantity,
                    unitPrice = it.UnitPrice,
                    vatValue = it.VatValue
                });
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            await _notif.NotifyOrderStatusAsync(order.id_Orders, dto.Status, companyId);
            return Ok(new { orderId = order.id_Orders });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return StatusCode(500, ex.InnerException?.Message ?? ex.Message);
        }
    }

    // READ (SINGLE)

    [HttpGet("order/{id:int}")]
    public async Task<IActionResult> GetOrder(int id)
    {
        int companyId;
        try { companyId = GetRequiredCompanyId(); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }

        var order = await _db.orders.AsNoTracking().FirstOrDefaultAsync(o => o.id_Orders == id);
        if (order == null) return NotFound();

        if (order.fk_Companyid_Company != companyId)
            return StatusCode(403, "Order is not in your company.");

        var items = await _db.ordersproducts.AsNoTracking()
            .Where(x => x.fk_Ordersid_Orders == id)
            .Select(x => new
            {
                productId = x.fk_Productid_Product,
                x.quantity,
                x.unitPrice,
                x.vatValue
            })
            .ToListAsync();

        return Ok(new
        {
            id = order.id_Orders,
            ordersDate = order.OrdersDate,
            totalAmount = order.totalAmount,
            paymentMethod = order.paymentMethod,
            deliveryPrice = order.deliveryPrice,
            status = order.status,
            clientUserId = order.fk_Clientid_Users,
            externalDocumentId = order.externalDocumentId,
            companyId = order.fk_Companyid_Company,
            // Delivery snapshot
            snapshotDeliveryMethod = order.snapshotDeliveryMethod,
            snapshotDeliveryAddress = order.snapshotDeliveryAddress,
            snapshotCity = order.snapshotCity,
            snapshotCountry = order.snapshotCountry,
            snapshotPhone = order.snapshotPhone,
            snapshotCourierId = order.snapshotCourierId,
            snapshotLockerId = order.snapshotLockerId,
            snapshotLockerName = order.snapshotLockerName,
            snapshotLockerAddress = order.snapshotLockerAddress,
            snapshotLat = order.snapshotLat,
            snapshotLng = order.snapshotLng,
            items
        });
    }

    // UPDATE

    [HttpPut("editOrder/{id:int}")]
    public async Task<IActionResult> UpdateOrder(int id, [FromBody] OrderUpsertDto dto)
    {
        int companyId;
        try { companyId = GetRequiredCompanyId(); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }

        var order = await _db.orders.FirstOrDefaultAsync(o => o.id_Orders == id);
        if (order == null) return NotFound();

        if (order.fk_Companyid_Company != companyId)
            return StatusCode(403, "Order is not in your company.");

        var cc = await _db.client_companies
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.fk_Companyid_Company == companyId &&
                x.fk_Clientid_Users == dto.ClientUserId);

        if (cc == null)
            return StatusCode(403, "Client is not in your company.");

        var productIds = dto.Items.Select(i => i.ProductId).Distinct().ToList();
        var okCount = await _db.products.AsNoTracking()
            .CountAsync(p => productIds.Contains(p.id_Product) && p.fk_Companyid_Company == companyId);

        if (okCount != productIds.Count)
            return StatusCode(403, "One or more products are not in your company.");

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            order.OrdersDate = dto.OrdersDate;
            order.totalAmount = dto.TotalAmount;
            order.paymentMethod = dto.PaymentMethod;
            order.deliveryPrice = dto.DeliveryPrice;
            order.status = dto.Status;
            order.fk_Clientid_Users = dto.ClientUserId;
            if (dto.ExternalDocumentId.HasValue)
            {
                order.externalDocumentId = dto.ExternalDocumentId;
            }

            // Update THIS ORDER's delivery snapshot only — never client_companies.
            // Staff editing an order changes where THIS shipment goes,
            // not the client's permanent profile address.
            var method = dto.DeliveryMethod?.ToUpperInvariant() ?? order.snapshotDeliveryMethod ?? "HOME";

            if (method == "LOCKER")
            {
                order.snapshotDeliveryMethod = "LOCKER";
                order.snapshotCourierId = dto.CourierId ?? order.snapshotCourierId;
                order.snapshotLockerId = dto.LockerId;
                order.snapshotLockerName = dto.LockerName;
                order.snapshotLockerAddress = dto.LockerAddress;
                order.snapshotLat = dto.DeliveryLat;
                order.snapshotLng = dto.DeliveryLng;
                // Clear home address — irrelevant for locker delivery
                order.snapshotDeliveryAddress = null;
                order.snapshotCity = null;
                order.snapshotCountry = null;
            }
            else
            {
                order.snapshotDeliveryMethod = "HOME";
                order.snapshotCourierId = dto.CourierId ?? order.snapshotCourierId;
                // Staff can override via ClientInfo, otherwise keep existing snapshot
                order.snapshotDeliveryAddress = dto.ClientInfo?.DeliveryAddress ?? order.snapshotDeliveryAddress;
                order.snapshotCity = dto.ClientInfo?.City ?? order.snapshotCity;
                order.snapshotCountry = dto.ClientInfo?.Country ?? order.snapshotCountry;
                // Clear locker info — irrelevant for home delivery
                order.snapshotLockerId = null;
                order.snapshotLockerName = null;
                order.snapshotLockerAddress = null;
                order.snapshotLat = null;
                order.snapshotLng = null;
            }

            // Replace order lines
            var old = await _db.ordersproducts
                .Where(x => x.fk_Ordersid_Orders == id)
                .ToListAsync();

            _db.ordersproducts.RemoveRange(old);

            foreach (var it in dto.Items)
            {
                _db.ordersproducts.Add(new ordersproduct
                {
                    fk_Ordersid_Orders = id,
                    fk_Productid_Product = it.ProductId,
                    quantity = it.Quantity,
                    unitPrice = it.UnitPrice,
                    vatValue = it.VatValue
                });
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            await _notif.NotifyOrderStatusAsync(id, dto.Status, companyId);
            return Ok();
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return StatusCode(500, ex.InnerException?.Message ?? ex.Message);
        }
    }

    // DELETE

    [HttpDelete("deleteOrder/{id:int}")]
    public async Task<IActionResult> DeleteOrder(int id)
    {
        int companyId;
        try { companyId = GetRequiredCompanyId(); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }

        var order = await _db.orders.FirstOrDefaultAsync(o => o.id_Orders == id);
        if (order == null) return NotFound();

        if (order.fk_Companyid_Company != companyId)
            return StatusCode(403, "Order is not in your company.");

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var lines = await _db.ordersproducts
                .Where(x => x.fk_Ordersid_Orders == id)
                .ToListAsync();

            _db.ordersproducts.RemoveRange(lines);
            _db.orders.Remove(order);

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return StatusCode(500, ex.InnerException?.Message ?? ex.Message);
        }
    }
}