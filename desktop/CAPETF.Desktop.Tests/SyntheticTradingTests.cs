using CAPETF.Desktop;
using System.Net;
using System.Net.Http;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace CAPETF.Desktop.Tests;

public static class SyntheticTradingTests
{
    public static void RunAll()
    {
        ValidatedSyntheticRiskPlansPersistAcrossStoreInstances();
        DefaultTestSuiteSelectionRunsBothSuitesExactlyOnce();
        TestSuiteSelectionPropagatesEitherSuiteFailure();
        AcceptedBasketSurvivesRestartReconcilesAndClosesWithoutDuplicateMutations();
        PartialExecutionSurvivesRestartWithoutSubmittingOrClosingRemainingLegs();
        SyntheticTradingWorkspaceHasProfessionalDemoContract();
        SyntheticTradingWorkspaceRuntimeUsesHostOwnedTicketsAndOperationLocks();
        TradingBrowserParserAllowsOnlyActionIdentifiersAndPreflightInputs();
        TradingBrowserParserAllowsOnlyRiskPlanIdentifiersAndLevels();
        ExecutionBasketSnapshotPreservesTrustedFormula();
        TradingBrowserParserRejectsMalformedShapesWithoutThrowing();
        TradingBrowserHandlerTurnsMalformedAndSemanticFailuresIntoRejections();
        HostConsumesFrozenTicketsBeforeExecutionAndRejectsReuse();
        HostRejectsExpiredTicketsWithoutMutation();
        HostDuplicateGuardDoesNotConsumeTheBlockedTicket();
        HostDemoGateBlocksExecutionAndCloseMutations();
        HostRejectsCrossAccountExecutionAndCloseMutations();
        HostScopesLegacyExecutionOnlyAfterExactCurrentAccountMatch();
        HostPersistsEveryTransitionBeforePublication();
        HostReconnectReconcilesAndPersistsBeforePublication();
        HostCancellationPreservesAcknowledgedExecutionState();
        WindowLifecycleDefersCloseUntilAcknowledgedSaveCompletes();
        WindowLifecycleBoundsOnlyPreDispatchWait();
        WpfHostPublishesTradingContractsWithoutLegacyPreviewMutation();
        WpfHostRejectsUnknownRiskPlanClearWithoutPersisting();
        FreshPreflightSnapshotsRejectFailedRefreshDespiteStaleMetadata();
        FreshPreflightSnapshotsRejectIncompleteCurrentMetadata();
        FreshPreflightSnapshotsBuildDetachedBasketFromExactResponses();
        PreflightRejectsNonDemoSessions();
        PreflightRejectsNonHedgingAccounts();
        PreflightRejectsInvalidComponentCounts();
        PreflightRejectsDuplicateEpics();
        PreflightReturnsLegFailuresInEpicOrder();
        PreflightRejectsZeroAndStaleQuotes();
        PreflightRejectsInvalidRoundedSize();
        PreflightRejectsMissingMargin();
        PreflightRejectsInsufficientFunds();
        PreflightCreatesFrozenTicketWithReversedNegativeLeg();
        ExecutionWaitsForAcceptedConfirmationBeforeSubmittingNextLeg();
        AcceptedConfirmationRequiresExplicitOpenedAffectedDeal();
        ExplicitRejectionStopsUnsentLegs();
        MalformedAcknowledgementStopsWithoutRetry();
        ConfirmationTimeoutIsUnknownWithoutRetry();
        GenericCreateFailureIsUnknownWithoutRetry();
        AmbiguousMutationFailureIsUnknownWithoutRetry();
        CancellationStopsUnsentLegsAfterAcceptedLeg();
        PartialSuccessRemainsOpenWithoutRollback();
        CloseConfirmsOnlyTrackedOpenDealIds();
        PartialClosePreservesRemainingOpenLeg();
        GenericCloseFailureIsUnknownAndCannotBeRetriedBlindly();
        MalformedCloseAcknowledgementIsUnknownAndCannotBeRetriedBlindly();
        CreateAcknowledgementPersistenceIgnoresCallerCancellation();
        AcceptedCreatePersistenceIgnoresCallerCancellation();
        CloseAcknowledgementPersistenceIgnoresCallerCancellation();
        AcceptedClosePersistenceIgnoresCallerCancellation();
        CancellationDuringLaterCreatePersistenceStopsBeforeGateway();
        CancellationDuringLaterClosePersistenceStopsBeforeGateway();
        ProductionTransportDisablesAutomaticRedirects();
        LiveMutationIsRejectedBeforeItIsSent();
        DemoPositionRequestUsesCapitalContract();
        LostCreateResponseRecoversUniqueNewPositionWithoutRetry();
        MalformedCreateResponseRecoversUniqueNewPositionWithoutRetry();
        DemoPositionRedirectDoesNotReachRedirectTarget();
        DealConfirmationParsesRequiredFields();
        OpenPositionsParseRequiredFields();
        BrokerAccountParsesTradingTotals();
        WorkingOrdersParseRequiredFields();
        DemoClosePositionUsesDeleteWithoutRetry();
        DemoCloseRedirectDoesNotReachRedirectTarget();
        ExecutionStoreRoundTripsVersionedRecordsAndDealIdentity();
        ExecutionStoreUpsertsAtomicallyWithoutCredentials();
        ExecutionStoreCoordinatesConcurrentInstancesWithoutLosingDealIdentity();
        ExecutionStoreQuarantinesMalformedFiles();
        ExecutionStoreQuarantinesStructurallyInvalidExecutions();
        ExecutionStoreQuarantinesInvalidLegs();
        ExecutionStoreQuarantinesLegsWithMissingDirection();
        ExecutionStoreQuarantinesDuplicateTrackedDealIds();
        ExecutionStoreQuarantinesClosedLegWithoutClosedTimestamp();
        ExecutionStoreQuarantinesOpenExecutionWithoutOpenLegs();
        ExecutionStoreQuarantinesInconsistentLegStateFields();
        ExecutionStoreQuarantinesInconsistentExecutionStateMix();
        ExecutionStoreQuarantinesNegativeTemporalOrdering();
        ExecutionStoreAcceptsStateMachineProgressSnapshots();
        ExecutionStoreAcceptsClosedPartialExecutionState();
        EmittedExecutionAndReconciliationStatesSatisfyPersistenceContract();
        ReconciliationMatchesOpenPositionsByDealIdAndUpdatesUpl();
        ReconciliationMarksMissingOpenPositionsClosed();
        ReconciliationNormalizesSubmittedLegWhenOpenPositionDisappearsAndPersistsIt();
        ReconciliationNormalizesConfirmingLegWhenOpenPositionDisappearsAndPersistsIt();
        ReconciliationReopensClosedLegWithoutClosureMetadataAndPersistsIt();
        ReconciliationLeavesUnresolvedUnknownUntilPositivelyMatched();
        ReconciliationClosesUnknownTrackedDealWhenCapitalNoLongerListsIt();
        ReconciliationMapsRejectedOpenPendingToNeedsAttentionAndPersistsIt();
    }

    private static void ValidatedSyntheticRiskPlansPersistAcrossStoreInstances()
    {
        var buy = SyntheticRiskPlanValidation.Validate("execution-1", "basket-1", "buy", 100m, 92m, 118m);
        AssertTrue(buy.IsValid, "BUY plan surrounds entry");
        AssertEqual("BUY", buy.Plan!.Side, "side is normalized");
        AssertFalse(SyntheticRiskPlanValidation.Validate("execution-1", "basket-1", "BUY", 100m, 105m, 118m).IsValid,
            "BUY stop must remain below entry");
        AssertFalse(SyntheticRiskPlanValidation.Validate("execution-1", "basket-1", "SELL", 100m, 92m, 118m).IsValid,
            "SELL levels must surround entry in reverse order");
        AssertTrue(SyntheticRiskPlanValidation.Validate("execution-1", "basket-1", "SELL", 100m, 118m, 92m).IsValid,
            "SELL plan surrounds entry");
        AssertTrue(SyntheticRiskPlanValidation.Validate("execution-1", "basket-1", "BUY", 100m, null, 118m).IsValid,
            "BUY stop may be empty");
        AssertTrue(SyntheticRiskPlanValidation.Validate("execution-1", "basket-1", "SELL", 100m, 118m, null).IsValid,
            "SELL take profit may be empty");
        AssertFalse(SyntheticRiskPlanValidation.Validate("execution-1", "basket-1", "BUY", 0m, 92m, 118m).IsValid,
            "zero entry is rejected");
        AssertFalse(SyntheticRiskPlanValidation.Validate("execution-1", "basket-1", "BUY", 100m, -1m, 118m).IsValid,
            "negative stop is rejected");
        AssertFalse(SyntheticRiskPlanValidation.Validate("execution-1", "basket-1", "BUY", decimal.MaxValue, decimal.MaxValue, null).IsValid,
            "decimal boundary values that cannot form a valid risk relationship are rejected");

        using var directory = new TemporaryDirectory();
        var path = System.IO.Path.Combine(directory.Path, "synthetic-risk-plans.json");
        var store = new SyntheticRiskPlanStore(path);
        store.Upsert(buy.Plan);
        AssertEqual(JsonSerializer.Serialize(buy.Plan), JsonSerializer.Serialize(new SyntheticRiskPlanStore(path).LoadAll().Single()),
            "risk plan persists exactly");
    }

    private static void DefaultTestSuiteSelectionRunsBothSuitesExactlyOnce()
    {
        var calls = new List<string>();

        var completed = TestSuiteRunner.Run(
            [],
            () => calls.Add("trading"),
            () => calls.Add("builder"));

        AssertEqual("trading|builder", string.Join("|", calls), "default test entrypoint executes both suites once");
        AssertEqual("SyntheticTrading|SyntheticBasketBuilder", string.Join("|", completed), "default test entrypoint reports both suites");

        calls.Clear();
        completed = TestSuiteRunner.Run(
            ["trading"],
            () => calls.Add("trading"),
            () => calls.Add("builder"));
        AssertEqual("trading", string.Join("|", calls), "focused trading filter");
        AssertEqual("SyntheticTrading", string.Join("|", completed), "focused trading completion");

        calls.Clear();
        completed = TestSuiteRunner.Run(
            ["builder"],
            () => calls.Add("trading"),
            () => calls.Add("builder"));
        AssertEqual("builder", string.Join("|", calls), "focused builder filter");
        AssertEqual("SyntheticBasketBuilder", string.Join("|", completed), "focused builder completion");
    }

    private static void TestSuiteSelectionPropagatesEitherSuiteFailure()
    {
        var builderCalls = 0;
        var tradingFailure = AssertThrows<InvalidOperationException>(
            () => TestSuiteRunner.Run(
                [],
                () => throw new InvalidOperationException("trading failed"),
                () => builderCalls++),
            "default test entrypoint must fail when trading fails");
        AssertEqual("trading failed", tradingFailure.Message, "trading suite failure identity");
        AssertEqual(0, builderCalls, "a failed trading suite stops the full run");

        var builderFailure = AssertThrows<InvalidOperationException>(
            () => TestSuiteRunner.Run(
                [],
                () => { },
                () => throw new InvalidOperationException("builder failed")),
            "default test entrypoint must fail when builder fails");
        AssertEqual("builder failed", builderFailure.Message, "builder suite failure identity");
    }

    private static void AcceptedBasketSurvivesRestartReconcilesAndClosesWithoutDuplicateMutations()
    {
        using var directory = new TemporaryDirectory();
        var storePath = Path.Combine(directory.Path, "executions.json");
        var gateway = AcceptedExecutionGateway("ALPHA", "BETA", "GAMMA");
        var lifecycleEvents = new List<string>();
        gateway.ObserveCall = lifecycleEvents.Add;
        var preflight = SyntheticTradePreflight.Build(CreateThreeLegPreflightInput());
        AssertTrue(preflight.IsReady, "accepted lifecycle preflight must be ready");
        AssertTrue(preflight.Ticket is not null, "accepted lifecycle preflight ticket");
        var ticketId = Guid.Parse(preflight.Ticket!.TicketId);
        SyntheticExecutionRecord opened;

        using (var coordinator = SyntheticTradingComposition.CreateCoordinator(
                   gateway,
                   storePath,
                   () => true,
                   _ => Task.FromResult<IReadOnlyList<CapitalOpenPosition>>([]),
                   new TestExecutionClock()))
        {
            coordinator.RegisterPreflight(preflight);
            using var execution = coordinator.BeginExecution(ticketId);
            opened = coordinator.ExecuteAsync(
                    execution,
                    record => ObservePersistedProgress(storePath, record, lifecycleEvents),
                    records => ObservePersistedExecutions(storePath, records, lifecycleEvents),
                    default)
                .GetAwaiter().GetResult();

            AssertThrows<InvalidOperationException>(
                () => coordinator.BeginExecution(ticketId),
                "an executed preflight ticket must remain one-time only");
        }

        AssertEqual(SyntheticExecutionState.Open, opened.State, "three accepted legs open the basket");
        AssertSequence(
            gateway.Calls,
            "POST:ALPHA", "CONFIRM:o_alpha",
            "POST:BETA", "CONFIRM:o_beta",
            "POST:GAMMA", "CONFIRM:o_gamma");
        AssertEqual("BUY|SELL|BUY", string.Join("|", gateway.PostRequests.Select(request => request.Direction)), "frozen ticket leg directions");
        AssertEqual(3, gateway.CreateInvocations, "accepted lifecycle submits each leg exactly once");
        AssertEqual("o_alpha|o_beta|o_gamma", string.Join("|", opened.Legs.Select(leg => leg.DealReference)), "accepted deal references");
        AssertEqual("d_alpha|d_beta|d_gamma", string.Join("|", opened.Legs.Select(leg => leg.DealId)), "accepted permanent deal IDs");
        AssertEqual(0, Directory.GetFiles(directory.Path, "executions.json.tmp*").Length, "atomic store leaves no temporary file");
        AssertImmediatelyBefore(lifecycleEvents, "PUBLISH:Submitting|Submitted,Pending,Pending", "POST:ALPHA");
        AssertImmediatelyBefore(lifecycleEvents, "PUBLISH:Submitting|Confirming,Pending,Pending", "CONFIRM:o_alpha");
        AssertImmediatelyBefore(lifecycleEvents, "PUBLISH:PartiallyOpen|Open,Submitted,Pending", "POST:BETA");
        AssertImmediatelyBefore(lifecycleEvents, "PUBLISH:PartiallyOpen|Open,Confirming,Pending", "CONFIRM:o_beta");
        AssertImmediatelyBefore(lifecycleEvents, "PUBLISH:PartiallyOpen|Open,Open,Submitted", "POST:GAMMA");
        AssertImmediatelyBefore(lifecycleEvents, "PUBLISH:PartiallyOpen|Open,Open,Confirming", "CONFIRM:o_gamma");
        var persistedOpen = AssertSingle(
            new SyntheticExecutionStore(storePath).LoadAsync(default).GetAwaiter().GetResult(),
            "accepted execution must persist through a fresh store instance");
        AssertEqual(JsonSerializer.Serialize(opened), JsonSerializer.Serialize(persistedOpen), "open execution survives persistence unchanged");

        gateway.ClearCalls();
        lifecycleEvents.Clear();
        var restartTime = DateTimeOffset.Parse("2026-07-30T13:00:00Z");
        IReadOnlyList<CapitalOpenPosition> remotePositions =
        [
            new("d_alpha", "ALPHA", "BUY", 20m, 101.25m, 11.25m, "USD", "TRADEABLE"),
            new("d_beta", "BETA", "SELL", 20m, 101.25m, 12.50m, "USD", "TRADEABLE"),
            new("d_gamma", "GAMMA", "BUY", 20m, 101.25m, 13.75m, "USD", "TRADEABLE"),
        ];
        using var restarted = SyntheticTradingComposition.CreateCoordinator(
            gateway,
            storePath,
            () => true,
            _ => Task.FromResult(remotePositions),
            new TestExecutionClock(restartTime));
        var reconciled = AssertSingle(
            restarted.ReconnectAsync(
                    records => ObservePersistedExecutions(storePath, records, lifecycleEvents),
                    default)
                .GetAwaiter().GetResult(),
            "restart must reconcile the persisted execution");

        var expectedReconciled = persistedOpen with
        {
            UpdatedUtc = restartTime,
            Legs = persistedOpen.Legs.Select((leg, index) => leg with
            {
                UpdatedUtc = restartTime,
                CurrentUnrealizedProfitLoss = new[] { 11.25m, 12.50m, 13.75m }[index],
            }).ToArray(),
        };
        AssertEqual(JsonSerializer.Serialize(expectedReconciled), JsonSerializer.Serialize(reconciled), "restart preserves every accepted execution field, timestamp, leg order, and deal identity");
        AssertEqual(0, gateway.CreateInvocations, "restart reconciliation must not resubmit positions");
        AssertEqual(0, gateway.CloseInvocations, "restart reconciliation must not close positions");
        AssertEqual("LIST:Open|Open,Open,Open", string.Join("|", lifecycleEvents), "restart publishes only after reconciled state is persisted");

        lifecycleEvents.Clear();
        foreach (var leg in reconciled.Legs)
        {
            var suffix = leg.Epic.ToLowerInvariant();
            gateway.CloseResults.Enqueue(() => Task.FromResult(Acknowledgement($"c_{suffix}")));
            gateway.ConfirmResults[$"c_{suffix}"] = new Queue<Func<Task<CapitalDealConfirmation>>>(
                [() => Task.FromResult(ClosedConfirmation($"c_{suffix}", $"d_{suffix}"))]);
        }

        var closed = restarted.CloseAsync(
                reconciled.ExecutionId,
                record => ObservePersistedProgress(storePath, record, lifecycleEvents),
                records => ObservePersistedExecutions(storePath, records, lifecycleEvents),
                default)
            .GetAwaiter().GetResult();

        AssertSequence(
            gateway.Calls,
            "CLOSE:d_alpha", "CONFIRM:c_alpha",
            "CLOSE:d_beta", "CONFIRM:c_beta",
            "CLOSE:d_gamma", "CONFIRM:c_gamma");
        AssertEqual(3, gateway.CloseInvocations, "close submits each tracked position exactly once");
        AssertEqual(SyntheticExecutionState.Closed, closed.State, "accepted lifecycle closes after all confirmations");
        AssertTrue(closed.Legs.All(leg => leg.State == SyntheticExecutionLegState.Closed), "all accepted legs are durably closed");
        AssertImmediatelyBefore(lifecycleEvents, "PUBLISH:Closing|Closing,Open,Open", "CLOSE:d_alpha");
        AssertImmediatelyBefore(lifecycleEvents, "PUBLISH:Closing|Closing,Open,Open", "CONFIRM:c_alpha");
        AssertImmediatelyBefore(lifecycleEvents, "PUBLISH:Closing|Closed,Closing,Open", "CLOSE:d_beta");
        AssertImmediatelyBefore(lifecycleEvents, "PUBLISH:Closing|Closed,Closing,Open", "CONFIRM:c_beta");
        AssertImmediatelyBefore(lifecycleEvents, "PUBLISH:Closing|Closed,Closed,Closing", "CLOSE:d_gamma");
        AssertImmediatelyBefore(lifecycleEvents, "PUBLISH:Closing|Closed,Closed,Closing", "CONFIRM:c_gamma");
        var persistedClosed = AssertSingle(
            new SyntheticExecutionStore(storePath).LoadAsync(default).GetAwaiter().GetResult(),
            "closed execution must persist through a fresh store instance");
        AssertEqual(JsonSerializer.Serialize(closed), JsonSerializer.Serialize(persistedClosed), "closed execution survives persistence unchanged");
    }

    private static void PartialExecutionSurvivesRestartWithoutSubmittingOrClosingRemainingLegs()
    {
        using var directory = new TemporaryDirectory();
        var storePath = Path.Combine(directory.Path, "executions.json");
        var gateway = new ScriptedTradingGateway();
        var lifecycleEvents = new List<string>();
        gateway.ObserveCall = lifecycleEvents.Add;
        gateway.PostResults.Enqueue(() => Task.FromResult(Acknowledgement("partial-alpha")));
        gateway.PostResults.Enqueue(() => Task.FromResult(Acknowledgement("partial-beta")));
        gateway.ConfirmResults["partial-alpha"] = new Queue<Func<Task<CapitalDealConfirmation>>>(
            [() => Task.FromResult(AcceptedConfirmation("partial-alpha", "partial-deal-alpha"))]);
        gateway.ConfirmResults["partial-beta"] = new Queue<Func<Task<CapitalDealConfirmation>>>(
            [() => Task.FromResult(RejectedConfirmation("partial-beta", "MARKET_CLOSED"))]);
        var preflight = SyntheticTradePreflight.Build(CreateThreeLegPreflightInput());
        var ticketId = Guid.Parse(preflight.Ticket?.TicketId ?? throw new Exception("partial lifecycle preflight ticket missing"));
        SyntheticExecutionRecord partial;

        using (var coordinator = SyntheticTradingComposition.CreateCoordinator(
                   gateway,
                   storePath,
                   () => true,
                   _ => Task.FromResult<IReadOnlyList<CapitalOpenPosition>>([]),
                   new TestExecutionClock()))
        {
            coordinator.RegisterPreflight(preflight);
            using var execution = coordinator.BeginExecution(ticketId);
            partial = coordinator.ExecuteAsync(
                    execution,
                    record => ObservePersistedProgress(storePath, record, lifecycleEvents),
                    records => ObservePersistedExecutions(storePath, records, lifecycleEvents),
                    default)
                .GetAwaiter().GetResult();
        }

        AssertSequence(gateway.Calls, "POST:ALPHA", "CONFIRM:partial-alpha", "POST:BETA", "CONFIRM:partial-beta");
        AssertEqual(2, gateway.CreateInvocations, "rejection stops before the third create mutation");
        AssertEqual(0, gateway.CloseInvocations, "partial execution must never auto-rollback the accepted leg");
        AssertEqual(SyntheticExecutionState.NeedsAttention, partial.State, "partial execution requires attention");
        AssertEqual(
            "Open|Rejected|Pending",
            string.Join("|", partial.Legs.Select(leg => leg.State)),
            "partial execution durable leg states");
        AssertEqual("partial-deal-alpha", partial.Legs[0].DealId, "accepted partial leg keeps its permanent deal ID");
        AssertEqual("", partial.Legs[2].DealReference, "unsent third leg has no mutation identity");
        AssertImmediatelyBefore(lifecycleEvents, "PUBLISH:Submitting|Submitted,Pending,Pending", "POST:ALPHA");
        AssertImmediatelyBefore(lifecycleEvents, "PUBLISH:Submitting|Confirming,Pending,Pending", "CONFIRM:partial-alpha");
        AssertImmediatelyBefore(lifecycleEvents, "PUBLISH:PartiallyOpen|Open,Submitted,Pending", "POST:BETA");
        AssertImmediatelyBefore(lifecycleEvents, "PUBLISH:PartiallyOpen|Open,Confirming,Pending", "CONFIRM:partial-beta");
        var persistedPartial = AssertSingle(
            new SyntheticExecutionStore(storePath).LoadAsync(default).GetAwaiter().GetResult(),
            "partial execution must persist through a fresh store instance");
        AssertEqual(JsonSerializer.Serialize(partial), JsonSerializer.Serialize(persistedPartial), "complete partial execution survives persistence unchanged");

        gateway.ClearCalls();
        lifecycleEvents.Clear();
        var restartTime = DateTimeOffset.Parse("2026-07-30T13:00:00Z");
        IReadOnlyList<CapitalOpenPosition> remotePositions =
        [
            new("partial-deal-alpha", "ALPHA", "BUY", 20m, 101.25m, 9.75m, "USD", "TRADEABLE"),
        ];
        using var restarted = SyntheticTradingComposition.CreateCoordinator(
            gateway,
            storePath,
            () => true,
            _ => Task.FromResult(remotePositions),
            new TestExecutionClock(restartTime));
        var reconciled = AssertSingle(
            restarted.ReconnectAsync(
                    records => ObservePersistedExecutions(storePath, records, lifecycleEvents),
                    default)
                .GetAwaiter().GetResult(),
            "partial execution must survive restart");

        var expectedReconciled = persistedPartial with
        {
            UpdatedUtc = restartTime,
            Legs =
            [
                persistedPartial.Legs[0] with { UpdatedUtc = restartTime, CurrentUnrealizedProfitLoss = 9.75m },
                persistedPartial.Legs[1],
                persistedPartial.Legs[2],
            ],
        };
        AssertEqual(JsonSerializer.Serialize(expectedReconciled), JsonSerializer.Serialize(reconciled), "restart preserves every partial execution field, timestamp, leg order, and deal reference");
        AssertEqual(0, gateway.Calls.Count, "reconciliation performs no create, confirm, or close mutation");
        AssertEqual(0, gateway.CreateInvocations, "restart does not retry rejected or unsent legs");
        AssertEqual(0, gateway.CloseInvocations, "restart does not auto-close the accepted leg");
        AssertEqual("LIST:NeedsAttention|Open,Rejected,Pending", string.Join("|", lifecycleEvents), "partial restart publishes only after reconciled state is persisted");
        var persisted = AssertSingle(
            new SyntheticExecutionStore(storePath).LoadAsync(default).GetAwaiter().GetResult(),
            "reconciled partial execution must persist through a fresh store instance");
        AssertEqual(JsonSerializer.Serialize(reconciled), JsonSerializer.Serialize(persisted), "reconciled partial state is durable");
    }

