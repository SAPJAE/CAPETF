namespace CAPETF.Desktop;

public sealed record SyntheticBasketUniverseResolution(
    TerminalUniverseKind Universe,
    IReadOnlyList<MarketInstrument> Instruments);

public static class SyntheticBasketUniverseResolver
{
    public static TerminalUniverseKind Resolve(
        SavedSyntheticBasket saved,
        IReadOnlySet<string> knownEtfEpics)
    {
        if (TryResolve(saved, knownEtfEpics, out var universe)) return universe;
        throw LegacyResolutionRequired(saved.Components.Select(component => component.Epic));
    }

    public static TerminalUniverseKind Resolve(
        SyntheticExecutionRecord execution,
        IReadOnlySet<string> knownEtfEpics)
    {
        if (TryResolve(execution, knownEtfEpics, out var universe)) return universe;
        throw LegacyResolutionRequired(execution.Legs.Select(leg => leg.Epic));
    }

    public static bool TryResolve(
        SavedSyntheticBasket saved,
        IReadOnlySet<string> knownEtfEpics,
        out TerminalUniverseKind universe)
    {
        ArgumentNullException.ThrowIfNull(saved);
        ArgumentNullException.ThrowIfNull(knownEtfEpics);
        if (saved.UniverseKind is { } persisted && Enum.IsDefined(persisted))
        {
            universe = persisted;
            return true;
        }
        if (saved.Strategy == SyntheticStrategyKind.ManualFormula ||
            saved.Block.TrimStart().StartsWith("Crypto /", StringComparison.OrdinalIgnoreCase))
        {
            universe = TerminalUniverseKind.Crypto;
            return true;
        }
        if (AreKnownEtfs(saved.Components.Select(component => component.Epic), knownEtfEpics))
        {
            universe = TerminalUniverseKind.ETFs;
            return true;
        }
        universe = default;
        return false;
    }

    public static bool TryResolve(
        SyntheticExecutionRecord execution,
        IReadOnlySet<string> knownEtfEpics,
        out TerminalUniverseKind universe)
    {
        ArgumentNullException.ThrowIfNull(execution);
        ArgumentNullException.ThrowIfNull(knownEtfEpics);
        if (execution.UniverseKind is { } persisted && Enum.IsDefined(persisted))
        {
            universe = persisted;
            return true;
        }
        if (execution.BasketQuantity is > 0m)
        {
            universe = TerminalUniverseKind.Crypto;
            return true;
        }
        if (AreKnownEtfs(execution.Legs.Select(leg => leg.Epic), knownEtfEpics))
        {
            universe = TerminalUniverseKind.ETFs;
            return true;
        }
        universe = default;
        return false;
    }

    public static Task<SyntheticBasketUniverseResolution> ResolveAsync(
        SavedSyntheticBasket saved,
        IReadOnlySet<string> knownEtfEpics,
        IReadOnlyDictionary<TerminalUniverseKind, IReadOnlyList<MarketInstrument>> cachedInstruments,
        Func<string, CancellationToken, Task<MarketInstrument?>> probeInstrument,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(saved);
        return ResolveAsync(
            saved.Components.Select(component => component.Epic).ToArray(),
            TryResolve(saved, knownEtfEpics, out var universe) ? universe : null,
            knownEtfEpics,
            cachedInstruments,
            probeInstrument,
            cancellationToken);
    }

    public static Task<SyntheticBasketUniverseResolution> ResolveAsync(
        SyntheticExecutionRecord execution,
        IReadOnlySet<string> knownEtfEpics,
        IReadOnlyDictionary<TerminalUniverseKind, IReadOnlyList<MarketInstrument>> cachedInstruments,
        Func<string, CancellationToken, Task<MarketInstrument?>> probeInstrument,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(execution);
        return ResolveAsync(
            execution.Legs.Select(leg => leg.Epic).ToArray(),
            TryResolve(execution, knownEtfEpics, out var universe) ? universe : null,
            knownEtfEpics,
            cachedInstruments,
            probeInstrument,
            cancellationToken);
    }

