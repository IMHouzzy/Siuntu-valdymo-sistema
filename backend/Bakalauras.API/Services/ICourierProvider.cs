public interface ICourierProvider
{
    string ProviderName { get; }

    /// <summary>
    /// True if this provider has a pickup-point / locker network that the user must choose from.
    /// False = home delivery only (no locker picker shown in UI).
    /// </summary>
    bool SupportsLockers { get; }

    /// <summary>
    /// Returns all pickup points / lockers for the given country.
    /// Returns empty list when SupportsLockers is false.
    /// </summary>
    Task<List<CourierLocker>> GetLockersAsync(
        string countryCode = "LT", CancellationToken ct = default);

    /// <summary>
    /// Registers a shipment with the provider and returns tracking number + label PDF bytes.
    /// LabelPdfBytes is null if the provider does not supply a label (fall back to QuestPDF).
    /// ErrorMessage is non-null on failure.
    /// </summary>
    Task<CourierShipmentResult> CreateShipmentAsync(
        CourierShipmentRequest request, CancellationToken ct = default);

    /// <summary>
    /// Returns tracking events for the given provider parcel number.
    /// </summary>
    Task<List<CourierTrackingEvent>> GetTrackingAsync(
        string parcelNumber, CancellationToken ct = default);
}

// Shared request / response DTOs used by ALL providers

public class CourierLocker
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string LockerType { get; set; } = "";
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public double Lat { get; set; }
    public double Lng { get; set; }
}

public class CourierShipmentRequest
{
    // Sender
    public string SenderName { get; set; } = "";
    public string SenderPhone { get; set; } = "";
    public string SenderStreet { get; set; } = "";
    public string SenderCity { get; set; } = "";
    public string SenderPostalCode { get; set; } = "";
    public string SenderCountry { get; set; } = "LT";

    // Recipient
    public string RecipientName { get; set; } = "";
    public string RecipientEmail { get; set; } = "";
    public string RecipientPhone { get; set; } = "";
    public string RecipientStreet { get; set; } = "";
    public string RecipientCity { get; set; } = "";
    public string RecipientPostalCode { get; set; } = "";
    public string RecipientCountry { get; set; } = "LT";

    public string? LockerId { get; set; }

    public int PackageCount { get; set; } = 1;
    public double PackageWeightKg { get; set; } = 1.0;   // fallback / first package

    /// <summary>Per-package weights. Length must equal PackageCount when provided.</summary>
    public List<double>? PackageWeights { get; set; }

    public string OrderReference { get; set; } = "";
}
public class CourierShipmentResult
{
    /// <summary>DPD internal shipment UUID (not a tracking number).</summary>
    public string ProviderShipmentId { get; set; } = "";

    /// <summary>
    /// DPD parcel numbers — one per physical parcel.
    /// These ARE the tracking numbers printed on labels and used with the tracking API.
    /// </summary>
    public List<string> ParcelNumbers { get; set; } = new();

    /// <summary>Combined first-label bytes (backwards compat). Prefer PerParcelLabelBytes.</summary>
    public byte[]? LabelPdfBytes { get; set; }

    /// <summary>
    /// Per-parcel label PDFs split from the single multi-page PDF DPD returns.
    /// Index matches ParcelNumbers index.
    /// </summary>
    public List<byte[]> PerParcelLabelBytes { get; set; } = new();

    public string? ErrorMessage { get; set; }
}

public class CourierTrackingEvent
{
    public string Status { get; set; } = "";
    public string DateTime { get; set; } = "";
}