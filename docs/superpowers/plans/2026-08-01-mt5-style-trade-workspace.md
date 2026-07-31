# MT5-Style Synthetic Trade Workspace Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a resizable MT5-style bottom trading dock and honest synthetic Entry/SL/TP chart presentation to cap.com Terminal while preserving the existing Capital.com demo execution safeguards.

**Architecture:** Capital.com broker snapshots and the host-owned synthetic execution ledger remain authoritative. A new versioned host-side risk-plan store persists visual synthetic SL/TP levels, while the browser renders dense dock tables, chart overlays, and price lines from trusted callbacks. Browser messages may identify a basket and submit validated planning values, but never supply formula, broker position, or order-mutation fields.

**Tech Stack:** .NET 8 WPF, WebView2, C# records and JSON persistence, HTML/CSS/JavaScript, TradingView Lightweight Charts 5.2.0, custom console test suite.

## Global Constraints

- Capital.com remains authoritative for funds, equity, available margin, open positions, working orders, quotes, broker SL/TP, and running P/L.
- Synthetic planning levels are labelled `PLAN SL` and `PLAN TP`; they are not broker protection and do not trigger automatic closure.
- Existing Buy, Sell, Close Basket, preflight, and confirmation safeguards remain unchanged.
- Formula multipliers and basket membership come only from the persisted host execution ledger.
- The dock renders broker positions, working orders, and persisted baskets only; it must not render the full instrument universe.
- No new charting dependency is introduced.

---

### Task 1: Persist Validated Synthetic Risk Plans

**Files:**
- Create: `desktop/CAPETF.Desktop/SyntheticRiskPlanStore.cs`
- Modify: `desktop/CAPETF.Desktop.Tests/SyntheticTradingTests.cs`

**Interfaces:**
- Produces: `SyntheticRiskPlan`, `SyntheticRiskPlanValidation.Validate(...)`, and `SyntheticRiskPlanStore.LoadAll/Upsert/Remove`.
- Consumes: basket identity, execution side, and calculated synthetic entry supplied by the host.

- [ ] **Step 1: Write failing validation and persistence tests**

Add a test to `SyntheticTradingTests.RunAll()` that verifies a BUY plan requires `stopLoss < entry < takeProfit`, a SELL plan requires `takeProfit < entry < stopLoss`, zero/negative/non-finite-equivalent decimal inputs are rejected, and a saved plan survives a fresh store instance.

```csharp
var buy = SyntheticRiskPlanValidation.Validate("execution-1", "basket-1", "BUY", 100m, 92m, 118m);
AssertTrue(buy.IsValid, "BUY plan surrounds entry");
AssertFalse(SyntheticRiskPlanValidation.Validate("execution-1", "basket-1", "BUY", 100m, 105m, 118m).IsValid,
    "BUY stop must remain below entry");

var path = Path.Combine(CreateTemporaryFolder(), "synthetic-risk-plans.json");
var store = new SyntheticRiskPlanStore(path);
store.Upsert(buy.Plan!);
AssertEqual(JsonSerializer.Serialize(buy.Plan), JsonSerializer.Serialize(new SyntheticRiskPlanStore(path).LoadAll().Single()),
    "risk plan persists exactly");
```

- [ ] **Step 2: Run the focused suite and verify RED**

Run: `dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj -- trading`

Expected: compilation fails because `SyntheticRiskPlanValidation` and `SyntheticRiskPlanStore` do not exist.

- [ ] **Step 3: Implement the focused model and store**

Create these contracts:

```csharp
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

public static class SyntheticRiskPlanValidation
{
    public static SyntheticRiskPlanValidationResult Validate(
        string executionId,
        string basketId,
        string side,
        decimal entry,
        decimal? stopLoss,
        decimal? takeProfit,
        DateTimeOffset? now = null);
}
```

Use an atomic temporary-file replacement pattern matching `SyntheticExecutionStore`. Store schema version `1`, reject duplicate execution IDs on load, and normalize side to `BUY` or `SELL`. Permit either planning level to be empty; validate every supplied level against the entry and side.

