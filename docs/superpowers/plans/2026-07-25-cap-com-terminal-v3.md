# cap.com Terminal V3 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the static native Canvas chart in `cap.com Terminal` with a full-screen interactive synthetic trading chart.

**Architecture:** WPF remains the native shell for credentials, Capital.com data, synthetic basket selection, streaming, and order preview. WebView2 hosts a local HTML terminal built on the bundled TradingView Lightweight Charts asset. WPF sends full chart payloads and live updates into JavaScript.

**Tech Stack:** .NET 8 WPF, Microsoft WebView2, bundled `lightweight-charts.standalone.production.js`, existing Capital.com API and synthetic basket services.

## Global Constraints

- Keep existing Capital.com API, stock universe loading, synthetic basket selection, and streaming logic.
- Replace WPF Canvas as the primary chart surface.
- Do not reintroduce StockSharp or DevExpress.
- Keep the app title `cap.com Terminal`.
- Keep Nike sample available and usable.
- Do not add live order execution in this pass.
- Use packaged chart assets only; no CDN dependency.

---

## File Structure

- Modify `desktop/CAPETF.Desktop/Assets/synthetic-terminal.html`: make it the V3 Lightweight Charts terminal with interactive chart functions.
- Modify `desktop/CAPETF.Desktop/CapComTerminalWindow.xaml`: replace Canvas chart area with WebView2 and compact terminal controls.
- Modify `desktop/CAPETF.Desktop/CapComTerminalWindow.xaml.cs`: initialize WebView2, send payloads, route chart mode/timeframe/fit/order commands, and stream incremental chart updates.
- Modify `desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs`: update tests to enforce V3 Lightweight Charts functions and absence of static Canvas primary rendering.
- Keep `desktop/CAPETF.Desktop/SyntheticTerminalChartPayload.cs`: existing payload is sufficient for candles, MAs, and components.

---

### Task 1: Add V3 HTML Contract Tests

**Files:**
- Modify: `desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs`
- Test: `desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs`

**Interfaces:**
- Consumes: `Assets/synthetic-terminal.html`
- Produces: tests that require `window.renderTerminal`, `window.updateTerminal`, `window.setTerminalChartMode`, `window.fitTerminalChart`, `LightweightCharts.createChart`, `CandlestickSeries`, `AreaSeries`, `heikin`, `subscribeCrosshairMove`, `priceScale`, and `timeScale`

- [ ] **Step 1: Add a failing test method**

Add a method that reads `Assets/synthetic-terminal.html` and checks for the V3 chart contract:

```csharp
private static void SyntheticTerminalHtmlUsesV3LightweightChartsTerminal()
{
    var path = Path.Combine(AppContext.BaseDirectory, "Assets", "synthetic-terminal.html");
    if (!File.Exists(path)) throw new Exception("terminal chart HTML must be copied to output");
    var html = File.ReadAllText(path);
    foreach (var required in new[]
    {
        "lightweight-charts.standalone.production.js",
        "LightweightCharts.createChart",
        "CandlestickSeries",
        "LineSeries",
        "window.renderTerminal",
        "window.updateTerminal",
        "window.setTerminalChartMode",
        "window.setTerminalInterval",
        "window.fitTerminalChart",
        "window.placeSyntheticPreviewOrder",
        "subscribeCrosshairMove",
        "timeScale",
        "priceScale",
        "heikin"
    })
    {
        if (!html.Contains(required, StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception($"terminal V3 HTML missing expected control {required}");
        }
    }
}
```

- [ ] **Step 2: Add the method to `RunAll()`**

Call `SyntheticTerminalHtmlUsesV3LightweightChartsTerminal();` after the existing terminal HTML tests.

- [ ] **Step 3: Run tests and verify failure**

Run: `dotnet run --project desktop\CAPETF.Desktop.Tests\CAPETF.Desktop.Tests.csproj`

Expected: FAIL because the current HTML uses KLineChart instead of the V3 Lightweight Charts terminal.

---

### Task 2: Replace HTML Terminal With Interactive Lightweight Charts

**Files:**
- Modify: `desktop/CAPETF.Desktop/Assets/synthetic-terminal.html`
- Test: `desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs`

**Interfaces:**
- Consumes: `SyntheticTerminalPayload` JSON with `Symbol`, `Block`, `CurrencyLabel`, `Candles`, `Ma20`, `Ma50`, `Ma200`, and `Components`
- Produces JavaScript functions:
  - `window.renderTerminal(payload)`
  - `window.updateTerminal(payload)`
  - `window.clearTerminal()`
  - `window.resizeTerminal()`
  - `window.fitTerminalChart()`
  - `window.setTerminalChartMode(mode)`
  - `window.setTerminalInterval(interval)`
  - `window.toggleTerminalMa(period, visible)`
  - `window.toggleTerminalComponents()`
  - `window.placeSyntheticPreviewOrder(side, quantity)`

- [ ] **Step 1: Replace the HTML body with a full-window chart layout**

Use a compact header, chart root, bottom OHLC/status strip, and collapsible component drawer. The chart root must consume the available window height.

- [ ] **Step 2: Load the packaged Lightweight Charts asset**

Use:

```html
<script src="lightweight-charts.standalone.production.js"></script>
```

- [ ] **Step 3: Initialize an interactive chart**

