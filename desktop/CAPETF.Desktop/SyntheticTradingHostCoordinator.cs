using System.Text.Json;

namespace CAPETF.Desktop;

internal abstract record SyntheticTradingBrowserRequest;

internal sealed record SyntheticPreflightBasketRequest(string Side, decimal BasketNotional)
    : SyntheticTradingBrowserRequest;

internal sealed record SyntheticExecuteBasketRequest(Guid TicketId)
    : SyntheticTradingBrowserRequest;

internal sealed record SyntheticRefreshExecutionsRequest
    : SyntheticTradingBrowserRequest;

internal sealed record SyntheticCloseBasketRequest(string ExecutionId)
    : SyntheticTradingBrowserRequest;

internal sealed record SyntheticShowExecutionBasketRequest(string ExecutionId)
    : SyntheticTradingBrowserRequest;

internal sealed record SyntheticCancelMarginPreviewRequest
    : SyntheticTradingBrowserRequest;

internal sealed record SyntheticPreviewMarginsRequest(decimal BasketNotional)
    : SyntheticTradingBrowserRequest;

internal sealed record SyntheticPreviewOrderRequest(string Side, decimal BasketNotional)
    : SyntheticTradingBrowserRequest;

internal sealed record SyntheticSetRiskPlanRequest(
    string ExecutionId,
    decimal? StopLoss,
    decimal? TakeProfit) : SyntheticTradingBrowserRequest;

internal sealed record SyntheticClearRiskPlanRequest(string ExecutionId)
    : SyntheticTradingBrowserRequest;

internal static class SyntheticTradingBrowserRequestParser
{
    public static bool TryParse(
        string json,
        out SyntheticTradingBrowserRequest? request,
        out string error)
    {
        request = null;
        error = "";
        try
        {
            using var document = JsonDocument.Parse(json);
            return TryParse(document.RootElement, out request, out error);
        }
        catch (Exception exception) when (IsSemanticJsonException(exception))
        {
            error = $"Browser request is invalid: {exception.Message}";
            return false;
        }
    }

    public static bool TryParse(
        JsonElement root,
        out SyntheticTradingBrowserRequest? request,
        out string error)
    {
        request = null;
        error = "";
        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty("type", out var typeValue)
            || typeValue.ValueKind != JsonValueKind.String)
        {
            error = "Trading request type is required.";
            return false;
        }

