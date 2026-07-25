namespace CAPETF.Desktop;

public static class SyntheticTerminalSelector
{
    private const int WeeklyThreeYearCandles = 156;

    public static SyntheticBasket? SelectBest(
        string block,
        IReadOnlyList<MarketInstrument> instruments,
        IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>> candles,
        int periodsPerYear)
    {
        var blockInstruments = instruments
            .Where(item => string.Equals(item.Group, block, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (blockInstruments.Count < 3) return null;

        var terminalCandles = candles.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<OhlcPoint>)LastThreeYears(pair.Value));
        var result = SyntheticBasketBuilder.Build(
            block,
            blockInstruments,
            terminalCandles,
            maxBaskets: 12,
            periodsPerYear: periodsPerYear);

        return result.Baskets
            .Where(basket => basket.Candles.Count >= 2 && basket.Components.Count is >= 3 and <= 4)
            .OrderByDescending(TerminalSelectionScore)
            .ThenByDescending(basket => basket.SimilarityScore)
            .ThenByDescending(basket => basket.Candles.Count)
            .ThenBy(basket => basket.Symbol, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    public static IReadOnlyList<OhlcPoint> LastThreeYears(IReadOnlyList<OhlcPoint> candles) =>
        candles.OrderBy(candle => candle.Time).TakeLast(WeeklyThreeYearCandles).ToList();

    private static decimal TerminalSelectionScore(SyntheticBasket basket)
    {
        if (basket.Components.Count == 0) return 0m;
        var volatilities = basket.Components.Select(component => component.AnnualizedVolatilityPct).ToList();
        var maxVol = volatilities.Max();
        var minVol = volatilities.Min();
        var scale = Math.Max(maxVol, 1m);
        var volatilityPenalty = (maxVol - minVol) / scale * 35m;
        return basket.SimilarityScore - volatilityPenalty;
    }
}
