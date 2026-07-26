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

        for (var index = 0; index < components.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var component = components[index];
            try
            {
                var source = await _api.GetAllAvailableOhlcPricesAsync(
                    component.Epic,
                    RequestResolution(timeframe),
                    cancellationToken);
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
            catch (Exception ex)
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
        timeframe is "2H" or "6H" ? "HOUR" :
        timeframe == "4H" ? "HOUR_4" :
        timeframe == "Daily" ? "DAY" :
        "WEEK";

    public static IReadOnlyList<OhlcPoint> Transform(IReadOnlyList<OhlcPoint> source, string timeframe) =>
        timeframe switch
        {
            "2H" => Aggregate(source, 2),
            "6H" => Aggregate(source, 6),
            _ => source.OrderBy(point => point.Time).ToList(),
        };

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
                candles.Count < minimumCandles) ||
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

    private static IReadOnlyList<OhlcPoint> Aggregate(IReadOnlyList<OhlcPoint> source, int bucketSize)
    {
        var ordered = source
            .GroupBy(point => point.Time.ToUniversalTime())
            .Select(group => group.Last())
            .OrderBy(point => point.Time.ToUniversalTime())
            .ToList();
        var result = new List<OhlcPoint>();
        var run = new List<OhlcPoint>();
        foreach (var point in ordered)
        {
            if (run.Count > 0 && point.Time.ToUniversalTime() - run[^1].Time.ToUniversalTime() != TimeSpan.FromHours(1))
            {
                AddCompleteBuckets(run, bucketSize, result);
                run.Clear();
            }
            run.Add(point);
        }
        AddCompleteBuckets(run, bucketSize, result);
        return result;
    }

    private static void AddCompleteBuckets(
        IReadOnlyList<OhlcPoint> run,
        int bucketSize,
        ICollection<OhlcPoint> destination)
    {
        for (var start = 0; start + bucketSize <= run.Count; start += bucketSize)
        {
            var candles = run.Skip(start).Take(bucketSize).ToList();
            destination.Add(new OhlcPoint(
                candles[^1].Time,
                candles[0].Open,
                candles.Max(point => point.High),
                candles.Min(point => point.Low),
                candles[^1].Close));
        }
    }

    private static IReadOnlyList<DateTimeOffset> FindSharedTimes(
        IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>> candlesByEpic,
        IReadOnlyList<MarketInstrument> components,
        string timeframe)
    {
        var rowsByKey = components
            .Select(component => candlesByEpic.TryGetValue(component.Epic, out var candles)
                ? candles.GroupBy(candle => AlignmentKey(candle.Time, timeframe))
                    .ToDictionary(group => group.Key, group => group.Last().Time)
                : new Dictionary<string, DateTimeOffset>())
            .ToList();
        if (rowsByKey.Count == 0 || rowsByKey.Any(rows => rows.Count == 0)) return [];

        var shared = rowsByKey[0].Keys.ToHashSet(StringComparer.Ordinal);
        foreach (var rows in rowsByKey.Skip(1)) shared.IntersectWith(rows.Keys);
        return shared.Select(key => rowsByKey[0][key]).OrderBy(time => time).ToList();
    }

    private static string AlignmentKey(DateTimeOffset time, string timeframe) => timeframe switch
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
