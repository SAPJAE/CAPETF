namespace CAPETF.Desktop;

public static class SyntheticTerminalBuildPolicy
{
    public static bool ShouldUseGenericHistoryFallback(
        SyntheticStrategyKind strategy,
        string? seedText,
        int usableCachedCandles,
        int genericSelectionCandidateCount)
    {
        var isSeededSimilarBuild =
            strategy == SyntheticStrategyKind.SimilarToSelectedSymbol &&
            !string.IsNullOrWhiteSpace(seedText);

        if (isSeededSimilarBuild) return false;

        return usableCachedCandles == 0 || genericSelectionCandidateCount < 3;
    }

    public static async Task<IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>>> LoadCandidateHistoryFallbackAsync(
        SyntheticStrategyKind strategy,
        string? seedText,
        IReadOnlyList<MarketInstrument> candidates,
        IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>> cachedCandles,
        int maximumCandidates,
        Func<IReadOnlyList<MarketInstrument>, Task<HistoryLoadResult>> loadHistory)
    {
        if (maximumCandidates <= 0) throw new ArgumentOutOfRangeException(nameof(maximumCandidates));
        var usableCandidateCount = candidates.Count(candidate => cachedCandles.ContainsKey(candidate.Epic));
        if (!ShouldUseGenericHistoryFallback(strategy, seedText, cachedCandles.Count, usableCandidateCount))
        {
            return cachedCandles;
        }

        var selected = candidates.Take(maximumCandidates).ToList();
        if (selected.Count == 0) return cachedCandles;
        var loaded = await loadHistory(selected);
        var merged = new Dictionary<string, IReadOnlyList<OhlcPoint>>(cachedCandles, StringComparer.OrdinalIgnoreCase);
        foreach (var (epic, rows) in loaded.CandlesByEpic)
        {
            if (rows.Count > 0) merged[epic] = rows;
        }
        return merged;
    }
}
