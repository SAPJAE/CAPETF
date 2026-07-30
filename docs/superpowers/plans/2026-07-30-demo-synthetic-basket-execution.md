# Demo Synthetic Basket Execution Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add safe, persistent, demo-only execution and management of multi-leg Capital.com synthetic baskets.

**Architecture:** Extend `CapitalApiClient` as a partial class with a demo-locked trading surface, then layer pure preflight, sequential execution, persistence, and reconciliation services above it. The WPF host owns immutable one-time execution tickets; the WebView only renders state and requests host actions, so browser values cannot alter approved leg quantities.

**Tech Stack:** .NET 8 WPF, `HttpClient`, `System.Text.Json`, WebView2, vanilla HTML/CSS/JavaScript, existing console-style .NET test project.

## Global Constraints

- This release must never send an order to the Capital.com live API.
- A `POST /positions` acknowledgment is not a filled position; confirmation is mandatory.
- Legs execute sequentially and subsequent legs stop after a failure.
- Successful legs remain open after a partial failure; there is no automatic rollback.
- Network uncertainty after submission must never trigger a blind duplicate `POST`.
- The app persists no credentials or session tokens in execution records.
- All implementation follows red-green-refactor with focused commits.

## File Structure

- `desktop/CAPETF.Desktop/CapitalApiClient.cs`: make the existing client partial and expose read-only demo-session identity.
- `desktop/CAPETF.Desktop/CapitalApiClient.Trading.cs`: demo-locked position mutation, confirmation, open-position, and account-preference endpoints.
- `desktop/CAPETF.Desktop/SyntheticTradeModels.cs`: immutable preflight, execution, leg, position, and persisted-record models.
- `desktop/CAPETF.Desktop/SyntheticTradePreflight.cs`: pure validation and immutable ticket construction.
- `desktop/CAPETF.Desktop/SyntheticBasketExecutionService.cs`: sequential submit/confirm state machine and close workflow.
- `desktop/CAPETF.Desktop/SyntheticExecutionStore.cs`: atomic versioned JSON persistence.
- `desktop/CAPETF.Desktop/SyntheticPositionReconciler.cs`: reconcile persisted deal IDs with current Capital.com positions.
- `desktop/CAPETF.Desktop/CapComTerminalWindow.xaml.cs`: operation ownership, one-time ticket registry, WebView bridge, refresh, and recovery orchestration.
- `desktop/CAPETF.Desktop/Assets/synthetic-terminal.html`: demo trading ticket, confirmation modal, execution/position workspace, progress and scroll layout.
- `desktop/CAPETF.Desktop.Tests/SyntheticTradingTests.cs`: focused service, API, persistence, and UI contract tests.
- `desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs`: invoke the focused synthetic trading suite.

---

### Task 1: Demo-Locked Capital Trading API

**Files:**
- Modify: `desktop/CAPETF.Desktop/CapitalApiClient.cs`
- Create: `desktop/CAPETF.Desktop/CapitalApiClient.Trading.cs`
- Create: `desktop/CAPETF.Desktop/SyntheticTradeModels.cs`
- Create: `desktop/CAPETF.Desktop.Tests/SyntheticTradingTests.cs`
- Modify: `desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs`

**Interfaces:**
- Produces: `bool CapitalApiClient.IsDemoTradingSession`
- Produces: `Task<CapitalDealAcknowledgement> CreatePositionAsync(CapitalPositionRequest request, CancellationToken token)`
- Produces: `Task<CapitalDealConfirmation> GetDealConfirmationAsync(string dealReference, CancellationToken token)`
- Produces: `Task<IReadOnlyList<CapitalOpenPosition>> GetOpenPositionsAsync(CancellationToken token)`
- Produces: `Task<CapitalDealAcknowledgement> ClosePositionAsync(string dealId, CancellationToken token)`

- [ ] **Step 1: Write failing API contract tests**

Add `SyntheticTradingTests.RunAll()` and tests that construct `CapitalApiClient` with fake HTTP handlers. Assert that live-host mutation throws before sending, demo `POST /api/v1/positions` serializes `epic`, `direction`, `size`, and `guaranteedStop:false`, confirmation parses `dealStatus`, `dealId`, `level`, and `affectedDeals`, and open positions parse direction, size, level, UPL, currency, and market status.

