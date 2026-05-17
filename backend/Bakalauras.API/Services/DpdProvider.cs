using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Bakalauras.API.Models;
using Microsoft.EntityFrameworkCore;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;

public class DpdProvider : ICourierProvider
{
    public string ProviderName => "DPD";
    public bool SupportsLockers => true;

    private readonly HttpClient _http;
    private readonly AppDbContext _db;
    private readonly int _companyId;

    public DpdProvider(HttpClient http, AppDbContext db, int companyId)
    {
        _http = http;
        _db = db;
        _companyId = companyId;
    }

    // Token management

    private async Task EnsureBearerAsync(CancellationToken ct)
    {
        var integ = await _db.company_integrations
            .FirstAsync(ci => ci.fk_Companyid_Company == _companyId && ci.type == "DPD", ct);

        if (!string.IsNullOrWhiteSpace(integ.dpdToken) &&
             integ.dpdTokenExpires.HasValue &&
             integ.dpdTokenExpires.Value > DateTime.UtcNow.AddMinutes(5))
        {
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", integ.dpdToken);
            return;
        }

        var (username, password, _) = IntegrationSecrets.TryUnpack(integ.encryptedSecrets);
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("DPD credentials are missing or corrupt.");

        var basicValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));

        using var tokenReq = new HttpRequestMessage(HttpMethod.Post, "auth/tokens");
        tokenReq.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicValue);
        tokenReq.Content = new StringContent(
            JsonSerializer.Serialize(new { name = "bakalauras-integration", ttl = 86400 }),
            Encoding.UTF8, "application/json");

        var tokenRes = await _http.SendAsync(tokenReq, ct);
        var tokenBody = await tokenRes.Content.ReadAsStringAsync(ct);
        tokenRes.EnsureSuccessStatusCode();

        var doc = JsonDocument.Parse(tokenBody).RootElement;
        var token = doc.GetProperty("token").GetString()!;

        integ.dpdToken = token;
        integ.dpdTokenSecretId = doc.TryGetProperty("secretId", out var sid) ? sid.GetString() : null;
        integ.dpdTokenExpires = DateTime.UtcNow.AddHours(23);
        await _db.SaveChangesAsync(ct);

        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    // GetLockers

    public async Task<List<CourierLocker>> GetLockersAsync(
        string countryCode = "LT", CancellationToken ct = default)
    {
        await EnsureBearerAsync(ct);

        using var req = new HttpRequestMessage(
            HttpMethod.Get, $"lockers?countryCode={countryCode}");
        req.Headers.Add("accept", "application/json+fulldata");

        var res = await _http.SendAsync(req, ct);
        var body = await res.Content.ReadAsStringAsync(ct);
        res.EnsureSuccessStatusCode();

        var lockers = new List<CourierLocker>();
        foreach (var item in JsonDocument.Parse(body).RootElement.EnumerateArray())
        {
            var addr = item.GetProperty("address");
            var latLng = Array.Empty<double>();

            if (addr.TryGetProperty("latLong", out var ll) &&
                ll.ValueKind == JsonValueKind.Array)
            {
                latLng = ll.EnumerateArray()
                    .Select(x => x.ValueKind == JsonValueKind.String
                        ? double.TryParse(x.GetString(),
                            System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : 0
                        : x.GetDouble())
                    .ToArray();
            }

            lockers.Add(new CourierLocker
            {
                Id = item.GetProperty("id").GetString()!,
                Name = GetStr(item, "name"),
                LockerType = GetStr(item, "lockerType"),
                Street = GetStr(addr, "street"),
                City = GetStr(addr, "city"),
                PostalCode = GetStr(addr, "postalCode"),
                Lat = latLng.Length >= 1 ? latLng[0] : 0,
                Lng = latLng.Length >= 2 ? latLng[1] : 0,
            });
        }
        return lockers;
    }

    // CreateShipment

    public async Task<CourierShipmentResult> CreateShipmentAsync(
        CourierShipmentRequest req, CancellationToken ct = default)
    {
        await EnsureBearerAsync(ct);

        // Build per-package weight list — index i = package i
        var weights = Enumerable.Range(0, req.PackageCount)
            .Select(i =>
            {
                double w = req.PackageWeights != null && i < req.PackageWeights.Count
                    ? req.PackageWeights[i]
                    : req.PackageWeightKg;
                return w > 0 ? w : 1.0;
            })
            .ToList();

        Console.WriteLine($"[DpdProvider] Sending {req.PackageCount} parcels, weights: [{string.Join(", ", weights)}]");

        object receiverAddress = req.LockerId != null
            ? (object)new
            {
                name = Trunc(req.RecipientName, 35),
                phone = NormalizePhone(req.RecipientPhone),
                email = req.RecipientEmail,
                pudoId = req.LockerId
            }
            : new
            {
                name = Trunc(req.RecipientName, 35),
                phone = NormalizePhone(req.RecipientPhone),
                email = req.RecipientEmail,
                street = Trunc(req.RecipientStreet, 35),
                city = Trunc(req.RecipientCity, 35),
                postalCode = req.RecipientPostalCode.Replace("-", "").Replace(" ", ""),
                country = req.RecipientCountry
            };

        // DPD: one shipment, N parcels each with individual weight.
        // Response: one shipmentLabels PDF with N pages (one per parcel).
        var payload = new[]
        {
            new
            {
                senderAddress = new
                {
                    name = Trunc(req.SenderName, 35),
                    phone  = NormalizePhone(req.SenderPhone),
                    street = Trunc(req.SenderStreet, 35),
                    city  = Trunc(req.SenderCity, 35),
                    postalCode = req.SenderPostalCode.Replace("-", "").Replace(" ", ""),
                    country = req.SenderCountry
                },
                receiverAddress,
                service = new { serviceAlias = req.LockerId != null ? "DPD PICKUP" : "B2C" },
                parcels = weights.Select(w => new { weight = w }).ToArray(),
                shipmentReferences = new[] { req.OrderReference },
                labelOptions = new
                {
                    shipmentIds = Array.Empty<string>(),
                    offsetPosition = 0,
                    downloadLabel  = true,
                    emailLabel = false,
                    labelFormat = "application/pdf",
                    paperSize = "A6"
                }
            }
        };

        var content = new StringContent(
            JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var res = await _http.PostAsync("shipments", content, ct);
        var body = await res.Content.ReadAsStringAsync(ct);

        if (!res.IsSuccessStatusCode)
            return new CourierShipmentResult
            { ErrorMessage = $"DPD error {(int)res.StatusCode}: {body}" };

        Console.WriteLine($"[DpdProvider] CreateShipment response (first 800): {body[..Math.Min(800, body.Length)]}");

        var first = JsonDocument.Parse(body).RootElement.EnumerateArray().First();
        var shipmentId = first.GetProperty("id").GetString()!;

        // parcelNumbers[i] is the tracking number for parcel i (printed on the label)
        var parcelNumbers = first.TryGetProperty("parcelNumbers", out var pn)
            ? pn.EnumerateArray().Select(x => x.GetString()!).ToList()
            : new List<string>();

        Console.WriteLine($"[DpdProvider] parcelNumbers: [{string.Join(", ", parcelNumbers)}]");

        // Extract the combined multi-page PDF
        byte[]? combinedPdf = TryExtractPdfFromJson(body);

        if (combinedPdf == null && first.TryGetProperty("shipmentLabels", out var sl))
        {
            JsonElement labelsEl = sl.ValueKind == JsonValueKind.Array
                ? sl.EnumerateArray().FirstOrDefault()
                : sl;

            if (labelsEl.ValueKind == JsonValueKind.Object &&
                labelsEl.TryGetProperty("pages", out var pages))
            {
                // DPD sometimes puts all parcels on one binaryData (multi-page PDF),
                // sometimes one binaryData per page element — try first page only first.
                foreach (var page in pages.EnumerateArray())
                {
                    if (!page.TryGetProperty("binaryData", out var bd)) continue;
                    var raw = bd.GetString();
                    if (string.IsNullOrWhiteSpace(raw)) continue;

                    var b64 = raw.Contains(',') ? raw[(raw.IndexOf(',') + 1)..] : raw;
                    b64 = b64.Replace("\r", "").Replace("\n", "").Replace(" ", "");

                    try
                    {
                        var pageBytes = Convert.FromBase64String(b64);
                        // Use first successfully decoded PDF as the combined PDF.
                        // If DPD puts each parcel in a separate page element we merge below.
                        if (combinedPdf == null)
                            combinedPdf = pageBytes;
                        else
                            combinedPdf = MergePdfs(combinedPdf, pageBytes);
                    }
                    catch (FormatException ex)
                    {
                        Console.WriteLine($"[DpdProvider] binaryData decode failed: {ex.Message}");
                    }
                }
            }
        }

        // Fallback: fetch the label PDF separately
        if (combinedPdf == null && !string.IsNullOrWhiteSpace(shipmentId))
        {
            Console.WriteLine($"[DpdProvider] No inline label for {shipmentId}, fetching separately…");
            try { combinedPdf = await FetchLabelAsync(shipmentId, ct); }
            catch (Exception ex) { Console.WriteLine($"[DpdProvider] Separate label fetch failed: {ex.Message}"); }
        }

        // Split the multi-page PDF into one PDF per parcel
        var perParcelLabels = new List<byte[]>();
        if (combinedPdf != null)
        {
            try
            {
                perParcelLabels = SplitPdf(combinedPdf);
                Console.WriteLine($"[DpdProvider] Split PDF into {perParcelLabels.Count} page(s).");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DpdProvider] PDF split failed ({ex.Message}), using combined PDF for all parcels.");
                // Fallback: give every parcel the same combined PDF
                for (int i = 0; i < req.PackageCount; i++)
                    perParcelLabels.Add(combinedPdf);
            }
        }

        return new CourierShipmentResult
        {
            ProviderShipmentId = shipmentId,
            ParcelNumbers = parcelNumbers,
            LabelPdfBytes = combinedPdf,
            PerParcelLabelBytes = perParcelLabels,
        };
    }

    // PDF helpers

    /// <summary>
    /// Splits a multi-page PDF into a list of single-page PDFs.
    /// Uses PdfSharpCore which is pure .NET and requires no native libs.
    /// </summary>
    private static List<byte[]> SplitPdf(byte[] pdfBytes)
    {
        var result = new List<byte[]>();

        using var inputStream = new MemoryStream(pdfBytes);
        using var inputDoc = PdfReader.Open(inputStream, PdfDocumentOpenMode.Import);

        for (int i = 0; i < inputDoc.PageCount; i++)
        {
            using var singlePageDoc = new PdfDocument();
            singlePageDoc.AddPage(inputDoc.Pages[i]);

            using var outStream = new MemoryStream();
            singlePageDoc.Save(outStream);
            result.Add(outStream.ToArray());
        }

        return result;
    }

    /// <summary>Merges two PDFs into one (used when DPD sends pages as separate binaryData entries).</summary>
    private static byte[] MergePdfs(byte[] first, byte[] second)
    {
        using var firstStream = new MemoryStream(first);
        using var secondStream = new MemoryStream(second);
        using var firstDoc = PdfReader.Open(firstStream, PdfDocumentOpenMode.Import);
        using var secondDoc = PdfReader.Open(secondStream, PdfDocumentOpenMode.Import);
        using var mergedDoc = new PdfDocument();

        foreach (var page in firstDoc.Pages) mergedDoc.AddPage(page);
        foreach (var page in secondDoc.Pages) mergedDoc.AddPage(page);

        using var outStream = new MemoryStream();
        mergedDoc.Save(outStream);
        return outStream.ToArray();
    }

    // FetchLabelAsync
    private async Task<byte[]?> FetchLabelAsync(string shipmentId, CancellationToken ct)
    {
        var reqBody = new
        {
            shipmentIds = new[] { shipmentId },
            parcelNumbers = Array.Empty<string>(),
            offsetPosition = 0,
            downloadLabel = true,
            emailLabel = false,
            labelFormat = "application/pdf",
            paperSize = "A6"
        };

        var content = new StringContent(
            JsonSerializer.Serialize(reqBody), Encoding.UTF8, "application/json");

        using var req = new HttpRequestMessage(HttpMethod.Post, "shipments/labels");
        req.Content = content;
        req.Headers.Add("Accept", "application/json");

        var res = await _http.SendAsync(req, ct);
        var bytes = await res.Content.ReadAsByteArrayAsync(ct);

        if (!res.IsSuccessStatusCode)
        {
            Console.WriteLine($"[DpdProvider] FetchLabel {shipmentId} failed {(int)res.StatusCode}");
            return null;
        }

        var contentType = res.Content.Headers.ContentType?.MediaType ?? "";
        if (contentType.Contains("pdf") || LooksLikePdf(bytes)) return bytes;

        try
        {
            var json = Encoding.UTF8.GetString(bytes);
            var extractedPdf = TryExtractPdfFromJson(json);
            if (extractedPdf != null) return extractedPdf;

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            JsonElement pages = default;
            if (root.TryGetProperty("pages", out pages) ||
                (root.ValueKind == JsonValueKind.Array &&
                 root.EnumerateArray().FirstOrDefault() is { ValueKind: JsonValueKind.Object } first2 &&
                 first2.TryGetProperty("pages", out pages)))
            {
                foreach (var page in pages.EnumerateArray())
                {
                    if (!page.TryGetProperty("binaryData", out var bd)) continue;
                    var raw = bd.GetString();
                    if (string.IsNullOrWhiteSpace(raw)) continue;
                    var b64 = raw.Contains(',') ? raw[(raw.IndexOf(',') + 1)..] : raw;
                    b64 = b64.Replace("\r", "").Replace("\n", "").Replace(" ", "");
                    return Convert.FromBase64String(b64);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DpdProvider] FetchLabel JSON parse failed: {ex.Message}");
        }

        return null;
    }

    // GetTracking

    private static byte[]? TryExtractPdfFromJson(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return TryExtractPdf(doc.RootElement);
        }
        catch
        {
            return null;
        }
    }

    private static byte[]? TryExtractPdf(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var fromChild = TryExtractPdf(property.Value);
                    if (fromChild != null) return fromChild;
                }
                break;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    var fromChild = TryExtractPdf(item);
                    if (fromChild != null) return fromChild;
                }
                break;

            case JsonValueKind.String:
                return TryDecodePdf(element.GetString());
        }

        return null;
    }

    private static byte[]? TryDecodePdf(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        var b64 = raw.Contains(',') ? raw[(raw.IndexOf(',') + 1)..] : raw;
        b64 = b64.Replace("\r", "").Replace("\n", "").Replace(" ", "");

        if (b64.Length < 8 || b64.Length % 4 != 0) return null;

        try
        {
            var bytes = Convert.FromBase64String(b64);
            return LooksLikePdf(bytes) ? bytes : null;
        }
        catch
        {
            return null;
        }
    }

    private static bool LooksLikePdf(byte[] bytes)
        => bytes.Length >= 5
        && bytes[0] == (byte)'%'
        && bytes[1] == (byte)'P'
        && bytes[2] == (byte)'D'
        && bytes[3] == (byte)'F'
        && bytes[4] == (byte)'-';

    public async Task<List<CourierTrackingEvent>> GetTrackingAsync(
        string parcelNumber, CancellationToken ct = default)
    {
        await EnsureBearerAsync(ct);

        var res = await _http.GetAsync(
            $"status/tracking?pknr={parcelNumber}&detail=0&show_all=1&lang=lt", ct);
        var body = await res.Content.ReadAsStringAsync(ct);
        res.EnsureSuccessStatusCode();

        var events = new List<CourierTrackingEvent>();
        foreach (var item in JsonDocument.Parse(body).RootElement.EnumerateArray())
        {
            if (!item.TryGetProperty("details", out var details)) continue;
            foreach (var ev in details.EnumerateArray())
                events.Add(new CourierTrackingEvent
                {
                    Status = ev.TryGetProperty("status", out var s) ? s.GetString() ?? "" : "",
                    DateTime = ev.TryGetProperty("dateTime", out var d) ? d.GetString() ?? "" : "",
                });
        }
        return events;
    }

    // Helpers

    private static string GetStr(JsonElement el, string prop)
        => el.TryGetProperty(prop, out var v) ? v.GetString() ?? "" : "";

    private static string Trunc(string? s, int max)
        => string.IsNullOrWhiteSpace(s) ? ""
           : s.Length <= max ? s : s[..max];

    private static string NormalizePhone(string? p)
        => string.IsNullOrWhiteSpace(p) ? "+37000000000"
           : p.StartsWith("+") ? p : $"+370{p.TrimStart('0')}";
}
