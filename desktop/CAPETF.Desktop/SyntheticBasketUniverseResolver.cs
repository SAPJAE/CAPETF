namespace CAPETF.Desktop;

public static class SyntheticBasketUniverseResolver
{
    public static TerminalUniverseKind Resolve(
        SavedSyntheticBasket saved,
        IReadOnlySet<string> knownEtfEpics)
    {
        ArgumentNullException.ThrowIfNull(saved);
        ArgumentNullException.ThrowIfNull(knownEtfEpics);
        if (saved.UniverseKind is { } persisted && Enum.IsDefined(persisted))
        {
            return persisted;
        }
        if (saved.Strategy == SyntheticStrategyKind.ManualFormula ||
            saved.Block.TrimStart().StartsWith("Crypto /", StringComparison.OrdinalIgnoreCase))
        {
            return TerminalUniverseKind.Crypto;
        }
        return AreKnownEtfs(saved.Components.Select(component => component.Epic), knownEtfEpics)
            ? TerminalUniverseKind.ETFs
            : TerminalUniverseKind.Stocks;
    }

    public static TerminalUniverseKind Resolve(
        SyntheticExecutionRecord execution,
        IReadOnlySet<string> knownEtfEpics)
    {
        ArgumentNullException.ThrowIfNull(execution);
        ArgumentNullException.ThrowIfNull(knownEtfEpics);
        if (execution.BasketQuantity is > 0m)
        {
            return TerminalUniverseKind.Crypto;
        }
        return AreKnownEtfs(execution.Legs.Select(leg => leg.Epic), knownEtfEpics)
            ? TerminalUniverseKind.ETFs
            : TerminalUniverseKind.Stocks;
    }

    private static bool AreKnownEtfs(IEnumerable<string> epics, IReadOnlySet<string> knownEtfEpics)
    {
        var selected = epics.Where(epic => !string.IsNullOrWhiteSpace(epic)).ToArray();
        return selected.Length > 0 && selected.All(knownEtfEpics.Contains);
    }
}