```csharp
AssertThrows<InvalidOperationException>(() => live.CreatePositionAsync(request, default).GetAwaiter().GetResult(), "demo");
AssertEqual("/api/v1/positions", handler.Requests.Single().RequestUri!.AbsolutePath, "position path");
AssertEqual("ACCEPTED", confirmation.DealStatus, "confirmation status");
```

- [ ] **Step 2: Run the suite and verify RED**

Run: `dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj -c Release`

Expected: compilation fails because trading models and methods do not exist.

- [ ] **Step 3: Implement the minimal demo trading API**

Change the client declaration to `public sealed partial class CapitalApiClient`. Add a strict host predicate:

```csharp
public bool IsDemoTradingSession =>
    _baseUri?.Host.Equals("demo-api-capital.backend-capital.com", StringComparison.OrdinalIgnoreCase) == true;

private void EnsureDemoMutationAllowed()
{
    EnsureSession();
    if (!IsDemoTradingSession) throw new InvalidOperationException("Trading is restricted to Capital.com demo accounts.");
}
```

Implement authenticated JSON POST/GET/DELETE helpers in the partial file. Never retry mutation requests inside the API client. Parse confirmations defensively and retain raw rejection reason/status without exposing tokens.

- [ ] **Step 4: Run tests and verify GREEN**

Run the full test command. Expected: `SyntheticBasketBuilder tests passed`.

- [ ] **Step 5: Commit**

```bash
git add desktop/CAPETF.Desktop/CapitalApiClient.cs desktop/CAPETF.Desktop/CapitalApiClient.Trading.cs desktop/CAPETF.Desktop/SyntheticTradeModels.cs desktop/CAPETF.Desktop.Tests/SyntheticTradingTests.cs desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs
git commit -m "feat: add demo-locked Capital trading API"
```

### Task 2: Executable Basket Preflight

**Files:**
- Create: `desktop/CAPETF.Desktop/SyntheticTradePreflight.cs`
- Modify: `desktop/CAPETF.Desktop/SyntheticTradeModels.cs`
- Modify: `desktop/CAPETF.Desktop.Tests/SyntheticTradingTests.cs`

**Interfaces:**
- Consumes: `SyntheticBasket`, `SyntheticMarginSummary`, refreshed `MarketInstrument` details.
- Produces: `SyntheticPreflightResult SyntheticTradePreflight.Build(SyntheticPreflightInput input)`
- Produces: immutable `SyntheticExecutionTicket` with `TicketId`, `BasketId`, `Side`, `RequestedNotional`, `CreatedUtc`, `ExpiresUtc`, and executable legs.

- [ ] **Step 1: Write failing pure preflight tests**

Cover demo-session rejection, component count outside 3-4, duplicate epics, non-`TRADEABLE` status, zero/stale quotes, invalid rounded size, missing margin, insufficient available funds, negative multiplier side reversal, and successful ticket creation. Use a five-minute quote-age limit and a two-minute ticket lifetime.

```csharp
var result = SyntheticTradePreflight.Build(input with { IsDemoSession = false });
AssertFalse(result.IsReady, "live sessions must fail preflight");
AssertEqual("SELL", ready.Ticket!.Legs.Single(x => x.Multiplier < 0).Direction, "negative leg reverses BUY basket");
```

- [ ] **Step 2: Run tests and verify RED**

Expected: compilation fails for missing `SyntheticTradePreflight` and ticket types.

- [ ] **Step 3: Implement pure preflight**

Build executable legs from `SyntheticOrderSizing.BuildExecutableOrderPreview`. Validate all reasons in deterministic epic order and return every failure, not only the first. Copy quantities, prices, directions, and margin values into immutable records so later quote updates cannot mutate the ticket.

- [ ] **Step 4: Run tests and verify GREEN**

Run the full test command and confirm all existing sizing/margin tests remain green.

- [ ] **Step 5: Commit**

