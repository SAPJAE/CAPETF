using System.IO;
using System.Text.Json;

namespace CAPETF.Desktop;

public enum TerminalUniverseStage
{
    Cached,
    Discovering,
    Complete,
}

public sealed record TerminalUniverseProgress(
    TerminalUniverseStage Stage,
    int Cached,
    int Discovered,
    int TotalDiscovered,
    bool IsComplete);

public sealed record TerminalUniverseSnapshot(
    IReadOnlyList<MarketInstrument> Instruments,
    TerminalUniverseProgress Progress);

public sealed record TerminalUniverseSelection(string Block, string SeedText);

public sealed class TerminalUniverseAccumulator
{
    private readonly Dictionary<string, MarketInstrument> _instruments = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _discoveredEpics = new(StringComparer.OrdinalIgnoreCase);
    private int _cached;
    private int _totalDiscovered;

    public TerminalUniverseAccumulator(TerminalUniverseKind universe)
    {
        Universe = universe;
    }

    public TerminalUniverseKind Universe { get; }

    public TerminalUniverseSnapshot PublishCached(IReadOnlyList<MarketInstrument> instruments)
    {
        ArgumentNullException.ThrowIfNull(instruments);
        _instruments.Clear();
        _discoveredEpics.Clear();
        _totalDiscovered = 0;
        Add(instruments, replaceExisting: true);
        _cached = _instruments.Count;
        return Snapshot(TerminalUniverseStage.Cached, isComplete: false);
    }

    public TerminalUniverseSnapshot MergeCached(IReadOnlyList<MarketInstrument> instruments)
    {
        ArgumentNullException.ThrowIfNull(instruments);
        Add(instruments, replaceExisting: false);
        _cached = _instruments.Count;
        return Snapshot(TerminalUniverseStage.Cached, isComplete: false);
    }

    public TerminalUniverseSnapshot MergeDiscoveryBatch(
        IReadOnlyList<MarketInstrument> instruments,
        int totalDiscovered,
        bool isComplete)
    {
        ArgumentNullException.ThrowIfNull(instruments);
        if (totalDiscovered < 0) throw new ArgumentOutOfRangeException(nameof(totalDiscovered));

        foreach (var instrument in instruments)
        {
            if (string.IsNullOrWhiteSpace(instrument.Epic)) continue;
            _discoveredEpics.Add(instrument.Epic.Trim());
        }
        Add(instruments, replaceExisting: true);
        _totalDiscovered = Math.Max(_totalDiscovered, totalDiscovered);
        return Snapshot(isComplete ? TerminalUniverseStage.Complete : TerminalUniverseStage.Discovering, isComplete);
    }

    public TerminalUniverseSelection PreserveSelection(
        TerminalUniverseSelection selection,
        TerminalUniverseSnapshot? snapshot = null)
    {
        ArgumentNullException.ThrowIfNull(selection);
        var instruments = snapshot?.Instruments ?? OrderedInstruments();
        var block = instruments.Any(item => string.Equals(item.Group, selection.Block, StringComparison.OrdinalIgnoreCase))
            ? selection.Block
            : instruments.FirstOrDefault()?.Group ?? "";
        var seedEpic = ExtractSeedEpic(selection.SeedText);
        var seed = !string.IsNullOrWhiteSpace(seedEpic) &&
                   instruments.Any(item => string.Equals(item.Epic, seedEpic, StringComparison.OrdinalIgnoreCase))
            ? selection.SeedText
            : string.Empty;
        return new TerminalUniverseSelection(block, seed);
    }

    private TerminalUniverseSnapshot Snapshot(TerminalUniverseStage stage, bool isComplete) =>
        new(
            OrderedInstruments(),
            new TerminalUniverseProgress(stage, _cached, _discoveredEpics.Count, _totalDiscovered, isComplete));

    private IReadOnlyList<MarketInstrument> OrderedInstruments() =>
        _instruments.Values
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Symbol, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Epic, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private void Add(IReadOnlyList<MarketInstrument> instruments, bool replaceExisting)
    {
        foreach (var instrument in instruments)
        {
            if (string.IsNullOrWhiteSpace(instrument.Epic)) continue;
            var epic = instrument.Epic.Trim();
            if (replaceExisting || !_instruments.ContainsKey(epic)) _instruments[epic] = instrument;
        }
    }

    private static string ExtractSeedEpic(string seedText)
    {
        if (string.IsNullOrWhiteSpace(seedText)) return "";
        var delimiter = seedText.IndexOf(" | ", StringComparison.Ordinal);
        return (delimiter > 0 ? seedText[..delimiter] : seedText).Trim();
    }
}

public sealed class TerminalUniverseCache
{
    private readonly string _directory;
    private readonly JsonSerializerOptions _options = new() { WriteIndented = false };

    public TerminalUniverseCache(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentException("Universe cache directory is required.", nameof(directory));
        _directory = directory;
    }

    public IReadOnlyList<MarketInstrument> Load(TerminalUniverseKind universe)
    {
        var path = PathFor(universe);
        if (!File.Exists(path)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<MarketInstrument>>(File.ReadAllText(path), _options) ?? [];
        }
        catch (IOException)
        {
            return [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public void Save(TerminalUniverseKind universe, IReadOnlyList<MarketInstrument> instruments)
    {
        ArgumentNullException.ThrowIfNull(instruments);
        Directory.CreateDirectory(_directory);
        var path = PathFor(universe);
        var temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(instruments, _options));
        File.Move(temporary, path, overwrite: true);
    }

    private string PathFor(TerminalUniverseKind universe) =>
        Path.Combine(_directory, $"terminal-universe-{universe.ToString().ToLowerInvariant()}.json");
}
