namespace CAPETF.Desktop;

public sealed record SyntheticTerminalTickResult(bool Matched, bool CandleChanged, SyntheticTerminalTickPayload? Tick);

public static class SyntheticTerminalLiveUpdate
{
    public static SyntheticTerminalTickResult Apply(
        SyntheticBasket basket,
        QuoteUpdate quote,
        DateTimeOffset? now = null)
    {
        var observedAt = now ?? DateTimeOffset.UtcNow;
        var result = SyntheticLiveUpdate.ApplyQuote(basket, quote);
        var candle = result.CandleChanged && basket.Candles.Count > 0
            ? basket.Candles[^1]
            : null;
        return new SyntheticTerminalTickResult(
            result.Matched,
            result.CandleChanged,
            result.Matched
                ? new SyntheticTerminalTickPayload(
                    SyntheticTerminalChartPayload.DrawingIdentity(basket),
                    candle is null ? null : new TerminalCandle(
                        candle.Time.ToUnixTimeSeconds(), candle.Open, candle.High, candle.Low, candle.Close),
                    basket.BidPrice,
                    basket.AskPrice,
                    basket.Components.Select(component => new TerminalComponentQuote(
                        component.Instrument.Epic,
                        component.Instrument.Bid,
                        component.Instrument.Offer,
                        component.Instrument.LastTickAt,
                        SyntheticTerminalChartPayload.QuoteStatus(component.Instrument.LastTickAt, observedAt))).ToList())
                : null);
    }
}
