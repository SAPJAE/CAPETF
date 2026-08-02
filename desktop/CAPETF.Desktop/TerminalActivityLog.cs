using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CAPETF.Desktop;

public enum TerminalActivitySeverity
{
    Info,
    Success,
    Warning,
    Error,
}

public sealed record TerminalActivityEvent(
    DateTimeOffset TimestampUtc,
    TerminalActivitySeverity Severity,
    string Operation,
    string Summary,
    string Detail = "");

public sealed class TerminalActivityLog
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _path;
    private readonly int _maximumEvents;
    private readonly object _gate = new();

    public TerminalActivityLog(string path, int maximumEvents = 1000)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Activity log path is required.", nameof(path));
        if (maximumEvents <= 0) throw new ArgumentOutOfRangeException(nameof(maximumEvents));
        _path = path;
        _maximumEvents = maximumEvents;
    }

    public IReadOnlyList<TerminalActivityEvent> Load()
    {
        lock (_gate)
        {
            return LoadUnsafe();
        }
    }

    public TerminalActivityEvent Append(
        TerminalActivitySeverity severity,
        string operation,
        string summary,
        string detail = "")
    {
        var entry = new TerminalActivityEvent(
            DateTimeOffset.UtcNow,
            severity,
            string.IsNullOrWhiteSpace(operation) ? "Terminal" : operation.Trim(),
            string.IsNullOrWhiteSpace(summary) ? "No detail supplied." : summary.Trim(),
            detail?.Trim() ?? "");
        lock (_gate)
        {
            var events = LoadUnsafe().Append(entry).TakeLast(_maximumEvents).ToArray();
            SaveUnsafe(events);
        }
        return entry;
    }

    public void Clear()
    {
        lock (_gate)
        {
            SaveUnsafe([]);
        }
    }

    public void Export(string destinationPath)
    {
        if (string.IsNullOrWhiteSpace(destinationPath)) throw new ArgumentException("Export path is required.", nameof(destinationPath));
        lock (_gate)
        {
            var directory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(destinationPath, JsonSerializer.Serialize(LoadUnsafe(), JsonOptions));
        }
    }

    private IReadOnlyList<TerminalActivityEvent> LoadUnsafe()
    {
        if (!File.Exists(_path)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<TerminalActivityEvent>>(File.ReadAllText(_path), JsonOptions)
                ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private void SaveUnsafe(IReadOnlyList<TerminalActivityEvent> events)
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        File.WriteAllText(_path, JsonSerializer.Serialize(events, JsonOptions));
    }
}
