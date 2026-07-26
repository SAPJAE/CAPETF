using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CAPETF.Desktop;

public sealed record EtfDataLoadResult(
    IReadOnlyList<MarketInstrument> Instruments,
    IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>> OhlcByEpic,
    DateTimeOffset? RefreshedAtUtc,
    DateOnly? SourceAsOf,
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>>> OhlcByEpicAndResolution,
    IReadOnlySet<string> KnownEtfEpics);

public static class DashboardEtfDataLoader
{
    private const string DefaultDashboardPassword = "jae";

    public static EtfDataLoadResult LoadEtfs(string? password = null)
    {
        var path = FindEtfDataFile();
        if (path is null)
        {
            return new EtfDataLoadResult([], EmptyCandles(), null, null, EmptyCandlesByResolution(), EmptyEtfEpics());
        }

        using var encryptedDocument = JsonDocument.Parse(File.ReadAllText(path));
        using var plainDocument = JsonDocument.Parse(Decrypt(encryptedDocument.RootElement, string.IsNullOrWhiteSpace(password) ? DefaultDashboardPassword : password.Trim()));
        var root = plainDocument.RootElement;
        var summary = root.TryGetProperty("summary", out var summaryElement) ? summaryElement : default;
        var ohlcByEpic = new Dictionary<string, IReadOnlyList<OhlcPoint>>(StringComparer.OrdinalIgnoreCase);
        var ohlcByEpicAndResolution = new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>>>(StringComparer.OrdinalIgnoreCase);
        var instruments = new List<MarketInstrument>();
        var knownEtfEpics = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (root.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in items.EnumerateArray())
            {
                var epic = ReadString(item, "epic");
                if (string.IsNullOrWhiteSpace(epic)) continue;
                knownEtfEpics.Add(epic);

                var instrument = new MarketInstrument
                {
                    Epic = epic,
                    Name = ReadString(item, "name") ?? epic,
                    Symbol = ReadString(item, "symbol") ?? "",
                    // Capital.com reports ETF CFDs as SHARES; this dedicated cache is pre-classified upstream.
                    Type = "ETF",
                    Currency = ReadString(item, "currency") ?? "",
                    Country = ReadString(item, "country") ?? "",
                    Sector = NormalizeSector(ReadString(item, "sector"), ReadString(item, "industry")),
                    Region = ReadString(item, "region") ?? RegionFromCountry(ReadString(item, "country")),
                    Status = ReadString(item, "status") ?? "",
                    Price = ReadDecimal(item, "price"),
                    ChangePercent = ReadDecimal(item, "return1w"),
                };
                if (!TerminalUniverse.Accepts(TerminalUniverseKind.ETFs, instrument)) continue;

                foreach (var point in ReadChartPoints(item, "dailyPoints")) instrument.Points.Add(point);
                if (instrument.Points.Count == 0)
                {
                    foreach (var point in ReadChartPoints(item, "weeklyPoints")) instrument.Points.Add(point);
                }

                instruments.Add(instrument);
                var byResolution = BuildCandlesByResolution(item);
                if (byResolution.Count == 0) continue;
                ohlcByEpicAndResolution[epic] = byResolution;
                ohlcByEpic[epic] =
                    byResolution.TryGetValue("Weekly", out var weekly) ? weekly :
                    byResolution.TryGetValue("Daily", out var daily) ? daily :
                    byResolution.Values.First();
            }
        }

