namespace CAPETF.Desktop;

public sealed record SyntheticTerminalTickResult(bool Matched, bool CandleChanged, SyntheticTerminalPayload? Payload);

public static class SyntheticTerminalLiveUpdate
{
    public static SyntheticTerminalTickResult Apply(SyntheticBasket basket, QuoteUpdate quote)
    {
        var result = SyntheticLiveUpdate.ApplyQuote(basket, quote);
        return new SyntheticTerminalTickResult(
            result.Matched,
            result.CandleChanged,
            result.Matched ? SyntheticTerminalChartPayload.Build(basket) : null);
    }
}
