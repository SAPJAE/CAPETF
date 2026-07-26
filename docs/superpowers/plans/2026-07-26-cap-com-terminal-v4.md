# cap.com Terminal V4 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver an unpacked Windows terminal with bid/ask-only synthetic quotes, first-common-candle index normalization, full available Capital.com history, visible progress, a resizable chart rail, expanded drawing tools, and separate stock and ETF universes.

**Architecture:** Keep WPF responsible for credentials, API calls, operation state, universe selection, basket lifecycle, and streaming. Keep WebView2 plus bundled TradingView Lightweight Charts responsible for chart rendering and drawings. Use cached data only for candidate discovery, then load full Capital.com history for the selected legs and rebuild those exact components.

**Tech Stack:** .NET/WPF, C#, WebView2, local HTML/CSS/JavaScript, TradingView Lightweight Charts, Capital.com REST/WebSocket APIs, existing executable test harness.

## Global Constraints

- The terminal remains an analysis and staging tool; it must not place live orders.
- Stocks are the default universe, and stock and ETF candidates must never be mixed in one basket.
- Closed markets remain eligible; close-only and obsolete instruments remain excluded.
- Display-index multipliers and executable-preview quantities are separate concepts.
- Missing or zero quotes render as unavailable, never as a synthetic zero.
- Weekly and daily request all available history; intraday renders all history Capital.com returns without fabricating candles.
- Lightweight Charts remains bundled locally; TradingView Advanced Charts is outside this release.
- Publish an unpacked Windows executable, not a ZIP archive.

---

### Task 1: First-Common-Candle Display Index

**Files:**
- Modify: `desktop/CAPETF.Desktop/SyntheticBasketBuilder.cs`
- Modify: `desktop/CAPETF.Desktop/SyntheticModels.cs`
- Test: `desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs`

**Interfaces:**
- Consumes: `IReadOnlyList<MarketInstrument>` and per-epic `IReadOnlyList<OhlcPoint>` already accepted by `SyntheticBasketBuilder.Build`.
- Produces: `SyntheticBasketBuilder.FindSharedBaselinePrices(...)` internally and baskets whose first intersected close is `100`, with fixed `FormulaMultiplier` values usable by historical and live quote calculations.

- [ ] **Step 1: Write failing index-normalization tests**

Add harness calls and tests proving that three components with different individual starting dates use the earliest shared timestamp, produce a first synthetic close of `100`, keep strict intersection, and do not force the latest close to `100`.

```csharp
private static void SyntheticIndexStartsAtOneHundredOnFirstSharedCandle()
{
    var result = SyntheticBasketBuilder.Build(
        "US / USD / Tech",
        [Instrument("A", 100m), Instrument("B", 50m), Instrument("C", 25m)],
        new Dictionary<string, IReadOnlyList<OhlcPoint>>
        {
            ["A"] = Candles((1, 100m), (2, 110m), (3, 120m)),
            ["B"] = Candles((2, 50m), (3, 60m)),
            ["C"] = Candles((2, 25m), (3, 20m)),
        },
        maxBaskets: 1,
        periodsPerYear: 252,
        minimumCandles: 2);

    var basket = result.Baskets.Single();
    AssertNear(100m, basket.Candles[0].Close, "first shared candle must be the index base");
    AssertTrue(basket.Candles[^1].Close != 100m, "latest candle must not be rebased to 100");
    AssertEqual(2, basket.Candles.Count, "only shared timestamps may be rendered");
}
```

- [ ] **Step 2: Run the harness and verify the new test fails**

Run: `dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj`

Expected: FAIL because the current builder uses each component's latest close as its reference and the latest synthetic close remains `100`.

- [ ] **Step 3: Implement shared-baseline multipliers**

In `SyntheticBasketBuilder`, determine the cadence exactly as `BuildCandles` does, intersect its timestamp/date/week keys, select the earliest common key, and return each component's close at that key. Calculate each multiplier as `weight / sharedBaselinePrice`, assign that same price to `FormulaReferencePrice` and `SyntheticBaselinePrice`, then build candles with the fixed multipliers.

```csharp
private static IReadOnlyList<decimal> CalculateDisplayMultipliers(
    IReadOnlyList<Candidate> cluster,
    IReadOnlyList<decimal> weights,
    IReadOnlyList<decimal> sharedBaselinePrices) =>
    sharedBaselinePrices.Select((price, index) =>
        price <= 0 ? 0m : decimal.Round(weights[index] / price, 8)).ToList();
```

