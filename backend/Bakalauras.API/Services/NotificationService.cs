using Bakalauras.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Bakalauras.API.Services;

public interface INotificationService
{
    Task NotifyOrderStatusAsync(int orderId, int newStatusId, int companyId);
    Task NotifyShipmentStatusAsync(int shipmentId, int newStatusTypeId, int companyId);
    Task NotifyReturnStatusAsync(int returnId, int newStatusId, int companyId);
}

public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;
    private readonly IEmailService _email;
    private readonly IInvoiceService _invoiceService;
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _cfg;
    private readonly ILogger<NotificationService> _log;

    public NotificationService(
        AppDbContext db,
        IEmailService email,
        IInvoiceService invoiceService,
        IWebHostEnvironment env,
        IConfiguration cfg,
        ILogger<NotificationService> log)
    {
        _db = db;
        _email = email;
        _invoiceService = invoiceService;
        _env = env;
        _cfg = cfg;
        _log = log;
    }

    // ORDER
    public async Task NotifyOrderStatusAsync(int orderId, int newStatusId, int companyId)
    {
        var order = await _db.orders
            .AsNoTracking()
            .Where(o => o.id_Orders == orderId)
            .Select(o => new
            {
                o.id_Orders,
                clientId = o.fk_Clientid_Users,
                clientEmail = o.fk_Clientid_UsersNavigation.email,
                clientName = o.fk_Clientid_UsersNavigation.name,
            })
            .FirstOrDefaultAsync();

        if (order == null) return;

        string? theme = null;
        string? content = null;
        bool sendEmail = false;
        bool generateInvoice = false;

        switch (newStatusId)
        {
            case 1:
                theme = "Patvirtinkite pristatymo duomenis";
                content = $"Jūsų užsakymas #{orderId} gautas! Prašome patvirtinti pristatymo adresą ir pasirinkti pristatymo būdą paspaudę mygtuką žemiau.";
                sendEmail = true;
                break;
            case 4:
                theme = "Užsakymas vykdomas";
                content = $"Jūsų užsakymas #{orderId} pradėtas vykdyti.";
                sendEmail = true;
                generateInvoice = true;
                break;
            case 5:
                theme = "Užsakymas išsiųstas";
                content = $"Užsakymas #{orderId} jau pakeliui.";
                sendEmail = true;
                break;
            case 3:
                theme = "Užsakymas įvykdytas";
                content = $"Užsakymas #{orderId} pristatytas.";
                sendEmail = false;
                break;
            case 2:
                theme = "Užsakymas atšauktas";
                content = $"Užsakymas #{orderId} atšauktas.";
                sendEmail = true;
                break;
            default:
                return;
        }

        var notif = new notification
        {
            theme = theme,
            content = content,
            date = DateTime.UtcNow,
            type = "ORDER",
            referenceId = orderId,
            referenceType = "ORDER",
            fk_Usersid_Users = order.clientId,
            fk_Companyid_Company = null,
            visibleToClient = true,
            visibleToCompany = false
        };

        _db.notifications.Add(notif);

        string? invoicePath = null;
        if (generateInvoice)
        {
            try
            {
                invoicePath = await _invoiceService.GenerateAndSaveAsync(orderId, companyId);
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Invoice failed");
            }
        }

        await _db.SaveChangesAsync();

        if (sendEmail && !string.IsNullOrWhiteSpace(order.clientEmail))
        {
            try
            {
                string? ctaUrl = null;
                string? ctaLabel = null;

                if (newStatusId == 1)
                {
                    // Read from config or hardcode your frontend URL
                    var frontendBase = _cfg["FrontendBaseUrl"] ?? "http://46.101.161.47";
                    ctaUrl = $"{frontendBase}/client/profile#orders?order={orderId}";
                    ctaLabel = "Pasirinkti pristatymą";
                }

                var body = BuildEmailBody(order.clientName, theme, content, ctaUrl, ctaLabel);

                if (invoicePath != null)
                {
                    await _email.SendWithAttachmentAsync(
                        order.clientEmail, theme, body,
                        invoicePath, $"Invoice_{orderId}.pdf");
                }
                else
                {
                    await _email.SendAsync(order.clientEmail, theme, body);
                }

                notif.emailSent = true;
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Email failed");
            }
        }
    }

    // SHIPMENT
    public async Task NotifyShipmentStatusAsync(int shipmentId, int statusId, int companyId)
    {
        var shipment = await _db.shipments
            .Where(s => s.id_Shipment == shipmentId)
            .Select(s => new
            {
                s.id_Shipment,
                clientId = s.fk_Ordersid_OrdersNavigation.fk_Clientid_Users,
                clientEmail = s.fk_Ordersid_OrdersNavigation.fk_Clientid_UsersNavigation.email,
                clientName = s.fk_Ordersid_OrdersNavigation.fk_Clientid_UsersNavigation.name,
                trackingNumber = s.packages.Select(p => p.trackingNumber).ToList()
            })
            .FirstOrDefaultAsync();
        var trackingList = shipment.trackingNumber != null && shipment.trackingNumber.Any()
            ? string.Join("<br/>• ", shipment.trackingNumber)
            : "—";
        if (shipment == null) return;

        string theme = "";
        string content = "";
        bool sendEmail = false;
        bool visibleToClient = true;
        bool visibleToCompany = false;

        switch (statusId)
        {
            case 1:
                theme = "Siunta sukurta";
                content = $"Siunta #{shipmentId} sukurta. Jos būseną galite sekti mūsų svetainėje. Sekimo numeris: {trackingList}.";
                sendEmail = false;
                break;
            case 2:
                theme = "Siunta vežama";
                content = $"Siunta pakeliui. Jos būseną galite sekti mūsų svetainėje. Sekimo numeris: {trackingList}.";
                sendEmail = true;
                break;
            case 3:
                theme = "Siunta pristatyta";
                content = $"Siunta pristatyta. Ačiū, kad naudojatės mūsų paslaugomis!";
                sendEmail = true;
                break;
            case 4:
                theme = "Siunta vėluoja";
                content = $"Siunta vėluoja. Atsiprašome už nepatogumus, dirbame, kad ji pasiektų jus kuo greičiau.";
                sendEmail = true;
                break;

            case 5:
            case 6:
            case 7:
            case 8:
                theme = "Grąžinimo siuntos būsena";
                content = $"Grąžinimo siunta #{shipmentId} atnaujinta.";
                visibleToClient = false;
                visibleToCompany = true;
                sendEmail = false;
                break;
        }

        var notif = new notification
        {
            theme = theme,
            content = content,
            date = DateTime.UtcNow,
            type = "SHIPMENT",
            referenceId = shipmentId,
            referenceType = "SHIPMENT",
            fk_Usersid_Users = visibleToClient ? shipment.clientId : null,
            fk_Companyid_Company = visibleToCompany ? companyId : null,
            visibleToClient = visibleToClient,
            visibleToCompany = visibleToCompany
        };

        _db.notifications.Add(notif);
        await _db.SaveChangesAsync();

        if (sendEmail && visibleToClient && !string.IsNullOrWhiteSpace(shipment.clientEmail))
        {
            try
            {
                await _email.SendAsync(
                    shipment.clientEmail,
                    theme,
                    BuildEmailBody(shipment.clientName, theme, content));

                notif.emailSent = true;
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Email failed");
            }
        }
    }

    // RETURN
    public async Task NotifyReturnStatusAsync(int returnId, int statusId, int companyId)
    {
        var ret = await _db.product_returns
            .Where(r => r.id_Returns == returnId)
            .Select(r => new
            {
                r.fk_Clientid_Users,
                r.fk_Clientid_UsersNavigation.email,
                r.fk_Clientid_UsersNavigation.name,
                // Collect label URL paths from the return's shipments
                labelUrls = r.shipments
                    .Where(s => s.fk_Returnsid_Returns == returnId)   // only return shipments
                    .SelectMany(s => s.packages)
                    .Where(p => p.labelFile != null)
                    .Select(p => p.labelFile!)
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (ret == null) return;

        string theme = "";
        string content = "";
        bool sendEmail = false;

        switch (statusId)
        {
            case 1:
                theme = "Grąžinimas sukurtas";
                content = $"Grąžinimas #{returnId} pateiktas vertinimui.";
                sendEmail = false;
                break;
            case 2:
                theme = "Grąžinimas vertinamas";
                content = $"Grąžinimas #{returnId} vertinamas. Laukiame įvertinimo rezultatų.";
                sendEmail = true;
                break;
            case 5:
                theme = "Grąžinimas patvirtintas";
                content = $"Grąžinimas #{returnId} patvirtintas. Laukiame prekės sugrąžinimo.";
                sendEmail = true;
                break;
            case 6:
                theme = "Grąžinimas atmestas";
                content = $"Grąžinimas #{returnId} atmestas. Susisiekite su klientų aptarnavimo tarnyba dėl daugiau informacijos.";
                sendEmail = true;
                break;
            case 7:
                theme = "Etiketė paruošta";
                content = $"Atsisiųskite etiketę ir grąžinkite prekę naudodami šią etiketę. Etiketė prisegta prie šio pranešimo.";
                sendEmail = true;
                break;
            case 3:
                theme = "Gauta";
                content = $"Prekė gauta.";
                sendEmail = true;
                break;
            case 4:
                theme = "Užbaigta";
                content = $"Grąžinimas užbaigtas.";
                sendEmail = true;
                break;
        }

        var notif = new notification
        {
            theme = theme,
            content = content,
            date = DateTime.UtcNow,
            type = "RETURN",
            referenceId = returnId,
            referenceType = "RETURN",
            fk_Usersid_Users = ret.fk_Clientid_Users,
            visibleToClient = true,
            visibleToCompany = false
        };

        _db.notifications.Add(notif);
        await _db.SaveChangesAsync();

        if (sendEmail && !string.IsNullOrWhiteSpace(ret.email))
        {
            try
            {
                var body = BuildEmailBody(ret.name, theme, content);

                // Status 7 = "Etiketė paruošta" → attach every label PDF
                if (statusId == 7 && ret.labelUrls.Count > 0)
                {
                    var webRoot = _env.WebRootPath
                        ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

                    var attachments = ret.labelUrls
                        .Select(url => Path.Combine(
                            webRoot,
                            url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)))
                        .Where(File.Exists)
                        .Select((path, i) => (path, name: $"Label_{i + 1}.pdf"))  // ← named clearly
                        .ToList();

                    if (attachments.Count > 0)
                    {
                        // All labels in ONE email
                        await _email.SendWithAttachmentsAsync(ret.email, theme, body, attachments);
                    }
                    else
                    {
                        _log.LogWarning("Return {ReturnId}: label files not found on disk.", returnId);
                        await _email.SendAsync(ret.email, theme, body);
                    }
                }
                else
                {
                    await _email.SendAsync(ret.email, theme, body);
                }

                notif.emailSent = true;
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Email failed for return {ReturnId}", returnId);
            }
        }
    }

    private static string BuildEmailBody(
        string name,
        string subject,
        string message,
        string? ctaUrl = null,
        string? ctaLabel = null) =>
    $"""
<!DOCTYPE html>
<html>
<head>
<meta charset="UTF-8" />
</head>
<body style="margin:0;padding:0;background-color:#f5f7fb;font-family:Arial, sans-serif;">

<table width="100%" cellpadding="0" cellspacing="0" style="background-color:#f5f7fb;padding:24px 0;">
    <tr>
    <td align="center">
        <table width="100%" cellpadding="0" cellspacing="0"
            style="max-width:600px;background:#ffffff;border-radius:12px;
                    box-shadow:0 4px 8px rgba(0,0,0,0.08);overflow:hidden;">
        <tr>
            <td style="background:#1d4ed8;padding:20px 24px;color:white;">
            <h1 style="margin:0;font-size:20px;font-weight:600;">
                {subject}
            </h1>
            </td>
        </tr>
        <tr>
            <td style="padding:24px;color:#0b1220;font-size:14px;line-height:1.6;">
            <p style="margin:0 0 12px 0;">
                Sveiki, <strong>{name}</strong>,
            </p>
            <p style="margin:0 0 16px 0;color:#3f4d63;">
                {message}
            </p>
            {(ctaUrl != null ? $"""
            <p style="margin:24px 0 0 0;text-align:center;">
                <a href="{ctaUrl}"
                   style="display:inline-block;background:#1d4ed8;color:#ffffff;
                          text-decoration:none;padding:12px 28px;border-radius:8px;
                          font-size:14px;font-weight:600;">
                    {ctaLabel ?? "Atidaryti"}
                </a>
            </p>
            """ : "")}
            </td>
        </tr>
        <tr>
            <td style="border-top:1px solid #e5e7eb;"></td>
        </tr>
        <tr>
            <td style="padding:16px 24px;font-size:12px;color:#8b98ad;text-align:center;">
            Šis el. laiškas siųstas automatiškai.<br/>
            Prašome į jį neatsakyti.
            </td>
        </tr>
        </table>
    </td>
    </tr>
</table>

</body>
</html>
""";
}