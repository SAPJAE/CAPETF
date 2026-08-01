namespace CAPETF.Desktop;

public sealed record TerminalUniverseControlState(
    IReadOnlyList<string> Blocks,
    string SelectedBlock,
    IReadOnlyList<string> SeedOptions);

public sealed class TerminalUniverseUiCoordinator
{
    private readonly Dictionary<TerminalUniverseKind, IReadOnlyList<MarketInstrument>> _cachedInstruments = [];

    public void EnsureEtfCatalogFor(TerminalUniverseKind universe, Action ensureEtfCatalog)
    {
        ArgumentNullException.ThrowIfNull(ensureEtfCatalog);
        if (universe is TerminalUniverseKind.Stocks or TerminalUniverseKind.ETFs)
        {
            ensureEtfCatalog();
        }
    }

    public void Cache(TerminalUniverseKind universe, IReadOnlyList<MarketInstrument> instruments)
    {
        ArgumentNullException.ThrowIfNull(instruments);
        _cachedInstruments[universe] = instruments;
    }

    public bool TryGetCached(TerminalUniverseKind universe, out IReadOnlyList<MarketInstrument> instruments) =>
        _cachedInstruments.TryGetValue(universe, out instruments!);

    public async Task SwitchAsync(Func<Task> clearAsync, Func<Task> loadAsync)
    {
        ArgumentNullException.ThrowIfNull(clearAsync);
        ArgumentNullException.ThrowIfNull(loadAsync);
        await clearAsync();
        await loadAsync();
    }

    public async Task EnsureActiveAsync(
        TerminalUniverseKind current,
        TerminalUniverseKind target,
        IReadOnlyList<MarketInstrument> currentInstruments,
        IReadOnlySet<string> knownEtfEpics,
        Action<TerminalUniverseKind> select,
        Func<Task> clearAsync,
        Func<TerminalUniverseKind, Task> loadAsync)
    {
        ArgumentNullException.ThrowIfNull(currentInstruments);
        ArgumentNullException.ThrowIfNull(knownEtfEpics);
        ArgumentNullException.ThrowIfNull(select);
        ArgumentNullException.ThrowIfNull(clearAsync);
        ArgumentNullException.ThrowIfNull(loadAsync);

        var changed = current != target;
        var requiresLoad = changed || currentInstruments.Count == 0 ||
            currentInstruments.Any(instrument => !TerminalUniverse.Accepts(target, instrument, knownEtfEpics));
        if (changed) select(target);
        if (requiresLoad)
        {
            await SwitchAsync(clearAsync, () => loadAsync(target));
        }
    }

    public TerminalUniverseControlState BuildControls(IReadOnlyList<MarketInstrument> instruments)
    {
        ArgumentNullException.ThrowIfNull(instruments);
        var blocks = instruments
            .GroupBy(item => item.Group)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Key)
            .ToList();
        var selectedBlock = blocks.FirstOrDefault() ?? "";
        return new TerminalUniverseControlState(
            blocks,
            selectedBlock,
            BuildSeedOptions(instruments, selectedBlock));
    }

    public IReadOnlyList<string> BuildSeedOptions(
        IReadOnlyList<MarketInstrument> instruments,
        string selectedBlock) =>
        SeedSearchOptionBuilder.BuildOptions(instruments, selectedBlock);
}
