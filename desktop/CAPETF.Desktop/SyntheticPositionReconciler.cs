namespace CAPETF.Desktop;

public sealed class SyntheticPositionReconciler
{
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
        var legs = record.Legs.Select(leg => ReconcileLeg(leg, positionsByDealId, now)).ToArray();

        return record with
        {
            Legs = legs,
            State = DetermineExecutionState(record.State, legs),
            UpdatedUtc = now,
        };
    }

    private static SyntheticExecutionLegRecord ReconcileLeg(
        SyntheticExecutionLegRecord leg,
        IReadOnlyDictionary<string, CapitalOpenPosition> positionsByDealId,
        DateTimeOffset now)
    {
        if (!string.IsNullOrWhiteSpace(leg.DealId)
            && positionsByDealId.TryGetValue(leg.DealId, out var position))
        {
            return leg with
            {
                State = SyntheticExecutionLegState.Open,
                CurrentUnrealizedProfitLoss = position.UnrealizedProfitLoss,
                UpdatedUtc = now,
            };
        }

        if (leg.State is SyntheticExecutionLegState.Open or SyntheticExecutionLegState.Closing)
        {
            return leg with
            {
                State = SyntheticExecutionLegState.Closed,
                CurrentUnrealizedProfitLoss = null,
                ClosedUtc = leg.ClosedUtc ?? now,
                UpdatedUtc = now,
            };
        }

        return leg.CurrentUnrealizedProfitLoss is null
            ? leg
            : leg with { CurrentUnrealizedProfitLoss = null, UpdatedUtc = now };
    }

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
