# Synthetic Close Icon and MA Markers Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a safeguarded close icon to active synthetic basket rows and remove moving-average crosshair dots.

**Architecture:** Extend the existing trade-dock table renderer with an action-cell formatter that invokes `requestCloseBasket(record)`. Reuse the current close-confirmation state machine and disable MA crosshair markers through Lightweight Charts series options.

**Tech Stack:** .NET 8 WPF, WebView2, JavaScript, TradingView Lightweight Charts 5.2.0, Node-backed runtime tests.

## Global Constraints

- The close icon must never post a mutation directly.
- Existing acknowledgement and demo-only close safeguards must remain unchanged.
- MA lines, candle crosshair, and drawing tools must remain available.

---

### Task 1: Runtime Regression Tests

**Files:**
- Modify: `desktop/CAPETF.Desktop.Tests/SyntheticTradingTests.cs`

**Interfaces:**
- Consumes: existing synthetic trade-dock runtime harness.
- Produces: assertions for `close-synthetic-<executionId>` and `crosshairMarkerVisible: false`.

- [ ] **Step 1: Write failing assertions**

Assert that an active synthetic row contains an icon-only close button, clicking it opens `close-confirmation` without posting a message, and all MA series disable crosshair markers.

- [ ] **Step 2: Run the trading suite and verify RED**

Run: `dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj trading`

Expected: failure because the row action and MA options do not exist.

### Task 2: Minimal Terminal Implementation

**Files:**
- Modify: `desktop/CAPETF.Desktop/Assets/synthetic-terminal.html`

**Interfaces:**
- Consumes: `requestCloseBasket(record)`, `refreshTradingControls()`, and `dockViews.baskets`.
- Produces: `renderSyntheticCloseAction(row)` and dot-free MA series.

- [ ] **Step 1: Add the action-cell formatter**

Create an icon button with `data-close-execution`, accessible label, tooltip, and a click handler that stops propagation and calls `requestCloseBasket(row.record)`.

- [ ] **Step 2: Include the backing record in synthetic rows**

Keep the host-owned execution record on the in-memory row object and add an Actions column using the formatter.

- [ ] **Step 3: Disable MA crosshair markers**

Set `crosshairMarkerVisible: false` on MA20, MA50, and MA200 series options.

- [ ] **Step 4: Run the trading and full suites**

Run the trading suite, then the unfiltered test project. Both must exit 0.

- [ ] **Step 5: Publish and verify**

Publish self-contained `win-x64` output to `desktop/publish/cap.com-terminal-v4-five-lots`, confirm no ZIP exists, launch the app, and inspect the bottom tab and chart behavior.

- [ ] **Step 6: Commit and push**

Commit the implementation and push `feature/cap-com-terminal-v4` to `origin`.
