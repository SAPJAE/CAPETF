namespace CAPETF.Desktop;

public static class SeededSyntheticSelector
{
    public static SyntheticBasket? SelectSeededBasket(
        string seedText,
        string fallbackBlock,
        IReadOnlyList<MarketInstrument> instruments,
        IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>> candles,
        int periodsPerYear)
    {
        if (string.IsNullOrWhiteSpace(seedText)) return null;

        var seed = FindSeed(seedText, instruments, candles);
        if (seed is null || !candles.TryGetValue(seed.Epic, out var seedCandles) || seedCandles.Count < 120) return null;

        var peerPool = instruments
            .Where(CapitalInstrumentTypes.IsStock)
            .Where(item => !string.Equals(item.Epic, seed.Epic, StringComparison.OrdinalIgnoreCase))
            .Where(item => SameCurrency(seed, item))
            .Where(item => candles.TryGetValue(item.Epic, out var rows) && rows.Count >= 120)
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
        var result = SyntheticBasketBuilder.Build(seed.Group, selected, candles, maxBaskets: 1, periodsPerYear: periodsPerYear);
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
        IReadOnlyList<MarketInstrument> instruments,
        IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>> candles)
    {
        var query = seedText.Trim();
        var eligible = instruments
            .Where(CapitalInstrumentTypes.IsStock)
            .Where(item => candles.TryGetValue(item.Epic, out var rows) && rows.Count >= 120)
            .ToList();

        var exact = eligible.FirstOrDefault(item => ExactMatch(item.Epic, query)) ??
                    eligible.FirstOrDefault(item => ExactMatch(item.Symbol, query));
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
        var leftByDate = left.GroupBy(row => row.Time.Date).ToDictionary(group => group.Key, group => group.Last().Close);
        var rightByDate = right.GroupBy(row => row.Time.Date).ToDictionary(group => group.Key, group => group.Last().Close);
        var dates = leftByDate.Keys.Intersect(rightByDate.Keys).OrderBy(date => date).ToList();
        return (dates.Select(date => leftByDate[date]).ToList(), dates.Select(date => rightByDate[date]).ToList());
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
