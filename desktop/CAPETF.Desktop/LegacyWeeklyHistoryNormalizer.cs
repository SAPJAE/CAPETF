namespace CAPETF.Desktop;

internal static class LegacyWeeklyHistoryNormalizer
{
    public static IReadOnlyList<OhlcPoint> Normalize(IReadOnlyList<OhlcPoint> source)
    {
        var ordered = source
            .Select((row, index) => (row, index))
            .OrderBy(item => item.row.Time)
            .ThenBy(item => item.index)
            .Select(item => item.row)
            .ToList();
        if (ordered.Count < 2 ||
            ordered.Select(row => row.Time).Distinct().Count() == ordered.Count ||
            ordered.Any(row => row.Time.Day != 1))
        {
            return ordered;
        }

        var result = new List<OhlcPoint>(ordered.Count);
        var usedWeeks = new HashSet<DateTime>();
        DateTimeOffset? previous = null;
        foreach (var month in ordered.GroupBy(row => (row.Time.Year, row.Time.Month)))
        {
            var monthStart = new DateTime(month.Key.Year, month.Key.Month, 1);
            var monthEnd = monthStart.AddMonths(1);
            var slots = Enumerable.Range(0, (monthEnd - monthStart).Days)
                .Select(offset => monthStart.AddDays(offset))
                .GroupBy(WeekStart)
                .Select(group => group.First())
                .Where(date => !usedWeeks.Contains(WeekStart(date)))
                .Select(date => new DateTimeOffset(DateTime.SpecifyKind(date, DateTimeKind.Utc)))
                .Where(time => previous is null || time > previous.Value)
                .Take(month.Count())
                .ToList();
            if (slots.Count != month.Count()) return ordered;

            foreach (var (row, slot) in month.Zip(slots))
            {
                result.Add(row with { Time = slot });
                usedWeeks.Add(WeekStart(slot.Date));
                previous = slot;
            }
        }

        return result;
    }

    private static DateTime WeekStart(DateTime date)
    {
        var offset = ((int)date.DayOfWeek + 6) % 7;
        return date.Date.AddDays(-offset);
    }
}
