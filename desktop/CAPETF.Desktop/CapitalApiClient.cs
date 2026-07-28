using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace CAPETF.Desktop;

internal sealed class CapitalApiException(HttpStatusCode statusCode, string reasonPhrase, string responseBody)
    : InvalidOperationException($"Capital.com request failed: {(int)statusCode} {reasonPhrase}. {responseBody}")
{
    public HttpStatusCode StatusCode { get; } = statusCode;
    public string ResponseBody { get; } = responseBody;

    public string? ErrorCode
    {
        get
        {
            try
            {
                using var document = JsonDocument.Parse(ResponseBody);
                return document.RootElement.TryGetProperty("errorCode", out var value) ? value.GetString() : null;
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }

    public bool IsHistoryUnavailable =>
        StatusCode == HttpStatusCode.NotFound ||
        StatusCode == HttpStatusCode.BadRequest && ErrorCode is
            "error.prices.not-found" or
            "error.market.not-found" or
            "error.instrument.not-found" or
            "error.invalid.epic" or
            "error.no.prices";

    public bool IsHistoryBoundary
    {
        get
        {
            if (StatusCode != HttpStatusCode.BadRequest) return false;
            return ErrorCode is
                "error.invalid.from" or
                "error.invalid.to" or
                "error.invalid.date-range" or
                "error.invalid.daterange";
        }
    }
}

public sealed class CapitalApiClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly bool _disposeHttp;
    private Uri? _baseUri;
    private ApiCredentials? _credentials;
    private CapitalSession? _session;

    public CapitalSession? Session => _session;

    public CapitalApiClient()
    {
        _http = new HttpClient();
        _disposeHttp = true;
    }

    internal CapitalApiClient(HttpMessageHandler handler)
    {
        _http = new HttpClient(handler);
        _disposeHttp = true;
    }

    public async Task<CapitalSession> LoginAsync(ApiCredentials credentials, CancellationToken cancellationToken = default)
    {
        _credentials = credentials;
        _baseUri = new Uri(credentials.UseDemo
            ? "https://demo-api-capital.backend-capital.com/"
            : "https://api-capital.backend-capital.com/");

        using var request = new HttpRequestMessage(HttpMethod.Post, BuildRequestUri("api/v1/session"));
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

        var loginMetadata = ParseLoginMetadata(body);
        _session = new CapitalSession
        {
            Cst = response.Headers.TryGetValues("CST", out var cst) ? cst.FirstOrDefault() ?? "" : "",
            SecurityToken = response.Headers.TryGetValues("X-SECURITY-TOKEN", out var security) ? security.FirstOrDefault() ?? "" : "",
            UseDemo = credentials.UseDemo,
            CurrentAccountId = loginMetadata.AccountId,
            AccountCurrency = loginMetadata.Currency,
        };

        if (string.IsNullOrWhiteSpace(_session.Cst) || string.IsNullOrWhiteSpace(_session.SecurityToken))
        {
            throw new InvalidOperationException($"Login succeeded but tokens were missing. Response: {body}");
        }

        return _session;
    }

    public async Task<CapitalAccountSnapshot> GetActiveAccountAsync(CancellationToken cancellationToken = default)
    {
        EnsureSession();
        using var doc = await GetJsonAsync("api/v1/accounts", cancellationToken);
        return ParseActiveAccount(doc.RootElement, _session!.CurrentAccountId, DateTimeOffset.UtcNow);
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

    public async Task<MarketInstrument?> GetMarketDetailsAsync(string epic, CancellationToken cancellationToken = default)
    {
        EnsureSession();
        using var doc = await GetJsonAsync($"api/v1/markets/{Uri.EscapeDataString(epic)}", cancellationToken);
        return ParseMarketDetails(doc.RootElement);
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

    public async Task<IReadOnlyList<OhlcPoint>> GetOhlcPricesAsync(string epic, string resolution, int max, CancellationToken cancellationToken = default)
    {
        EnsureSession();
        using var doc = await GetJsonAsync(BuildPricesPath(epic, resolution, max), cancellationToken);
        return ParseOhlcPrices(doc.RootElement);
    }

    public async Task<IReadOnlyList<OhlcPoint>> GetAllAvailableOhlcPricesAsync(string epic, string resolution, CancellationToken cancellationToken = default)
    {
        EnsureSession();
        const int apiMax = 1000;
        var step = HistoricalWindow(resolution);
        var earliest = DateTimeOffset.Parse("1970-01-01T00:00:00Z", CultureInfo.InvariantCulture);
        var to = DateTimeOffset.UtcNow.AddDays(1);
        var rowsByTime = new Dictionary<DateTimeOffset, OhlcPoint>();
        var requestCount = 0;

        while (to > earliest && requestCount < 120)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var from = to - step;
            if (from < earliest) from = earliest;

            JsonDocument doc;
            try
            {
                doc = await GetJsonAsync(BuildPricesPath(epic, resolution, apiMax, from, to), cancellationToken);
            }
            catch (CapitalApiException ex) when (ex.IsHistoryBoundary)
            {
                break;
            }

            using (doc)
            {
                var rows = ParseOhlcPrices(doc.RootElement);
                requestCount++;

                if (rows.Count == 0)
                {
                    if (rowsByTime.Count > 0) break;
                    to = from.AddSeconds(-1);
                    continue;
                }

                foreach (var row in rows)
                {
                    rowsByTime[row.Time] = row;
                }

                var oldest = rows.Min(row => row.Time);
                to = rows.Count >= apiMax ? oldest.AddSeconds(-1) : from.AddSeconds(-1);
            }
        }

        return rowsByTime.Values.OrderBy(row => row.Time).ToList();
    }

    internal static string BuildPricesPath(string epic, string resolution, int max, DateTimeOffset? from = null, DateTimeOffset? to = null)
    {
        var parts = new List<string>
        {
            $"resolution={Uri.EscapeDataString(resolution)}",
            $"max={max}",
        };
        if (from is not null) parts.Add($"from={Uri.EscapeDataString(FormatCapitalDate(from.Value))}");
        if (to is not null) parts.Add($"to={Uri.EscapeDataString(FormatCapitalDate(to.Value))}");
        return $"api/v1/prices/{Uri.EscapeDataString(epic)}?{string.Join("&", parts)}";
    }

    private static TimeSpan HistoricalWindow(string resolution) =>
        resolution.ToUpperInvariant() switch
        {
            "HOUR" => TimeSpan.FromDays(30),
            "HOUR_4" => TimeSpan.FromDays(120),
            "MINUTE" or "MINUTE_5" or "MINUTE_15" or "MINUTE_30" => TimeSpan.FromDays(5),
            "DAY" => TimeSpan.FromDays(365),
            "WEEK" => TimeSpan.FromDays(3650),
            _ => TimeSpan.FromDays(365),
        };

    private static string FormatCapitalDate(DateTimeOffset value) =>
        value.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);

    internal static IReadOnlyList<OhlcPoint> ParseOhlcPrices(string json)
    {
        using var document = JsonDocument.Parse(json);
        return ParseOhlcPrices(document.RootElement);
    }

    internal static MarketInstrument? ParseMarketDetails(string json)
    {
        using var document = JsonDocument.Parse(json);
        return ParseMarketDetails(document.RootElement);
    }

    internal static MarketInstrument? ParseMarketDetails(JsonElement root)
    {
        if (!root.TryGetProperty("instrument", out var instrument) || instrument.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var bid = root.TryGetProperty("snapshot", out var snapshot) ? ReadDecimal(snapshot, "bid") : null;
        var offer = root.TryGetProperty("snapshot", out snapshot) ? ReadDecimal(snapshot, "offer") : null;
        var price = bid is > 0 && offer is > 0 ? (bid.Value + offer.Value) / 2m : bid is > 0 ? bid : offer is > 0 ? offer : null;
        var result = new MarketInstrument
        {
            Epic = ReadString(instrument, "epic") ?? "",
            Name = ReadString(instrument, "name") ?? ReadString(instrument, "instrumentName") ?? "",
            Symbol = ReadString(instrument, "symbol") ?? "",
            Type = ReadString(instrument, "type") ?? ReadString(instrument, "instrumentType") ?? "",
            Currency = ReadString(instrument, "currency") ?? "",
            Country = ReadString(instrument, "country") ?? ReadString(instrument, "countryName") ?? "",
            Region = ReadString(instrument, "region") ?? "",
            Sector = ReadString(instrument, "sector") ?? ReadString(instrument, "industry") ?? "",
            LotSize = ReadDecimal(instrument, "lotSize"),
            MarginFactor = ReadDecimal(instrument, "marginFactor"),
            MarginFactorUnit = ReadString(instrument, "marginFactorUnit") ?? "",
            MinDealSize = ReadRuleValue(root, "minDealSize"),
            MinSizeIncrement = ReadRuleValue(root, "minSizeIncrement"),
            Bid = bid,
            Offer = offer,
            Price = price,
            LastTickAt = root.TryGetProperty("snapshot", out snapshot)
                ? ParseSnapshotTimestamp(snapshot)
                : null,
            Status = root.TryGetProperty("snapshot", out snapshot)
                ? ReadString(snapshot, "marketStatus") ?? ""
                : "",
        };

        return string.IsNullOrWhiteSpace(result.Epic) ? null : result;
    }

    internal static CapitalAccountSnapshot ParseActiveAccount(string json, string activeAccountId, DateTimeOffset retrievedAt)
    {
        using var document = JsonDocument.Parse(json);
        return ParseActiveAccount(document.RootElement, activeAccountId, retrievedAt);
    }

    internal static CapitalAccountSnapshot ParseActiveAccount(JsonElement root, string activeAccountId, DateTimeOffset retrievedAt)
    {
        if (!root.TryGetProperty("accounts", out var accounts) || accounts.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("Capital.com did not return any accounts.");
        }

        if (string.IsNullOrWhiteSpace(activeAccountId))
        {
            throw new InvalidOperationException("Capital.com current account ID was missing.");
        }

        JsonElement? selected = null;
        foreach (var account in accounts.EnumerateArray())
        {
            if (string.Equals(ReadString(account, "accountId"), activeAccountId, StringComparison.Ordinal))
            {
                selected = account;
                break;
            }
        }

        if (selected is null)
        {
            throw new InvalidOperationException($"Capital.com did not return active account '{activeAccountId}'.");
        }

        var accountValue = selected.Value;
        if (!accountValue.TryGetProperty("balance", out var balanceValue) || balanceValue.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("Capital.com account balance.available was missing.");
        }

        var balance = ReadDecimal(balanceValue, "available")
            ?? throw new InvalidOperationException("Capital.com account balance.available was not numeric.");
        return new CapitalAccountSnapshot(
            ReadString(accountValue, "accountId") ?? "",
            ReadString(accountValue, "currency") ?? ReadString(accountValue, "currencyIsoCode") ?? "",
            balance,
            retrievedAt);
    }

    private static (string AccountId, string Currency) ParseLoginMetadata(string json)
    {
        using var document = JsonDocument.Parse(json);
        return (
            ReadString(document.RootElement, "currentAccountId") ?? "",
            ReadString(document.RootElement, "currencyIsoCode") ?? "");
    }

    private static DateTimeOffset? ParseSnapshotTimestamp(JsonElement snapshot)
    {
        var source = ReadString(snapshot, "updateTimeUTC");
        if (string.IsNullOrWhiteSpace(source)) return null;
        return DateTimeOffset.TryParse(
            source,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var parsed)
                ? parsed
                : null;
    }

    private static IReadOnlyList<OhlcPoint> ParseOhlcPrices(JsonElement root)
    {
        if (!root.TryGetProperty("prices", out var prices) || prices.ValueKind != JsonValueKind.Array) return [];
        var rows = new List<OhlcPoint>();
        foreach (var row in prices.EnumerateArray())
        {
            var time = ReadString(row, "snapshotTimeUTC") ?? ReadString(row, "snapshotTime") ?? ReadString(row, "time");
            var close = ReadPrice(row, "closePrice") ?? ReadPrice(row, "lastTradedPrice");
            var open = ReadPrice(row, "openPrice");
            var high = ReadPrice(row, "highPrice");
            var low = ReadPrice(row, "lowPrice");
            if (time is null || open is null || high is null || low is null || close is null) continue;
            if (open <= 0 || high <= 0 || low <= 0 || close <= 0) continue;
            if (DateTimeOffset.TryParse(time, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
            {
                rows.Add(new OhlcPoint(parsed, open.Value, high.Value, low.Value, close.Value));
            }
        }

        return rows.OrderBy(point => point.Time).ToList();
    }

    private async Task<JsonDocument> GetJsonAsync(string uri, CancellationToken cancellationToken)
    {
        EnsureSession();
        using var request = new HttpRequestMessage(HttpMethod.Get, BuildRequestUri(uri));
        request.Headers.Add("CST", _session!.Cst);
        request.Headers.Add("X-SECURITY-TOKEN", _session.SecurityToken);
        request.Headers.Add("X-CAP-API-KEY", _credentials!.ApiKey);

        using var response = await _http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new CapitalApiException(response.StatusCode, response.ReasonPhrase ?? "Request failed", body);
        }
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
    }

    private void EnsureSession()
    {
        if (_session is null || _credentials is null) throw new InvalidOperationException("Connect to Capital.com first.");
    }

    private Uri BuildRequestUri(string relativeUri)
    {
        if (_baseUri is null) throw new InvalidOperationException("Connect to Capital.com first.");
        return new Uri(_baseUri, relativeUri);
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
                LotSize = ReadDecimal(market, "lotSize"),
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

    private static decimal? ReadDecimal(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var value) &&
            value.ValueKind == JsonValueKind.Number &&
            value.TryGetDecimal(out var result)
                ? result
                : null;
    }

    private static decimal? ReadRuleValue(JsonElement root, string ruleName)
    {
        if (!root.TryGetProperty("dealingRules", out var rules) || rules.ValueKind != JsonValueKind.Object) return null;
        if (!rules.TryGetProperty(ruleName, out var rule) || rule.ValueKind != JsonValueKind.Object) return null;
        return ReadDecimal(rule, "value");
    }

    public void Dispose()
    {
        if (_disposeHttp) _http.Dispose();
    }
}
