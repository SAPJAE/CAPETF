namespace CAPETF.Desktop;

public static class SyntheticTerminalChartPayload
{
    private static readonly TimeSpan FreshQuoteAge = TimeSpan.FromMinutes(5);

    public static SyntheticTerminalPayload Build(
        SyntheticBasket basket,
        DateTimeOffset? now = null,
        string? drawingIdentity = null)
    {
        var observedAt = now ?? DateTimeOffset.UtcNow;
        var candles = basket.Candles
            .OrderBy(candle => candle.Time)
            .Select(candle => new TerminalCandle(
                candle.Time.ToUnixTimeSeconds(),
                candle.Open,
                candle.High,
                candle.Low,
                candle.Close))
            .ToList();

        return new SyntheticTerminalPayload(
            basket.Symbol,
            string.IsNullOrWhiteSpace(drawingIdentity) ? DrawingIdentity(basket) : drawingIdentity.Trim(),
            basket.Block,
            CurrencyLabel(basket),
            basket.BidPrice,
            basket.AskPrice,
            candles,
            MovingAverage(basket.Candles, 20),
            MovingAverage(basket.Candles, 50),
            MovingAverage(basket.Candles, 200),
            "Selection uses similar price path, similar volatility, similar drawdown, and same currency. Formula uses price-stabilized equal notional multipliers so one high-priced leg does not dominate the basket.",
            basket.Components.Select((component, index) => new TerminalComponentRow(
                component.Instrument.Name,
                component.Instrument.Epic,
                string.IsNullOrWhiteSpace(component.Instrument.Currency) ? "n/a" : component.Instrument.Currency.Trim(),
                component.Weight,
                component.FormulaMultiplier,
                SyntheticOrderSizing.DisplayMultiplier(component),
                component.FormulaReferencePrice,
                component.Instrument.LotSize,
                component.Instrument.MinDealSize,
                component.Instrument.MinSizeIncrement,
                index == 0 ? "Anchor" : "Peer",
                component.AnnualizedVolatilityPct,
                component.FourYearReturnPct,
                component.Instrument.Bid,
                component.Instrument.Offer,
                component.Instrument.LastTickAt,
                QuoteStatus(component.Instrument.LastTickAt, observedAt))).ToList(),
            1m);
    }

    internal static string DrawingIdentity(SyntheticBasket basket) =>
        string.Join("|", new[] { basket.Symbol }.Concat(basket.Components
            .Select(component => component.Instrument.Epic)
            .Where(epic => !string.IsNullOrWhiteSpace(epic))
            .OrderBy(epic => epic, StringComparer.OrdinalIgnoreCase)));

    internal static string QuoteStatus(DateTimeOffset? quoteTimestamp, DateTimeOffset now)
    {
        if (quoteTimestamp is null) return "stale";
        var age = now.ToUniversalTime() - quoteTimestamp.Value.ToUniversalTime();
        return age >= TimeSpan.Zero && age <= FreshQuoteAge ? "fresh" : "stale";
    }

    internal static string BasketQuoteStatus(SyntheticBasket basket, DateTimeOffset now) =>
        basket.Components.All(component => QuoteStatus(component.Instrument.LastTickAt, now) == "fresh")
            ? "quotes fresh"
            : "stale components";

    private static IReadOnlyList<TerminalLinePoint> MovingAverage(IReadOnlyList<OhlcPoint> source, int period)
    {
        var ordered = source.OrderBy(candle => candle.Time).ToList();
        if (ordered.Count < period) return [];

        var result = new List<TerminalLinePoint>();
        for (var index = period - 1; index < ordered.Count; index++)
        {
            var average = ordered.Skip(index - period + 1).Take(period).Average(candle => candle.Close);
            result.Add(new TerminalLinePoint(ordered[index].Time.ToUnixTimeSeconds(), decimal.Round(average, 6)));
        }
        return result;
    }

    private static string CurrencyLabel(SyntheticBasket basket)
    {
        var known = basket.Components
            .Select(component => component.Instrument.Currency)
            .Where(currency => !string.IsNullOrWhiteSpace(currency))
            .Select(currency => currency.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return known.Count == 1 ? known[0] : "currency unavailable from Capital.com";
    }
}
