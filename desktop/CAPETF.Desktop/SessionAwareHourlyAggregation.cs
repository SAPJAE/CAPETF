namespace CAPETF.Desktop;

internal static class SessionAwareHourlyAggregation
{
    public static IReadOnlyList<OhlcPoint> Aggregate(IReadOnlyList<OhlcPoint> source, int bucketSize)
    {
        if (bucketSize <= 0) throw new ArgumentOutOfRangeException(nameof(bucketSize));

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
}
