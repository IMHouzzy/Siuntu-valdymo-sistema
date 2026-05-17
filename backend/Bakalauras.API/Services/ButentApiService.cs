using System.Text.Json;
using Bakalauras.API.Dtos;
public class ButentApiService
{
    private readonly HttpClient _http;

    public ButentApiService(HttpClient http)
    {
        _http = http;
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // Small helper to reduce repetition and keep errors readable
    private async Task<T?> GetJsonAsync<T>(string url, CancellationToken ct)
    {
        using var res = await _http.GetAsync(url, ct);

        // If Butent returns a body on error, include it for debugging
        if (!res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"Butent API error {(int)res.StatusCode} {res.ReasonPhrase}. Url='{url}'. Body='{body}'"
            );
        }

        var json = await res.Content.ReadAsStringAsync(ct);
        if (string.IsNullOrWhiteSpace(json)) return default;

        return JsonSerializer.Deserialize<T>(json, JsonOpts);
    }

    public async Task<List<ButentClientDto>> GetClientsAsync(CancellationToken ct = default)
    {
        var wrapper = await GetJsonAsync<ButentClientResponse>("client", ct);
        return wrapper?.Clients ?? new List<ButentClientDto>();
    }

    public async Task<List<ButentProductDto>> GetProductsAsync(CancellationToken ct = default)
    {
        // endpoint is "good"
        var wrapper = await GetJsonAsync<ButentGoodsResponse>("good", ct);
        return wrapper?.Goods ?? new List<ButentProductDto>();
    }

    public async Task<List<ButentSaleDocDto>> GetSalesAsync(string dateFrom, CancellationToken ct = default)
    {
        var url = $"trade/sale?dateFrom={Uri.EscapeDataString(dateFrom)}";
        var wrapper = await GetJsonAsync<ButentSalesResponse>(url, ct);
        return wrapper?.Documents ?? new List<ButentSaleDocDto>();
    }

    public async Task<ButentDocumentDto?> GetDocumentAsync(int id, CancellationToken ct = default)
    {
        var wrapper = await GetJsonAsync<ButentDocumentResponse>($"document?id={id}", ct);
        return wrapper?.Documents?.FirstOrDefault();
    }

    public async Task<List<ButentItemDto>> GetDocumentItemsAsync(int id, CancellationToken ct = default)
    {
        var wrapper = await GetJsonAsync<ButentItemsResponse>($"document/{id}/item", ct);
        return wrapper?.Items ?? new List<ButentItemDto>();
    }
}