        var distinct = instruments
            .GroupBy(item => item.Epic, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(item => item.Group, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new EtfDataLoadResult(
            distinct,
            ohlcByEpic,
            ReadDateTimeOffset(summary, "refreshedAtUtc"),
            ReadDateOnly(summary, "sourceAsOf"),
            ohlcByEpicAndResolution,
            knownEtfEpics);
    }

    private static string? FindEtfDataFile()
    {
        foreach (var directory in CandidateDataDirectories())
        {
            var path = Path.Combine(directory, "etfs.enc.json");
            if (File.Exists(path)) return path;
        }

        return null;
    }

    private static IEnumerable<string> CandidateDataDirectories()
    {
        var baseDirectory = AppContext.BaseDirectory;
        yield return Path.Combine(baseDirectory, "data");
        yield return Path.Combine(Environment.CurrentDirectory, "data");
        yield return Path.GetFullPath(Path.Combine(baseDirectory, "..", "..", "..", "..", "data"));
        yield return Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "data"));
    }

    private static byte[] Decrypt(JsonElement encrypted, string password)
    {
        var salt = FromBase64Url(ReadRequiredString(encrypted, "salt"));
        var iv = FromBase64Url(ReadRequiredString(encrypted, "iv"));
        var payload = FromBase64Url(ReadRequiredString(encrypted, "ciphertext"));
        var iterations = encrypted.TryGetProperty("iterations", out var iterationsElement) && iterationsElement.TryGetInt32(out var parsed)
            ? parsed
            : 250_000;
        var key = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, iterations, HashAlgorithmName.SHA256, 32);
        var cipherText = payload[..^16];
        var tag = payload[^16..];
        var plainText = new byte[cipherText.Length];
        using var aes = new AesGcm(key, 16);
        aes.Decrypt(iv, cipherText, tag, plainText);
        return plainText;
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>> EmptyCandles() =>
        new Dictionary<string, IReadOnlyList<OhlcPoint>>(StringComparer.OrdinalIgnoreCase);

    private static IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>>> EmptyCandlesByResolution() =>
        new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>>>(StringComparer.OrdinalIgnoreCase);

    private static IReadOnlySet<string> EmptyEtfEpics() =>
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    private static IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>> BuildCandlesByResolution(JsonElement item)
    {
        var result = new Dictionary<string, IReadOnlyList<OhlcPoint>>(StringComparer.OrdinalIgnoreCase);
        AddCandles(result, "Weekly", ReadChartPoints(item, "weeklyPoints"));
        AddCandles(result, "Daily", ReadChartPoints(item, "dailyPoints"));
        var hourly = ClosePointsToCandles(ReadChartPoints(item, "hourlyPoints"));
        if (hourly.Count >= 2)
        {
            result["2H"] = AggregateCandles(hourly, 2);
            result["4H"] = AggregateCandles(hourly, 4);
            result["6H"] = AggregateCandles(hourly, 6);
        }

        if (result.Count == 0) AddCandles(result, "Weekly", ReadChartPoints(item, "monthlyPoints"));
        return result.Where(pair => pair.Value.Count >= 2).ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
    }

    private static void AddCandles(Dictionary<string, IReadOnlyList<OhlcPoint>> target, string resolution, IReadOnlyList<ChartPoint> points)
    {
        var candles = ClosePointsToCandles(points);
        if (candles.Count >= 2) target[resolution] = candles;
    }

    private static IReadOnlyList<OhlcPoint> AggregateCandles(IReadOnlyList<OhlcPoint> source, int bucketSize)
    {
        var ordered = source.OrderBy(point => point.Time).ToList();
        var result = new List<OhlcPoint>();
        for (var index = 0; index + bucketSize <= ordered.Count; index += bucketSize)
        {
            var bucket = ordered.Skip(index).Take(bucketSize).ToList();
            result.Add(new OhlcPoint(bucket[^1].Time, bucket[0].Open, bucket.Max(point => point.High), bucket.Min(point => point.Low), bucket[^1].Close));
        }

        return result;
    }

    private static IReadOnlyList<OhlcPoint> ClosePointsToCandles(IReadOnlyList<ChartPoint> points)
    {
        var ordered = points.Where(point => point.Close > 0).OrderBy(point => point.Time).ToList();
        if (ordered.Count < 2) return [];

        var result = new List<OhlcPoint>(ordered.Count);
        var previousClose = ordered[0].Close;
        foreach (var point in ordered)
        {
            var open = previousClose;
            result.Add(new OhlcPoint(point.Time, open, Math.Max(open, point.Close), Math.Min(open, point.Close), point.Close));
            previousClose = point.Close;
        }

        return result;
    }

    private static IReadOnlyList<ChartPoint> ReadChartPoints(JsonElement item, string propertyName)
    {
        if (!item.TryGetProperty(propertyName, out var points) || points.ValueKind != JsonValueKind.Array) return [];
        var result = new List<ChartPoint>();
        foreach (var point in points.EnumerateArray())
        {
            var price = ReadDecimal(point, "p") ?? ReadDecimal(point, "v");
            var date = ReadString(point, "d");
            if (price is null || string.IsNullOrWhiteSpace(date) || !TryParsePointTime(date, out var time)) continue;
            result.Add(new ChartPoint(time, price.Value));
        }

        return result;
    }

    private static bool TryParsePointTime(string value, out DateTimeOffset time)
    {
        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out time)) return true;
        if (DateTime.TryParseExact(value, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var month))
        {
            time = new DateTimeOffset(DateTime.SpecifyKind(month, DateTimeKind.Utc));
            return true;
        }

        time = default;
        return false;
    }

    private static string NormalizeSector(string? sector, string? industry) =>
        !string.IsNullOrWhiteSpace(sector) ? sector : !string.IsNullOrWhiteSpace(industry) ? industry : "All";

    private static string RegionFromCountry(string? country) =>
        string.IsNullOrWhiteSpace(country) ? "Other" : country.Contains("United States", StringComparison.OrdinalIgnoreCase) ? "US" : "Other";

    private static string ReadRequiredString(JsonElement element, string name) =>
        ReadString(element, name) ?? throw new InvalidOperationException($"Encrypted payload missing {name}.");

    private static string? ReadString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static decimal? ReadDecimal(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value)) return null;
        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number)) return number;
        return value.ValueKind == JsonValueKind.String && decimal.TryParse(value.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
    }

    private static DateTimeOffset? ReadDateTimeOffset(JsonElement element, string name) =>
        DateTimeOffset.TryParse(ReadString(element, name), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed) ? parsed : null;

    private static DateOnly? ReadDateOnly(JsonElement element, string name) =>
        DateOnly.TryParse(ReadString(element, name), CultureInfo.InvariantCulture, out var parsed) ? parsed : null;

    private static byte[] FromBase64Url(string value)
    {
        var padded = value.PadRight(value.Length + ((4 - value.Length % 4) % 4), '=');
        return Convert.FromBase64String(padded.Replace('-', '+').Replace('_', '/'));
    }
}
