using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace CAPETF.Desktop;

public sealed class CapitalApiClient : IDisposable
{
    private readonly HttpClient _http = new();
    private ApiCredentials? _credentials;
    private CapitalSession? _session;

    public CapitalSession? Session => _session;

    public async Task<CapitalSession> LoginAsync(ApiCredentials credentials, CancellationToken cancellationToken = default)
    {
        _credentials = credentials;
        _http.BaseAddress = new Uri(credentials.UseDemo
            ? "https://demo-api-capital.backend-capital.com/"
            : "https://api-capital.backend-capital.com/");

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/session");
        request.Headers.Add("X-CAP-API-KEY", credentials.ApiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(new { identifier = credentials.Identifier, password = credentials.Password, encryptedPassword = false }),
            Encoding.UTF8,
            "application/json");

        using var response = await _http.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Capital.com login failed: {(int)response.StatusCode} {response.ReasonPhrase}. {body}");
        }

        _session = new CapitalSession
        {
            Cst = response.Headers.TryGetValues("CST", out var cst) ? cst.FirstOrDefault() ?? "" : "",
            SecurityToken = response.Headers.TryGetValues("X-SECURITY-TOKEN", out var security) ? security.FirstOrDefault() ?? "" : "",
            UseDemo = credentials.UseDemo,
        };

        if (string.IsNullOrWhiteSpace(_session.Cst) || string.IsNullOrWhiteSpace(_session.SecurityToken))
        {
            throw new InvalidOperationException($"Login succeeded but tokens were missing. Response: {body}");
        }

        return _session;
    }

    public async Task<IReadOnlyList<MarketInstrument>> SearchMarketsAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        EnsureSession();
        var uri = string.IsNullOrWhiteSpace(searchTerm)
            ? "api/v1/markets"
            : $"api/v1/markets?searchTerm={Uri.EscapeDataString(searchTerm)}";
        using var doc = await GetJsonAsync(uri, cancellationToken);
        return ExtractMarkets(doc.RootElement).ToList();
    }

    public async Task<IReadOnlyList<ChartPoint>> GetPricesAsync(string epic, string resolution, int max, CancellationToken cancellationToken = default)
    {
        EnsureSession();
        using var doc = await GetJsonAsync($"api/v1/prices/{Uri.EscapeDataString(epic)}?resolution={resolution}&max={max}", cancellationToken);
        if (!doc.RootElement.TryGetProperty("prices", out var prices) || prices.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var points = new List<ChartPoint>();
        foreach (var row in prices.EnumerateArray())
        {
            var time = ReadString(row, "snapshotTimeUTC") ?? ReadString(row, "snapshotTime") ?? ReadString(row, "time");
            var close = ReadPrice(row, "closePrice") ?? ReadPrice(row, "lastTradedPrice");
            if (time is null || close is null) continue;
            if (DateTimeOffset.TryParse(time, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
            {
                points.Add(new ChartPoint(parsed, close.Value));
            }
        }

        return points.OrderBy(point => point.Time).ToList();
    }

    private async Task<JsonDocument> GetJsonAsync(string uri, CancellationToken cancellationToken)
    {
        EnsureSession();
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Add("CST", _session!.Cst);
        request.Headers.Add("X-SECURITY-TOKEN", _session.SecurityToken);
        request.Headers.Add("X-CAP-API-KEY", _credentials!.ApiKey);

        using var response = await _http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Capital.com request failed: {(int)response.StatusCode} {response.ReasonPhrase}. {body}");
        }
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
    }

    private void EnsureSession()
    {
        if (_session is null || _credentials is null) throw new InvalidOperationException("Connect to Capital.com first.");
    }

    private static IEnumerable<MarketInstrument> ExtractMarkets(JsonElement root)
    {
        if (!root.TryGetProperty("markets", out var markets) || markets.ValueKind != JsonValueKind.Array) yield break;
        foreach (var market in markets.EnumerateArray())
        {
            yield return new MarketInstrument
            {
                Epic = ReadString(market, "epic") ?? "",
                Name = ReadString(market, "instrumentName") ?? ReadString(market, "name") ?? "",
                Symbol = ReadString(market, "symbol") ?? "",
                Type = ReadString(market, "instrumentType") ?? ReadString(market, "type") ?? "",
                Currency = ReadString(market, "currency") ?? ReadString(market, "currencyCode") ?? "",
                Country = ReadString(market, "country") ?? ReadString(market, "countryName") ?? "",
                Sector = ReadString(market, "sector") ?? ReadString(market, "industry") ?? "",
                Region = ReadString(market, "region") ?? "",
                Status = ReadString(market, "marketStatus") ?? ReadString(market, "status") ?? "",
            };
        }
    }

    private static string? ReadString(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }

    private static decimal? ReadPrice(JsonElement row, string name)
    {
        if (!row.TryGetProperty(name, out var value)) return null;
        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var direct)) return direct;
        if (value.ValueKind != JsonValueKind.Object) return null;
        foreach (var key in new[] { "bid", "ask", "lastTraded", "mid" })
        {
            if (value.TryGetProperty(key, out var part) && part.ValueKind == JsonValueKind.Number && part.TryGetDecimal(out var price))
            {
                return price;
            }
        }
        return null;
    }

    public void Dispose() => _http.Dispose();
}
