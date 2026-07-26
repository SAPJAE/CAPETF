namespace CAPETF.Desktop;

public enum SyntheticStrategyKind
{
    SimilarToSelectedSymbol,
    BelowMa200,
    BelowTwoYearLow,
    NearTwoYearLow,
    AboveAllTimeHigh,
    BreakoutCandidate,
    DipInsideUptrend,
    HighMomentum,
    MeanReversion,
}

public sealed record SyntheticStrategy(SyntheticStrategyKind Kind, string Label);

public sealed record SyntheticStrategyRank(MarketInstrument Instrument, decimal Score, string Reason);

public static class SyntheticStrategyCatalog
{
    public static IReadOnlyList<SyntheticStrategy> All { get; } =
    [
        new(SyntheticStrategyKind.SimilarToSelectedSymbol, "Similar to selected symbol"),
        new(SyntheticStrategyKind.DipInsideUptrend, "Dip inside uptrend"),
        new(SyntheticStrategyKind.BreakoutCandidate, "Breakout candidate"),
        new(SyntheticStrategyKind.BelowMa200, "Below 200 MA"),
        new(SyntheticStrategyKind.BelowTwoYearLow, "Below 2Y low"),
        new(SyntheticStrategyKind.NearTwoYearLow, "Near 2Y low"),
        new(SyntheticStrategyKind.AboveAllTimeHigh, "Above all-time high"),
        new(SyntheticStrategyKind.HighMomentum, "High momentum"),
        new(SyntheticStrategyKind.MeanReversion, "Mean reversion"),
    ];
}

