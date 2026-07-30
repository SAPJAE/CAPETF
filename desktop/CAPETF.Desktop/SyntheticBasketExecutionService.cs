using System.Net.Http;

namespace CAPETF.Desktop;

public interface ICapitalTradingGateway
{
    Task<CapitalDealAcknowledgement> CreatePositionAsync(CapitalPositionRequest request, CancellationToken cancellationToken);
    Task<CapitalDealConfirmation> GetDealConfirmationAsync(string dealReference, CancellationToken cancellationToken);
    Task<CapitalDealAcknowledgement> ClosePositionAsync(string dealId, CancellationToken cancellationToken);
}

public sealed class CapitalTradingGateway : ICapitalTradingGateway
{
    private readonly CapitalApiClient _client;

    public CapitalTradingGateway(CapitalApiClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public Task<CapitalDealConfirmation> GetDealConfirmationAsync(string dealReference, CancellationToken cancellationToken) =>
        _client.GetDealConfirmationAsync(dealReference, cancellationToken);

    public async Task<CapitalDealAcknowledgement> CreatePositionAsync(
        CapitalPositionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _client.CreatePositionAsync(request, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            throw new CapitalMutationOutcomeUnknownException("The position request outcome is unknown.", exception);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new CapitalMutationOutcomeUnknownException("The position request timed out with an unknown outcome.", exception);
        }
    }

    public async Task<CapitalDealAcknowledgement> ClosePositionAsync(string dealId, CancellationToken cancellationToken)
    {
        try
        {
            return await _client.ClosePositionAsync(dealId, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            throw new CapitalMutationOutcomeUnknownException("The close request outcome is unknown.", exception);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new CapitalMutationOutcomeUnknownException("The close request timed out with an unknown outcome.", exception);
        }
    }
}

public sealed class CapitalMutationOutcomeUnknownException : Exception
{
    public CapitalMutationOutcomeUnknownException(string message) : base(message)
    {
    }

    public CapitalMutationOutcomeUnknownException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public interface ISyntheticExecutionClock
{
    DateTimeOffset UtcNow { get; }
    Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken);
}

public delegate Task SyntheticExecutionProgress(SyntheticExecutionRecord record, CancellationToken cancellationToken);

public sealed class SyntheticBasketExecutionService
{
    private const int ConfirmationAttempts = 15;
    private static readonly TimeSpan ConfirmationDelay = TimeSpan.FromSeconds(1);

    private readonly ICapitalTradingGateway _gateway;
    private readonly ISyntheticExecutionClock _clock;

    public SyntheticBasketExecutionService(ICapitalTradingGateway gateway, ISyntheticExecutionClock? clock = null)
    {
        _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        _clock = clock ?? new SystemSyntheticExecutionClock();
    }

    public async Task<SyntheticExecutionRecord> ExecuteAsync(
        SyntheticExecutionTicket ticket,
        SyntheticExecutionProgress progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        ArgumentNullException.ThrowIfNull(progress);
        if (ticket.Legs.Count == 0) throw new ArgumentException("An execution ticket must contain at least one leg.", nameof(ticket));

        var record = CreateRecord(ticket);
        await progress(record, cancellationToken);
        var cancelled = false;
        var mutationDispatched = false;

        for (var index = 0; index < record.Legs.Count; index++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                cancelled = true;
                break;
            }

            record = UpdateLeg(
                record,
                index,
                leg => leg with
                {
                    State = SyntheticExecutionLegState.Submitted,
                    SubmittedUtc = _clock.UtcNow,
                    UpdatedUtc = _clock.UtcNow,
                    Message = "",
                },
                record.Legs.Any(leg => leg.State == SyntheticExecutionLegState.Open)
                    ? SyntheticExecutionState.PartiallyOpen
                    : SyntheticExecutionState.Submitting);
            await progress(record, mutationDispatched ? CancellationToken.None : cancellationToken);
            if (cancellationToken.IsCancellationRequested)
            {
                cancelled = true;
                record = UpdateLeg(
                    record,
                    index,
                    leg => leg with
                    {
                        State = SyntheticExecutionLegState.Pending,
                        SubmittedUtc = null,
                        UpdatedUtc = _clock.UtcNow,
                    });
                break;
            }

            CapitalDealAcknowledgement acknowledgement;
            try
            {
                var leg = record.Legs[index];
                mutationDispatched = true;
                acknowledgement = await _gateway.CreatePositionAsync(
                    new CapitalPositionRequest(leg.Epic, leg.Direction, leg.Quantity),
                    cancellationToken);
            }
            catch (CapitalMutationOutcomeUnknownException exception)
            {
                record = MarkLeg(record, index, SyntheticExecutionLegState.Unknown, $"{record.Legs[index].Epic} submission: {exception.Message}");
                break;
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
                record = MarkLeg(record, index, SyntheticExecutionLegState.Unknown, $"{record.Legs[index].Epic} submission was cancelled with an unknown outcome.");
                break;
            }
            catch (Exception exception)
            {
                record = MarkLeg(record, index, SyntheticExecutionLegState.Unknown, $"{record.Legs[index].Epic} submission failed with an unknown outcome: {exception.Message}");
                break;
            }

            if (IsRejected(acknowledgement.DealStatus))
            {
                record = MarkLeg(record, index, SyntheticExecutionLegState.Rejected, FormatReason(record.Legs[index].Epic, "submission", acknowledgement.Reason));
                break;
            }

            if (string.IsNullOrWhiteSpace(acknowledgement.DealReference))
            {
                record = MarkLeg(record, index, SyntheticExecutionLegState.Unknown, $"{record.Legs[index].Epic} submission returned no deal reference; the outcome is unknown.");
                break;
            }

            record = UpdateLeg(
                record,
                index,
                leg => leg with
                {
                    State = SyntheticExecutionLegState.Confirming,
                    DealReference = acknowledgement.DealReference,
                    UpdatedUtc = _clock.UtcNow,
                });
            await progress(record, CancellationToken.None);

            var confirmation = await PollConfirmationAsync(acknowledgement.DealReference, cancellationToken);
            if (confirmation.Cancelled || cancellationToken.IsCancellationRequested) cancelled = true;
            record = ApplyOpenConfirmation(record, index, confirmation);
            await progress(record, CancellationToken.None);
            if (record.Legs[index].State != SyntheticExecutionLegState.Open) break;
        }

        record = record with
        {
            State = DetermineExecutionState(record, cancelled),
            UpdatedUtc = _clock.UtcNow,
        };
        await progress(record, mutationDispatched ? CancellationToken.None : cancellationToken);
        return record;
    }

    public async Task<SyntheticExecutionRecord> CloseAsync(
        SyntheticExecutionRecord record,
        SyntheticExecutionProgress progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(progress);

        var openIndexes = record.Legs
            .Select((leg, index) => (leg, index))
            .Where(value => value.leg.State == SyntheticExecutionLegState.Open && !string.IsNullOrWhiteSpace(value.leg.DealId))
            .Select(value => value.index)
            .ToArray();
        var current = record with { State = SyntheticExecutionState.Closing, UpdatedUtc = _clock.UtcNow };
        await progress(current, cancellationToken);
        var cancelled = false;
        var mutationDispatched = false;

        foreach (var index in openIndexes)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                cancelled = true;
                break;
            }

            current = UpdateLeg(
                current,
                index,
                leg => leg with { State = SyntheticExecutionLegState.Closing, Message = "", UpdatedUtc = _clock.UtcNow },
                SyntheticExecutionState.Closing);
            await progress(current, mutationDispatched ? CancellationToken.None : cancellationToken);
            if (cancellationToken.IsCancellationRequested)
            {
                cancelled = true;
                current = UpdateLeg(
                    current,
                    index,
                    leg => leg with { State = SyntheticExecutionLegState.Open, UpdatedUtc = _clock.UtcNow });
                break;
            }

            CapitalDealAcknowledgement acknowledgement;
            try
            {
                mutationDispatched = true;
                acknowledgement = await _gateway.ClosePositionAsync(current.Legs[index].DealId, cancellationToken);
            }
            catch (CapitalMutationOutcomeUnknownException exception)
            {
                current = MarkLeg(current, index, SyntheticExecutionLegState.Unknown, $"{current.Legs[index].Epic} close: {exception.Message}");
                break;
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
                current = MarkLeg(current, index, SyntheticExecutionLegState.Unknown, $"{current.Legs[index].Epic} close was cancelled with an unknown outcome.");
                break;
            }
            catch (Exception exception)
            {
                current = MarkLeg(current, index, SyntheticExecutionLegState.Unknown, $"{current.Legs[index].Epic} close failed with an unknown outcome: {exception.Message}");
                break;
            }

            if (IsRejected(acknowledgement.DealStatus))
            {
                current = MarkLeg(current, index, SyntheticExecutionLegState.Open, FormatReason(current.Legs[index].Epic, "close", acknowledgement.Reason));
                break;
            }

            if (string.IsNullOrWhiteSpace(acknowledgement.DealReference))
            {
                current = MarkLeg(current, index, SyntheticExecutionLegState.Unknown, $"{current.Legs[index].Epic} close returned no deal reference; the outcome is unknown.");
                break;
            }

            current = UpdateLeg(
                current,
                index,
                leg => leg with { CloseDealReference = acknowledgement.DealReference, UpdatedUtc = _clock.UtcNow });
            await progress(current, CancellationToken.None);

            var confirmation = await PollConfirmationAsync(acknowledgement.DealReference, cancellationToken);
            if (confirmation.Cancelled || cancellationToken.IsCancellationRequested) cancelled = true;
            current = ApplyCloseConfirmation(current, index, confirmation);
            await progress(current, CancellationToken.None);
            if (current.Legs[index].State != SyntheticExecutionLegState.Closed) break;
        }

