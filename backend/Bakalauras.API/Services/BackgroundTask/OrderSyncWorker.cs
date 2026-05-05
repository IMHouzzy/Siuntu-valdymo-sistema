using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using Bakalauras.API.Models;
using Microsoft.EntityFrameworkCore;

public class OrderSyncWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;

    public OrderSyncWorker(IServiceScopeFactory scopeFactory, IHttpClientFactory httpClientFactory)
    {
        _scopeFactory      = scopeFactory;
        _httpClientFactory = httpClientFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait for ClientSyncWorker and ProductSyncWorker to finish first
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        try { await SyncAllCompanies(stoppingToken); }
        catch (Exception ex) { Console.WriteLine($"[OrderSyncWorker] Startup error: {ex}"); }

        while (!stoppingToken.IsCancellationRequested)
        {
            try { await SyncAllCompanies(stoppingToken); }
            catch (Exception ex) { Console.WriteLine($"[OrderSyncWorker] Error: {ex}"); }

            await Task.Delay(TimeSpan.FromMinutes(60), stoppingToken);
        }
    }

    private async Task SyncAllCompanies(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var integrations = await db.company_integrations
            .AsNoTracking()
            .Where(x => x.enabled == true && x.type == "BUTENT")
            .Select(x => new { x.fk_Companyid_Company, x.baseUrl, x.encryptedSecrets })
            .ToListAsync(ct);

        foreach (var integ in integrations)
        {
            ct.ThrowIfCancellationRequested();

            var (u, p, b) = IntegrationSecrets.TryUnpack(integ.encryptedSecrets);
            var baseUrl   = !string.IsNullOrWhiteSpace(integ.baseUrl) ? integ.baseUrl : b;

            if (string.IsNullOrWhiteSpace(baseUrl) ||
                string.IsNullOrWhiteSpace(u) ||
                string.IsNullOrWhiteSpace(p))
            {
                Console.WriteLine($"[OrderSyncWorker] Skipping company {integ.fk_Companyid_Company}: missing baseUrl/username/password.");
                continue;
            }

            await SyncCompanyOrders(integ.fk_Companyid_Company, baseUrl!, u!, p!, ct);
        }
    }

    private async Task SyncCompanyOrders(int companyId, string baseUrl, string username, string password, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var http = _httpClientFactory.CreateClient();
        http.BaseAddress = new Uri(baseUrl.Trim().TrimEnd('/') + "/");

        var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);

        var butent = new ButentApiService(http);

        var sales = await butent.GetSalesAsync("2000-10-01", ct);
        Console.WriteLine($"[OrderSyncWorker] Company={companyId} docs={sales.Count}");
        if (sales.Count == 0) return;

        var existing = (await db.orders
                .Where(o => o.fk_Companyid_Company == companyId)
                .Select(o => o.externalDocumentId)
                .ToListAsync(ct))
            .ToHashSet();

        var clientsByExternal = await db.client_companies
            .Where(cc => cc.fk_Companyid_Company == companyId && cc.externalClientId.HasValue)
            .ToDictionaryAsync(cc => cc.externalClientId!.Value, cc => cc.fk_Clientid_Users, ct);

        var productsByExternal = await db.products
            .Where(p => p.fk_Companyid_Company == companyId && p.externalCode.HasValue)
            .ToDictionaryAsync(p => p.externalCode!.Value, p => p.id_Product, ct);

        foreach (var bill in sales)
        {
            ct.ThrowIfCancellationRequested();

            var extDocId = bill.DocumentID;
            if (existing.Contains(extDocId)) continue;

            var doc = await butent.GetDocumentAsync(extDocId, ct);
            if (doc?.Client_Id == null) continue;

            if (!clientsByExternal.TryGetValue(doc.Client_Id.Value, out var clientUserId))
            {
                Console.WriteLine($"[OrderSyncWorker] Client missing for company {companyId}, external client={doc.Client_Id.Value}");
                continue;
            }

            // Look up the client's current profile address to snapshot at import time.
            // This makes Butent-imported orders consistent with manually created ones —
            // staff can create shipments for them without having to fill in the address manually.
            var clientCc = await db.client_companies
                .AsNoTracking()
                .Where(x => x.fk_Companyid_Company == companyId && x.fk_Clientid_Users == clientUserId)
                .Select(x => new { x.deliveryAddress, x.city, x.country })
                .FirstOrDefaultAsync(ct);

            var order = new order
            {
                fk_Companyid_Company = companyId,
                externalDocumentId   = extDocId,
                OrdersDate           = ParseButentDate(doc.Date)?.Date ?? DateTime.UtcNow.Date,
                totalAmount          = doc.Total ?? bill.Total ?? 0,
                paymentMethod        = "butent",
                deliveryPrice        = 0,
                status               = 4,
                fk_Clientid_Users    = clientUserId,

                // Snapshot the client's address at the time of import.
                // This is the starting point — staff or client can change it later
                // via the order's delivery endpoint without touching client_companies.
                snapshotDeliveryAddress = clientCc?.deliveryAddress,
                snapshotCity            = clientCc?.city,
                snapshotCountry         = clientCc?.country,
                snapshotDeliveryMethod  = "HOME",   // Butent orders default to home delivery
                // snapshotPhone will be null — Butent doesn't provide it;
                // the user's phoneNumber on the users table is used when creating labels
            };

            db.orders.Add(order);
            await db.SaveChangesAsync(ct);

            var items = await butent.GetDocumentItemsAsync(extDocId, ct);

            const double VatRate = 0.21;

            foreach (var it in items)
            {
                if (!productsByExternal.TryGetValue(it.Good_Id, out var productId))
                    continue;

                var unitPrice = it.Price ?? 0;
                var vatValue  = Math.Round(unitPrice * VatRate, 2);

                db.ordersproducts.Add(new ordersproduct
                {
                    fk_Ordersid_Orders   = order.id_Orders,
                    fk_Productid_Product = productId,
                    quantity             = it.Quantity,
                    unitPrice            = unitPrice,
                    vatValue             = vatValue
                });
            }

            await db.SaveChangesAsync(ct);

            Console.WriteLine($"[OrderSyncWorker] Company={companyId} inserted order ext={extDocId}");
        }
    }

    private static DateTime? ParseButentDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        return DateTime.TryParseExact(
            value.Trim(),
            "yyyy-MM-dd HH:mm:ss",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var dt)
            ? dt
            : null;
    }
}