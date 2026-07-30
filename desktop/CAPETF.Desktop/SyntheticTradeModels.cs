namespace CAPETF.Desktop;

public sealed record CapitalPositionRequest(string Epic, string Direction, decimal Size);

public sealed record CapitalDealAcknowledgement(string DealReference, string DealStatus, string Reason);

public sealed record CapitalAffectedDeal(string DealId, string Status);

public sealed record CapitalDealConfirmation(
    string DealReference,
    string DealStatus,
    string DealId,
    decimal? Level,
    IReadOnlyList<CapitalAffectedDeal> AffectedDeals,
    string Reason);

public sealed record CapitalOpenPosition(
    string DealId,
    string Epic,
    string Direction,
    decimal? Size,
    decimal? Level,
    decimal? UnrealizedProfitLoss,
    string Currency,
    string MarketStatus);

public sealed record SyntheticPreflightInput(
    bool IsDemoSession,
    string BasketId,
    SyntheticBasket Basket,
    string Side,
    decimal RequestedNotional,
    DateTimeOffset NowUtc,
    SyntheticMarginSummary? Margin);

public sealed record SyntheticPreflightFailure(string Epic, string Reason);

public sealed record SyntheticExecutionLeg(
    string Epic,
    string Direction,
    decimal Multiplier,
    decimal ReferencePrice,
    decimal Quantity,
    decimal Notional,
    decimal EstimatedMargin,
    string MarginCurrency);

public sealed record SyntheticExecutionTicket(
    string TicketId,
    string BasketId,
    string Side,
    decimal RequestedNotional,
    DateTimeOffset CreatedUtc,
    DateTimeOffset ExpiresUtc,
    decimal EstimatedMargin,
    string MarginCurrency,
    IReadOnlyList<SyntheticExecutionLeg> Legs);

public sealed record SyntheticPreflightResult(
    bool IsReady,
    SyntheticExecutionTicket? Ticket,
    IReadOnlyList<SyntheticPreflightFailure> Failures);
