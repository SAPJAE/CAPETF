using CAPETF.Desktop;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace CAPETF.Desktop.Tests;

public static class SyntheticTradingTests
{
    public static void RunAll()
    {
        PreflightRejectsNonDemoSessions();
        PreflightRejectsInvalidComponentCounts();
        PreflightRejectsDuplicateEpics();
        PreflightReturnsLegFailuresInEpicOrder();
        PreflightRejectsZeroAndStaleQuotes();
        PreflightRejectsInvalidRoundedSize();
        PreflightRejectsMissingMargin();
        PreflightRejectsInsufficientFunds();
        PreflightCreatesFrozenTicketWithReversedNegativeLeg();
        ProductionTransportDisablesAutomaticRedirects();
        LiveMutationIsRejectedBeforeItIsSent();
        DemoPositionRequestUsesCapitalContract();
        DemoPositionRedirectDoesNotReachRedirectTarget();
        DealConfirmationParsesRequiredFields();
        OpenPositionsParseRequiredFields();
        DemoClosePositionUsesDeleteWithoutRetry();
        DemoCloseRedirectDoesNotReachRedirectTarget();
    }

    private static void PreflightRejectsNonDemoSessions()
    {
        var result = SyntheticTradePreflight.Build(CreatePreflightInput() with { IsDemoSession = false });

        AssertFalse(result.IsReady, "live sessions must fail preflight");
        AssertContainsFailure(result, "", "Demo trading session is required.");
    }

    private static void PreflightRejectsInvalidComponentCounts()
    {
        var basket = CreateBasket().Components.Take(2).ToList();
        var result = SyntheticTradePreflight.Build(CreatePreflightInput(CreateBasket(basket)));

        AssertFalse(result.IsReady, "two-leg baskets must fail preflight");
        AssertContainsFailure(result, "", "Synthetic baskets must contain 3 or 4 components.");
    }

    private static void PreflightRejectsDuplicateEpics()
    {
        var components = CreateBasket().Components.ToList();
        components[1] = CreateComponent("alpha", -1m);
        var result = SyntheticTradePreflight.Build(CreatePreflightInput(CreateBasket(components)));

        AssertFalse(result.IsReady, "duplicate epics must fail preflight");
        AssertContainsFailure(result, "ALPHA", "Duplicate epic.");
    }

    private static void PreflightReturnsLegFailuresInEpicOrder()
    {
        var basket = CreateBasket();
        basket.Components.Single(component => component.Instrument.Epic == "ALPHA").Instrument.Status = "SUSPENDED";
        basket.Components.Single(component => component.Instrument.Epic == "ZETA").Instrument.Status = "CLOSED";

        var result = SyntheticTradePreflight.Build(CreatePreflightInput(basket));

        AssertFalse(result.IsReady, "untradeable legs must fail preflight");
        AssertEqual(
            "ALPHA|ZETA",
            string.Join("|", result.Failures.Where(failure => failure.Reason == "Market is not TRADEABLE.").Select(failure => failure.Epic)),
            "leg failures must be reported in deterministic epic order");
    }

    private static void PreflightRejectsZeroAndStaleQuotes()
    {
        var now = DateTimeOffset.Parse("2026-07-30T12:00:00Z");
        var basket = CreateBasket();
        basket.Components.Single(component => component.Instrument.Epic == "BETA").Instrument.Bid = 0m;
        basket.Components.Single(component => component.Instrument.Epic == "GAMMA").Instrument.LastTickAt = now.AddMinutes(-5).AddTicks(-1);

        var result = SyntheticTradePreflight.Build(CreatePreflightInput(basket, now));

        AssertFalse(result.IsReady, "zero and stale quotes must fail preflight");
        AssertContainsFailure(result, "BETA", "Bid and offer prices must be positive.");
        AssertContainsFailure(result, "GAMMA", "Quote is older than five minutes.");
    }