    private static void SyntheticTradingWorkspaceHasProfessionalDemoContract()
    {
        var html = ReadRepositoryFile("desktop", "CAPETF.Desktop", "Assets", "synthetic-terminal.html");
        foreach (var required in new[]
        {
            "DEMO TRADING",
            "Place Buy Basket",
            "Place Sell Basket",
            "id=\"trade-readiness\"",
            "id=\"trade-confirmation\"",
            "id=\"confirmation-legs\"",
            "id=\"partial-execution-ack\"",
            "id=\"confirm-execution\"",
            "id=\"close-confirmation\"",
            "id=\"close-confirmation-legs\"",
            "id=\"partial-close-ack\"",
            "id=\"confirm-close\"",
            "id=\"refresh-executions\"",
            "setTerminalPreflight",
            "setTerminalExecutions",
            "setTerminalExecutionProgress",
            "setTerminalTradingMode",
            "setTerminalBrokerSnapshot",
            "Needs attention",
            "Close Basket",
            "Show on Chart",
            "showExecutionBasket",
            "<summary>Formula</summary>",
            "<summary>Preflight</summary>",
            "<summary>Execution</summary>",
            "Open Positions",
            "Pending Orders",
            "id=\"trade-dock\"",
            "id=\"trade-dock-splitter\"",
            "id=\"trade-dock-minimize\"",
            "id=\"trade-tab-positions\"",
            "id=\"trade-tab-pending\"",
            "id=\"trade-tab-baskets\"",
            "id=\"trade-tab-history\"",
            "id=\"trade-dock-table\"",
            "id=\"trade-dock-account-strip\"",
            "Running P/L",
            "Funds",
            "Equity",
            "Entry",
            "Stop loss",
            "Take profit",
            "<summary>Audit</summary>",
            "overflow-y: auto",
            "position: sticky",
        })
        {
            AssertContains(html, required, $"professional trading workspace contract {required}");
        }

        AssertFalse(html.Contains("Preview only", StringComparison.OrdinalIgnoreCase),
            "the executable demo workspace must not describe placement as preview-only");
    }

