using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using Bakalauras.API.Models;
using Microsoft.EntityFrameworkCore;

public class ProductSyncWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;

    public ProductSyncWorker(IServiceScopeFactory scopeFactory, IHttpClientFactory httpClientFactory)
    {
        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try { await SyncProductsForAllCompanies(stoppingToken); }
        catch (Exception ex) { Console.WriteLine($"[ProductSyncWorker] Startup sync error: {ex}"); }

        while (!stoppingToken.IsCancellationRequested)
        {
            try { await SyncProductsForAllCompanies(stoppingToken); }
            catch (Exception ex) { Console.WriteLine($"[ProductSyncWorker] Error: {ex}"); }

            await Task.Delay(TimeSpan.FromMinutes(60), stoppingToken);
        }
    }

    private static DateTime? ParseButentTime(string? value)
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

    private async Task SyncProductsForAllCompanies(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var integrations = await db.company_integrations
            .AsNoTracking()
            .Where(ci => ci.enabled == true && ci.type == "BUTENT")
            .Select(ci => new { ci.fk_Companyid_Company, ci.baseUrl, ci.encryptedSecrets })
            .ToListAsync(ct);

        if (integrations.Count == 0)
        {
            Console.WriteLine("[ProductSyncWorker] No enabled BUTENT integrations found.");
            return;
        }

        var categoriesById = await db.categories.AsNoTracking()
            .ToDictionaryAsync(c => c.id_Category, ct);

        var groupsById = await db.productgroups.AsNoTracking()
            .ToDictionaryAsync(g => g.id_ProductGroup, ct);

        foreach (var integ in integrations)
        {
            ct.ThrowIfCancellationRequested();

            var (u, p, b) = IntegrationSecrets.TryUnpack(integ.encryptedSecrets);
            var baseUrl = !string.IsNullOrWhiteSpace(integ.baseUrl) ? integ.baseUrl : b;

            if (string.IsNullOrWhiteSpace(baseUrl) ||
                string.IsNullOrWhiteSpace(u) ||
                string.IsNullOrWhiteSpace(p))
            {
                Console.WriteLine($"[ProductSyncWorker] Skipping company {integ.fk_Companyid_Company}: missing baseUrl/username/password.");
                continue;
            }

            try
            {
                await SyncProductsForCompany(
                    db,
                    integ.fk_Companyid_Company,
                    baseUrl!,
                    u!,
                    p!,
                    categoriesById,
                    groupsById,
                    ct);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ProductSyncWorker] Company {integ.fk_Companyid_Company} sync failed: {ex.Message}");
            }
        }
    }

    private async Task SyncProductsForCompany(
        AppDbContext db,
        int companyId,
        string baseUrl,
        string username,
        string password,
        Dictionary<int, category> categoriesById,
        Dictionary<int, productgroup> groupsById,
        CancellationToken ct)
    {
        var http = _httpClientFactory.CreateClient();
        http.BaseAddress = new Uri(baseUrl.Trim().TrimEnd('/') + "/");

        var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);

        var butentApi = new ButentApiService(http);

        var externalProducts = await butentApi.GetProductsAsync(ct);
        Console.WriteLine($"[ProductSyncWorker] Company {companyId} API goods: {externalProducts.Count}");
        if (externalProducts.Count == 0) return;

        var existingExternalCodes = (await db.products.AsNoTracking()
                .Where(p => p.fk_Companyid_Company == companyId)
                .Select(p => p.externalCode)
                .ToListAsync(ct))
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .ToHashSet();

        var toInsert = new List<product>();

        foreach (var ext in externalProducts)
        {
            if (existingExternalCodes.Contains(ext.Code))
                continue;

            var typeName = ext.Type?.Name?.Trim();
            var canReturn = !string.Equals(typeName, "Paslaugos", StringComparison.OrdinalIgnoreCase);

            var p = new product
            {
                fk_Companyid_Company = companyId,
                externalCode = ext.Code,
                name = string.IsNullOrWhiteSpace(ext.Name) ? $"Prekė {ext.Code}" : ext.Name.Trim(),

                description = null,
                price = GeneratePrice(ext.Code),
                currency = "EUR",

                canTheProductBeProductReturned = canReturn,
                countableItem = ext.CountableItem,
                unit = string.IsNullOrWhiteSpace(ext.Unit) ? "vnt" : ext.Unit.Trim(),
                shipping_mode = string.IsNullOrWhiteSpace(ext.ShippingMode) ? null : ext.ShippingMode.Trim(),
                vat = ext.Vat,
                creationDate = ParseButentTime(ext.InpTime)
            };

            var catId = ext.Type?.Id;
            if (catId.HasValue && categoriesById.TryGetValue(catId.Value, out var catEntity))
            {
                db.Attach(catEntity);
                p.fk_Categoryid_Categories.Add(catEntity);
            }

            var grpId = ext.Group?.Id;
            if (grpId.HasValue && groupsById.TryGetValue(grpId.Value, out var grpEntity))
            {
                db.Attach(grpEntity);
                p.fk_ProductGroupId_ProductGroups.Add(grpEntity);
            }

            toInsert.Add(p);
        }

        if (toInsert.Count == 0)
        {
            Console.WriteLine($"[ProductSyncWorker] Company {companyId}: nothing new to insert.");
            return;
        }

        db.products.AddRange(toInsert);

        try
        {
            await db.SaveChangesAsync(ct);
            Console.WriteLine($"[ProductSyncWorker] Company {companyId}: inserted {toInsert.Count} new products.");
        }
        catch (DbUpdateException ex)
        {
            Console.WriteLine($"[ProductSyncWorker] Company {companyId}: DB update failed: {ex.InnerException?.Message ?? ex.Message}");
            throw;
        }
    }
    private static double GeneratePrice(int code)
    {
        // pvz: deterministic (tas pats code -> ta pati kaina)
        var baseVal = (code % 90) + 10;      // 10..99
        return Math.Round(baseVal + 0.99, 2);
    }
}