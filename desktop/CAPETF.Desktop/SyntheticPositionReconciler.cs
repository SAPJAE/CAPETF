namespace CAPETF.Desktop;

public sealed class SyntheticPositionReconciler
{
    private static readonly TimeSpan SubmissionRecoveryWindow = TimeSpan.FromMinutes(2);

    public SyntheticExecutionRecord Reconcile(
        SyntheticExecutionRecord record,
        IReadOnlyList<CapitalOpenPosition> positions,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(positions);

        var positionsByDealId = positions
            .Where(position => !string.IsNullOrWhiteSpace(position.DealId))
            .GroupBy(position => position.DealId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var claimedDealIds = record.Legs
            .Where(leg => !string.IsNullOrWhiteSpace(leg.DealId))
            .Select(leg => leg.DealId)
            .ToHashSet(StringComparer.Ordinal);
        var legs = new List<SyntheticExecutionLegRecord>(record.Legs.Count);
        foreach (var leg in record.Legs)
        {
            var reconciled = ReconcileLeg(leg, positionsByDealId, positions, claimedDealIds, now);
            legs.Add(reconciled);
            if (!string.IsNullOrWhiteSpace(reconciled.DealId)) claimedDealIds.Add(reconciled.DealId);
        }

        return record with
        {
            Legs = legs.ToArray(),
            State = DetermineExecutionState(record.State, legs),
            UpdatedUtc = now,
        };
    }

    private static SyntheticExecutionLegRecord ReconcileLeg(
        SyntheticExecutionLegRecord leg,
        IReadOnlyDictionary<string, CapitalOpenPosition> positionsByDealId,
        IReadOnlyList<CapitalOpenPosition> positions,
        IReadOnlySet<string> claimedDealIds,
        DateTimeOffset now)
    {
        if (!string.IsNullOrWhiteSpace(leg.DealId)
            && positionsByDealId.TryGetValue(leg.DealId, out var position))
        {
            return leg with
            {
                State = SyntheticExecutionLegState.Open,
                CloseDealReference = "",
                CurrentUnrealizedProfitLoss = position.UnrealizedProfitLoss,
                ClosedUtc = null,
                UpdatedUtc = now,
            };
        }

        if (TryRecoverSubmittedLeg(leg, positions, claimedDealIds, now, out var recovered))
        {
            return recovered;
        }

        if (leg.State is SyntheticExecutionLegState.Open or SyntheticExecutionLegState.Closing
            || leg.State == SyntheticExecutionLegState.Unknown && !string.IsNullOrWhiteSpace(leg.DealId))
        {
            return leg with
            {
                State = SyntheticExecutionLegState.Closed,
                CurrentUnrealizedProfitLoss = null,
                ClosedUtc = leg.ClosedUtc ?? now,
                UpdatedUtc = now,
            };
        }

        if (leg.State is SyntheticExecutionLegState.Submitted or SyntheticExecutionLegState.Confirming)
        {
            return leg with
            {
                State = SyntheticExecutionLegState.Unknown,
                CurrentUnrealizedProfitLoss = null,
                UpdatedUtc = now,
            };
        }

        return leg.CurrentUnrealizedProfitLoss is null
            ? leg
            : leg with { CurrentUnrealizedProfitLoss = null, UpdatedUtc = now };
    }

    private static bool TryRecoverSubmittedLeg(
        SyntheticExecutionLegRecord leg,
        IReadOnlyList<CapitalOpenPosition> positions,
        IReadOnlySet<string> claimedDealIds,
        DateTimeOffset now,
        out SyntheticExecutionLegRecord recovered)
    {
        recovered = leg;
        if (leg.State != SyntheticExecutionLegState.Submitted
            || !string.IsNullOrWhiteSpace(leg.DealId)
            || leg.SubmittedUtc is not { } submittedUtc)
        {
            return false;
        }

        var earliestCreation = submittedUtc.AddSeconds(-5);
        var latestCreation = submittedUtc + SubmissionRecoveryWindow;
        if (latestCreation > now.AddSeconds(5)) latestCreation = now.AddSeconds(5);
        var matches = positions
            .Where(position =>
                !string.IsNullOrWhiteSpace(position.DealId)
                && !claimedDealIds.Contains(position.DealId)
                && position.CreatedUtc is { } createdUtc
                && createdUtc >= earliestCreation
                && createdUtc <= latestCreation
                && string.Equals(position.Epic, leg.Epic, StringComparison.OrdinalIgnoreCase)
                && string.Equals(position.Direction, leg.Direction, StringComparison.OrdinalIgnoreCase)
                && position.Size == leg.Quantity)
            .Take(2)
            .ToList();
        if (matches.Count != 1) return false;

        var match = matches[0];
        var confirmedUtc = match.CreatedUtc!.Value < submittedUtc ? submittedUtc : match.CreatedUtc.Value;
        recovered = leg with
        {
            State = SyntheticExecutionLegState.Open,
            DealReference = $"RECOVERED:{match.DealId}",
            DealId = match.DealId,
            FillLevel = match.Level,
            Message = AppendMessage(leg.Message, "Recovered from one fresh exact Capital.com position after restart."),
            ConfirmedUtc = confirmedUtc,
            CurrentUnrealizedProfitLoss = match.UnrealizedProfitLoss,
            UpdatedUtc = now,
        };
        return true;
    }

    private static string AppendMessage(string existing, string message) =>
        string.IsNullOrWhiteSpace(existing) ? message : $"{existing} {message}";

    private static SyntheticExecutionState DetermineExecutionState(
        SyntheticExecutionState previousState,
        IReadOnlyList<SyntheticExecutionLegRecord> legs)
    {
        if (legs.All(leg => leg.State == SyntheticExecutionLegState.Closed)) return SyntheticExecutionState.Closed;
        if (legs.Any(leg => leg.State == SyntheticExecutionLegState.Unknown)) return SyntheticExecutionState.NeedsAttention;
        if (legs.Any(leg => leg.State == SyntheticExecutionLegState.Rejected)
            && legs.Any(leg => leg.State == SyntheticExecutionLegState.Open)) return SyntheticExecutionState.NeedsAttention;
        if (legs.All(leg => leg.State == SyntheticExecutionLegState.Open)) return SyntheticExecutionState.Open;
        if (legs.Any(leg => leg.State == SyntheticExecutionLegState.Closed)
            && legs.Any(leg => leg.State == SyntheticExecutionLegState.Open)) return SyntheticExecutionState.PartiallyClosed;
        if (legs.Any(leg => leg.State == SyntheticExecutionLegState.Open)) return SyntheticExecutionState.PartiallyOpen;
        if (legs.Any(leg => leg.State == SyntheticExecutionLegState.Closed)) return SyntheticExecutionState.Closed;
        if (legs.Any(leg => leg.State == SyntheticExecutionLegState.Rejected)) return SyntheticExecutionState.Rejected;
        return previousState;
    }
}
