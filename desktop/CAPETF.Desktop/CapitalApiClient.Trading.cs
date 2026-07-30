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

    public async Task<CapitalAccountPreferences> GetAccountPreferencesAsync(CancellationToken cancellationToken = default)
    {
        using var document = await SendTradingJsonAsync(HttpMethod.Get, "api/v1/accounts/preferences", null, cancellationToken);
        return new CapitalAccountPreferences(
            document.RootElement.TryGetProperty("hedgingMode", out var value)
            && value.ValueKind is JsonValueKind.True or JsonValueKind.False
            && value.GetBoolean());
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
                ReadString(market, "marketStatus") ?? ReadString(position, "marketStatus") ?? ""));
        }

        return positions;
    }
}