    private static void PreflightRejectsInvalidRoundedSize()
    {
        var components = CreateBasket().Components.ToList();
        components[0] = CreateComponent("ALPHA", 1m, 0m);
        components[0].Instrument.MinDealSize = null;
        components[0].Instrument.MinSizeIncrement = null;
        var basket = CreateBasket(components);

        var result = SyntheticTradePreflight.Build(CreatePreflightInput(basket));

        AssertFalse(result.IsReady, "zero rounded quantities must fail preflight");
        AssertContainsFailure(result, "ALPHA", "Rounded size is invalid.");
    }

    private static void PreflightRejectsMissingMargin()
    {
        var result = SyntheticTradePreflight.Build(CreatePreflightInput() with { Margin = null });

        AssertFalse(result.IsReady, "missing margin must fail preflight");
        AssertContainsFailure(result, "", "Margin preview is unavailable.");
    }

    private static void PreflightRejectsInsufficientFunds()
    {
        var result = SyntheticTradePreflight.Build(CreatePreflightInput() with { Margin = CreateMarginSummary(available: 119m) });

        AssertFalse(result.IsReady, "insufficient available funds must fail preflight");
        AssertContainsFailure(result, "", "Estimated margin exceeds available funds.");
    }

    private static void PreflightCreatesFrozenTicketWithReversedNegativeLeg()
    {
        var now = DateTimeOffset.Parse("2026-07-30T12:00:00Z");
        var basket = CreateBasket();
        var result = SyntheticTradePreflight.Build(CreatePreflightInput(basket, now));

        AssertTrue(result.IsReady, "valid demo basket must be ready");
        var ticket = result.Ticket ?? throw new Exception("ready preflight must create a ticket");
        AssertTrue(!string.IsNullOrWhiteSpace(ticket.TicketId), "ticket must have an ID");
        AssertEqual("basket-123", ticket.BasketId, "ticket basket ID");
        AssertEqual("BUY", ticket.Side, "ticket side");
        AssertEqual(600m, ticket.RequestedNotional, "ticket requested notional");
        AssertEqual(now, ticket.CreatedUtc, "ticket creation time");
        AssertEqual(now.AddMinutes(2), ticket.ExpiresUtc, "ticket expiry");
        AssertEqual(120m, ticket.EstimatedMargin, "ticket estimated margin");
        var negativeLeg = ticket.Legs.Single(leg => leg.Multiplier < 0m);
        AssertEqual("SELL", negativeLeg.Direction, "negative leg reverses BUY basket");
        AssertEqual(15m, negativeLeg.Quantity, "ticket copies executable quantity");
        AssertEqual(10m, negativeLeg.ReferencePrice, "ticket copies executable price");
        AssertEqual(30m, negativeLeg.EstimatedMargin, "ticket copies executable margin");

        basket.Components.Single(component => component.Instrument.Epic == negativeLeg.Epic).Instrument.Bid = 99m;
        AssertEqual(10m, negativeLeg.ReferencePrice, "chart updates must not mutate a frozen ticket");
    }

    private static SyntheticPreflightInput CreatePreflightInput(
        SyntheticBasket? basket = null,
        DateTimeOffset? now = null) =>
        new(
            true,
            "basket-123",
            basket ?? CreateBasket(),
            "BUY",
            600m,
            now ?? DateTimeOffset.Parse("2026-07-30T12:00:00Z"),
            CreateMarginSummary());

    private static SyntheticBasket CreateBasket(IEnumerable<SyntheticComponent>? components = null)
    {
        var basket = new SyntheticBasket { Symbol = "SYN-TEST-01" };
        foreach (var component in components ?? new[]
        {
            CreateComponent("ALPHA", 1m),
            CreateComponent("BETA", -1m),
            CreateComponent("GAMMA", 1m),
            CreateComponent("ZETA", 1m),
        })
        {
            basket.Components.Add(component);
        }
        return basket;
    }

