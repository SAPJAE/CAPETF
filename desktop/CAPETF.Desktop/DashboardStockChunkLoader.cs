using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CAPETF.Desktop;

public sealed record StockChunkLoadResult(
    IReadOnlyList<MarketInstrument> Instruments,
    IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>> OhlcByEpic,
    int ChunkCount,
    DateTimeOffset? RefreshedAtUtc,
    DateOnly? SourceAsOf,
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>>>? OhlcByEpicAndResolution = null);

public static class DashboardStockChunkLoader
{
    private const string DefaultDashboardPassword = "jae";

    public static StockChunkLoadResult LoadStocks(string? password = null)
    {
        var files = FindStockChunkFiles();
        if (files.Count == 0)
        {
            return new StockChunkLoadResult([], new Dictionary<string, IReadOnlyList<OhlcPoint>>(StringComparer.OrdinalIgnoreCase), 0, null, null);
        }

        var instruments = new List<MarketInstrument>();
        var OhlcByEpic = new Dictionary<string, IReadOnlyList<OhlcPoint>>(StringComparer.OrdinalIgnoreCase);
        var OhlcByEpicAndResolution = new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>>>(StringComparer.OrdinalIgnoreCase);
        DateTimeOffset? refreshedAt = null;
        DateOnly? sourceAsOf = null;
        var secret = string.IsNullOrWhiteSpace(password) ? DefaultDashboardPassword : password.Trim();

        foreach (var file in files)
        {
            using var encryptedDocument = JsonDocument.Parse(File.ReadAllText(file));
            using var plainDocument = JsonDocument.Parse(Decrypt(encryptedDocument.RootElement, secret));
            var root = plainDocument.RootElement;

            if (root.TryGetProperty("summary", out var summary))
            {
                refreshedAt ??= ReadDateTimeOffset(summary, "refreshedAtUtc");
                sourceAsOf ??= ReadDateOnly(summary, "sourceAsOf");
            }

            if (!root.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array) continue;
            foreach (var item in items.EnumerateArray())
            {
                var epic = ReadString(item, "epic");
                if (string.IsNullOrWhiteSpace(epic)) continue;

                var instrument = new MarketInstrument
                {
                    Epic = epic,
                    Name = ReadString(item, "name") ?? epic,
                    Symbol = ReadString(item, "symbol") ?? "",
                    Type = ReadString(item, "instrumentType") ?? "SHARES",
                    Currency = ReadString(item, "currency") ?? "",
                    Country = ReadString(item, "country") ?? "",
                    Sector = NormalizeSector(ReadString(item, "sector"), ReadString(item, "industry")),
                    Region = ReadString(item, "region") ?? RegionFromCountry(ReadString(item, "country")),
                    Status = ReadString(item, "status") ?? "",
                    Price = ReadDecimal(item, "price"),
                    ChangePercent = ReadDecimal(item, "return1w"),
                };

                foreach (var point in ReadChartPoints(item, "dailyPoints"))
                {
                    instrument.Points.Add(point);
                }

                if (instrument.Points.Count == 0)
                {
                    foreach (var point in ReadChartPoints(item, "weeklyPoints"))
                    {
                        instrument.Points.Add(point);
                    }
                }

                instruments.Add(instrument);
                var candlesByResolution = BuildSyntheticCandlesByResolution(item);
                if (candlesByResolution.Count > 0)
                {
                    OhlcByEpicAndResolution[epic] = candlesByResolution;
                    OhlcByEpic[epic] =
                        candlesByResolution.TryGetValue("Weekly", out var weekly) ? weekly :
                        candlesByResolution.TryGetValue("Daily", out var daily) ? daily :
                        candlesByResolution.Values.First();
                }
            }
        }

        var distinct = instruments
            .GroupBy(item => item.Epic, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(item => item.Group, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new StockChunkLoadResult(distinct, OhlcByEpic, files.Count, refreshedAt, sourceAsOf, OhlcByEpicAndResolution);
    }

    private static IReadOnlyList<string> FindStockChunkFiles()
    {
        foreach (var directory in CandidateDataDirectories())
        {
            if (!Directory.Exists(directory)) continue;
            var files = Directory.GetFiles(directory, "stocks-*.enc.json")
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (files.Count > 0) return files;
        }

        return [];
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

    private static IReadOnlyList<OhlcPoint> BuildSyntheticCandles(JsonElement item)
    {
        var points = ReadChartPoints(item, "weeklyPoints");
        if (points.Count < 2) points = ReadChartPoints(item, "dailyPoints");
        if (points.Count < 2) points = ReadChartPoints(item, "monthlyPoints");
        if (points.Count < 2) points = ReadChartPoints(item, "hourlyPoints");
        return ClosePointsToCandles(points);
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>> BuildSyntheticCandlesByResolution(JsonElement item)
    {
        var result = new Dictionary<string, IReadOnlyList<OhlcPoint>>(StringComparer.OrdinalIgnoreCase);
        var weekly = ClosePointsToCandles(ReadChartPoints(item, "weeklyPoints"));
        var daily = ClosePointsToCandles(ReadChartPoints(item, "dailyPoints"));
        var hourly = ClosePointsToCandles(ReadChartPoints(item, "hourlyPoints"));

        if (weekly.Count >= 2) result["Weekly"] = weekly;
        if (daily.Count >= 2) result["Daily"] = daily;
        if (hourly.Count >= 2)
        {
            result["2H"] = AggregateCandles(hourly, 2);
            result["4H"] = AggregateCandles(hourly, 4);
            result["6H"] = AggregateCandles(hourly, 6);
        }

        var monthly = ClosePointsToCandles(ReadChartPoints(item, "monthlyPoints"));
        if (result.Count == 0 && monthly.Count >= 2) result["Weekly"] = monthly;
        return result
            .Where(pair => pair.Value.Count >= 2)
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<OhlcPoint> AggregateCandles(IReadOnlyList<OhlcPoint> source, int bucketSize)
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

    private static IReadOnlyList<OhlcPoint> ClosePointsToCandles(IReadOnlyList<ChartPoint> points)
    {
        var ordered = points
            .Where(point => point.Close > 0)
            .OrderBy(point => point.Time)
            .ToList();
        if (ordered.Count < 2) return [];

        var result = new List<OhlcPoint>(ordered.Count);
        var previousClose = ordered[0].Close;
        foreach (var point in ordered)
        {
            var open = previousClose;
            var close = point.Close;
            var high = Math.Max(open, close);
            var low = Math.Min(open, close);
            result.Add(new OhlcPoint(point.Time, open, high, low, close));
            previousClose = close;
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
            if (price is null || string.IsNullOrWhiteSpace(date)) continue;
            if (TryParsePointTime(date, out var time))
            {
                result.Add(new ChartPoint(time, price.Value));
            }
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

    private static string NormalizeSector(string? sector, string? industry)
    {
        if (!string.IsNullOrWhiteSpace(sector)) return sector;
        if (!string.IsNullOrWhiteSpace(industry)) return industry;
        return "All";
    }

    private static string RegionFromCountry(string? country)
    {
        if (string.IsNullOrWhiteSpace(country)) return "Other";
        return country.Contains("United States", StringComparison.OrdinalIgnoreCase) ? "US" : "Other";
    }

    private static string ReadRequiredString(JsonElement element, string name) =>
        ReadString(element, name) ?? throw new InvalidOperationException($"Encrypted payload missing {name}.");

    private static string? ReadString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static decimal? ReadDecimal(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value)) return null;
        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number)) return number;
        if (value.ValueKind == JsonValueKind.String &&
            decimal.TryParse(value.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)) return parsed;
        return null;
    }

    private static DateTimeOffset? ReadDateTimeOffset(JsonElement element, string name)
    {
        var value = ReadString(element, name);
        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed) ? parsed : null;
    }

    private static DateOnly? ReadDateOnly(JsonElement element, string name)
    {
        var value = ReadString(element, name);
        return DateOnly.TryParse(value, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
    }

    private static byte[] FromBase64Url(string value)
    {
        var padded = value.PadRight(value.Length + ((4 - value.Length % 4) % 4), '=');
        return Convert.FromBase64String(padded.Replace('-', '+').Replace('_', '/'));
    }
}