        var type = typeValue.GetString();
        switch (type)
        {
            case "preflightBasket":
                if (!HasOnlyProperties(root, "type", "side", "basketNotional"))
                {
                    error = "Preflight accepts side and basket notional only.";
                    return false;
                }
                if (!root.TryGetProperty("side", out var sideValue)
                    || sideValue.ValueKind != JsonValueKind.String
                    || string.IsNullOrWhiteSpace(sideValue.GetString())
                    || !root.TryGetProperty("basketNotional", out var notionalValue)
                    || !notionalValue.TryGetDecimal(out var basketNotional))
                {
                    error = "Preflight requires a side and basket notional.";
                    return false;
                }
                request = new SyntheticPreflightBasketRequest(sideValue.GetString()!, basketNotional);
                return true;

            case "executeBasket":
                if (!HasOnlyProperties(root, "type", "ticketId"))
                {
                    error = "Execution accepts a ticket ID only.";
                    return false;
                }
                if (!TryGetGuid(root, "ticketId", out var ticketId))
                {
                    error = "A valid execution ticket ID is required.";
                    return false;
                }
                request = new SyntheticExecuteBasketRequest(ticketId);
                return true;

            case "refreshExecutions":
                if (!HasOnlyProperties(root, "type"))
                {
                    error = "Execution refresh does not accept mutation data.";
                    return false;
                }
                request = new SyntheticRefreshExecutionsRequest();
                return true;

            case "closeBasket":
                if (!HasOnlyProperties(root, "type", "executionId"))
                {
                    error = "Close Basket accepts an execution ID only.";
                    return false;
                }
                if (!root.TryGetProperty("executionId", out var executionIdValue)
                    || executionIdValue.ValueKind != JsonValueKind.String
                    || string.IsNullOrWhiteSpace(executionIdValue.GetString()))
                {
                    error = "A valid execution ID is required.";
                    return false;
                }
                request = new SyntheticCloseBasketRequest(executionIdValue.GetString()!);
                return true;

            case "showExecutionBasket":
                if (!HasOnlyProperties(root, "type", "executionId"))
                {
                    error = "Show on Chart accepts an execution ID only.";
                    return false;
                }
                if (!root.TryGetProperty("executionId", out var showExecutionIdValue)
                    || showExecutionIdValue.ValueKind != JsonValueKind.String
                    || string.IsNullOrWhiteSpace(showExecutionIdValue.GetString()))
                {
                    error = "A valid execution ID is required.";
                    return false;
                }
                request = new SyntheticShowExecutionBasketRequest(showExecutionIdValue.GetString()!);
                return true;

            case "cancelMarginPreview":
                if (!HasOnlyProperties(root, "type"))
                {
                    error = "Margin preview cancellation does not accept input data.";
                    return false;
                }
                request = new SyntheticCancelMarginPreviewRequest();
                return true;

            case "previewMargins":
                if (!HasOnlyProperties(root, "type", "basketNotional")
                    || !TryGetDecimal(root, "basketNotional", out var marginNotional))
                {
                    error = "Margin preview requires a numeric basket notional.";
                    return false;
                }
                request = new SyntheticPreviewMarginsRequest(marginNotional);
                return true;

            case "previewOrder":
                if (!HasOnlyProperties(root, "type", "side", "basketNotional")
                    || !root.TryGetProperty("side", out var orderSideValue)
                    || orderSideValue.ValueKind != JsonValueKind.String
                    || string.IsNullOrWhiteSpace(orderSideValue.GetString())
                    || !TryGetDecimal(root, "basketNotional", out var orderNotional))
                {
                    error = "Order preview requires a side and numeric basket notional.";
                    return false;
                }
                request = new SyntheticPreviewOrderRequest(orderSideValue.GetString()!, orderNotional);
                return true;

            case "setRiskPlan":
                if (!HasOnlyProperties(root, "type", "executionId", "stopLoss", "takeProfit")
                    || !TryGetRequiredString(root, "executionId", out var riskPlanExecutionId)
                    || !TryGetNullableDecimal(root, "stopLoss", out var stopLoss)
                    || !TryGetNullableDecimal(root, "takeProfit", out var takeProfit))
                {
                    error = "Risk plan requires an execution ID and numeric or empty risk levels only.";
                    return false;
                }
                request = new SyntheticSetRiskPlanRequest(riskPlanExecutionId, stopLoss, takeProfit);
                return true;

            case "clearRiskPlan":
                if (!HasOnlyProperties(root, "type", "executionId")
                    || !TryGetRequiredString(root, "executionId", out var clearRiskPlanExecutionId))
                {
                    error = "Risk plan clear requires an execution ID only.";
                    return false;
                }
                request = new SyntheticClearRiskPlanRequest(clearRiskPlanExecutionId);
                return true;

            default:
                error = "Unsupported trading request.";
                return false;
        }
    }

    private static bool HasOnlyProperties(JsonElement root, params string[] allowed)
    {
        var allowedSet = new HashSet<string>(allowed, StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        return root.EnumerateObject().All(property =>
            allowedSet.Contains(property.Name) && seen.Add(property.Name));
    }

    private static bool TryGetGuid(JsonElement root, string propertyName, out Guid value)
    {
        value = default;
        return root.TryGetProperty(propertyName, out var property)
            && property.ValueKind == JsonValueKind.String
            && Guid.TryParse(property.GetString(), out value);
    }

    private static bool TryGetDecimal(JsonElement root, string propertyName, out decimal value)
    {
        value = default;
        return root.TryGetProperty(propertyName, out var property)
            && property.ValueKind == JsonValueKind.Number
            && property.TryGetDecimal(out value);
    }

    private static bool TryGetNullableDecimal(JsonElement root, string propertyName, out decimal? value)
    {
        value = null;
        if (!root.TryGetProperty(propertyName, out var property)) return false;
        if (property.ValueKind == JsonValueKind.Null) return true;
        if (property.ValueKind != JsonValueKind.Number || !property.TryGetDecimal(out var decimalValue)) return false;
        value = decimalValue;
        return true;
    }

    private static bool TryGetRequiredString(JsonElement root, string propertyName, out string value)
    {
        value = "";
        return root.TryGetProperty(propertyName, out var property)
            && property.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(property.GetString())
            && (value = property.GetString()!) is not null;
    }

    internal static bool IsSemanticJsonException(Exception exception) =>
        exception is JsonException or InvalidOperationException or FormatException or OverflowException;
}

