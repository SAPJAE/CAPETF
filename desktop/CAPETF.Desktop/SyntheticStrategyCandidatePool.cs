namespace CAPETF.Desktop;

public static class SyntheticStrategyCandidatePool
{
    public const int MaximumCandidates = 8;

    public static IReadOnlyList<MarketInstrument> Select(
        SyntheticStrategyKind strategy,
        IReadOnlyList<MarketInstrument> candidates,
        IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>> primaryCandles,
        int primaryPeriodsPerYear,
        IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>> fallbackCandles,
        int fallbackPeriodsPerYear)
    {
        var ranked = SyntheticStrategyRanker.RankWithFallback(
            strategy,
            candidates,
            primaryCandles,
            primaryPeriodsPerYear,
            fallbackCandles,
            fallbackPeriodsPerYear,
            MaximumCandidates);
        return ranked.Count >= 3
            ? ranked.Select(rank => rank.Instrument).ToList()
            : [];
    }
}