Use `LightweightCharts.createChart(chartRoot, options)` with dark layout, visible time scale, right price scale, crosshair, and `handleScroll` / `handleScale` enabled.

- [ ] **Step 4: Render normal and Heikin Ashi candles**

Convert payload candles from Unix seconds to business/date-time chart data. Compute Heikin Ashi in JavaScript and switch by calling `window.setTerminalChartMode('heikin')`.

- [ ] **Step 5: Render MA 20 / 50 / 200 overlays**

Use `LineSeries` for MA overlays. Keep toggles functional through `window.toggleTerminalMa(period, visible)`.

- [ ] **Step 6: Add crosshair and OHLC footer updates**

Subscribe to crosshair move and update footer values for the pointed candle. If the pointer leaves the chart, show the latest candle.

- [ ] **Step 7: Add fit/reset and resize functions**

`window.fitTerminalChart()` calls `chart.timeScale().fitContent()`. `window.resizeTerminal()` calls `chart.resize(width, height)`.

- [ ] **Step 8: Run tests**

Run: `dotnet run --project desktop\CAPETF.Desktop.Tests\CAPETF.Desktop.Tests.csproj`

Expected: PASS for HTML contract tests.

---

### Task 3: Replace Native Canvas With WebView2 Host

**Files:**
- Modify: `desktop/CAPETF.Desktop/CapComTerminalWindow.xaml`
- Modify: `desktop/CAPETF.Desktop/CapComTerminalWindow.xaml.cs`
- Test: `desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs`

**Interfaces:**
- Consumes: `SyntheticTerminalChartPayload.Build(SyntheticBasket basket)`
- Produces:
  - `InitializeChartHostAsync()`
  - `SendTerminalPayloadAsync(SyntheticTerminalPayload payload, bool liveUpdate)`
  - `InvokeTerminalScriptAsync(string script)`
  - `SetTerminalChartModeAsync()`
  - `SetTerminalIntervalAsync()`
  - `FitTerminalChartAsync()`

- [ ] **Step 1: Update XAML**

Remove `NativeCandleCanvas` from the primary chart area. Add:

```xml
<wv2:WebView2 x:Name="TerminalWebView" DefaultBackgroundColor="#050914"/>
```

Add `xmlns:wv2="clr-namespace:Microsoft.Web.WebView2.Wpf;assembly=Microsoft.Web.WebView2.Wpf"` to the window.

- [ ] **Step 2: Keep compact top controls**

Keep seed search, connect, load stocks, build synthetic, Nike sample, stream, block, resolution, candle type, fit/reset, and ticket toggle in compact rows.

- [ ] **Step 3: Initialize WebView2**

Load `Assets/synthetic-terminal.html` from `AppContext.BaseDirectory`:

```csharp
var terminalPath = Path.Combine(AppContext.BaseDirectory, "Assets", "synthetic-terminal.html");
TerminalWebView.Source = new Uri(terminalPath);
```

- [ ] **Step 4: Send payloads as JSON**

Serialize `SyntheticTerminalPayload` with `System.Text.Json.JsonSerializer.Serialize(payload)` and call:

```csharp
await TerminalWebView.ExecuteScriptAsync($"window.renderTerminal({json});");
```

Use `window.updateTerminal` for streaming quote updates.

- [ ] **Step 5: Route chart controls**

Call JavaScript functions from selection/button handlers:

```csharp
await ExecuteScriptAsync($"window.setTerminalChartMode('{mode}');");
await ExecuteScriptAsync($"window.setTerminalInterval('{interval}');");
await ExecuteScriptAsync("window.fitTerminalChart();");
```

- [ ] **Step 6: Run tests and build**

Run:

```powershell
dotnet run --project desktop\CAPETF.Desktop.Tests\CAPETF.Desktop.Tests.csproj
dotnet build desktop\CAPETF.Desktop\CAPETF.Desktop.csproj -c Release
```

Expected: both PASS.

---

### Task 4: Verify Desktop Interaction And Package

**Files:**
- Modify only if verification finds defects.
- Output: `artifacts/CAPETF-Realtime-win-x64.zip`

**Interfaces:**
- Consumes: built Release app
- Produces: verified package

- [ ] **Step 1: Stop existing running app instances**

Close existing `CAPETF` processes before launch.

- [ ] **Step 2: Launch Release app**

Run the published or Release executable.

- [ ] **Step 3: Verify manually**

Check:

- Nike sample renders `SYN-NKE-01`.
- Chart is visible and full-screen-first.
- Mouse wheel zoom works.
- Drag pan works.
- Crosshair footer changes.
- Fit/reset works.
- Heikin Ashi changes candle shape.
- MA overlays show.
- Component/ticket drawer toggles.

- [ ] **Step 4: Rebuild ZIP**

Run the existing packaging script or publish command that creates `artifacts/CAPETF-Realtime-win-x64.zip`.

- [ ] **Step 5: Commit and push**

Commit implementation files and push to GitHub main after verification.

---

## Self-Review

- Spec coverage: chart replacement, Heikin Ashi, zoom/pan/timeline/crosshair, MA overlays, compact layout, Nike sample, streaming updates, and package verification are covered.
- Marker scan: no unfinished markers or undefined future work markers are used.
- Type consistency: JavaScript function names match the WPF calls listed in Task 3 and the HTML contract tests in Task 1.