- [ ] **Step 4: Run the focused suite and verify GREEN**

Run: `dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj -- trading`

Expected: `SyntheticTrading tests passed`.

- [ ] **Step 5: Commit the risk-plan domain**

```powershell
git add desktop/CAPETF.Desktop/SyntheticRiskPlanStore.cs desktop/CAPETF.Desktop.Tests/SyntheticTradingTests.cs
git commit -m "Persist synthetic risk planning levels"
```

---

### Task 2: Add Host-Owned Risk-Plan Browser Contracts

**Files:**
- Modify: `desktop/CAPETF.Desktop/SyntheticTradingHostCoordinator.cs`
- Modify: `desktop/CAPETF.Desktop/CapComTerminalWindow.xaml.cs`
- Modify: `desktop/CAPETF.Desktop.Tests/SyntheticTradingTests.cs`

**Interfaces:**
- Consumes: `SyntheticRiskPlanStore` from Task 1 and `_terminalExecutions` already maintained by the WPF host.
- Produces: `SyntheticSetRiskPlanRequest`, `SyntheticClearRiskPlanRequest`, and callback `window.setTerminalRiskPlans(plans)`.

- [ ] **Step 1: Write failing parser and host-source tests**

Test the exact accepted browser payloads:

```csharp
{"type":"setRiskPlan","executionId":"execution-1","stopLoss":92.5,"takeProfit":118.0}
{"type":"clearRiskPlan","executionId":"execution-1"}
```

Reject payloads containing `epic`, `dealId`, `multiplier`, `direction`, or `quantity`. Add source-contract assertions for `setTerminalRiskPlans`, `SetSyntheticRiskPlanAsync`, and `ClearSyntheticRiskPlanAsync`.

- [ ] **Step 2: Run the focused suite and verify RED**

Run: `dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj -- trading`

Expected: parser rejects `setRiskPlan` and the source contract is absent.

- [ ] **Step 3: Implement strict request parsing**

Add:

```csharp
internal sealed record SyntheticSetRiskPlanRequest(
    string ExecutionId,
    decimal? StopLoss,
    decimal? TakeProfit) : SyntheticTradingBrowserRequest;

internal sealed record SyntheticClearRiskPlanRequest(string ExecutionId)
    : SyntheticTradingBrowserRequest;
```

`setRiskPlan` accepts only `type`, `executionId`, `stopLoss`, and `takeProfit`. Values may be JSON null or decimal. `clearRiskPlan` accepts only `type` and `executionId`.

- [ ] **Step 4: Integrate host validation and publication**

Add a `_riskPlanStore` field. Resolve the execution exclusively from `_terminalExecutions`, calculate entry as `Sum(leg.Multiplier * leg.FillLevel-or-ReferencePrice)`, validate using Task 1, persist, then publish the entire plan list:

```csharp
private Task PublishTerminalRiskPlansAsync() =>
    PublishTerminalCallbackAsync("setTerminalRiskPlans", _riskPlanStore.LoadAll());
```

Publish plans after chart initialization, connection, execution refresh, set, and clear. On validation failure, publish `setTerminalRiskPlanError` and leave the previous plan unchanged.

- [ ] **Step 5: Run focused and full suites**

Run:

```powershell
dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj -- trading
dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj
```

Expected: both report passed suites with exit code `0`.

- [ ] **Step 6: Commit host-owned risk-plan messaging**

```powershell
git add desktop/CAPETF.Desktop/SyntheticTradingHostCoordinator.cs desktop/CAPETF.Desktop/CapComTerminalWindow.xaml.cs desktop/CAPETF.Desktop.Tests/SyntheticTradingTests.cs
git commit -m "Wire synthetic risk plans to terminal host"
```

---

### Task 3: Build The Resizable Bottom Trade Dock

**Files:**
- Modify: `desktop/CAPETF.Desktop/Assets/synthetic-terminal.html`
- Modify: `desktop/CAPETF.Desktop.Tests/SyntheticTradingTests.cs`

