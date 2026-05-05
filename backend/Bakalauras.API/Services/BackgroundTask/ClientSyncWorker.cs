using System.Net.Http.Headers;
using System.Text;
using Bakalauras.API.Models;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Fixed sync worker: instead of fabricating per-company unique emails like
/// "butent-c1-client5@local", we now try to match on the client's real VAT/name,
/// fall back to a stable synthetic email keyed only on the external client ID
/// (not the company), so one real person stays one user row across companies.
/// </summary>
public class ClientSyncWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;

    public ClientSyncWorker(IServiceScopeFactory scopeFactory, IHttpClientFactory httpClientFactory)
    {
        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try { await SyncAllCompanies(stoppingToken); }
        catch (Exception ex) { Console.WriteLine($"[ClientSyncWorker] Startup sync error: {ex}"); }

        while (!stoppingToken.IsCancellationRequested)
        {
            try { await SyncAllCompanies(stoppingToken); }
            catch (Exception ex) { Console.WriteLine($"[ClientSyncWorker] Error syncing clients: {ex}"); }

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

        if (integrations.Count == 0)
        {
            Console.WriteLine("[ClientSyncWorker] No enabled BUTENT integrations found.");
            return;
        }

        foreach (var integ in integrations)
        {
            ct.ThrowIfCancellationRequested();

            var (u, p, b) = IntegrationSecrets.TryUnpack(integ.encryptedSecrets);
            var baseUrl = !string.IsNullOrWhiteSpace(integ.baseUrl) ? integ.baseUrl : b;

            if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(u) || string.IsNullOrWhiteSpace(p))
            {
                Console.WriteLine($"[ClientSyncWorker] Skipping company {integ.fk_Companyid_Company}: missing credentials.");
                continue;
            }

            await SyncCompanyClients(integ.fk_Companyid_Company, baseUrl!, u!, p!, ct);
        }
    }

    private async Task SyncCompanyClients(int companyId, string baseUrl, string username, string password, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var http = _httpClientFactory.CreateClient();
        http.BaseAddress = new Uri(baseUrl.Trim().TrimEnd('/') + "/");

        var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);

        var butent = new ButentApiService(http);

        var externalClients = await butent.GetClientsAsync(ct);
        Console.WriteLine($"[ClientSyncWorker] Company={companyId} external clients: {externalClients.Count}");
        if (externalClients.Count == 0) return;

        // Which externalClientIds already have a client_company row for this company?
        var existingExternalIds = await db.client_companies
            .AsNoTracking()
            .Where(cc => cc.fk_Companyid_Company == companyId && cc.externalClientId.HasValue)
            .Select(cc => cc.externalClientId!.Value)
            .ToListAsync(ct);

        var existingSet = existingExternalIds.ToHashSet();

        var newClients = externalClients.Where(ext => !existingSet.Contains(ext.ClientID)).ToList();
        Console.WriteLine($"[ClientSyncWorker] Company={companyId} new clients to add: {newClients.Count}");
        if (newClients.Count == 0) return;

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        try
        {
            foreach (var ext in newClients)
            {
                ct.ThrowIfCancellationRequested();

                // ── 1. Find or create the user ────────────────────────────────
                //
                // Strategy (in order):
                //   a) If the external client has a VAT number, look for an
                //      existing user whose vat matches across ANY company
                //      (same legal entity = same person).
                //   b) Otherwise fall back to a stable synthetic email keyed
                //      ONLY on the external client ID (not company-scoped),
                //      so the same vendor appearing in two companies maps to
                //      the same user row.
                //
                user? existingUser = null;

                if (!string.IsNullOrWhiteSpace(ext.Vat))
                {
                    // Try to find an existing client_company row with this VAT
                    var matchByVat = await db.client_companies
                        .AsNoTracking()
                        .Where(cc => cc.vat == ext.Vat)
                        .Select(cc => cc.fk_Clientid_Users)
                        .FirstOrDefaultAsync(ct);

                    if (matchByVat != 0)
                        existingUser = await db.users.FirstOrDefaultAsync(u => u.id_Users == matchByVat, ct);
                }

                if (existingUser == null)
                {
                    // Stable synthetic email — NOT company-scoped
                    var syntheticEmail = BuildSyntheticEmail(ext.ClientID);

                    existingUser = await db.users.FirstOrDefaultAsync(u => u.email == syntheticEmail, ct);

                    if (existingUser == null)
                    {
                        existingUser = new user
                        {
                            email                = syntheticEmail,
                            password             = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
                            name                 = ext.Name ?? $"Client {ext.ClientID}",
                            surname              = "Klientas",
                            authProvider         = "LOCAL",
                            creationDate         = DateTime.Now,
                            fk_Companyid_Company = null,
                            isMasterAdmin        = false
                        };

                        db.users.Add(existingUser);
                        await db.SaveChangesAsync(ct); // get id_Users assigned
                    }
                }

                // ── 2. Add company_users CLIENT link if missing ───────────────
                var cuExists = await db.company_users.AnyAsync(cu =>
                    cu.fk_Companyid_Company == companyId &&
                    cu.fk_Usersid_Users     == existingUser.id_Users, ct);

                if (!cuExists)
                {
                    db.company_users.Add(new company_user
                    {
                        fk_Companyid_Company = companyId,
                        fk_Usersid_Users     = existingUser.id_Users,
                        role                 = "CLIENT",
                        active               = true
                    });
                }

                // ── 3. Add client_company row (skip if already exists via race) ──
                // Check both by PK (user+company) AND externalClientId to avoid
                // duplicate tracking when two external clients map to the same user
                var existingCc = await db.client_companies
                    .FirstOrDefaultAsync(cc =>
                        cc.fk_Companyid_Company == companyId &&
                        (cc.externalClientId == ext.ClientID || cc.fk_Clientid_Users == existingUser.id_Users), ct);

                if (existingCc == null)
                {
                    db.client_companies.Add(new client_company
                    {
                        fk_Clientid_Users    = existingUser.id_Users,
                        fk_Companyid_Company = companyId,
                        externalClientId     = ext.ClientID,
                        deliveryAddress      = ext.Address ?? string.Empty,
                        city                 = ext.City    ?? string.Empty,
                        country              = ext.Country ?? string.Empty,
                        vat                  = ext.Vat,
                        bankCode             = int.TryParse(ext.BankCode, out var bc) ? bc : null
                    });
                }
                else if (existingCc.externalClientId != ext.ClientID)
                {
                    // User already linked — just update the externalClientId if missing
                    existingCc.externalClientId = ext.ClientID;
                }

                await db.SaveChangesAsync(ct);
            }

            await tx.CommitAsync(ct);
            Console.WriteLine($"[ClientSyncWorker] Company={companyId} sync complete: {newClients.Count} processed.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            Console.WriteLine($"[ClientSyncWorker] Company={companyId} sync failed: {ex}");
        }
    }

    /// <summary>
    /// Stable synthetic email keyed on the external client ID only —
    /// NOT company-scoped, so the same external entity maps to the same user.
    /// </summary>
    private static string BuildSyntheticEmail(int clientId)
        => $"butent-client{clientId}@sync.local";
}