public static class SyntheticStrategyRanker
{
    public static IReadOnlyList<SyntheticStrategyRank> Rank(
        SyntheticStrategyKind strategy,
        IReadOnlyList<MarketInstrument> instruments,
        IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>> candles,
        int periodsPerYear,
        int maximum)
    {
        if (strategy == SyntheticStrategyKind.SimilarToSelectedSymbol) return [];
        return instruments
            .Where(CapitalInstrumentTypes.IsStock)
            .Where(instrument => !string.IsNullOrWhiteSpace(instrument.Epic))
            .Select(instrument => candles.TryGetValue(instrument.Epic, out var rows)
                ? Score(strategy, instrument, rows.OrderBy(row => row.Time).ToList(), periodsPerYear)
                : null)
            .OfType<SyntheticStrategyRank>()
            .Where(rank => rank.Score > 0)
            .OrderByDescending(rank => rank.Score)
            .ThenBy(rank => rank.Instrument.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(rank => rank.Instrument.Epic, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Max(0, maximum))
            .ToList();
    }

    private static SyntheticStrategyRank? Score(
        SyntheticStrategyKind strategy,
        MarketInstrument instrument,
        IReadOnlyList<OhlcPoint> candles,
        int periodsPerYear)
    {
        if (candles.Count < 60) return null;
        var close = candles[^1].Close;
        if (close <= 0) return null;

        return strategy switch
        {
            SyntheticStrategyKind.BelowMa200 => BelowMa200(instrument, candles, close),
            SyntheticStrategyKind.BelowTwoYearLow => BelowTwoYearLow(instrument, candles, close, periodsPerYear),
            SyntheticStrategyKind.NearTwoYearLow => NearTwoYearLow(instrument, candles, close, periodsPerYear),
            SyntheticStrategyKind.AboveAllTimeHigh => AboveAllTimeHigh(instrument, candles, close),
            SyntheticStrategyKind.BreakoutCandidate => BreakoutCandidate(instrument, candles, close, periodsPerYear),
            SyntheticStrategyKind.DipInsideUptrend => DipInsideUptrend(instrument, candles, close),
            SyntheticStrategyKind.HighMomentum => HighMomentum(instrument, candles, close, periodsPerYear),
            SyntheticStrategyKind.MeanReversion => MeanReversion(instrument, candles, close),
            _ => null,
        };
    }

    private static SyntheticStrategyRank? BelowMa200(MarketInstrument instrument, IReadOnlyList<OhlcPoint> candles, decimal close)
    {
        if (candles.Count < 200) return null;
        var ma200 = AverageClose(candles.TakeLast(200));
        if (close >= ma200) return null;
        var discount = (ma200 - close) / ma200 * 100m;
        return new SyntheticStrategyRank(instrument, decimal.Round(discount, 4), $"{discount:0.##}% below MA200");
    }

    private static SyntheticStrategyRank? BelowTwoYearLow(
        MarketInstrument instrument,
        IReadOnlyList<OhlcPoint> candles,
        decimal close,
        int periodsPerYear)
    {
        var lookback = Lookback(candles, periodsPerYear * 2);
        var low = lookback.SkipLast(1).Select(row => row.Close).DefaultIfEmpty(close).Min();
        if (close >= low) return null;
        var discount = (low - close) / low * 100m;
        return new SyntheticStrategyRank(instrument, decimal.Round(100m + discount, 4), $"{discount:0.##}% below 2Y low");
    }

    private static SyntheticStrategyRank? NearTwoYearLow(
        MarketInstrument instrument,
        IReadOnlyList<OhlcPoint> candles,
        decimal close,
        int periodsPerYear)
    {
        var lookback = Lookback(candles, periodsPerYear * 2);
        var low = lookback.Select(row => row.Close).Min();
        if (low <= 0 || close < low) return null;
        var distance = (close - low) / low * 100m;
        if (distance > 12m) return null;
        return new SyntheticStrategyRank(instrument, decimal.Round(100m - distance * 5m, 4), $"{distance:0.##}% above 2Y low");
    }

    private static SyntheticStrategyRank? AboveAllTimeHigh(MarketInstrument instrument, IReadOnlyList<OhlcPoint> candles, decimal close)
    {
        var priorHigh = candles.SkipLast(1).Select(row => row.Close).DefaultIfEmpty(close).Max();
        if (close <= priorHigh) return null;
        var breakout = (close - priorHigh) / priorHigh * 100m;
        return new SyntheticStrategyRank(instrument, decimal.Round(100m + breakout, 4), $"{breakout:0.##}% above prior high");
    }

    private static SyntheticStrategyRank? BreakoutCandidate(
        MarketInstrument instrument,
        IReadOnlyList<OhlcPoint> candles,
        decimal close,
        int periodsPerYear)
    {
        if (candles.Count < 80) return null;
        var lookback = Lookback(candles, Math.Max(20, periodsPerYear));
        var high = lookback.SkipLast(1).Select(row => row.Close).DefaultIfEmpty(close).Max();
        if (close > high) return null;
        var nearHigh = high <= 0 ? 0 : Math.Max(0m, 1m - Math.Abs(high - close) / high);
        var ma50 = candles.Count >= 50 ? AverageClose(candles.TakeLast(50)) : close;
        var priorMa50 = candles.Count >= 70 ? AverageClose(candles.Skip(candles.Count - 70).Take(50)) : ma50;
        var trend = ma50 > priorMa50 ? 1m : 0m;
        var compression = Volatility(candles.TakeLast(20).ToList()) < Volatility(candles.TakeLast(80).ToList()) ? 1m : 0m;
        var score = 70m * nearHigh + 20m * trend + 10m * compression;
        return score < 70m ? null : new SyntheticStrategyRank(instrument, decimal.Round(score, 4), "near high with rising MA");
    }

    private static SyntheticStrategyRank? DipInsideUptrend(MarketInstrument instrument, IReadOnlyList<OhlcPoint> candles, decimal close)
    {
        if (candles.Count < 220) return null;
        var ma200 = AverageClose(candles.TakeLast(200));
        var priorMa200 = AverageClose(candles.Skip(candles.Count - 220).Take(200));
        var peak = candles.TakeLast(60).Select(row => row.Close).Max();
        if (ma200 <= priorMa200 || close >= ma200 || peak <= close) return null;
        var dip = (peak - close) / peak * 100m;
        var trend = (ma200 - priorMa200) / priorMa200 * 100m;
        return new SyntheticStrategyRank(instrument, decimal.Round(dip * 2m + trend, 4), $"uptrend MA200 with {dip:0.##}% dip");
    }

    private static SyntheticStrategyRank? HighMomentum(
        MarketInstrument instrument,
        IReadOnlyList<OhlcPoint> candles,
        decimal close,
        int periodsPerYear)
    {
        if (candles.Count <= periodsPerYear) return null;
        var oneYearAgo = candles[^Math.Min(candles.Count, periodsPerYear + 1)].Close;
        if (oneYearAgo <= 0 || close <= oneYearAgo) return null;
        var returnPct = (close / oneYearAgo - 1m) * 100m;
        return new SyntheticStrategyRank(instrument, decimal.Round(returnPct, 4), $"{returnPct:0.##}% one-year momentum");
    }

    private static SyntheticStrategyRank? MeanReversion(MarketInstrument instrument, IReadOnlyList<OhlcPoint> candles, decimal close)
    {
        if (candles.Count < 200) return null;
        var ma200 = AverageClose(candles.TakeLast(200));
        var ma50 = AverageClose(candles.TakeLast(50));
        if (close >= ma200 || ma50 < ma200 * 0.85m) return null;
        var discount = (ma200 - close) / ma200 * 100m;
        return new SyntheticStrategyRank(instrument, decimal.Round(discount, 4), $"{discount:0.##}% below MA200 with stable MA50");
    }

    private static IReadOnlyList<OhlcPoint> Lookback(IReadOnlyList<OhlcPoint> candles, int desired) =>
        candles.TakeLast(Math.Min(candles.Count, Math.Max(2, desired))).ToList();

    private static decimal AverageClose(IEnumerable<OhlcPoint> candles) => candles.Average(row => row.Close);

    private static decimal Volatility(IReadOnlyList<OhlcPoint> candles)
    {
        var returns = candles.Zip(candles.Skip(1), (previous, current) => previous.Close <= 0 ? 0d : (double)(current.Close / previous.Close - 1m)).ToList();
        if (returns.Count < 2) return 0m;
        var mean = returns.Average();
        return (decimal)Math.Sqrt(returns.Select(value => Math.Pow(value - mean, 2)).Average());
    }
}
