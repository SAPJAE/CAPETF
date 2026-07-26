# Synthetic Strategies Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add strategy-based synthetic basket selection and accurate synthetic bid/ask/last values to cap.com Terminal.

**Architecture:** Keep existing seed-symbol selection, add a strategy selector that can either choose peers around a seed or scan the selected block. Strategy ranking is computed from available Capital.com OHLC history, then the existing synthetic basket builder creates formula multipliers. Synthetic bid/ask/last are derived by applying those formula multipliers to component bid/offer/last prices.

**Tech Stack:** WPF/.NET 8, Capital.com REST and WebSocket APIs, existing Lightweight Charts HTML host.

## Global Constraints

- Use Capital.com data first; do not add paid data dependencies.
- Keep direct runnable publish folder; do not zip output.
- Synthetic trade preview must use the same formula multipliers as chart and realtime tick logic.
- Strategy filters must stay inside the selected block/currency/sector grouping unless a typed seed requires a broader exact-symbol lookup.

---

### Task 1: Strategy Model And Scoring

**Files:**
- Create: `desktop/CAPETF.Desktop/SyntheticStrategy.cs`
- Modify: `desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs`

**Interfaces:**
- Produces: `SyntheticStrategyKind`, `SyntheticStrategy`, `SyntheticStrategyCatalog.All`, `SyntheticStrategyRanker.Rank(...)`.

- [ ] Write tests for below-MA200, below-2Y-low, all-time-high, breakout, and dip-inside-uptrend ranking.
- [ ] Implement strategy metrics using ordered OHLC close data.
- [ ] Run `dotnet run --project desktop\CAPETF.Desktop.Tests\CAPETF.Desktop.Tests.csproj`.

### Task 2: Strategy Dropdown Wiring

**Files:**
- Modify: `desktop/CAPETF.Desktop/CapComTerminalWindow.xaml`
- Modify: `desktop/CAPETF.Desktop/CapComTerminalWindow.xaml.cs`
- Modify: `desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs`

**Interfaces:**
- Consumes: `SyntheticStrategyCatalog.All`, `SyntheticStrategyRanker.Rank(...)`.
- Produces: a WPF ComboBox named `StrategyBox` and `SelectedStrategy()`.

- [ ] Add a Strategy dropdown before Build Basket.
- [ ] Keep the seed symbol ComboBox editable and optional.
- [ ] In build flow, use seeded selection for `Similar to selected symbol`; otherwise use strategy-ranked candidates from the selected block.
- [ ] Run tests.

### Task 3: Synthetic Bid/Ask/Last

**Files:**
- Modify: `desktop/CAPETF.Desktop/SyntheticModels.cs`
- Modify: `desktop/CAPETF.Desktop/SyntheticTerminalModels.cs`
- Modify: `desktop/CAPETF.Desktop/SyntheticTerminalChartPayload.cs`
- Modify: `desktop/CAPETF.Desktop/SyntheticTerminalLiveUpdate.cs`
- Modify: `desktop/CAPETF.Desktop/Assets/synthetic-terminal.html`
- Modify: `desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs`

**Interfaces:**
- Produces: `SyntheticBasket.BidPrice`, `SyntheticBasket.AskPrice`, `SyntheticBasket.LastPrice`.

- [ ] Add tests proving bid/ask/last equal sum of component prices multiplied by formula multipliers.
- [ ] Recalculate synthetic quotes on payload build and live update.
- [ ] Display synthetic bid/ask/last in the chart header and footer.
- [ ] Run tests.

### Task 4: Full-History And Chart Polish

**Files:**
- Modify: `desktop/CAPETF.Desktop/SyntheticTerminalSelector.cs`
- Modify: `desktop/CAPETF.Desktop/Assets/synthetic-terminal.html`
- Modify: `desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs`

**Interfaces:**
- Produces: full available chart history for selected timeframe while keeping strategy comparison bounded when needed.

- [ ] Stop trimming terminal chart candles to only three years after basket selection.
- [ ] Keep similarity/strategy comparisons bounded internally so old data does not dominate selection.
- [ ] Add chart controls/labels for bid/ask/last and go-to-realtime behavior.
- [ ] Run tests, Release build, publish direct executable folder.
