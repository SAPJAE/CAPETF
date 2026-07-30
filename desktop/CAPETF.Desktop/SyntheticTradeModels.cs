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