**Interfaces:**
- Consumes: existing `terminalBrokerSnapshot`, `terminalExecutions`, `postHostAction`, and `showExecutionBasket` action.
- Produces: dock tabs `positions`, `pending`, `baskets`, `history`; row selection; horizontal splitter; minimized account strip.

- [ ] **Step 1: Extend the HTML contract test and JavaScript runtime test**

Require these stable IDs:

```text
trade-dock
trade-dock-splitter
trade-dock-minimize
trade-tab-positions
trade-tab-pending
trade-tab-baskets
trade-tab-history
trade-dock-table
trade-dock-account-strip
```

In the Node runtime harness, call `setTerminalBrokerSnapshot` and `setTerminalExecutions`, select each tab, and assert table headers and row text. Assert clicking a basket row posts only `{ type: 'showExecutionBasket', executionId }`.

- [ ] **Step 2: Run the focused suite and verify RED**

Run: `dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj -- trading`

Expected: the HTML contract test reports missing `trade-dock`.

- [ ] **Step 3: Add dock structure and restrained styling**

Change the chart workspace to two rows: chart area plus dock. Add a horizontal splitter with pointer capture and clamp dock height between `118px` and `45vh`. The minimized state is `34px`. Persist `{height, minimized, activeTab}` in `localStorage` under `capetf.tradeDock.v1`.

Use one unframed table surface with sticky headers. Use compact text, tab buttons, and icon buttons for minimize/restore. Do not use cards or nested cards.

- [ ] **Step 4: Implement row projections in JavaScript**

Create pure functions:

```javascript
function brokerPositionRows(snapshot, executions) { /* deal-linked leg rows */ }
function pendingOrderRows(snapshot) { /* working-order rows */ }
function syntheticBasketRows(executions, snapshot, riskPlans) { /* aggregate rows */ }
function historyRows(executions) { /* closed/rejected executions */ }
```

Position columns: symbol, basket, side, quantity, entry, bid, ask, broker SL, broker TP, P/L, status. Basket columns: basket, side, legs, synthetic entry, bid, ask, PLAN SL, PLAN TP, margin, P/L, state. Render `n/a` rather than zero when a broker value is absent.

- [ ] **Step 5: Connect row selection without mutation**

Clicking a synthetic basket or linked position row sets `selectedDockExecutionId`, applies a selected row style, and posts `showExecutionBasket`. Close actions remain separate buttons and continue through the existing confirmation modal.

- [ ] **Step 6: Run tests and commit**

Run: `dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj`

Expected: both suites pass.

```powershell
git add desktop/CAPETF.Desktop/Assets/synthetic-terminal.html desktop/CAPETF.Desktop.Tests/SyntheticTradingTests.cs
git commit -m "Add resizable terminal trade dock"
```

---

### Task 4: Add Chart Overlay And Honest Entry/SL/TP Lines

**Files:**
- Modify: `desktop/CAPETF.Desktop/Assets/synthetic-terminal.html`
- Modify: `desktop/CAPETF.Desktop.Tests/SyntheticTradingTests.cs`

**Interfaces:**
- Consumes: active chart payload, matching execution record, broker snapshot, and risk plans.
- Produces: `activeSyntheticTradeView()`, compact overlay, and labelled price lines.

- [ ] **Step 1: Write failing aggregate and line-rendering runtime tests**

Use a three-leg BUY execution with trusted multipliers and matching broker positions. Assert:

```javascript
assert.equal(view.entry, 713.60);
assert.equal(view.runningProfitLoss, -8.01);
assert.equal(view.brokerStopLoss, null);
assert.equal(view.planStopLoss, 680);
assert.equal(view.planTakeProfit, 760);
```

Assert the chart creates `Entry`, `PLAN SL`, and `PLAN TP` labels, but does not create a `Broker SL` label when Capital returns no stop.

- [ ] **Step 2: Run the focused suite and verify RED**

Run: `dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj -- trading`

Expected: `activeSyntheticTradeView` is undefined or required labels are absent.

- [ ] **Step 3: Implement exact execution matching**

