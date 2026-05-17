using Bakalauras.API.Models;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Bakalauras.API.Services;

public interface IInvoiceService
{
    /// <summary>
    /// Creates (or fetches existing) invoice for the order, generates a PDF,
    /// and returns the physical file path for SmtpClient attachment.
    /// Returns null if the order is not found or PDF generation fails.
    /// </summary>
    Task<string?> GenerateAndSaveAsync(int orderId, int companyId);
}

public class InvoiceService : IInvoiceService
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<InvoiceService> _log;

    public InvoiceService(AppDbContext db, IWebHostEnvironment env, ILogger<InvoiceService> log)
    {
        _db = db;
        _env = env;
        _log = log;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<string?> GenerateAndSaveAsync(int orderId, int companyId)
    {
        // Return existing invoice physical path if already generated
        var existing = await _db.invoices
            .FirstOrDefaultAsync(i => i.fk_Ordersid_Orders == orderId);

        if (existing?.fileUrl != null)
            return MapUrlToPath(existing.fileUrl);

        // ── Load order data ───────────────────────────────────────────────────
        var order = await _db.orders
            .AsNoTracking()
            .Where(o => o.id_Orders == orderId && o.fk_Companyid_Company == companyId)
            .Select(o => new
            {
                o.id_Orders,
                o.OrdersDate,
                o.totalAmount,
                o.deliveryPrice,
                o.paymentMethod,

                clientName = o.fk_Clientid_UsersNavigation.name + " " + o.fk_Clientid_UsersNavigation.surname,
                clientEmail = o.fk_Clientid_UsersNavigation.email,
                clientPhone = o.fk_Clientid_UsersNavigation.phoneNumber,

                companyName = o.fk_Companyid_CompanyNavigation.name,
                companyCode = o.fk_Companyid_CompanyNavigation.companyCode,
                companyEmail = o.fk_Companyid_CompanyNavigation.email,
                companyPhone = o.fk_Companyid_CompanyNavigation.phoneNumber,

                // ── Delivery address from the ORDER snapshot ──────────────────
                // This is what the client actually chose for this specific order.
                // It may differ from their current profile address in client_companies
                // if they changed delivery for this order via /api/client/orders/{id}/delivery.
                o.snapshotDeliveryMethod,
                o.snapshotDeliveryAddress,
                o.snapshotCity,
                o.snapshotCountry,
                o.snapshotLockerName,
                o.snapshotLockerAddress,

                // Billing data (VAT number etc.) comes from client_companies —
                // this is separate from the delivery address and does not change per-order.
                vat = _db.client_companies
                    .Where(cc => cc.fk_Companyid_Company == companyId && cc.fk_Clientid_Users == o.fk_Clientid_Users)
                    .Select(cc => cc.vat)
                    .FirstOrDefault(),

                items = o.ordersproducts.Select(op => new
                {
                    name = op.fk_Productid_ProductNavigation.name,
                    quantity = op.quantity,
                    unitPrice = op.unitPrice,
                    vatValue = op.vatValue,
                }).ToList()
            })
            .FirstOrDefaultAsync();

        if (order == null) return null;

        // Build display address from snapshot fields
        // For locker delivery show the locker name/address instead of a home address.
        var displayAddress = order.snapshotDeliveryMethod?.ToUpperInvariant() == "LOCKER"
            ? string.Join(", ", new[]
                {
                    order.snapshotLockerName,
                    order.snapshotLockerAddress
                }
                .Where(s => !string.IsNullOrWhiteSpace(s)))
            : string.Join(", ", new[]
                {
                    order.snapshotDeliveryAddress,
                    order.snapshotCity,
                    order.snapshotCountry
                }
                .Where(s => !string.IsNullOrWhiteSpace(s)));

        if (string.IsNullOrWhiteSpace(displayAddress))
            displayAddress = "—";

        // Invoice metadata
        var invoiceNumber = $"INV-{companyId}-{orderId}-{DateTime.UtcNow:yyyyMMdd}";
        var vatTotal = order.items.Sum(i => i.vatValue * i.quantity);
        var subtotal = order.items.Sum(i => i.unitPrice * i.quantity);

        // Persist invoice record
        var inv = existing ?? new invoice();
        inv.invoiceNumber = invoiceNumber;
        inv.date = DateTime.UtcNow;
        inv.dueDate = DateTime.UtcNow.AddDays(30);
        inv.total = order.totalAmount;
        inv.vatTotal = vatTotal;
        inv.fk_Ordersid_Orders = orderId;

        if (existing == null)
            _db.invoices.Add(inv);

        await _db.SaveChangesAsync();

        // Generate PDF
        try
        {
            var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var dir = Path.Combine(webRoot, "invoices", orderId.ToString());
            Directory.CreateDirectory(dir);

            var filePath = Path.Combine(dir, $"invoice_{orderId}.pdf");
            var fileUrl = $"/invoices/{orderId}/invoice_{orderId}.pdf";

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    // Header 
                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text(order.companyName)
                               .SemiBold().FontSize(16);

                            row.RelativeItem().AlignRight().Column(c =>
                            {
                                c.Item().Text("Sąskaita faktūra").Bold().FontSize(14);
                                c.Item().Text($"Nr. {invoiceNumber}").FontSize(10);
                                c.Item().Text($"Data: {inv.date:yyyy-MM-dd}").FontSize(10);
                            });
                        });
                        col.Item().PaddingTop(4).LineHorizontal(1);
                    });

                    // Content
                    page.Content().PaddingTop(16).Column(col =>
                    {
                        // Seller / Buyer block
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Pardavėjas:").SemiBold();
                                c.Item().Text(order.companyName);
                                c.Item().Text($"Įm. kodas: {order.companyCode}");
                                c.Item().Text($"Tel: {order.companyPhone}");
                                c.Item().Text($"El. p.: {order.companyEmail}");
                            });

                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Pirkėjas:").SemiBold();
                                c.Item().Text(order.clientName);
                                // Display address comes from the order snapshot —
                                // reflects what the client actually chose for this order
                                c.Item().Text(displayAddress);
                                if (!string.IsNullOrWhiteSpace(order.vat))
                                    c.Item().Text($"PVM kodas: {order.vat}");
                                c.Item().Text($"Tel: {order.clientPhone}");
                                c.Item().Text($"El. p.: {order.clientEmail}");
                            });
                        });

                        // Items table
                        col.Item().PaddingTop(16).Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(4); // name
                                cols.RelativeColumn(1); // qty
                                cols.RelativeColumn(2); // unit price
                                cols.RelativeColumn(2); // total
                            });

                            static IContainer HeaderCell(IContainer c) =>
                                c.Background("#2563eb").Padding(4);

                            table.Header(h =>
                            {
                                h.Cell().Element(HeaderCell).Text("Pavadinimas").FontColor("#fff").SemiBold();
                                h.Cell().Element(HeaderCell).Text("Kiekis").FontColor("#fff").SemiBold();
                                h.Cell().Element(HeaderCell).Text("Kaina").FontColor("#fff").SemiBold();
                                h.Cell().Element(HeaderCell).Text("Suma").FontColor("#fff").SemiBold();
                            });

                            bool shade = false;
                            foreach (var item in order.items)
                            {
                                var bg = shade ? "#f1f5f9" : "#ffffff";
                                shade = !shade;
                                var lineTotal = item.unitPrice * item.quantity;

                                static IContainer BodyCell(IContainer c, string bg) =>
                                    c.Background(bg).Padding(4);

                                table.Cell().Element(c => BodyCell(c, bg)).Text(item.name);
                                table.Cell().Element(c => BodyCell(c, bg)).Text(item.quantity.ToString());
                                table.Cell().Element(c => BodyCell(c, bg)).Text($"{item.unitPrice:F2} €");
                                table.Cell().Element(c => BodyCell(c, bg)).Text($"{lineTotal:F2} €");
                            }
                        });

                        // Totals
                        col.Item().PaddingTop(8).AlignRight().Column(c =>
                        {
                            c.Item().Text($"Pristatymas: {order.deliveryPrice:F2} €");
                            c.Item().Text($"Tarpinė suma (be PVM): {subtotal:F2} €");
                            c.Item().Text($"PVM (21%): {vatTotal:F2} €");
                            c.Item().Text($"Iš viso: {order.totalAmount:F2} €").SemiBold().FontSize(13);
                        });

                        col.Item().PaddingTop(16).Text($"Mokėjimo būdas: {order.paymentMethod ?? "—"}");
                        col.Item().Text($"Apmokėti iki: {inv.dueDate:yyyy-MM-dd}");
                    });

                    // Footer
                    page.Footer().AlignCenter().Text(
                        $"Sąskaita sugeneruota automatiškai • {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");
                });
            }).GeneratePdf(filePath);

            inv.fileUrl = fileUrl;
            await _db.SaveChangesAsync();

            // Return the physical path — SmtpClient needs this for attachment
            return filePath;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "PDF generation failed for order {OrderId}", orderId);
            return null;
        }
    }

    private string MapUrlToPath(string url)
    {
        var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        return Path.Combine(webRoot, url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
    }
}