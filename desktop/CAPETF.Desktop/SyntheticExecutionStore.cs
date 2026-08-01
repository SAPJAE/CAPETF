using System.IO;
using System.Text.Json;
using System.Collections.Concurrent;

namespace CAPETF.Desktop;

public sealed class SyntheticExecutionStore
{
    private const int SchemaVersion = 1;
    private static readonly TimeSpan AbandonedTemporaryFileAge = TimeSpan.FromMinutes(1);
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> PathGates = new(StringComparer.OrdinalIgnoreCase);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private readonly SemaphoreSlim _gate;
    private readonly string _path;
    private readonly string _temporaryPathPrefix;

    public string LastLoadWarning { get; private set; } = "";

    public SyntheticExecutionStore(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("A persistence path is required.", nameof(path));

        _path = Path.GetFullPath(path);
        _temporaryPathPrefix = _path + ".tmp-";
        _gate = PathGates.GetOrAdd(_path, _ => new SemaphoreSlim(1, 1));
    }

    public async Task<IReadOnlyList<SyntheticExecutionRecord>> LoadAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            CleanupAbandonedTemporaryFiles();
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
            CleanupAbandonedTemporaryFiles();
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
            CleanupAbandonedTemporaryFiles();
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

            ValidateExecutions(document.Executions);

