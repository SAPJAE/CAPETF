using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;

namespace CAPETF.Desktop;

public sealed record SyntheticRiskPlan(
    string ExecutionId,
    string BasketId,
    string Side,
    decimal? StopLoss,
    decimal? TakeProfit,
    DateTimeOffset UpdatedUtc);

public sealed record SyntheticRiskPlanValidationResult(
    bool IsValid,
    SyntheticRiskPlan? Plan,
    string Error);

public sealed record SyntheticRiskPlanLevels(decimal StopLoss, decimal TakeProfit);

public static class SyntheticRiskPlanDefaults
{
    public static SyntheticRiskPlanLevels Create(string side, decimal entry)
    {
        if (!SyntheticRiskPlanValidation.TryNormalizeSide(side, out var normalizedSide))
            throw new ArgumentException("Side must be BUY or SELL.", nameof(side));
        if (entry <= 0m) throw new ArgumentOutOfRangeException(nameof(entry), "Entry must be positive.");

        return normalizedSide == "BUY"
            ? new SyntheticRiskPlanLevels(entry * 0.98m, entry * 1.04m)
            : new SyntheticRiskPlanLevels(entry * 1.02m, entry * 0.96m);
    }
}

public static class SyntheticRiskPlanValidation
{
    public static SyntheticRiskPlanValidationResult Validate(
        string executionId,
        string basketId,
        string side,
        decimal entry,
        decimal? stopLoss,
        decimal? takeProfit,
        DateTimeOffset? now = null)
    {
        if (string.IsNullOrWhiteSpace(executionId)) return Invalid("Execution ID is required.");
        if (string.IsNullOrWhiteSpace(basketId)) return Invalid("Basket ID is required.");
        if (!TryNormalizeSide(side, out var normalizedSide)) return Invalid("Side must be BUY or SELL.");
        if (entry <= 0m) return Invalid("Entry must be positive.");
        if (stopLoss is <= 0m) return Invalid("Stop loss must be positive.");
        if (takeProfit is <= 0m) return Invalid("Take profit must be positive.");

        var validLevels = normalizedSide == "BUY"
            ? (stopLoss is null || stopLoss < entry) && (takeProfit is null || takeProfit > entry)
            : (takeProfit is null || takeProfit < entry) && (stopLoss is null || stopLoss > entry);
        if (!validLevels) return Invalid("Risk levels do not surround entry for the execution side.");

        return new SyntheticRiskPlanValidationResult(
            true,
            new SyntheticRiskPlan(executionId, basketId, normalizedSide, stopLoss, takeProfit, now ?? DateTimeOffset.UtcNow),
            "");
    }

    internal static bool TryNormalizeSide(string? side, out string normalizedSide)
    {
        if (string.Equals(side?.Trim(), "BUY", StringComparison.OrdinalIgnoreCase))
        {
            normalizedSide = "BUY";
            return true;
        }

        if (string.Equals(side?.Trim(), "SELL", StringComparison.OrdinalIgnoreCase))
        {
            normalizedSide = "SELL";
            return true;
        }

        normalizedSide = "";
        return false;
    }

    private static SyntheticRiskPlanValidationResult Invalid(string error) =>
        new(false, null, error);
}

public sealed class SyntheticRiskPlanStore
{
    private const int SchemaVersion = 1;
    private static readonly ConcurrentDictionary<string, object> PathGates = new(StringComparer.OrdinalIgnoreCase);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private readonly object _gate;
    private readonly string _path;
    private readonly string _temporaryPathPrefix;

    public SyntheticRiskPlanStore(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("A persistence path is required.", nameof(path));

        _path = Path.GetFullPath(path);
        _temporaryPathPrefix = _path + ".tmp-";
        _gate = PathGates.GetOrAdd(_path, _ => new object());
    }

    public IReadOnlyList<SyntheticRiskPlan> LoadAll()
    {
        lock (_gate)
        {
            return LoadCore();
        }
    }

    public void Upsert(SyntheticRiskPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        lock (_gate)
        {
            var plans = LoadCore().ToList();
            var normalized = NormalizeStoredPlan(plan);
            var existingIndex = plans.FindIndex(existing =>
                existing.ExecutionId.Equals(normalized.ExecutionId, StringComparison.Ordinal));
            if (existingIndex >= 0) plans[existingIndex] = normalized;
            else plans.Add(normalized);

            SaveCore(plans);
        }
    }

    public void Remove(string executionId)
    {
        if (string.IsNullOrWhiteSpace(executionId)) throw new ArgumentException("An execution ID is required.", nameof(executionId));

        lock (_gate)
        {
            var plans = LoadCore().ToList();
            if (plans.RemoveAll(plan => plan.ExecutionId.Equals(executionId, StringComparison.Ordinal)) == 0) return;
            SaveCore(plans);
        }
    }

    private IReadOnlyList<SyntheticRiskPlan> LoadCore()
    {
        if (!File.Exists(_path)) return [];

        using var stream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var document = JsonSerializer.Deserialize<PersistedRiskPlans>(stream, JsonOptions)
            ?? throw new JsonException("Synthetic risk-plan persistence is empty.");
        if (document.SchemaVersion != SchemaVersion || document.Plans is null)
        {
            throw new JsonException("Synthetic risk-plan persistence has an unsupported schema.");
        }

        var plans = document.Plans!.Select(NormalizeStoredPlan).ToList();
        var executionIds = new HashSet<string>(StringComparer.Ordinal);
        if (plans.Any(plan => !executionIds.Add(plan.ExecutionId)))
        {
            throw new JsonException("Synthetic risk-plan persistence contains duplicate execution IDs.");
        }

        return plans;
    }

    private void SaveCore(IReadOnlyList<SyntheticRiskPlan> plans)
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);

        var content = JsonSerializer.SerializeToUtf8Bytes(new PersistedRiskPlans(SchemaVersion, plans), JsonOptions);
        var temporaryPath = _temporaryPathPrefix + Guid.NewGuid().ToString("N");
        try
        {
            using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
            {
                stream.Write(content);
                stream.Flush(flushToDisk: true);
            }

            File.Move(temporaryPath, _path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }

    private static SyntheticRiskPlan NormalizeStoredPlan(SyntheticRiskPlan plan)
    {
        if (string.IsNullOrWhiteSpace(plan.ExecutionId)
            || string.IsNullOrWhiteSpace(plan.BasketId)
            || !SyntheticRiskPlanValidation.TryNormalizeSide(plan.Side, out var side)
            || plan.StopLoss is <= 0m
            || plan.TakeProfit is <= 0m
            || plan.UpdatedUtc == default)
        {
            throw new JsonException("Synthetic risk-plan persistence contains an invalid plan.");
        }

        return plan with { Side = side };
    }

    private sealed record PersistedRiskPlans(int SchemaVersion, IReadOnlyList<SyntheticRiskPlan> Plans);
}
