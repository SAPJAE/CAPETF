using CAPETF.Desktop;
using System.Net;
using System.Net.Http;
using System.IO;
using System.Text;
using System.Text.Json;

namespace CAPETF.Desktop.Tests;

public static class SyntheticTradingTests
{
    public static void RunAll()
    {
        PreflightRejectsNonDemoSessions();
        PreflightRejectsInvalidComponentCounts();
        PreflightRejectsDuplicateEpics();
        PreflightReturnsLegFailuresInEpicOrder();
        PreflightRejectsZeroAndStaleQuotes();
        PreflightRejectsInvalidRoundedSize();
        PreflightRejectsMissingMargin();
        PreflightRejectsInsufficientFunds();
        PreflightCreatesFrozenTicketWithReversedNegativeLeg();
        ExecutionWaitsForAcceptedConfirmationBeforeSubmittingNextLeg();
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
        DemoPositionRedirectDoesNotReachRedirectTarget();
        DealConfirmationParsesRequiredFields();
        OpenPositionsParseRequiredFields();
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
        ReconciliationMatchesOpenPositionsByDealIdAndUpdatesUpl();
        ReconciliationMarksMissingOpenPositionsClosed();
        ReconciliationLeavesUnresolvedUnknownUntilPositivelyMatched();
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

    private static void PreflightRejectsNonDemoSessions()
    {
        var result = SyntheticTradePreflight.Build(CreatePreflightInput() with { IsDemoSession = false });

        AssertFalse(result.IsReady, "live sessions must fail preflight");
        AssertContainsFailure(result, "", "Demo trading session is required.");
    }

    private static void PreflightRejectsInvalidComponentCounts()
    {
        var basket = CreateBasket().Components.Take(2).ToList();
        var result = SyntheticTradePreflight.Build(CreatePreflightInput(CreateBasket(basket)));

        AssertFalse(result.IsReady, "two-leg baskets must fail preflight");
        AssertContainsFailure(result, "", "Synthetic baskets must contain 3 or 4 components.");
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

        var result = SyntheticTradePreflight.Build(CreatePreflightInput(basket, now));

        AssertFalse(result.IsReady, "zero and stale quotes must fail preflight");
        AssertContainsFailure(result, "BETA", "Bid and offer prices must be positive.");
        AssertContainsFailure(result, "GAMMA", "Quote is older than five minutes.");
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
                AffectedDeals = [new CapitalAffectedDeal("d_msft", "")],
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
            CreateMarginSummary());

    private static SyntheticExecutionRecord Execute(ScriptedTradingGateway gateway, params string[] epics) =>
        CreateExecutionService(gateway).ExecuteAsync(CreateExecutionTicket(epics), IgnoreProgress, default).GetAwaiter().GetResult();

    private static SyntheticBasketExecutionService CreateExecutionService(
        ScriptedTradingGateway gateway,
        ISyntheticExecutionClock? clock = null) =>
        new(gateway, clock ?? new TestExecutionClock());

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

    private static void AssertContains(string value, string expected, string message)
    {
        if (!value.Contains(expected, StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception($"{message}: expected '{value}' to contain '{expected}'");
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
                "/api/v1/positions" => JsonResponse(HttpStatusCode.OK, "{\"positions\":[{\"position\":{\"dealId\":\"DEAL-123\",\"direction\":\"BUY\",\"size\":2.5,\"level\":123.45,\"upl\":17.25,\"currency\":\"USD\"},\"market\":{\"epic\":\"AAPL\",\"marketStatus\":\"TRADEABLE\"}}]}"),
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

    private sealed class ScriptedTradingGateway : ICapitalTradingGateway
    {
        public Queue<Func<Task<CapitalDealAcknowledgement>>> PostResults { get; } = [];
        public Queue<Func<Task<CapitalDealAcknowledgement>>> CloseResults { get; } = [];
        public Dictionary<string, Queue<Func<Task<CapitalDealConfirmation>>>> ConfirmResults { get; } = new(StringComparer.Ordinal);
        public List<string> Calls { get; } = [];
        public List<string> PostCalls { get; } = [];
        public List<string> ConfirmCalls { get; } = [];
        public List<string> CloseCalls { get; } = [];
        public int CreateInvocations { get; private set; }
        public int CloseInvocations { get; private set; }

        public Task<CapitalDealAcknowledgement> CreatePositionAsync(CapitalPositionRequest request, CancellationToken cancellationToken)
        {
            CreateInvocations++;
            cancellationToken.ThrowIfCancellationRequested();
            Calls.Add($"POST:{request.Epic}");
            PostCalls.Add(request.Epic);
            return PostResults.Dequeue()();
        }

        public Task<CapitalDealConfirmation> GetDealConfirmationAsync(string dealReference, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls.Add($"CONFIRM:{dealReference}");
            ConfirmCalls.Add(dealReference);
            return ConfirmResults[dealReference].Dequeue()();
        }

        public Task<CapitalDealAcknowledgement> ClosePositionAsync(string dealId, CancellationToken cancellationToken)
        {
            CloseInvocations++;
            cancellationToken.ThrowIfCancellationRequested();
            Calls.Add($"CLOSE:{dealId}");
            CloseCalls.Add(dealId);
            return CloseResults.Dequeue()();
        }

        public void ClearCalls()
        {
            Calls.Clear();
            PostCalls.Clear();
            ConfirmCalls.Clear();
            CloseCalls.Clear();
            CreateInvocations = 0;
            CloseInvocations = 0;
        }
    }

    private sealed class TestExecutionClock : ISyntheticExecutionClock
    {
        public DateTimeOffset UtcNow { get; private set; } = DateTimeOffset.Parse("2026-07-30T12:00:00Z");
        public List<TimeSpan> Delays { get; } = [];

        public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Delays.Add(delay);
            UtcNow += delay;
            return Task.CompletedTask;
        }
    }

    private sealed record RecordedRequest(HttpMethod Method, string Host, string Path, string Body);
}