    private static async Task<SyntheticBasketUniverseResolution> ResolveAsync(
        IReadOnlyList<string> epics,
        TerminalUniverseKind? frozenUniverse,
        IReadOnlySet<string> knownEtfEpics,
        IReadOnlyDictionary<TerminalUniverseKind, IReadOnlyList<MarketInstrument>> cachedInstruments,
        Func<string, CancellationToken, Task<MarketInstrument?>> probeInstrument,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(knownEtfEpics);
        ArgumentNullException.ThrowIfNull(cachedInstruments);
        ArgumentNullException.ThrowIfNull(probeInstrument);
        var orderedEpics = epics
            .Where(epic => !string.IsNullOrWhiteSpace(epic))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (orderedEpics.Length != epics.Count)
        {
            throw new InvalidOperationException("Universe resolution requires distinct non-empty basket epics.");
        }

        var cachedMatches = CachedMatches(orderedEpics, cachedInstruments);
        var noProbedInstruments = new Dictionary<string, MarketInstrument>(StringComparer.OrdinalIgnoreCase);
        if (frozenUniverse is { } frozen)
        {
            return new SyntheticBasketUniverseResolution(
                frozen,
                SelectInstruments(orderedEpics, frozen, cachedMatches, noProbedInstruments, knownEtfEpics));
        }

        var cachedCandidates = CandidateSets(orderedEpics, cachedMatches, noProbedInstruments, knownEtfEpics);
        if (TrySingleUniverse(cachedCandidates, out var cachedUniverse))
        {
            return new SyntheticBasketUniverseResolution(
                cachedUniverse,
                SelectInstruments(orderedEpics, cachedUniverse, cachedMatches, noProbedInstruments, knownEtfEpics));
        }

        var probed = new Dictionary<string, MarketInstrument>(StringComparer.OrdinalIgnoreCase);
        foreach (var epic in orderedEpics)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var instrument = await probeInstrument(epic, cancellationToken);
            if (instrument is not null && string.Equals(instrument.Epic?.Trim(), epic, StringComparison.OrdinalIgnoreCase))
            {
                probed[epic] = instrument;
            }
        }

        var candidates = CandidateSets(orderedEpics, cachedMatches, probed, knownEtfEpics);
        var missing = orderedEpics.Where(epic => candidates[epic].Count == 0).ToArray();
        if (missing.Length > 0)
        {
            throw new InvalidOperationException(
                $"Legacy basket is missing universe metadata for: {string.Join(", ", missing)}.");
        }

        var shared = candidates[orderedEpics[0]].ToHashSet();
        foreach (var epic in orderedEpics.Skip(1)) shared.IntersectWith(candidates[epic]);
        if (shared.Count != 1)
        {
            var detail = string.Join(", ", orderedEpics.Select(epic =>
                $"{epic}=[{string.Join("/", candidates[epic].Order())}]"));
            throw new InvalidOperationException($"Legacy basket universe is ambiguous: {detail}.");
        }

