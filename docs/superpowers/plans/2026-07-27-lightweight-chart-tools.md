# Lightweight Chart Tools Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver a professional, persistent Lightweight Charts drawing workspace with percentage measurement and safe saved-basket deletion in the unzipped Windows app.

**Architecture:** Keep Lightweight Charts 5.2 and the current Capital.com/synthetic pipeline unchanged. Add a self-contained browser drawing module that owns geometry, interaction, rendering, persistence, and undo state; let the terminal HTML own controls and chart wiring. Add deletion to the existing C# saved-basket store and coordinate confirmation in WPF.

**Tech Stack:** .NET 8 WPF, WebView2, HTML/CSS/JavaScript, TradingView Lightweight Charts 5.2, local JSON persistence, custom C# console test harness.

## Global Constraints

- Runtime charting and drawing must work offline with no CDN dependency.
- Preserve Capital.com authentication, history, synthetic calculation, bid/ask, and streaming behavior.
- Measurement must show start/end price, absolute change, percentage change, bar count, and elapsed time.
- Drawings persist per stable synthetic basket identity.
- The published executable remains unzipped and directly runnable.

---

### Task 1: Saved Basket Deletion

**Files:**
- Modify: `desktop/CAPETF.Desktop/SavedSyntheticBasketStore.cs`
- Modify: `desktop/CAPETF.Desktop/CapComTerminalWindow.xaml`
- Modify: `desktop/CAPETF.Desktop/CapComTerminalWindow.xaml.cs`
- Test: `desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs`

**Interfaces:**
- Produces: `bool SavedSyntheticBasketStore.Delete(string id)`.
- Produces: `DeleteBasket_Click(object sender, RoutedEventArgs e)` and selected-state enablement.

- [ ] **Step 1: Add failing store and UI contract tests**

Add a test that saves two baskets, deletes one by ID, verifies only the other remains, and verifies an unknown ID returns `false`. Add markup/source assertions for a disabled-until-selected delete button, click handler, named confirmation, and list refresh.

- [ ] **Step 2: Run the test harness and verify the new tests fail**

Run: `dotnet run --project desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj -c Release`

- [ ] **Step 3: Implement deletion and WPF coordination**

Implement atomic logical deletion by loading, filtering case-insensitively by ID, writing only when a record was removed, and returning whether deletion occurred. Add a compact trash button next to `SavedBasketsBox`; confirm with `MessageBox`, delete, refresh, and leave `_currentBasket` unchanged.

- [ ] **Step 4: Run the test harness and commit**

Run the Release test harness and commit as `feat: add saved basket deletion`.

### Task 2: Drawing Model And Measurement Math

**Files:**
- Create: `desktop/CAPETF.Desktop/Assets/synthetic-drawings.js`
- Modify: `desktop/CAPETF.Desktop/CAPETF.Desktop.csproj`
- Test: `desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs`

**Interfaces:**
- Produces: `window.CapComDrawings.createManager(options)`.
- Manager methods: `setTool`, `setRecords`, `getRecords`, `undo`, `redo`, `deleteSelected`, `clear`, `setLocked`, `setVisible`, `setMagnet`, `setStyle`, `dispose`.
- Measurement record shape: `{ type: 'measure', p1, p2, style }` with computed `priceDelta`, `percentDelta`, `bars`, and `elapsedMs`.

- [ ] **Step 1: Add failing asset/API/measurement tests**

Assert the project publishes `synthetic-drawings.js`, the module exposes the manager contract, percentage uses `(end - start) / start * 100`, and a zero start price returns a non-numeric percentage label instead of infinity.

- [ ] **Step 2: Run tests and verify failure**

Run the Release test harness.

- [ ] **Step 3: Implement the focused drawing module**

Adapt the v5 primitive/view interaction pattern from `deepentropy/lightweight-charts-drawing` for CAPETF's required tools. Implement coordinate conversion, canvas rendering, hit testing, anchor dragging, whole-object dragging, selection handles, measure calculations, bounded undo/redo snapshots, record validation, and JSON-safe serialization. Preserve TradingView attribution.

- [ ] **Step 4: Run tests and commit**

Run the Release test harness and commit as `feat: add lightweight chart drawing manager`.

### Task 3: Professional Drawing Workspace UI

**Files:**
- Modify: `desktop/CAPETF.Desktop/Assets/synthetic-terminal.html`
- Test: `desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs`

**Interfaces:**
- Consumes: `window.CapComDrawings.createManager(options)` from Task 2.
- Produces: icon toolbar controls with stable `data-tool` values and keyboard bindings.

- [ ] **Step 1: Replace legacy toolbar assertions with failing workspace tests**

Assert the legacy abbreviated strip is absent. Assert controls for cursor, trend, ray, horizontal, vertical, Fibonacci, rectangle, brush, text, measure, undo, redo, magnet, lock, visibility, and clear exist with tooltips. Assert `Escape` and `Delete` bindings and a contextual color/width/style editor.

- [ ] **Step 2: Run tests and verify failure**

Run the Release test harness.

- [ ] **Step 3: Build the toolbar and wire the manager**

Create a fixed-width left rail that does not cover the price scale or chart data. Use locally embedded familiar line icons, stable button dimensions, accessible titles, selected/disabled states, and restrained dark styling. Route chart pointer and keyboard events through the manager, show text-entry UI only for text annotations, and require confirmation before clearing all drawings.

- [ ] **Step 4: Preserve per-basket records and style state**

Replace the previous primitive helpers with manager-backed load/save using the existing `capcom-terminal-drawings:` identity. Skip malformed legacy records and keep market-data updates independent of drawing persistence.

- [ ] **Step 5: Run tests and commit**

Run the Release test harness and commit as `feat: add professional chart drawing workspace`.

### Task 4: Publish And End-To-End Verification

**Files:**
- Modify only if defects are found in Tasks 1-3 files.
- Output: `desktop/publish/cap.com-terminal-v4-complete/CAPETF.exe`

**Interfaces:**
- Consumes all prior tasks.
- Produces the directly runnable unzipped Windows application and screenshots.

- [ ] **Step 1: Run full automated verification**

Run the Release test harness, `dotnet build desktop/CAPETF.Desktop/CAPETF.Desktop.csproj -c Release`, and publish for `win-x64` to the existing complete output directory.

- [ ] **Step 2: Exercise the published app**

Launch the published executable, connect with saved demo credentials, build a synthetic basket, verify candles and bid/ask, add and edit a trend line and percentage measure, test undo/redo, reload the basket to verify drawings persist, and delete a saved basket after confirmation.

- [ ] **Step 3: Inspect screenshots and layout**

Capture the full terminal and verify the chart is nonblank, labels do not overlap, the left rail is compact, the measure text is readable, handles are visible, and the formula panel splitter still works.

- [ ] **Step 4: Fix any discovered defects and rerun verification**

Repeat the relevant automated and manual checks after every fix.

- [ ] **Step 5: Commit final verification fixes**

Commit only if defects required code changes, then report the executable and screenshot paths.