internal static class SyntheticTradingBrowserMessageHandler
{
    public static async Task HandleAsync(
        string json,
        Func<SyntheticTradingBrowserRequest, Task> handleRequest,
        Func<string, Task> rejectRequest)
    {
        ArgumentNullException.ThrowIfNull(handleRequest);
        ArgumentNullException.ThrowIfNull(rejectRequest);

        if (!SyntheticTradingBrowserRequestParser.TryParse(json, out var request, out var error))
        {
            await rejectRequest(error);
            return;
        }

        try
        {
            await handleRequest(request!);
        }
        catch (Exception exception) when (SyntheticTradingBrowserRequestParser.IsSemanticJsonException(exception))
        {
            await rejectRequest($"Browser request was rejected: {exception.Message}");
        }
    }
}

internal sealed class SyntheticTradingHostCoordinator : IDisposable
{
    private readonly Dictionary<Guid, SyntheticExecutionTicket> _tickets = [];
    private readonly object _ticketGate = new();
    private readonly SyntheticBasketExecutionService _executionService;
    private readonly SyntheticExecutionStore _store;
    private readonly SyntheticPositionReconciler _reconciler;
    private readonly Func<bool> _isDemoTradingSession;
    private readonly Func<CancellationToken, Task<IReadOnlyList<CapitalOpenPosition>>> _getOpenPositions;
    private readonly Func<DateTimeOffset> _utcNow;
    private readonly Func<string> _currentAccountId;
    private readonly CancellationTokenSource _shutdown = new();
    private int _operationActive;
    private int _disposed;

    public string PersistenceWarning => _store.LastLoadWarning;

    public SyntheticTradingHostCoordinator(
        SyntheticBasketExecutionService executionService,
        SyntheticExecutionStore store,
        SyntheticPositionReconciler reconciler,
        Func<bool> isDemoTradingSession,
        Func<CancellationToken, Task<IReadOnlyList<CapitalOpenPosition>>> getOpenPositions,
        Func<DateTimeOffset>? utcNow = null,
        Func<string>? currentAccountId = null)
    {
        _executionService = executionService ?? throw new ArgumentNullException(nameof(executionService));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _reconciler = reconciler ?? throw new ArgumentNullException(nameof(reconciler));
        _isDemoTradingSession = isDemoTradingSession ?? throw new ArgumentNullException(nameof(isDemoTradingSession));
        _getOpenPositions = getOpenPositions ?? throw new ArgumentNullException(nameof(getOpenPositions));
        _utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
        _currentAccountId = currentAccountId ?? (() => "");
    }

    public SyntheticPreflightResult RegisterPreflight(SyntheticPreflightResult result)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        ArgumentNullException.ThrowIfNull(result);
        if (!result.IsReady || result.Ticket is null) return result;
        if (!Guid.TryParse(result.Ticket.TicketId, out var ticketId))
        {
            throw new InvalidOperationException("Preflight returned an invalid execution ticket ID.");
        }

        var frozen = result.Ticket with
        {
            Legs = Array.AsReadOnly(result.Ticket.Legs.Select(leg => leg with { }).ToArray()),
        };
        lock (_ticketGate)
        {
            PurgeExpiredTicketsLocked();
            if (!_tickets.TryAdd(ticketId, frozen))
            {
                throw new InvalidOperationException("Execution ticket is already registered.");
            }
        }