        var universe = shared.Single();
        return new SyntheticBasketUniverseResolution(
            universe,
            SelectInstruments(orderedEpics, universe, cachedMatches, probed, knownEtfEpics));
    }

    private static Dictionary<string, List<(TerminalUniverseKind Universe, MarketInstrument Instrument)>> CachedMatches(
        IReadOnlyList<string> epics,
        IReadOnlyDictionary<TerminalUniverseKind, IReadOnlyList<MarketInstrument>> cachedInstruments)
    {
        var matches = epics.ToDictionary(
            epic => epic,
            _ => new List<(TerminalUniverseKind, MarketInstrument)>(),
            StringComparer.OrdinalIgnoreCase);
        foreach (var (universe, instruments) in cachedInstruments.OrderBy(pair => pair.Key))
        {
            foreach (var instrument in instruments)
            {
                if (matches.TryGetValue(instrument.Epic?.Trim() ?? "", out var rows))
                {
                    rows.Add((universe, instrument));
                }
            }
        }
        return matches;
    }

    private static Dictionary<string, HashSet<TerminalUniverseKind>> CandidateSets(
        IReadOnlyList<string> epics,
        IReadOnlyDictionary<string, List<(TerminalUniverseKind Universe, MarketInstrument Instrument)>> cachedMatches,
        IReadOnlyDictionary<string, MarketInstrument> probed,
        IReadOnlySet<string> knownEtfEpics)
    {
        var result = new Dictionary<string, HashSet<TerminalUniverseKind>>(StringComparer.OrdinalIgnoreCase);
        foreach (var epic in epics)
        {
            if (knownEtfEpics.Contains(epic))
            {
                result[epic] = [TerminalUniverseKind.ETFs];
                continue;
            }
            if (probed.TryGetValue(epic, out var metadata) && ClassifyType(metadata) is { } probedUniverse)
            {
                result[epic] = [probedUniverse];
                continue;
            }

            var candidates = new HashSet<TerminalUniverseKind>();
            foreach (var (cachedUniverse, instrument) in cachedMatches[epic])
            {
                candidates.Add(ClassifyType(instrument) ?? cachedUniverse);
            }
            result[epic] = candidates;
        }
        return result;
    }

    private static bool TrySingleUniverse(
        IReadOnlyDictionary<string, HashSet<TerminalUniverseKind>> candidates,
        out TerminalUniverseKind universe)
    {
        universe = default;
        if (candidates.Count == 0 || candidates.Any(pair => pair.Value.Count == 0)) return false;
        var shared = candidates.First().Value.ToHashSet();
        foreach (var candidatesForEpic in candidates.Values.Skip(1)) shared.IntersectWith(candidatesForEpic);
        if (shared.Count != 1) return false;
        universe = shared.Single();
        return true;
    }

    private static IReadOnlyList<MarketInstrument> SelectInstruments(
        IReadOnlyList<string> epics,
        TerminalUniverseKind universe,
        IReadOnlyDictionary<string, List<(TerminalUniverseKind Universe, MarketInstrument Instrument)>> cachedMatches,
        IReadOnlyDictionary<string, MarketInstrument> probed,
        IReadOnlySet<string> knownEtfEpics)
    {
        var selected = new List<MarketInstrument>(epics.Count);
        foreach (var epic in epics)
        {
            if (probed.TryGetValue(epic, out var metadata) &&
                MatchesUniverse(epic, metadata, universe, knownEtfEpics))
            {
                selected.Add(metadata);
                continue;
            }

            var typed = cachedMatches[epic]
                .Select(match => match.Instrument)
                .FirstOrDefault(instrument => MatchesUniverse(epic, instrument, universe, knownEtfEpics));
            if (typed is not null)
            {
                selected.Add(typed);
                continue;
            }

            var cached = cachedMatches[epic].FirstOrDefault(match => match.Universe == universe).Instrument;
            if (cached is not null) selected.Add(cached);
        }
        return selected;
    }

    private static bool MatchesUniverse(
        string epic,
        MarketInstrument instrument,
        TerminalUniverseKind universe,
        IReadOnlySet<string> knownEtfEpics) =>
        knownEtfEpics.Contains(epic)
            ? universe == TerminalUniverseKind.ETFs
            : ClassifyType(instrument) == universe;

    private static TerminalUniverseKind? ClassifyType(MarketInstrument instrument) =>
        CapitalInstrumentTypes.IsCrypto(instrument) ? TerminalUniverseKind.Crypto :
        CapitalInstrumentTypes.IsEtf(instrument) ? TerminalUniverseKind.ETFs :
        CapitalInstrumentTypes.IsStock(instrument) ? TerminalUniverseKind.Stocks :
        null;

    private static bool AreKnownEtfs(IEnumerable<string> epics, IReadOnlySet<string> knownEtfEpics)
    {
        var selected = epics.Where(epic => !string.IsNullOrWhiteSpace(epic)).ToArray();
        return selected.Length > 0 && selected.All(knownEtfEpics.Contains);
    }

    private static InvalidOperationException LegacyResolutionRequired(IEnumerable<string> epics) => new(
        $"Legacy basket universe requires cache or Capital type metadata for: {string.Join(", ", epics)}.");
}