    private static SyntheticComponent CreateComponent(string epic, decimal multiplier, decimal weight = 25m) =>
        new(
            new MarketInstrument
            {
                Epic = epic,
                Currency = "USD",
                Bid = 10m,
                Offer = 10m,
                LastTickAt = DateTimeOffset.Parse("2026-07-30T12:00:00Z"),
                Status = "TRADEABLE",
                LotSize = 1m,
                MinDealSize = 1m,
                MinSizeIncrement = 1m,
            },
            weight,
            10m,
            20m)
        {
            FormulaMultiplier = multiplier,
        };

    private static SyntheticMarginSummary CreateMarginSummary(decimal available = 500m)
    {
        var legs = new[]
        {
            CreateMarginLeg("BUY", "ALPHA"),
            CreateMarginLeg("SELL", "BETA"),
            CreateMarginLeg("BUY", "GAMMA"),
            CreateMarginLeg("BUY", "ZETA"),
        };
        var buy = new SyntheticMarginSidePreview("BUY", "USD", true, "", 120m, legs);
        var sell = new SyntheticMarginSidePreview("SELL", "USD", true, "", 120m, legs);
        return new SyntheticMarginSummary("USD", available, available - 120m, available - 120m, buy, sell);
    }

    private static SyntheticMarginLegPreview CreateMarginLeg(string side, string epic) =>
        new(side, epic, 10m, 15m, 150m, "USD", 30m, "USD", 30m);

    private static void AssertContainsFailure(SyntheticPreflightResult result, string epic, string reason)
    {
        if (!result.Failures.Any(failure => failure.Epic == epic && failure.Reason == reason))
        {
            throw new Exception($"preflight failure missing: {epic} {reason}");
        }
    }

    private static void ProductionTransportDisablesAutomaticRedirects()
    {
        using var handler = CapitalApiClient.CreateProductionHttpHandler();

        AssertFalse(handler.AllowAutoRedirect, "production transport must not follow redirects");
    }

    private static void LiveMutationIsRejectedBeforeItIsSent()
    {
        var handler = new TradingHandler();
        using var client = Login(handler, useDemo: false);
        var request = new CapitalPositionRequest("AAPL", "BUY", 2m);

        var exception = AssertThrows<InvalidOperationException>(
            () => client.CreatePositionAsync(request, default).GetAwaiter().GetResult(),
            "live trading must be rejected");

        AssertTrue(exception.Message.Contains("demo", StringComparison.OrdinalIgnoreCase), "rejection must explain the demo-only restriction");
        AssertEqual(0, handler.Requests.Count(request => request.Method != HttpMethod.Post || request.Path != "/api/v1/session"), "live mutation must not be sent");
    }

    private static void DemoPositionRequestUsesCapitalContract()
    {
        var handler = new TradingHandler();
        using var client = Login(handler, useDemo: true);

        var acknowledgement = client.CreatePositionAsync(new CapitalPositionRequest("AAPL", "BUY", 2.5m), default).GetAwaiter().GetResult();

        var sent = handler.Requests.Single(request => request.Path == "/api/v1/positions");
        using var body = JsonDocument.Parse(sent.Body);
        AssertEqual(HttpMethod.Post, sent.Method, "position method");
        AssertEqual("AAPL", body.RootElement.GetProperty("epic").GetString(), "position epic");
        AssertEqual("BUY", body.RootElement.GetProperty("direction").GetString(), "position direction");
        AssertEqual(2.5m, body.RootElement.GetProperty("size").GetDecimal(), "position size");
        AssertFalse(body.RootElement.GetProperty("guaranteedStop").GetBoolean(), "position must disable guaranteed stops");
        AssertFalse(body.RootElement.TryGetProperty("orderType", out _), "position must not infer an unsupported order type");
        AssertEqual("REF-123", acknowledgement.DealReference, "position acknowledgement reference");
    }

