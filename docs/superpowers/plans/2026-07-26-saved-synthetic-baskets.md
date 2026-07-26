# Saved Synthetic Baskets Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make strategy selection graded instead of empty-on-strict-failure, and allow users to save/reload synthetic basket formulas locally.

**Architecture:** Strategy scoring stays in `SyntheticStrategyRanker` and returns best available candidates even when strict thresholds are not fully met. Saved baskets are stored as JSON under `%LOCALAPPDATA%\CAPETF\saved-synthetics.json`; loading a saved basket reuses saved component epics and current cached/API candles to rebuild the chart and formula.

**Tech Stack:** WPF/.NET 8, local JSON persistence, existing synthetic basket builder and Lightweight Charts host.

## Global Constraints

- Do not store API credentials or secrets in saved basket files.
- Saved formulas must preserve component epics, multipliers, and reference prices.
- Strategy fallback candidates must be penalized, not excluded, when strict matches are insufficient.
- Existing `Load Universe`, typed seed symbol, and strategy dropdown flows must remain available.

---

### Task 1: Graded Strategy Ranking

**Files:**
- Modify: `desktop/CAPETF.Desktop/SyntheticStrategy.cs`
- Modify: `desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs`

**Interfaces:**
- Consumes: `SyntheticStrategyRanker.Rank(...)`.
- Produces: ranked fallback candidates for every non-symbol strategy.

- [ ] Add tests that below-MA200 and below-2Y-low return closest fallback candidates when no strict candidates exist.
- [ ] Change strategy scoring to return lower positive scores for near misses instead of `null`.
- [ ] Run tests.

### Task 2: Saved Basket Store

**Files:**
- Create: `desktop/CAPETF.Desktop/SavedSyntheticBasketStore.cs`
- Modify: `desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs`

**Interfaces:**
- Produces: `SavedSyntheticBasket`, `SavedSyntheticComponent`, `SavedSyntheticBasketStore.LoadAll()`, `Save(...)`.

- [ ] Add tests that saving persists name, strategy, block, epics, multipliers, and reference prices.
- [ ] Implement DPAPI-free local JSON persistence under `%LOCALAPPDATA%\CAPETF`.
- [ ] Run tests.

### Task 3: Terminal UI Wiring

**Files:**
- Modify: `desktop/CAPETF.Desktop/CapComTerminalWindow.xaml`
- Modify: `desktop/CAPETF.Desktop/CapComTerminalWindow.xaml.cs`
- Modify: `desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs`

**Interfaces:**
- Consumes: saved basket store and current `_basket`.
- Produces: `Save Basket` button and `SavedBasketsBox` dropdown.

- [ ] Add `Save Basket` and saved basket dropdown.
- [ ] Save current basket with generated editable-free name.
- [ ] Loading a saved basket rebuilds selected epics from current candles.
- [ ] Run tests, Release build, publish direct executable folder.
