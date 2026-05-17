using Bakalauras.API.Models;
using Microsoft.EntityFrameworkCore;

public class CourierProviderFactory
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly AppDbContext _db;

    // Default base URLs per integration key.
    // The stored baseUrl in company_integration overrides these when present.
    private static readonly Dictionary<string, string> DefaultBaseUrls = new()
    {
        ["DPD"] = "https://sandbox-esiunta.dpd.lt/api/v1/",
        ["LP_EXPRESS"] = "https://api.lpexpress.lt/v2/",   // update when you have real URL
        ["OMNIVA"] = "https://api.omniva.eu/v1/",      // update when you have real URL
    };

    public CourierProviderFactory(IHttpClientFactory httpFactory, AppDbContext db)
    {
        _httpFactory = httpFactory;
        _db = db;
    }

    // Public API

    /// <summary>
    /// Resolves the correct ICourierProvider for the given courier type and company.
    /// Throws InvalidOperationException if the company does not have an enabled integration
    /// for this provider — that message is safe to return as a 400/403 to the client.
    /// Throws NotSupportedException if the courier type string is unknown.
    /// </summary>
    public async Task<ICourierProvider> GetProviderAsync(
        int companyId, string courierType, CancellationToken ct = default)
    {
        var integrationKey = GetIntegrationKey(courierType)
            ?? throw new NotSupportedException(
                $"No courier provider is registered for type '{courierType}'.");

        // Verify THIS company has the integration enabled
        var integ = await _db.company_integrations
            .FirstOrDefaultAsync(ci =>
                ci.fk_Companyid_Company == companyId &&
                ci.type == integrationKey &&
                ci.enabled == true, ct)
            ?? throw new InvalidOperationException(
                $"This company does not have an active {integrationKey} integration.");

        var (_, _, storedBaseUrl) = IntegrationSecrets.TryUnpack(integ.encryptedSecrets);
        var baseUrl = !string.IsNullOrWhiteSpace(storedBaseUrl)
            ? storedBaseUrl
            : DefaultBaseUrls.GetValueOrDefault(integrationKey, "");

        var http = _httpFactory.CreateClient();
        http.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");

        return integrationKey switch
        {
            "DPD" => new DpdProvider(http, _db, companyId),
            "LP_EXPRESS" => new LpExpressProvider(http, _db, companyId),
            _ => throw new NotSupportedException(
                                $"Provider '{integrationKey}' is not yet implemented.")
        };
    }

    /// <summary>
    /// Returns the set of integration type keys that this company currently has enabled.
    /// E.g. { "BUTENT", "DPD" }.  Used by CouriersController to decide which couriers to show.
    /// </summary>
    public async Task<HashSet<string>> GetEnabledIntegrationKeysAsync(
        int companyId, CancellationToken ct = default)
    {
        var keys = await _db.company_integrations
            .AsNoTracking()
            .Where(ci => ci.fk_Companyid_Company == companyId && ci.enabled == true)
            .Select(ci => ci.type)
            .ToListAsync(ct);

        return keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    // ── Static helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// The complete set of every non-CUSTOM courier type string known to the system.
    /// Used by controllers to build a plain HashSet BEFORE an EF query, so EF can
    /// translate .Contains(c.type) to SQL IN(...) without calling GetIntegrationKey inside SQL.
    /// Add a new entry here whenever you add entries to GetIntegrationKey below.
    /// </summary>
    public static readonly HashSet<string> AllProviderCourierTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "DPD_PARCEL",        "DPD_HOME",
            "LP_EXPRESS_PARCEL", "LP_EXPRESS_HOME",
            "OMNIVA_PARCEL",     "OMNIVA_HOME",
        };

    /// <summary>
    /// Maps a courier.type DB value to its integration key.
    /// Returns null for CUSTOM couriers (no external integration needed).
    /// Add new entries here AND in AllProviderCourierTypes above.
    /// </summary>
    public static string? GetIntegrationKey(string? courierType) => courierType switch
    {
        "DPD_PARCEL" => "DPD",
        "DPD_HOME" => "DPD",
        "LP_EXPRESS_PARCEL" => "LP_EXPRESS",
        "LP_EXPRESS_HOME" => "LP_EXPRESS",
        "OMNIVA_PARCEL" => "OMNIVA",
        "OMNIVA_HOME" => "OMNIVA",
        _ => null   // CUSTOM or null = no provider
    };

    /// <summary>
    /// Returns true if the courier type requires a locker/pickup-point selection.
    /// Convention: type names ending in _PARCEL always need a locker picker.
    /// </summary>
    public static bool RequiresLockerPicker(string? courierType)
        => courierType?.EndsWith("_PARCEL", StringComparison.OrdinalIgnoreCase) == true;
}