            LastLoadWarning = "";
            return document.Executions;
        }
        catch (JsonException exception)
        {
            var quarantinePath = QuarantineMalformedFile();
            LastLoadWarning = $"Synthetic position history was invalid and quarantined at {quarantinePath}. Capital.com positions may still be open. {exception.Message}";
            return [];
        }
    }

    private async Task SaveCoreAsync(IReadOnlyList<SyntheticExecutionRecord> records, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);

        ValidateExecutions(records);
        var content = JsonSerializer.SerializeToUtf8Bytes(new PersistedExecutions(SchemaVersion, records), JsonOptions);
        var temporaryPath = _temporaryPathPrefix + Guid.NewGuid().ToString("N");
        try
        {
            await using (var stream = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                FileOptions.WriteThrough))
            {
                await stream.WriteAsync(content, cancellationToken);
                await stream.FlushAsync(cancellationToken);
                stream.Flush(flushToDisk: true);
            }

            await ReplaceWithRetryAsync(temporaryPath, cancellationToken);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }

    private async Task ReplaceWithRetryAsync(string temporaryPath, CancellationToken cancellationToken)
    {
        const int maximumAttempts = 8;
        for (var attempt = 1; ; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                File.Move(temporaryPath, _path, overwrite: true);
                return;
            }
            catch (IOException) when (attempt < maximumAttempts)
            {
            }
            catch (UnauthorizedAccessException) when (attempt < maximumAttempts)
            {
            }

            await Task.Delay(TimeSpan.FromMilliseconds(25 * attempt), cancellationToken);
        }
    }

    private string QuarantineMalformedFile()
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
        return quarantinePath;
    }

    private void CleanupAbandonedTemporaryFiles()
    {
        var directory = Path.GetDirectoryName(_path);
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory)) return;

        var cutoff = DateTime.UtcNow - AbandonedTemporaryFileAge;
        foreach (var temporaryPath in Directory.EnumerateFiles(directory, Path.GetFileName(_path) + ".tmp*"))
        {
            try
            {
                if (File.GetLastWriteTimeUtc(temporaryPath) > cutoff) continue;
                using (new FileStream(temporaryPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                }
                File.Delete(temporaryPath);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    private static void ValidateExecutions(IReadOnlyList<SyntheticExecutionRecord> executions)
    {
        var executionIds = new HashSet<string>(StringComparer.Ordinal);
        var trackedDealIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var execution in executions)
        {
            if (execution is null
                || string.IsNullOrWhiteSpace(execution.ExecutionId)
                || string.IsNullOrWhiteSpace(execution.TicketId)
                || string.IsNullOrWhiteSpace(execution.BasketId)
                || !IsTradingDirection(execution.Side)
                || execution.RequestedNotional <= 0m
                || execution.BasketQuantity is <= 0m
                || execution.EstimatedMargin < 0m
                || string.IsNullOrWhiteSpace(execution.MarginCurrency)
                || execution.CreatedUtc == default
                || execution.UpdatedUtc == default
                || execution.UpdatedUtc < execution.CreatedUtc
                || execution.UniverseKind is { } universeKind && !Enum.IsDefined(universeKind)
                || !Enum.IsDefined(execution.State)
                || execution.State is SyntheticExecutionState.Preflighting or SyntheticExecutionState.Ready
                || execution.Legs is null
                || execution.Legs.Count == 0
                || !executionIds.Add(execution.ExecutionId))
            {
                throw new JsonException("Synthetic execution persistence contains an invalid execution record.");
            }

            foreach (var leg in execution.Legs)
            {
                ValidateLeg(execution, leg, trackedDealIds);
            }

            ValidateExecutionState(execution.State, execution.Legs);
        }
    }

    private static void ValidateLeg(
        SyntheticExecutionRecord execution,
        SyntheticExecutionLegRecord? leg,
        ISet<string> trackedDealIds)
    {
        if (leg is null
            || string.IsNullOrWhiteSpace(leg.Epic)
            || !IsTradingDirection(leg.Direction)
            || leg.Multiplier == 0m
            || leg.ReferencePrice <= 0m
            || leg.Quantity <= 0m
            || leg.Notional <= 0m
            || leg.EstimatedMargin < 0m
            || string.IsNullOrWhiteSpace(leg.MarginCurrency)
            || !Enum.IsDefined(leg.State)
            || leg.UpdatedUtc == default
            || leg.UpdatedUtc < execution.CreatedUtc
            || leg.UpdatedUtc > execution.UpdatedUtc
            || (leg.SubmittedUtc is { } submittedUtc && (submittedUtc == default || submittedUtc < execution.CreatedUtc || submittedUtc > leg.UpdatedUtc))
            || (leg.ConfirmedUtc is { } confirmedUtc && (confirmedUtc == default || leg.SubmittedUtc is not { } submitted || confirmedUtc < submitted || confirmedUtc > leg.UpdatedUtc))
            || (leg.ClosedUtc is { } closedUtc && (closedUtc == default || leg.ConfirmedUtc is not { } confirmed || closedUtc < confirmed || closedUtc > leg.UpdatedUtc))
            || (!string.IsNullOrWhiteSpace(leg.DealId) && !trackedDealIds.Add(leg.DealId)))
        {
            throw new JsonException("Synthetic execution persistence contains an invalid execution leg.");
        }

        var hasOpenIdentity = !string.IsNullOrWhiteSpace(leg.DealReference)
            && !string.IsNullOrWhiteSpace(leg.DealId)
            && leg.SubmittedUtc is not null
            && leg.ConfirmedUtc is not null;
        var hasNoDealIdentity = string.IsNullOrWhiteSpace(leg.DealReference)
            && string.IsNullOrWhiteSpace(leg.DealId)
            && string.IsNullOrWhiteSpace(leg.CloseDealReference)
            && leg.ConfirmedUtc is null
            && leg.ClosedUtc is null
            && leg.FillLevel is null;
        var validState = leg.State switch
        {
            SyntheticExecutionLegState.Pending => leg.SubmittedUtc is null && hasNoDealIdentity && leg.CurrentUnrealizedProfitLoss is null,
            SyntheticExecutionLegState.Submitted => leg.SubmittedUtc is not null && hasNoDealIdentity && leg.CurrentUnrealizedProfitLoss is null,
            SyntheticExecutionLegState.Confirming => leg.SubmittedUtc is not null
                && !string.IsNullOrWhiteSpace(leg.DealReference)
                && string.IsNullOrWhiteSpace(leg.DealId)
                && string.IsNullOrWhiteSpace(leg.CloseDealReference)
                && leg.ConfirmedUtc is null
                && leg.ClosedUtc is null
                && leg.FillLevel is null
                && leg.CurrentUnrealizedProfitLoss is null,
            SyntheticExecutionLegState.Open => hasOpenIdentity
                && leg.ClosedUtc is null,
            SyntheticExecutionLegState.Rejected => leg.SubmittedUtc is not null
                && string.IsNullOrWhiteSpace(leg.DealId)
                && string.IsNullOrWhiteSpace(leg.CloseDealReference)
                && leg.ConfirmedUtc is null
                && leg.ClosedUtc is null
                && leg.FillLevel is null
                && leg.CurrentUnrealizedProfitLoss is null,
            SyntheticExecutionLegState.Unknown => leg.SubmittedUtc is not null
                && leg.ClosedUtc is null
                && (string.IsNullOrWhiteSpace(leg.DealId)
                    ? leg.ConfirmedUtc is null
                        && leg.FillLevel is null
                        && string.IsNullOrWhiteSpace(leg.CloseDealReference)
                        && leg.CurrentUnrealizedProfitLoss is null
                    : hasOpenIdentity),
            SyntheticExecutionLegState.Closing => hasOpenIdentity
                && leg.ClosedUtc is null,
            SyntheticExecutionLegState.Closed => hasOpenIdentity
                && leg.ClosedUtc is not null,
            _ => false,
        };
        if (!validState)
        {
            throw new JsonException("Synthetic execution persistence contains inconsistent execution leg state.");
        }
    }

    private static void ValidateExecutionState(
        SyntheticExecutionState state,
        IReadOnlyList<SyntheticExecutionLegRecord> legs)
    {
        var has = (SyntheticExecutionLegState legState) => legs.Any(leg => leg.State == legState);
        bool Only(params SyntheticExecutionLegState[] allowed) => legs.All(leg => allowed.Contains(leg.State));
        var validState = state switch
        {
            SyntheticExecutionState.Submitting => Only(
                SyntheticExecutionLegState.Pending,
                SyntheticExecutionLegState.Submitted,
                SyntheticExecutionLegState.Confirming),
            SyntheticExecutionState.PartiallyOpen => has(SyntheticExecutionLegState.Open) && Only(
                SyntheticExecutionLegState.Pending,
                SyntheticExecutionLegState.Submitted,
                SyntheticExecutionLegState.Confirming,
                SyntheticExecutionLegState.Open),
            SyntheticExecutionState.Open => legs.All(leg => leg.State == SyntheticExecutionLegState.Open),
            SyntheticExecutionState.NeedsAttention => Only(
                SyntheticExecutionLegState.Pending,
                SyntheticExecutionLegState.Rejected,
                SyntheticExecutionLegState.Open,
                SyntheticExecutionLegState.Unknown,
                SyntheticExecutionLegState.Closed)
                && !legs.All(leg => leg.State == SyntheticExecutionLegState.Rejected),
            SyntheticExecutionState.Closing => Only(
                SyntheticExecutionLegState.Pending,
                SyntheticExecutionLegState.Rejected,
                SyntheticExecutionLegState.Open,
                SyntheticExecutionLegState.Unknown,
                SyntheticExecutionLegState.Closing,
                SyntheticExecutionLegState.Closed),
            SyntheticExecutionState.PartiallyClosed => has(SyntheticExecutionLegState.Closed)
                && (has(SyntheticExecutionLegState.Open)
                    || has(SyntheticExecutionLegState.Unknown)
                    || has(SyntheticExecutionLegState.Closing))
                && Only(
                    SyntheticExecutionLegState.Pending,
                    SyntheticExecutionLegState.Rejected,
                    SyntheticExecutionLegState.Open,
                    SyntheticExecutionLegState.Unknown,
                    SyntheticExecutionLegState.Closing,
                    SyntheticExecutionLegState.Closed),
            SyntheticExecutionState.Closed => Only(
                SyntheticExecutionLegState.Pending,
                SyntheticExecutionLegState.Rejected,
                SyntheticExecutionLegState.Closed),
            SyntheticExecutionState.Rejected => has(SyntheticExecutionLegState.Rejected)
                && Only(SyntheticExecutionLegState.Pending, SyntheticExecutionLegState.Rejected),
            _ => false,
        };
        if (!validState)
        {
            throw new JsonException("Synthetic execution persistence contains inconsistent execution state.");
        }
    }

    private static bool IsTradingDirection(string? value) =>
        string.Equals(value, "BUY", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "SELL", StringComparison.OrdinalIgnoreCase);

    private sealed record PersistedExecutions(int SchemaVersion, IReadOnlyList<SyntheticExecutionRecord> Executions);
}