        current = current with { State = DetermineCloseState(current, cancelled), UpdatedUtc = _clock.UtcNow };
        await progress(current, mutationDispatched ? CancellationToken.None : cancellationToken);
        return current;
    }

    private SyntheticExecutionRecord CreateRecord(SyntheticExecutionTicket ticket)
    {
        var now = _clock.UtcNow;
        var legs = ticket.Legs.Select(leg => new SyntheticExecutionLegRecord(
            leg.Epic,
            leg.Direction,
            leg.Multiplier,
            leg.ReferencePrice,
            leg.Quantity,
            leg.Notional,
            leg.EstimatedMargin,
            leg.MarginCurrency,
            SyntheticExecutionLegState.Pending,
            "",
            "",
            "",
            null,
            "",
            null,
            null,
            null,
            now)).ToArray();
        return new SyntheticExecutionRecord(
            Guid.NewGuid().ToString("N"),
            ticket.TicketId,
            ticket.BasketId,
            ticket.Side,
            ticket.RequestedNotional,
            ticket.EstimatedMargin,
            ticket.MarginCurrency,
            now,
            now,
            SyntheticExecutionState.Submitting,
            legs);
    }

    private async Task<ConfirmationResult> PollConfirmationAsync(string dealReference, CancellationToken cancellationToken)
    {
        Exception? lastException = null;
        for (var attempt = 0; attempt < ConfirmationAttempts; attempt++)
        {
            try
            {
                await _clock.DelayAsync(ConfirmationDelay, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return new ConfirmationResult(null, true, "Confirmation was cancelled with an unknown outcome.");
            }

            try
            {
                var confirmation = await _gateway.GetDealConfirmationAsync(dealReference, cancellationToken);
                if (IsAccepted(confirmation.DealStatus) || IsRejected(confirmation.DealStatus))
                {
                    return new ConfirmationResult(confirmation, false, "");
                }
            }
            catch (OperationCanceledException)
            {
                return new ConfirmationResult(null, true, "Confirmation was cancelled with an unknown outcome.");
            }
            catch (Exception exception)
            {
                lastException = exception;
            }
        }

        var message = lastException is null
            ? "Confirmation timed out with an unknown outcome."
            : $"Confirmation failed with an unknown outcome: {lastException.Message}";
        return new ConfirmationResult(null, false, message);
    }

    private SyntheticExecutionRecord ApplyOpenConfirmation(
        SyntheticExecutionRecord record,
        int index,
        ConfirmationResult result)
    {
        var epic = record.Legs[index].Epic;
        if (result.Confirmation is null)
        {
            return MarkOpenLegTerminalOutcome(
                record,
                index,
                SyntheticExecutionLegState.Unknown,
                $"{epic} confirmation: {result.Message}");
        }

        if (IsRejected(result.Confirmation.DealStatus))
        {
            return MarkOpenLegTerminalOutcome(
                record,
                index,
                SyntheticExecutionLegState.Rejected,
                FormatReason(epic, "confirmation", result.Confirmation.Reason));
        }

        var dealId = FindAffectedDealId(result.Confirmation, "OPENED");
        if (string.IsNullOrWhiteSpace(dealId))
        {
            return MarkOpenLegTerminalOutcome(
                record,
                index,
                SyntheticExecutionLegState.Unknown,
                $"{epic} confirmation was accepted without a permanent deal ID.");
        }

        return UpdateLeg(
            record,
            index,
            leg => leg with
            {
                State = SyntheticExecutionLegState.Open,
                DealId = dealId,
                FillLevel = result.Confirmation.Level,
                Message = "",
                ConfirmedUtc = _clock.UtcNow,
                UpdatedUtc = _clock.UtcNow,
            },
            record.Legs.Count(leg => leg.State == SyntheticExecutionLegState.Open) + 1 == record.Legs.Count
                ? SyntheticExecutionState.Open
                : SyntheticExecutionState.PartiallyOpen);
    }

    private SyntheticExecutionRecord MarkOpenLegTerminalOutcome(
        SyntheticExecutionRecord record,
        int index,
        SyntheticExecutionLegState state,
        string message)
    {
        var updated = MarkLeg(record, index, state, message);
        return updated with { State = DetermineExecutionState(updated, cancelled: false) };
    }

    private SyntheticExecutionRecord ApplyCloseConfirmation(
        SyntheticExecutionRecord record,
        int index,
        ConfirmationResult result)
    {
        var leg = record.Legs[index];
        if (result.Confirmation is null)
        {
            return MarkLeg(record, index, SyntheticExecutionLegState.Unknown, $"{leg.Epic} close confirmation: {result.Message}");
        }

        if (IsRejected(result.Confirmation.DealStatus))
        {
            return MarkLeg(record, index, SyntheticExecutionLegState.Open, FormatReason(leg.Epic, "close confirmation", result.Confirmation.Reason));
        }

        var closedDealId = FindAffectedDealId(result.Confirmation, "CLOSED");
        if (!closedDealId.Equals(leg.DealId, StringComparison.Ordinal))
        {
            return MarkLeg(record, index, SyntheticExecutionLegState.Unknown, $"{leg.Epic} close was accepted without confirmation for tracked deal {leg.DealId}.");
        }

        return UpdateLeg(
            record,
            index,
            value => value with
            {
                State = SyntheticExecutionLegState.Closed,
                Message = "",
                ClosedUtc = _clock.UtcNow,
                UpdatedUtc = _clock.UtcNow,
            });
    }

    private SyntheticExecutionRecord MarkLeg(
        SyntheticExecutionRecord record,
        int index,
        SyntheticExecutionLegState state,
        string message) =>
        UpdateLeg(
            record,
            index,
            leg => leg with { State = state, Message = message, UpdatedUtc = _clock.UtcNow });

    private SyntheticExecutionRecord UpdateLeg(
        SyntheticExecutionRecord record,
        int index,
        Func<SyntheticExecutionLegRecord, SyntheticExecutionLegRecord> update,
        SyntheticExecutionState? state = null)
    {
        var legs = record.Legs.ToArray();
        legs[index] = update(legs[index]);
        return record with
        {
            State = state ?? record.State,
            UpdatedUtc = _clock.UtcNow,
            Legs = legs,
        };
    }

    private static SyntheticExecutionState DetermineExecutionState(SyntheticExecutionRecord record, bool cancelled)
    {
        if (!cancelled && record.Legs.All(leg => leg.State == SyntheticExecutionLegState.Open)) return SyntheticExecutionState.Open;
        if (record.Legs.Any(leg => leg.State is SyntheticExecutionLegState.Open or SyntheticExecutionLegState.Unknown))
        {
            return SyntheticExecutionState.NeedsAttention;
        }
        if (record.Legs.Any(leg => leg.State == SyntheticExecutionLegState.Rejected)) return SyntheticExecutionState.Rejected;
        return SyntheticExecutionState.NeedsAttention;
    }

    private static SyntheticExecutionState DetermineCloseState(SyntheticExecutionRecord record, bool cancelled)
    {
        var hasClosed = record.Legs.Any(leg => leg.State == SyntheticExecutionLegState.Closed);
        var hasUnresolved = record.Legs.Any(leg => leg.State is SyntheticExecutionLegState.Open or SyntheticExecutionLegState.Unknown or SyntheticExecutionLegState.Closing);
        if (!cancelled && !hasUnresolved) return SyntheticExecutionState.Closed;
        return hasClosed ? SyntheticExecutionState.PartiallyClosed : SyntheticExecutionState.NeedsAttention;
    }

    private static string FindAffectedDealId(CapitalDealConfirmation confirmation, string requiredStatus)
    {
        var affectedDeal = confirmation.AffectedDeals.FirstOrDefault(deal =>
            !string.IsNullOrWhiteSpace(deal.DealId)
            && deal.Status.Equals(requiredStatus, StringComparison.OrdinalIgnoreCase));
        if (affectedDeal is not null) return affectedDeal.DealId;
        return confirmation.DealId;
    }

    private static bool IsAccepted(string status) => status.Equals("ACCEPTED", StringComparison.OrdinalIgnoreCase);

    private static bool IsRejected(string status) => status.Equals("REJECTED", StringComparison.OrdinalIgnoreCase);

    private static string FormatReason(string epic, string operation, string reason) =>
        string.IsNullOrWhiteSpace(reason)
            ? $"{epic} {operation} was rejected."
            : $"{epic} {operation} was rejected: {reason}";

    private sealed record ConfirmationResult(CapitalDealConfirmation? Confirmation, bool Cancelled, string Message);

    private sealed class SystemSyntheticExecutionClock : ISyntheticExecutionClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

        public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken) => Task.Delay(delay, cancellationToken);
    }
}