        return result with { Ticket = frozen };
    }

    public SyntheticHostExecution BeginExecution(Guid ticketId)
    {
        EnterOperation();
        try
        {
            EnsureDemoMutationAllowed();
            SyntheticExecutionTicket ticket;
            lock (_ticketGate)
            {
                if (!_tickets.Remove(ticketId, out ticket!))
                {
                    throw new InvalidOperationException("Execution ticket is missing or has already been used.");
                }
            }

            if (_utcNow() >= ticket.ExpiresUtc)
            {
                throw new InvalidOperationException("Execution ticket has expired. Run preflight again.");
            }
            EnsureAccountOwnership(ticket.AccountId, "execution ticket");

            return new SyntheticHostExecution(this, ticket);
        }
        catch
        {
            ExitOperation();
            throw;
        }
    }

    public async Task<SyntheticExecutionRecord> ExecuteAsync(
        SyntheticHostExecution execution,
        Func<SyntheticExecutionRecord, Task> publishProgress,
        Func<IReadOnlyList<SyntheticExecutionRecord>, Task> publishExecutions,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(execution, publishProgress, publishExecutions, null, cancellationToken);

    public async Task<SyntheticExecutionRecord> ExecuteAsync(
        SyntheticHostExecution execution,
        Func<SyntheticExecutionRecord, Task> publishProgress,
        Func<IReadOnlyList<SyntheticExecutionRecord>, Task> publishExecutions,
        Action? mutationDispatching,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(execution);
        ArgumentNullException.ThrowIfNull(publishProgress);
        ArgumentNullException.ThrowIfNull(publishExecutions);
        if (!ReferenceEquals(execution.Owner, this) || !execution.TryStart())
        {
            throw new InvalidOperationException("Execution ticket ownership is invalid or already started.");
        }

        try
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _shutdown.Token);
            var result = await _executionService.ExecuteAsync(
                execution.Ticket,
                (record, persistenceToken) => PersistThenPublishAsync(record, publishProgress, persistenceToken),
                mutationDispatching,
                linked.Token);
            await PublishStoredExecutionsAsync(publishExecutions, CancellationToken.None);
            return result;
        }
        finally
        {
            execution.Dispose();
        }
    }

    public async Task<SyntheticExecutionRecord> CloseAsync(
        string executionId,
        Func<SyntheticExecutionRecord, Task> publishProgress,
        Func<IReadOnlyList<SyntheticExecutionRecord>, Task> publishExecutions,
        CancellationToken cancellationToken) =>
        await CloseAsync(executionId, publishProgress, publishExecutions, null, cancellationToken);

    public async Task<SyntheticExecutionRecord> CloseAsync(
        string executionId,
        Func<SyntheticExecutionRecord, Task> publishProgress,
        Func<IReadOnlyList<SyntheticExecutionRecord>, Task> publishExecutions,
        Action? mutationDispatching,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(executionId)) throw new ArgumentException("An execution ID is required.", nameof(executionId));
        ArgumentNullException.ThrowIfNull(publishProgress);
        ArgumentNullException.ThrowIfNull(publishExecutions);
        EnterOperation();
        try
        {
            EnsureDemoMutationAllowed();
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _shutdown.Token);
            var records = await _store.LoadAsync(linked.Token);
            var record = records.SingleOrDefault(candidate =>
                candidate.ExecutionId.Equals(executionId, StringComparison.Ordinal))
                ?? throw new InvalidOperationException("Synthetic execution was not found.");
            EnsureAccountOwnership(record.AccountId, "synthetic execution");
            var result = await _executionService.CloseAsync(
                record,
                (transition, persistenceToken) => PersistThenPublishAsync(transition, publishProgress, persistenceToken),
                mutationDispatching,
                linked.Token);
            await PublishStoredExecutionsAsync(publishExecutions, CancellationToken.None);
            return result;
        }
        finally
        {
            ExitOperation();
        }
    }

    public Task<IReadOnlyList<SyntheticExecutionRecord>> RefreshAsync(
        Func<IReadOnlyList<SyntheticExecutionRecord>, Task> publishExecutions,
        CancellationToken cancellationToken) =>
        ReconcileAsync(publishExecutions, cancellationToken, requireDemoSession: true);

    public Task<IReadOnlyList<SyntheticExecutionRecord>> ReconnectAsync(
        Func<IReadOnlyList<SyntheticExecutionRecord>, Task> publishExecutions,
        CancellationToken cancellationToken) =>
        ReconcileAsync(publishExecutions, cancellationToken, requireDemoSession: false);

    public Task<IReadOnlyList<SyntheticExecutionRecord>> PublishStoredAsync(
        Func<IReadOnlyList<SyntheticExecutionRecord>, Task> publishExecutions,
        CancellationToken cancellationToken) =>
        PublishStoredExecutionsAsync(publishExecutions, cancellationToken);

    public void CancelPendingOperations()
    {
        lock (_ticketGate)
        {
            _tickets.Clear();
        }
        try
        {
            _shutdown.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        CancelPendingOperations();
        _shutdown.Dispose();
    }

    internal void ExitOperation()
    {
        Interlocked.Exchange(ref _operationActive, 0);
    }

    private async Task<IReadOnlyList<SyntheticExecutionRecord>> ReconcileAsync(
        Func<IReadOnlyList<SyntheticExecutionRecord>, Task> publishExecutions,
        CancellationToken cancellationToken,
        bool requireDemoSession)
    {
        ArgumentNullException.ThrowIfNull(publishExecutions);
        EnterOperation();
        try
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _shutdown.Token);
            var records = await _store.LoadAsync(linked.Token);
            if (!_isDemoTradingSession())
            {
                if (requireDemoSession)
                {
                    throw new InvalidOperationException("Synthetic trading refresh requires a Capital.com demo session.");
                }
                await publishExecutions(records);
                return records;
            }

            var positions = await _getOpenPositions(linked.Token);
            var now = _utcNow();
            var activeAccountId = _currentAccountId().Trim();
            var positionIds = positions
                .Where(position => !string.IsNullOrWhiteSpace(position.DealId))
                .Select(position => position.DealId)
                .ToHashSet(StringComparer.Ordinal);
            var reconciled = records.Select(record =>
            {
                if (string.IsNullOrWhiteSpace(activeAccountId)) return _reconciler.Reconcile(record, positions, now);
                if (!string.IsNullOrWhiteSpace(record.AccountId))
                {
                    return record.AccountId.Equals(activeAccountId, StringComparison.Ordinal)
                        ? _reconciler.Reconcile(record, positions, now)
                        : record;
                }

                var tracked = record.Legs
                    .Where(leg => leg.State is SyntheticExecutionLegState.Open or SyntheticExecutionLegState.Closing)
                    .Select(leg => leg.DealId)
                    .Where(dealId => !string.IsNullOrWhiteSpace(dealId))
                    .ToArray();
                if (tracked.Length == 0 || tracked.Any(dealId => !positionIds.Contains(dealId))) return record;
                return _reconciler.Reconcile(record with { AccountId = activeAccountId }, positions, now);
            }).ToArray();
            await _store.SaveAsync(reconciled, linked.Token);
            await publishExecutions(reconciled);
            return reconciled;
        }
        finally
        {
            ExitOperation();
        }
    }

    private async Task PersistThenPublishAsync(
        SyntheticExecutionRecord record,
        Func<SyntheticExecutionRecord, Task> publishProgress,
        CancellationToken cancellationToken)
    {
        await _store.UpsertAsync(record, cancellationToken);
        await publishProgress(record);
    }

    private async Task<IReadOnlyList<SyntheticExecutionRecord>> PublishStoredExecutionsAsync(
        Func<IReadOnlyList<SyntheticExecutionRecord>, Task> publishExecutions,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(publishExecutions);
        var records = await _store.LoadAsync(cancellationToken);
        await publishExecutions(records);
        return records;
    }

    private void EnterOperation()
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        if (Interlocked.CompareExchange(ref _operationActive, 1, 0) != 0)
        {
            throw new InvalidOperationException("A synthetic trading operation is already running.");
        }
    }

    private void EnsureDemoMutationAllowed()
    {
        if (!_isDemoTradingSession())
        {
            throw new InvalidOperationException("Synthetic basket mutations require a Capital.com demo session.");
        }
    }

    private void EnsureAccountOwnership(string recordAccountId, string subject)
    {
        var activeAccountId = _currentAccountId().Trim();
        if (string.IsNullOrWhiteSpace(activeAccountId)) return;
        if (!string.IsNullOrWhiteSpace(recordAccountId)
            && recordAccountId.Equals(activeAccountId, StringComparison.Ordinal)) return;
        throw new InvalidOperationException($"The {subject} belongs to a different or unverified Capital.com account.");
    }

    private void PurgeExpiredTicketsLocked()
    {
        var now = _utcNow();
        foreach (var ticketId in _tickets
                     .Where(pair => now >= pair.Value.ExpiresUtc)
                     .Select(pair => pair.Key)
                     .ToArray())
        {
            _tickets.Remove(ticketId);
        }
    }
}

internal sealed class SyntheticHostExecution : IDisposable
{
    private int _started;
    private int _disposed;

    internal SyntheticHostExecution(SyntheticTradingHostCoordinator owner, SyntheticExecutionTicket ticket)
    {
        Owner = owner;
        Ticket = ticket;
    }

    internal SyntheticTradingHostCoordinator Owner { get; }
    internal SyntheticExecutionTicket Ticket { get; }

    internal bool TryStart() => Interlocked.CompareExchange(ref _started, 1, 0) == 0;

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        Owner.ExitOperation();
    }
}
