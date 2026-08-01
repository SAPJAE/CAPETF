namespace CAPETF.Desktop;

public sealed class SyntheticHistorySessionCache
{
    private readonly Dictionary<string, Dictionary<string, IReadOnlyList<OhlcPoint>>> _rowsByTimeframe =
        new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<MarketInstrument> Missing(
        IReadOnlyList<MarketInstrument> components,
        string timeframe)
    {
        var cached = RowsFor(timeframe);
        return components
            .Where(component => !string.IsNullOrWhiteSpace(component.Epic) && !cached.ContainsKey(component.Epic))
            .GroupBy(component => component.Epic, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
    }

    public IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>> Get(
        IReadOnlyList<MarketInstrument> components,
        string timeframe)
    {
        var cached = RowsFor(timeframe);
        var result = new Dictionary<string, IReadOnlyList<OhlcPoint>>(StringComparer.OrdinalIgnoreCase);
        foreach (var component in components)
        {
            if (cached.TryGetValue(component.Epic, out var rows)) result[component.Epic] = rows;
        }
        return result;
    }

    public void Store(string timeframe, HistoryLoadResult history)
    {
        if (!_rowsByTimeframe.TryGetValue(timeframe, out var cached))
        {
            cached = new Dictionary<string, IReadOnlyList<OhlcPoint>>(StringComparer.OrdinalIgnoreCase);
            _rowsByTimeframe[timeframe] = cached;
        }

        foreach (var (epic, rows) in history.CandlesByEpic)
        {
            if (rows.Count > 0) cached[epic] = rows.OrderBy(row => row.Time).ToList();
        }
    }

    private IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>> RowsFor(string timeframe) =>
        _rowsByTimeframe.TryGetValue(timeframe, out var cached)
            ? cached
            : new Dictionary<string, IReadOnlyList<OhlcPoint>>(StringComparer.OrdinalIgnoreCase);
}
