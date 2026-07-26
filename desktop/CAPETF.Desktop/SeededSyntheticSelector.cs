namespace CAPETF.Desktop;

public static class SeededSyntheticSelector
{
    public static MarketInstrument? ResolveSeed(
        string seedText,
        string preferredBlock,
        IReadOnlyList<MarketInstrument> instruments)
    {
        if (string.IsNullOrWhiteSpace(seedText)) return null;
        return FindSeed(seedText, preferredBlock, instruments, candles: null, requireCandles: false);
    }

    public static SyntheticBasket? SelectSeededBasket(
        string seedText,
        string fallbackBlock,
        IReadOnlyList<MarketInstrument> instruments,
        IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>> candles,
        int periodsPerYear,
        int minimumCandles = 120)
    {
        if (string.IsNullOrWhiteSpace(seedText)) return null;

        var seed = FindSeed(seedText, fallbackBlock, instruments, candles, requireCandles: true, minimumCandles: minimumCandles);
        if (seed is null || !candles.TryGetValue(seed.Epic, out var seedCandles) || seedCandles.Count < minimumCandles) return null;

        var peerPool = instruments
            .Where(CapitalInstrumentTypes.IsStock)
            .Where(item => !string.Equals(item.Epic, seed.Epic, StringComparison.OrdinalIgnoreCase))
            .Where(item => SameCurrency(seed, item))
            .Where(item => candles.TryGetValue(item.Epic, out var rows) &&
                           rows.Count >= minimumCandles &&
                           AnnualizedVolatilityPct(rows, periodsPerYear) > 0 &&
                           SharedAlignedPointCount(seedCandles, rows) >= RequiredSharedPointCount(seedCandles, rows, minimumCandles))
            .ToList();

        if (IsNikeSeed(seed))
        {
            var nikeLikePeers = peerPool.Where(item => IsRetailApparelPeer(item.Name, item.Symbol, item.Epic)).ToList();
            if (nikeLikePeers.Count >= 2)
            {
                peerPool = nikeLikePeers;
            }
        }

        var peers = peerPool
            .Select(item => new
            {
                Instrument = item,
                Score = PeerScore(seed, seedCandles, item, candles[item.Epic], periodsPerYear),
            })
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Instrument.Name, StringComparer.OrdinalIgnoreCase)
            .Take(2)
            .Select(item => item.Instrument)
            .ToList();

        if (peers.Count < 2) return null;

        var selected = new[] { seed }.Concat(peers).ToList();
        var result = SyntheticBasketBuilder.Build(seed.Group, selected, candles, maxBaskets: 1, periodsPerYear: periodsPerYear, minimumCandles: minimumCandles);
        var basket = result.Baskets.FirstOrDefault();
        if (basket is null) return null;

