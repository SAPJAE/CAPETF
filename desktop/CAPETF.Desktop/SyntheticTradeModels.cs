namespace CAPETF.Desktop;

public sealed record CapitalPositionRequest(string Epic, string Direction, decimal Size);

public sealed record CapitalDealAcknowledgement(
    string DealReference,
    string DealStatus,
    string Reason,
    string RecoveredDealId = "",
    decimal? RecoveredLevel = null);

public sealed record CapitalAccountPreferences(bool HedgingMode);

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
    string MarketStatus,
    decimal? StopLevel = null,
    decimal? ProfitLevel = null,
    decimal? Bid = null,
    decimal? Offer = null,
    string InstrumentName = "",
    DateTimeOffset? CreatedUtc = null);

public sealed record CapitalWorkingOrder(
    string DealId,
    string Epic,
    string Direction,
    decimal? Size,
    decimal? OrderLevel,
    string OrderType,
    string TimeInForce,
    decimal? StopLevel,
    decimal? ProfitLevel,
    string Currency,
    string MarketStatus,
    decimal? Bid,
    decimal? Offer,
    string InstrumentName,
    DateTimeOffset? CreatedUtc);

public sealed record CapitalBrokerAccount(
    string AccountId,
    string Currency,
    decimal? Balance,
    decimal? Deposit,
    decimal? ProfitLoss,
    decimal? Available,
    DateTimeOffset RetrievedAt);

public sealed record CapitalBrokerSnapshot(
    CapitalBrokerAccount Account,
    IReadOnlyList<CapitalOpenPosition> Positions,
    IReadOnlyList<CapitalWorkingOrder> WorkingOrders,
    DateTimeOffset RetrievedAt);

public sealed record SyntheticPreflightInput(
    bool IsDemoSession,
    string BasketId,
    SyntheticBasket Basket,
    string Side,
    decimal RequestedNotional,
    DateTimeOffset NowUtc,
    SyntheticMarginSummary? Margin,
    string AccountId = "",
    bool HedgingMode = false);

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
    IReadOnlyList<SyntheticExecutionLeg> Legs,
    string AccountId = "");

public enum SyntheticExecutionState
{
    Preflighting,
    Ready,
    Submitting,
    PartiallyOpen,
    Open,
    NeedsAttention,
    Closing,
    PartiallyClosed,
    Closed,
    Rejected,
}

public enum SyntheticExecutionLegState
{
    Pending,
    Submitted,
    Confirming,
    Open,
    Rejected,
    Unknown,
    Closing,
    Closed,
}

public sealed record SyntheticExecutionLegRecord(
    string Epic,
    string Direction,
    decimal Multiplier,
    decimal ReferencePrice,
    decimal Quantity,
    decimal Notional,
    decimal EstimatedMargin,
    string MarginCurrency,
    SyntheticExecutionLegState State,
    string DealReference,
    string DealId,
    string CloseDealReference,
    decimal? FillLevel,
    string Message,
    DateTimeOffset? SubmittedUtc,
    DateTimeOffset? ConfirmedUtc,
    DateTimeOffset? ClosedUtc,
    DateTimeOffset UpdatedUtc,
    decimal? CurrentUnrealizedProfitLoss = null);

public sealed record SyntheticExecutionRecord(
    string ExecutionId,
    string TicketId,
    string BasketId,
    string Side,
    decimal RequestedNotional,
    decimal EstimatedMargin,
    string MarginCurrency,
    DateTimeOffset CreatedUtc,
    DateTimeOffset UpdatedUtc,
    SyntheticExecutionState State,
    IReadOnlyList<SyntheticExecutionLegRecord> Legs,
    string AccountId = "");

public sealed record SyntheticPreflightResult(
    bool IsReady,
    SyntheticExecutionTicket? Ticket,
    IReadOnlyList<SyntheticPreflightFailure> Failures);