    private static void DealConfirmationParsesRequiredFields()
    {
        var handler = new TradingHandler();
        using var client = Login(handler, useDemo: true);

        var confirmation = client.GetDealConfirmationAsync("REF-123", default).GetAwaiter().GetResult();

        AssertEqual("ACCEPTED", confirmation.DealStatus, "confirmation status");
        AssertEqual("DEAL-123", confirmation.DealId, "confirmation deal ID");
        AssertEqual(123.45m, confirmation.Level, "confirmation level");
        AssertEqual(2, confirmation.AffectedDeals.Count, "confirmation affected deal count");
        AssertEqual("DEAL-OTHER", confirmation.AffectedDeals[1].DealId, "confirmation affected deal");
    }

    private static void DemoPositionRedirectDoesNotReachRedirectTarget()
    {
        var handler = new RedirectingTradingHandler();
        using var client = Login(handler, useDemo: true);

        var exception = AssertThrows<CapitalApiException>(
            () => client.CreatePositionAsync(new CapitalPositionRequest("AAPL", "BUY", 2.5m), default).GetAwaiter().GetResult(),
            "redirected position request must fail without a follow-up request");

        AssertEqual(HttpStatusCode.TemporaryRedirect, exception.StatusCode, "position redirect status");
        AssertEqual(1, handler.MutationRequests.Count, "position redirect must issue one mutation request");
        AssertEqual("demo-api-capital.backend-capital.com", handler.MutationRequests[0].Host, "position must never reach the redirect target");
    }

    private static void OpenPositionsParseRequiredFields()
    {
        var handler = new TradingHandler();
        using var client = Login(handler, useDemo: true);

        var positions = client.GetOpenPositionsAsync(default).GetAwaiter().GetResult();

        AssertEqual(1, positions.Count, "open position count");
        var position = positions[0];
        AssertEqual("DEAL-123", position.DealId, "open position deal ID");
        AssertEqual("AAPL", position.Epic, "open position epic");
        AssertEqual("BUY", position.Direction, "open position direction");
        AssertEqual(2.5m, position.Size, "open position size");
        AssertEqual(123.45m, position.Level, "open position level");
        AssertEqual(17.25m, position.UnrealizedProfitLoss, "open position UPL");
        AssertEqual("USD", position.Currency, "open position currency");
        AssertEqual("TRADEABLE", position.MarketStatus, "open position market status");
    }

    private static void DemoClosePositionUsesDeleteWithoutRetry()
    {
        var handler = new TradingHandler();
        using var client = Login(handler, useDemo: true);

        var acknowledgement = client.ClosePositionAsync("DEAL-123", default).GetAwaiter().GetResult();

        var sent = handler.Requests.Single(request => request.Path == "/api/v1/positions/DEAL-123");
        AssertEqual(HttpMethod.Delete, sent.Method, "close method");
        AssertEqual("REF-CLOSE-123", acknowledgement.DealReference, "close acknowledgement reference");
        AssertEqual(1, handler.Requests.Count(request => request.Path == "/api/v1/positions/DEAL-123"), "close must not retry");
    }

    private static void DemoCloseRedirectDoesNotReachRedirectTarget()
    {
        var handler = new RedirectingTradingHandler();
        using var client = Login(handler, useDemo: true);

        var exception = AssertThrows<CapitalApiException>(
            () => client.ClosePositionAsync("DEAL-123", default).GetAwaiter().GetResult(),
            "redirected close request must fail without a follow-up request");

        AssertEqual(HttpStatusCode.TemporaryRedirect, exception.StatusCode, "close redirect status");
        AssertEqual(1, handler.MutationRequests.Count, "close redirect must issue one mutation request");
        AssertEqual("demo-api-capital.backend-capital.com", handler.MutationRequests[0].Host, "close must never reach the redirect target");
    }