Match positions by `DealId` first and by unique epic only as a legacy fallback. Require every execution leg to match once. Calculate:

```javascript
entry = sum(multiplier * position.level)
currentBid = sum(multiplier >= 0 ? multiplier * position.bid : multiplier * position.offer)
currentAsk = sum(multiplier >= 0 ? multiplier * position.offer : multiplier * position.bid)
runningProfitLoss = sum(position.profitLoss)
```

Do not render execution lines when matching is incomplete or ambiguous.

- [ ] **Step 4: Render distinguishable price lines**

Use solid blue `Entry`, dashed orange `Broker SL`, dashed green `Broker TP`, dotted red `PLAN SL`, and dotted lime `PLAN TP`. Keep Bid and Ask unchanged. Broker projection lines are created only from actual Capital position levels; planning lines come only from the host-published risk plan.

- [ ] **Step 5: Add the compact chart overlay**

Add `synthetic-trade-overlay` in the upper-left chart area. It contains basket symbol, currency, direction, leg count, running P/L, estimated margin, and a one-line formula. Add an icon-only collapse button with a tooltip. Keep overlay width constrained to `min(420px, 36vw)` and never exceed two formula lines.

- [ ] **Step 6: Add selected-basket planning controls**

In the basket dock tab, show numeric PLAN SL and PLAN TP inputs only for the selected execution. `Apply` posts `setRiskPlan`; `Clear` posts `clearRiskPlan`. The nearby copy reads `Visual plan only; not a Capital.com stop.` without occupying the chart.

- [ ] **Step 7: Run tests and commit**

Run: `dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj`

Expected: both suites pass.

```powershell
git add desktop/CAPETF.Desktop/Assets/synthetic-terminal.html desktop/CAPETF.Desktop.Tests/SyntheticTradingTests.cs
git commit -m "Show synthetic trade levels on chart"
```

---

### Task 5: Publish And Visually Verify The Complete Workspace

**Files:**
- Verify: `desktop/CAPETF.Desktop/Assets/synthetic-terminal.html`
- Publish: `desktop/publish/cap.com-terminal-v4-complete/`

**Interfaces:**
- Consumes: all previous tasks.
- Produces: verified unzipped `CAPETF.exe` with the new workspace.

- [ ] **Step 1: Run clean verification**

```powershell
dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj
git diff --check
```

Expected: `SyntheticTrading tests passed`, `SyntheticBasketBuilder tests passed`, and exit code `0`.

- [ ] **Step 2: Close the running terminal and publish**

Close the existing `cap.com Terminal` window through Computer Use, confirm no `CAPETF` process remains, then run:

```powershell
dotnet publish desktop/CAPETF.Desktop/CAPETF.Desktop.csproj -c Release -r win-x64 --self-contained true -o desktop/publish/cap.com-terminal-v4-complete
```

Expected: publish exit code `0` with no locked-file retries.

- [ ] **Step 3: Perform live read-only verification**

Open `desktop/publish/cap.com-terminal-v4-complete/CAPETF.exe`, connect to the saved Capital.com demo credentials, and do not place or close any trade. Verify:

- The open basket restores from a dock row.
- The bottom dock resizes and minimizes.
- Positions show broker entry, current Bid/Ask, running P/L, and honest `n/a` SL/TP.
- The synthetic basket row shows aggregate entry, P/L, margin, and state.
- PLAN SL and PLAN TP persist across an app restart and appear as distinct chart lines.
- The chart overlay remains compact at 1536x816 and does not cover the latest candles.
- No pending orders is shown when `/workingorders` returns none.

- [ ] **Step 4: Verify shutdown and repository state**

Close and reopen the app once to prove the broker loop does not retain a headless process. Run `git status --short` and inspect the final diff for credentials or generated churn.

- [ ] **Step 5: Commit final integration adjustments**

If visual verification required source adjustments, rerun Step 1 and commit only those files:

```powershell
git add desktop/CAPETF.Desktop desktop/CAPETF.Desktop.Tests
git commit -m "Polish synthetic trading workspace"
```
