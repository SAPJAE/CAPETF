namespace CAPETF.Desktop;

public static class SyntheticBasketBuilder
{
    public static SyntheticBuildResult Build(
        string block,
        IReadOnlyList<MarketInstrument> instruments,
        IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>> candles,
        int maxBaskets = 12,
        int periodsPerYear = 52,
        int minimumCandles = 120)
    {
        if (periodsPerYear <= 0) throw new ArgumentOutOfRangeException(nameof(periodsPerYear));
        if (minimumCandles < 2) throw new ArgumentOutOfRangeException(nameof(minimumCandles));

        var candidates = instruments
            .Where(CapitalInstrumentTypes.IsStock)
            .Where(item => !string.IsNullOrWhiteSpace(item.Epic))
            .Where(item => candles.TryGetValue(item.Epic, out var rows) && rows.Count >= minimumCandles)
            .Select(item =>
            {
                var history = candles[item.Epic].OrderBy(row => row.Time).ToList();
                return new Candidate(
                    item,
                    history,
                    AnnualizedVolatilityPct(history, periodsPerYear),
                    FourYearReturnPct(history),
                    TrailingReturnsPct(history, periodsPerYear),
                    MaximumDrawdownPct(history),
                    CurrentDrawdownPct(history));
            })
            .Where(item => item.VolatilityPct > 0)
            .OrderBy(item => item.Instrument.Name)
            .ThenBy(item => item.Instrument.Epic)
            .ToList();

        if (candidates.Count < 3)
        {
            return new SyntheticBuildResult([], "Not enough stocks with stable price history for this block.");
        }

        var baskets = new List<SyntheticBasket>();
        foreach (var currencyGroup in candidates
                     .GroupBy(item => EffectiveCurrency(item.Instrument), StringComparer.OrdinalIgnoreCase)
                     .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase))
        {
            var remaining = currencyGroup.ToList();
            while (remaining.Count >= 3 && baskets.Count < maxBaskets)
            {
                var clusterSize = PreferredClusterSize(remaining.Count);
                var cluster = SelectMostSimilarCluster(remaining, clusterSize);
                var weights = CalculateEqualWeights(cluster.Count);
                var multipliers = CalculatePriceStabilizedMultipliers(cluster, weights);
                var syntheticCandles = BuildCandles(cluster, multipliers).ToList();
                var basket = new SyntheticBasket
                {
                    Symbol = $"SYN-{NormalizeSymbol(block)}-{baskets.Count + 1:00}",
                    Block = block,
                    AverageVolatilityPct = decimal.Round(cluster.Average(item => item.VolatilityPct), 2),
                    SimilarityScore = decimal.Round(ClusterSimilarity(cluster), 2),
                    BasketPrice = syntheticCandles.Count == 0 ? 0 : syntheticCandles[^1].Close,
                    LastUpdated = syntheticCandles.Count == 0 ? null : syntheticCandles[^1].Time,
                };

                for (var index = 0; index < cluster.Count; index++)
                {
                    basket.Components.Add(new SyntheticComponent(
                        cluster[index].Instrument,
                        weights[index],
                        cluster[index].VolatilityPct,
                        cluster[index].FourYearReturnPct)
                    {
                        FormulaMultiplier = multipliers[index],
                        FormulaReferencePrice = cluster[index].Candles[^1].Close,
                        SyntheticBaselinePrice = cluster[index].Candles[^1].Close,
                    });
                }

                foreach (var candle in syntheticCandles)
                {
                    basket.Candles.Add(candle);
                }

                SyntheticQuoteCalculator.Refresh(basket);
                if (basket.Candles.Count >= 2) baskets.Add(basket);
                foreach (var candidate in cluster) remaining.Remove(candidate);
            }
        }

