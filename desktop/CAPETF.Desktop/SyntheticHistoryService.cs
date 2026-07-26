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
        var result = SyntheticBasketBuilder.Build(
            block,
            selectedComponents,
            history.CandlesByEpic,
            maxBaskets: 1,
            periodsPerYear: periodsPerYear,
            minimumCandles: minimumCandles);
        return result.Baskets.FirstOrDefault();
    }

    private static IReadOnlyList<OhlcPoint> Aggregate(IReadOnlyList<OhlcPoint> source, int bucketSize)
    {
        var ordered = source.OrderBy(point => point.Time).ToList();
        var result = new List<OhlcPoint>();
        for (var index = 0; index + bucketSize <= ordered.Count; index += bucketSize)
        {
            var bucket = ordered.Skip(index).Take(bucketSize).ToList();
            result.Add(new OhlcPoint(
                bucket[^1].Time,
                bucket[0].Open,
                bucket.Max(point => point.High),
                bucket.Min(point => point.Low),
                bucket[^1].Close));
        }

        return result;
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
