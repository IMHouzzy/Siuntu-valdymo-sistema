using Bakalauras.API.Models;
using Bakalauras.API.Dtos;
using Bakalauras.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/shipments")]
[Authorize]
public class ShipmentController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly CourierProviderFactory _providerFactory;
    private readonly INotificationService _notif;

    public ShipmentController(
        AppDbContext db,
        IWebHostEnvironment env,
        CourierProviderFactory providerFactory,
        INotificationService notif)
    {
        _db = db;
        _env = env;
        _providerFactory = providerFactory;
        _notif = notif;
    }

    private int GetRequiredCompanyId()
    {
        var id = User.GetCompanyId();
        if (id <= 0) throw new UnauthorizedAccessException("No active company selected.");
        return id;
    }

    // GET /api/shipments/couriers 
    // DEPRECATED — kept for backwards compat. Frontend should use
    // GET /api/companies/{companyId}/couriers  instead.

    [HttpGet("couriers")]
    public async Task<IActionResult> GetCouriers()
    {
        int companyId;
        try { companyId = GetRequiredCompanyId(); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }

        var enabledKeys = await _providerFactory.GetEnabledIntegrationKeysAsync(companyId);

        var allowedProviderTypes = CourierProviderFactory.AllProviderCourierTypes
            .Where(t =>
            {
                var key = CourierProviderFactory.GetIntegrationKey(t);
                return key != null && enabledKeys.Contains(key);
            })
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var couriers = await _db.couriers
            .AsNoTracking()
            .Where(c =>
                (c.type == "CUSTOM" && c.fk_Companyid_Company == companyId) ||
                (c.type == "CUSTOM" && c.fk_Companyid_Company == null) ||
                (c.fk_Companyid_Company == null && allowedProviderTypes.Contains(c.type))
            )
            .OrderBy(c => c.fk_Companyid_Company == null ? 1 : 0)
            .ThenBy(c => c.name)
            .Select(c => new
            {
                c.id_Courier,
                c.name,
                c.type,
                c.contactPhone,
                c.deliveryTermDays,
                c.deliveryPrice,
                supportsLockers = c.type.EndsWith("_PARCEL"),
                isOwn = c.fk_Companyid_Company == companyId,
            })
            .ToListAsync();

        return Ok(couriers);
    }

    // GET /api/shipments/order-for-shipment/{orderId}

    [HttpGet("order-for-shipment/{orderId:int}")]
    public async Task<IActionResult> GetOrderForShipment(int orderId)
    {
        int companyId;
        try { companyId = GetRequiredCompanyId(); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }

        var order = await _db.orders
            .AsNoTracking()
            .Where(o => o.id_Orders == orderId && o.fk_Companyid_Company == companyId)
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
                clientId = o.fk_Clientid_Users,
                clientName = o.fk_Clientid_UsersNavigation.name,
                clientSurname = o.fk_Clientid_UsersNavigation.surname,
                clientEmail = o.fk_Clientid_UsersNavigation.email,
                clientPhoneNumber = o.fk_Clientid_UsersNavigation.phoneNumber,

                // Delivery snapshot — the authoritative source for WHERE to ship
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

                items = o.ordersproducts.Select(op => new
                {
                    op.id_OrdersProduct,
                    op.quantity,
                    op.unitPrice,
                    op.vatValue,
                    productId = op.fk_Productid_Product,
                    productName = op.fk_Productid_ProductNavigation.name,
                    productUnit = op.fk_Productid_ProductNavigation.unit,
                    productExternalCode = op.fk_Productid_ProductNavigation.externalCode,
                    productImages = op.fk_Productid_ProductNavigation.product_images
                        .OrderBy(pi => pi.sortOrder)
                        .Select(pi => new { pi.url, pi.isPrimary })
                        .ToList()
                }).ToList()
            })
            .FirstOrDefaultAsync();

        if (order == null)
            return NotFound("Order not found or does not belong to your company.");

        var cc = await _db.client_companies
            .AsNoTracking()
            .Where(x => x.fk_Companyid_Company == companyId && x.fk_Clientid_Users == order.clientId)
            .Select(x => new { x.vat, x.bankCode, x.externalClientId })
            .FirstOrDefaultAsync();

        var existingShipment = await _db.shipments
            .AsNoTracking()
            .Where(s => s.fk_Ordersid_Orders == orderId)
            .Select(s => new { s.id_Shipment, s.trackingNumber })
            .FirstOrDefaultAsync();

        // Company shipping address so the frontend can pre-fill sender fields
        var companyInfo = await _db.companies
            .AsNoTracking()
            .Where(c => c.id_Company == companyId)
            .Select(c => new
            {
                c.shippingAddress,
                c.shippingStreet,
                c.shippingCity,
                c.shippingPostalCode,
                c.shippingCountry,
            })
            .FirstOrDefaultAsync();

        return Ok(new
        {
            order.id_Orders,
            order.OrdersDate,
            order.totalAmount,
            order.paymentMethod,
            order.deliveryPrice,
            order.status,
            order.statusName,
            order.externalDocumentId,

            client = new
            {
                id = order.clientId,
                name = order.clientName,
                surname = order.clientSurname,
                email = order.clientEmail,
                phoneNumber = order.clientPhoneNumber,
            },

            // Billing info only
            clientBilling = cc == null ? null : (object)new
            {
                cc.vat,
                cc.bankCode,
                cc.externalClientId
            },

            // Delivery snapshot — pre-fill the shipment form from order's chosen delivery
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

            // Company sender address for pre-filling the shipment form
            shippingStreet = companyInfo?.shippingStreet,
            shippingAddress = companyInfo?.shippingAddress,
            shippingCity = companyInfo?.shippingCity,
            shippingPostalCode = companyInfo?.shippingPostalCode,
            shippingCountry = companyInfo?.shippingCountry ?? "LT",

            items = order.items.Select(op => new
            {
                op.id_OrdersProduct,
                op.quantity,
                op.unitPrice,
                op.vatValue,
                product = new
                {
                    id = op.productId,
                    name = op.productName,
                    unit = op.productUnit,
                    externalCode = op.productExternalCode,
                    images = op.productImages
                }
            }).ToList(),

            existingShipment
        });
    }

    // GET /api/shipments/all

    [HttpGet("all")]
    public async Task<IActionResult> GetAllShipments()
    {
        int companyId;
        try { companyId = GetRequiredCompanyId(); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }

        var shipments = await _db.shipments
            .AsNoTracking()
            .Where(s => s.fk_Companyid_Company == companyId)
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
                courierId = s.fk_Courierid_Courier,
                courierName = s.fk_Courierid_CourierNavigation == null ? null : s.fk_Courierid_CourierNavigation.name,
                courierType = s.fk_Courierid_CourierNavigation == null ? null : s.fk_Courierid_CourierNavigation.type,
                courierPrice = s.fk_Courierid_CourierNavigation == null ? (double?)null : s.fk_Courierid_CourierNavigation.deliveryPrice,
                orderId = s.fk_Ordersid_Orders,
            })
            .OrderByDescending(s => s.id_Shipment)
            .ToListAsync();

        if (!shipments.Any()) return Ok(new List<object>());

        var shipmentIds = shipments.Select(s => s.id_Shipment).ToList();

        var allStatuses = await _db.shipment_statuses
            .AsNoTracking()
            .Where(ss => shipmentIds.Contains(ss.fk_Shipmentid_Shipment))
            .Select(ss => new
            {
                ss.fk_Shipmentid_Shipment,
                ss.date,
                typeName = ss.fk_ShipmentStatusTypeid_ShipmentStatusTypeNavigation.name
            })
            .ToListAsync();

        var latestByShipment = allStatuses
            .GroupBy(ss => ss.fk_Shipmentid_Shipment)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(ss => ss.date).First());

        var result = shipments.Select(s => new
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
            courier = s.courierId == null ? null : (object)new
            {
                id_Courier = s.courierId,
                name = s.courierName,
                type = s.courierType,
                deliveryPrice = s.courierPrice,
            },
            s.orderId,
            latestStatus = latestByShipment.TryGetValue(s.id_Shipment, out var ls)
                ? new { ls.date, ls.typeName } : null
        });

        return Ok(result);
    }

    // GET /api/shipments/{id}

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetShipment(int id)
    {
        int companyId;
        try { companyId = GetRequiredCompanyId(); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }

        var shipment = await _db.shipments
            .AsNoTracking()
            .Where(s => s.id_Shipment == id && s.fk_Companyid_Company == companyId)
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
                courierId = s.fk_Courierid_Courier,
                orderId = s.fk_Ordersid_Orders,
                statuses = s.shipment_statuses
                    .OrderByDescending(ss => ss.date)
                    .Select(ss => new
                    {
                        ss.id_ShipmentStatus,
                        ss.date,
                        typeId = ss.fk_ShipmentStatusTypeid_ShipmentStatusType,
                        typeName = ss.fk_ShipmentStatusTypeid_ShipmentStatusTypeNavigation.name
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (shipment == null) return NotFound();
        return Ok(shipment);
    }

    // GET /api/shipments/{id}/packages

    [HttpGet("{id:int}/packages")]
    public async Task<IActionResult> GetPackages(int id)
    {
        int companyId;
        try { companyId = GetRequiredCompanyId(); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }

        var shipmentExists = await _db.shipments
            .AnyAsync(s => s.id_Shipment == id && s.fk_Companyid_Company == companyId);
        if (!shipmentExists) return NotFound();

        var packages = await _db.packages
            .AsNoTracking()
            .Where(p => p.fk_Shipmentid_Shipment == id)
            .OrderBy(p => p.id_Package)
            .Select(p => new
            {
                p.id_Package,
                p.creationDate,
                p.labelFile,
                p.weight,
                p.trackingNumber,
                p.fk_Shipmentid_Shipment
            })
            .ToListAsync();

        return Ok(packages);
    }

    // POST /api/shipments/create

    [HttpPost("create")]
    public async Task<IActionResult> CreateShipment([FromBody] CreateShipmentDto dto)
    {
        int companyId;
        try { companyId = GetRequiredCompanyId(); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }

        if (dto.PackageCount < 1) return BadRequest("PackageCount must be at least 1.");

        // Load order — delivery address comes from the ORDER snapshot, not client_companies 
        var order = await _db.orders
            .AsNoTracking()
            .Where(o => o.id_Orders == dto.OrderId && o.fk_Companyid_Company == companyId)
            .Select(o => new
            {
                o.id_Orders,
                clientId = o.fk_Clientid_Users,
                clientName = o.fk_Clientid_UsersNavigation.name,
                clientSurname = o.fk_Clientid_UsersNavigation.surname,
                clientPhone = o.fk_Clientid_UsersNavigation.phoneNumber,
                clientEmail = o.fk_Clientid_UsersNavigation.email,

                // These snapshot fields are the authoritative delivery destination.
                // The client may have changed them after order creation via /api/client/orders/{id}/delivery.
                o.snapshotDeliveryMethod,
                o.snapshotDeliveryAddress,
                o.snapshotCity,
                o.snapshotCountry,
                o.snapshotPhone,
                o.snapshotLockerId,
                o.snapshotCourierId,
            })
            .FirstOrDefaultAsync();

        if (order == null) return NotFound("Order not found or does not belong to your company.");

        if (await _db.shipments.AnyAsync(s => s.fk_Ordersid_Orders == dto.OrderId))
            return Conflict("A shipment already exists for this order.");

        // Validate courier
        courier? courier = null;
        if (dto.CourierId.HasValue)
        {
            courier = await _db.couriers.FindAsync(dto.CourierId.Value);
            if (courier == null) return BadRequest("Courier not found.");
        }
        else if (order.snapshotCourierId.HasValue)
        {
            // Fall back to courier chosen at order time if not overridden in dto
            courier = await _db.couriers.FindAsync(order.snapshotCourierId.Value);
        }

        // Fetch company (sender) info
        var company = await _db.companies
            .AsNoTracking()
            .Where(c => c.id_Company == companyId)
            .Select(c => new
            {
                c.name,
                c.phoneNumber,
                c.address,
                c.shippingAddress,
                c.shippingStreet,
                c.shippingCity,
                c.shippingPostalCode,
                c.shippingCountry,
            })
            .FirstOrDefaultAsync();

        // Build label text helpers 
        var recipientName = $"{order.clientName} {order.clientSurname}".Trim();
        var recipientPhone = order.snapshotPhone ?? order.clientPhone ?? "";

        // Delivery address: order snapshot is the authoritative source.
        // dto fields allow staff to override at shipment creation time if needed.
        var recipientStreet = !string.IsNullOrWhiteSpace(dto.RecipientStreet)
            ? dto.RecipientStreet
            : (order.snapshotDeliveryAddress ?? "");

        var recipientCity = !string.IsNullOrWhiteSpace(dto.RecipientCity)
            ? dto.RecipientCity
            : (order.snapshotCity ?? "");

        var recipientPostalCode = (!string.IsNullOrWhiteSpace(dto.RecipientPostalCode)
            ? dto.RecipientPostalCode
            : "").Replace("-", "").Replace(" ", "");

        var recipientCountry = MapCountry(
            !string.IsNullOrWhiteSpace(dto.RecipientCountry)
                ? dto.RecipientCountry
                : order.snapshotCountry);

        // Locker: dto overrides, then fall back to whatever the client chose for this order
        var effectiveLockerId = dto.LockerId ?? order.snapshotLockerId;

        var recipientAddress = string.Join(", ",
            new[] { recipientStreet, recipientCity, recipientCountry }
            .Where(s => !string.IsNullOrWhiteSpace(s)));

        var senderName = company?.name ?? "—";
        var senderAddress = company?.shippingAddress ?? company?.address ?? "—";
        var senderPhone = company?.phoneNumber ?? "";
        var courierName = courier?.name ?? "—";

        var shippingDateStr = dto.ShippingDate?.ToString("yyyy-MM-dd") ?? "—";
        var estimatedDateStr = dto.EstimatedDeliveryDate?.ToString("yyyy-MM-dd") ?? "—";

        // Determine path: provider courier vs custom courier
        var courierType = courier?.type ?? "CUSTOM";
        var integKey = CourierProviderFactory.GetIntegrationKey(courierType);
        var isProvider = integKey != null;

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var shipment = new shipment
            {
                trackingNumber = "",
                shippingDate = dto.ShippingDate,
                estimatedDeliveryDate = dto.EstimatedDeliveryDate,
                fk_Courierid_Courier = courier?.id_Courier,
                fk_Ordersid_Orders = dto.OrderId,
                fk_Companyid_Company = companyId,
                DeliveryLat = dto.DeliveryLat,
                DeliveryLng = dto.DeliveryLng,
            };

            _db.shipments.Add(shipment);
            await _db.SaveChangesAsync();

            _db.shipment_statuses.Add(new shipment_status
            {
                fk_Shipmentid_Shipment = shipment.id_Shipment,
                fk_ShipmentStatusTypeid_ShipmentStatusType = 1,
                date = DateTime.UtcNow
            });

            var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var createdPackages = new List<object>();

            // Resolve per-package weights
            var resolvedWeights = Enumerable.Range(0, dto.PackageCount)
                .Select(i =>
                {
                    double? w = dto.PackageWeights != null && i < dto.PackageWeights.Count
                        ? dto.PackageWeights[i]
                        : null;
                    return (w.HasValue && w.Value > 0) ? w.Value : (dto.PackageWeightKg ?? 1.0);
                })
                .ToList();

            if (isProvider)
            {
                // Provider path (DPD, LP Express, …) 
                if (string.IsNullOrWhiteSpace(recipientPostalCode) && effectiveLockerId == null)
                    return BadRequest("RecipientPostalCode is required for courier service home delivery.");

                ICourierProvider provider;
                try { provider = await _providerFactory.GetProviderAsync(companyId, courierType); }
                catch (InvalidOperationException ex)
                {
                    await tx.RollbackAsync();
                    return BadRequest(ex.Message);
                }

                var providerReq = new CourierShipmentRequest
                {
                    SenderName = senderName,
                    SenderPhone = senderPhone,
                    SenderStreet = !string.IsNullOrWhiteSpace(dto.SenderStreet)
                        ? dto.SenderStreet
                        : (company?.shippingStreet ?? company?.shippingAddress ?? company?.address ?? ""),
                    SenderCity = !string.IsNullOrWhiteSpace(dto.SenderCity)
                        ? dto.SenderCity
                        : (company?.shippingCity ?? ""),
                    SenderPostalCode = (!string.IsNullOrWhiteSpace(dto.SenderPostalCode)
                        ? dto.SenderPostalCode
                        : (company?.shippingPostalCode ?? ""))
                        .Replace("-", "").Replace(" ", ""),
                    SenderCountry = company?.shippingCountry ?? "LT",

                    RecipientName = recipientName,
                    RecipientEmail = order.clientEmail ?? "",
                    RecipientPhone = recipientPhone,
                    RecipientStreet = recipientStreet,
                    RecipientCity = recipientCity,
                    RecipientPostalCode = recipientPostalCode,
                    RecipientCountry = recipientCountry,

                    // Use the effective locker ID (dto override or order's chosen locker)
                    LockerId = effectiveLockerId,
                    PackageCount = dto.PackageCount,
                    PackageWeightKg = resolvedWeights[0],
                    PackageWeights = resolvedWeights,
                    OrderReference = $"Order-{dto.OrderId}",
                };

                var result = await provider.CreateShipmentAsync(providerReq);

                if (result.ErrorMessage != null)
                {
                    await tx.RollbackAsync();
                    return StatusCode(502, result.ErrorMessage);
                }

                shipment.trackingNumber = result.ProviderShipmentId;
                shipment.providerShipmentId = result.ProviderShipmentId;
                shipment.providerParcelNumber = result.ParcelNumbers.Count > 0
                    ? string.Join(",", result.ParcelNumbers)
                    : result.ProviderShipmentId;
                shipment.providerLockerId = effectiveLockerId;

                var dir = Path.Combine(webRoot, "labels", shipment.id_Shipment.ToString());
                Directory.CreateDirectory(dir);

                for (int i = 0; i < dto.PackageCount; i++)
                {
                    var pkgTrackingNumber = i < result.ParcelNumbers.Count
                        ? result.ParcelNumbers[i]
                        : result.ParcelNumbers.LastOrDefault() ?? result.ProviderShipmentId;

                    byte[]? labelBytes = null;
                    if (result.PerParcelLabelBytes.Count > 0)
                        labelBytes = i < result.PerParcelLabelBytes.Count
                            ? result.PerParcelLabelBytes[i]
                            : result.PerParcelLabelBytes.Last();

                    string? labelUrl = null;
                    if (labelBytes != null)
                    {
                        var filePath = Path.Combine(dir, $"label_{i + 1}.pdf");
                        await System.IO.File.WriteAllBytesAsync(filePath, labelBytes);
                        labelUrl = $"/labels/{shipment.id_Shipment}/label_{i + 1}.pdf";
                    }

                    var pkg = new package
                    {
                        fk_Shipmentid_Shipment = shipment.id_Shipment,
                        labelFile = labelUrl,
                        creationDate = DateTime.UtcNow,
                        weight = resolvedWeights[i],
                        trackingNumber = pkgTrackingNumber,
                    };
                    _db.packages.Add(pkg);
                    await _db.SaveChangesAsync();

                    createdPackages.Add(new
                    {
                        pkg.id_Package,
                        pkg.labelFile,
                        pkg.weight,
                        pkg.trackingNumber,
                        packageIndex = i + 1,
                    });
                }
            }
            else
            {
                // Custom courier path — generate QuestPDF labels
                var dir = Path.Combine(webRoot, "labels", shipment.id_Shipment.ToString());
                Directory.CreateDirectory(dir);

                var rng = new Random();

                for (int i = 0; i < dto.PackageCount; i++)
                {
                    string pkgTrackingNumber;
                    do
                    {
                        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                        var suffix = rng.Next(1000, 9999);
                        pkgTrackingNumber = $"PKG-{companyId}-{dto.OrderId}-{ts}-{suffix}";
                    }
                    while (await _db.packages.AnyAsync(p => p.trackingNumber == pkgTrackingNumber));

                    string labelUrl = LabelGenerator.Generate(
                        webRootPath: webRoot,
                        shipmentId: shipment.id_Shipment,
                        packageIndex: i + 1,
                        totalPackages: dto.PackageCount,
                        trackingNumber: pkgTrackingNumber,
                        senderName: senderName,
                        senderAddress: senderAddress,
                        senderPhone: senderPhone,
                        recipientName: recipientName,
                        recipientAddress: recipientAddress,
                        recipientPhone: recipientPhone,
                        courierName: courierName,
                        shippingDate: shippingDateStr,
                        estimatedDelivery: estimatedDateStr
                    );

                    var pkg = new package
                    {
                        fk_Shipmentid_Shipment = shipment.id_Shipment,
                        labelFile = labelUrl,
                        creationDate = DateTime.UtcNow,
                        weight = resolvedWeights[i],
                        trackingNumber = pkgTrackingNumber,
                    };
                    _db.packages.Add(pkg);
                    await _db.SaveChangesAsync();

                    createdPackages.Add(new
                    {
                        pkg.id_Package,
                        pkg.labelFile,
                        pkg.weight,
                        pkg.trackingNumber,
                        packageIndex = i + 1,
                    });
                }

                shipment.trackingNumber = ((dynamic)createdPackages[0]).trackingNumber;
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            await _notif.NotifyShipmentStatusAsync(shipment.id_Shipment, 1, companyId);

            return Ok(new
            {
                shipmentId = shipment.id_Shipment,
                trackingNumber = shipment.trackingNumber,
                packageCount = dto.PackageCount,
                packages = createdPackages,
                providerShipmentId = shipment.providerShipmentId,
                providerParcelNumber = shipment.providerParcelNumber,
            });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return StatusCode(500, ex.InnerException?.Message ?? ex.Message);
        }
    }

    // POST /api/shipments/{id}/status

    [HttpPost("{id:int}/status")]
    public async Task<IActionResult> AddShipmentStatus(int id, [FromBody] AddShipmentStatusDto dto)
    {
        int companyId;
        try { companyId = GetRequiredCompanyId(); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }

        var shipment = await _db.shipments
            .FirstOrDefaultAsync(s => s.id_Shipment == id && s.fk_Companyid_Company == companyId);
        if (shipment == null) return NotFound();

        var typeExists = await _db.shipment_status_types
            .AnyAsync(t => t.id_ShipmentStatusType == dto.StatusTypeId);
        if (!typeExists) return BadRequest("Invalid status type.");

        _db.shipment_statuses.Add(new shipment_status
        {
            fk_Shipmentid_Shipment = id,
            fk_ShipmentStatusTypeid_ShipmentStatusType = dto.StatusTypeId,
            date = dto.Date ?? DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        await _notif.NotifyShipmentStatusAsync(id, dto.StatusTypeId, companyId);
        return Ok();
    }

    // GET /api/shipments/status-types

    [HttpGet("status-types")]
    public async Task<IActionResult> GetStatusTypes()
    {
        var types = await _db.shipment_status_types
            .AsNoTracking()
            .Select(t => new { t.id_ShipmentStatusType, t.name })
            .ToListAsync();
        return Ok(types);
    }

    // DELETE /api/shipments/{id} 

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteShipment(int id)
    {
        int companyId;
        try { companyId = GetRequiredCompanyId(); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }

        var shipment = await _db.shipments
            .FirstOrDefaultAsync(s => s.id_Shipment == id && s.fk_Companyid_Company == companyId);
        if (shipment == null) return NotFound();

        _db.shipments.Remove(shipment);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    //  Helper 

    private static string MapCountry(string? country) => country?.ToUpperInvariant() switch
    {
        "LIETUVA" or "LIETUVOS RESPUBLIKA" or "LT" => "LT",
        "LATVIJA" or "LATVIJOS RESPUBLIKA" or "LV" => "LV",
        "ESTIJA" or "ESTIJOS RESPUBLIKA" or "EE" => "EE",
        "LENKIJA" or "LENKIJOS RESPUBLIKA" or "PL" => "PL",
        "VOKIETIJA" or "VOKIETIJOS FEDERACINĖ RESPUBLIKA" or "DE" => "DE",
        _ => "LT"
    };
}