    private static CapitalApiClient Login(HttpMessageHandler handler, bool useDemo)
    {
        var client = new CapitalApiClient(handler);
        client.LoginAsync(new ApiCredentials
        {
            Identifier = "test-user",
            Password = "test-password",
            ApiKey = "test-key",
            UseDemo = useDemo,
        }).GetAwaiter().GetResult();
        return client;
    }

    private static TException AssertThrows<TException>(Action action, string message) where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException exception)
        {
            return exception;
        }

        throw new Exception(message);
    }

    private static void AssertTrue(bool value, string message)
    {
        if (!value) throw new Exception(message);
    }

    private static void AssertFalse(bool value, string message) => AssertTrue(!value, message);

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new Exception($"{message}: expected {expected}, actual {actual}");
        }
    }

    private sealed class TradingHandler : HttpMessageHandler
    {
        public List<RecordedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add(new RecordedRequest(
                request.Method,
                request.RequestUri?.Host ?? "",
                request.RequestUri?.AbsolutePath ?? "",
                body));

            var response = request.RequestUri?.AbsolutePath switch
            {
                "/api/v1/session" => JsonResponse(HttpStatusCode.OK, "{}", includeSessionHeaders: true),
                "/api/v1/positions" when request.Method == HttpMethod.Post => JsonResponse(HttpStatusCode.OK, "{\"dealReference\":\"REF-123\"}"),
                "/api/v1/confirms/REF-123" => JsonResponse(HttpStatusCode.OK, "{\"dealStatus\":\"ACCEPTED\",\"dealId\":\"DEAL-123\",\"level\":123.45,\"affectedDeals\":[\"DEAL-123\",\"DEAL-OTHER\"]}"),
                "/api/v1/positions" => JsonResponse(HttpStatusCode.OK, "{\"positions\":[{\"position\":{\"dealId\":\"DEAL-123\",\"direction\":\"BUY\",\"size\":2.5,\"level\":123.45,\"upl\":17.25,\"currency\":\"USD\"},\"market\":{\"epic\":\"AAPL\",\"marketStatus\":\"TRADEABLE\"}}]}"),
                "/api/v1/positions/DEAL-123" when request.Method == HttpMethod.Delete => JsonResponse(HttpStatusCode.OK, "{\"dealReference\":\"REF-CLOSE-123\"}"),
                _ => JsonResponse(HttpStatusCode.NotFound, "{}"),
            };
            return response;
        }

        private static HttpResponseMessage JsonResponse(HttpStatusCode status, string body, bool includeSessionHeaders = false)
        {
            var response = new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
            if (includeSessionHeaders)
            {
                response.Headers.Add("CST", "cst-token");
                response.Headers.Add("X-SECURITY-TOKEN", "security-token");
            }
            return response;
        }
    }

    private sealed class RedirectingTradingHandler : HttpMessageHandler
    {
        public List<RecordedRequest> Requests { get; } = [];
        public IReadOnlyList<RecordedRequest> MutationRequests => Requests.Where(request => request.Path != "/api/v1/session").ToList();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add(new RecordedRequest(
                request.Method,
                request.RequestUri?.Host ?? "",
                request.RequestUri?.AbsolutePath ?? "",
                body));

            if (request.RequestUri?.AbsolutePath == "/api/v1/session")
            {
                return JsonResponse(HttpStatusCode.OK, "{}", includeSessionHeaders: true);
            }

            var response = JsonResponse(HttpStatusCode.TemporaryRedirect, "{}");
            response.Headers.Location = new Uri("https://api-capital.backend-capital.com/api/v1/positions");
            return response;
        }

        private static HttpResponseMessage JsonResponse(HttpStatusCode status, string body, bool includeSessionHeaders = false)
        {
            var response = new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
            if (includeSessionHeaders)
            {
                response.Headers.Add("CST", "cst-token");
                response.Headers.Add("X-SECURITY-TOKEN", "security-token");
            }
            return response;
        }
    }

    private sealed record RecordedRequest(HttpMethod Method, string Host, string Path, string Body);
}