    private static void SyntheticTradingWorkspaceRuntimeUsesHostOwnedTicketsAndOperationLocks()
    {
        var htmlPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "CAPETF.Desktop", "Assets", "synthetic-terminal.html"));
        const string script = """
            const assert = require('node:assert/strict');
            const fs = require('node:fs');
            const vm = require('node:vm');

            class FakeClassList {
              constructor() { this.values = new Set(); }
              add(...values) { values.forEach(value => this.values.add(value)); }
              remove(...values) { values.forEach(value => this.values.delete(value)); }
              contains(value) { return this.values.has(value); }
              toggle(value, force) {
                const enabled = force === undefined ? !this.values.has(value) : !!force;
                if (enabled) this.values.add(value); else this.values.delete(value);
                return enabled;
              }
            }

            class FakeElement {
              constructor(id = '') {
                this._id = '';
                this.id = id;
                this.dataset = {};
                this.classList = new FakeClassList();
                this.style = { display: '', setProperty() {} };
                this.textContent = '';
                this.value = id === 'quantity' ? '300' : '';
                this.checked = false;
                this.disabled = false;
                this.open = false;
                this.attributes = new Map();
                this.children = [];
                this.listeners = new Map();
              }
              get id() { return this._id; }
              set id(value) {
                this._id = String(value || '');
                if (this._id) elements.set(this._id, this);
              }
              addEventListener(name, callback) {
                if (!this.listeners.has(name)) this.listeners.set(name, []);
                this.listeners.get(name).push(callback);
              }
              removeEventListener() {}
              dispatch(name, extra = {}) {
                const event = { target: this, currentTarget: this, preventDefault() {}, ...extra };
                for (const callback of this.listeners.get(name) || []) callback(event);
              }
              click() { this.dispatch('click'); }
              setAttribute(name, value) { this.attributes.set(name, String(value)); }
              getAttribute(name) { return this.attributes.get(name) || null; }
              removeAttribute(name) { this.attributes.delete(name); }
              showModal() { this.open = true; }
              close() { this.open = false; }
              getBoundingClientRect() { return { left: 0, top: 0, right: 800, width: 800, height: 600 }; }
              hasPointerCapture() { return false; }
              setPointerCapture() {}
              releasePointerCapture() {}
              focus() { document.activeElement = this; }
              appendChild(child) { this.children.push(child); return child; }
              replaceChildren(...children) { this.children = [...children]; this.textContent = ''; }
            }

            const elements = new Map();
            const element = id => {
              if (!elements.has(id)) elements.set(id, new FakeElement(id));
              return elements.get(id);
            };
            global.window = globalThis;
            global.document = {
              activeElement: null,
              body: new FakeElement('body'),
              documentElement: new FakeElement('html'),
              createElement() { return new FakeElement(); },
              getElementById: element,
              querySelectorAll() { return []; }
            };
            const storedValues = new Map();
            global.localStorage = {
              getItem(key) { return storedValues.has(key) ? storedValues.get(key) : null; },
              setItem(key, value) { storedValues.set(key, String(value)); },
              removeItem(key) { storedValues.delete(key); }
            };
            global.confirm = () => true;
            global.addEventListener = () => {};
            global.removeEventListener = () => {};
            global.requestAnimationFrame = callback => { callback(); return 1; };
            global.cancelAnimationFrame = () => {};
            global.setTimeout = () => 1;
            global.clearTimeout = () => {};
            global.innerHeight = 800;

            const descendantText = node => [node.textContent, ...node.children.map(descendantText)].filter(Boolean).join(' ');

            const messages = [];
            const allMessages = [];
            global.chrome = { webview: { postMessage(message) { messages.push(message); allMessages.push(message); } } };
            function makeSeries() {
              return {
                setData() {}, update() {}, createPriceLine() { return {}; }, removePriceLine() {},
                priceToCoordinate(value) { return Number(value); }, coordinateToPrice(value) { return Number(value); },
                attachPrimitive() {}, detachPrimitive() {}
              };
            }
            const timeScale = {
              fitContent() {}, scrollToRealTime() {}, applyOptions() {}, scrollPosition() { return 0; },
              scrollToPosition() {}, timeToCoordinate(value) { return Number(value); }, coordinateToTime(value) { return Number(value); }
            };
            global.LightweightCharts = {
              CandlestickSeries: {}, LineSeries: {}, CrosshairMode: { Normal: 0 }, version() { return 'test'; },
              createChart() {
                return {
                  addSeries() { return makeSeries(); }, timeScale() { return timeScale; },
                  priceScale() { return { applyOptions() {} }; }, subscribeCrosshairMove() {}, resize() {}
                };
              }
            };
            const fakeDrawingManager = {
              updateContext() {}, getRecords() { return []; }, setRecords() {}, setTool() { return true; },
              cancel() {}, undo() {}, redo() {}, setMagnet() {}, setLocked() {}, setVisible() {},
              setStyle() {}, clear() {}, getState() { return {}; }
            };
            global.CapComDrawings = {
              createManager() { return fakeDrawingManager; },
              createTextDialogController() { return { open() {} }; },
              switchDrawingIdentity(_manager, _storage, _oldIdentity, nextIdentity) { return nextIdentity; },
              persistStoredRecords() { return true; }
            };
            global.lucide = { createIcons() {} };

            const html = fs.readFileSync(process.argv[2], 'utf8');
            const scripts = [...html.matchAll(/<script>([\s\S]*?)<\/script>/g)];
            vm.runInThisContext(scripts.at(-1)[1], { filename: process.argv[2] });

            setTerminalTradingMode({ IsDemo: true, IsExecutionEnabled: true, AccountId: 'DEMO-1', AccountCurrency: 'USDd', Label: 'DEMO TRADING' });
            setTerminalData({
              DrawingIdentity: 'basket-1', Symbol: 'SYN-1', Currency: 'USD', Candles: [],
              Components: [{ Epic: 'AAPL' }, { Epic: 'MSFT' }, { Epic: 'NVDA' }]
            });
            setTerminalBrokerSnapshot({
              RetrievedAt: '2026-07-31T15:00:00Z',
              Account: { Currency: 'USDd', Balance: 20993.11, Deposit: 21000, ProfitLoss: -6.89, Available: 20684.88 },
              Positions: [{ DealId: 'DEAL-AAPL', Epic: 'AAPL', Direction: 'BUY', Size: 0.9, Level: 123.45, UnrealizedProfitLoss: -2.26, Currency: 'USDd', StopLevel: 118, ProfitLevel: 140, Bid: 123.4, Offer: 123.5 }],
              WorkingOrders: [{ DealId: 'ORDER-AAPL', Epic: 'AAPL', Direction: 'SELL', Size: 1, OrderLevel: 130, OrderType: 'LIMIT', StopLevel: 140, ProfitLevel: 110 }]
            });
            assert.match(element('broker-profit-loss').textContent, /-6\.89/);
            assert.match(element('broker-margin-used').textContent, /308\.23/);
            assert.match(element('broker-deposit').textContent, /21000\.00/);
            assert.match(element('broker-position-list').children[0].textContent, /Entry 123\.4500.*Running P\/L USDd -2\.26.*Stop loss 118\.0000.*Take profit 140\.0000/);
            assert.match(element('working-order-list').children[0].textContent, /LIMIT 130\.0000.*ORDER-AAPL/);
            messages.length = 0;
            allMessages.length = 0;
            setTerminalBusy(true, 'Refreshing positions');
            assert.equal(element('place-buy-basket').disabled, true);
            assert.equal(element('place-sell-basket').disabled, true);
            setTerminalBusy(false, '');
            setTerminalPreflight({ IsReady: false, Ticket: null, Failures: [{ Epic: 'AAPL', Reason: 'Market closed' }] });
            assert.equal(element('trade-readiness').textContent, 'Market closed');
            element('place-buy-basket').click();
            assert.deepEqual(messages.pop(), { type: 'preflightBasket', side: 'BUY', basketNotional: 300 });
            setTerminalBusy(true, 'Preflighting synthetic basket');
            setTerminalBusy(false, '');
            assert.equal(element('place-buy-basket').disabled, false, 'host completion after failure must release the browser lock');
            element('place-buy-basket').click();
            assert.deepEqual(messages.pop(), { type: 'preflightBasket', side: 'BUY', basketNotional: 300 });

            setTerminalPreflight({
              IsReady: true,
              Ticket: {
                TicketId: 'host-ticket-1', BasketId: 'basket-1', Side: 'BUY', RequestedNotional: 300,
                EstimatedMargin: 72.5, MarginCurrency: 'USDd', ExpiresUtc: '2026-07-30T12:02:00Z',
                Legs: [
                  { Direction: 'BUY', Epic: 'AAPL', Quantity: 0.1, ReferencePrice: 201.25 },
                  { Direction: 'SELL', Epic: 'MSFT', Quantity: 0.2, ReferencePrice: 502.5 },
                  { Direction: 'BUY', Epic: 'NVDA', Quantity: 0.3, ReferencePrice: 180.75 }
                ]
              },
              Failures: []
            });
            assert.equal(element('trade-confirmation').open, true);
            assert.equal(element('confirmation-legs').children.length, 3);
            assert.equal(element('confirm-execution').disabled, true);
            element('partial-execution-ack').checked = true;
            element('partial-execution-ack').dispatch('change');
            assert.equal(element('confirm-execution').disabled, false);
            setTerminalData({
              DrawingIdentity: 'basket-2', Symbol: 'SYN-2', Currency: 'USD', Candles: [],
              Components: [{ Epic: 'ADBE' }, { Epic: 'CRM' }, { Epic: 'ORCL' }]
            });
            assert.equal(element('trade-confirmation').open, false, 'basket replacement must close the stale ticket dialog');
            assert.equal(element('partial-execution-ack').checked, false, 'basket replacement must clear acknowledgement');
            assert.equal(element('confirm-execution').disabled, true, 'basket replacement must revoke confirmation');
            const messagesBeforeStaleConfirm = messages.length;
            element('confirm-execution').click();
            assert.equal(messages.length, messagesBeforeStaleConfirm, 'revoked basket A ticket must not be emitted');

            element('place-buy-basket').click();
            assert.deepEqual(messages.pop(), { type: 'preflightBasket', side: 'BUY', basketNotional: 300 });
            setTerminalPreflight({
              IsReady: true,
              Ticket: {
                TicketId: 'host-ticket-2', BasketId: 'basket-2', Side: 'BUY', RequestedNotional: 300,
                EstimatedMargin: 70, MarginCurrency: 'USDd', ExpiresUtc: '2026-07-30T12:04:00Z',
                Legs: [
                  { Direction: 'BUY', Epic: 'ADBE', Quantity: 0.1, ReferencePrice: 401.25 },
                  { Direction: 'BUY', Epic: 'CRM', Quantity: 0.2, ReferencePrice: 252.5 },
                  { Direction: 'SELL', Epic: 'ORCL', Quantity: 0.3, ReferencePrice: 180.75 }
                ]
              },
              Failures: []
            });
            element('partial-execution-ack').checked = true;
            element('partial-execution-ack').dispatch('change');
            element('confirm-execution').click();
            assert.deepEqual(messages.pop(), { type: 'executeBasket', ticketId: 'host-ticket-2' });

            setTerminalExecutionProgress({
              ExecutionId: 'host-execution-1', TicketId: 'host-ticket-1', BasketId: 'basket-1', Side: 'BUY',
              State: 'NeedsAttention', UpdatedUtc: '2026-07-30T12:01:00Z',
              Legs: [{ Epic: 'AAPL', Direction: 'BUY', Quantity: 0.1, State: 'Open', DealReference: 'REF-1', DealId: 'DEAL-1', Message: '' }]
            });
            setTerminalExecutions([{
              ExecutionId: 'host-execution-1', TicketId: 'host-ticket-1', BasketId: 'basket-1', Side: 'BUY',
              State: 'NeedsAttention', UpdatedUtc: '2026-07-30T12:01:00Z',
              Legs: [{ Epic: 'AAPL', Direction: 'BUY', Quantity: 0.1, State: 'Open', DealReference: 'REF-1', DealId: 'DEAL-1', Message: '' }]
            }]);
            assert.match(element('execution-state').textContent, /Needs attention/i);
            assert.match(element('position-list').children[0].textContent, /DEAL-1/);

            element('refresh-executions').click();
            assert.deepEqual(messages.pop(), { type: 'refreshExecutions' });
            setTerminalExecutionProgress({});
            assert.equal(element('refresh-executions').disabled, false, 'empty progress callback must release operation lock');
            element('refresh-executions').click();
            assert.deepEqual(messages.pop(), { type: 'refreshExecutions' });
            setTerminalExecutionProgress({ Error: 'refresh failed' });
            assert.equal(element('refresh-executions').disabled, false, 'error progress callback must release operation lock');
            element('refresh-executions').click();
            assert.deepEqual(messages.pop(), { type: 'refreshExecutions' });
            setTerminalExecutions(null);
            assert.equal(element('refresh-executions').disabled, false, 'empty executions callback must release operation lock');
            setTerminalExecutions([{
              ExecutionId: 'host-execution-1', TicketId: 'host-ticket-1', BasketId: 'basket-1', Side: 'BUY',
              RequestedNotional: 300, EstimatedMargin: 72.5, MarginCurrency: 'USDd',
              State: 'NeedsAttention', UpdatedUtc: '2026-07-30T12:02:00Z',
              Legs: [
                { Epic: 'AAPL', Direction: 'BUY', Multiplier: 1, ReferencePrice: 120, FillLevel: 123.45, Quantity: 0.9, State: 'Open', DealReference: 'REF-1', DealId: 'DEAL-AAPL', CurrentUnrealizedProfitLoss: -2.26, Message: '' },
                { Epic: 'MSFT', Direction: 'BUY', Multiplier: -0.5, ReferencePrice: 500, Quantity: 0.2, State: 'Unknown', DealReference: 'REF-2', DealId: '', Message: 'Confirmation timed out' },
                { Epic: 'NVDA', Direction: 'SELL', Multiplier: 2, ReferencePrice: 180, FillLevel: 181, Quantity: 0.3, State: 'Closed', DealReference: 'REF-3', DealId: 'DEAL-3', Message: '' }
              ]
            }, {
              ExecutionId: 'host-execution-closed', TicketId: 'host-ticket-closed', BasketId: 'basket-closed', Side: 'SELL',
              RequestedNotional: 450, EstimatedMargin: 90, MarginCurrency: 'USDd', State: 'Closed', UpdatedUtc: '2026-07-29T12:02:00Z',
              Legs: [{ Epic: 'TSLA', Direction: 'SELL', Multiplier: 1, ReferencePrice: 220, FillLevel: 219.5, Quantity: 0.4, State: 'Closed', DealReference: 'REF-4', DealId: 'DEAL-4', CurrentUnrealizedProfitLoss: 7.5, Message: '' }]
            }, {
              ExecutionId: 'host-execution-rejected', TicketId: 'host-ticket-rejected', BasketId: 'basket-rejected', Side: 'BUY',
              RequestedNotional: 300, EstimatedMargin: 0, MarginCurrency: 'USDd', State: 'Rejected', UpdatedUtc: '2026-07-28T12:02:00Z',
              Legs: [{ Epic: 'META', Direction: 'BUY', Multiplier: 1, ReferencePrice: 600, Quantity: 0.1, State: 'Rejected', DealReference: '', DealId: '', Message: 'Market closed' }]
            }]);
            setTerminalRiskPlans([{
              ExecutionId: 'host-execution-1', BasketId: 'basket-1', Side: 'BUY', StopLoss: 220, TakeProfit: 260, UpdatedUtc: '2026-07-30T12:03:00Z'
            }]);

            const tableText = () => descendantText(element('trade-dock-table'));
            assert.match(tableText(), /Symbol.*Basket.*Side.*Quantity.*Entry.*Bid.*Ask.*Broker SL.*Broker TP.*P\/L.*Status/);
            assert.match(tableText(), /AAPL.*basket-1.*BUY.*0\.9.*123\.4500.*123\.4000.*123\.5000.*118\.0000.*140\.0000.*-2\.26.*Open/);
            messages.length = 0;
            const linkedPositionRow = element('trade-dock-table').children[1].children[0];
            linkedPositionRow.click();
            assert.deepEqual(messages, [{ type: 'showExecutionBasket', executionId: 'host-execution-1' }]);
            assert.equal(linkedPositionRow.classList.contains('selected'), true);

            element('trade-tab-pending').click();
            assert.match(tableText(), /Symbol.*Side.*Quantity.*Type.*Order.*Broker SL.*Broker TP.*Deal/);
            assert.match(tableText(), /AAPL.*SELL.*1.*LIMIT.*130\.0000.*140\.0000.*110\.0000.*ORDER-AAPL/);

            element('trade-tab-baskets').click();
            assert.match(tableText(), /Basket.*Side.*Legs.*Synthetic entry.*Bid.*Ask.*PLAN SL.*PLAN TP.*Margin.*P\/L.*State/);
            assert.match(tableText(), /basket-1.*BUY.*3.*235\.4500.*n\/a.*n\/a.*220\.0000.*260\.0000.*USDd 72\.50.*-2\.26.*Needs attention/);
            assert.doesNotMatch(tableText(), /basket-closed/);
            messages.length = 0;
            const basketRow = element('trade-dock-table').children[1].children[0];
            basketRow.click();
            assert.deepEqual(messages, [{ type: 'showExecutionBasket', executionId: 'host-execution-1' }]);
            assert.equal(basketRow.classList.contains('selected'), true);
            assert.equal(JSON.parse(storedValues.get('capetf.tradeDock.v1')).activeTab, 'baskets');

            element('trade-tab-history').click();
            assert.match(tableText(), /Updated.*Basket.*Side.*Legs.*Margin.*P\/L.*State.*Message/);
            assert.match(tableText(), /basket-closed.*SELL.*1.*USDd 90\.00.*7\.50.*Closed/);
            assert.match(tableText(), /basket-rejected.*BUY.*1.*USDd 0\.00.*n\/a.*Rejected.*Market closed/);
            assert.doesNotMatch(tableText(), /basket-1/);

            element('trade-dock-minimize').click();
            assert.equal(element('trade-dock').classList.contains('minimized'), true);
            assert.match(descendantText(element('trade-dock-account-strip')), /Funds.*USDd 21000\.00.*Equity.*USDd 20993\.11.*P\/L.*USDd -6\.89/);
            assert.equal(JSON.parse(storedValues.get('capetf.tradeDock.v1')).minimized, true);
            element('trade-dock-minimize').click();
            assert.equal(element('trade-dock').classList.contains('minimized'), false);

            element('trade-dock-splitter').dispatch('pointerdown', { pointerId: 9, clientY: 400 });
            element('trade-dock-splitter').dispatch('pointermove', { pointerId: 9, clientY: 590 });
            assert.equal(element('trade-dock').style.height, '118px');
            element('trade-dock-splitter').dispatch('pointermove', { pointerId: 9, clientY: 0 });
            assert.equal(element('trade-dock').style.height, '360px');
            element('trade-dock-splitter').dispatch('pointerup', { pointerId: 9 });

            messages.length = 0;
            element('close-host-execution-1').click();
            assert.equal(messages.length, 0, 'first close click must not send a mutation');
            assert.equal(element('close-confirmation').open, true);
            assert.equal(element('close-confirmation-legs').children.length, 1);
            assert.match(element('close-confirmation-legs').children[0].textContent, /AAPL.*Open/i);
            assert.equal(element('confirm-close').disabled, true);
            element('partial-close-ack').checked = true;
            element('partial-close-ack').dispatch('change');
            assert.equal(element('confirm-close').disabled, false);
            element('confirm-close').click();
            assert.deepEqual(messages.pop(), { type: 'closeBasket', executionId: 'host-execution-1' });
            const mutationCountAfterClose = allMessages.filter(message => message.type === 'closeBasket').length;
            element('confirm-close').click();
            assert.equal(
              allMessages.filter(message => message.type === 'closeBasket').length,
              mutationCountAfterClose,
              'a consumed close confirmation cannot submit twice');

            for (const message of allMessages) {
              for (const forbidden of ['epic', 'direction', 'quantity', 'price', 'dealId']) {
                assert.equal(Object.hasOwn(message, forbidden), false, `browser message leaked ${forbidden}`);
              }
            }
            setTerminalTradingMode({ IsDemo: false, IsExecutionEnabled: false, Label: 'TRADING DISABLED' });
            assert.equal(element('place-buy-basket').disabled, true);
            assert.equal(element('place-sell-basket').disabled, true);
            """;

        RunNodeScript(script, htmlPath, "synthetic trading workspace runtime");
    }

    private static void TradingBrowserParserAllowsOnlyActionIdentifiersAndPreflightInputs()
    {
        using var preflightDocument = JsonDocument.Parse(
            "{\"type\":\"preflightBasket\",\"side\":\"SELL\",\"basketNotional\":450}");
        AssertTrue(
            SyntheticTradingBrowserRequestParser.TryParse(preflightDocument.RootElement, out var preflight, out var preflightError),
            $"valid preflight request must parse: {preflightError}");
        var preflightRequest = preflight as SyntheticPreflightBasketRequest
            ?? throw new Exception("preflight request type");
        AssertEqual("SELL", preflightRequest.Side, "preflight side");
        AssertEqual(450m, preflightRequest.BasketNotional, "preflight notional");

        var ticketId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        using var executeDocument = JsonDocument.Parse(
            "{\"type\":\"executeBasket\",\"ticketId\":\"11111111-1111-1111-1111-111111111111\"}");
        AssertTrue(
            SyntheticTradingBrowserRequestParser.TryParse(executeDocument.RootElement, out var execute, out var executeError),
            $"valid execute request must parse: {executeError}");
        AssertEqual(ticketId, ((SyntheticExecuteBasketRequest)execute!).TicketId, "execute ticket identity");

        using var showDocument = JsonDocument.Parse(
            "{\"type\":\"showExecutionBasket\",\"executionId\":\"execution-123\"}");
        AssertTrue(
            SyntheticTradingBrowserRequestParser.TryParse(showDocument.RootElement, out var show, out var showError),
            $"valid show execution request must parse: {showError}");
        AssertEqual("execution-123", ((SyntheticShowExecutionBasketRequest)show!).ExecutionId, "show execution identity");

        foreach (var unsafePayload in new[]
        {
            "{\"type\":\"preflightBasket\",\"side\":\"BUY\",\"basketNotional\":300,\"epic\":\"AAPL\"}",
            "{\"type\":\"executeBasket\",\"ticketId\":\"11111111-1111-1111-1111-111111111111\",\"direction\":\"SELL\"}",
            "{\"type\":\"executeBasket\",\"ticketId\":\"11111111-1111-1111-1111-111111111111\",\"quantity\":999}",
            "{\"type\":\"closeBasket\",\"executionId\":\"execution-123\",\"dealId\":\"attacker-deal\"}",
            "{\"type\":\"refreshExecutions\",\"epic\":\"AAPL\"}",
            "{\"type\":\"showExecutionBasket\",\"executionId\":\"execution-123\",\"multiplier\":999}",
        })
        {
            using var unsafeDocument = JsonDocument.Parse(unsafePayload);
            AssertFalse(
                SyntheticTradingBrowserRequestParser.TryParse(unsafeDocument.RootElement, out _, out _),
                $"browser mutation fields must be rejected: {unsafePayload}");
        }
    }

    private static void TradingBrowserParserAllowsOnlyRiskPlanIdentifiersAndLevels()
    {
        using var setDocument = JsonDocument.Parse(
            "{\"type\":\"setRiskPlan\",\"executionId\":\"execution-1\",\"stopLoss\":92.5,\"takeProfit\":118.0}");
        AssertTrue(
            SyntheticTradingBrowserRequestParser.TryParse(setDocument.RootElement, out var set, out var setError),
            $"valid risk-plan request must parse: {setError}");
        var setRequest = set as SyntheticSetRiskPlanRequest
            ?? throw new Exception("risk-plan request type");
        AssertEqual("execution-1", setRequest.ExecutionId, "risk-plan execution identity");
        AssertEqual(92.5m, setRequest.StopLoss, "risk-plan stop loss");
        AssertEqual(118.0m, setRequest.TakeProfit, "risk-plan take profit");

        using var nullableSetDocument = JsonDocument.Parse(
            "{\"type\":\"setRiskPlan\",\"executionId\":\"execution-1\",\"stopLoss\":null,\"takeProfit\":118.0}");
        AssertTrue(
            SyntheticTradingBrowserRequestParser.TryParse(nullableSetDocument.RootElement, out var nullableSet, out var nullableSetError),
            $"risk-plan request with an empty level must parse: {nullableSetError}");
        AssertEqual(null, ((SyntheticSetRiskPlanRequest)nullableSet!).StopLoss, "risk-plan empty stop loss");

        using var clearDocument = JsonDocument.Parse(
            "{\"type\":\"clearRiskPlan\",\"executionId\":\"execution-1\"}");
        AssertTrue(
            SyntheticTradingBrowserRequestParser.TryParse(clearDocument.RootElement, out var clear, out var clearError),
            $"valid risk-plan clear request must parse: {clearError}");
        AssertEqual("execution-1", ((SyntheticClearRiskPlanRequest)clear!).ExecutionId, "risk-plan clear identity");

        foreach (var unsafePayload in new[]
        {
            "{\"type\":\"setRiskPlan\",\"executionId\":\"execution-1\",\"stopLoss\":92.5,\"takeProfit\":118.0,\"epic\":\"AAPL\"}",
            "{\"type\":\"setRiskPlan\",\"executionId\":\"execution-1\",\"stopLoss\":92.5,\"takeProfit\":118.0,\"dealId\":\"deal-1\"}",
            "{\"type\":\"setRiskPlan\",\"executionId\":\"execution-1\",\"stopLoss\":92.5,\"takeProfit\":118.0,\"multiplier\":1.5}",
            "{\"type\":\"setRiskPlan\",\"executionId\":\"execution-1\",\"stopLoss\":92.5,\"takeProfit\":118.0,\"direction\":\"BUY\"}",
            "{\"type\":\"clearRiskPlan\",\"executionId\":\"execution-1\",\"quantity\":1}",
        })
        {
            using var unsafeDocument = JsonDocument.Parse(unsafePayload);
            AssertFalse(
                SyntheticTradingBrowserRequestParser.TryParse(unsafeDocument.RootElement, out _, out _),
                $"risk-plan mutation fields must be rejected: {unsafePayload}");
        }
    }

    private static void ExecutionBasketSnapshotPreservesTrustedFormula()
    {
        var now = DateTimeOffset.Parse("2026-07-30T12:00:00Z");
        var record = new SyntheticExecutionRecord(
            "execution-123", "ticket-123", "SYN-USUSDALL-01|AMD|BB|DDOG", "SELL", 300m, 60m, "USD",
            now, now, SyntheticExecutionState.Open,
            [
                ExecutionLeg("AMD", "SELL", 0.06m, 156m),
                ExecutionLeg("BB", "SELL", 3.93m, 2.54m),
                ExecutionLeg("DDOG", "BUY", -0.13m, 75m),
            ]);
        var instruments = new[]
        {
            new MarketInstrument { Epic = "AMD", Name = "AMD", Region = "US", Currency = "USD", Sector = "Technology" },
            new MarketInstrument { Epic = "BB", Name = "BlackBerry", Region = "US", Currency = "USD", Sector = "Technology" },
            new MarketInstrument { Epic = "DDOG", Name = "Datadog", Region = "US", Currency = "USD", Sector = "Technology" },
        };

        var saved = SyntheticExecutionBasketSnapshot.Create(record, instruments);

        AssertEqual("SYN-USUSDALL-01", saved.Symbol, "execution basket symbol");
        AssertEqual("US / USD / All", saved.Block, "execution basket block");
        AssertEqual(3, saved.Components.Count, "execution basket leg count");
        AssertEqual(0.06m, saved.Components[0].FormulaMultiplier, "first trusted multiplier");
        AssertEqual(3.93m, saved.Components[1].FormulaMultiplier, "second trusted multiplier");
        AssertEqual(-0.13m, saved.Components[2].FormulaMultiplier, "signed trusted multiplier");
        AssertTrue(saved.Components.All(component => component.Weight == 100m / 3m), "execution basket equal weights");

        SyntheticExecutionLegRecord ExecutionLeg(string epic, string direction, decimal multiplier, decimal reference) =>
            new(epic, direction, multiplier, reference, 1m, 100m, 20m, "USD",
                SyntheticExecutionLegState.Open, "ref", $"deal-{epic}", "", reference, "", now, now, null, now);
    }

    private static void TradingBrowserParserRejectsMalformedShapesWithoutThrowing()
    {
        foreach (var payload in new[]
        {
            "42",
            "[]",
            "{}",
            "{\"type\":42}",
            "{\"type\":\"executeBasket\",\"ticketId\":\"not-a-guid\"}",
            "{\"type\":\"preflightBasket\",\"side\":\"BUY\",\"basketNotional\":\"300\"}",
            "{\"type\":\"preflightBasket\",\"side\":\"BUY\",\"basketNotional\":1e999}",
            "{\"type\":\"unknownAction\"}",
            "{",
        })
        {
            var parsed = SyntheticTradingBrowserRequestParser.TryParse(payload, out var request, out var error);

            AssertFalse(parsed, $"malformed browser payload must be rejected without throwing: {payload}");
            AssertTrue(request is null, "a rejected browser payload must not produce a request");
            AssertFalse(string.IsNullOrWhiteSpace(error), "a rejected browser payload must have a visible error");
        }
    }

    private static void TradingBrowserHandlerTurnsMalformedAndSemanticFailuresIntoRejections()
    {
        var handled = 0;
        var rejections = new List<string>();
        var rejectedPayloads = new[]
        {
            "42",
            "{}",
            "{\"type\":42}",
            "{\"type\":\"executeBasket\",\"ticketId\":\"not-a-guid\"}",
            "{\"type\":\"preflightBasket\",\"side\":\"BUY\",\"basketNotional\":\"300\"}",
            "{\"type\":\"unknownAction\"}",
        };
        foreach (var payload in rejectedPayloads)
        {
            SyntheticTradingBrowserMessageHandler.HandleAsync(
                    payload,
                    _ =>
                    {
                        handled++;
                        return Task.CompletedTask;
                    },
                    error =>
                    {
                        rejections.Add(error);
                        return Task.CompletedTask;
                    })
                .GetAwaiter().GetResult();
        }

        AssertEqual(0, handled, "malformed browser messages must not reach the action handler");
        AssertEqual(rejectedPayloads.Length, rejections.Count, "each malformed browser message must publish one rejection");

        SyntheticTradingBrowserMessageHandler.HandleAsync(
                "{\"type\":\"refreshExecutions\"}",
                _ => throw new InvalidOperationException("semantic request failure"),
                error =>
                {
                    rejections.Add(error);
                    return Task.CompletedTask;
                })
            .GetAwaiter().GetResult();

        AssertEqual(rejectedPayloads.Length + 1, rejections.Count, "semantic JSON failures must be rejected at the handler boundary");
        AssertContains(rejections[^1], "semantic request failure", "semantic rejection message");

        SyntheticTradingBrowserMessageHandler.HandleAsync(
                "{\"type\":\"refreshExecutions\"}",
                _ => throw new FormatException("semantic format failure"),
                error =>
                {
                    rejections.Add(error);
                    return Task.CompletedTask;
                })
            .GetAwaiter().GetResult();

        AssertEqual(rejectedPayloads.Length + 2, rejections.Count, "format failures must be rejected at the handler boundary");
        AssertContains(rejections[^1], "semantic format failure", "format rejection message");
    }

    private static void HostConsumesFrozenTicketsBeforeExecutionAndRejectsReuse()
    {
        using var directory = new TemporaryDirectory();
        var gateway = AcceptedExecutionGateway("AAPL");
        var mutableLegs = new List<SyntheticExecutionLeg>
        {
            new("AAPL", "BUY", 1m, 100m, 1m, 100m, 20m, "USD"),
        };
        var ticketId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var ticket = CreateHostTicket(ticketId, mutableLegs);
        using var coordinator = CreateHostCoordinator(directory.Path, gateway);
        coordinator.RegisterPreflight(new SyntheticPreflightResult(true, ticket, []));
        mutableLegs[0] = mutableLegs[0] with { Direction = "SELL", Quantity = 999m };

        using (var execution = coordinator.BeginExecution(ticketId))
        {
            AssertThrows<InvalidOperationException>(
                () => coordinator.BeginExecution(ticketId),
                "a consumed ticket must be unavailable before any gateway mutation");
            coordinator.ExecuteAsync(execution, _ => Task.CompletedTask, _ => Task.CompletedTask, default)
                .GetAwaiter().GetResult();
        }

        AssertEqual("BUY", gateway.PostRequests.Single().Direction, "host-owned ticket direction must be authoritative");
        AssertEqual(1m, gateway.PostRequests.Single().Size, "host-owned ticket quantity must be authoritative");
        AssertThrows<InvalidOperationException>(
            () => coordinator.BeginExecution(ticketId),
            "a used ticket must not be reusable");
    }

    private static void HostRejectsExpiredTicketsWithoutMutation()
    {
        using var directory = new TemporaryDirectory();
        var gateway = AcceptedExecutionGateway("AAPL");
        var now = DateTimeOffset.Parse("2026-07-30T12:05:00Z");
        var ticketId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        using var coordinator = CreateHostCoordinator(directory.Path, gateway, utcNow: () => now);
        coordinator.RegisterPreflight(new SyntheticPreflightResult(
            true,
            CreateHostTicket(ticketId) with { ExpiresUtc = now.AddSeconds(-1) },
            []));

        var exception = AssertThrows<InvalidOperationException>(
            () => coordinator.BeginExecution(ticketId),
            "expired execution ticket must fail");

        AssertContains(exception.Message, "expired", "expired ticket error");
        AssertEqual(0, gateway.CreateInvocations, "expired ticket must fail before gateway mutation");
    }

    private static void HostDuplicateGuardDoesNotConsumeTheBlockedTicket()
    {
        using var directory = new TemporaryDirectory();
        var firstTicketId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var secondTicketId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        using var coordinator = CreateHostCoordinator(directory.Path, AcceptedExecutionGateway("AAPL"));
        coordinator.RegisterPreflight(new SyntheticPreflightResult(true, CreateHostTicket(firstTicketId), []));
        coordinator.RegisterPreflight(new SyntheticPreflightResult(true, CreateHostTicket(secondTicketId), []));

        var first = coordinator.BeginExecution(firstTicketId);
        var duplicate = AssertThrows<InvalidOperationException>(
            () => coordinator.BeginExecution(secondTicketId),
            "duplicate trading operation must be blocked");
        AssertContains(duplicate.Message, "already running", "duplicate operation error");
        first.Dispose();

        using var second = coordinator.BeginExecution(secondTicketId);
    }

    private static void HostDemoGateBlocksExecutionAndCloseMutations()
    {
        using var directory = new TemporaryDirectory();
        var isDemo = false;
        var gateway = AcceptedExecutionGateway("AAPL");
        var ticketId = Guid.Parse("66666666-6666-6666-6666-666666666666");
        var store = new SyntheticExecutionStore(Path.Combine(directory.Path, "executions.json"));
        store.SaveAsync([CreatePersistedExecutionRecord()], default).GetAwaiter().GetResult();
        using var coordinator = CreateHostCoordinator(directory.Path, gateway, () => isDemo, store: store);
        coordinator.RegisterPreflight(new SyntheticPreflightResult(true, CreateHostTicket(ticketId), []));

        AssertThrows<InvalidOperationException>(
            () => coordinator.BeginExecution(ticketId),
            "host execution gate must reject a non-demo session");
        AssertThrows<InvalidOperationException>(
            () => coordinator.CloseAsync("execution-123", _ => Task.CompletedTask, _ => Task.CompletedTask, default)
                .GetAwaiter().GetResult(),
            "host close gate must reject a non-demo session");
        AssertEqual(0, gateway.CreateInvocations, "non-demo execution must not mutate");
        AssertEqual(0, gateway.CloseInvocations, "non-demo close must not mutate");

        isDemo = true;
        using var permitted = coordinator.BeginExecution(ticketId);
    }

    private static void HostRejectsCrossAccountExecutionAndCloseMutations()
    {
        using var directory = new TemporaryDirectory();
        var store = new SyntheticExecutionStore(Path.Combine(directory.Path, "executions.json"));
        store.SaveAsync([CreatePersistedExecutionRecord() with { AccountId = "account-a" }], default).GetAwaiter().GetResult();
        var gateway = AcceptedExecutionGateway("AAPL");
        var ticketId = Guid.Parse("67676767-6767-6767-6767-676767676767");
        using var coordinator = CreateHostCoordinator(
            directory.Path,
            gateway,
            store: store,
            currentAccountId: () => "account-b");
        coordinator.RegisterPreflight(new SyntheticPreflightResult(
            true,
            CreateHostTicket(ticketId) with { AccountId = "account-a" },
            []));

        AssertThrows<InvalidOperationException>(
            () => coordinator.BeginExecution(ticketId),
            "a ticket created for another Capital account must not execute");
        AssertThrows<InvalidOperationException>(
            () => coordinator.CloseAsync("execution-123", _ => Task.CompletedTask, _ => Task.CompletedTask, default)
                .GetAwaiter().GetResult(),
            "a persisted execution from another Capital account must not close");
        AssertEqual(0, gateway.CreateInvocations, "cross-account execution must not mutate");
        AssertEqual(0, gateway.CloseInvocations, "cross-account close must not mutate");
    }

    private static void HostScopesLegacyExecutionOnlyAfterExactCurrentAccountMatch()
    {
        using var directory = new TemporaryDirectory();
        var store = new SyntheticExecutionStore(Path.Combine(directory.Path, "executions.json"));
        var legacy = CreatePersistedExecutionRecord() with { AccountId = "" };
        store.SaveAsync([legacy], default).GetAwaiter().GetResult();
        var position = legacy.Legs[0];
        using var coordinator = CreateHostCoordinator(
            directory.Path,
            new ScriptedTradingGateway(),
            store: store,
            utcNow: () => DateTimeOffset.Parse("2026-07-30T12:10:00Z"),
            currentAccountId: () => "account-current",
            getOpenPositions: _ => Task.FromResult<IReadOnlyList<CapitalOpenPosition>>([
                new(position.DealId, position.Epic, position.Direction, position.Quantity, position.FillLevel, 1m, "USD", "TRADEABLE"),
            ]));

        var reconciled = coordinator.ReconnectAsync(_ => Task.CompletedTask, default).GetAwaiter().GetResult().Single();

        AssertEqual("account-current", reconciled.AccountId, "legacy execution is scoped only after its exact permanent deal is present");
    }

    private static void HostPersistsEveryTransitionBeforePublication()
    {
        using var directory = new TemporaryDirectory();
        var store = new SyntheticExecutionStore(Path.Combine(directory.Path, "executions.json"));
        var ticketId = Guid.Parse("77777777-7777-7777-7777-777777777777");
        using var coordinator = CreateHostCoordinator(
            directory.Path,
            AcceptedExecutionGateway("AAPL"),
            store: store);
        coordinator.RegisterPreflight(new SyntheticPreflightResult(true, CreateHostTicket(ticketId), []));
        var progressPublications = 0;

        Task AssertProgressPersisted(SyntheticExecutionRecord published)
        {
            var persisted = store.LoadAsync(default).GetAwaiter().GetResult()
                .Single(record => record.ExecutionId == published.ExecutionId);
            AssertEqual(published.TicketId, persisted.TicketId, "persisted transition ticket identity");
            AssertEqual(StateSignature(published), StateSignature(persisted), "execution transition must persist before progress publication");
            AssertEqual(published.Legs[0].DealReference, persisted.Legs[0].DealReference, "persisted transition acknowledgement identity");
            AssertEqual(published.Legs[0].DealId, persisted.Legs[0].DealId, "persisted transition permanent deal identity");
            progressPublications++;
            return Task.CompletedTask;
        }

        Task AssertExecutionsPersisted(IReadOnlyList<SyntheticExecutionRecord> published)
        {
            var persisted = store.LoadAsync(default).GetAwaiter().GetResult();
            AssertEqual(published.Count, persisted.Count, "persisted execution publication count");
            AssertEqual(
                string.Join("|", published.Select(record => $"{record.ExecutionId}:{StateSignature(record)}")),
                string.Join("|", persisted.Select(record => $"{record.ExecutionId}:{StateSignature(record)}")),
                "execution list must persist before publication");
            return Task.CompletedTask;
        }

        using var execution = coordinator.BeginExecution(ticketId);
        var result = coordinator.ExecuteAsync(execution, AssertProgressPersisted, AssertExecutionsPersisted, default)
            .GetAwaiter().GetResult();

        AssertEqual(SyntheticExecutionState.Open, result.State, "accepted host execution state");
        AssertTrue(progressPublications >= 5, "every service transition must be published after persistence");
    }

    private static void HostReconnectReconcilesAndPersistsBeforePublication()
    {
        using var directory = new TemporaryDirectory();
        var store = new SyntheticExecutionStore(Path.Combine(directory.Path, "executions.json"));
        store.SaveAsync([CreatePersistedExecutionRecord()], default).GetAwaiter().GetResult();
        using var coordinator = CreateHostCoordinator(
            directory.Path,
            new ScriptedTradingGateway(),
            utcNow: () => DateTimeOffset.Parse("2026-07-30T12:10:00Z"),
            store: store,
            getOpenPositions: _ => Task.FromResult<IReadOnlyList<CapitalOpenPosition>>([]));
        var publishedAfterPersistence = false;

        var reconciled = coordinator.ReconnectAsync(records =>
        {
            var persisted = store.LoadAsync(default).GetAwaiter().GetResult();
            AssertEqual(SyntheticExecutionState.Closed, persisted.Single().State, "reconnect persistence state");
            AssertEqual(SyntheticExecutionState.Closed, records.Single().State, "reconnect publication state");
            publishedAfterPersistence = true;
            return Task.CompletedTask;
        }, default).GetAwaiter().GetResult();

        AssertEqual(SyntheticExecutionState.Closed, reconciled.Single().State, "reconnect reconciled state");
        AssertTrue(publishedAfterPersistence, "reconnect must publish reconciled records");
    }

    private static void HostCancellationPreservesAcknowledgedExecutionState()
    {
        using var directory = new TemporaryDirectory();
        var store = new SyntheticExecutionStore(Path.Combine(directory.Path, "executions.json"));
        var gateway = AcceptedExecutionGateway("AAPL", "MSFT");
        var ticketId = Guid.Parse("88888888-8888-8888-8888-888888888888");
        using var coordinator = CreateHostCoordinator(directory.Path, gateway, store: store);
        coordinator.RegisterPreflight(new SyntheticPreflightResult(
            true,
            CreateHostTicket(ticketId, [
                new SyntheticExecutionLeg("AAPL", "BUY", 1m, 100m, 1m, 100m, 20m, "USD"),
                new SyntheticExecutionLeg("MSFT", "BUY", 1m, 100m, 1m, 100m, 20m, "USD"),
            ]),
            []));
        var cancelledAfterAcknowledgement = false;

        Task CancelAfterAcknowledgement(SyntheticExecutionRecord record)
        {
            if (!cancelledAfterAcknowledgement && record.Legs[0].State == SyntheticExecutionLegState.Confirming)
            {
                cancelledAfterAcknowledgement = true;
                coordinator.CancelPendingOperations();
            }
            return Task.CompletedTask;
        }

        using var execution = coordinator.BeginExecution(ticketId);
        var result = coordinator.ExecuteAsync(execution, CancelAfterAcknowledgement, _ => Task.CompletedTask, default)
            .GetAwaiter().GetResult();
        var persisted = store.LoadAsync(default).GetAwaiter().GetResult().Single();

        AssertTrue(cancelledAfterAcknowledgement, "shutdown cancellation must occur after acknowledgement persistence");
        AssertEqual(SyntheticExecutionLegState.Unknown, result.Legs[0].State, "acknowledged cancellation outcome");
        AssertEqual("o_aapl", persisted.Legs[0].DealReference, "acknowledged deal reference must remain persisted");
        AssertEqual(SyntheticExecutionLegState.Pending, persisted.Legs[1].State, "shutdown must leave unsent legs pending");
        AssertEqual(1, gateway.CreateInvocations, "shutdown must stop unsent mutations");
    }

    private static void WpfHostPublishesTradingContractsWithoutLegacyPreviewMutation()
    {
        var source = ReadRepositoryFile("desktop", "CAPETF.Desktop", "CapComTerminalWindow.xaml.cs");
        var xaml = ReadRepositoryFile("desktop", "CAPETF.Desktop", "CapComTerminalWindow.xaml");
        AssertContains(source, "SyntheticTradingComposition.CreateCoordinator", "WPF host production trading composition root");
        AssertFalse(
            source.Contains("new SyntheticTradingHostCoordinator", StringComparison.Ordinal),
            "WPF host must not reconstruct the trading service graph outside the production composition root");
        foreach (var callback in new[]
        {
            "setTerminalPreflight",
            "setTerminalExecutions",
            "setTerminalExecutionProgress",
            "setTerminalTradingMode",
            "setTerminalRiskPlans",
        })
        {
            AssertContains(source, callback, $"WPF callback {callback}");
        }
        AssertContains(source, "SetSyntheticRiskPlanAsync", "WPF host risk-plan set handler");
        AssertContains(source, "ClearSyntheticRiskPlanAsync", "WPF host risk-plan clear handler");

        var preflightBody = SliceSource(source, "private async Task PreflightSyntheticBasketAsync", "private Task ExecuteSyntheticBasketAsync");
        AssertFalse(
            preflightBody.Contains("RefreshBasketMarketDetailsAsync", StringComparison.Ordinal),
            "trading preflight must not treat the mutable basket refresh as authoritative");
        AssertOrdered(preflightBody,
            "_preflightMarketSnapshots.LoadAsync",
            "snapshotResult.Basket",
            "InvalidateCaches",
            "BuildAsync(freshBasket",
            "SyntheticTradePreflight.Build",
            "RegisterPreflight");
        var executeBody = SliceSource(source, "private Task ExecuteSyntheticBasketAsync", "private static async Task FinishSyntheticExecutionAsync");
        AssertTrue(
            executeBody.IndexOf("BeginExecution(ticketId)", StringComparison.Ordinal)
            < executeBody.IndexOf("await ", StringComparison.Ordinal),
            "ticket consumption must happen before the execute path can await");
        AssertOrdered(executeBody, "_operationState.TryBegin", "BeginExecution(ticketId)", "RunStartedOperationAsync");
        var previewBody = SliceSource(source, "private void PreviewSyntheticOrder", "protected override void OnClosing");
        AssertFalse(previewBody.Contains("CreatePositionAsync", StringComparison.Ordinal), "legacy preview must not create positions");
        AssertFalse(previewBody.Contains("ClosePositionAsync", StringComparison.Ordinal), "legacy preview must not close positions");
        AssertFalse(previewBody.Contains("BeginExecution", StringComparison.Ordinal), "legacy preview must not consume execution tickets");
        var closingBody = SliceSource(source, "protected override void OnClosing", "protected override void OnClosed");
        AssertOrdered(closingBody, "CancelPendingOperations", "BeginClosing");
        var closedBody = SliceSource(source, "protected override void OnClosed", "\n}");
        AssertFalse(
            closedBody.Contains("_brokerRefreshLoop.GetAwaiter().GetResult()", StringComparison.Ordinal),
            "window close must not synchronously wait on a dispatcher-captured broker refresh loop");
        AssertContains(xaml, "TradingModeText", "persistent WPF demo-state label");
    }

    private static void WpfHostRejectsUnknownRiskPlanClearWithoutPersisting()
    {
        var source = ReadRepositoryFile("desktop", "CAPETF.Desktop", "CapComTerminalWindow.xaml.cs");
        var clearBody = SliceSource(source, "private async Task ClearSyntheticRiskPlanAsync", "private async Task RejectTerminalBrowserRequestAsync");

        AssertOrdered(clearBody,
            "_terminalExecutions.FirstOrDefault",
            "if (execution is null)",
            "PublishTerminalRiskPlanErrorAsync",
            "return;",
            "_riskPlanStore.Remove(request.ExecutionId)");

        using var directory = new TemporaryDirectory();
        var store = new SyntheticRiskPlanStore(Path.Combine(directory.Path, "synthetic-risk-plans.json"));
        store.Upsert(new SyntheticRiskPlan("execution-1", "basket-1", "BUY", 92m, 118m, DateTimeOffset.UtcNow));
        store.Remove("unknown-execution");
        AssertEqual("execution-1", store.LoadAll().Single().ExecutionId,
            "an unknown clear ID must not remove a persisted execution plan");
    }

    private static void WindowLifecycleDefersCloseUntilAcknowledgedSaveCompletes()
    {
        using var closeAuthorized = new ManualResetEventSlim();
        var saveGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var events = new List<string>();
        var lifecycle = new SyntheticTradingWindowLifecycleCoordinator(TimeSpan.FromMilliseconds(20));
        var operation = lifecycle.BeginOperation();

        async Task PersistAcknowledgedMutationAsync()
        {
            operation.MarkMutationDispatched();
            events.Add("acknowledged");
            await saveGate.Task;
            events.Add("saved");
        }

        var workflow = PersistAcknowledgedMutationAsync();
        operation.Track(workflow);
        var mayCloseImmediately = lifecycle.RequestClose(
            () => events.Add("cancelled"),
            () =>
            {
                events.Add("authorized");
                events.Add("disposed");
                closeAuthorized.Set();
            });

        AssertFalse(mayCloseImmediately, "an acknowledged mutation must defer window close");
        Thread.Sleep(75);
        AssertFalse(closeAuthorized.IsSet, "post-dispatch persistence must not use the pre-dispatch timeout");
        AssertFalse(events.Contains("disposed"), "API/store disposal must wait for acknowledged persistence");

        saveGate.SetResult();
        workflow.GetAwaiter().GetResult();
        AssertTrue(closeAuthorized.Wait(TimeSpan.FromSeconds(2)), "close must be authorized after persistence completes");
        AssertOrdered(string.Join('|', events), "cancelled", "saved", "authorized", "disposed");
        AssertTrue(
            lifecycle.RequestClose(() => throw new Exception("cancellation must run once"), () => throw new Exception("authorization must run once")),
            "the authorized reentrant close must proceed without starting another close cycle");
    }

    private static void WindowLifecycleBoundsOnlyPreDispatchWait()
    {
        using var closeAuthorized = new ManualResetEventSlim();
        var operationGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var lifecycle = new SyntheticTradingWindowLifecycleCoordinator(TimeSpan.FromMilliseconds(20));
        var operation = lifecycle.BeginOperation();
        operation.Track(operationGate.Task);

        AssertFalse(
            lifecycle.RequestClose(() => { }, closeAuthorized.Set),
            "an active pre-dispatch operation must initially defer close");
        AssertTrue(
            closeAuthorized.Wait(TimeSpan.FromSeconds(2)),
            "a cancelled pre-dispatch operation must have a bounded shutdown wait");
        AssertThrows<OperationCanceledException>(
            operation.MarkMutationDispatched,
            "a timed-out pre-dispatch operation must be blocked before entering the gateway");

        operationGate.SetResult();
    }

    private static void FreshPreflightSnapshotsRejectFailedRefreshDespiteStaleMetadata()
    {
        var basket = CreateBasket();
        foreach (var component in basket.Components)
        {
            component.Instrument.MarginFactor = 99m;
            component.Instrument.MarginFactorUnit = "PERCENTAGE";
        }
        var requestedEpics = new List<string>();
        var loader = new SyntheticPreflightMarketSnapshotLoader((epic, _) =>
        {
            requestedEpics.Add(epic);
            return epic == "BETA"
                ? Task.FromException<MarketInstrument?>(new InvalidOperationException("refresh failed"))
                : Task.FromResult<MarketInstrument?>(CreateFreshMarketDetails(epic));
        });

        var result = loader.LoadAsync(basket, default).GetAwaiter().GetResult();

        AssertTrue(result.Basket is null, "a failed current market refresh must not return a tradable basket");
        AssertEqual(3, result.Snapshots.Count, "only successful current snapshots may be retained");
        AssertContainsFailure(result.Failures, "BETA", "refresh failed");
        AssertEqual(4, requestedEpics.Count, "preflight must attempt current market details for every leg");
        AssertEqual(99m, basket.Components[1].Instrument.MarginFactor, "stale source margin proves refresh cannot fall back");
    }

    private static void FreshPreflightSnapshotsRejectIncompleteCurrentMetadata()
    {
        var basket = CreateBasket();
        var loader = new SyntheticPreflightMarketSnapshotLoader((epic, _) =>
        {
            var details = CreateFreshMarketDetails(epic);
            if (epic == "GAMMA") details.MarginFactor = null;
            return Task.FromResult<MarketInstrument?>(details);
        });

        var result = loader.LoadAsync(basket, default).GetAwaiter().GetResult();

        AssertTrue(result.Basket is null, "missing current trading metadata must fail closed");
        AssertContainsFailure(result.Failures, "GAMMA", "margin factor");
    }

    private static void FreshPreflightSnapshotsBuildDetachedBasketFromExactResponses()
    {
        var basket = CreateBasket();
        var responses = basket.Components.ToDictionary(
            component => component.Instrument.Epic,
            component => CreateFreshMarketDetails(component.Instrument.Epic),
            StringComparer.OrdinalIgnoreCase);
        var loader = new SyntheticPreflightMarketSnapshotLoader((epic, _) =>
            Task.FromResult<MarketInstrument?>(responses[epic]));

        var result = loader.LoadAsync(basket, default).GetAwaiter().GetResult();
        var freshBasket = result.Basket ?? throw new Exception("complete current snapshots must produce a fresh basket");

        AssertFalse(ReferenceEquals(basket, freshBasket), "preflight must not mutate or reuse the displayed basket");
        AssertEqual(basket.Components.Count, result.Snapshots.Count, "every current response must enter the snapshot collection");
        foreach (var component in freshBasket.Components)
        {
            AssertTrue(
                ReferenceEquals(responses[component.Instrument.Epic], component.Instrument),
                "the preflight basket must consume the exact fetched market snapshot");
        }
        AssertEqual(10m, basket.Components[0].Instrument.Bid, "the displayed basket quote must remain unchanged");
        AssertEqual(11m, freshBasket.Components[0].Instrument.Bid, "the detached basket must carry the current quote");
    }

    private static void ExecutionStoreRoundTripsVersionedRecordsAndDealIdentity()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "executions.json");
        var store = new SyntheticExecutionStore(path);
        var record = CreatePersistedExecutionRecord();

        store.SaveAsync([record], default).GetAwaiter().GetResult();
        var restored = AssertSingle(store.LoadAsync(default).GetAwaiter().GetResult(), "round-trip must restore one execution");
        using var document = JsonDocument.Parse(File.ReadAllText(path));

        AssertEqual(1, document.RootElement.GetProperty("schemaVersion").GetInt32(), "store schema version");
        AssertEqual(record.ExecutionId, restored.ExecutionId, "versioned JSON execution identity");
        AssertEqual(record.Legs[0], restored.Legs[0], "versioned JSON leg round-trip");
        AssertEqual("deal-123", restored.Legs[0].DealId, "deal identity survives restart");
    }

    private static void ExecutionStoreUpsertsAtomicallyWithoutCredentials()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "executions.json");
        var store = new SyntheticExecutionStore(path);
        var original = CreatePersistedExecutionRecord();
        var revised = original with
        {
            UpdatedUtc = original.UpdatedUtc.AddMinutes(1),
            Legs = [original.Legs[0] with { Message = "Capital reconciliation observed an open position." }],
        };

        store.SaveAsync([original], default).GetAwaiter().GetResult();
        store.UpsertAsync(revised, default).GetAwaiter().GetResult();
        var restored = AssertSingle(store.LoadAsync(default).GetAwaiter().GetResult(), "upsert must replace the existing execution");
        var persisted = File.ReadAllText(path);

        AssertEqual(revised.ExecutionId, restored.ExecutionId, "upsert must retain execution identity");
        AssertEqual(revised.UpdatedUtc, restored.UpdatedUtc, "upsert must retain latest record timestamp");
        AssertEqual(revised.Legs[0].Message, restored.Legs[0].Message, "upsert must retain latest audit message");
        AssertFalse(File.Exists(path + ".tmp"), "atomic save must replace the temporary file");
        AssertFalse(persisted.Contains("securityToken", StringComparison.OrdinalIgnoreCase), "security tokens must not persist");
        AssertFalse(persisted.Contains("password", StringComparison.OrdinalIgnoreCase), "credentials must not persist");
    }

    private static void ExecutionStoreQuarantinesMalformedFiles()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "executions.json");
        File.WriteAllText(path, "{ definitely-not-json");
        var store = new SyntheticExecutionStore(path);

        var restored = store.LoadAsync(default).GetAwaiter().GetResult();

        AssertEqual(0, restored.Count, "malformed persistence must not create execution records");
        AssertContains(store.LastLoadWarning, "quarantined", "malformed persistence must surface a visible warning");
        AssertFalse(File.Exists(path), "malformed persistence must be moved away from the active path");
        AssertEqual(1, Directory.GetFiles(directory.Path, "executions.json.corrupt-*").Length, "malformed file must be quarantined with a UTC suffix");
    }

    private static void ExecutionStoreCoordinatesConcurrentInstancesWithoutLosingDealIdentity()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "executions.json");
        var firstStore = new SyntheticExecutionStore(path);
        var secondStore = new SyntheticExecutionStore(Path.GetFullPath(path));
        var auditPayload = new string('x', 128 * 1024);
        var seeds = Enumerable.Range(0, 32)
            .Select(index => CreatePersistedExecutionRecord() with
            {
                ExecutionId = $"seed-{index}",
                Legs = [CreatePersistedExecutionRecord().Legs[0] with
                {
                    DealId = $"deal-seed-{index}",
                    Message = auditPayload,
                }],
            })
            .ToArray();
        var firstRecord = CreatePersistedExecutionRecord() with
        {
            ExecutionId = "execution-first",
            Legs = [CreatePersistedExecutionRecord().Legs[0] with { DealId = "deal-first" }],
        };
        var secondRecord = CreatePersistedExecutionRecord() with
        {
            ExecutionId = "execution-second",
            Legs = [CreatePersistedExecutionRecord().Legs[0] with { DealId = "deal-second" }],
        };
        firstStore.SaveAsync(seeds, default).GetAwaiter().GetResult();
        var abandonedTemporaryPath = path + ".tmp";
        File.WriteAllText(abandonedTemporaryPath, "abandoned");
        File.SetLastWriteTimeUtc(abandonedTemporaryPath, DateTime.UtcNow.AddMinutes(-2));

        using var start = new Barrier(3);
        var first = Task.Factory.StartNew(() =>
        {
            start.SignalAndWait();
            firstStore.UpsertAsync(firstRecord, default).GetAwaiter().GetResult();
        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        var second = Task.Factory.StartNew(() =>
        {
            start.SignalAndWait();
            secondStore.UpsertAsync(secondRecord, default).GetAwaiter().GetResult();
        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        start.SignalAndWait();
        Task.WaitAll(first, second);

        var restored = firstStore.LoadAsync(default).GetAwaiter().GetResult();

        AssertTrue(restored.Any(record => record.ExecutionId == "execution-first" && record.Legs[0].DealId == "deal-first"), "concurrent first upsert must retain deal identity");
        AssertTrue(restored.Any(record => record.ExecutionId == "execution-second" && record.Legs[0].DealId == "deal-second"), "concurrent second upsert must retain deal identity");
        AssertEqual(0, Directory.GetFiles(directory.Path, "executions.json.tmp*").Length, "concurrent persistence must clean temporary files");
    }

    private static void ExecutionStoreQuarantinesStructurallyInvalidExecutions()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "executions.json");
        File.WriteAllText(path, "{\"schemaVersion\":1,\"executions\":[{}]}");
        var store = new SyntheticExecutionStore(path);

        var restored = store.LoadAsync(default).GetAwaiter().GetResult();

        AssertEqual(0, restored.Count, "structurally invalid executions must not load");
        AssertFalse(File.Exists(path), "structurally invalid executions must be quarantined");
        AssertEqual(1, Directory.GetFiles(directory.Path, "executions.json.corrupt-*").Length, "structurally invalid execution file must be quarantined");
    }

    private static void ExecutionStoreQuarantinesInvalidLegs()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "executions.json");
        var invalidLeg = CreatePersistedExecutionRecord().Legs[0] with
        {
            Epic = "",
            State = (SyntheticExecutionLegState)999,
        };
        var document = new
        {
            schemaVersion = 1,
            executions = new[] { CreatePersistedExecutionRecord() with { Legs = [invalidLeg] } },
        };
        File.WriteAllText(path, JsonSerializer.Serialize(document, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        }));
        var store = new SyntheticExecutionStore(path);

        var restored = store.LoadAsync(default).GetAwaiter().GetResult();

        AssertEqual(0, restored.Count, "invalid leg identity or state must not load");
        AssertFalse(File.Exists(path), "invalid leg persistence must be quarantined");
        AssertEqual(1, Directory.GetFiles(directory.Path, "executions.json.corrupt-*").Length, "invalid leg file must be quarantined");
    }

    private static void ExecutionStoreQuarantinesLegsWithMissingDirection()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "executions.json");
        var invalidLeg = CreatePersistedExecutionRecord().Legs[0] with { Direction = null! };
        var document = new
        {
            schemaVersion = 1,
            executions = new[] { CreatePersistedExecutionRecord() with { Legs = [invalidLeg] } },
        };
        File.WriteAllText(path, JsonSerializer.Serialize(document, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        }));
        var store = new SyntheticExecutionStore(path);

        var restored = store.LoadAsync(default).GetAwaiter().GetResult();

        AssertEqual(0, restored.Count, "legs with missing direction must not load");
        AssertFalse(File.Exists(path), "legs with missing direction must be quarantined");
        AssertEqual(1, Directory.GetFiles(directory.Path, "executions.json.corrupt-*").Length, "missing-direction leg file must be quarantined");
    }

    private static void ExecutionStoreQuarantinesDuplicateTrackedDealIds()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "executions.json");
        var first = CreatePersistedExecutionRecord();
        var second = CreatePersistedExecutionRecord() with { ExecutionId = "execution-duplicate-deal" };
        var document = new
        {
            schemaVersion = 1,
            executions = new[] { first, second },
        };
        File.WriteAllText(path, JsonSerializer.Serialize(document, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        }));
        var store = new SyntheticExecutionStore(path);

        var restored = store.LoadAsync(default).GetAwaiter().GetResult();

        AssertEqual(0, restored.Count, "duplicate tracked deal IDs must not load");
        AssertFalse(File.Exists(path), "duplicate tracked deal IDs must be quarantined");
        AssertEqual(1, Directory.GetFiles(directory.Path, "executions.json.corrupt-*").Length, "duplicate deal ID file must be quarantined");
    }

    private static void ExecutionStoreQuarantinesClosedLegWithoutClosedTimestamp()
    {
        var record = CreatePersistedExecutionRecord() with
        {
            State = SyntheticExecutionState.Closed,
            Legs = [CreatePersistedExecutionRecord().Legs[0] with
            {
                State = SyntheticExecutionLegState.Closed,
                ClosedUtc = null,
            }],
        };

        AssertStorePayloadIsQuarantined(record, "closed legs require a closure timestamp");
    }

    private static void ExecutionStoreQuarantinesOpenExecutionWithoutOpenLegs()
    {
        var pending = CreatePersistedExecutionRecord().Legs[0] with
        {
            State = SyntheticExecutionLegState.Pending,
            DealReference = "",
            DealId = "",
            FillLevel = null,
            SubmittedUtc = null,
            ConfirmedUtc = null,
            ClosedUtc = null,
        };
        var record = CreatePersistedExecutionRecord() with { Legs = [pending] };

        AssertStorePayloadIsQuarantined(record, "open executions require every leg to be open");
    }

    private static void ExecutionStoreQuarantinesInconsistentLegStateFields()
    {
        var confirmingWithoutReference = CreatePersistedExecutionRecord().Legs[0] with
        {
            State = SyntheticExecutionLegState.Confirming,
            DealReference = "",
            DealId = "",
            FillLevel = null,
            ConfirmedUtc = null,
            ClosedUtc = null,
        };
        var record = CreatePersistedExecutionRecord() with
        {
            State = SyntheticExecutionState.Submitting,
            Legs = [confirmingWithoutReference],
        };

        AssertStorePayloadIsQuarantined(record, "confirming legs require their submission identity");
    }

    private static void ExecutionStoreQuarantinesInconsistentExecutionStateMix()
    {
        var closed = CreatePersistedExecutionRecord().Legs[0] with
        {
            State = SyntheticExecutionLegState.Closed,
            ClosedUtc = DateTimeOffset.Parse("2026-07-30T12:02:00Z"),
        };
        var record = CreatePersistedExecutionRecord() with
        {
            State = SyntheticExecutionState.PartiallyClosed,
            Legs = [closed],
        };

        AssertStorePayloadIsQuarantined(record, "partially closed executions require both closed and unresolved legs");
    }

    private static void ExecutionStoreQuarantinesNegativeTemporalOrdering()
    {
        var record = CreatePersistedExecutionRecord() with
        {
            Legs = [CreatePersistedExecutionRecord().Legs[0] with
            {
                ConfirmedUtc = DateTimeOffset.Parse("2026-07-30T11:59:00Z"),
            }],
        };

        AssertStorePayloadIsQuarantined(record, "confirmation timestamps cannot precede submission");
    }

    private static void AssertStorePayloadIsQuarantined(SyntheticExecutionRecord record, string message)
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "executions.json");
        var document = new
        {
            schemaVersion = 1,
            executions = new[] { record },
        };
        File.WriteAllText(path, JsonSerializer.Serialize(document, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        }));
        var store = new SyntheticExecutionStore(path);

        var restored = store.LoadAsync(default).GetAwaiter().GetResult();

        AssertEqual(0, restored.Count, message);
        AssertFalse(File.Exists(path), message);
        AssertEqual(1, Directory.GetFiles(directory.Path, "executions.json.corrupt-*").Length, message);
    }

    private static void ExecutionStoreAcceptsStateMachineProgressSnapshots()
    {
        using var directory = new TemporaryDirectory();
        var gateway = AcceptedExecutionGateway("AAPL", "MSFT");
        var service = CreateExecutionService(gateway);
        var openingSnapshots = new List<SyntheticExecutionRecord>();
        var open = service.ExecuteAsync(
            CreateExecutionTicket("AAPL", "MSFT"),
            Capture(openingSnapshots),
            default).GetAwaiter().GetResult();
        gateway.ClearCalls();
        gateway.CloseResults.Enqueue(() => Task.FromResult(Acknowledgement("c_aapl")));
        gateway.CloseResults.Enqueue(() => Task.FromResult(Acknowledgement("c_msft")));
        gateway.ConfirmResults["c_aapl"] = new Queue<Func<Task<CapitalDealConfirmation>>>(
            [() => Task.FromResult(ClosedConfirmation("c_aapl", "d_aapl"))]);
        gateway.ConfirmResults["c_msft"] = new Queue<Func<Task<CapitalDealConfirmation>>>(
            [() => Task.FromResult(ClosedConfirmation("c_msft", "d_msft"))]);
        var closingSnapshots = new List<SyntheticExecutionRecord>();
        var reconciledOpen = open with
        {
            Legs = open.Legs.Select(leg => leg with { CurrentUnrealizedProfitLoss = 17.25m }).ToArray(),
        };
        service.CloseAsync(reconciledOpen, Capture(closingSnapshots), default).GetAwaiter().GetResult();

        foreach (var snapshot in openingSnapshots.Concat(closingSnapshots).Select((record, index) => (record, index)))
        {
            var store = new SyntheticExecutionStore(Path.Combine(directory.Path, $"snapshot-{snapshot.index}.json"));
            store.SaveAsync([snapshot.record], default).GetAwaiter().GetResult();
            var restored = AssertSingle(store.LoadAsync(default).GetAwaiter().GetResult(), "state machine snapshot must round-trip");
            AssertEqual(snapshot.record.State, restored.State, "state machine snapshot state");
        }
    }

    private static void ExecutionStoreAcceptsClosedPartialExecutionState()
    {
        using var directory = new TemporaryDirectory();
        var gateway = new ScriptedTradingGateway();
        gateway.PostResults.Enqueue(() => Task.FromResult(Acknowledgement("o_aapl")));
        gateway.PostResults.Enqueue(() => Task.FromResult(Acknowledgement("o_msft")));
        gateway.ConfirmResults["o_aapl"] = new Queue<Func<Task<CapitalDealConfirmation>>>(
            [() => Task.FromResult(AcceptedConfirmation("o_aapl", "d_aapl"))]);
        gateway.ConfirmResults["o_msft"] = new Queue<Func<Task<CapitalDealConfirmation>>>(
            [() => Task.FromResult(RejectedConfirmation("o_msft", "INSUFFICIENT_FUNDS"))]);
        var service = CreateExecutionService(gateway);
        var partial = service.ExecuteAsync(CreateExecutionTicket("AAPL", "MSFT", "NVDA"), IgnoreProgress, default).GetAwaiter().GetResult();
        gateway.ClearCalls();
        gateway.CloseResults.Enqueue(() => Task.FromResult(Acknowledgement("c_aapl")));
        gateway.ConfirmResults["c_aapl"] = new Queue<Func<Task<CapitalDealConfirmation>>>(
            [() => Task.FromResult(ClosedConfirmation("c_aapl", "d_aapl"))]);
        var closed = service.CloseAsync(partial, IgnoreProgress, default).GetAwaiter().GetResult();
        var store = new SyntheticExecutionStore(Path.Combine(directory.Path, "partial-closed.json"));

        store.SaveAsync([closed], default).GetAwaiter().GetResult();
        var restored = AssertSingle(store.LoadAsync(default).GetAwaiter().GetResult(), "closed partial execution must round-trip");

        AssertEqual(SyntheticExecutionState.Closed, restored.State, "partial execution closes after its only tracked position closes");
        AssertEqual(SyntheticExecutionLegState.Closed, restored.Legs[0].State, "opened leg is closed");
        AssertEqual(SyntheticExecutionLegState.Rejected, restored.Legs[1].State, "rejected leg remains terminal");
        AssertEqual(SyntheticExecutionLegState.Pending, restored.Legs[2].State, "unsent leg remains pending");
    }

    private static void EmittedExecutionAndReconciliationStatesSatisfyPersistenceContract()
    {
        using var directory = new TemporaryDirectory();
        var executionCases = new[]
        {
            new ExecutionEmissionContractCase(
                "accepted execution",
                () => CaptureExecutionContract(OpenContractOutcome.Accepted, OpenContractOutcome.Accepted),
                [
                    "Submitting|Pending,Pending",
                    "Submitting|Submitted,Pending",
                    "Submitting|Confirming,Pending",
                    "PartiallyOpen|Open,Pending",
                    "PartiallyOpen|Open,Submitted",
                    "PartiallyOpen|Open,Confirming",
                    "Open|Open,Open",
                ]),
            new ExecutionEmissionContractCase(
                "first-leg rejection",
                () => CaptureExecutionContract(OpenContractOutcome.Rejected, OpenContractOutcome.Accepted),
                [
                    "Submitting|Pending,Pending",
                    "Submitting|Submitted,Pending",
                    "Submitting|Confirming,Pending",
                    "Rejected|Rejected,Pending",
                ]),
            new ExecutionEmissionContractCase(
                "later-leg rejection",
                () => CaptureExecutionContract(OpenContractOutcome.Accepted, OpenContractOutcome.Rejected, OpenContractOutcome.Accepted),
                [
                    "Submitting|Pending,Pending,Pending",
                    "Submitting|Submitted,Pending,Pending",
                    "Submitting|Confirming,Pending,Pending",
                    "PartiallyOpen|Open,Pending,Pending",
                    "PartiallyOpen|Open,Submitted,Pending",
                    "PartiallyOpen|Open,Confirming,Pending",
                    "NeedsAttention|Open,Rejected,Pending",
                ]),
            new ExecutionEmissionContractCase(
                "first-leg unknown confirmation",
                () => CaptureExecutionContract(OpenContractOutcome.Unknown, OpenContractOutcome.Accepted),
                [
                    "Submitting|Pending,Pending",
                    "Submitting|Submitted,Pending",
                    "Submitting|Confirming,Pending",
                    "NeedsAttention|Unknown,Pending",
                ]),
            new ExecutionEmissionContractCase(
                "later-leg unknown confirmation",
                () => CaptureExecutionContract(OpenContractOutcome.Accepted, OpenContractOutcome.Unknown, OpenContractOutcome.Accepted),
                [
                    "Submitting|Pending,Pending,Pending",
                    "Submitting|Submitted,Pending,Pending",
                    "Submitting|Confirming,Pending,Pending",
                    "PartiallyOpen|Open,Pending,Pending",
                    "PartiallyOpen|Open,Submitted,Pending",
                    "PartiallyOpen|Open,Confirming,Pending",
                    "NeedsAttention|Open,Unknown,Pending",
                ]),
            new ExecutionEmissionContractCase(
                "cancelled execution before dispatch",
                CaptureCancelledExecutionContract,
                [
                    "Submitting|Pending,Pending",
                    "NeedsAttention|Pending,Pending",
                ]),
            new ExecutionEmissionContractCase(
                "accepted close",
                () => CaptureCloseContract(CloseContractOutcome.Accepted, CloseContractOutcome.Accepted),
                [
                    "Closing|Open,Open",
                    "Closing|Closing,Open",
                    "Closing|Closed,Open",
                    "Closing|Closed,Closing",
                    "Closing|Closed,Closed",
                    "Closed|Closed,Closed",
                ]),
            new ExecutionEmissionContractCase(
                "rejected close before any closure",
                () => CaptureCloseContract(CloseContractOutcome.Rejected, CloseContractOutcome.Accepted),
                [
                    "Closing|Open,Open",
                    "Closing|Closing,Open",
                    "NeedsAttention|Open,Open",
                ]),
            new ExecutionEmissionContractCase(
                "rejected close after a confirmed closure",
                () => CaptureCloseContract(CloseContractOutcome.Accepted, CloseContractOutcome.Rejected, CloseContractOutcome.Accepted),
                [
                    "Closing|Open,Open,Open",
                    "Closing|Closing,Open,Open",
                    "Closing|Closed,Open,Open",
                    "Closing|Closed,Closing,Open",
                    "PartiallyClosed|Closed,Open,Open",
                ]),
            new ExecutionEmissionContractCase(
                "unknown close confirmation",
                () => CaptureCloseContract(CloseContractOutcome.Unknown, CloseContractOutcome.Accepted),
                [
                    "Closing|Open,Open",
                    "Closing|Closing,Open",
                    "Closing|Unknown,Open",
                    "NeedsAttention|Unknown,Open",
                ]),
            new ExecutionEmissionContractCase(
                "cancelled close before dispatch",
                CaptureCancelledCloseContract,
                [
                    "Closing|Open",
                    "NeedsAttention|Open",
                ]),
            new ExecutionEmissionContractCase(
                "close partially opened execution",
                CapturePartialExecutionCloseContract,
                [
                    "Closing|Open,Rejected,Pending",
                    "Closing|Closing,Rejected,Pending",
                    "Closing|Closed,Rejected,Pending",
                    "Closed|Closed,Rejected,Pending",
                ]),
        };
        var pathIndex = 0;
        foreach (var testCase in executionCases)
        {
            var emitted = testCase.Emit();
            var actualSignatures = emitted.Select(StateSignature).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal);
            var expectedSignatures = testCase.ExpectedSignatures.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal);
            AssertEqual(
                string.Join(";", expectedSignatures),
                string.Join(";", actualSignatures),
                $"{testCase.Name} emitted state contract");

            foreach (var record in emitted)
            {
                AssertExecutionRoundTrips(record, directory.Path, pathIndex++, $"{testCase.Name} emitted record");
            }
        }

        var submittingPending = CreateContractRecord(SyntheticExecutionState.Submitting, SyntheticExecutionLegState.Pending);
        var submittingSubmitted = CreateContractRecord(SyntheticExecutionState.Submitting, SyntheticExecutionLegState.Submitted);
        var submittingConfirming = CreateContractRecord(SyntheticExecutionState.Submitting, SyntheticExecutionLegState.Confirming);
        var partiallyOpenPending = CreateContractRecord(
            SyntheticExecutionState.PartiallyOpen,
            SyntheticExecutionLegState.Open,
            SyntheticExecutionLegState.Pending);
        var partiallyOpenSubmitted = CreateContractRecord(
            SyntheticExecutionState.PartiallyOpen,
            SyntheticExecutionLegState.Open,
            SyntheticExecutionLegState.Submitted);
        var partiallyOpenConfirming = CreateContractRecord(
            SyntheticExecutionState.PartiallyOpen,
            SyntheticExecutionLegState.Open,
            SyntheticExecutionLegState.Confirming);
        var open = CreateContractRecord(
            SyntheticExecutionState.Open,
            SyntheticExecutionLegState.Open,
            SyntheticExecutionLegState.Open);
        var needsAttentionOpenRejectedPending = CreateContractRecord(
            SyntheticExecutionState.NeedsAttention,
            SyntheticExecutionLegState.Open,
            SyntheticExecutionLegState.Rejected,
            SyntheticExecutionLegState.Pending);
        var needsAttentionOpenUnknown = CreateContractRecord(
            SyntheticExecutionState.NeedsAttention,
            SyntheticExecutionLegState.Open,
            SyntheticExecutionLegState.Unknown);
        var needsAttentionOpenPending = CreateContractRecord(
            SyntheticExecutionState.NeedsAttention,
            SyntheticExecutionLegState.Open,
            SyntheticExecutionLegState.Pending);
        var needsAttentionClosedPending = CreateContractRecord(
            SyntheticExecutionState.NeedsAttention,
            SyntheticExecutionLegState.Closed,
            SyntheticExecutionLegState.Pending);
        var needsAttentionRejectedPending = CreateContractRecord(
            SyntheticExecutionState.NeedsAttention,
            SyntheticExecutionLegState.Rejected,
            SyntheticExecutionLegState.Pending);
        var needsAttentionPending = CreateContractRecord(
            SyntheticExecutionState.NeedsAttention,
            SyntheticExecutionLegState.Pending);
        var closingOpen = CreateContractRecord(SyntheticExecutionState.Closing, SyntheticExecutionLegState.Closing);
        var closingPending = CreateContractRecord(SyntheticExecutionState.Closing, SyntheticExecutionLegState.Pending);
        var partiallyClosedOpen = CreateContractRecord(
            SyntheticExecutionState.PartiallyClosed,
            SyntheticExecutionLegState.Closed,
            SyntheticExecutionLegState.Open);
        var partiallyClosedUnknown = CreateContractRecord(
            SyntheticExecutionState.PartiallyClosed,
            SyntheticExecutionLegState.Closed,
            SyntheticExecutionLegState.Unknown);
        var closed = CreateContractRecord(SyntheticExecutionState.Closed, SyntheticExecutionLegState.Closed);
        var closedPartial = CreateContractRecord(
            SyntheticExecutionState.Closed,
            SyntheticExecutionLegState.Closed,
            SyntheticExecutionLegState.Rejected,
            SyntheticExecutionLegState.Pending);
        var rejected = CreateContractRecord(
            SyntheticExecutionState.Rejected,
            SyntheticExecutionLegState.Rejected,
            SyntheticExecutionLegState.Pending);
        var reconciliationCases = new[]
        {
            new ReconciliationEmissionContractCase("submitting pending remains in flight", submittingPending, [], "Submitting|Pending"),
            new ReconciliationEmissionContractCase("submitted normalizes to unknown", submittingSubmitted, [], "NeedsAttention|Unknown"),
            new ReconciliationEmissionContractCase("confirming normalizes to unknown", submittingConfirming, [], "NeedsAttention|Unknown"),
            new ReconciliationEmissionContractCase("partially open pending remains live", partiallyOpenPending, PositionsFor(partiallyOpenPending, 0), "PartiallyOpen|Open,Pending"),
            new ReconciliationEmissionContractCase("partially open submitted normalizes", partiallyOpenSubmitted, PositionsFor(partiallyOpenSubmitted, 0), "NeedsAttention|Open,Unknown"),
            new ReconciliationEmissionContractCase("missing open with confirming normalizes", partiallyOpenConfirming, [], "NeedsAttention|Closed,Unknown"),
            new ReconciliationEmissionContractCase("all current open positions remain open", open, PositionsFor(open, 0, 1), "Open|Open,Open"),
            new ReconciliationEmissionContractCase("missing one open position partially closes", open, PositionsFor(open, 0), "PartiallyClosed|Open,Closed"),
            new ReconciliationEmissionContractCase("missing all open positions closes", open, [], "Closed|Closed,Closed"),
            new ReconciliationEmissionContractCase("open rejected pending needs attention", needsAttentionOpenRejectedPending, PositionsFor(needsAttentionOpenRejectedPending, 0), "NeedsAttention|Open,Rejected,Pending"),
            new ReconciliationEmissionContractCase("open unknown needs attention", needsAttentionOpenUnknown, PositionsFor(needsAttentionOpenUnknown, 0), "NeedsAttention|Open,Unknown"),
            new ReconciliationEmissionContractCase("open pending remains partially open", needsAttentionOpenPending, PositionsFor(needsAttentionOpenPending, 0), "PartiallyOpen|Open,Pending"),
            new ReconciliationEmissionContractCase("closed pending resolves closed", needsAttentionClosedPending, [], "Closed|Closed,Pending"),
            new ReconciliationEmissionContractCase("rejected pending resolves rejected", needsAttentionRejectedPending, [], "Rejected|Rejected,Pending"),
            new ReconciliationEmissionContractCase("pending attention remains visible", needsAttentionPending, [], "NeedsAttention|Pending"),
            new ReconciliationEmissionContractCase("closing deal still present reopens", closingOpen, PositionsFor(closingOpen, 0), "Open|Open"),
            new ReconciliationEmissionContractCase("closing pending remains closing", closingPending, [], "Closing|Pending"),
            new ReconciliationEmissionContractCase("partially closed remains partial", partiallyClosedOpen, PositionsFor(partiallyClosedOpen, 1), "PartiallyClosed|Closed,Open"),
            new ReconciliationEmissionContractCase("partially closed unknown needs attention", partiallyClosedUnknown, [], "NeedsAttention|Closed,Unknown"),
            new ReconciliationEmissionContractCase("closed deal reappears open", closed, PositionsFor(closed, 0), "Open|Open"),
            new ReconciliationEmissionContractCase("closed partial audit remains closed", closedPartial, [], "Closed|Closed,Rejected,Pending"),
            new ReconciliationEmissionContractCase("rejected execution remains rejected", rejected, [], "Rejected|Rejected,Pending"),
        };
        var reconciler = new SyntheticPositionReconciler();
        foreach (var testCase in reconciliationCases)
        {
            AssertExecutionRoundTrips(testCase.Input, directory.Path, pathIndex++, $"{testCase.Name} input");
            var reconciled = reconciler.Reconcile(
                testCase.Input,
                testCase.Positions,
                DateTimeOffset.Parse("2026-07-30T13:00:00Z"));

            AssertEqual(testCase.ExpectedSignature, StateSignature(reconciled), $"{testCase.Name} normalized state contract");
            AssertExecutionRoundTrips(reconciled, directory.Path, pathIndex++, $"{testCase.Name} output");
        }
    }

    private static IReadOnlyList<SyntheticExecutionRecord> CaptureExecutionContract(params OpenContractOutcome[] outcomes)
    {
        var epics = Enumerable.Range(0, outcomes.Length).Select(index => $"OPEN-{index}").ToArray();
        var gateway = new ScriptedTradingGateway();
        for (var index = 0; index < outcomes.Length; index++)
        {
            var reference = $"open-reference-{index}";
            var dealId = $"open-deal-{index}";
            gateway.PostResults.Enqueue(() => Task.FromResult(Acknowledgement(reference)));
            gateway.ConfirmResults[reference] = outcomes[index] switch
            {
                OpenContractOutcome.Accepted => new Queue<Func<Task<CapitalDealConfirmation>>>(
                    [() => Task.FromResult(AcceptedConfirmation(reference, dealId))]),
                OpenContractOutcome.Rejected => new Queue<Func<Task<CapitalDealConfirmation>>>(
                    [() => Task.FromResult(RejectedConfirmation(reference, "CONTRACT_REJECTED"))]),
                OpenContractOutcome.Unknown => new Queue<Func<Task<CapitalDealConfirmation>>>(
                    Enumerable.Range(0, 15).Select(_ => (Func<Task<CapitalDealConfirmation>>)(
                        () => Task.FromResult(PendingConfirmation(reference))))),
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        var snapshots = new List<SyntheticExecutionRecord>();
        CreateExecutionService(gateway).ExecuteAsync(
            CreateExecutionTicket(epics),
            Capture(snapshots),
            default).GetAwaiter().GetResult();
        return snapshots;
    }

    private static IReadOnlyList<SyntheticExecutionRecord> CaptureCancelledExecutionContract()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var snapshots = new List<SyntheticExecutionRecord>();
        CreateExecutionService(new ScriptedTradingGateway()).ExecuteAsync(
            CreateExecutionTicket("CANCEL-0", "CANCEL-1"),
            Capture(snapshots),
            cancellation.Token).GetAwaiter().GetResult();
        return snapshots;
    }

    private static IReadOnlyList<SyntheticExecutionRecord> CaptureCloseContract(params CloseContractOutcome[] outcomes)
    {
        var epics = Enumerable.Range(0, outcomes.Length).Select(index => $"CLOSE-{index}").ToArray();
        var gateway = AcceptedExecutionGateway(epics);
        var service = CreateExecutionService(gateway);
        var open = service.ExecuteAsync(CreateExecutionTicket(epics), IgnoreProgress, default).GetAwaiter().GetResult();
        gateway.ClearCalls();
        for (var index = 0; index < outcomes.Length; index++)
        {
            var reference = $"close-reference-{index}";
            var dealId = $"d_{epics[index].ToLowerInvariant()}";
            gateway.CloseResults.Enqueue(() => Task.FromResult(Acknowledgement(reference)));
            gateway.ConfirmResults[reference] = outcomes[index] switch
            {
                CloseContractOutcome.Accepted => new Queue<Func<Task<CapitalDealConfirmation>>>(
                    [() => Task.FromResult(ClosedConfirmation(reference, dealId))]),
                CloseContractOutcome.Rejected => new Queue<Func<Task<CapitalDealConfirmation>>>(
                    [() => Task.FromResult(RejectedConfirmation(reference, "CONTRACT_REJECTED"))]),
                CloseContractOutcome.Unknown => new Queue<Func<Task<CapitalDealConfirmation>>>(
                    Enumerable.Range(0, 15).Select(_ => (Func<Task<CapitalDealConfirmation>>)(
                        () => Task.FromResult(PendingConfirmation(reference))))),
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        var snapshots = new List<SyntheticExecutionRecord>();
        service.CloseAsync(open, Capture(snapshots), default).GetAwaiter().GetResult();
        return snapshots;
    }

    private static IReadOnlyList<SyntheticExecutionRecord> CaptureCancelledCloseContract()
    {
        var gateway = AcceptedExecutionGateway("CANCEL-CLOSE");
        var service = CreateExecutionService(gateway);
        var open = service.ExecuteAsync(CreateExecutionTicket("CANCEL-CLOSE"), IgnoreProgress, default).GetAwaiter().GetResult();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var snapshots = new List<SyntheticExecutionRecord>();
        service.CloseAsync(open, Capture(snapshots), cancellation.Token).GetAwaiter().GetResult();
        return snapshots;
    }

    private static IReadOnlyList<SyntheticExecutionRecord> CapturePartialExecutionCloseContract()
    {
        var gateway = new ScriptedTradingGateway();
        gateway.PostResults.Enqueue(() => Task.FromResult(Acknowledgement("partial-open-0")));
        gateway.PostResults.Enqueue(() => Task.FromResult(Acknowledgement("partial-open-1")));
        gateway.ConfirmResults["partial-open-0"] = new Queue<Func<Task<CapitalDealConfirmation>>>(
            [() => Task.FromResult(AcceptedConfirmation("partial-open-0", "partial-deal-0"))]);
        gateway.ConfirmResults["partial-open-1"] = new Queue<Func<Task<CapitalDealConfirmation>>>(
            [() => Task.FromResult(RejectedConfirmation("partial-open-1", "CONTRACT_REJECTED"))]);
        var service = CreateExecutionService(gateway);
        var partial = service.ExecuteAsync(
            CreateExecutionTicket("PARTIAL-0", "PARTIAL-1", "PARTIAL-2"),
            IgnoreProgress,
            default).GetAwaiter().GetResult();
        gateway.ClearCalls();
        gateway.CloseResults.Enqueue(() => Task.FromResult(Acknowledgement("partial-close-0")));
        gateway.ConfirmResults["partial-close-0"] = new Queue<Func<Task<CapitalDealConfirmation>>>(
            [() => Task.FromResult(ClosedConfirmation("partial-close-0", "partial-deal-0"))]);
        var snapshots = new List<SyntheticExecutionRecord>();

        service.CloseAsync(partial, Capture(snapshots), default).GetAwaiter().GetResult();
        return snapshots;
    }

    private static SyntheticExecutionRecord CreateContractRecord(
        SyntheticExecutionState state,
        params SyntheticExecutionLegState[] legStates)
    {
        var baseRecord = CreatePersistedExecutionRecord();
        return baseRecord with
        {
            ExecutionId = $"contract-{state}-{string.Join("-", legStates)}",
            State = state,
            Legs = legStates.Select((legState, index) => CreateContractLeg(baseRecord, legState, index)).ToArray(),
        };
    }

    private static SyntheticExecutionLegRecord CreateContractLeg(
        SyntheticExecutionRecord record,
        SyntheticExecutionLegState state,
        int index)
    {
        var common = record.Legs[0] with
        {
            Epic = $"CONTRACT-{index}",
            State = state,
            DealReference = "",
            DealId = "",
            CloseDealReference = "",
            FillLevel = null,
            Message = $"{state} contract leg.",
            SubmittedUtc = null,
            ConfirmedUtc = null,
            ClosedUtc = null,
            CurrentUnrealizedProfitLoss = null,
        };
        var submittedUtc = record.CreatedUtc.AddMinutes(1);
        var confirmedUtc = record.CreatedUtc.AddMinutes(2);
        return state switch
        {
            SyntheticExecutionLegState.Pending => common,
            SyntheticExecutionLegState.Submitted => common with { SubmittedUtc = submittedUtc },
            SyntheticExecutionLegState.Confirming => common with
            {
                DealReference = $"confirm-reference-{index}",
                SubmittedUtc = submittedUtc,
            },
            SyntheticExecutionLegState.Open => common with
            {
                DealReference = $"open-reference-{index}",
                DealId = $"open-deal-{index}",
                FillLevel = 101.25m,
                SubmittedUtc = submittedUtc,
                ConfirmedUtc = confirmedUtc,
            },
            SyntheticExecutionLegState.Rejected => common with
            {
                DealReference = $"rejected-reference-{index}",
                SubmittedUtc = submittedUtc,
            },
            SyntheticExecutionLegState.Unknown => common with
            {
                DealReference = $"unknown-reference-{index}",
                SubmittedUtc = submittedUtc,
            },
            SyntheticExecutionLegState.Closing => common with
            {
                DealReference = $"open-reference-{index}",
                DealId = $"open-deal-{index}",
                CloseDealReference = $"close-reference-{index}",
                FillLevel = 101.25m,
                SubmittedUtc = submittedUtc,
                ConfirmedUtc = confirmedUtc,
            },
            SyntheticExecutionLegState.Closed => common with
            {
                DealReference = $"open-reference-{index}",
                DealId = $"open-deal-{index}",
                CloseDealReference = $"close-reference-{index}",
                FillLevel = 101.25m,
                SubmittedUtc = submittedUtc,
                ConfirmedUtc = confirmedUtc,
                ClosedUtc = record.CreatedUtc.AddMinutes(4),
            },
            _ => throw new ArgumentOutOfRangeException(nameof(state)),
        };
    }

    private static IReadOnlyList<CapitalOpenPosition> PositionsFor(
        SyntheticExecutionRecord record,
        params int[] legIndexes) =>
        legIndexes.Select(index => new CapitalOpenPosition(
            record.Legs[index].DealId,
            record.Legs[index].Epic,
            record.Legs[index].Direction,
            record.Legs[index].Quantity,
            record.Legs[index].FillLevel,
            17.25m + index,
            record.Legs[index].MarginCurrency,
            "TRADEABLE")).ToArray();

    private static string StateSignature(SyntheticExecutionRecord record) =>
        $"{record.State}|{string.Join(",", record.Legs.Select(leg => leg.State))}";

    private static Task ObservePersistedProgress(
        string storePath,
        SyntheticExecutionRecord published,
        ICollection<string> lifecycleEvents)
    {
        var persisted = AssertSingle(
            new SyntheticExecutionStore(storePath).LoadAsync(default).GetAwaiter().GetResult(),
            "progress publication requires one persisted execution");
        AssertEqual(JsonSerializer.Serialize(published), JsonSerializer.Serialize(persisted), "progress must be persisted before publication");
        lifecycleEvents.Add($"PUBLISH:{StateSignature(published)}");
        return Task.CompletedTask;
    }

    private static Task ObservePersistedExecutions(
        string storePath,
        IReadOnlyList<SyntheticExecutionRecord> published,
        ICollection<string> lifecycleEvents)
    {
        var persisted = new SyntheticExecutionStore(storePath).LoadAsync(default).GetAwaiter().GetResult();
        AssertEqual(JsonSerializer.Serialize(published), JsonSerializer.Serialize(persisted), "execution list must be persisted before publication");
        lifecycleEvents.Add($"LIST:{string.Join(";", published.Select(StateSignature))}");
        return Task.CompletedTask;
    }

    private static void AssertImmediatelyBefore(
        IReadOnlyList<string> events,
        string expectedPrevious,
        string target)
    {
        var targetIndex = -1;
        for (var index = 0; index < events.Count; index++)
        {
            if (!events[index].Equals(target, StringComparison.Ordinal)) continue;
            targetIndex = index;
            break;
        }
        if (targetIndex <= 0)
        {
            throw new Exception($"lifecycle event '{target}' was missing or had no preceding persistence observation");
        }
        AssertEqual(expectedPrevious, events[targetIndex - 1], $"persistence event immediately before {target}");
    }

    private static void AssertExecutionRoundTrips(
        SyntheticExecutionRecord record,
        string directory,
        int pathIndex,
        string message)
    {
        var store = new SyntheticExecutionStore(Path.Combine(directory, $"contract-{pathIndex}.json"));
        store.SaveAsync([record], default).GetAwaiter().GetResult();
        var restored = AssertSingle(store.LoadAsync(default).GetAwaiter().GetResult(), message);

        AssertEqual(
            JsonSerializer.Serialize(record),
            JsonSerializer.Serialize(restored),
            $"{message} round-trip");
    }

    private static void ReconciliationMatchesOpenPositionsByDealIdAndUpdatesUpl()
    {
        var record = CreatePersistedExecutionRecord() with
        {
            State = SyntheticExecutionState.NeedsAttention,
            Legs = [CreatePersistedExecutionRecord().Legs[0] with
            {
                State = SyntheticExecutionLegState.Unknown,
                Message = "Submission outcome was unknown.",
            }],
        };
        var position = new CapitalOpenPosition("deal-123", "AAPL", "BUY", 3m, 99m, 17.25m, "USD", "TRADEABLE");
        var now = DateTimeOffset.Parse("2026-07-30T13:00:00Z");

        var reconciled = new SyntheticPositionReconciler().Reconcile(record, [position], now);

        AssertEqual(SyntheticExecutionLegState.Open, reconciled.Legs[0].State, "matching deal ID positively reconciles an unknown leg");
        AssertEqual(17.25m, reconciled.Legs[0].CurrentUnrealizedProfitLoss, "Capital is the source of current UPL");
        AssertEqual("deal-123", reconciled.Legs[0].DealId, "reconciliation preserves tracked deal identity");
        AssertEqual("ticket-123", reconciled.TicketId, "reconciliation preserves ticket identity");
        AssertEqual("Submission outcome was unknown.", reconciled.Legs[0].Message, "reconciliation preserves audit messages");
        AssertEqual(101.25m, reconciled.Legs[0].FillLevel, "reconciliation preserves immutable fill detail");
    }

    private static void ReconciliationMarksMissingOpenPositionsClosed()
    {
        var record = CreatePersistedExecutionRecord();
        var now = DateTimeOffset.Parse("2026-07-30T13:00:00Z");

        var reconciled = new SyntheticPositionReconciler().Reconcile(record, [], now);

        AssertEqual(SyntheticExecutionLegState.Closed, reconciled.Legs[0].State, "missing Capital position closes a known open leg");
        AssertEqual(SyntheticExecutionState.Closed, reconciled.State, "all missing known positions close the execution");
        AssertEqual(now, reconciled.Legs[0].ClosedUtc, "reconciliation records when the closure was observed");
        AssertEqual(null, reconciled.Legs[0].CurrentUnrealizedProfitLoss, "closed positions have no current UPL");
    }

    private static void ReconciliationNormalizesSubmittedLegWhenOpenPositionDisappearsAndPersistsIt()
    {
        AssertMissingOpenWithInFlightLegRoundTrips(
            SyntheticExecutionLegState.Submitted,
            "",
            "submitted sibling is normalized after the tracked open position disappears");
    }

    private static void ReconciliationNormalizesConfirmingLegWhenOpenPositionDisappearsAndPersistsIt()
    {
        AssertMissingOpenWithInFlightLegRoundTrips(
            SyntheticExecutionLegState.Confirming,
            "confirm-reference-456",
            "confirming sibling is normalized after the tracked open position disappears");
    }

    private static void AssertMissingOpenWithInFlightLegRoundTrips(
        SyntheticExecutionLegState inFlightState,
        string dealReference,
        string message)
    {
        using var directory = new TemporaryDirectory();
        var baseRecord = CreatePersistedExecutionRecord();
        var inFlight = baseRecord.Legs[0] with
        {
            Epic = "MSFT",
            State = inFlightState,
            DealReference = dealReference,
            DealId = "",
            CloseDealReference = "",
            FillLevel = null,
            Message = message,
            ConfirmedUtc = null,
            ClosedUtc = null,
            CurrentUnrealizedProfitLoss = null,
        };
        var record = baseRecord with
        {
            State = SyntheticExecutionState.PartiallyOpen,
            Legs = [baseRecord.Legs[0], inFlight],
        };
        var reconciled = new SyntheticPositionReconciler().Reconcile(
            record,
            [],
            DateTimeOffset.Parse("2026-07-30T13:00:00Z"));
        var store = new SyntheticExecutionStore(Path.Combine(directory.Path, $"{inFlightState}.json"));

        AssertEqual(SyntheticExecutionState.NeedsAttention, reconciled.State, "unresolved in-flight work keeps the execution visible");
        AssertEqual(SyntheticExecutionLegState.Closed, reconciled.Legs[0].State, "missing tracked position is closed");
        AssertEqual(SyntheticExecutionLegState.Unknown, reconciled.Legs[1].State, "orphaned in-flight leg becomes an explicit unknown outcome");
        AssertEqual(dealReference, reconciled.Legs[1].DealReference, "available in-flight deal reference is preserved");
        AssertEqual(message, reconciled.Legs[1].Message, "in-flight audit message is preserved");

        store.SaveAsync([reconciled], default).GetAwaiter().GetResult();
        var restored = AssertSingle(store.LoadAsync(default).GetAwaiter().GetResult(), "normalized in-flight reconciliation must round-trip");

        AssertEqual(reconciled.ExecutionId, restored.ExecutionId, "normalized in-flight execution identity survives save and load");
        AssertEqual(reconciled.State, restored.State, "normalized in-flight execution state survives save and load");
        AssertTrue(reconciled.Legs.SequenceEqual(restored.Legs), "normalized in-flight legs survive save and load exactly");
    }

    private static void ReconciliationReopensClosedLegWithoutClosureMetadataAndPersistsIt()
    {
        using var directory = new TemporaryDirectory();
        var baseRecord = CreatePersistedExecutionRecord();
        var closed = baseRecord.Legs[0] with
        {
            State = SyntheticExecutionLegState.Closed,
            CloseDealReference = "close-reference-123",
            Message = "Capital previously reported the position closed.",
            ClosedUtc = baseRecord.CreatedUtc.AddMinutes(4),
        };
        var record = baseRecord with
        {
            State = SyntheticExecutionState.Closed,
            Legs = [closed],
        };
        var position = new CapitalOpenPosition("deal-123", "AAPL", "BUY", 3m, 99m, 21.5m, "USD", "TRADEABLE");
        var reconciled = new SyntheticPositionReconciler().Reconcile(
            record,
            [position],
            DateTimeOffset.Parse("2026-07-30T13:00:00Z"));
        var store = new SyntheticExecutionStore(Path.Combine(directory.Path, "reopened.json"));

        AssertEqual(SyntheticExecutionState.Open, reconciled.State, "a currently reported tracked deal reopens its execution");
        AssertEqual(SyntheticExecutionLegState.Open, reconciled.Legs[0].State, "a currently reported tracked deal reopens its leg");
        AssertEqual(null, reconciled.Legs[0].ClosedUtc, "a reopened leg has no closure timestamp");
        AssertEqual("", reconciled.Legs[0].CloseDealReference, "a reopened leg has no stale close reference");
        AssertEqual("open-reference-123", reconciled.Legs[0].DealReference, "reopening preserves original open reference");
        AssertEqual("deal-123", reconciled.Legs[0].DealId, "reopening preserves permanent deal identity");
        AssertEqual("Capital previously reported the position closed.", reconciled.Legs[0].Message, "reopening preserves audit history");

        store.SaveAsync([reconciled], default).GetAwaiter().GetResult();
        var restored = AssertSingle(store.LoadAsync(default).GetAwaiter().GetResult(), "reopened reconciliation must round-trip");

        AssertEqual(reconciled.ExecutionId, restored.ExecutionId, "reopened execution identity survives save and load");
        AssertEqual(reconciled.State, restored.State, "reopened execution state survives save and load");
        AssertTrue(reconciled.Legs.SequenceEqual(restored.Legs), "reopened legs survive save and load exactly");
    }

    private static void ReconciliationLeavesUnresolvedUnknownUntilPositivelyMatched()
    {
        var original = CreatePersistedExecutionRecord() with
        {
            State = SyntheticExecutionState.NeedsAttention,
            Legs = [CreatePersistedExecutionRecord().Legs[0] with
            {
                State = SyntheticExecutionLegState.Unknown,
                DealId = "",
                Message = "No permanent deal ID was received.",
            }],
        };
        var unrelatedPosition = new CapitalOpenPosition("deal-other", "AAPL", "BUY", 3m, 99m, 17.25m, "USD", "TRADEABLE");

        var reconciled = new SyntheticPositionReconciler().Reconcile(original, [unrelatedPosition], DateTimeOffset.Parse("2026-07-30T13:00:00Z"));

        AssertEqual(SyntheticExecutionLegState.Unknown, reconciled.Legs[0].State, "unmatched unknown outcomes must remain unknown");
        AssertEqual(SyntheticExecutionState.NeedsAttention, reconciled.State, "unresolved execution must remain visible for review");
        AssertEqual("No permanent deal ID was received.", reconciled.Legs[0].Message, "unresolved audit message must remain intact");
    }

    private static void ReconciliationClosesUnknownTrackedDealWhenCapitalNoLongerListsIt()
    {
        var original = CreatePersistedExecutionRecord() with
        {
            State = SyntheticExecutionState.NeedsAttention,
            Legs = [CreatePersistedExecutionRecord().Legs[0] with
            {
                State = SyntheticExecutionLegState.Unknown,
                CloseDealReference = "close-response-lost",
                Message = "Close outcome was unknown.",
            }],
        };

        var reconciled = new SyntheticPositionReconciler().Reconcile(
            original,
            [],
            DateTimeOffset.Parse("2026-07-30T13:00:00Z"));

        AssertEqual(SyntheticExecutionLegState.Closed, reconciled.Legs[0].State, "missing tracked deal resolves an ambiguous close as closed");
        AssertEqual(SyntheticExecutionState.Closed, reconciled.State, "resolved ambiguous close closes the basket");
    }

    private static void ReconciliationMapsRejectedOpenPendingToNeedsAttentionAndPersistsIt()
    {
        using var directory = new TemporaryDirectory();
        var baseRecord = CreatePersistedExecutionRecord();
        var open = baseRecord.Legs[0] with
        {
            DealId = "deal-open",
            Message = "AAPL open audit.",
        };
        var rejected = baseRecord.Legs[0] with
        {
            Epic = "MSFT",
            State = SyntheticExecutionLegState.Rejected,
            DealReference = "reject-reference",
            DealId = "",
            CloseDealReference = "",
            FillLevel = null,
            Message = "MSFT rejected audit.",
            ConfirmedUtc = null,
            ClosedUtc = null,
            CurrentUnrealizedProfitLoss = null,
        };
        var pending = baseRecord.Legs[0] with
        {
            Epic = "NVDA",
            State = SyntheticExecutionLegState.Pending,
            DealReference = "",
            DealId = "",
            CloseDealReference = "",
            FillLevel = null,
            Message = "NVDA pending audit.",
            SubmittedUtc = null,
            ConfirmedUtc = null,
            ClosedUtc = null,
            UpdatedUtc = baseRecord.CreatedUtc,
            CurrentUnrealizedProfitLoss = null,
        };
        var record = baseRecord with
        {
            State = SyntheticExecutionState.NeedsAttention,
            Legs = [open, rejected, pending],
        };
        var now = DateTimeOffset.Parse("2026-07-30T13:00:00Z");
        var reconciled = new SyntheticPositionReconciler().Reconcile(
            record,
            [new CapitalOpenPosition("deal-open", "AAPL", "BUY", 3m, 99m, 17.25m, "USD", "TRADEABLE")],
            now);
        var store = new SyntheticExecutionStore(Path.Combine(directory.Path, "reconciled.json"));

        AssertEqual(SyntheticExecutionState.NeedsAttention, reconciled.State, "rejected plus open and pending legs need attention");
        store.SaveAsync([reconciled], default).GetAwaiter().GetResult();
        var restored = AssertSingle(store.LoadAsync(default).GetAwaiter().GetResult(), "reconciled mixture must round-trip");

        AssertEqual(SyntheticExecutionState.NeedsAttention, restored.State, "reconciled execution state survives persistence");
        AssertEqual(SyntheticExecutionLegState.Open, restored.Legs[0].State, "open leg state survives persistence");
        AssertEqual("deal-open", restored.Legs[0].DealId, "open permanent deal identity survives persistence");
        AssertEqual(17.25m, restored.Legs[0].CurrentUnrealizedProfitLoss, "Capital UPL survives persistence");
        AssertEqual("AAPL open audit.", restored.Legs[0].Message, "open audit survives persistence");
        AssertEqual(SyntheticExecutionLegState.Rejected, restored.Legs[1].State, "rejected leg state survives persistence");
        AssertEqual("reject-reference", restored.Legs[1].DealReference, "rejected audit deal reference survives persistence");
        AssertEqual("MSFT rejected audit.", restored.Legs[1].Message, "rejected audit survives persistence");
        AssertEqual(SyntheticExecutionLegState.Pending, restored.Legs[2].State, "pending leg state survives persistence");
        AssertEqual("NVDA pending audit.", restored.Legs[2].Message, "pending audit survives persistence");

        var observedClosed = baseRecord.Legs[0] with
        {
            Epic = "TSLA",
            DealId = "deal-observed-closed",
            Message = "TSLA open audit.",
        };
        var recordWithObservedClosure = record with { Legs = [open, observedClosed, rejected, pending] };
        var reconciledWithObservedClosure = new SyntheticPositionReconciler().Reconcile(
            recordWithObservedClosure,
            [new CapitalOpenPosition("deal-open", "AAPL", "BUY", 3m, 99m, 17.25m, "USD", "TRADEABLE")],
            now);

        AssertEqual(SyntheticExecutionState.NeedsAttention, reconciledWithObservedClosure.State, "rejected live mixture remains needs attention after an observed closure");
        store.SaveAsync([reconciledWithObservedClosure], default).GetAwaiter().GetResult();
        var observedClosureRestored = AssertSingle(store.LoadAsync(default).GetAwaiter().GetResult(), "observed closure reconciled mixture must round-trip");

        AssertEqual(SyntheticExecutionState.NeedsAttention, observedClosureRestored.State, "observed closure mixture state survives persistence");
        AssertEqual(SyntheticExecutionLegState.Closed, observedClosureRestored.Legs[1].State, "missing Capital deal is preserved as closed");
        AssertEqual("deal-observed-closed", observedClosureRestored.Legs[1].DealId, "closed permanent deal identity survives persistence");
        AssertEqual("TSLA open audit.", observedClosureRestored.Legs[1].Message, "closed audit survives persistence");

        var closed = new SyntheticPositionReconciler().Reconcile(record, [], now);
        store.SaveAsync([closed], default).GetAwaiter().GetResult();
        var closedRestored = AssertSingle(store.LoadAsync(default).GetAwaiter().GetResult(), "closed reconciled mixture must round-trip");

        AssertEqual(SyntheticExecutionState.Closed, closedRestored.State, "no remaining Capital position closes the execution");
        AssertEqual(SyntheticExecutionLegState.Closed, closedRestored.Legs[0].State, "absent Capital deal closes the tracked open leg");
        AssertEqual(SyntheticExecutionLegState.Rejected, closedRestored.Legs[1].State, "rejected leg remains terminal after reconciliation");
        AssertEqual(SyntheticExecutionLegState.Pending, closedRestored.Legs[2].State, "unsent leg remains terminal audit context after reconciliation");
    }

    private static void PreflightRejectsNonDemoSessions()
    {
        var result = SyntheticTradePreflight.Build(CreatePreflightInput() with { IsDemoSession = false });

        AssertFalse(result.IsReady, "live sessions must fail preflight");
        AssertContainsFailure(result, "", "Demo trading session is required.");
    }

    private static void PreflightRejectsNonHedgingAccounts()
    {
        var result = SyntheticTradePreflight.Build(CreatePreflightInput() with { HedgingMode = false });

        AssertFalse(result.IsReady, "netting accounts must fail preflight");
        AssertContainsFailure(result, "", "Capital.com hedging mode is required.");
    }

    private static void PreflightRejectsInvalidComponentCounts()
    {
        var basket = CreateBasket().Components.Take(2).ToList();
        var result = SyntheticTradePreflight.Build(CreatePreflightInput(CreateBasket(basket)));

        AssertFalse(result.IsReady, "two-leg baskets must fail preflight");
        AssertContainsFailure(result, "", "Synthetic baskets must contain 3 or 4 components.");

        var fiveLegBasket = CreateBasket(CreateBasket().Components
            .Concat([CreateComponent("DELTA", 1m), CreateComponent("EPSILON", -1m)])
            .ToList());
        var fiveLegResult = SyntheticTradePreflight.Build(CreatePreflightInput(fiveLegBasket));
        AssertFalse(fiveLegResult.IsReady, "five-leg baskets must fail preflight");
        AssertContainsFailure(fiveLegResult, "", "Synthetic baskets must contain 3 or 4 components.");
    }

    private static void PreflightRejectsDuplicateEpics()
    {
        var components = CreateBasket().Components.ToList();
        components[1] = CreateComponent("alpha", -1m);
        var result = SyntheticTradePreflight.Build(CreatePreflightInput(CreateBasket(components)));

        AssertFalse(result.IsReady, "duplicate epics must fail preflight");
        AssertContainsFailure(result, "ALPHA", "Duplicate epic.");
    }

    private static void PreflightReturnsLegFailuresInEpicOrder()
    {
        var basket = CreateBasket();
        basket.Components.Single(component => component.Instrument.Epic == "ALPHA").Instrument.Status = "SUSPENDED";
        basket.Components.Single(component => component.Instrument.Epic == "ZETA").Instrument.Status = "CLOSED";

        var result = SyntheticTradePreflight.Build(CreatePreflightInput(basket));

        AssertFalse(result.IsReady, "untradeable legs must fail preflight");
        AssertEqual(
            "ALPHA|ZETA",
            string.Join("|", result.Failures.Where(failure => failure.Reason == "Market is not TRADEABLE.").Select(failure => failure.Epic)),
            "leg failures must be reported in deterministic epic order");
    }

    private static void PreflightRejectsZeroAndStaleQuotes()
    {
        var now = DateTimeOffset.Parse("2026-07-30T12:00:00Z");
        var basket = CreateBasket();
        basket.Components.Single(component => component.Instrument.Epic == "BETA").Instrument.Bid = 0m;
        basket.Components.Single(component => component.Instrument.Epic == "GAMMA").Instrument.LastTickAt = now.AddMinutes(-5).AddTicks(-1);
        basket.Components.Single(component => component.Instrument.Epic == "ALPHA").Instrument.LastTickAt = now.AddSeconds(1);

        var result = SyntheticTradePreflight.Build(CreatePreflightInput(basket, now));

        AssertFalse(result.IsReady, "zero and stale quotes must fail preflight");
        AssertContainsFailure(result, "BETA", "Bid and offer prices must be positive.");
        AssertContainsFailure(result, "GAMMA", "Quote is older than five minutes.");
        AssertContainsFailure(result, "ALPHA", "Quote timestamp is in the future.");
    }

    private static void PreflightRejectsInvalidRoundedSize()
    {
        var components = CreateBasket().Components.ToList();
        components[0] = CreateComponent("ALPHA", 1m, 0m);
        components[0].Instrument.MinDealSize = null;
        components[0].Instrument.MinSizeIncrement = null;
        var basket = CreateBasket(components);

        var result = SyntheticTradePreflight.Build(CreatePreflightInput(basket));

        AssertFalse(result.IsReady, "zero rounded quantities must fail preflight");
        AssertContainsFailure(result, "ALPHA", "Rounded size is invalid.");
    }

    private static void PreflightRejectsMissingMargin()
    {
        var result = SyntheticTradePreflight.Build(CreatePreflightInput() with { Margin = null });

        AssertFalse(result.IsReady, "missing margin must fail preflight");
        AssertContainsFailure(result, "", "Margin preview is unavailable.");
    }

    private static void PreflightRejectsInsufficientFunds()
    {
        var result = SyntheticTradePreflight.Build(CreatePreflightInput() with { Margin = CreateMarginSummary(available: 119m) });

        AssertFalse(result.IsReady, "insufficient available funds must fail preflight");
        AssertContainsFailure(result, "", "Estimated margin exceeds available funds.");
    }

    private static void PreflightCreatesFrozenTicketWithReversedNegativeLeg()
    {
        var now = DateTimeOffset.Parse("2026-07-30T12:00:00Z");
        var basket = CreateBasket();
        var result = SyntheticTradePreflight.Build(CreatePreflightInput(basket, now));

        AssertTrue(result.IsReady, "valid demo basket must be ready");
        var ticket = result.Ticket ?? throw new Exception("ready preflight must create a ticket");
        AssertTrue(!string.IsNullOrWhiteSpace(ticket.TicketId), "ticket must have an ID");
        AssertEqual("basket-123", ticket.BasketId, "ticket basket ID");
        AssertEqual("BUY", ticket.Side, "ticket side");
        AssertEqual(600m, ticket.RequestedNotional, "ticket requested notional");
        AssertEqual(now, ticket.CreatedUtc, "ticket creation time");
        AssertEqual(now.AddMinutes(2), ticket.ExpiresUtc, "ticket expiry");
        AssertEqual(120m, ticket.EstimatedMargin, "ticket estimated margin");
        var negativeLeg = ticket.Legs.Single(leg => leg.Multiplier < 0m);
        AssertEqual("SELL", negativeLeg.Direction, "negative leg reverses BUY basket");
        AssertEqual(15m, negativeLeg.Quantity, "ticket copies executable quantity");
        AssertEqual(10m, negativeLeg.ReferencePrice, "ticket copies executable price");
        AssertEqual(30m, negativeLeg.EstimatedMargin, "ticket copies executable margin");

        basket.Components.Single(component => component.Instrument.Epic == negativeLeg.Epic).Instrument.Bid = 99m;
        AssertEqual(10m, negativeLeg.ReferencePrice, "chart updates must not mutate a frozen ticket");
    }

    private static void ExecutionWaitsForAcceptedConfirmationBeforeSubmittingNextLeg()
    {
        var gateway = new ScriptedTradingGateway();
        gateway.PostResults.Enqueue(() => Task.FromResult(Acknowledgement("o_aapl")));
        gateway.PostResults.Enqueue(() => Task.FromResult(Acknowledgement("o_msft")));
        gateway.ConfirmResults["o_aapl"] = new Queue<Func<Task<CapitalDealConfirmation>>>(
            [() => Task.FromResult(AcceptedConfirmation("o_aapl", "d_aapl"))]);
        gateway.ConfirmResults["o_msft"] = new Queue<Func<Task<CapitalDealConfirmation>>>(
            [() => Task.FromResult(AcceptedConfirmation("o_msft", "d_msft") with
            {
                AffectedDeals = [new CapitalAffectedDeal("d_msft", "OPENED")],
            })]);
        var progress = new List<SyntheticExecutionRecord>();

        var result = CreateExecutionService(gateway).ExecuteAsync(
            CreateExecutionTicket("AAPL", "MSFT"),
            Capture(progress),
            default).GetAwaiter().GetResult();

        AssertSequence(gateway.Calls, "POST:AAPL", "CONFIRM:o_aapl", "POST:MSFT", "CONFIRM:o_msft");
        AssertEqual(SyntheticExecutionState.Open, result.State, "accepted basket state");
        AssertTrue(result.Legs.All(leg => leg.State == SyntheticExecutionLegState.Open), "every accepted leg must be open");
        AssertEqual("d_aapl", result.Legs[0].DealId, "first permanent deal ID");
        AssertEqual("d_msft", result.Legs[1].DealId, "second permanent deal ID");
        AssertTrue(progress.Count > 4, "every execution transition must be published");
        AssertEqual(SyntheticExecutionLegState.Pending, progress[0].Legs[0].State, "record must be published before the first submission");
    }

    private static void AcceptedConfirmationRequiresExplicitOpenedAffectedDeal()
    {
        var gateway = new ScriptedTradingGateway();
        gateway.PostResults.Enqueue(() => Task.FromResult(Acknowledgement("o_alpha")));
        gateway.ConfirmResults["o_alpha"] = new Queue<Func<Task<CapitalDealConfirmation>>>(
            [() => Task.FromResult(new CapitalDealConfirmation("o_alpha", "ACCEPTED", "top-level-deal", 101m, [], ""))]);

        var result = Execute(gateway, "ALPHA");

        AssertEqual(SyntheticExecutionState.NeedsAttention, result.State, "accepted confirmation without OPENED affected deal needs attention");
        AssertEqual(SyntheticExecutionLegState.Unknown, result.Legs[0].State, "top-level deal ID must not be treated as a newly opened position");
        AssertEqual("", result.Legs[0].DealId, "unverified top-level deal ID must not be persisted as an open position");
    }

    private static void ExplicitRejectionStopsUnsentLegs()
    {
        var gateway = new ScriptedTradingGateway();
        gateway.PostResults.Enqueue(() => Task.FromResult(Acknowledgement("o_aapl")));
        gateway.ConfirmResults["o_aapl"] = new Queue<Func<Task<CapitalDealConfirmation>>>(
            [() => Task.FromResult(RejectedConfirmation("o_aapl", "MARKET_CLOSED"))]);

        var result = Execute(gateway, "AAPL", "MSFT");

        AssertSequence(gateway.Calls, "POST:AAPL", "CONFIRM:o_aapl");
        AssertEqual(SyntheticExecutionState.Rejected, result.State, "fully rejected basket state");
        AssertEqual(SyntheticExecutionLegState.Rejected, result.Legs[0].State, "rejected leg state");
        AssertEqual(SyntheticExecutionLegState.Pending, result.Legs[1].State, "unsent leg remains pending");
        AssertTrue(result.Legs[0].Message.Contains("MARKET_CLOSED", StringComparison.Ordinal), "Capital rejection reason must be retained");
    }

    private static void MalformedAcknowledgementStopsWithoutRetry()
    {
        var gateway = new ScriptedTradingGateway();
        gateway.PostResults.Enqueue(() => Task.FromResult(new CapitalDealAcknowledgement("", "", "")));

        var result = Execute(gateway, "AAPL", "MSFT");

        AssertSequence(gateway.Calls, "POST:AAPL");
        AssertEqual(SyntheticExecutionState.NeedsAttention, result.State, "malformed acknowledgement basket state");
        AssertEqual(SyntheticExecutionLegState.Unknown, result.Legs[0].State, "malformed acknowledgement state");
        AssertContains(result.Legs[0].Message, "deal reference", "malformed acknowledgement message");
        AssertEqual(1, gateway.PostCalls.Count, "malformed acknowledgement must not retry POST");
    }

    private static void ConfirmationTimeoutIsUnknownWithoutRetry()
    {
        var gateway = new ScriptedTradingGateway();
        gateway.PostResults.Enqueue(() => Task.FromResult(Acknowledgement("o_aapl")));
        gateway.ConfirmResults["o_aapl"] = new Queue<Func<Task<CapitalDealConfirmation>>>(
            Enumerable.Range(0, 15).Select(_ => (Func<Task<CapitalDealConfirmation>>)(
                () => Task.FromResult(PendingConfirmation("o_aapl")))));
        var clock = new TestExecutionClock();

        var result = CreateExecutionService(gateway, clock).ExecuteAsync(
            CreateExecutionTicket("AAPL", "MSFT"),
            IgnoreProgress,
            default).GetAwaiter().GetResult();

        AssertEqual(SyntheticExecutionState.NeedsAttention, result.State, "timed-out basket state");
        AssertEqual(SyntheticExecutionLegState.Unknown, result.Legs[0].State, "timed-out leg state");
        AssertEqual(15, gateway.ConfirmCalls.Count, "confirmation polling must be bounded");
        AssertEqual(15, clock.Delays.Count, "confirmation timeout must span fifteen injected seconds");
        AssertEqual(TimeSpan.FromSeconds(1), clock.Delays[0], "confirmation polling delay");
        AssertEqual(1, gateway.PostCalls.Count, "confirmation timeout must not retry POST");
    }

    private static void GenericCreateFailureIsUnknownWithoutRetry()
    {
        var gateway = new ScriptedTradingGateway();
        gateway.PostResults.Enqueue(() => Task.FromException<CapitalDealAcknowledgement>(new InvalidOperationException("connection reset after dispatch")));

        var result = Execute(gateway, "AAPL", "MSFT");

        AssertEqual(SyntheticExecutionState.NeedsAttention, result.State, "generic create failure basket state");
        AssertEqual(SyntheticExecutionLegState.Unknown, result.Legs[0].State, "generic create failure leg state");
        AssertEqual(SyntheticExecutionLegState.Pending, result.Legs[1].State, "network failure stops unsent legs");
        AssertEqual(1, gateway.PostCalls.Count, "network failure must not retry POST");
    }

    private static void AmbiguousMutationFailureIsUnknownWithoutRetry()
    {
        var gateway = new ScriptedTradingGateway();
        gateway.PostResults.Enqueue(() => Task.FromException<CapitalDealAcknowledgement>(
            new CapitalMutationOutcomeUnknownException("connection lost after dispatch")));

        var result = Execute(gateway, "AAPL", "MSFT");

        AssertEqual(SyntheticExecutionState.NeedsAttention, result.State, "ambiguous basket state");
        AssertEqual(SyntheticExecutionLegState.Unknown, result.Legs[0].State, "ambiguous leg state");
        AssertEqual(SyntheticExecutionLegState.Pending, result.Legs[1].State, "ambiguous failure stops unsent legs");
        AssertEqual(1, gateway.PostCalls.Count, "ambiguous failure must not retry POST");
    }

    private static void CancellationStopsUnsentLegsAfterAcceptedLeg()
    {
        var gateway = new ScriptedTradingGateway();
        gateway.PostResults.Enqueue(() => Task.FromResult(Acknowledgement("o_aapl")));
        gateway.ConfirmResults["o_aapl"] = new Queue<Func<Task<CapitalDealConfirmation>>>(
            [() => Task.FromResult(AcceptedConfirmation("o_aapl", "d_aapl"))]);
        using var cancellation = new CancellationTokenSource();
        SyntheticExecutionProgress progress = (record, _) =>
        {
            if (record.Legs[0].State == SyntheticExecutionLegState.Open) cancellation.Cancel();
            return Task.CompletedTask;
        };

        var result = CreateExecutionService(gateway).ExecuteAsync(
            CreateExecutionTicket("AAPL", "MSFT"),
            progress,
            cancellation.Token).GetAwaiter().GetResult();

        AssertSequence(gateway.Calls, "POST:AAPL", "CONFIRM:o_aapl");
        AssertEqual(SyntheticExecutionState.NeedsAttention, result.State, "cancelled partial basket state");
        AssertEqual(SyntheticExecutionLegState.Open, result.Legs[0].State, "accepted leg remains open after cancellation");
        AssertEqual(SyntheticExecutionLegState.Pending, result.Legs[1].State, "cancelled unsent leg remains pending");
    }

    private static void PartialSuccessRemainsOpenWithoutRollback()
    {
        var gateway = new ScriptedTradingGateway();
        gateway.PostResults.Enqueue(() => Task.FromResult(Acknowledgement("o_aapl")));
        gateway.PostResults.Enqueue(() => Task.FromResult(Acknowledgement("o_msft")));
        gateway.ConfirmResults["o_aapl"] = new Queue<Func<Task<CapitalDealConfirmation>>>(
            [() => Task.FromResult(AcceptedConfirmation("o_aapl", "d_aapl"))]);
        gateway.ConfirmResults["o_msft"] = new Queue<Func<Task<CapitalDealConfirmation>>>(
            [() => Task.FromResult(RejectedConfirmation("o_msft", "INSUFFICIENT_FUNDS"))]);

        var partial = Execute(gateway, "AAPL", "MSFT", "NVDA");

        AssertSequence(gateway.Calls, "POST:AAPL", "CONFIRM:o_aapl", "POST:MSFT", "CONFIRM:o_msft");
        AssertEqual(SyntheticExecutionState.NeedsAttention, partial.State, "partial basket remains visible");
        AssertEqual(SyntheticExecutionLegState.Open, partial.Legs[0].State, "successful leg stays open");
        AssertEqual("d_aapl", partial.Legs[0].DealId, "successful permanent deal ID is retained");
        AssertEqual(SyntheticExecutionLegState.Pending, partial.Legs[2].State, "leg after failure is not sent");
        AssertEqual(0, gateway.CloseCalls.Count, "execution failure must not roll back opened legs");
    }

    private static void CloseConfirmsOnlyTrackedOpenDealIds()
    {
        var gateway = AcceptedExecutionGateway("AAPL", "MSFT");
        var service = CreateExecutionService(gateway);
        var open = service.ExecuteAsync(CreateExecutionTicket("AAPL", "MSFT"), IgnoreProgress, default).GetAwaiter().GetResult();
        gateway.ClearCalls();
        gateway.CloseResults.Enqueue(() => Task.FromResult(Acknowledgement("c_aapl")));
        gateway.CloseResults.Enqueue(() => Task.FromResult(Acknowledgement("c_msft")));
        gateway.ConfirmResults["c_aapl"] = new Queue<Func<Task<CapitalDealConfirmation>>>(
            [() => Task.FromResult(ClosedConfirmation("c_aapl", "d_aapl"))]);
        gateway.ConfirmResults["c_msft"] = new Queue<Func<Task<CapitalDealConfirmation>>>(
            [() => Task.FromResult(ClosedConfirmation("c_msft", "d_msft"))]);

        var closed = service.CloseAsync(open, IgnoreProgress, default).GetAwaiter().GetResult();

        AssertSequence(gateway.Calls, "CLOSE:d_aapl", "CONFIRM:c_aapl", "CLOSE:d_msft", "CONFIRM:c_msft");
        AssertEqual("d_aapl|d_msft", string.Join("|", gateway.CloseCalls), "close uses tracked open deal IDs");
        AssertEqual(SyntheticExecutionState.Closed, closed.State, "closed basket state");
        AssertTrue(closed.Legs.All(leg => leg.State == SyntheticExecutionLegState.Closed), "all confirmed closes are closed");
        AssertEqual("o_aapl", closed.Legs[0].DealReference, "close must preserve the original open reference");
        AssertEqual("c_aapl", closed.Legs[0].CloseDealReference, "close acknowledgement reference");
        AssertTrue(closed.Legs[0].ClosedUtc is not null, "confirmed close timestamp");
    }

    private static void PartialClosePreservesRemainingOpenLeg()
    {
        var gateway = AcceptedExecutionGateway("AAPL", "MSFT", "NVDA");
        var service = CreateExecutionService(gateway);
        var open = service.ExecuteAsync(CreateExecutionTicket("AAPL", "MSFT", "NVDA"), IgnoreProgress, default).GetAwaiter().GetResult();
        gateway.ClearCalls();
        gateway.CloseResults.Enqueue(() => Task.FromResult(Acknowledgement("c_aapl")));
        gateway.CloseResults.Enqueue(() => Task.FromResult(Acknowledgement("c_msft")));
        gateway.ConfirmResults["c_aapl"] = new Queue<Func<Task<CapitalDealConfirmation>>>(
            [() => Task.FromResult(ClosedConfirmation("c_aapl", "d_aapl"))]);
        gateway.ConfirmResults["c_msft"] = new Queue<Func<Task<CapitalDealConfirmation>>>(
            [() => Task.FromResult(RejectedConfirmation("c_msft", "POSITION_NOT_FOUND"))]);

        var partial = service.CloseAsync(open, IgnoreProgress, default).GetAwaiter().GetResult();

        AssertSequence(gateway.Calls, "CLOSE:d_aapl", "CONFIRM:c_aapl", "CLOSE:d_msft", "CONFIRM:c_msft");
        AssertEqual(SyntheticExecutionState.PartiallyClosed, partial.State, "partial close basket state");
        AssertEqual(SyntheticExecutionLegState.Closed, partial.Legs[0].State, "confirmed close state");
        AssertEqual(SyntheticExecutionLegState.Open, partial.Legs[1].State, "rejected close remains explicitly open");
        AssertContains(partial.Legs[1].Message, "POSITION_NOT_FOUND", "close rejection reason");
        AssertEqual(SyntheticExecutionLegState.Open, partial.Legs[2].State, "close failure stops later open legs");
        AssertEqual(2, gateway.CloseCalls.Count, "failed close must not retry or continue");
    }

    private static void GenericCloseFailureIsUnknownAndCannotBeRetriedBlindly()
    {
        var gateway = AcceptedExecutionGateway("AAPL");
        var service = CreateExecutionService(gateway);
        var open = service.ExecuteAsync(CreateExecutionTicket("AAPL"), IgnoreProgress, default).GetAwaiter().GetResult();
        gateway.ClearCalls();
        gateway.CloseResults.Enqueue(() => Task.FromException<CapitalDealAcknowledgement>(
            new InvalidOperationException("connection reset after close dispatch")));

        var unknown = service.CloseAsync(open, IgnoreProgress, default).GetAwaiter().GetResult();

        AssertEqual(SyntheticExecutionState.NeedsAttention, unknown.State, "generic close failure basket state");
        AssertEqual(SyntheticExecutionLegState.Unknown, unknown.Legs[0].State, "generic close failure leg state");
        AssertEqual(1, gateway.CloseCalls.Count, "generic close failure must not retry DELETE");
        gateway.ClearCalls();

        service.CloseAsync(unknown, IgnoreProgress, default).GetAwaiter().GetResult();

        AssertEqual(0, gateway.CloseCalls.Count, "unknown close outcome must not permit a blind second DELETE");
    }

    private static void MalformedCloseAcknowledgementIsUnknownAndCannotBeRetriedBlindly()
    {
        var gateway = AcceptedExecutionGateway("AAPL");
        var service = CreateExecutionService(gateway);
        var open = service.ExecuteAsync(CreateExecutionTicket("AAPL"), IgnoreProgress, default).GetAwaiter().GetResult();
        gateway.ClearCalls();
        gateway.CloseResults.Enqueue(() => Task.FromResult(new CapitalDealAcknowledgement("", "", "")));

        var unknown = service.CloseAsync(open, IgnoreProgress, default).GetAwaiter().GetResult();

        AssertEqual(SyntheticExecutionState.NeedsAttention, unknown.State, "malformed close acknowledgement basket state");
        AssertEqual(SyntheticExecutionLegState.Unknown, unknown.Legs[0].State, "malformed close acknowledgement leg state");
        AssertContains(unknown.Legs[0].Message, "deal reference", "malformed close acknowledgement message");
        AssertEqual(1, gateway.CloseCalls.Count, "malformed close acknowledgement must not retry DELETE");
        gateway.ClearCalls();

        service.CloseAsync(unknown, IgnoreProgress, default).GetAwaiter().GetResult();

        AssertEqual(0, gateway.CloseCalls.Count, "malformed close outcome must not permit a blind second DELETE");
    }

    private static void CreateAcknowledgementPersistenceIgnoresCallerCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        var gateway = new ScriptedTradingGateway();
        gateway.PostResults.Enqueue(() =>
        {
            cancellation.Cancel();
            return Task.FromResult(Acknowledgement("o_aapl"));
        });
        var persisted = new List<SyntheticExecutionRecord>();

        var result = CreateExecutionService(gateway).ExecuteAsync(
            CreateExecutionTicket("AAPL", "MSFT"),
            CaptureAndHonorCancellation(persisted),
            cancellation.Token).GetAwaiter().GetResult();

        AssertEqual(SyntheticExecutionState.NeedsAttention, result.State, "cancelled acknowledged create basket state");
        AssertEqual(SyntheticExecutionLegState.Unknown, result.Legs[0].State, "cancelled acknowledged create outcome");
        AssertEqual("o_aapl", result.Legs[0].DealReference, "acknowledgement reference must survive cancellation");
        AssertTrue(persisted.Any(record => record.Legs[0].State == SyntheticExecutionLegState.Confirming), "acknowledgement state must be critically persisted");
        AssertTrue(persisted.Any(record => record.Legs[0].State == SyntheticExecutionLegState.Unknown), "unknown outcome must be critically persisted");
        AssertEqual(0, gateway.ConfirmCalls.Count, "cancellation before confirmation stops polling");
        AssertEqual(1, gateway.PostCalls.Count, "acknowledged mutation must not be retried");
    }

    private static void AcceptedCreatePersistenceIgnoresCallerCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        var gateway = new ScriptedTradingGateway();
        gateway.PostResults.Enqueue(() => Task.FromResult(Acknowledgement("o_aapl")));
        gateway.ConfirmResults["o_aapl"] = new Queue<Func<Task<CapitalDealConfirmation>>>(
            [() =>
            {
                cancellation.Cancel();
                return Task.FromResult(AcceptedConfirmation("o_aapl", "d_aapl"));
            }]);
        var persisted = new List<SyntheticExecutionRecord>();

        var result = CreateExecutionService(gateway).ExecuteAsync(
            CreateExecutionTicket("AAPL", "MSFT"),
            CaptureAndHonorCancellation(persisted),
            cancellation.Token).GetAwaiter().GetResult();

        AssertEqual(SyntheticExecutionState.NeedsAttention, result.State, "cancelled accepted create basket state");
        AssertEqual(SyntheticExecutionLegState.Open, result.Legs[0].State, "accepted leg remains open after cancellation");
        AssertEqual(SyntheticExecutionLegState.Pending, result.Legs[1].State, "cancellation stops future create mutations");
        AssertTrue(persisted.Any(record => record.Legs[0].State == SyntheticExecutionLegState.Open), "accepted open state must be critically persisted");
        AssertSequence(gateway.Calls, "POST:AAPL", "CONFIRM:o_aapl");
    }

    private static void CloseAcknowledgementPersistenceIgnoresCallerCancellation()
    {
        var gateway = AcceptedExecutionGateway("AAPL");
        var service = CreateExecutionService(gateway);
        var open = service.ExecuteAsync(CreateExecutionTicket("AAPL"), IgnoreProgress, default).GetAwaiter().GetResult();
        gateway.ClearCalls();
        using var cancellation = new CancellationTokenSource();
        gateway.CloseResults.Enqueue(() =>
        {
            cancellation.Cancel();
            return Task.FromResult(Acknowledgement("c_aapl"));
        });
        var persisted = new List<SyntheticExecutionRecord>();

        var result = service.CloseAsync(open, CaptureAndHonorCancellation(persisted), cancellation.Token).GetAwaiter().GetResult();

        AssertEqual(SyntheticExecutionState.NeedsAttention, result.State, "cancelled acknowledged close basket state");
        AssertEqual(SyntheticExecutionLegState.Unknown, result.Legs[0].State, "cancelled acknowledged close outcome");
        AssertEqual("c_aapl", result.Legs[0].CloseDealReference, "close acknowledgement must survive cancellation");
        AssertTrue(persisted.Any(record => record.Legs[0].CloseDealReference == "c_aapl"), "close acknowledgement state must be critically persisted");
        AssertTrue(persisted.Any(record => record.Legs[0].State == SyntheticExecutionLegState.Unknown), "unknown close outcome must be critically persisted");
        AssertEqual(0, gateway.ConfirmCalls.Count, "cancellation before close confirmation stops polling");
        AssertEqual(1, gateway.CloseCalls.Count, "acknowledged close must not be retried");
    }

    private static void AcceptedClosePersistenceIgnoresCallerCancellation()
    {
        var gateway = AcceptedExecutionGateway("AAPL", "MSFT");
        var service = CreateExecutionService(gateway);
        var open = service.ExecuteAsync(CreateExecutionTicket("AAPL", "MSFT"), IgnoreProgress, default).GetAwaiter().GetResult();
        gateway.ClearCalls();
        using var cancellation = new CancellationTokenSource();
        gateway.CloseResults.Enqueue(() => Task.FromResult(Acknowledgement("c_aapl")));
        gateway.ConfirmResults["c_aapl"] = new Queue<Func<Task<CapitalDealConfirmation>>>(
            [() =>
            {
                cancellation.Cancel();
                return Task.FromResult(ClosedConfirmation("c_aapl", "d_aapl"));
            }]);
        var persisted = new List<SyntheticExecutionRecord>();

        var result = service.CloseAsync(open, CaptureAndHonorCancellation(persisted), cancellation.Token).GetAwaiter().GetResult();

        AssertEqual(SyntheticExecutionState.PartiallyClosed, result.State, "cancelled accepted close basket state");
        AssertEqual(SyntheticExecutionLegState.Closed, result.Legs[0].State, "accepted close remains closed after cancellation");
        AssertEqual(SyntheticExecutionLegState.Open, result.Legs[1].State, "cancellation stops future close mutations");
        AssertTrue(persisted.Any(record => record.Legs[0].State == SyntheticExecutionLegState.Closed), "accepted closed state must be critically persisted");
        AssertSequence(gateway.Calls, "CLOSE:d_aapl", "CONFIRM:c_aapl");
    }

    private static void CancellationDuringLaterCreatePersistenceStopsBeforeGateway()
    {
        using var cancellation = new CancellationTokenSource();
        var gateway = new ScriptedTradingGateway();
        gateway.PostResults.Enqueue(() => Task.FromResult(Acknowledgement("o_aapl")));
        gateway.ConfirmResults["o_aapl"] = new Queue<Func<Task<CapitalDealConfirmation>>>(
            [() => Task.FromResult(AcceptedConfirmation("o_aapl", "d_aapl"))]);
        var persisted = new List<SyntheticExecutionRecord>();
        SyntheticExecutionProgress progress = (record, persistenceToken) =>
        {
            persistenceToken.ThrowIfCancellationRequested();
            persisted.Add(record);
            if (record.Legs[0].State == SyntheticExecutionLegState.Open
                && record.Legs[1].State == SyntheticExecutionLegState.Submitted)
            {
                cancellation.Cancel();
            }
            return Task.CompletedTask;
        };

        var result = CreateExecutionService(gateway).ExecuteAsync(
            CreateExecutionTicket("AAPL", "MSFT"),
            progress,
            cancellation.Token).GetAwaiter().GetResult();

        AssertEqual(1, gateway.CreateInvocations, "cancellation after later create persistence must stop before gateway entry");
        AssertSequence(gateway.Calls, "POST:AAPL", "CONFIRM:o_aapl");
        AssertEqual(SyntheticExecutionState.NeedsAttention, result.State, "cancelled later create basket state");
        AssertEqual(SyntheticExecutionLegState.Open, result.Legs[0].State, "earlier open state remains durable");
        AssertEqual("d_aapl", result.Legs[0].DealId, "earlier open deal ID remains durable");
        AssertEqual(SyntheticExecutionLegState.Pending, result.Legs[1].State, "unsent later create leg remains pending");
        AssertEqual(result, persisted[^1], "final execute state must be durably persisted");
    }

    private static void CancellationDuringLaterClosePersistenceStopsBeforeGateway()
    {
        var gateway = AcceptedExecutionGateway("AAPL", "MSFT");
        var service = CreateExecutionService(gateway);
        var open = service.ExecuteAsync(CreateExecutionTicket("AAPL", "MSFT"), IgnoreProgress, default).GetAwaiter().GetResult();
        gateway.ClearCalls();
        gateway.CloseResults.Enqueue(() => Task.FromResult(Acknowledgement("c_aapl")));
        gateway.ConfirmResults["c_aapl"] = new Queue<Func<Task<CapitalDealConfirmation>>>(
            [() => Task.FromResult(ClosedConfirmation("c_aapl", "d_aapl"))]);
        using var cancellation = new CancellationTokenSource();
        var persisted = new List<SyntheticExecutionRecord>();
        SyntheticExecutionProgress progress = (record, persistenceToken) =>
        {
            persistenceToken.ThrowIfCancellationRequested();
            persisted.Add(record);
            if (record.Legs[0].State == SyntheticExecutionLegState.Closed
                && record.Legs[1].State == SyntheticExecutionLegState.Closing)
            {
                cancellation.Cancel();
            }
            return Task.CompletedTask;
        };

        var result = service.CloseAsync(open, progress, cancellation.Token).GetAwaiter().GetResult();

        AssertEqual(1, gateway.CloseInvocations, "cancellation after later close persistence must stop before gateway entry");
        AssertSequence(gateway.Calls, "CLOSE:d_aapl", "CONFIRM:c_aapl");
        AssertEqual(SyntheticExecutionState.PartiallyClosed, result.State, "cancelled later close basket state");
        AssertEqual(SyntheticExecutionLegState.Closed, result.Legs[0].State, "earlier closed state remains durable");
        AssertTrue(result.Legs[0].ClosedUtc is not null, "earlier close timestamp remains durable");
        AssertEqual(SyntheticExecutionLegState.Open, result.Legs[1].State, "unsent later close leg remains open");
        AssertEqual(result, persisted[^1], "final close state must be durably persisted");
    }

    private static SyntheticPreflightInput CreatePreflightInput(
        SyntheticBasket? basket = null,
        DateTimeOffset? now = null) =>
        new(
            true,
            "basket-123",
            basket ?? CreateBasket(),
            "BUY",
            600m,
            now ?? DateTimeOffset.Parse("2026-07-30T12:00:00Z"),
            CreateMarginSummary(),
            "account-123",
            true);

    private static SyntheticPreflightInput CreateThreeLegPreflightInput() =>
        CreatePreflightInput(CreateBasket([
            CreateComponent("ALPHA", 1m, 100m / 3m),
            CreateComponent("BETA", -1m, 100m / 3m),
            CreateComponent("GAMMA", 1m, 100m / 3m),
        ]));

    private static SyntheticExecutionRecord Execute(ScriptedTradingGateway gateway, params string[] epics) =>
        CreateExecutionService(gateway).ExecuteAsync(CreateExecutionTicket(epics), IgnoreProgress, default).GetAwaiter().GetResult();

    private static SyntheticBasketExecutionService CreateExecutionService(
        ScriptedTradingGateway gateway,
        ISyntheticExecutionClock? clock = null) =>
        new(gateway, clock ?? new TestExecutionClock());

    private static SyntheticTradingHostCoordinator CreateHostCoordinator(
        string directory,
        ScriptedTradingGateway gateway,
        Func<bool>? isDemo = null,
        Func<DateTimeOffset>? utcNow = null,
        SyntheticExecutionStore? store = null,
        Func<CancellationToken, Task<IReadOnlyList<CapitalOpenPosition>>>? getOpenPositions = null,
        ISyntheticExecutionClock? clock = null,
        Func<string>? currentAccountId = null)
    {
        var executionClock = clock ?? new TestExecutionClock();
        return new SyntheticTradingHostCoordinator(
            new SyntheticBasketExecutionService(gateway, executionClock),
            store ?? new SyntheticExecutionStore(Path.Combine(directory, "executions.json")),
            new SyntheticPositionReconciler(),
            isDemo ?? (() => true),
            getOpenPositions ?? (_ => Task.FromResult<IReadOnlyList<CapitalOpenPosition>>([])),
            utcNow ?? (() => executionClock.UtcNow),
            currentAccountId);
    }

    private static SyntheticExecutionTicket CreateHostTicket(
        Guid ticketId,
        IReadOnlyList<SyntheticExecutionLeg>? legs = null)
    {
        var now = DateTimeOffset.Parse("2026-07-30T12:00:00Z");
        return new SyntheticExecutionTicket(
            ticketId.ToString("N"),
            "basket-123",
            "BUY",
            300m,
            now,
            now.AddMinutes(2),
            60m,
            "USD",
            legs ?? [new SyntheticExecutionLeg("AAPL", "BUY", 1m, 100m, 1m, 100m, 20m, "USD")]);
    }

    private static SyntheticExecutionTicket CreateExecutionTicket(params string[] epics)
    {
        var now = DateTimeOffset.Parse("2026-07-30T12:00:00Z");
        return new SyntheticExecutionTicket(
            "ticket-123",
            "basket-123",
            "BUY",
            300m,
            now,
            now.AddMinutes(2),
            60m,
            "USD",
            epics.Select(epic => new SyntheticExecutionLeg(epic, "BUY", 1m, 100m, 1m, 100m, 20m, "USD")).ToArray());
    }

    private static SyntheticExecutionRecord CreatePersistedExecutionRecord()
    {
        var created = DateTimeOffset.Parse("2026-07-30T12:00:00Z");
        var updated = created.AddMinutes(5);
        return new SyntheticExecutionRecord(
            "execution-123",
            "ticket-123",
            "basket-123",
            "BUY",
            300m,
            60m,
            "USD",
            created,
            updated,
            SyntheticExecutionState.Open,
            [new SyntheticExecutionLegRecord(
                "AAPL",
                "BUY",
                1m,
                100m,
                3m,
                300m,
                60m,
                "USD",
                SyntheticExecutionLegState.Open,
                "open-reference-123",
                "deal-123",
                "",
                101.25m,
                "Capital accepted the position.",
                created,
                created.AddMinutes(1),
                null,
                updated)]);
    }

    private static ScriptedTradingGateway AcceptedExecutionGateway(params string[] epics)
    {
        var gateway = new ScriptedTradingGateway();
        foreach (var epic in epics)
        {
            var suffix = epic.ToLowerInvariant();
            gateway.PostResults.Enqueue(() => Task.FromResult(Acknowledgement($"o_{suffix}")));
            gateway.ConfirmResults[$"o_{suffix}"] = new Queue<Func<Task<CapitalDealConfirmation>>>(
                [() => Task.FromResult(AcceptedConfirmation($"o_{suffix}", $"d_{suffix}"))]);
        }
        return gateway;
    }

    private static CapitalDealAcknowledgement Acknowledgement(string reference) => new(reference, "", "");

    private static CapitalDealConfirmation AcceptedConfirmation(string reference, string dealId) =>
        new(reference, "ACCEPTED", dealId, 101.25m, [new CapitalAffectedDeal(dealId, "OPENED")], "");

    private static CapitalDealConfirmation ClosedConfirmation(string reference, string dealId) =>
        new(reference, "ACCEPTED", dealId, null, [new CapitalAffectedDeal(dealId, "CLOSED")], "");

    private static CapitalDealConfirmation RejectedConfirmation(string reference, string reason) =>
        new(reference, "REJECTED", "", null, [], reason);

    private static CapitalDealConfirmation PendingConfirmation(string reference) =>
        new(reference, "PENDING", "", null, [], "");

    private static SyntheticExecutionProgress Capture(List<SyntheticExecutionRecord> records) => (record, _) =>
    {
        records.Add(record);
        return Task.CompletedTask;
    };

    private static SyntheticExecutionProgress CaptureAndHonorCancellation(List<SyntheticExecutionRecord> records) => (record, cancellationToken) =>
    {
        cancellationToken.ThrowIfCancellationRequested();
        records.Add(record);
        return Task.CompletedTask;
    };

    private static Task IgnoreProgress(SyntheticExecutionRecord record, CancellationToken cancellationToken) => Task.CompletedTask;

    private static SyntheticBasket CreateBasket(IEnumerable<SyntheticComponent>? components = null)
    {
        var basket = new SyntheticBasket { Symbol = "SYN-TEST-01" };
        foreach (var component in components ?? new[]
        {
            CreateComponent("ALPHA", 1m),
            CreateComponent("BETA", -1m),
            CreateComponent("GAMMA", 1m),
            CreateComponent("ZETA", 1m),
        })
        {
            basket.Components.Add(component);
        }
        return basket;
    }

    private static SyntheticComponent CreateComponent(string epic, decimal multiplier, decimal weight = 25m) =>
        new(
            new MarketInstrument
            {
                Epic = epic,
                Currency = "USD",
                Bid = 10m,
                Offer = 10m,
                LastTickAt = DateTimeOffset.Parse("2026-07-30T12:00:00Z"),
                Status = "TRADEABLE",
                LotSize = 1m,
                MinDealSize = 1m,
                MinSizeIncrement = 1m,
            },
            weight,
            10m,
            20m)
        {
            FormulaMultiplier = multiplier,
        };

    private static MarketInstrument CreateFreshMarketDetails(string epic) =>
        new()
        {
            Epic = epic,
            Name = epic,
            Symbol = epic,
            Type = "SHARES",
            Currency = "USD",
            Bid = 11m,
            Offer = 12m,
            Price = 11.5m,
            LastTickAt = DateTimeOffset.Parse("2026-07-30T12:01:00Z"),
            Status = "TRADEABLE",
            LotSize = 1m,
            MinDealSize = 1m,
            MinSizeIncrement = 1m,
            MarginFactor = 20m,
            MarginFactorUnit = "PERCENTAGE",
        };

    private static SyntheticMarginSummary CreateMarginSummary(decimal available = 500m)
    {
        var legs = new[]
        {
            CreateMarginLeg("BUY", "ALPHA"),
            CreateMarginLeg("SELL", "BETA"),
            CreateMarginLeg("BUY", "GAMMA"),
            CreateMarginLeg("BUY", "ZETA"),
        };
        var buy = new SyntheticMarginSidePreview("BUY", "USD", true, "", 120m, legs);
        var sell = new SyntheticMarginSidePreview("SELL", "USD", true, "", 120m, legs);
        return new SyntheticMarginSummary("USD", available, available - 120m, available - 120m, buy, sell);
    }

    private static SyntheticMarginLegPreview CreateMarginLeg(string side, string epic) =>
        new(side, epic, 10m, 15m, 150m, "USD", 30m, "USD", 30m);

    private static void AssertContainsFailure(SyntheticPreflightResult result, string epic, string reason)
    {
        if (!result.Failures.Any(failure => failure.Epic == epic && failure.Reason == reason))
        {
            throw new Exception($"preflight failure missing: {epic} {reason}");
        }
    }

    private static void AssertContainsFailure(
        IReadOnlyList<SyntheticPreflightFailure> failures,
        string epic,
        string reasonFragment)
    {
        if (!failures.Any(failure =>
                failure.Epic == epic &&
                failure.Reason.Contains(reasonFragment, StringComparison.OrdinalIgnoreCase)))
        {
            throw new Exception($"preflight failure missing: {epic} containing {reasonFragment}");
        }
    }

    private static void AssertContains(string value, string expected, string message)
    {
        if (!value.Contains(expected, StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception($"{message}: expected '{value}' to contain '{expected}'");
        }
    }

    private static void AssertOrdered(string value, params string[] expected)
    {
        var previous = -1;
        foreach (var item in expected)
        {
            var current = value.IndexOf(item, StringComparison.Ordinal);
            if (current <= previous)
            {
                throw new Exception($"expected '{item}' after index {previous} in source contract");
            }
            previous = current;
        }
    }

    private static string SliceSource(string source, string start, string end)
    {
        var startIndex = source.IndexOf(start, StringComparison.Ordinal);
        var endIndex = source.IndexOf(end, startIndex + Math.Max(start.Length, 1), StringComparison.Ordinal);
        if (startIndex < 0 || endIndex < 0 || endIndex <= startIndex)
        {
            throw new Exception($"source contract boundaries missing: {start} -> {end}");
        }
        return source[startIndex..endIndex];
    }

    private static string ReadRepositoryFile(params string[] segments)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var path = segments.Aggregate(directory.FullName, Path.Combine);
            if (File.Exists(path)) return File.ReadAllText(path);
        }

        throw new FileNotFoundException($"Repository file not found: {Path.Combine(segments)}");
    }

    private static void RunNodeScript(string script, string htmlPath, string description)
    {
        var scriptPath = Path.Combine(Path.GetTempPath(), $"capetf-{Guid.NewGuid():N}.cjs");
        File.WriteAllText(scriptPath, script, new UTF8Encoding(false));
        try
        {
            using var process = Process.Start(new ProcessStartInfo("node")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                ArgumentList = { scriptPath, htmlPath },
            }) ?? throw new Exception($"could not start Node for {description}");
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                throw new Exception($"{description} failed ({process.ExitCode}): {error}{output}");
            }
        }
        finally
        {
            File.Delete(scriptPath);
        }
    }

    private static void AssertSequence(IEnumerable<string> actual, params string[] expected)
    {
        AssertEqual(string.Join(" -> ", expected), string.Join(" -> ", actual), "gateway call sequence");
    }

    private static void ProductionTransportDisablesAutomaticRedirects()
    {
        using var handler = CapitalApiClient.CreateProductionHttpHandler();

        AssertFalse(handler.AllowAutoRedirect, "production transport must not follow redirects");
    }

    private static void LiveMutationIsRejectedBeforeItIsSent()
    {
        var handler = new TradingHandler();
        using var client = Login(handler, useDemo: false);
        var request = new CapitalPositionRequest("AAPL", "BUY", 2m);

        var exception = AssertThrows<InvalidOperationException>(
            () => client.CreatePositionAsync(request, default).GetAwaiter().GetResult(),
            "live trading must be rejected");

        AssertTrue(exception.Message.Contains("demo", StringComparison.OrdinalIgnoreCase), "rejection must explain the demo-only restriction");
        AssertEqual(0, handler.Requests.Count(request => request.Method != HttpMethod.Post || request.Path != "/api/v1/session"), "live mutation must not be sent");
    }

    private static void DemoPositionRequestUsesCapitalContract()
    {
        var handler = new TradingHandler();
        using var client = Login(handler, useDemo: true);

        var acknowledgement = client.CreatePositionAsync(new CapitalPositionRequest("AAPL", "BUY", 2.5m), default).GetAwaiter().GetResult();

        var sent = handler.Requests.Single(request => request.Path == "/api/v1/positions");
        using var body = JsonDocument.Parse(sent.Body);
        AssertEqual(HttpMethod.Post, sent.Method, "position method");
        AssertEqual("AAPL", body.RootElement.GetProperty("epic").GetString(), "position epic");
        AssertEqual("BUY", body.RootElement.GetProperty("direction").GetString(), "position direction");
        AssertEqual(2.5m, body.RootElement.GetProperty("size").GetDecimal(), "position size");
        AssertFalse(body.RootElement.GetProperty("guaranteedStop").GetBoolean(), "position must disable guaranteed stops");
        AssertFalse(body.RootElement.TryGetProperty("orderType", out _), "position must not infer an unsupported order type");
        AssertEqual("REF-123", acknowledgement.DealReference, "position acknowledgement reference");
    }

    private static void LostCreateResponseRecoversUniqueNewPositionWithoutRetry()
    {
        var handler = new LostCreateResponseTradingHandler();
        using var client = Login(handler, useDemo: true);
        var gateway = new CapitalTradingGateway(client);

        var acknowledgement = gateway.CreatePositionAsync(
            new CapitalPositionRequest("AAPL", "BUY", 2.5m),
            default).GetAwaiter().GetResult();

        AssertEqual("DEAL-NEW", acknowledgement.RecoveredDealId, "lost response must recover the unique new permanent deal ID");
        AssertEqual(123.45m, acknowledgement.RecoveredLevel, "recovery must preserve the opened level");
        AssertEqual(1, handler.PostCount, "an ambiguous create must never be retried");
        AssertEqual(2, handler.PositionListCount, "recovery compares one before and one after snapshot");
    }

    private static void MalformedCreateResponseRecoversUniqueNewPositionWithoutRetry()
    {
        var handler = new LostCreateResponseTradingHandler(returnMalformedResponse: true);
        using var client = Login(handler, useDemo: true);
        var acknowledgement = new CapitalTradingGateway(client).CreatePositionAsync(
            new CapitalPositionRequest("AAPL", "BUY", 2.5m),
            default).GetAwaiter().GetResult();

        AssertEqual("DEAL-NEW", acknowledgement.RecoveredDealId, "malformed accepted response must recover the unique new deal");
        AssertEqual(1, handler.PostCount, "malformed create response must not cause a retry");
    }

    private static void DealConfirmationParsesRequiredFields()
    {
        var handler = new TradingHandler();
        using var client = Login(handler, useDemo: true);

        var confirmation = client.GetDealConfirmationAsync("REF-123", default).GetAwaiter().GetResult();

        AssertEqual("ACCEPTED", confirmation.DealStatus, "confirmation status");
        AssertEqual("DEAL-123", confirmation.DealId, "confirmation deal ID");
        AssertEqual(123.45m, confirmation.Level, "confirmation level");
        AssertEqual(2, confirmation.AffectedDeals.Count, "confirmation affected deal count");
        AssertEqual("DEAL-OTHER", confirmation.AffectedDeals[1].DealId, "confirmation affected deal");
    }

    private static void DemoPositionRedirectDoesNotReachRedirectTarget()
    {
        var handler = new RedirectingTradingHandler();
        using var client = Login(handler, useDemo: true);

        var exception = AssertThrows<CapitalApiException>(
            () => client.CreatePositionAsync(new CapitalPositionRequest("AAPL", "BUY", 2.5m), default).GetAwaiter().GetResult(),
            "redirected position request must fail without a follow-up request");

        AssertEqual(HttpStatusCode.TemporaryRedirect, exception.StatusCode, "position redirect status");
        AssertEqual(1, handler.MutationRequests.Count, "position redirect must issue one mutation request");
        AssertEqual("demo-api-capital.backend-capital.com", handler.MutationRequests[0].Host, "position must never reach the redirect target");
    }

    private static void OpenPositionsParseRequiredFields()
    {
        var handler = new TradingHandler();
        using var client = Login(handler, useDemo: true);

        var positions = client.GetOpenPositionsAsync(default).GetAwaiter().GetResult();

        AssertEqual(1, positions.Count, "open position count");
        var position = positions[0];
        AssertEqual("DEAL-123", position.DealId, "open position deal ID");
        AssertEqual("AAPL", position.Epic, "open position epic");
        AssertEqual("BUY", position.Direction, "open position direction");
        AssertEqual(2.5m, position.Size, "open position size");
        AssertEqual(123.45m, position.Level, "open position level");
        AssertEqual(17.25m, position.UnrealizedProfitLoss, "open position UPL");
        AssertEqual("USD", position.Currency, "open position currency");
        AssertEqual("TRADEABLE", position.MarketStatus, "open position market status");
        AssertEqual(118m, position.StopLevel, "open position stop level");
        AssertEqual(140m, position.ProfitLevel, "open position take profit level");
        AssertEqual(123.40m, position.Bid, "open position bid");
        AssertEqual(123.50m, position.Offer, "open position offer");
    }

    private static void BrokerAccountParsesTradingTotals()
    {
        const string json = """
        {"accounts":[{"accountId":"active","currency":"USDd","balance":{"balance":20993.11,"deposit":21000,"profitLoss":-6.89,"available":20684.88}}]}
        """;

        var account = CapitalApiClient.ParseBrokerAccount(json, "active", DateTimeOffset.UnixEpoch);

        AssertEqual("active", account.AccountId, "broker account ID");
        AssertEqual("USDd", account.Currency, "broker account currency");
        AssertEqual(20993.11m, account.Balance, "broker equity");
        AssertEqual(21000m, account.Deposit, "broker deposit");
        AssertEqual(-6.89m, account.ProfitLoss, "broker running P/L");
        AssertEqual(20684.88m, account.Available, "broker available funds");
    }

    private static void WorkingOrdersParseRequiredFields()
    {
        const string json = """
        {"workingOrders":[{"workingOrderData":{"dealId":"ORDER-1","direction":"SELL","epic":"AAPL","orderSize":2.5,"orderLevel":130,"orderType":"LIMIT","timeInForce":"GOOD_TILL_CANCELLED","stopLevel":140,"profitLevel":110,"currencyCode":"USD"},"marketData":{"instrumentName":"Apple Inc","marketStatus":"TRADEABLE","bid":123.4,"offer":123.5}}]}
        """;

        var orders = CapitalApiClient.ParseWorkingOrders(json);

        AssertEqual(1, orders.Count, "working order count");
        var order = orders[0];
        AssertEqual("ORDER-1", order.DealId, "working order deal ID");
        AssertEqual("AAPL", order.Epic, "working order epic");
        AssertEqual("SELL", order.Direction, "working order direction");
        AssertEqual(2.5m, order.Size, "working order size");
        AssertEqual(130m, order.OrderLevel, "working order level");
        AssertEqual("LIMIT", order.OrderType, "working order type");
        AssertEqual(140m, order.StopLevel, "working order stop level");
        AssertEqual(110m, order.ProfitLevel, "working order take profit level");
    }

    private static void DemoClosePositionUsesDeleteWithoutRetry()
    {
        var handler = new TradingHandler();
        using var client = Login(handler, useDemo: true);

        var acknowledgement = client.ClosePositionAsync("DEAL-123", default).GetAwaiter().GetResult();

        var sent = handler.Requests.Single(request => request.Path == "/api/v1/positions/DEAL-123");
        AssertEqual(HttpMethod.Delete, sent.Method, "close method");
        AssertEqual("REF-CLOSE-123", acknowledgement.DealReference, "close acknowledgement reference");
        AssertEqual(1, handler.Requests.Count(request => request.Path == "/api/v1/positions/DEAL-123"), "close must not retry");
    }

    private static void DemoCloseRedirectDoesNotReachRedirectTarget()
    {
        var handler = new RedirectingTradingHandler();
        using var client = Login(handler, useDemo: true);

        var exception = AssertThrows<CapitalApiException>(
            () => client.ClosePositionAsync("DEAL-123", default).GetAwaiter().GetResult(),
            "redirected close request must fail without a follow-up request");

        AssertEqual(HttpStatusCode.TemporaryRedirect, exception.StatusCode, "close redirect status");
        AssertEqual(1, handler.MutationRequests.Count, "close redirect must issue one mutation request");
        AssertEqual("demo-api-capital.backend-capital.com", handler.MutationRequests[0].Host, "close must never reach the redirect target");
    }

    private static CapitalApiClient Login(HttpMessageHandler handler, bool useDemo)
    {
        var client = new CapitalApiClient(handler);
        client.LoginAsync(new ApiCredentials
        {
            Identifier = "test-user",
            Password = "test-password",
            ApiKey = "test-key",
            UseDemo = useDemo,
        }).GetAwaiter().GetResult();
        return client;
    }

    private static TException AssertThrows<TException>(Action action, string message) where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException exception)
        {
            return exception;
        }

        throw new Exception(message);
    }

    private static void AssertTrue(bool value, string message)
    {
        if (!value) throw new Exception(message);
    }

    private static void AssertFalse(bool value, string message) => AssertTrue(!value, message);

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new Exception($"{message}: expected {expected}, actual {actual}");
        }
    }

    private static T AssertSingle<T>(IReadOnlyList<T> values, string message)
    {
        AssertEqual(1, values.Count, message);
        return values[0];
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"capetf-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path)) Directory.Delete(Path, recursive: true);
        }
    }

    private sealed class TradingHandler : HttpMessageHandler
    {
        public List<RecordedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add(new RecordedRequest(
                request.Method,
                request.RequestUri?.Host ?? "",
                request.RequestUri?.AbsolutePath ?? "",
                body));

            var response = request.RequestUri?.AbsolutePath switch
            {
                "/api/v1/session" => JsonResponse(HttpStatusCode.OK, "{}", includeSessionHeaders: true),
                "/api/v1/positions" when request.Method == HttpMethod.Post => JsonResponse(HttpStatusCode.OK, "{\"dealReference\":\"REF-123\"}"),
                "/api/v1/confirms/REF-123" => JsonResponse(HttpStatusCode.OK, "{\"dealStatus\":\"ACCEPTED\",\"dealId\":\"DEAL-123\",\"level\":123.45,\"affectedDeals\":[\"DEAL-123\",\"DEAL-OTHER\"]}"),
                "/api/v1/positions" => JsonResponse(HttpStatusCode.OK, "{\"positions\":[{\"position\":{\"dealId\":\"DEAL-123\",\"direction\":\"BUY\",\"size\":2.5,\"level\":123.45,\"upl\":17.25,\"currency\":\"USD\",\"stopLevel\":118,\"profitLevel\":140},\"market\":{\"epic\":\"AAPL\",\"marketStatus\":\"TRADEABLE\",\"bid\":123.40,\"offer\":123.50}}]}"),
                "/api/v1/positions/DEAL-123" when request.Method == HttpMethod.Delete => JsonResponse(HttpStatusCode.OK, "{\"dealReference\":\"REF-CLOSE-123\"}"),
                _ => JsonResponse(HttpStatusCode.NotFound, "{}"),
            };
            return response;
        }

        private static HttpResponseMessage JsonResponse(HttpStatusCode status, string body, bool includeSessionHeaders = false)
        {
            var response = new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
            if (includeSessionHeaders)
            {
                response.Headers.Add("CST", "cst-token");
                response.Headers.Add("X-SECURITY-TOKEN", "security-token");
            }
            return response;
        }
    }

    private sealed class RedirectingTradingHandler : HttpMessageHandler
    {
        public List<RecordedRequest> Requests { get; } = [];
        public IReadOnlyList<RecordedRequest> MutationRequests => Requests.Where(request => request.Path != "/api/v1/session").ToList();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add(new RecordedRequest(
                request.Method,
                request.RequestUri?.Host ?? "",
                request.RequestUri?.AbsolutePath ?? "",
                body));

            if (request.RequestUri?.AbsolutePath == "/api/v1/session")
            {
                return JsonResponse(HttpStatusCode.OK, "{}", includeSessionHeaders: true);
            }

            var response = JsonResponse(HttpStatusCode.TemporaryRedirect, "{}");
            response.Headers.Location = new Uri("https://api-capital.backend-capital.com/api/v1/positions");
            return response;
        }

        private static HttpResponseMessage JsonResponse(HttpStatusCode status, string body, bool includeSessionHeaders = false)
        {
            var response = new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
            if (includeSessionHeaders)
            {
                response.Headers.Add("CST", "cst-token");
                response.Headers.Add("X-SECURITY-TOKEN", "security-token");
            }
            return response;
        }
    }

    private sealed class LostCreateResponseTradingHandler : HttpMessageHandler
    {
        private readonly bool _returnMalformedResponse;

        public LostCreateResponseTradingHandler(bool returnMalformedResponse = false)
        {
            _returnMalformedResponse = returnMalformedResponse;
        }

        public int PostCount { get; private set; }
        public int PositionListCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath;
            if (path == "/api/v1/session")
            {
                var login = JsonResponse("{}");
                login.Headers.Add("CST", "cst-token");
                login.Headers.Add("X-SECURITY-TOKEN", "security-token");
                return Task.FromResult(login);
            }

            if (path == "/api/v1/positions" && request.Method == HttpMethod.Get)
            {
                PositionListCount++;
                var added = PositionListCount > 1
                    ? ",{\"position\":{\"dealId\":\"DEAL-NEW\",\"direction\":\"BUY\",\"size\":2.5,\"level\":123.45},\"market\":{\"epic\":\"AAPL\"}}"
                    : "";
                return Task.FromResult(JsonResponse(
                    $"{{\"positions\":[{{\"position\":{{\"dealId\":\"DEAL-OLD\",\"direction\":\"BUY\",\"size\":1,\"level\":100}},\"market\":{{\"epic\":\"AAPL\"}}}}{added}]}}"));
            }

            if (path == "/api/v1/positions" && request.Method == HttpMethod.Post)
            {
                PostCount++;
                if (_returnMalformedResponse) return Task.FromResult(JsonResponse("{ accepted-but-truncated"));
                throw new HttpRequestException("response lost after Capital accepted the request");
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        private static HttpResponseMessage JsonResponse(string body) => new(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
    }

    private sealed class ScriptedTradingGateway : ICapitalTradingGateway
    {
        public Queue<Func<Task<CapitalDealAcknowledgement>>> PostResults { get; } = [];
        public Queue<Func<Task<CapitalDealAcknowledgement>>> CloseResults { get; } = [];
        public Dictionary<string, Queue<Func<Task<CapitalDealConfirmation>>>> ConfirmResults { get; } = new(StringComparer.Ordinal);
        public List<string> Calls { get; } = [];
        public List<string> PostCalls { get; } = [];
        public List<CapitalPositionRequest> PostRequests { get; } = [];
        public List<string> ConfirmCalls { get; } = [];
        public List<string> CloseCalls { get; } = [];
        public int CreateInvocations { get; private set; }
        public int CloseInvocations { get; private set; }
        public Action<string>? ObserveCall { get; set; }

        public Task<CapitalDealAcknowledgement> CreatePositionAsync(CapitalPositionRequest request, CancellationToken cancellationToken)
        {
            CreateInvocations++;
            cancellationToken.ThrowIfCancellationRequested();
            RecordCall($"POST:{request.Epic}");
            PostCalls.Add(request.Epic);
            PostRequests.Add(request);
            return PostResults.Dequeue()();
        }

        public Task<CapitalDealConfirmation> GetDealConfirmationAsync(string dealReference, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RecordCall($"CONFIRM:{dealReference}");
            ConfirmCalls.Add(dealReference);
            return ConfirmResults[dealReference].Dequeue()();
        }

        public Task<CapitalDealAcknowledgement> ClosePositionAsync(string dealId, CancellationToken cancellationToken)
        {
            CloseInvocations++;
            cancellationToken.ThrowIfCancellationRequested();
            RecordCall($"CLOSE:{dealId}");
            CloseCalls.Add(dealId);
            return CloseResults.Dequeue()();
        }

        public void ClearCalls()
        {
            Calls.Clear();
            PostCalls.Clear();
            PostRequests.Clear();
            ConfirmCalls.Clear();
            CloseCalls.Clear();
            CreateInvocations = 0;
            CloseInvocations = 0;
        }

        private void RecordCall(string call)
        {
            Calls.Add(call);
            ObserveCall?.Invoke(call);
        }
    }

    private sealed class TestExecutionClock : ISyntheticExecutionClock
    {
        public TestExecutionClock(DateTimeOffset? start = null)
        {
            UtcNow = start ?? DateTimeOffset.Parse("2026-07-30T12:00:00Z");
        }

        public DateTimeOffset UtcNow { get; private set; }
        public List<TimeSpan> Delays { get; } = [];

        public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Delays.Add(delay);
            UtcNow += delay;
            return Task.CompletedTask;
        }
    }

    private enum OpenContractOutcome
    {
        Accepted,
        Rejected,
        Unknown,
    }

    private enum CloseContractOutcome
    {
        Accepted,
        Rejected,
        Unknown,
    }

    private sealed record ExecutionEmissionContractCase(
        string Name,
        Func<IReadOnlyList<SyntheticExecutionRecord>> Emit,
        IReadOnlyList<string> ExpectedSignatures);

    private sealed record ReconciliationEmissionContractCase(
        string Name,
        SyntheticExecutionRecord Input,
        IReadOnlyList<CapitalOpenPosition> Positions,
        string ExpectedSignature);

    private sealed record RecordedRequest(HttpMethod Method, string Host, string Path, string Body);
}
