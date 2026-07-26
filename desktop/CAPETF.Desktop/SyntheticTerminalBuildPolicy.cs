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
}
