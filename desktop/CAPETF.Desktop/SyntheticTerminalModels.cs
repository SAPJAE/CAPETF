namespace CAPETF.Desktop;

public sealed record TerminalCandle(long Time, decimal Open, decimal High, decimal Low, decimal Close);

public sealed record TerminalLinePoint(long Time, decimal Value);

public sealed record TerminalComponentQuote(
    string Epic,
    decimal? Bid,
    decimal? Offer,
    DateTimeOffset? QuoteTimestamp,
    string QuoteStatus);

public sealed record SyntheticTerminalTickPayload(
    TerminalCandle? Candle,
    decimal? BidPrice,
    decimal? AskPrice,
    IReadOnlyList<TerminalComponentQuote> ComponentQuotes);

public sealed record TerminalComponentRow(
    string Name,
    string Epic,
    string Currency,
    decimal Weight,
    decimal FormulaMultiplier,
    decimal DisplayMultiplier,
    decimal? FormulaReferencePrice,
    decimal? LotSize,
    decimal? MinDealSize,
    decimal? MinSizeIncrement,
    string Role,
    decimal AnnualizedVolatilityPct,
    decimal FourYearReturnPct,
    decimal? Bid,
    decimal? Offer,
    DateTimeOffset? QuoteTimestamp,
    string QuoteStatus);

public sealed record SyntheticTerminalPayload(
    string Symbol,
    string DrawingIdentity,
    string Block,
    string CurrencyLabel,
    decimal? BidPrice,
    decimal? AskPrice,
    IReadOnlyList<TerminalCandle> Candles,
    IReadOnlyList<TerminalLinePoint> Ma20,
    IReadOnlyList<TerminalLinePoint> Ma50,
    IReadOnlyList<TerminalLinePoint> Ma200,
    string SelectionBasis,
    IReadOnlyList<TerminalComponentRow> Components);

public static class SyntheticTerminalWorkspace
{
    public const string ModeName = "Terminal";

    public static IReadOnlyList<string> StreamingEpics(SyntheticBasket basket) =>
        basket.Components
            .Select(component => component.Instrument.Epic)
            .Where(epic => !string.IsNullOrWhiteSpace(epic))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(40)
            .ToList();
}