Retain strict intersection and open normalization. Return no basket when a complete positive shared baseline cannot be found.

- [ ] **Step 4: Run the complete harness**

Run: `dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj`

Expected: PASS, including existing gap-normalization and selector tests.

- [ ] **Step 5: Commit the index behavior**

```powershell
git add desktop/CAPETF.Desktop/SyntheticBasketBuilder.cs desktop/CAPETF.Desktop/SyntheticModels.cs desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs
git commit -m "Rebase synthetic index at shared history start"
```

### Task 2: Bid/Ask Contract And Executable Preview Sizing

**Files:**
- Modify: `desktop/CAPETF.Desktop/SyntheticModels.cs`
- Modify: `desktop/CAPETF.Desktop/SyntheticOrderSizing.cs`
- Modify: `desktop/CAPETF.Desktop/SyntheticTerminalModels.cs`
- Modify: `desktop/CAPETF.Desktop/SyntheticTerminalChartPayload.cs`
- Test: `desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs`

**Interfaces:**
- Consumes: fixed display multipliers, component bid/offer, `MinDealSize`, and `MinSizeIncrement`.
- Produces: `SyntheticOrderSizing.ExecutableLegPreview(SyntheticComponent component, decimal basketNotional, decimal referencePrice)` returning `ExecutableLegPreview(Quantity, Notional, WeightPct)`, and a chart payload containing bid/ask but no synthetic last-price display field.

- [ ] **Step 1: Write failing quote and sizing tests**

Add tests asserting: all-positive component bids/offers produce synthetic bid/ask; any null or zero side produces null; executable quantities round upward to the Capital.com increment and minimum; equal target notionals are calculated independently of `FormulaMultiplier`.

```csharp
private static void ExecutablePreviewUsesCurrentEqualNotionalAndDealRules()
{
    var component = Component("A", bid: 49m, offer: 51m, minDeal: 0.1m, increment: 0.1m);
    component.FormulaMultiplier = 9.99m;
    var preview = SyntheticOrderSizing.ExecutableLegPreview(component, 300m, 50m);
    AssertNear(2m, preview.Quantity, "one-third of 300 at price 50 is quantity 2");
    AssertNear(100m, preview.Notional, "preview notional must use executable quantity");
}
```

- [ ] **Step 2: Verify the tests fail for the expected contract differences**

Run: `dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj`

Expected: FAIL because `ExecutableLegPreview` does not exist and the terminal payload still carries/display-contracts `LastPrice`.

- [ ] **Step 3: Implement bid/ask-only payload and separate sizing**

Add:

```csharp
public sealed record ExecutableLegPreview(decimal Quantity, decimal Notional, decimal WeightPct);
```

Calculate target leg notional as `basketNotional * component.Weight / 100`, divide by a positive current reference price, then round upward to `MinDealSize` and `MinSizeIncrement`. Keep `SyntheticQuoteCalculator` strict for bid and ask and remove last-price calculation from its public result. Remove `LastPrice` from `SyntheticTerminalPayload` and remove `Last` from `TerminalComponentRow` unless it is required internally for staleness fallback; no HTML-facing payload may label it as a displayed last price.

- [ ] **Step 4: Run the harness**

Run: `dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj`

Expected: PASS.

- [ ] **Step 5: Commit quote and sizing behavior**

```powershell
git add desktop/CAPETF.Desktop/SyntheticModels.cs desktop/CAPETF.Desktop/SyntheticOrderSizing.cs desktop/CAPETF.Desktop/SyntheticTerminalModels.cs desktop/CAPETF.Desktop/SyntheticTerminalChartPayload.cs desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs
git commit -m "Separate synthetic quotes from order sizing"
```

### Task 3: Full Selected-Leg History Service

**Files:**
- Create: `desktop/CAPETF.Desktop/SyntheticHistoryService.cs`
- Modify: `desktop/CAPETF.Desktop/CapitalApiClient.cs`
- Modify: `desktop/CAPETF.Desktop/CapComTerminalWindow.xaml.cs`
- Test: `desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs`

