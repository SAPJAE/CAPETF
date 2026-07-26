namespace CAPETF.Desktop;

public static class SyntheticTerminalSelector
{
    private const int WeeklyThreeYearCandles = 156;

    public static SyntheticBasket? SelectBest(
        string block,
        IReadOnlyList<MarketInstrument> instruments,
        IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>> candles,
        int periodsPerYear,
        int minimumCandles = 120)
    {
        var blockInstruments = instruments
            .Where(item => string.Equals(item.Group, block, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (blockInstruments.Count < 3) return null;

        var comparisonCandles = candles.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<OhlcPoint>)LastThreeYears(pair.Value));
        var result = SyntheticBasketBuilder.Build(
            block,
            blockInstruments,
            comparisonCandles,
            maxBaskets: 12,
            periodsPerYear: periodsPerYear,
            minimumCandles: minimumCandles);

        var selected = result.Baskets
            .Where(basket => basket.Candles.Count >= 2 && basket.Components.Count is >= 3 and <= 4)
            .OrderByDescending(TerminalSelectionScore)
            .ThenByDescending(basket => basket.SimilarityScore)
            .ThenByDescending(basket => basket.Candles.Count)
            .ThenBy(basket => basket.Symbol, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
        if (selected is null) return null;

        var selectedInstruments = selected.Components.Select(component => component.Instrument).ToList();
        return SyntheticBasketBuilder.Build(block, selectedInstruments, candles, maxBaskets: 1, periodsPerYear: periodsPerYear, minimumCandles: minimumCandles)
            .Baskets
            .FirstOrDefault() ?? selected;
    }

    public static IReadOnlyList<MarketInstrument> HistoryLoadCandidates(
        string block,
        IReadOnlyList<MarketInstrument> instruments,
        int limit = 160) =>
        instruments
            .Where(item => string.Equals(item.Group, block, StringComparison.OrdinalIgnoreCase))
            .Where(item => !string.IsNullOrWhiteSpace(item.Epic))
            .OrderBy(item => string.IsNullOrWhiteSpace(item.Sector) ? 1 : 0)
            .ThenBy(item => item.Price is null ? 1 : 0)
            .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Epic, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Max(0, limit))
            .ToList();

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