```bash
git add desktop/CAPETF.Desktop/SyntheticTradePreflight.cs desktop/CAPETF.Desktop/SyntheticTradeModels.cs desktop/CAPETF.Desktop.Tests/SyntheticTradingTests.cs
git commit -m "feat: preflight synthetic demo baskets"
```

### Task 3: Sequential Execution And Confirmation State Machine

**Files:**
- Create: `desktop/CAPETF.Desktop/SyntheticBasketExecutionService.cs`
- Modify: `desktop/CAPETF.Desktop/SyntheticTradeModels.cs`
- Modify: `desktop/CAPETF.Desktop.Tests/SyntheticTradingTests.cs`

**Interfaces:**
- Consumes: `ICapitalTradingGateway`, immutable `SyntheticExecutionTicket`.
- Produces: `Task<SyntheticExecutionRecord> ExecuteAsync(ticket, progress, token)`.
- Produces: `Task<SyntheticExecutionRecord> CloseAsync(record, progress, token)`.

- [ ] **Step 1: Write failing state-machine tests**

Use a scripted gateway and assert strict call order `POST leg 1 -> confirm leg 1 -> POST leg 2`. Cover accepted confirmation, explicit rejection, malformed acknowledgment, confirmation timeout, network failure before acknowledgment, ambiguous network failure after request dispatch, cancellation, partial success without rollback, close success, and partial close.

```csharp
AssertSequence(gateway.Calls, "POST:AAPL", "CONFIRM:o_aapl", "POST:MSFT", "CONFIRM:o_msft");
AssertEqual(SyntheticExecutionState.NeedsAttention, partial.State, "partial basket remains visible");
AssertEqual(0, gateway.CloseCalls.Count, "execution failure must not roll back opened legs");
```

- [ ] **Step 2: Run tests and verify RED**

Expected: missing execution service and gateway interface.

- [ ] **Step 3: Implement the state machine**

Create one record before submitting. For each leg, persist transitions through a callback, submit exactly once, then poll confirmation up to 15 times with one-second waits. Treat `ACCEPTED` plus an opened affected deal/permanent ID as open; rejection stops unsent legs; timeout becomes `Unknown`. Close only explicitly requested open deal IDs and confirm each close response.

- [ ] **Step 4: Run tests and verify GREEN**

Run the full suite. Inspect the scripted gateway call log to prove no POST retry and no rollback.

- [ ] **Step 5: Commit**

```bash
git add desktop/CAPETF.Desktop/SyntheticBasketExecutionService.cs desktop/CAPETF.Desktop/SyntheticTradeModels.cs desktop/CAPETF.Desktop.Tests/SyntheticTradingTests.cs
git commit -m "feat: execute and confirm synthetic demo baskets"
```

### Task 4: Atomic Persistence And Position Reconciliation

**Files:**
- Create: `desktop/CAPETF.Desktop/SyntheticExecutionStore.cs`
- Create: `desktop/CAPETF.Desktop/SyntheticPositionReconciler.cs`
- Modify: `desktop/CAPETF.Desktop/SyntheticTradeModels.cs`
- Modify: `desktop/CAPETF.Desktop.Tests/SyntheticTradingTests.cs`

**Interfaces:**
- Produces: `LoadAsync`, `SaveAsync`, and `UpsertAsync` on `SyntheticExecutionStore`.
- Produces: `SyntheticExecutionRecord Reconcile(record, IReadOnlyList<CapitalOpenPosition> positions, DateTimeOffset now)`.

- [ ] **Step 1: Write failing persistence/recovery tests**

Assert versioned JSON round-trip, atomic replacement, malformed-file quarantine, no credential/token fields, exact preservation of deal references, restart matching by deal ID, current UPL updates, closed detection, and unresolved `Unknown` behavior.

```csharp
await store.UpsertAsync(record, default);
var restored = AssertSingle(await store.LoadAsync(default));
AssertEqual("deal-123", restored.Legs[0].DealId, "deal identity survives restart");
AssertFalse(File.ReadAllText(path).Contains("securityToken", StringComparison.OrdinalIgnoreCase), "tokens must not persist");
```

