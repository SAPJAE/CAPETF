namespace CAPETF.Desktop;

public static class SyntheticTerminalChartPayload
{
    public static SyntheticTerminalPayload Build(SyntheticBasket basket)
    {
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
            basket.Block,
            CurrencyLabel(basket),
            basket.BidPrice,
            basket.AskPrice,
            basket.LastPrice ?? basket.BasketPrice,
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
                component.FormulaReferencePrice,
                index == 0 ? "Anchor" : "Peer",
                component.AnnualizedVolatilityPct,
                component.FourYearReturnPct,
                component.Instrument.Bid,
                component.Instrument.Offer,
                component.DisplayPrice,
                component.Instrument.LastTickAt?.ToLocalTime().ToString("HH:mm:ss") ?? "n/a")).ToList());
    }

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
