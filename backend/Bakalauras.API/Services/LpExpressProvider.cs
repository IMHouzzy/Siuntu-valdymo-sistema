using Bakalauras.API.Models;
public class LpExpressProvider : ICourierProvider
{
    public string ProviderName    => "LP Express";
    public bool   SupportsLockers => true;

    private readonly HttpClient  _http;
    private readonly AppDbContext _db;
    private readonly int          _companyId;

    public LpExpressProvider(HttpClient http, AppDbContext db, int companyId)
    {
        _http      = http;
        _db        = db;
        _companyId = companyId;
    }

    public Task<List<CourierLocker>> GetLockersAsync(
        string countryCode = "LT", CancellationToken ct = default)
        => throw new NotImplementedException(
            "LP Express locker fetch not yet implemented. Add when API credentials are available.");

    public Task<CourierShipmentResult> CreateShipmentAsync(
        CourierShipmentRequest request, CancellationToken ct = default)
        => throw new NotImplementedException(
            "LP Express shipment creation not yet implemented.");

    public Task<List<CourierTrackingEvent>> GetTrackingAsync(
        string parcelNumber, CancellationToken ct = default)
        => throw new NotImplementedException(
            "LP Express tracking not yet implemented.");
}