**Interfaces:**
- Consumes: `CapitalApiClient.GetAllAvailableOhlcPricesAsync`, selected component epics, terminal timeframe, and `IProgress<HistoryLoadProgress>`.
- Produces: `SyntheticHistoryService.LoadSelectedAsync(...)`, `SyntheticHistoryService.RequestResolution(string)`, `SyntheticHistoryService.Transform(...)`, and `HistoryLoadResult(CandlesByEpic, SharedStart, SharedEnd, SharedCount)`.

- [ ] **Step 1: Write failing timeframe and component-preservation tests**

Test the mapping `Weekly -> WEEK`, `Daily -> DAY`, `4H -> HOUR_4`, `2H/6H -> HOUR`; verify 2H and 6H aggregation OHLC values; verify rebuilding from refreshed history accepts the exact selected epics rather than rerunning the selector.

```csharp
AssertEqual("HOUR", SyntheticHistoryService.RequestResolution("2H"), "2H source");
AssertEqual("HOUR_4", SyntheticHistoryService.RequestResolution("4H"), "4H source");
AssertEqual("DAY", SyntheticHistoryService.RequestResolution("Daily"), "daily source");
AssertEqual("WEEK", SyntheticHistoryService.RequestResolution("Weekly"), "weekly source");
```

- [ ] **Step 2: Verify the new history tests fail**

Run: `dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj`

Expected: FAIL because the focused service does not exist.

- [ ] **Step 3: Implement paging windows and history service**

Move timeframe mapping and aggregation from `CapComTerminalWindow` into the service. Update `CapitalApiClient.HistoricalWindow` so daily pages use a one-year request window, weekly uses ten years, hourly uses thirty days, and four-hour uses one hundred twenty days; continue paging until the current termination conditions are met. Report progress once per component.

After cached candidate selection, call `LoadSelectedAsync` for only the selected basket components. Rebuild with those exact `MarketInstrument` instances and the returned full histories. On timeframe change, preserve component epics, reload full histories, rebuild, refresh market details, rerender, and resubscribe streaming.

- [ ] **Step 4: Run the harness and Release build**

Run: `dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj`

Run: `dotnet build desktop/CAPETF.Desktop/CAPETF.Desktop.csproj -c Release`

Expected: both commands succeed with zero errors.

- [ ] **Step 5: Commit selected-leg history loading**

```powershell
git add desktop/CAPETF.Desktop/SyntheticHistoryService.cs desktop/CAPETF.Desktop/CapitalApiClient.cs desktop/CAPETF.Desktop/CapComTerminalWindow.xaml.cs desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs
git commit -m "Load full history for selected synthetic legs"
```

### Task 4: Separate Stock And ETF Universes

**Files:**
- Create: `desktop/CAPETF.Desktop/DashboardEtfDataLoader.cs`
- Create: `desktop/CAPETF.Desktop/TerminalUniverse.cs`
- Modify: `desktop/CAPETF.Desktop/CapitalInstrumentTypes.cs`
- Modify: `desktop/CAPETF.Desktop/CapComTerminalWindow.xaml`
- Modify: `desktop/CAPETF.Desktop/CapComTerminalWindow.xaml.cs`
- Modify: `desktop/CAPETF.Desktop/CAPETF.Desktop.csproj`
- Test: `desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs`

**Interfaces:**
- Consumes: encrypted `data/etfs.enc.json`, cached stock chunks, and Capital.com market results.
- Produces: `TerminalUniverseKind.Stocks`, `TerminalUniverseKind.ETFs`, `DashboardEtfDataLoader.LoadEtfs`, `CapitalInstrumentTypes.IsEtf`, and per-universe instrument/history caches.

- [ ] **Step 1: Write failing ETF recognition and isolation tests**

Add tests for common Capital.com ETF type values present in `etfs.enc.json`, verify `SHARES` remains stock-only, and verify a basket request with ETF eligibility excludes stocks.

```csharp
AssertTrue(CapitalInstrumentTypes.IsEtf(new MarketInstrument { Type = "ETF" }), "ETF type");
AssertFalse(CapitalInstrumentTypes.IsEtf(new MarketInstrument { Type = "SHARES" }), "stocks are not ETFs");
AssertTrue(TerminalUniverse.Accepts(TerminalUniverseKind.ETFs, etf), "ETF universe accepts ETF");
AssertFalse(TerminalUniverse.Accepts(TerminalUniverseKind.ETFs, stock), "ETF universe excludes stock");
```

- [ ] **Step 2: Verify ETF tests fail**

Run: `dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj`

Expected: FAIL because ETF recognition and universe policy do not exist.

