namespace CAPETF.Desktop;

public sealed record HistoryLoadProgress(int CompletedComponents, int TotalComponents, string Epic);

public sealed record HistoryLoadResult(
    IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>> CandlesByEpic,
    DateTimeOffset? SharedStart,
    DateTimeOffset? SharedEnd,
    int SharedCount);

public sealed class SyntheticHistoryService
{
    private readonly CapitalApiClient _api;

    public SyntheticHistoryService(CapitalApiClient api)
    {
        _api = api;
    }

    public async Task<HistoryLoadResult> LoadSelectedAsync(
        IReadOnlyList<MarketInstrument> selectedComponents,
        string timeframe,
        IProgress<HistoryLoadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var components = selectedComponents
            .Where(component => !string.IsNullOrWhiteSpace(component.Epic))
            .GroupBy(component => component.Epic, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
        var candlesByEpic = new Dictionary<string, IReadOnlyList<OhlcPoint>>(StringComparer.OrdinalIgnoreCase);
        var pagingAnchor = DateTimeOffset.UtcNow.AddDays(1);

        for (var index = 0; index < components.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var component = components[index];
            try
            {
                var source = await _api.GetAllAvailableOhlcPricesAsync(
                    component.Epic,
                    RequestResolution(timeframe),
                    cancellationToken,
                    pagingAnchor);
                var candles = Transform(source, timeframe);
                if (candles.Count > 0)
                {
                    candlesByEpic[component.Epic] = candles;
                }
                else
                {
                    component.Status = "History n/a";
                }
            }
            catch (CapitalApiException ex) when (ex.IsHistoryUnavailable)
            {
                component.Status = $"History n/a: {ex.Message}";
            }
            finally
            {
                progress?.Report(new HistoryLoadProgress(index + 1, components.Count, component.Epic));
            }
        }

        var sharedTimes = FindSharedTimes(candlesByEpic, components, timeframe);
        return new HistoryLoadResult(
            candlesByEpic,
            sharedTimes.Count == 0 ? null : sharedTimes[0],
            sharedTimes.Count == 0 ? null : sharedTimes[^1],
            sharedTimes.Count);
    }

    public static string RequestResolution(string timeframe) =>
        timeframe is "2H" or "4H" or "6H" ? "HOUR" :
        timeframe == "Daily" ? "DAY" :
        "WEEK";

    public static IReadOnlyList<OhlcPoint> Transform(IReadOnlyList<OhlcPoint> source, string timeframe) =>
        timeframe switch
        {
            "2H" => SessionAwareHourlyAggregation.Aggregate(source, 2),
            "4H" => SessionAwareHourlyAggregation.Aggregate(source, 4),
            "6H" => SessionAwareHourlyAggregation.Aggregate(source, 6),
            _ => source.OrderBy(point => point.Time).ToList(),
        };

    public static HistoryLoadResult MergeSelectedHistory(
        IReadOnlyList<MarketInstrument> selectedComponents,
        string timeframe,
        HistoryLoadResult apiHistory,
        IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>> cachedHistory) =>
        MergeSelectedHistory(
            selectedComponents,
            apiHistory,
            cachedHistory,
            time => AlignmentKey(time, timeframe));

    public static HistoryLoadResult MergeSelectedManualHistory(
        IReadOnlyList<MarketInstrument> selectedComponents,
        string timeframe,
        HistoryLoadResult apiHistory,
        IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>> cachedHistory) =>
        MergeSelectedHistory(
            selectedComponents,
            apiHistory,
            cachedHistory,
            time => $"T:{time.ToUniversalTime().Ticks}");

    private static HistoryLoadResult MergeSelectedHistory(
        IReadOnlyList<MarketInstrument> selectedComponents,
        HistoryLoadResult apiHistory,
        IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>> cachedHistory,
        Func<DateTimeOffset, string> alignmentKey)
    {
        var components = selectedComponents
            .Where(component => !string.IsNullOrWhiteSpace(component.Epic))
            .GroupBy(component => component.Epic, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
        var merged = new Dictionary<string, IReadOnlyList<OhlcPoint>>(StringComparer.OrdinalIgnoreCase);

        foreach (var component in components)
        {
            var rowsByKey = new Dictionary<string, OhlcPoint>(StringComparer.Ordinal);
            if (cachedHistory.TryGetValue(component.Epic, out var cachedRows))
            {
                foreach (var row in cachedRows) rowsByKey[alignmentKey(row.Time)] = row;
            }
            if (apiHistory.CandlesByEpic.TryGetValue(component.Epic, out var apiRows))
            {
                foreach (var row in apiRows) rowsByKey[alignmentKey(row.Time)] = row;
            }
            if (rowsByKey.Count > 0)
            {
                merged[component.Epic] = rowsByKey.Values.OrderBy(row => row.Time).ToList();
            }
        }

        var sharedTimes = FindSharedTimes(merged, components, alignmentKey);
        return new HistoryLoadResult(
            merged,
            sharedTimes.Count == 0 ? null : sharedTimes[0],
            sharedTimes.Count == 0 ? null : sharedTimes[^1],
            sharedTimes.Count);
    }

    public static SyntheticBasket? BuildSelected(
        string block,
        IReadOnlyList<MarketInstrument> selectedComponents,
        HistoryLoadResult history,
        string timeframe,
        int periodsPerYear,
        int minimumCandles)
    {
        var requested = selectedComponents
            .Where(component => !string.IsNullOrWhiteSpace(component.Epic))
            .ToList();
        var requestedEpics = requested
            .Select(component => component.Epic)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (requested.Count != selectedComponents.Count ||
            requestedEpics.Count != requested.Count ||
            requested.Any(component =>
                !history.CandlesByEpic.TryGetValue(component.Epic, out var candles) ||
                DistinctAlignmentKeyCount(candles, timeframe) < minimumCandles) ||
            FindSharedTimes(history.CandlesByEpic, requested, timeframe).Count == 0)
        {
            return null;
        }

        var result = SyntheticBasketBuilder.Build(
            block,
            requested,
            history.CandlesByEpic,
            maxBaskets: 1,
            periodsPerYear: periodsPerYear,
            minimumCandles: minimumCandles);
        var basket = result.Baskets.FirstOrDefault();
        if (basket is null || basket.Components.Count != requested.Count) return null;

        var basketEpics = basket.Components
            .Select(component => component.Instrument.Epic)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return basketEpics.Count == requestedEpics.Count && basketEpics.SetEquals(requestedEpics) ? basket : null;
    }

    private static IReadOnlyList<DateTimeOffset> FindSharedTimes(
        IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>> candlesByEpic,
        IReadOnlyList<MarketInstrument> components,
        string timeframe) =>
        FindSharedTimes(candlesByEpic, components, time => AlignmentKey(time, timeframe));

    private static IReadOnlyList<DateTimeOffset> FindSharedTimes(
        IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>> candlesByEpic,
        IReadOnlyList<MarketInstrument> components,
        Func<DateTimeOffset, string> alignmentKey)
    {
        var rowsByKey = components
            .Select(component => candlesByEpic.TryGetValue(component.Epic, out var candles)
                ? candles.GroupBy(candle => alignmentKey(candle.Time))
                    .ToDictionary(group => group.Key, group => group.Last().Time)
                : new Dictionary<string, DateTimeOffset>())
            .ToList();
        if (rowsByKey.Count == 0 || rowsByKey.Any(rows => rows.Count == 0)) return [];

        var shared = rowsByKey[0].Keys.ToHashSet(StringComparer.Ordinal);
        foreach (var rows in rowsByKey.Skip(1)) shared.IntersectWith(rows.Keys);
        return shared.Select(key => rowsByKey[0][key]).OrderBy(time => time).ToList();
    }

    internal static int DistinctAlignmentKeyCount(IReadOnlyList<OhlcPoint> candles, string timeframe) =>
        candles.Select(candle => AlignmentKey(candle.Time, timeframe)).Distinct(StringComparer.Ordinal).Count();

    internal static string AlignmentKey(DateTimeOffset time, string timeframe) => timeframe switch
    {
        "Daily" => $"D:{time:yyyyMMdd}",
        "Weekly" => $"W:{WeekStart(time.Date):yyyyMMdd}",
        _ => $"T:{time.ToUniversalTime().Ticks}",
    };

    private static DateTime WeekStart(DateTime date)
    {
        var offset = ((int)date.DayOfWeek + 6) % 7;
        return date.Date.AddDays(-offset);
    }
}
