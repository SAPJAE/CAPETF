# Synthetic Lots Trading Workspace Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver a stable synthetic-lot order ticket, selected-instrument right rail, account-wide bottom activity workspace, draggable synthetic SL/TP plans, and progressive cached universe loading.

**Architecture:** Treat the displayed formula as the immutable one-lot execution unit and pass integer synthetic lot counts through browser requests, margin/preflight services, tickets, and execution records. Keep the HTML workspace mounted and publish granular state updates; use the existing WebView2 host bridge for activity events, risk plans, and staged universe batches.

**Tech Stack:** .NET 8 WPF, WebView2, Capital.com REST/streaming API, TradingView Lightweight Charts 5.2, plain HTML/CSS/JavaScript, custom C# test runner.

## Global Constraints

- One complete displayed formula equals one synthetic lot.
- Synthetic lot input defaults to `1` and accepts positive whole numbers only.
- Background refresh must never steal focus or disable the quantity input.
- The right rail is selected-instrument context only; account-wide state belongs in the bottom dock.
- Risk lines are visual synthetic plans, not Capital.com broker stops.
- Never submit an order without the existing confirmation and partial-execution acknowledgement.

---

### Task 1: Synthetic Lot Contract

**Files:**
- Modify: `desktop/CAPETF.Desktop/SyntheticOrderSizing.cs`
- Modify: `desktop/CAPETF.Desktop/SyntheticTradeModels.cs`
- Modify: `desktop/CAPETF.Desktop/SyntheticTradingHostCoordinator.cs`
- Modify: `desktop/CAPETF.Desktop/CapComTerminalWindow.xaml.cs`
- Modify: `desktop/CAPETF.Desktop/Assets/synthetic-terminal.html`
- Test: `desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs`
- Test: `desktop/CAPETF.Desktop.Tests/SyntheticTradingTests.cs`

**Interfaces:**
- Consumes: `SyntheticBasket.Components[].FormulaMultiplier` as the exact one-lot leg quantity.
- Produces: positive integer `SyntheticLots` requests and formula-preserving executable previews.

- [x] Write failing tests proving 1 and 3 synthetic lots multiply every formula leg exactly, invalid fractional/zero lots are rejected, and browser requests parse `syntheticLots` only.
- [x] Run the focused desktop test project and verify the new assertions fail for the old notional behavior.
- [x] Implement integer lot validation and formula-preserving sizing for every synthetic strategy.
- [x] Rename UI and host request semantics from basket notional/quantity to synthetic lots while retaining persistence compatibility for existing execution records.
- [x] Run focused tests and commit the lot-contract change.

### Task 2: Stable Ticket And Margin Feedback

**Files:**
- Modify: `desktop/CAPETF.Desktop/Assets/synthetic-terminal.html`
- Modify: `desktop/CAPETF.Desktop/CapComTerminalWindow.xaml.cs`
- Test: `desktop/CAPETF.Desktop.Tests/SyntheticTradingTests.cs`

**Interfaces:**
- Consumes: integer synthetic lots and existing `SyntheticMarginPreviewSummary` publications.
- Produces: non-flickering ticket state and side-specific affordability feedback.

- [x] Write failing source/UI behavior tests proving background busy state does not disable the quantity input, the input defaults to 1/step 1, previous margin values remain visible while refreshing, and guarded Buy/Sell clicks always publish feedback.
- [x] Run tests and verify failure against the current `quantity.disabled = busy` behavior.
- [x] Split background activity from mutation lock, debounce margin refresh, preserve last valid values, and add an inline progress/error region.
- [x] Render basket price, estimated notional, required margin, available margin, and remaining Buy/Sell margin directly below the action buttons.
- [x] Run focused tests and commit the stable-ticket change.

### Task 3: Context Rail And Activity Dock

**Files:**
- Create: `desktop/CAPETF.Desktop/TerminalActivityLog.cs`
- Modify: `desktop/CAPETF.Desktop/CapComTerminalWindow.xaml.cs`
- Modify: `desktop/CAPETF.Desktop/Assets/synthetic-terminal.html`
- Test: `desktop/CAPETF.Desktop.Tests/SyntheticTradingTests.cs`

**Interfaces:**
- Produces: `TerminalActivityEvent` publications with timestamp, severity, operation, summary, and detail.
- Persists: `%LocalAppData%/CAPETF/terminal-activity.json`.

- [ ] Write failing tests for bounded persistent log append/load/clear and for the Activity Log bottom tab.
- [ ] Implement the activity store and host publication hooks for connection, API, universe, margin, preflight, execution, close, and failure paths.
- [ ] Remove account-wide positions, orders, baskets, execution stream, and audit sections from the right rail.
- [ ] Add bottom Activity Log tab with severity filters, Clear, and Export controls; keep other bottom tabs synchronized with execution state.
- [ ] Run focused tests and commit the workspace cleanup.

### Task 4: Draggable Synthetic Risk Lines

**Files:**
- Modify: `desktop/CAPETF.Desktop/Assets/synthetic-terminal.html`
- Modify: `desktop/CAPETF.Desktop/SyntheticRiskPlan.cs`
- Test: `desktop/CAPETF.Desktop.Tests/SyntheticTradingTests.cs`

**Interfaces:**
- Consumes: selected active execution entry/direction and existing `setRiskPlan` host request.
- Produces: add-risk control plus draggable planned SL/TP overlays persisted on pointer release.

- [ ] Write failing tests for Buy/Sell surrounding rules, risk-line creation defaults, drag publication, and selection isolation.
- [ ] Implement a chart overlay with an entry-adjacent plus button and HTML drag handles synchronized to chart price coordinates.
- [ ] Update planned lines live during drag and send one revisioned `setRiskPlan` request on pointer release.
- [ ] Keep numeric bottom-dock editors synchronized as an accessible fallback and show validation without moving the chart.
- [ ] Run focused tests and commit the chart-risk interaction.

### Task 5: Progressive Universe Accumulator

**Files:**
- Create: `desktop/CAPETF.Desktop/TerminalUniverseAccumulator.cs`
- Modify: `desktop/CAPETF.Desktop/CapComTerminalWindow.xaml.cs`
- Test: `desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs`

**Interfaces:**
- Consumes: local dashboard/cache instruments and Capital.com market discovery results.
- Produces: deduplicated incremental universe snapshots keyed by epic without resetting current selection.

- [ ] Write failing tests for immediate cached publication, deterministic merge/deduplication, selection preservation, and staged progress.
- [ ] Implement the accumulator and local merged-universe cache.
- [ ] Change Connect to publish cache first, then discover and merge API batches in the background with cancellation and rate control.
- [ ] Publish progress/activity without blocking chart, search, or order controls.
- [ ] Run focused tests and commit the progressive-universe change.

### Task 6: End-To-End Verification And Publish

**Files:**
- Modify only files required by defects found during verification.

**Interfaces:**
- Produces: unzipped runnable `desktop/publish/cap.com-terminal-v4-five-lots/CAPETF.exe`.

- [ ] Run all desktop tests and verify zero failures.
- [ ] Build and publish the Windows executable.
- [ ] Launch the app, connect to Capital.com demo, verify cached universe appears before background completion, and capture screenshots of the ticket, margin, Activity Log, and draggable risk plan.
- [ ] Validate a 1-lot and multi-lot crypto basket through preflight without submitting live-account orders; verify exact leg multiplication and explicit insufficient-margin feedback.
- [ ] Inspect git diff/status, commit fixes, and push `feature/cap-com-terminal-v4`.
