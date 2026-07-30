using System.IO;
using System.Text.Json;

namespace CAPETF.Desktop;

public sealed class SyntheticExecutionStore
{
    private const int SchemaVersion = 1;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _path;
    private readonly string _temporaryPath;

    public SyntheticExecutionStore(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("A persistence path is required.", nameof(path));

        _path = Path.GetFullPath(path);
        _temporaryPath = _path + ".tmp";
    }

    public async Task<IReadOnlyList<SyntheticExecutionRecord>> LoadAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return await LoadCoreAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(IReadOnlyList<SyntheticExecutionRecord> records, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(records);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            await SaveCoreAsync(records, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task UpsertAsync(SyntheticExecutionRecord record, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(record);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var records = (await LoadCoreAsync(cancellationToken)).ToList();
            var existingIndex = records.FindIndex(existing =>
                existing.ExecutionId.Equals(record.ExecutionId, StringComparison.Ordinal));
            if (existingIndex >= 0) records[existingIndex] = record;
            else records.Add(record);

            await SaveCoreAsync(records, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<IReadOnlyList<SyntheticExecutionRecord>> LoadCoreAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_path)) return [];

        try
        {
            await using var stream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var document = await JsonSerializer.DeserializeAsync<PersistedExecutions>(stream, JsonOptions, cancellationToken);
            if (document is null || document.SchemaVersion != SchemaVersion || document.Executions is null)
            {
                throw new JsonException("Synthetic execution persistence has an unsupported schema.");
            }

            return document.Executions;
        }
        catch (JsonException)
        {
            QuarantineMalformedFile();
            return [];
        }
    }

    private async Task SaveCoreAsync(IReadOnlyList<SyntheticExecutionRecord> records, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);

        var content = JsonSerializer.SerializeToUtf8Bytes(new PersistedExecutions(SchemaVersion, records), JsonOptions);
        try
        {
            await using (var stream = new FileStream(
                _temporaryPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                FileOptions.WriteThrough))
            {
                await stream.WriteAsync(content, cancellationToken);
                await stream.FlushAsync(cancellationToken);
                stream.Flush(flushToDisk: true);
            }

            File.Move(_temporaryPath, _path, overwrite: true);
        }
        finally
        {
            if (File.Exists(_temporaryPath)) File.Delete(_temporaryPath);
        }
    }

    private void QuarantineMalformedFile()
    {
        var suffix = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmssfffffff");
        var quarantinePath = _path + ".corrupt-" + suffix;
        var collision = 0;
        while (File.Exists(quarantinePath))
        {
            collision++;
            quarantinePath = _path + ".corrupt-" + suffix + "-" + collision;
        }

        File.Move(_path, quarantinePath);
    }

    private sealed record PersistedExecutions(int SchemaVersion, IReadOnlyList<SyntheticExecutionRecord> Executions);
}
