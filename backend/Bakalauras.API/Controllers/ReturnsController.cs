// Controllers/ReturnsController.cs
//
// Address logic for return labels:
//   SENDER  = the client returning the goods.
//             Uses ret.returnStreet/City/PostalCode (what they filled in on the return form).
//             Falls back to their current profile address in client_companies only if
//             they left the return address blank.
//
//   RECIPIENT = the COMPANY receiving the returned goods.
//               Uses company.returnStreet/City/PostalCode (the company's structured return address).
//               Falls back to company.shippingStreet then company.address — never a random value.

using Bakalauras.API.Models;
using Bakalauras.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/returns")]
[Authorize]
public class ReturnsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly CourierProviderFactory _providerFactory;
    private readonly INotificationService _notif;

    public ReturnsController(
        AppDbContext db,
        IWebHostEnvironment env,
        CourierProviderFactory providerFactory,
        INotificationService notif)
    {
        _db              = db;
        _env             = env;
        _providerFactory = providerFactory;
        _notif           = notif;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private int GetRequiredCompanyId()
    {
        var id = User.GetCompanyId();
        if (id <= 0) throw new UnauthorizedAccessException("No active company selected.");
        return id;
    }

    private async Task<bool> IsStaffAsync(int companyId)
    {
        if (User.IsMasterAdmin()) return true;
        var userId = User.GetUserId();
        var role = await _db.company_users
            .AsNoTracking()
            .Where(cu => cu.fk_Companyid_Company == companyId && cu.fk_Usersid_Users == userId)
            .Select(cu => cu.role)
            .FirstOrDefaultAsync();
        return role is "OWNER" or "ADMIN" or "STAFF";
    }

    // ── GET /api/returns/all ──────────────────────────────────────────────────

    [HttpGet("all")]
    public async Task<IActionResult> ListAll()
    {
        int companyId;
        try { companyId = GetRequiredCompanyId(); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }

        if (!await IsStaffAsync(companyId))
            return Forbid("Only company staff can view returns.");

        var returns = await _db.product_returns
            .AsNoTracking()
            .Where(r => r.fk_Companyid_Company == companyId)
            .OrderByDescending(r => r.id_Returns)
            .Select(r => new
            {
                r.id_Returns,
                r.date,
                displayStatusId = r.fk_Adminid_Users == null
                    ? 1
                    : r.fk_ReturnStatusTypeid_ReturnStatusType,
                r.fk_ReturnStatusTypeid_ReturnStatusType,
                statusName = r.fk_Adminid_Users == null
                    ? "Sukurtas"
                    : r.fk_ReturnStatusTypeid_ReturnStatusTypeNavigation.name,
                r.returnMethod,
                r.clientNote,
                r.employeeNote,
                orderId             = r.fk_ordersid_orders,
                itemCount           = r.return_items.Count,
                totalAmount         = r.return_items.Sum(ri => ri.returnSubTotal),
                clientName          = r.fk_Clientid_Users > 0
                    ? r.fk_Clientid_UsersNavigation.name + " " + r.fk_Clientid_UsersNavigation.surname
                    : null,
                clientEmail         = r.fk_Clientid_Users > 0
                    ? r.fk_Clientid_UsersNavigation.email
                    : null,
                evaluationSubmitted = r.fk_Adminid_Users != null,
            })
            .ToListAsync();

        return Ok(returns);
    }

    // ── GET /api/returns/{id} ─────────────────────────────────────────────────

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetReturn(int id)
    {
        int companyId;
        try { companyId = GetRequiredCompanyId(); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }

        if (!await IsStaffAsync(companyId))
            return Forbid();

        var ret = await _db.product_returns
            .AsNoTracking()
            .Where(r => r.id_Returns == id && r.fk_Companyid_Company == companyId)
            .Select(r => new
            {
                r.id_Returns,
                r.date,
                r.fk_ReturnStatusTypeid_ReturnStatusType,
                displayStatusId = r.fk_Adminid_Users == null
                    ? 1
                    : r.fk_ReturnStatusTypeid_ReturnStatusType,
                statusName = r.fk_Adminid_Users == null
                    ? "Sukurtas"
                    : r.fk_ReturnStatusTypeid_ReturnStatusTypeNavigation.name,
                evaluationSubmitted = r.fk_Adminid_Users != null,
                r.returnMethod,
                r.clientNote,
                r.employeeNote,
                r.returnStreet,
                r.returnCity,
                r.returnPostalCode,
                r.returnCountry,
                r.returnCourierId,
                r.fk_Courierid_Courier,
                r.returnLockerId,
                r.returnLockerName,
                r.returnLockerAddress,
                r.returnLat,
                r.returnLng,
                r.fk_Adminid_Users,
                orderId     = r.fk_ordersid_orders,
                clientName  = r.fk_Clientid_UsersNavigation.name + " " + r.fk_Clientid_UsersNavigation.surname,
                clientEmail = r.fk_Clientid_UsersNavigation.email,
                clientPhone = r.fk_Clientid_UsersNavigation.phoneNumber,
                clientId    = r.fk_Clientid_Users,
                items = r.return_items.Select(ri => new
                {
                    ri.id_ReturnItem,
                    ri.quantity,
                    ri.returnSubTotal,
                    ri.evaluationComment,
                    ri.evaluation,
                    ri.evaluationDate,
                    ri.imageUrls,
                    reason = ri.reasonId != null && ri.reasonNavigation != null
                        ? ri.reasonNavigation.name
                        : ri.reason,
                    product = new
                    {
                        id   = ri.fk_OrdersProductid_OrdersProductNavigation.fk_Productid_Product,
                        name = ri.fk_OrdersProductid_OrdersProductNavigation.fk_Productid_ProductNavigation.name,
                        unit = ri.fk_OrdersProductid_OrdersProductNavigation.fk_Productid_ProductNavigation.unit,
                        imageUrl = ri.fk_OrdersProductid_OrdersProductNavigation
                                      .fk_Productid_ProductNavigation.product_images
                                      .OrderBy(pi => pi.sortOrder)
                                      .Select(pi => pi.url)
                                      .FirstOrDefault()
                    }
                }).ToList()
            })
            .FirstOrDefaultAsync();

        if (ret == null) return NotFound("Return not found or does not belong to your company.");
        return Ok(ret);
    }

    // ── PUT /api/returns/{id}/evaluate/open ───────────────────────────────────

    [HttpPut("{id:int}/evaluate/open")]
    public async Task<IActionResult> MarkAsBeingEvaluated(int id)
    {
        int companyId;
        try { companyId = GetRequiredCompanyId(); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }

        if (!await IsStaffAsync(companyId))
            return Forbid();

        var ret = await _db.product_returns
            .FirstOrDefaultAsync(r => r.id_Returns == id && r.fk_Companyid_Company == companyId);
        if (ret == null) return NotFound();

        if (ret.fk_ReturnStatusTypeid_ReturnStatusType == 1)
        {
            ret.fk_ReturnStatusTypeid_ReturnStatusType = 2;
            await _db.SaveChangesAsync();
            await _notif.NotifyReturnStatusAsync(id, 2, companyId);
        }

        return Ok(new { statusId = ret.fk_ReturnStatusTypeid_ReturnStatusType });
    }

    // ── PUT /api/returns/{id}/evaluate ────────────────────────────────────────

    [HttpPut("{id:int}/evaluate")]
    public async Task<IActionResult> Evaluate(int id, [FromBody] EvaluateReturnDto dto)
    {
        int companyId;
        try { companyId = GetRequiredCompanyId(); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }

        if (!await IsStaffAsync(companyId))
            return Forbid("Only company staff can evaluate returns.");

        var ret = await _db.product_returns
            .FirstOrDefaultAsync(r => r.id_Returns == id && r.fk_Companyid_Company == companyId);
        if (ret == null) return NotFound("Return not found.");

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // ── 1. Save per-item evaluations ──────────────────────────────────
            var itemIds = dto.Items?.Select(i => i.ReturnItemId).ToList() ?? new();
            if (itemIds.Count > 0)
            {
                var dbItems = await _db.return_items
                    .Where(ri => ri.fk_Returnsid_Returns == id && itemIds.Contains(ri.id_ReturnItem))
                    .ToListAsync();

                foreach (var itemDto in dto.Items ?? new())
                {
                    var item = dbItems.FirstOrDefault(i => i.id_ReturnItem == itemDto.ReturnItemId);
                    if (item == null) continue;
                    item.evaluation        = itemDto.Evaluation;
                    item.evaluationComment = itemDto.EvaluationComment?.Trim();
                    item.evaluationDate    = DateOnly.FromDateTime(DateTime.UtcNow);
                }
            }

            var allItems = await _db.return_items
                .Where(ri => ri.fk_Returnsid_Returns == id)
                .ToListAsync();

            // ── 2. Auto-derive status ─────────────────────────────────────────
            bool allDeclined = allItems.Count > 0 && allItems.All(i => i.evaluation == false);
            bool anyApproved = allItems.Any(i => i.evaluation == true);

            ret.employeeNote     = dto.EmployeeNote?.Trim();
            ret.fk_Adminid_Users = User.GetUserId();

            bool labelsCreated = false;

            if (anyApproved)
            {
                var approvedItems = allItems.Where(i => i.evaluation == true).ToList();
                int packageCount  = approvedItems.Count;

                // ── Sender = the CLIENT who is shipping the return ────────────
                var clientUser = await _db.users.AsNoTracking()
                    .Where(u => u.id_Users == ret.fk_Clientid_Users)
                    .Select(u => new { u.name, u.surname, u.phoneNumber, u.email })
                    .FirstOrDefaultAsync();

                // Profile address used ONLY as a fallback if the client left
                // their return address blank on the return form
                var clientProfileAddress = await _db.client_companies.AsNoTracking()
                    .Where(x => x.fk_Companyid_Company == companyId && x.fk_Clientid_Users == ret.fk_Clientid_Users)
                    .Select(x => new { x.deliveryAddress, x.city, x.country })
                    .FirstOrDefaultAsync();

                var senderName    = $"{clientUser?.name} {clientUser?.surname}".Trim();
                var senderPhone   = clientUser?.phoneNumber ?? "";

                // Return form address takes priority — profile address is only a fallback
                var senderStreet  = !string.IsNullOrWhiteSpace(ret.returnStreet)
                    ? ret.returnStreet
                    : (clientProfileAddress?.deliveryAddress ?? "");

                var senderCity    = !string.IsNullOrWhiteSpace(ret.returnCity)
                    ? ret.returnCity
                    : (clientProfileAddress?.city ?? "");

                var senderPostal  = (ret.returnPostalCode ?? "")
                    .Replace("-", "").Replace(" ", "");

                var senderCountry = MapCountry(
                    !string.IsNullOrWhiteSpace(ret.returnCountry)
                        ? ret.returnCountry
                        : clientProfileAddress?.country);

                // ── Recipient = the COMPANY receiving the return ───────────────
                var company = await _db.companies.AsNoTracking()
                    .Where(c => c.id_Company == companyId)
                    .Select(c => new
                    {
                        c.name,
                        c.phoneNumber,
                        c.email,
                        // Use structured return address first, fall back to shipping address,
                        // then the legacy single-field address — but never use random data
                        c.returnStreet,
                        c.returnCity,
                        c.returnPostalCode,
                        c.returnCountry,
                        c.shippingStreet,
                        c.shippingCity,
                        c.shippingPostalCode,
                        c.shippingCountry,
                        c.address   // legacy fallback only
                    })
                    .FirstOrDefaultAsync();

                // Build recipient address with clear fallback chain
                var recipientStreet = FirstNonEmpty(
                    company?.returnStreet,
                    company?.shippingStreet,
                    company?.address);

                var recipientCity = FirstNonEmpty(
                    company?.returnCity,
                    company?.shippingCity);

                var recipientPostalCode = FirstNonEmpty(
                    company?.returnPostalCode,
                    company?.shippingPostalCode)
                    .Replace("-", "").Replace(" ", "");

                var recipientCountry = MapCountry(FirstNonEmpty(
                    company?.returnCountry,
                    company?.shippingCountry,
                    "LT"));

                // ── Courier ───────────────────────────────────────────────────
                courier? courier = ret.fk_Courierid_Courier.HasValue
                    ? await _db.couriers.FindAsync(ret.fk_Courierid_Courier.Value)
                    : null;

                var courierType = courier?.type ?? "CUSTOM";
                var integKey    = CourierProviderFactory.GetIntegrationKey(courierType);
                bool isProvider = integKey != null;

                var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var orderId = ret.fk_ordersid_orders ?? 0;

                var returnShipment = new shipment
                {
                    trackingNumber        = "",
                    shippingDate          = DateTime.UtcNow,
                    estimatedDeliveryDate = DateTime.UtcNow.AddDays(courier?.deliveryTermDays ?? 3),
                    fk_Courierid_Courier  = ret.fk_Courierid_Courier,
                    fk_Ordersid_Orders    = orderId,
                    fk_Returnsid_Returns  = id,
                    fk_Companyid_Company  = companyId,
                    providerLockerId      = ret.returnLockerId,
                    DeliveryLat           = ret.returnLat,
                    DeliveryLng           = ret.returnLng,
                };
                _db.shipments.Add(returnShipment);
                await _db.SaveChangesAsync();

                _db.shipment_statuses.Add(new shipment_status
                {
                    fk_Shipmentid_Shipment                     = returnShipment.id_Shipment,
                    fk_ShipmentStatusTypeid_ShipmentStatusType = 5,
                    date                                       = DateTime.UtcNow
                });

                var labelDir = Path.Combine(webRoot, "labels", returnShipment.id_Shipment.ToString());
                Directory.CreateDirectory(labelDir);

                var labelCourierName = courier?.name ?? "—";
                var shippingDateStr = DateTime.UtcNow.ToString("yyyy-MM-dd");
                var estimatedStr    = DateTime.UtcNow.AddDays(courier?.deliveryTermDays ?? 3).ToString("yyyy-MM-dd");
                var senderAddr      = BuildAddressLine(senderStreet, senderCity, senderCountry);
                var recipientAddr   = BuildAddressLine(recipientStreet, recipientCity, recipientCountry);

                if (isProvider)
                {
                    // ── Provider path (DPD etc.) ──────────────────────────────
                    ICourierProvider provider;
                    try { provider = await _providerFactory.GetProviderAsync(companyId, courierType); }
                    catch (InvalidOperationException ex)
                    {
                        await tx.RollbackAsync();
                        return BadRequest(ex.Message);
                    }

                    var providerReq = new CourierShipmentRequest
                    {
                        SenderName          = senderName,
                        SenderPhone         = senderPhone,
                        SenderStreet        = senderStreet,
                        SenderCity          = senderCity,
                        SenderPostalCode    = senderPostal,
                        SenderCountry       = senderCountry,

                        RecipientName        = company?.name ?? "—",
                        RecipientEmail       = company?.email ?? "",
                        RecipientPhone       = company?.phoneNumber ?? "",
                        RecipientStreet      = recipientStreet,
                        RecipientCity        = recipientCity,
                        RecipientPostalCode  = recipientPostalCode,
                        RecipientCountry     = recipientCountry,

                        LockerId        = ret.returnLockerId,
                        PackageCount    = packageCount,
                        PackageWeightKg = 1.0,
                        OrderReference  = $"Return-{id}",
                    };

                    var result = await provider.CreateShipmentAsync(providerReq);

                    if (result.ErrorMessage == null)
                    {
                        returnShipment.trackingNumber      = result.ProviderShipmentId;
                        returnShipment.providerShipmentId  = result.ProviderShipmentId;
                        returnShipment.providerParcelNumber = result.ParcelNumbers.Count > 0
                            ? string.Join(",", result.ParcelNumbers)
                            : result.ProviderShipmentId;

                        for (int i = 0; i < packageCount; i++)
                        {
                            var pkgTracking = i < result.ParcelNumbers.Count
                                ? result.ParcelNumbers[i]
                                : result.ProviderShipmentId;

                            byte[]? labelBytes = result.PerParcelLabelBytes.Count > 0
                                ? (i < result.PerParcelLabelBytes.Count
                                    ? result.PerParcelLabelBytes[i]
                                    : result.PerParcelLabelBytes.Last())
                                : null;

                            string? labelUrl = null;
                            if (labelBytes != null)
                            {
                                var filePath = Path.Combine(labelDir, $"label_{i + 1}.pdf");
                                await System.IO.File.WriteAllBytesAsync(filePath, labelBytes);
                                labelUrl = $"/labels/{returnShipment.id_Shipment}/label_{i + 1}.pdf";
                            }
                            else
                            {
                                labelUrl = LabelGenerator.Generate(
                                    webRootPath:       webRoot,
                                    shipmentId:        returnShipment.id_Shipment,
                                    packageIndex:      i + 1,
                                    totalPackages:     packageCount,
                                    trackingNumber:    pkgTracking,
                                    senderName:        senderName,
                                    senderAddress:     senderAddr,
                                    senderPhone:       senderPhone,
                                    recipientName:     company?.name ?? "—",
                                    recipientAddress:  recipientAddr,
                                    recipientPhone:    company?.phoneNumber ?? "",
                                    courierName:       labelCourierName,
                                    shippingDate:      shippingDateStr,
                                    estimatedDelivery: estimatedStr
                                );
                            }

                            _db.packages.Add(new package
                            {
                                fk_Shipmentid_Shipment = returnShipment.id_Shipment,
                                labelFile              = labelUrl,
                                creationDate           = DateTime.UtcNow,
                                weight                 = 1.0,
                                trackingNumber         = pkgTracking,
                            });
                        }
                        labelsCreated = true;
                    }
                    else
                    {
                        var rng = new Random();
                        string? firstTracking = null;

                        for (int i = 0; i < packageCount; i++)
                        {
                            string pkgTracking;
                            do
                            {
                                var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                                var suffix = rng.Next(1000, 9999);
                                pkgTracking = $"RET-{companyId}-{id}-{ts}-{suffix}";
                            }
                            while (await _db.packages.AnyAsync(p => p.trackingNumber == pkgTracking));

                            if (firstTracking == null) firstTracking = pkgTracking;

                            var labelUrl = LabelGenerator.Generate(
                                webRootPath:       webRoot,
                                shipmentId:        returnShipment.id_Shipment,
                                packageIndex:      i + 1,
                                totalPackages:     packageCount,
                                trackingNumber:    pkgTracking,
                                senderName:        senderName,
                                senderAddress:     senderAddr,
                                senderPhone:       senderPhone,
                                recipientName:     company?.name ?? "—",
                                recipientAddress:  recipientAddr,
                                recipientPhone:    company?.phoneNumber ?? "",
                                courierName:       labelCourierName,
                                shippingDate:      shippingDateStr,
                                estimatedDelivery: estimatedStr
                            );

                            _db.packages.Add(new package
                            {
                                fk_Shipmentid_Shipment = returnShipment.id_Shipment,
                                labelFile              = labelUrl,
                                creationDate           = DateTime.UtcNow,
                                weight                 = 1.0,
                                trackingNumber         = pkgTracking,
                            });
                        }

                        returnShipment.trackingNumber = firstTracking ?? $"RET-{id}";
                        labelsCreated = true;
                    }
                }
                else
                {
                    // ── Custom courier path — professional QuestPDF labels ─────
                    var rng = new Random();
                    var courierName     = courier?.name ?? "—";
                    string? firstTracking = null;
                    for (int i = 0; i < packageCount; i++)
                    {
                        string pkgTracking;
                        do
                        {
                            var ts     = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                            var suffix = rng.Next(1000, 9999);
                            pkgTracking = $"RET-{companyId}-{id}-{ts}-{suffix}";
                        }
                        while (await _db.packages.AnyAsync(p => p.trackingNumber == pkgTracking));

                        if (firstTracking == null) firstTracking = pkgTracking;

                        // Generate professional label with barcode + QR
                        string labelUrl = LabelGenerator.Generate(
                            webRootPath:       webRoot,
                            shipmentId:        returnShipment.id_Shipment,
                            packageIndex:      i + 1,
                            totalPackages:     packageCount,
                            trackingNumber:    pkgTracking,
                            senderName:        senderName,
                            senderAddress:     senderAddr,
                            senderPhone:       senderPhone,
                            recipientName:     company?.name ?? "—",
                            recipientAddress:  recipientAddr,
                            recipientPhone:    company?.phoneNumber ?? "",
                            courierName:       courierName,
                            shippingDate:      shippingDateStr,
                            estimatedDelivery: estimatedStr
                        );

                        _db.packages.Add(new package
                        {
                            fk_Shipmentid_Shipment = returnShipment.id_Shipment,
                            labelFile              = labelUrl,
                            creationDate           = DateTime.UtcNow,
                            weight                 = 1.0,
                            trackingNumber         = pkgTracking,
                        });
                    }

                    returnShipment.trackingNumber = firstTracking ?? $"RET-{id}";
                    labelsCreated = true;
                }

                await _db.SaveChangesAsync();
            }

            // ── 4. Set final status ───────────────────────────────────────────
            int finalStatus = allDeclined ? 6
                            : labelsCreated ? 7
                            : 5;

            ret.fk_ReturnStatusTypeid_ReturnStatusType = finalStatus;
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            await _notif.NotifyReturnStatusAsync(id, finalStatus, companyId);

            return Ok(new { returnId = id, statusId = finalStatus, labelsCreated });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return StatusCode(500, ex.InnerException?.Message ?? ex.Message);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Returns the first non-null, non-whitespace string from the candidates.</summary>
    private static string FirstNonEmpty(params string?[] candidates)
        => candidates.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s)) ?? "";

    /// <summary>Joins non-empty address parts with ", ".</summary>
    private static string BuildAddressLine(params string?[] parts)
        => string.Join(", ", parts.Where(s => !string.IsNullOrWhiteSpace(s)));

    private static string MapCountry(string? country) => country?.ToUpperInvariant() switch
    {
        "LIETUVA" or "LIETUVOS RESPUBLIKA" or "LT" => "LT",
        "LATVIJA" or "LATVIJOS RESPUBLIKA" or "LV" => "LV",
        "ESTIJA"  or "ESTIJOS RESPUBLIKA"  or "EE" => "EE",
        "LENKIJA" or "LENKIJOS RESPUBLIKA" or "PL" => "PL",
        "VOKIETIJA" or "VOKIETIJOS FEDERACINĖ RESPUBLIKA" or "DE" => "DE",
        _ => "LT"
    };
}

// ── DTOs ──────────────────────────────────────────────────────────────────────

public class EvaluateReturnDto
{
    /// <summary>Kept for backwards compat — status is now derived automatically from item evaluations.</summary>
    public int? StatusId { get; set; }
    public string? EmployeeNote { get; set; }
    public List<ReturnItemEvalDto> Items { get; set; } = new();
}

public class ReturnItemEvalDto
{
    public int     ReturnItemId      { get; set; }
    public bool    Evaluation        { get; set; }
    public string? EvaluationComment { get; set; }
}