- [ ] **Step 2: Run tests and verify RED**

Expected: missing store and reconciler.

- [ ] **Step 3: Implement atomic store and pure reconciler**

Write UTF-8 JSON to `<path>.tmp`, flush, then replace/move. Store schema version `1`. On malformed JSON move the file to `.corrupt-<UTC timestamp>` and return an empty list. Reconciliation uses Capital.com as truth for current position presence and P&L but retains immutable original ticket details and audit messages.

- [ ] **Step 4: Run tests and verify GREEN**

Run the full suite, including a temporary-directory persistence test.

- [ ] **Step 5: Commit**

```bash
git add desktop/CAPETF.Desktop/SyntheticExecutionStore.cs desktop/CAPETF.Desktop/SyntheticPositionReconciler.cs desktop/CAPETF.Desktop/SyntheticTradeModels.cs desktop/CAPETF.Desktop.Tests/SyntheticTradingTests.cs
git commit -m "feat: persist and reconcile synthetic positions"
```

### Task 5: WPF Host Orchestration And One-Time Tickets

**Files:**
- Modify: `desktop/CAPETF.Desktop/CapComTerminalWindow.xaml.cs`
- Modify: `desktop/CAPETF.Desktop/CapComTerminalWindow.xaml`
- Modify: `desktop/CAPETF.Desktop.Tests/SyntheticTradingTests.cs`

**Interfaces:**
- Consumes browser messages: `preflightBasket`, `executeBasket`, `refreshExecutions`, `closeBasket`.
- Publishes browser callbacks: `setTerminalPreflight`, `setTerminalExecutions`, `setTerminalExecutionProgress`, `setTerminalTradingMode`.

- [ ] **Step 1: Write failing host contract tests**

Assert message names, a host-side `Dictionary<Guid, SyntheticExecutionTicket>`, removal of a ticket before execution starts, expiry checks, operation duplicate guard, reconnect reconciliation, and demo-state publication. Assert the old `PreviewSyntheticOrder` click path cannot place an order.

- [ ] **Step 2: Run tests and verify RED**

Expected: static contract checks fail for missing messages and ownership logic.

- [ ] **Step 3: Implement orchestration**

Instantiate gateway/service/store/reconciler. Preflight refreshes markets and margins, creates a ticket, stores it only in host memory, and sends its display DTO to WebView. Execution accepts only a `ticketId`; atomically remove it before starting. Persist every progress transition. On connect, fetch positions and reconcile. On closing, cancel unsent work while preserving records already written.

- [ ] **Step 4: Run tests and verify GREEN**

Run the full test command. Verify no browser payload can provide epic, direction, or quantity to the mutation call.

- [ ] **Step 5: Commit**

```bash
git add desktop/CAPETF.Desktop/CapComTerminalWindow.xaml.cs desktop/CAPETF.Desktop/CapComTerminalWindow.xaml desktop/CAPETF.Desktop.Tests/SyntheticTradingTests.cs
git commit -m "feat: orchestrate synthetic demo execution"
```

### Task 6: Professional Trading Ticket And Position Workspace

**Files:**
- Modify: `desktop/CAPETF.Desktop/Assets/synthetic-terminal.html`
- Modify: `desktop/CAPETF.Desktop.Tests/SyntheticTradingTests.cs`

**Interfaces:**
- Consumes the callbacks defined in Task 5.
- Produces only action IDs and requested basket notional; execution uses only the one-time ticket ID returned by preflight.

- [ ] **Step 1: Write failing DOM/runtime tests**

Extend the existing Node DOM harness to assert `DEMO TRADING`, `Place Buy Basket`, `Place Sell Basket`, readiness statuses, exact-leg confirmation modal, progress text, independent rail scrolling, sticky summary, collapsible sections, execution cards, deal IDs, `Needs attention`, Refresh, and Close Basket. Assert button disabling during operations and no `Preview only` copy.

- [ ] **Step 2: Run tests and verify RED**

Expected: DOM contract fails for absent controls and callbacks.

- [ ] **Step 3: Implement the UI**

