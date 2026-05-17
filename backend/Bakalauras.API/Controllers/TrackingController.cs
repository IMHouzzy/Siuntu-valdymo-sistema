using Bakalauras.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/tracking")]
public class TrackingController : ControllerBase
{
    private readonly AppDbContext _db;

    public TrackingController(AppDbContext db)
    {
        _db = db;
    }

    // GET /api/tracking/{trackingNumber}
    // Returns:
    //   { type: "dpd",dpdUrl: "https://..." } — redirect client to DPD
    //   { type: "custom", shipment: { ... }, statuses: [...] } — show internal timeline
    //   404 if not found
    [HttpGet("{identifier}")]
    public async Task<IActionResult> Track(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            return BadRequest("Tracking identifier is required.");

        package? package = null;
        int shipmentId = 0;

        // CASE 1: Shipment ID
        if (int.TryParse(identifier, out var parsedId))
        {
            shipmentId = parsedId;

            package = await _db.packages
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.fk_Shipmentid_Shipment == shipmentId);
        }
        else
        {
            // CASE 2: Tracking Number
            package = await _db.packages
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.trackingNumber == identifier);

            if (package != null)
                shipmentId = package.fk_Shipmentid_Shipment;
        }

        if (package == null)
            return NotFound(new { message = "Siunta nerasta." });

        var shipment = await _db.shipments
            .AsNoTracking()
            .Where(s => s.id_Shipment == shipmentId)
            .Select(s => new
            {
                s.id_Shipment,
                s.trackingNumber,
                s.shippingDate,
                s.estimatedDeliveryDate,
                s.DeliveryLat,
                s.DeliveryLng,
                s.providerParcelNumber,
                s.providerLockerId,
                s.fk_Ordersid_Orders,
                courier = s.fk_Courierid_CourierNavigation == null ? null : new
                {
                    s.fk_Courierid_CourierNavigation.name,
                    s.fk_Courierid_CourierNavigation.type,
                },
                allPackages = s.packages.Select(p => new
                {
                    p.id_Package,
                    p.trackingNumber,
                    p.weight,
                    p.creationDate,
                    p.labelFile,
                }).ToList()
            })
            .FirstOrDefaultAsync();

        if (shipment == null)
            return NotFound(new { message = "Siunta nerasta." });

        var courierType = shipment.courier?.type ?? "CUSTOM";
        var isProvider = CourierProviderFactory.GetIntegrationKey(courierType) != null;

        if (isProvider)
        {
            var parcelNumber = package.trackingNumber;

            return Ok(new
            {
                type = "dpd",
                dpdUrl = $"https://www.dpdgroup.com/lt/mydpd/my-parcels/search?lang=lt&parcelNumber={Uri.EscapeDataString(parcelNumber)}",
                courierName = shipment.courier?.name,
                trackingNumber = parcelNumber
            });
        }

        // Custom courier — return full status history + order info
        var statuses = await _db.shipment_statuses
            .AsNoTracking()
            .Where(ss => ss.fk_Shipmentid_Shipment == shipment.id_Shipment)
            .OrderByDescending(ss => ss.date)
            .Select(ss => new
            {
                ss.id_ShipmentStatus,
                ss.date,
                typeId = ss.fk_ShipmentStatusTypeid_ShipmentStatusType,
                typeName = ss.fk_ShipmentStatusTypeid_ShipmentStatusTypeNavigation.name
            })
            .ToListAsync();

        // Load order + client delivery address for status messages
        var order = await _db.orders
            .AsNoTracking()
            .Where(o => o.id_Orders == shipment.fk_Ordersid_Orders)
            .Select(o => new
            {
                o.id_Orders,
                o.OrdersDate,
                o.totalAmount,
                o.paymentMethod,
                o.deliveryPrice,
                statusName = o.statusNavigation.name,
                client = new
                {
                    name = o.fk_Clientid_UsersNavigation.name,
                    surname = o.fk_Clientid_UsersNavigation.surname,
                },
                companyId = o.fk_Companyid_Company
            })
            .FirstOrDefaultAsync();

        // Get delivery address for the status message "Vežama" etc.
        string? deliveryAddress = null;
        if (order != null)
        {
            var cc = await _db.client_companies
                .AsNoTracking()
                .Where(x =>
                    x.fk_Companyid_Company == order.companyId &&
                    x.fk_Clientid_Users == _db.orders
                        .Where(o2 => o2.id_Orders == shipment.fk_Ordersid_Orders)
                        .Select(o2 => o2.fk_Clientid_Users)
                        .FirstOrDefault())
                .Select(x => new { x.deliveryAddress, x.city })
                .FirstOrDefaultAsync();

            deliveryAddress = string.Join(", ",
                new[] { cc?.deliveryAddress, cc?.city }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
        }

        return Ok(new
        {
            type = "custom",
            trackingNumber = package.trackingNumber,
            courierName = shipment.courier?.name ?? "Įmonės kurjeris",
            shipment = new
            {
                shipment.id_Shipment,
                shipment.shippingDate,
                shipment.estimatedDeliveryDate,
                shipment.DeliveryLat,
                shipment.DeliveryLng,
                shipment.providerLockerId,
            },
            packages = shipment.allPackages,
            statuses,
            deliveryAddress,
            order
        });
    }
}