- [ ] **Step 3: Implement ETF loading and the universe selector**

Reuse the existing AES-GCM dashboard decryption format in a focused ETF loader. Parse instrument identity, currency, region, sector/type, tradeability status, price, and weekly/daily/hourly chart points. Add `UniverseBox` before `BlockBox` with Stocks selected by default. Maintain separate stock and ETF cache results, rebuild groups and symbol search when the universe changes, and filter build candidates through `TerminalUniverse.Accepts`.

Ensure `data/etfs.enc.json` is copied to the publish output by the desktop project. API fallback searches markets and retains only the selected universe type plus the existing open-eligibility policy.

- [ ] **Step 4: Run tests and build**

Run: `dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj`

Run: `dotnet build desktop/CAPETF.Desktop/CAPETF.Desktop.csproj -c Release`

Expected: PASS and zero build errors.

- [ ] **Step 5: Commit ETF universe support**

```powershell
git add desktop/CAPETF.Desktop/DashboardEtfDataLoader.cs desktop/CAPETF.Desktop/TerminalUniverse.cs desktop/CAPETF.Desktop/CapitalInstrumentTypes.cs desktop/CAPETF.Desktop/CapComTerminalWindow.xaml desktop/CAPETF.Desktop/CapComTerminalWindow.xaml.cs desktop/CAPETF.Desktop/CAPETF.Desktop.csproj desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs
git commit -m "Add ETF synthetic basket universe"
```

### Task 5: Operation Progress And Duplicate-Action Guard

**Files:**
- Create: `desktop/CAPETF.Desktop/TerminalOperationState.cs`
- Modify: `desktop/CAPETF.Desktop/CapComTerminalWindow.xaml`
- Modify: `desktop/CAPETF.Desktop/CapComTerminalWindow.xaml.cs`
- Test: `desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs`

**Interfaces:**
- Consumes: operation name, optional current/total counts, and asynchronous delegates.
- Produces: `TerminalOperationState.TryBegin`, `Report`, `Complete`, `Fail`, plus WPF bindings for bottom-right progress visibility, label, percentage, and busy state.

- [ ] **Step 1: Write failing operation-state tests**

Test that a second `TryBegin` returns false while active, progress is clamped to the total, completion clears busy state, and failure clears busy state while retaining an error message.

```csharp
var state = new TerminalOperationState();
AssertTrue(state.TryBegin("Loading history", 3), "first operation starts");
AssertFalse(state.TryBegin("Loading history", 3), "duplicate operation is rejected");
state.Report(2);
AssertNear(66.67m, state.Percent, "progress percentage");
state.Complete("History loaded");
AssertFalse(state.IsBusy, "completion releases operation guard");
```

- [ ] **Step 2: Verify operation tests fail**

Run: `dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj`

Expected: FAIL because `TerminalOperationState` does not exist.

- [ ] **Step 3: Implement progress UI and guarded operations**

Add a bottom-right bordered panel over the chart with label and `ProgressBar`. Bind determinate/indeterminate state in code-behind. Route connect, universe load, build, selected-leg history, market details, and stream startup through one guarded `RunOperationAsync` helper. Disable Connect, Build Basket, universe, group, strategy, saved basket, and timeframe controls while busy; restore them in `finally`.

- [ ] **Step 4: Run tests and build**

Run: `dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj`

Run: `dotnet build desktop/CAPETF.Desktop/CAPETF.Desktop.csproj -c Release`

Expected: PASS and zero build errors.

- [ ] **Step 5: Commit operation feedback**

```powershell
git add desktop/CAPETF.Desktop/TerminalOperationState.cs desktop/CAPETF.Desktop/CapComTerminalWindow.xaml desktop/CAPETF.Desktop/CapComTerminalWindow.xaml.cs desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs
git commit -m "Show terminal operation progress"
```

### Task 6: Resizable Rail, Bid/Ask Display, And Drawing Tools

**Files:**
- Modify: `desktop/CAPETF.Desktop/Assets/synthetic-terminal.html`
- Modify: `desktop/CAPETF.Desktop/CapComTerminalWindow.xaml.cs`
- Test: `desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs`

**Interfaces:**
- Consumes: `SyntheticTerminalPayload` from Task 2 and existing `window.setTerminalData`, `window.updateTerminalTick`, and chart-control functions.
- Produces: `window.setTerminalBusy`, `window.toggleTerminalComponents`, splitter persistence, bid/ask-only metadata and price lines, ray/rectangle drawing primitives, and per-symbol drawing persistence.