        return new SyntheticBasket
        {
            Symbol = $"SYN-{NormalizeSeedSymbol(seed)}-01",
            Block = seed.Group,
            AverageVolatilityPct = basket.AverageVolatilityPct,
            SimilarityScore = basket.SimilarityScore,
            BasketPrice = basket.BasketPrice,
            LastUpdated = basket.LastUpdated,
        }.CopyFrom(basket);
    }

    private static SyntheticBasket CopyFrom(this SyntheticBasket target, SyntheticBasket source)
    {
        target.BidPrice = source.BidPrice;
        target.AskPrice = source.AskPrice;
        target.LastPrice = source.LastPrice;
        foreach (var component in source.Components)
        {
            target.Components.Add(component);
        }

        foreach (var candle in source.Candles)
        {
            target.Candles.Add(candle);
        }

        return target;
    }

    private static MarketInstrument? FindSeed(
        string seedText,
        string preferredBlock,
        IReadOnlyList<MarketInstrument> instruments,
        IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>>? candles,
        bool requireCandles,
        int minimumCandles = 120)
    {
        var query = seedText.Trim();
        var eligible = instruments
            .Where(CapitalInstrumentTypes.IsStock)
            .Where(item => !requireCandles || (candles is not null && candles.TryGetValue(item.Epic, out var rows) && rows.Count >= minimumCandles))
            .ToList();

        var preferred = eligible
            .Where(item => string.Equals(item.Group, preferredBlock, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return MatchSeed(preferred, query) ?? MatchSeed(eligible, query);
    }

    private static MarketInstrument? MatchSeed(IReadOnlyList<MarketInstrument> eligible, string query)
    {
        var exact = eligible.FirstOrDefault(item => ExactMatch(item.Epic, query)) ??
                    eligible.FirstOrDefault(item => ExactMatch(item.Symbol, query)) ??
                    eligible.FirstOrDefault(item => ExactMatch(item.Name, query)) ??
                    eligible.FirstOrDefault(item => CapitalSymbolAliasMatch(item.Symbol, query));
        if (exact is not null || IsTickerLikeQuery(query)) return exact;

        return eligible
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(item => ContainsMatch(item.Name, query));
    }

    private static decimal PeerScore(
        MarketInstrument seed,
        IReadOnlyList<OhlcPoint> seedCandles,
        MarketInstrument peer,
        IReadOnlyList<OhlcPoint> peerCandles,
        int periodsPerYear)
    {
        var seedVol = AnnualizedVolatilityPct(seedCandles, periodsPerYear);
        var peerVol = AnnualizedVolatilityPct(peerCandles, periodsPerYear);
        var volatility = RelativeCloseness(seedVol, peerVol, 1m);
        var path = NormalizedPathSimilarity(seedCandles, peerCandles);
        var drawdown = RelativeCloseness(CurrentDrawdownPct(seedCandles), CurrentDrawdownPct(peerCandles), 5m);
        var sameRegion = string.Equals(seed.Region, peer.Region, StringComparison.OrdinalIgnoreCase) ? 1m : 0m;
        var sameSector = !string.IsNullOrWhiteSpace(seed.Sector) &&
                         !string.Equals(seed.Sector, "All", StringComparison.OrdinalIgnoreCase) &&
                         string.Equals(seed.Sector, peer.Sector, StringComparison.OrdinalIgnoreCase)
            ? 1m
            : 0m;
        var retailName = RetailApparelNameScore(peer.Name, peer.Symbol, peer.Epic);

        return 35m * volatility + 30m * path + 15m * drawdown + 10m * sameRegion + 5m * sameSector + 5m * retailName;
    }

    private static bool IsNikeSeed(MarketInstrument seed) =>
        ExactMatch(seed.Epic, "NKE") ||
        ExactMatch(seed.Symbol, "NKE") ||
        seed.Name.Contains("Nike", StringComparison.OrdinalIgnoreCase);

    private static decimal RetailApparelNameScore(string name, string symbol, string epic) =>
        IsRetailApparelPeer(name, symbol, epic) ? 1m : 0m;

    private static bool IsRetailApparelPeer(string name, string symbol, string epic)
    {
        if (ExactMatch(symbol, "ONON") || ExactMatch(epic, "ONON")) return true;
        var text = $"{name} {symbol} {epic}".ToLowerInvariant();
        return new[] { "lululemon", "deckers", "under armour", "skechers", "foot locker", "adidas", "puma", "crocs", "capri", "columbia sportswear", "ralph lauren", "abercrombie", "american eagle", "coach" }
            .Any(text.Contains);
    }

    private static bool SameCurrency(MarketInstrument seed, MarketInstrument peer) =>
        string.Equals(seed.Currency, peer.Currency, StringComparison.OrdinalIgnoreCase);

    private static bool ExactMatch(string value, string query) =>
        string.Equals(value?.Trim(), query, StringComparison.OrdinalIgnoreCase);

    private static bool ContainsMatch(string value, string query) =>
        !string.IsNullOrWhiteSpace(value) && value.Contains(query, StringComparison.OrdinalIgnoreCase);

    private static bool CapitalSymbolAliasMatch(string value, string query)
    {
        var symbol = value?.Trim();
        var seed = query.Trim();
        if (string.IsNullOrWhiteSpace(symbol) || !IsTickerLikeQuery(seed)) return false;
        if (!symbol.StartsWith(seed, StringComparison.OrdinalIgnoreCase)) return false;

        var suffix = symbol[seed.Length..];
        return suffix.Length is > 0 and <= 2 && suffix.All(char.IsLower);
    }

    private static bool IsTickerLikeQuery(string query) =>
        query.Trim().Length <= 5 &&
        query.All(character => char.IsLetterOrDigit(character) || character is '.' or '-');

    private static decimal AnnualizedVolatilityPct(IReadOnlyList<OhlcPoint> candles, int periodsPerYear)
    {
        var returns = candles.Zip(candles.Skip(1), (previous, current) => previous.Close <= 0 ? 0m : (current.Close / previous.Close) - 1m).ToList();
        if (returns.Count < 2) return 0m;
        var average = returns.Average();
        var variance = returns.Select(value => Math.Pow((double)(value - average), 2)).Average();
        return decimal.Round((decimal)Math.Sqrt(variance) * (decimal)Math.Sqrt(periodsPerYear) * 100m, 4);
    }

    private static decimal CurrentDrawdownPct(IReadOnlyList<OhlcPoint> candles)
    {
        var peak = candles.Max(candle => candle.Close);
        return peak <= 0 ? 0m : (peak - candles[^1].Close) / peak * 100m;
    }

    private static decimal NormalizedPathSimilarity(IReadOnlyList<OhlcPoint> left, IReadOnlyList<OhlcPoint> right)
    {
        var aligned = AlignCloses(left, right);
        if (aligned.Left.Count < 2 || aligned.Right.Count != aligned.Left.Count || aligned.Left[0] <= 0 || aligned.Right[0] <= 0) return 0m;
        var squaredErrors = aligned.Left.Zip(aligned.Right, (leftPrice, rightPrice) =>
        {
            if (leftPrice <= 0 || rightPrice <= 0) return 1d;
            var leftNormalized = Math.Log((double)(leftPrice / aligned.Left[0]));
            var rightNormalized = Math.Log((double)(rightPrice / aligned.Right[0]));
            return Math.Pow(leftNormalized - rightNormalized, 2);
        });
        var rootMeanSquareError = Math.Sqrt(squaredErrors.Average());
        return (decimal)Math.Exp(-4d * rootMeanSquareError);
    }

    private static (IReadOnlyList<decimal> Left, IReadOnlyList<decimal> Right) AlignCloses(
        IReadOnlyList<OhlcPoint> left,
        IReadOnlyList<OhlcPoint> right)
    {
        if (UsesIntradayAlignment(left, right))
        {
            var leftByTime = left.GroupBy(row => row.Time).ToDictionary(group => group.Key, group => group.Last().Close);
            var rightByTime = right.GroupBy(row => row.Time).ToDictionary(group => group.Key, group => group.Last().Close);
            var times = leftByTime.Keys.Intersect(rightByTime.Keys).OrderBy(time => time).ToList();
            return (times.Select(time => leftByTime[time]).ToList(), times.Select(time => rightByTime[time]).ToList());
        }

        if (UsesWeeklyAlignment(left, right))
        {
            var leftByWeek = left.GroupBy(row => WeekStart(row.Time.Date)).ToDictionary(group => group.Key, group => group.Last().Close);
            var rightByWeek = right.GroupBy(row => WeekStart(row.Time.Date)).ToDictionary(group => group.Key, group => group.Last().Close);
            var weeks = leftByWeek.Keys.Intersect(rightByWeek.Keys).OrderBy(week => week).ToList();
            return (weeks.Select(week => leftByWeek[week]).ToList(), weeks.Select(week => rightByWeek[week]).ToList());
        }

        var leftByDate = left.GroupBy(row => row.Time.Date).ToDictionary(group => group.Key, group => group.Last().Close);
        var rightByDate = right.GroupBy(row => row.Time.Date).ToDictionary(group => group.Key, group => group.Last().Close);
        var dates = leftByDate.Keys.Intersect(rightByDate.Keys).OrderBy(date => date).ToList();
        return (dates.Select(date => leftByDate[date]).ToList(), dates.Select(date => rightByDate[date]).ToList());
    }

    private static int SharedAlignedPointCount(IReadOnlyList<OhlcPoint> left, IReadOnlyList<OhlcPoint> right)
    {
        if (UsesIntradayAlignment(left, right))
        {
            var leftTimes = left.Select(row => row.Time).ToHashSet();
            leftTimes.IntersectWith(right.Select(row => row.Time));
            return leftTimes.Count;
        }

        if (UsesWeeklyAlignment(left, right))
        {
            var leftWeeks = left.Select(row => WeekStart(row.Time.Date)).ToHashSet();
            leftWeeks.IntersectWith(right.Select(row => WeekStart(row.Time.Date)));
            return leftWeeks.Count;
        }

        var leftDates = left.Select(row => row.Time.Date).ToHashSet();
        leftDates.IntersectWith(right.Select(row => row.Time.Date));
        return leftDates.Count;
    }

    private static int RequiredSharedPointCount(
        IReadOnlyList<OhlcPoint> left,
        IReadOnlyList<OhlcPoint> right,
        int minimumCandles)
    {
        var available = Math.Min(minimumCandles, Math.Min(AlignedPointCount(left, right), AlignedPointCount(right, left)));
        return Math.Max(2, (int)Math.Ceiling(available * 0.8m));
    }

    private static int AlignedPointCount(IReadOnlyList<OhlcPoint> source, IReadOnlyList<OhlcPoint> comparison)
    {
        if (UsesIntradayAlignment(source, comparison))
        {
            return source.Select(row => row.Time).Distinct().Count();
        }

        if (UsesWeeklyAlignment(source, comparison))
        {
            return source.Select(row => WeekStart(row.Time.Date)).Distinct().Count();
        }

        return source.Select(row => row.Time.Date).Distinct().Count();
    }

    private static bool UsesIntradayAlignment(IReadOnlyList<OhlcPoint> left, IReadOnlyList<OhlcPoint> right) =>
        HasSubDailyCadence(left) || HasSubDailyCadence(right);

    private static bool UsesWeeklyAlignment(IReadOnlyList<OhlcPoint> left, IReadOnlyList<OhlcPoint> right) =>
        HasWeeklyCadence(left) || HasWeeklyCadence(right);

    private static bool HasSubDailyCadence(IReadOnlyList<OhlcPoint> candles)
    {
        var ordered = candles.OrderBy(row => row.Time).ToList();
        if (ordered.GroupBy(row => row.Time.Date).Any(group => group.Select(row => row.Time.TimeOfDay).Distinct().Count() > 1)) return true;

        for (var index = 1; index < ordered.Count; index++)
        {
            var gap = ordered[index].Time - ordered[index - 1].Time;
            if (gap > TimeSpan.Zero && gap < TimeSpan.FromHours(20))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasWeeklyCadence(IReadOnlyList<OhlcPoint> candles)
    {
        var dates = candles.Select(row => row.Time.Date).Distinct().OrderBy(date => date).ToList();
        if (dates.Count < 3) return false;

        var averageGapDays = dates.Zip(dates.Skip(1), (previous, current) => (current - previous).TotalDays).Average();
        return averageGapDays >= 3.5;
    }

    private static DateTime WeekStart(DateTime date)
    {
        var offset = ((int)date.DayOfWeek + 6) % 7;
        return date.Date.AddDays(-offset);
    }

    private static decimal RelativeCloseness(decimal left, decimal right, decimal floor)
    {
        var scale = Math.Max(Math.Max(Math.Abs(left), Math.Abs(right)), floor);
        return Math.Clamp(1m - Math.Abs(left - right) / scale, 0m, 1m);
    }

    private static string NormalizeSeedSymbol(MarketInstrument seed)
    {
        var source = string.IsNullOrWhiteSpace(seed.Symbol) ? seed.Epic : seed.Symbol;
        var chars = source.ToUpperInvariant().Where(char.IsLetterOrDigit).Take(10).ToArray();
        return chars.Length == 0 ? "SEED" : new string(chars);
    }
}