Use the existing quiet dark terminal style. Make the rail `overflow-y:auto`, its header sticky, and retain the draggable separator. Render compact status chips and tables; do not put cards inside cards. Confirmation lists every side/epic/quantity/price and requires a checkbox acknowledging partial-execution behavior before enabling Confirm. Keep long logs in an expandable audit section.

- [ ] **Step 4: Run tests and verify GREEN**

Run the full test command and confirm the browser harness has no console errors.

- [ ] **Step 5: Commit**

```bash
git add desktop/CAPETF.Desktop/Assets/synthetic-terminal.html desktop/CAPETF.Desktop.Tests/SyntheticTradingTests.cs
git commit -m "feat: add synthetic demo trading workspace"
```

### Task 7: End-to-End Failure, Recovery, And Packaging Verification

**Files:**
- Modify: `desktop/CAPETF.Desktop.Tests/SyntheticTradingTests.cs`
- Modify: `desktop/README.md`

**Interfaces:**
- Verifies all prior public contracts together.

- [ ] **Step 1: Add failing end-to-end scripted scenarios**

Run a three-leg accepted basket through preflight, one-time ticket consumption, sequential confirmations, persistence, restart, open-position reconciliation, and close. Add a second scenario where leg two rejects and leg one remains open with `Needs attention`.

- [ ] **Step 2: Run tests and verify RED if integration gaps remain**

Run the full suite. Expected: any missing wiring fails with the exact state/call-order mismatch.

- [ ] **Step 3: Apply only the minimal integration corrections**

Correct wiring uncovered by the scenarios without changing the approved execution rules. Update the README with demo-only scope, execution states, recovery location, and the fact that partial positions are not automatically closed.

- [ ] **Step 4: Verify tests, build, and publish**

```powershell
dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj -c Release
dotnet build desktop/CAPETF.Desktop/CAPETF.Desktop.csproj -c Release
dotnet publish desktop/CAPETF.Desktop/CAPETF.Desktop.csproj -c Release -r win-x64 --self-contained true -o desktop/publish/cap.com-terminal-v4-complete
```

Expected: tests pass; build/publish complete with zero errors; published/source `synthetic-terminal.html` hashes match; `CAPETF.exe` exists; no zip package is created.

- [ ] **Step 5: Commit**

```bash
git add desktop/CAPETF.Desktop.Tests/SyntheticTradingTests.cs desktop/README.md
git commit -m "test: verify synthetic demo order lifecycle"
```

### Task 8: Live Capital.com Demo Verification

**Files:**
- No source changes unless a reproducible defect is first captured by a failing automated test.

**Interfaces:**
- Uses the published `desktop/publish/cap.com-terminal-v4-complete/CAPETF.exe` and saved user-owned demo credentials.

- [ ] **Step 1: Launch the published app and connect**

Verify `DEMO TRADING`, active account identity, available funds, universe load, and reconciled prior execution list.

- [ ] **Step 2: Build and preflight a small liquid three-leg basket**

Use a basket whose executable minimum sizes fit current demo available funds. Confirm all three current market statuses, quote timestamps, quantities, estimated margin, and ticket expiry in the dialog.

- [ ] **Step 3: Place the demo basket and capture evidence**

Confirm the dialog once. Record every initial deal reference, permanent deal ID, accepted fill level, and timestamp. Verify the UI advances one leg at a time and does not duplicate requests.

- [ ] **Step 4: Reconcile and restart**

Use Refresh, compare the app against `GET /positions`, restart the app, reconnect, and verify the basket returns as `Open` with matching deal IDs and visible current P&L.

- [ ] **Step 5: Inspect UX and leave the basket open**

Verify scrolling, separator resizing, collapsible details, progress visibility, readable non-overlapping text, and chart readiness state. Leave the successful demo basket open for user inspection. Report exact deal references and the executable path.

- [ ] **Step 6: Final independent review**

Review for live-host mutation escape paths, duplicate submission races, ambiguous retry, data loss, unsafe close behavior, missing tests, and misleading UI. Fix Important or Critical findings through a new failing test, rerun the full suite/build/publish, and confirm a clean worktree.