- [ ] **Step 1: Write failing HTML contract tests**

Read the packaged HTML as existing tests do and assert that it contains `id="component-splitter"`, pointer drag handlers, `localStorage`, tools `RAY` and `RECT`, bid and ask labels, and no `id="last-price"`, `title: 'Last'`, or visible `Last ${...}` metadata.

```csharp
AssertContains(html, "component-splitter", "chart rail splitter");
AssertContains(html, "data-tool=\"ray\"", "ray drawing tool");
AssertContains(html, "data-tool=\"rectangle\"", "rectangle drawing tool");
AssertNotContains(html, "id=\"last-price\"", "last-price display must be removed");
```

- [ ] **Step 2: Verify HTML tests fail**

Run: `dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj`

Expected: FAIL because the splitter and new drawing tools are absent and last-price markup remains.

- [ ] **Step 3: Implement the chart workspace changes**

Change the chart workspace grid to `minmax(0, 1fr) 6px var(--rail-width)`, insert a keyboard-accessible splitter, clamp rail width to `260..620` pixels, and save it under `capcom-terminal-rail-width`. Hide splitter and rail together when collapsed.

Render only bid and ask in metadata and price lines. Show currency and shared candle range/count. Add ray and rectangle primitives using the existing primitive attachment pattern. Persist serializable drawing coordinates under `capcom-terminal-drawings:<synthetic-symbol>` and restore them after full chart rerenders; live candle updates must not clear primitives.

- [ ] **Step 4: Run tests and build**

Run: `dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj`

Run: `dotnet build desktop/CAPETF.Desktop/CAPETF.Desktop.csproj -c Release`

Expected: PASS and zero build errors.

- [ ] **Step 5: Commit chart usability changes**

```powershell
git add desktop/CAPETF.Desktop/Assets/synthetic-terminal.html desktop/CAPETF.Desktop/CapComTerminalWindow.xaml.cs desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs
git commit -m "Improve terminal chart workspace"
```

### Task 7: End-To-End Verification And Unpacked Release

**Files:**
- Modify: `desktop/README.md`
- Create: `desktop/publish/cap.com-terminal-v4/` through `dotnet publish`
- Test: `desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs`

**Interfaces:**
- Consumes: completed V4 desktop project and saved user credentials.
- Produces: launchable `desktop/publish/cap.com-terminal-v4/CAPETF.exe`, verification screenshots, and concise run instructions.

- [ ] **Step 1: Add final static acceptance checks**

Add tests verifying the XAML contains `UniverseBox` and `OperationProgressBar`, the HTML footer identifies Terminal V4, and project output includes `data/etfs.enc.json` plus local Lightweight Charts assets.

- [ ] **Step 2: Run the full automated verification**

Run: `dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj`

Run: `dotnet build desktop/CAPETF.Desktop/CAPETF.Desktop.csproj -c Release`

Expected: all harness checks pass and the build reports zero errors.

- [ ] **Step 3: Publish to a new unpacked directory**

Run:

```powershell
dotnet publish desktop/CAPETF.Desktop/CAPETF.Desktop.csproj -c Release -r win-x64 --self-contained true -o desktop/publish/cap.com-terminal-v4
```

Expected: `desktop/publish/cap.com-terminal-v4/CAPETF.exe` exists together with WebView assets and encrypted stock/ETF data.

- [ ] **Step 4: Launch and manually verify the release**

Start `CAPETF.exe`, connect using saved credentials, and verify one stock basket and one ETF basket. For the stock basket, switch Weekly, Daily, 6H, 4H, and 2H and record component epics, candle count/range, currency, non-zero bid/ask, splitter resizing, progress panel, and drawing restoration. Capture a screenshot of the stock chart and ETF chart after data is visible.

- [ ] **Step 5: Update the desktop readme with the direct executable path**

Document the unpacked executable location, automatic connection/universe/stream behavior, universe switching, basket build/save flow, and the fact that history depth is bounded by Capital.com availability.

- [ ] **Step 6: Run final diff and test verification**

Run: `git diff --check`

Run: `dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj`

Expected: no whitespace errors and all tests pass.

- [ ] **Step 7: Commit release documentation**

```powershell
git add desktop/README.md desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs
git commit -m "Document cap.com Terminal V4 release"
```