        return new SyntheticBuildResult(baskets, baskets.Count == 0 ? "No synthetic baskets could be formed." : $"{baskets.Count} synthetic baskets built.");
    }

    public static IReadOnlyList<decimal> CalculateInverseVolatilityWeights(IReadOnlyList<decimal> volatilities)
    {
        if (volatilities.Count == 0) return [];
        var raw = volatilities.Select(value => value <= 0 ? 0m : 1m / value).ToList();
        var sum = raw.Sum();
        var weights = sum == 0 ? Enumerable.Repeat(100m / volatilities.Count, volatilities.Count).ToList() : raw.Select(value => value / sum * 100m).ToList();
        return ApplyWeightBounds(weights, 10m, 45m);
    }

    public static IReadOnlyList<decimal> CalculateEqualWeights(int count)
    {
        if (count <= 0) return [];
        var rounded = decimal.Round(100m / count, 4);
        var weights = Enumerable.Repeat(rounded, count).ToList();
        weights[^1] = decimal.Round(100m - weights.Take(count - 1).Sum(), 4);
        return weights;
    }

    private static IReadOnlyList<decimal> CalculatePriceStabilizedMultipliers(
        IReadOnlyList<Candidate> cluster,
        IReadOnlyList<decimal> weights)
    {
        return cluster.Select((item, index) =>
        {
            var referencePrice = item.Candles[^1].Close;
            if (referencePrice <= 0) return 0m;
            return decimal.Round(weights[index] / referencePrice, 8);
        }).ToList();
    }

    private static IReadOnlyList<decimal> ApplyWeightBounds(IReadOnlyList<decimal> source, decimal minimum, decimal maximum)
    {
        var weights = source.Select(value => Math.Clamp(value, minimum, maximum)).ToList();
        for (var iteration = 0; iteration < 12; iteration++)
        {
            var diff = 100m - weights.Sum();
            if (Math.Abs(diff) < 0.0001m) break;
            var adjustable = weights.Select((value, index) => new { value, index })
                .Where(item => diff > 0 ? item.value < maximum : item.value > minimum)
                .ToList();
            if (adjustable.Count == 0) break;
            var step = diff / adjustable.Count;
            foreach (var item in adjustable)
            {
                weights[item.index] = Math.Clamp(weights[item.index] + step, minimum, maximum);
            }
        }
        return weights.Select(value => decimal.Round(value, 4)).ToList();
    }

    private static IEnumerable<OhlcPoint> BuildCandles(IReadOnlyList<Candidate> cluster, IReadOnlyList<decimal> multipliers)
    {
        var candlesByTime = cluster
            .Select(item => item.Candles
                .GroupBy(candle => candle.Time)
                .ToDictionary(group => group.Key, group => group.Last()))
            .ToList();
        var times = candlesByTime
            .Skip(1)
            .Aggregate(
                candlesByTime[0].Keys.ToHashSet(),
                (shared, rows) =>
                {
                    shared.IntersectWith(rows.Keys);
                    return shared;
                })
            .OrderBy(time => time)
            .ToList();

        foreach (var timeKey in times)
        {
            decimal open = 0, high = 0, low = 0, close = 0;
            DateTimeOffset time = default;
            for (var index = 0; index < cluster.Count; index++)
            {
                var candle = candlesByTime[index][timeKey];
                var multiplier = multipliers[index];
                open += candle.Open * multiplier;
                high += candle.High * multiplier;
                low += candle.Low * multiplier;
                close += candle.Close * multiplier;
                time = candle.Time;
            }
            yield return new OhlcPoint(time, decimal.Round(open, 6), decimal.Round(high, 6), decimal.Round(low, 6), decimal.Round(close, 6));
        }
    }

    private static decimal AnnualizedVolatilityPct(IReadOnlyList<OhlcPoint> candles, int periodsPerYear)
    {
        var returns = candles.Zip(candles.Skip(1), (previous, current) => previous.Close <= 0 ? 0m : (current.Close / previous.Close) - 1m).ToList();
        if (returns.Count < 2) return 0m;
        var average = returns.Average();
        var variance = returns.Select(value => Math.Pow((double)(value - average), 2)).Average();
        return decimal.Round((decimal)Math.Sqrt(variance) * (decimal)Math.Sqrt(periodsPerYear) * 100m, 4);
    }

    private static decimal FourYearReturnPct(IReadOnlyList<OhlcPoint> candles)
    {
        if (candles.Count < 2 || candles[0].Close <= 0) return 0m;
        return decimal.Round((candles[^1].Close / candles[0].Close - 1m) * 100m, 2);
    }

    private static int PreferredClusterSize(int candidateCount)
    {
        if (candidateCount == 3) return 3;
        if (candidateCount == 5) return 4;
        return candidateCount % 4 is 1 or 2 ? 3 : 4;
    }

    private static List<Candidate> SelectMostSimilarCluster(IReadOnlyList<Candidate> candidates, int clusterSize)
    {
        var pairScores = new decimal[candidates.Count, candidates.Count];
        for (var left = 0; left < candidates.Count; left++)
        {
            for (var right = left + 1; right < candidates.Count; right++)
            {
                pairScores[left, right] = PairSimilarity(candidates[left], candidates[right]);
                pairScores[right, left] = pairScores[left, right];
            }
        }

        int[]? best = null;
        var bestScore = decimal.MinValue;
        foreach (var combination in CombinationIndices(candidates.Count, clusterSize))
        {
            var score = CombinationSimilarity(combination, pairScores);
            if (score > bestScore)
            {
                best = combination;
                bestScore = score;
            }
        }

        return (best ?? throw new InvalidOperationException("No eligible synthetic cluster found."))
            .Select(index => candidates[index])
            .ToList();
    }

    private static IEnumerable<int[]> CombinationIndices(int count, int size)
    {
        var indices = Enumerable.Range(0, size).ToArray();
        while (true)
        {
            yield return indices.ToArray();
            var cursor = size - 1;
            while (cursor >= 0 && indices[cursor] == count - size + cursor) cursor--;
            if (cursor < 0) yield break;
            indices[cursor]++;
            for (var index = cursor + 1; index < size; index++) indices[index] = indices[index - 1] + 1;
        }
    }

    private static decimal CombinationSimilarity(IReadOnlyList<int> combination, decimal[,] pairScores)
    {
        decimal total = 0;
        var pairCount = 0;
        for (var left = 0; left < combination.Count; left++)
        {
            for (var right = left + 1; right < combination.Count; right++)
            {
                total += pairScores[combination[left], combination[right]];
                pairCount++;
            }
        }
        return pairCount == 0 ? 0 : total / pairCount;
    }

    private static decimal ClusterSimilarity(IReadOnlyList<Candidate> cluster)
    {
        decimal total = 0;
        var pairCount = 0;
        for (var left = 0; left < cluster.Count; left++)
        {
            for (var right = left + 1; right < cluster.Count; right++)
            {
                total += PairSimilarity(cluster[left], cluster[right]);
                pairCount++;
            }
        }
        return pairCount == 0 ? 0 : total / pairCount;
    }

    private static decimal PairSimilarity(Candidate left, Candidate right)
    {
        var aligned = AlignCloses(left.Candles, right.Candles);
        var correlation = ReturnCorrelation(aligned.Left, aligned.Right);
        var shape = NormalizedPathSimilarity(aligned.Left, aligned.Right);
        var volatility = RelativeCloseness(left.VolatilityPct, right.VolatilityPct, 1m);
        var periodReturns = VectorCloseness(left.TrailingReturnsPct, right.TrailingReturnsPct, 10m);
        var maximumDrawdown = RelativeCloseness(left.MaximumDrawdownPct, right.MaximumDrawdownPct, 5m);
        var currentDrawdown = RelativeCloseness(left.CurrentDrawdownPct, right.CurrentDrawdownPct, 5m);

        return 40m * volatility
            + 25m * correlation
            + 15m * shape
            + 10m * periodReturns
            + 6m * maximumDrawdown
            + 4m * currentDrawdown;
    }

    private static (IReadOnlyList<decimal> Left, IReadOnlyList<decimal> Right) AlignCloses(
        IReadOnlyList<OhlcPoint> left,
        IReadOnlyList<OhlcPoint> right)
    {
        var leftByDate = left.GroupBy(row => row.Time.Date).ToDictionary(group => group.Key, group => group.Last().Close);
        var rightByDate = right.GroupBy(row => row.Time.Date).ToDictionary(group => group.Key, group => group.Last().Close);
        var dates = leftByDate.Keys.Intersect(rightByDate.Keys).OrderBy(date => date).ToList();
        return (dates.Select(date => leftByDate[date]).ToList(), dates.Select(date => rightByDate[date]).ToList());
    }

    private static decimal ReturnCorrelation(IReadOnlyList<decimal> left, IReadOnlyList<decimal> right)
    {
        if (left.Count < 3 || right.Count != left.Count) return 0m;
        var leftReturns = left.Zip(left.Skip(1), (previous, current) => previous <= 0 ? 0d : (double)(current / previous - 1m)).ToArray();
        var rightReturns = right.Zip(right.Skip(1), (previous, current) => previous <= 0 ? 0d : (double)(current / previous - 1m)).ToArray();
        var leftMean = leftReturns.Average();
        var rightMean = rightReturns.Average();
        var numerator = leftReturns.Zip(rightReturns, (a, b) => (a - leftMean) * (b - rightMean)).Sum();
        var leftScale = Math.Sqrt(leftReturns.Sum(value => Math.Pow(value - leftMean, 2)));
        var rightScale = Math.Sqrt(rightReturns.Sum(value => Math.Pow(value - rightMean, 2)));
        if (leftScale == 0 || rightScale == 0) return 0m;
        return Math.Clamp((decimal)(numerator / (leftScale * rightScale)), 0m, 1m);
    }

    private static decimal NormalizedPathSimilarity(IReadOnlyList<decimal> left, IReadOnlyList<decimal> right)
    {
        if (left.Count < 2 || right.Count != left.Count || left[0] <= 0 || right[0] <= 0) return 0m;
        var squaredErrors = left.Zip(right, (leftPrice, rightPrice) =>
        {
            if (leftPrice <= 0 || rightPrice <= 0) return 1d;
            var leftNormalized = Math.Log((double)(leftPrice / left[0]));
            var rightNormalized = Math.Log((double)(rightPrice / right[0]));
            return Math.Pow(leftNormalized - rightNormalized, 2);
        });
        var rootMeanSquareError = Math.Sqrt(squaredErrors.Average());
        return (decimal)Math.Exp(-4d * rootMeanSquareError);
    }

    internal static IReadOnlyList<decimal> TrailingReturnsPct(
        IReadOnlyList<OhlcPoint> candles,
        int periodsPerYear)
    {
        if (periodsPerYear <= 0) throw new ArgumentOutOfRangeException(nameof(periodsPerYear));
        if (candles.Count <= periodsPerYear || candles[^1].Close <= 0) return [];

        var finalIndex = candles.Count - 1;
        var horizons = new[] { periodsPerYear, periodsPerYear / 2, periodsPerYear / 4 };
        return horizons.Select(periods =>
        {
            var baseline = candles[finalIndex - Math.Max(1, periods)].Close;
            return baseline <= 0 ? 0m : (candles[finalIndex].Close / baseline - 1m) * 100m;
        }).ToList();
    }

    private static decimal MaximumDrawdownPct(IReadOnlyList<OhlcPoint> candles)
    {
        decimal peak = 0;
        decimal maximum = 0;
        foreach (var candle in candles)
        {
            if (candle.Close > peak) peak = candle.Close;
            if (peak > 0) maximum = Math.Max(maximum, (peak - candle.Close) / peak * 100m);
        }
        return maximum;
    }

    private static decimal CurrentDrawdownPct(IReadOnlyList<OhlcPoint> candles)
    {
        if (candles.Count == 0) return 0m;
        var peak = candles.Max(candle => candle.Close);
        return peak <= 0 ? 0m : (peak - candles[^1].Close) / peak * 100m;
    }

    private static decimal VectorCloseness(IReadOnlyList<decimal> left, IReadOnlyList<decimal> right, decimal floor)
    {
        if (left.Count == 0 || right.Count != left.Count) return 0m;
        return left.Zip(right, (a, b) => RelativeCloseness(a, b, floor)).Average();
    }

    private static decimal RelativeCloseness(decimal left, decimal right, decimal floor)
    {
        var scale = Math.Max(Math.Max(Math.Abs(left), Math.Abs(right)), floor);
        return Math.Clamp(1m - Math.Abs(left - right) / scale, 0m, 1m);
    }

    private static string NormalizeSymbol(string block)
    {
        var chars = block.ToUpperInvariant().Where(char.IsLetterOrDigit).Take(14).ToArray();
        return chars.Length == 0 ? "BLOCK" : new string(chars);
    }

    private static string EffectiveCurrency(MarketInstrument instrument) =>
        string.IsNullOrWhiteSpace(instrument.Currency) ? "__CAPITAL_CURRENCY_UNSPECIFIED__" : instrument.Currency.Trim();

    private sealed record Candidate(
        MarketInstrument Instrument,
        IReadOnlyList<OhlcPoint> Candles,
        decimal VolatilityPct,
        decimal FourYearReturnPct,
        IReadOnlyList<decimal> TrailingReturnsPct,
        decimal MaximumDrawdownPct,
        decimal CurrentDrawdownPct);
}
