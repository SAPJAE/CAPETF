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

        var sharedTimes = FindSharedTimes(candlesByEpic, components);
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
            FindSharedTimes(history.CandlesByEpic, requested).Count == 0)
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
        return source
            .GroupBy(point => BucketStart(point.Time, bucketSize))
            .OrderBy(group => group.Key)
            .Select(group => CreateCompleteBucket(group.Key, group, bucketSize))
            .OfType<OhlcPoint>()
            .ToList();
    }

    private static OhlcPoint? CreateCompleteBucket(
        DateTimeOffset bucketStart,
        IEnumerable<OhlcPoint> rows,
        int bucketSize)
    {
        var ordered = rows.OrderBy(point => point.Time).ToList();
        var byTime = ordered
            .GroupBy(point => point.Time.ToUniversalTime())
            .ToDictionary(group => group.Key, group => group.Last());
        if (ordered.Count != bucketSize || byTime.Count != bucketSize)
        {
            return null;
        }

        var expectedTimes = Enumerable.Range(0, bucketSize)
            .Select(offset => bucketStart.AddHours(offset))
            .ToList();
        if (expectedTimes.Any(time => !byTime.ContainsKey(time))) return null;

        var candles = expectedTimes.Select(time => byTime[time]).ToList();
        return new OhlcPoint(
            expectedTimes[^1],
            candles[0].Open,
            candles.Max(point => point.High),
            candles.Min(point => point.Low),
            candles[^1].Close);
    }

    private static DateTimeOffset BucketStart(DateTimeOffset timestamp, int bucketSize)
    {
        var utc = timestamp.ToUniversalTime();
        var hour = utc.Hour / bucketSize * bucketSize;
        return new DateTimeOffset(utc.Year, utc.Month, utc.Day, hour, 0, 0, TimeSpan.Zero);
    }

    private static IReadOnlyList<DateTimeOffset> FindSharedTimes(
        IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>> candlesByEpic,
        IReadOnlyList<MarketInstrument> components)
    {
        var timeSets = components
            .Select(component => candlesByEpic.TryGetValue(component.Epic, out var candles)
                ? candles.Select(candle => candle.Time).ToHashSet()
                : [])
            .ToList();
        if (timeSets.Count == 0 || timeSets.Any(times => times.Count == 0)) return [];

        var shared = timeSets[0];
        foreach (var times in timeSets.Skip(1)) shared.IntersectWith(times);
        return shared.OrderBy(time => time).ToList();
    }
}
