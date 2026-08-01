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
        return ordered
            .GroupBy(point => BucketNumber(point.Time, bucketSize))
            .OrderBy(group => group.Key)
            .Select(group => CompleteBucket(group.OrderBy(point => point.Time.ToUniversalTime()).ToList(), bucketSize))
            .Where(point => point is not null)
            .Select(point => point!)
            .ToList();
    }

    private static long BucketNumber(DateTimeOffset time, int bucketSize)
    {
        var utcTicks = time.ToUniversalTime().Ticks - DateTimeOffset.UnixEpoch.Ticks;
        var hourNumber = Math.DivRem(utcTicks, TimeSpan.TicksPerHour, out _);
        return Math.DivRem(hourNumber, bucketSize, out _);
    }

    private static OhlcPoint? CompleteBucket(IReadOnlyList<OhlcPoint> candles, int bucketSize)
    {
        if (candles.Count != bucketSize) return null;
        for (var index = 1; index < candles.Count; index++)
        {
            if (candles[index].Time.ToUniversalTime() - candles[index - 1].Time.ToUniversalTime() != TimeSpan.FromHours(1))
            {
                return null;
            }
        }

        return new OhlcPoint(
            candles[^1].Time,
            candles[0].Open,
            candles.Max(point => point.High),
            candles.Min(point => point.Low),
            candles[^1].Close);
    }
}
