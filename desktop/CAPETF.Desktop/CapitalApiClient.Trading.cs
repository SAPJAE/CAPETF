using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace CAPETF.Desktop;

public sealed partial class CapitalApiClient
{
    public bool IsDemoTradingSession =>
        _baseUri?.Host.Equals("demo-api-capital.backend-capital.com", StringComparison.OrdinalIgnoreCase) == true;

    public async Task<CapitalDealAcknowledgement> CreatePositionAsync(CapitalPositionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureDemoMutationAllowed();

        using var document = await SendTradingJsonAsync(
            HttpMethod.Post,
            "api/v1/positions",
            new
            {
                epic = request.Epic,
                direction = request.Direction,
                size = request.Size,
                guaranteedStop = false,
            },
            cancellationToken);
        return ParseAcknowledgement(document.RootElement);
    }

    public async Task<CapitalDealConfirmation> GetDealConfirmationAsync(string dealReference, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dealReference)) throw new ArgumentException("A deal reference is required.", nameof(dealReference));

        using var document = await SendTradingJsonAsync(
            HttpMethod.Get,
            $"api/v1/confirms/{Uri.EscapeDataString(dealReference)}",
            null,
            cancellationToken);
        return ParseConfirmation(document.RootElement, dealReference);
    }

    public async Task<IReadOnlyList<CapitalOpenPosition>> GetOpenPositionsAsync(CancellationToken cancellationToken = default)
    {
        using var document = await SendTradingJsonAsync(HttpMethod.Get, "api/v1/positions", null, cancellationToken);
        return ParseOpenPositions(document.RootElement);
    }

    public async Task<IReadOnlyList<CapitalWorkingOrder>> GetWorkingOrdersAsync(CancellationToken cancellationToken = default)
    {
        using var document = await SendTradingJsonAsync(HttpMethod.Get, "api/v1/workingorders", null, cancellationToken);
        return ParseWorkingOrders(document.RootElement);
    }

    public async Task<CapitalBrokerAccount> GetBrokerAccountAsync(CancellationToken cancellationToken = default)
    {
        EnsureSession();
        using var document = await SendTradingJsonAsync(HttpMethod.Get, "api/v1/accounts", null, cancellationToken);
        return ParseBrokerAccount(document.RootElement, _session!.CurrentAccountId, DateTimeOffset.UtcNow);
    }

    public async Task<CapitalBrokerSnapshot> GetBrokerSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var account = await GetBrokerAccountAsync(cancellationToken);
        var positions = await GetOpenPositionsAsync(cancellationToken);
        var orders = await GetWorkingOrdersAsync(cancellationToken);
        return new CapitalBrokerSnapshot(account, positions, orders, DateTimeOffset.UtcNow);
    }

    public async Task<CapitalAccountPreferences> GetAccountPreferencesAsync(CancellationToken cancellationToken = default)
    {
        using var document = await SendTradingJsonAsync(HttpMethod.Get, "api/v1/accounts/preferences", null, cancellationToken);
        return new CapitalAccountPreferences(
            document.RootElement.TryGetProperty("hedgingMode", out var value)
            && value.ValueKind is JsonValueKind.True or JsonValueKind.False
            && value.GetBoolean());
    }

    public async Task SetHedgingModeAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        EnsureDemoMutationAllowed();
        using var document = await SendTradingJsonAsync(
            HttpMethod.Put,
            "api/v1/accounts/preferences",
            new { hedgingMode = enabled },
            cancellationToken);
        if (!document.RootElement.TryGetProperty("status", out var status) ||
            !string.Equals(status.GetString(), "SUCCESS", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Capital.com did not confirm the hedging-mode update.");
        }
    }

    public async Task<CapitalDealAcknowledgement> ClosePositionAsync(string dealId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dealId)) throw new ArgumentException("A deal ID is required.", nameof(dealId));
        EnsureDemoMutationAllowed();

        using var document = await SendTradingJsonAsync(
            HttpMethod.Delete,
            $"api/v1/positions/{Uri.EscapeDataString(dealId)}",
            null,
            cancellationToken);
        return ParseAcknowledgement(document.RootElement);
    }

    private void EnsureDemoMutationAllowed()
    {
        EnsureSession();
        if (!IsDemoTradingSession) throw new InvalidOperationException("Trading is restricted to Capital.com demo accounts.");
    }

    private async Task<JsonDocument> SendTradingJsonAsync(
        HttpMethod method,
        string uri,
        object? body,
        CancellationToken cancellationToken)
    {
        EnsureSession();
        using var request = new HttpRequestMessage(method, BuildRequestUri(uri));
        request.Headers.Add("CST", _session!.Cst);
        request.Headers.Add("X-SECURITY-TOKEN", _session.SecurityToken);
        request.Headers.Add("X-CAP-API-KEY", _credentials!.ApiKey);
        if (body is not null)
        {
            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        }

        using var response = await _http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new CapitalApiException(response.StatusCode, response.ReasonPhrase ?? "Request failed", responseBody);
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
    }

    private static CapitalDealAcknowledgement ParseAcknowledgement(JsonElement root) => new(
        ReadString(root, "dealReference") ?? "",
        ReadString(root, "dealStatus") ?? ReadString(root, "status") ?? "",
        ReadString(root, "reason") ?? "");

    private static CapitalDealConfirmation ParseConfirmation(JsonElement root, string dealReference)
    {
        var affectedDeals = new List<CapitalAffectedDeal>();
        if (root.TryGetProperty("affectedDeals", out var values) && values.ValueKind == JsonValueKind.Array)
        {
            foreach (var value in values.EnumerateArray())
            {
                if (value.ValueKind == JsonValueKind.String)
                {
                    affectedDeals.Add(new CapitalAffectedDeal(value.GetString() ?? "", ""));
                    continue;
                }

                if (value.ValueKind == JsonValueKind.Object)
                {
                    affectedDeals.Add(new CapitalAffectedDeal(
                        ReadString(value, "dealId") ?? "",
                        ReadString(value, "status") ?? ""));
                }
            }
        }

        return new CapitalDealConfirmation(
            ReadString(root, "dealReference") ?? dealReference,
            ReadString(root, "dealStatus") ?? "",
            ReadString(root, "dealId") ?? "",
            ReadDecimal(root, "level"),
            affectedDeals,
            ReadString(root, "reason") ?? "");
    }

    private static IReadOnlyList<CapitalOpenPosition> ParseOpenPositions(JsonElement root)
    {
        if (!root.TryGetProperty("positions", out var values) || values.ValueKind != JsonValueKind.Array) return [];

        var positions = new List<CapitalOpenPosition>();
        foreach (var value in values.EnumerateArray())
        {
            if (value.ValueKind != JsonValueKind.Object) continue;
            var position = value.TryGetProperty("position", out var positionValue) && positionValue.ValueKind == JsonValueKind.Object
                ? positionValue
                : value;
            var market = value.TryGetProperty("market", out var marketValue) && marketValue.ValueKind == JsonValueKind.Object
                ? marketValue
                : value;
            positions.Add(new CapitalOpenPosition(
                ReadString(position, "dealId") ?? "",
                ReadString(market, "epic") ?? ReadString(position, "epic") ?? "",
                ReadString(position, "direction") ?? "",
                ReadDecimal(position, "size"),
                ReadDecimal(position, "level"),
                ReadDecimal(position, "upl"),
                ReadString(position, "currency") ?? "",
                ReadString(market, "marketStatus") ?? ReadString(position, "marketStatus") ?? "",
                ReadDecimal(position, "stopLevel"),
                ReadDecimal(position, "profitLevel"),
                ReadDecimal(market, "bid"),
                ReadDecimal(market, "offer"),
                ReadString(market, "instrumentName") ?? "",
                ReadDateTimeOffset(position, "createdDateUTC")));
        }

        return positions;
    }

    internal static IReadOnlyList<CapitalWorkingOrder> ParseWorkingOrders(string json)
    {
        using var document = JsonDocument.Parse(json);
        return ParseWorkingOrders(document.RootElement);
    }

    private static IReadOnlyList<CapitalWorkingOrder> ParseWorkingOrders(JsonElement root)
    {
        if (!root.TryGetProperty("workingOrders", out var values) || values.ValueKind != JsonValueKind.Array) return [];

        var orders = new List<CapitalWorkingOrder>();
        foreach (var value in values.EnumerateArray())
        {
            if (value.ValueKind != JsonValueKind.Object) continue;
            var order = value.TryGetProperty("workingOrderData", out var orderValue) && orderValue.ValueKind == JsonValueKind.Object
                ? orderValue
                : value;
            var market = value.TryGetProperty("marketData", out var marketValue) && marketValue.ValueKind == JsonValueKind.Object
                ? marketValue
                : value;
            orders.Add(new CapitalWorkingOrder(
                ReadString(order, "dealId") ?? "",
                ReadString(order, "epic") ?? ReadString(market, "epic") ?? "",
                ReadString(order, "direction") ?? "",
                ReadDecimal(order, "orderSize") ?? ReadDecimal(order, "size"),
                ReadDecimal(order, "orderLevel") ?? ReadDecimal(order, "level"),
                ReadString(order, "orderType") ?? "",
                ReadString(order, "timeInForce") ?? "",
                ReadDecimal(order, "stopLevel"),
                ReadDecimal(order, "profitLevel"),
                ReadString(order, "currencyCode") ?? ReadString(order, "currency") ?? "",
                ReadString(market, "marketStatus") ?? "",
                ReadDecimal(market, "bid"),
                ReadDecimal(market, "offer"),
                ReadString(market, "instrumentName") ?? "",
                ReadDateTimeOffset(order, "createdDateUTC")));
        }

        return orders;
    }

    internal static CapitalBrokerAccount ParseBrokerAccount(string json, string activeAccountId, DateTimeOffset retrievedAt)
    {
        using var document = JsonDocument.Parse(json);
        return ParseBrokerAccount(document.RootElement, activeAccountId, retrievedAt);
    }

    private static CapitalBrokerAccount ParseBrokerAccount(JsonElement root, string activeAccountId, DateTimeOffset retrievedAt)
    {
        if (!root.TryGetProperty("accounts", out var accounts) || accounts.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("Capital.com did not return any accounts.");

        foreach (var account in accounts.EnumerateArray())
        {
            if (!string.Equals(ReadString(account, "accountId"), activeAccountId, StringComparison.Ordinal)) continue;
            var balance = account.TryGetProperty("balance", out var balanceValue) && balanceValue.ValueKind == JsonValueKind.Object
                ? balanceValue
                : default;
            return new CapitalBrokerAccount(
                ReadString(account, "accountId") ?? "",
                ReadString(account, "currency") ?? ReadString(account, "currencyIsoCode") ?? "",
                balance.ValueKind == JsonValueKind.Object ? ReadDecimal(balance, "balance") : null,
                balance.ValueKind == JsonValueKind.Object ? ReadDecimal(balance, "deposit") : null,
                balance.ValueKind == JsonValueKind.Object ? ReadDecimal(balance, "profitLoss") : null,
                balance.ValueKind == JsonValueKind.Object ? ReadDecimal(balance, "available") : null,
                retrievedAt);
        }

        throw new InvalidOperationException($"Capital.com did not return active account '{activeAccountId}'.");
    }

    private static DateTimeOffset? ReadDateTimeOffset(JsonElement element, string property)
    {
        var value = ReadString(element, property);
        return DateTimeOffset.TryParse(value, out var parsed) ? parsed : null;
    }
}
