# Full-Screen Synthetic Terminal Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a full-screen, real-time synthetic instrument terminal in the CAPETF Windows app.

**Architecture:** Keep the existing discovery dashboard intact and add a separate `Terminal` workspace that renders one selected synthetic basket only. Move terminal-specific selection, chart payload, and live update logic into focused services so `MainWindow.xaml.cs` does not become the terminal engine.

**Tech Stack:** C#/.NET 8 WPF, WebView2, TradingView Lightweight Charts local HTML, Capital.com REST prices, Capital.com streaming quotes, existing custom console test project.

## Global Constraints

- The terminal is a local Windows app feature only; no API keys or tokens go to GitHub Pages or any public page.
- TradingView Lightweight Charts runs inside local WebView2 and receives data from WPF.
- Terminal mode must not render stock grids, dashboard mini charts, or broad discovery stats.
- Subscribe only to the selected synthetic basket's 3 to 4 component epics in Terminal mode.
- Terminal auto-selection uses approximately three years of historical OHLC data.
- Terminal auto-selection ranks normalized chart-shape similarity and relatively similar individual volatility; materially different volatility lowers the rank.
- There is no intentional delay or polling in the live pricing path.
- MA 20, MA 50, and MA 200 must appear when enough candles exist.
- Buy/Sell controls are disabled or preview-only in this implementation.
- Known different currencies must not mix; blank Capital.com currency values use a separate fallback bucket inside the selected UI block.

---

## File Structure

- Create `desktop/CAPETF.Desktop/SyntheticTerminalModels.cs`
  - Holds chart DTOs sent to WebView2 and terminal state models.
- Create `desktop/CAPETF.Desktop/SyntheticTerminalSelector.cs`
  - Chooses the best available basket automatically from a block and candle cache using three-year chart-shape and volatility similarity.
- Create `desktop/CAPETF.Desktop/SyntheticTerminalChartPayload.cs`
  - Converts `SyntheticBasket` candles/components into chart payloads with MA lines.
- Create `desktop/CAPETF.Desktop/SyntheticTerminalLiveUpdate.cs`
  - Applies live component ticks to the terminal state and produces incremental chart updates.
- Create `desktop/CAPETF.Desktop/Assets/synthetic-terminal.html`
  - Full-screen TradingView Lightweight Charts host.
- Modify `desktop/CAPETF.Desktop/CAPETF.Desktop.csproj`
  - Package the new HTML asset.
- Modify `desktop/CAPETF.Desktop/MainWindow.xaml`
  - Add `Terminal` workspace mode with chart-first layout.
- Modify `desktop/CAPETF.Desktop/MainWindow.xaml.cs`
  - Wire terminal build, WebView2 chart rendering, and terminal-only streaming subscription.
- Modify `desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs`
  - Add terminal selector, chart payload, MA, and live update tests.
- Modify `desktop/CAPETF.Desktop.Tests/Program.cs`
  - Keep a single test entry point that runs all desktop tests.

---

### Task 1: Terminal Chart Payload and Moving Averages

**Files:**
- Create: `desktop/CAPETF.Desktop/SyntheticTerminalModels.cs`
- Create: `desktop/CAPETF.Desktop/SyntheticTerminalChartPayload.cs`
- Modify: `desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs`

**Interfaces:**
- Consumes: `SyntheticBasket`, `OhlcPoint`, `SyntheticComponent`
- Produces:
  - `public sealed record TerminalCandle(long Time, decimal Open, decimal High, decimal Low, decimal Close);`
  - `public sealed record TerminalLinePoint(long Time, decimal Value);`
  - `public sealed record TerminalComponentRow(string Name, string Epic, string Currency, decimal Weight, decimal? Bid, decimal? Offer, decimal? Last, string LastTickText);`
  - `public sealed record SyntheticTerminalPayload(string Symbol, string Block, string CurrencyLabel, IReadOnlyList<TerminalCandle> Candles, IReadOnlyList<TerminalLinePoint> Ma20, IReadOnlyList<TerminalLinePoint> Ma50, IReadOnlyList<TerminalLinePoint> Ma200, IReadOnlyList<TerminalComponentRow> Components);`
  - `public static class SyntheticTerminalChartPayload`
  - `public static SyntheticTerminalPayload Build(SyntheticBasket basket)`

- [ ] **Step 1: Write failing test for chart payload**

Add this test to `SyntheticBasketBuilderTests.RunAll()`:

```csharp
SyntheticTerminalPayloadIncludesCandlesComponentsCurrencyAndMas();
```

Add this method:

```csharp
private static void SyntheticTerminalPayloadIncludesCandlesComponentsCurrencyAndMas()
{
    var basket = new SyntheticBasket
    {
        Symbol = "SYN-US-01",
        Block = "US / USD / Technology",
        BasketPrice = 150m,
        LastUpdated = DateTimeOffset.Parse("2026-07-25T00:00:00Z")
    };

    basket.Components.Add(new SyntheticComponent(
        new MarketInstrument
        {
            Epic = "AAPL",
            Name = "Apple Inc",
            Type = "SHARES",
            Currency = "USD",
            Price = 200m,
            Bid = 199.9m,
            Offer = 200.1m,
            LastTickAt = DateTimeOffset.Parse("2026-07-25T00:00:00Z")
        },
        60m,
        20m,
        40m));
    basket.Components.Add(new SyntheticComponent(
        new MarketInstrument
        {
            Epic = "MSFT",
            Name = "Microsoft",
            Type = "SHARES",
            Currency = "USD",
            Price = 300m,
            Bid = 299.8m,
            Offer = 300.2m,
            LastTickAt = DateTimeOffset.Parse("2026-07-25T00:00:00Z")
        },
        40m,
        18m,
        35m));

    var start = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
    for (var index = 1; index <= 220; index++)
    {
        var close = 100m + index;
        basket.Candles.Add(new OhlcPoint(start.AddDays(index), close - 1m, close + 2m, close - 2m, close));
    }

    var payload = SyntheticTerminalChartPayload.Build(basket);

    if (payload.Symbol != "SYN-US-01") throw new Exception("terminal payload must include synthetic symbol");
    if (payload.CurrencyLabel != "USD") throw new Exception("matching known component currency must be displayed");
    if (payload.Candles.Count != 220) throw new Exception("terminal payload must include all synthetic candles");
    if (payload.Components.Count != 2) throw new Exception("terminal payload must include component rows");
    if (payload.Ma20.Count == 0 || payload.Ma50.Count == 0 || payload.Ma200.Count == 0)
    {
        throw new Exception("terminal payload must include MA 20, MA 50, and MA 200 when enough candles exist");
    }
    AssertNear(310.5m, payload.Ma20[^1].Value, "MA20 must average the last 20 closes");
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet run --project desktop\CAPETF.Desktop.Tests\CAPETF.Desktop.Tests.csproj
```

Expected: compile failure because `SyntheticTerminalChartPayload` and DTOs do not exist.

- [ ] **Step 3: Implement terminal payload types**

Create `SyntheticTerminalModels.cs`:

```csharp
namespace CAPETF.Desktop;

public sealed record TerminalCandle(long Time, decimal Open, decimal High, decimal Low, decimal Close);

public sealed record TerminalLinePoint(long Time, decimal Value);

public sealed record TerminalComponentRow(
    string Name,
    string Epic,
    string Currency,
    decimal Weight,
    decimal? Bid,
    decimal? Offer,
    decimal? Last,
    string LastTickText);

public sealed record SyntheticTerminalPayload(
    string Symbol,
    string Block,
    string CurrencyLabel,
    IReadOnlyList<TerminalCandle> Candles,
    IReadOnlyList<TerminalLinePoint> Ma20,
    IReadOnlyList<TerminalLinePoint> Ma50,
    IReadOnlyList<TerminalLinePoint> Ma200,
    IReadOnlyList<TerminalComponentRow> Components);
```

- [ ] **Step 4: Implement payload builder**

Create `SyntheticTerminalChartPayload.cs`:

```csharp
namespace CAPETF.Desktop;

public static class SyntheticTerminalChartPayload
{
    public static SyntheticTerminalPayload Build(SyntheticBasket basket)
    {
        var candles = basket.Candles
            .OrderBy(candle => candle.Time)
            .Select(candle => new TerminalCandle(
                candle.Time.ToUnixTimeSeconds(),
                candle.Open,
                candle.High,
                candle.Low,
                candle.Close))
            .ToList();

        return new SyntheticTerminalPayload(
            basket.Symbol,
            basket.Block,
            CurrencyLabel(basket),
            candles,
            MovingAverage(basket.Candles, 20),
            MovingAverage(basket.Candles, 50),
            MovingAverage(basket.Candles, 200),
            basket.Components.Select(component => new TerminalComponentRow(
                component.Instrument.Name,
                component.Instrument.Epic,
                string.IsNullOrWhiteSpace(component.Instrument.Currency) ? "n/a" : component.Instrument.Currency,
                component.Weight,
                component.Instrument.Bid,
                component.Instrument.Offer,
                component.DisplayPrice,
                component.Instrument.LastTickAt?.ToLocalTime().ToString("HH:mm:ss") ?? "n/a")).ToList());
    }

    private static IReadOnlyList<TerminalLinePoint> MovingAverage(IReadOnlyList<OhlcPoint> source, int period)
    {
        var ordered = source.OrderBy(candle => candle.Time).ToList();
        if (ordered.Count < period) return [];
        var result = new List<TerminalLinePoint>();
        for (var index = period - 1; index < ordered.Count; index++)
        {
            var average = ordered.Skip(index - period + 1).Take(period).Average(candle => candle.Close);
            result.Add(new TerminalLinePoint(ordered[index].Time.ToUnixTimeSeconds(), decimal.Round(average, 6)));
        }
        return result;
    }

    private static string CurrencyLabel(SyntheticBasket basket)
    {
        var known = basket.Components
            .Select(component => component.Instrument.Currency)
            .Where(currency => !string.IsNullOrWhiteSpace(currency))
            .Select(currency => currency.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return known.Count == 1 ? known[0] : "currency unavailable from Capital.com";
    }
}
```

- [ ] **Step 5: Run test to verify it passes**

Run:

```powershell
dotnet run --project desktop\CAPETF.Desktop.Tests\CAPETF.Desktop.Tests.csproj
```

Expected: `SyntheticBasketBuilder tests passed`.

- [ ] **Step 6: Commit**

```powershell
git add desktop/CAPETF.Desktop/SyntheticTerminalModels.cs desktop/CAPETF.Desktop/SyntheticTerminalChartPayload.cs desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs
git commit -m "Add synthetic terminal chart payload"
```

---

### Task 2: Automatic Terminal Basket Selection

**Files:**
- Create: `desktop/CAPETF.Desktop/SyntheticTerminalSelector.cs`
- Modify: `desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs`

**Interfaces:**
- Consumes: `SyntheticBasketBuilder.Build(...)`
- Produces:
  - `public static class SyntheticTerminalSelector`
  - `public static SyntheticBasket? SelectBest(string block, IReadOnlyList<MarketInstrument> instruments, IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>> candles, int periodsPerYear)`
  - `public static IReadOnlyList<OhlcPoint> LastThreeYears(IReadOnlyList<OhlcPoint> candles)`

- [ ] **Step 1: Write failing test for automatic selection**

Add this call to `RunAll()`:

```csharp
SyntheticTerminalSelectorChoosesHighestSimilarityBasket();
SyntheticTerminalSelectorUsesThreeYearComparisonWindow();
SyntheticTerminalSelectorPenalizesVolatilityMismatch();
```

Add this method:

```csharp
private static void SyntheticTerminalSelectorChoosesHighestSimilarityBasket()
{
    var day = DateTimeOffset.Parse("2022-01-01T00:00:00Z");
    var instruments = new[]
    {
        CreateStock("BAD", "Bad"),
        CreateStock("GOOD-1", "Good 1"),
        CreateStock("GOOD-2", "Good 2"),
        CreateStock("GOOD-3", "Good 3"),
        CreateStock("GOOD-4", "Good 4"),
    };
    var goodReturns = new[] { 0.02m, -0.01m, 0.015m, -0.005m, 0.012m };
    var badReturns = goodReturns.Select(value => -value * 2m).ToArray();
    var candles = instruments.ToDictionary(
        instrument => instrument.Epic,
        instrument => CreateReturnCandles(day, instrument.Epic == "BAD" ? badReturns : goodReturns));

    var selected = SyntheticTerminalSelector.SelectBest("US / USD / Tech", instruments, candles, 52);
    if (selected is null) throw new Exception("terminal selector must return a valid basket");
    if (selected.Components.Any(component => component.Instrument.Epic == "BAD"))
    {
        throw new Exception("terminal selector must choose the highest similarity basket");
    }
}
```

Add this method:

```csharp
private static void SyntheticTerminalSelectorUsesThreeYearComparisonWindow()
{
    var start = DateTimeOffset.Parse("2020-01-01T00:00:00Z");
    var candles = Enumerable.Range(0, 260)
        .Select(index =>
        {
            var time = start.AddDays(index * 7);
            var close = 100m + index;
            return new OhlcPoint(time, close, close, close, close);
        })
        .ToList();

    var trimmed = SyntheticTerminalSelector.LastThreeYears(candles);

    if (trimmed.Count > 158 || trimmed.Count < 150)
    {
        throw new Exception("terminal selector must compare approximately the last three years of weekly candles");
    }
    if (trimmed[0].Time <= candles[0].Time)
    {
        throw new Exception("terminal selector must trim older history outside the three-year comparison window");
    }
}
```

Add this method:

```csharp
private static void SyntheticTerminalSelectorPenalizesVolatilityMismatch()
{
    var day = DateTimeOffset.Parse("2022-01-01T00:00:00Z");
    var instruments = new[]
    {
        CreateStock("CALM-1", "Calm 1"),
        CreateStock("CALM-2", "Calm 2"),
        CreateStock("CALM-3", "Calm 3"),
        CreateStock("CALM-4", "Calm 4"),
        CreateStock("WILD", "Wild")
    };

    var calmReturns = new[] { 0.01m, -0.005m, 0.008m, -0.004m, 0.006m };
    var wildReturns = new[] { 0.08m, -0.05m, 0.075m, -0.045m, 0.065m };
    var candles = instruments.ToDictionary(
        instrument => instrument.Epic,
        instrument => CreateReturnCandles(day, instrument.Epic == "WILD" ? wildReturns : calmReturns));

    var selected = SyntheticTerminalSelector.SelectBest("US / USD / Tech", instruments, candles, 52);
    if (selected is null) throw new Exception("terminal selector must return a valid basket");
    if (selected.Components.Any(component => component.Instrument.Epic == "WILD"))
    {
        throw new Exception("terminal selector must penalize materially different component volatility");
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet run --project desktop\CAPETF.Desktop.Tests\CAPETF.Desktop.Tests.csproj
```

Expected: compile failure because `SyntheticTerminalSelector` does not exist.

- [ ] **Step 3: Implement selector**

Create `SyntheticTerminalSelector.cs`:

```csharp
namespace CAPETF.Desktop;

public static class SyntheticTerminalSelector
{
    private const int WeeklyThreeYearCandles = 156;

    public static SyntheticBasket? SelectBest(
        string block,
        IReadOnlyList<MarketInstrument> instruments,
        IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>> candles,
        int periodsPerYear)
    {
        var terminalCandles = candles.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<OhlcPoint>)LastThreeYears(pair.Value));
        var result = SyntheticBasketBuilder.Build(
            block,
            instruments.Where(item => item.Group == block || string.Equals(block, item.Group, StringComparison.OrdinalIgnoreCase)).ToList(),
            terminalCandles,
            maxBaskets: 12,
            periodsPerYear: periodsPerYear);

        return result.Baskets
            .Where(basket => basket.Candles.Count >= 2 && basket.Components.Count is >= 3 and <= 4)
            .OrderByDescending(basket => basket.SimilarityScore)
            .ThenBy(basket => basket.Components.Max(component => component.AnnualizedVolatilityPct) - basket.Components.Min(component => component.AnnualizedVolatilityPct))
            .ThenByDescending(basket => basket.Candles.Count)
            .ThenBy(basket => basket.Symbol, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    public static IReadOnlyList<OhlcPoint> LastThreeYears(IReadOnlyList<OhlcPoint> candles) =>
        candles.OrderBy(candle => candle.Time).TakeLast(WeeklyThreeYearCandles).ToList();
}
```

- [ ] **Step 4: Run test to verify it passes**

Run:

```powershell
dotnet run --project desktop\CAPETF.Desktop.Tests\CAPETF.Desktop.Tests.csproj
```

Expected: `SyntheticBasketBuilder tests passed`.

- [ ] **Step 5: Commit**

```powershell
git add desktop/CAPETF.Desktop/SyntheticTerminalSelector.cs desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs
git commit -m "Add automatic synthetic terminal selection"
```

---

### Task 3: Terminal Live Tick Update

**Files:**
- Create: `desktop/CAPETF.Desktop/SyntheticTerminalLiveUpdate.cs`
- Modify: `desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs`

**Interfaces:**
- Consumes: `SyntheticLiveUpdate.ApplyQuote(...)`, `SyntheticTerminalChartPayload.Build(...)`
- Produces:
  - `public sealed record SyntheticTerminalTickResult(bool Matched, bool CandleChanged, SyntheticTerminalPayload? Payload);`
  - `public static class SyntheticTerminalLiveUpdate`
  - `public static SyntheticTerminalTickResult Apply(SyntheticBasket basket, QuoteUpdate quote)`

- [ ] **Step 1: Write failing test for terminal live update**

Add this call to `RunAll()`:

```csharp
SyntheticTerminalLiveUpdateReturnsPayloadImmediately();
```

Add this method:

```csharp
private static void SyntheticTerminalLiveUpdateReturnsPayloadImmediately()
{
    var basket = CreateLiveBasket("SYN-LIVE", "LIVE-A", 10m, DateTimeOffset.Parse("2026-07-25T00:00:00Z"));
    basket.Block = "US / USD / Technology";

    var result = SyntheticTerminalLiveUpdate.Apply(
        basket,
        new QuoteUpdate("LIVE-A", 12m, 12.2m, 12m, DateTimeOffset.Parse("2026-07-25T00:01:00Z")));

    if (!result.Matched) throw new Exception("terminal live update must report matching component ticks");
    if (!result.CandleChanged) throw new Exception("terminal live update must update the current synthetic candle");
    if (result.Payload is null) throw new Exception("terminal live update must return a fresh chart payload");
    AssertNear(12m, result.Payload.Candles[^1].Close, "terminal payload must contain the updated synthetic close");
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet run --project desktop\CAPETF.Desktop.Tests\CAPETF.Desktop.Tests.csproj
```

Expected: compile failure because `SyntheticTerminalLiveUpdate` does not exist.

- [ ] **Step 3: Implement terminal live update**

Create `SyntheticTerminalLiveUpdate.cs`:

```csharp
namespace CAPETF.Desktop;

public sealed record SyntheticTerminalTickResult(bool Matched, bool CandleChanged, SyntheticTerminalPayload? Payload);

public static class SyntheticTerminalLiveUpdate
{
    public static SyntheticTerminalTickResult Apply(SyntheticBasket basket, QuoteUpdate quote)
    {
        var result = SyntheticLiveUpdate.ApplyQuote(basket, quote);
        return new SyntheticTerminalTickResult(
            result.Matched,
            result.CandleChanged,
            result.Matched ? SyntheticTerminalChartPayload.Build(basket) : null);
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run:

```powershell
dotnet run --project desktop\CAPETF.Desktop.Tests\CAPETF.Desktop.Tests.csproj
```

Expected: `SyntheticBasketBuilder tests passed`.

- [ ] **Step 5: Commit**

```powershell
git add desktop/CAPETF.Desktop/SyntheticTerminalLiveUpdate.cs desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs
git commit -m "Add synthetic terminal live update payload"
```

---

### Task 4: Full-Screen TradingView Terminal Host

**Files:**
- Create: `desktop/CAPETF.Desktop/Assets/synthetic-terminal.html`
- Modify: `desktop/CAPETF.Desktop/CAPETF.Desktop.csproj`

**Interfaces:**
- Consumes: `SyntheticTerminalPayload` JSON shape
- Produces JavaScript functions:
  - `window.renderTerminal(payload)`
  - `window.updateTerminal(payload)`
  - `window.clearTerminal()`

- [ ] **Step 1: Add static asset smoke test**

Add this method to `SyntheticBasketBuilderTests` and call it from `RunAll()`:

```csharp
private static void SyntheticTerminalHtmlExposesRequiredFunctions()
{
    var path = Path.Combine(AppContext.BaseDirectory, "Assets", "synthetic-terminal.html");
    if (!File.Exists(path)) throw new Exception("terminal chart HTML must be copied to output");
    var html = File.ReadAllText(path);
    foreach (var functionName in new[] { "window.renderTerminal", "window.updateTerminal", "window.clearTerminal" })
    {
        if (!html.Contains(functionName, StringComparison.Ordinal))
        {
            throw new Exception($"terminal chart HTML missing {functionName}");
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet run --project desktop\CAPETF.Desktop.Tests\CAPETF.Desktop.Tests.csproj
```

Expected: failure because `synthetic-terminal.html` is missing from output.

- [ ] **Step 3: Create full-screen chart HTML**

Create `Assets/synthetic-terminal.html`:

```html
<!doctype html>
<html>
<head>
  <meta charset="utf-8">
  <meta http-equiv="Content-Security-Policy" content="default-src 'self' https://unpkg.com; script-src 'self' 'unsafe-inline' https://unpkg.com; style-src 'unsafe-inline';">
  <style>
    html, body { width: 100%; height: 100%; margin: 0; background: #050914; color: #d8e1f0; overflow: hidden; font-family: Segoe UI, Arial, sans-serif; }
    #terminal { display: grid; grid-template-rows: 42px 1fr 34px; width: 100%; height: 100%; }
    #top, #bottom { display: flex; align-items: center; gap: 16px; padding: 0 14px; background: #0b1220; border-color: #263246; white-space: nowrap; }
    #top { border-bottom: 1px solid #263246; }
    #bottom { border-top: 1px solid #263246; color: #94a3b8; }
    #body { display: grid; grid-template-columns: 1fr 280px; min-height: 0; }
    #chart { min-width: 0; min-height: 0; }
    #components { border-left: 1px solid #263246; background: #0b1220; padding: 10px; overflow: auto; }
    .component { border-bottom: 1px solid #263246; padding: 8px 0; }
    .muted { color: #94a3b8; }
    .strong { color: #f8fafc; font-weight: 600; }
  </style>
  <script src="https://unpkg.com/lightweight-charts@4.2.0/dist/lightweight-charts.standalone.production.js"></script>
</head>
<body>
  <div id="terminal">
    <div id="top"><span id="symbol" class="strong">No synthetic</span><span id="block" class="muted"></span><span id="currency" class="muted"></span></div>
    <div id="body"><div id="chart"></div><div id="components"></div></div>
    <div id="bottom"><span id="ohlc">OHLC n/a</span><span id="mas">MA n/a</span></div>
  </div>
  <script>
    const chartElement = document.getElementById('chart');
    const chart = LightweightCharts.createChart(chartElement, {
      layout: { background: { color: '#050914' }, textColor: '#cbd5e1' },
      grid: { vertLines: { color: '#111827' }, horzLines: { color: '#111827' } },
      rightPriceScale: { borderColor: '#334155' },
      timeScale: { borderColor: '#334155', timeVisible: true, secondsVisible: false },
      crosshair: { mode: LightweightCharts.CrosshairMode.Normal }
    });
    const candleSeries = chart.addCandlestickSeries({ upColor: '#22c55e', downColor: '#ef4444', borderUpColor: '#22c55e', borderDownColor: '#ef4444', wickUpColor: '#86efac', wickDownColor: '#fca5a5' });
    const ma20Series = chart.addLineSeries({ color: '#60a5fa', lineWidth: 1 });
    const ma50Series = chart.addLineSeries({ color: '#fbbf24', lineWidth: 1 });
    const ma200Series = chart.addLineSeries({ color: '#f472b6', lineWidth: 1 });

    function row(component) {
      return `<div class="component"><div class="strong">${component.Name}</div><div class="muted">${component.Epic}</div><div>${component.Weight.toFixed(2)}% | last ${component.Last ?? 'n/a'}</div><div class="muted">bid ${component.Bid ?? 'n/a'} | offer ${component.Offer ?? 'n/a'} | ${component.LastTickText}</div></div>`;
    }

    function apply(payload, fit) {
      document.getElementById('symbol').textContent = payload.Symbol || 'No synthetic';
      document.getElementById('block').textContent = payload.Block || '';
      document.getElementById('currency').textContent = payload.CurrencyLabel || '';
      document.getElementById('components').innerHTML = (payload.Components || []).map(row).join('');
      const candles = (payload.Candles || []).map(candle => ({
        time: candle.Time ?? candle.time,
        open: candle.Open ?? candle.open,
        high: candle.High ?? candle.high,
        low: candle.Low ?? candle.low,
        close: candle.Close ?? candle.close
      }));
      const ma20 = (payload.Ma20 || []).map(point => ({ time: point.Time ?? point.time, value: point.Value ?? point.value }));
      const ma50 = (payload.Ma50 || []).map(point => ({ time: point.Time ?? point.time, value: point.Value ?? point.value }));
      const ma200 = (payload.Ma200 || []).map(point => ({ time: point.Time ?? point.time, value: point.Value ?? point.value }));
      candleSeries.setData(candles);
      ma20Series.setData(ma20);
      ma50Series.setData(ma50);
      ma200Series.setData(ma200);
      const last = candles[candles.length - 1];
      document.getElementById('ohlc').textContent = last ? `O ${last.open ?? last.Open} H ${last.high ?? last.High} L ${last.low ?? last.Low} C ${last.close ?? last.Close}` : 'OHLC n/a';
      document.getElementById('mas').textContent = `MA20 ${ma20.length ? ma20[ma20.length - 1].value : 'n/a'} | MA50 ${ma50.length ? ma50[ma50.length - 1].value : 'n/a'} | MA200 ${ma200.length ? ma200[ma200.length - 1].value : 'n/a'}`;
      if (fit) chart.timeScale().fitContent();
    }

    window.renderTerminal = function(payload) { apply(payload || {}, true); };
    window.updateTerminal = function(payload) { apply(payload || {}, false); };
    window.clearTerminal = function() { apply({ Candles: [], Ma20: [], Ma50: [], Ma200: [], Components: [] }, true); };
    window.addEventListener('resize', () => chart.resize(chartElement.clientWidth, chartElement.clientHeight));
    chart.resize(chartElement.clientWidth, chartElement.clientHeight);
  </script>
</body>
</html>
```

- [ ] **Step 4: Package asset in project**

Modify `CAPETF.Desktop.csproj`:

```xml
<Content Include="Assets\synthetic-terminal.html">
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</Content>
```

- [ ] **Step 5: Run test to verify it passes**

Run:

```powershell
dotnet run --project desktop\CAPETF.Desktop.Tests\CAPETF.Desktop.Tests.csproj
```

Expected: `SyntheticBasketBuilder tests passed`.

- [ ] **Step 6: Commit**

```powershell
git add desktop/CAPETF.Desktop/Assets/synthetic-terminal.html desktop/CAPETF.Desktop/CAPETF.Desktop.csproj desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs
git commit -m "Add full screen synthetic terminal chart host"
```

---

### Task 5: WPF Terminal Workspace

**Files:**
- Modify: `desktop/CAPETF.Desktop/MainWindow.xaml`
- Modify: `desktop/CAPETF.Desktop/MainWindow.xaml.cs`

**Interfaces:**
- Consumes: `SyntheticTerminalSelector.SelectBest(...)`, `SyntheticTerminalChartPayload.Build(...)`
- Produces:
  - `TerminalPanel`
  - `TerminalChartWebView`
  - `OpenTerminal_Click`
  - terminal rendering path isolated from dashboard redraw path

- [ ] **Step 1: Add behavior test for workspace mode strings**

Add this method and call to `SyntheticBasketBuilderTests`:

```csharp
private static void TerminalWorkspaceModeNameIsAvailable()
{
    if (SyntheticTerminalWorkspace.ModeName != "Terminal")
    {
        throw new Exception("terminal workspace mode must be named Terminal");
    }
}
```

This requires a tiny model:

```csharp
public static class SyntheticTerminalWorkspace
{
    public const string ModeName = "Terminal";
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet run --project desktop\CAPETF.Desktop.Tests\CAPETF.Desktop.Tests.csproj
```

Expected: compile failure because `SyntheticTerminalWorkspace` does not exist.

- [ ] **Step 3: Add workspace constant**

Add to `SyntheticTerminalModels.cs`:

```csharp
public static class SyntheticTerminalWorkspace
{
    public const string ModeName = "Terminal";
}
```

- [ ] **Step 4: Add terminal option and layout**

Modify `MainWindow.xaml`:

- Add `<ComboBoxItem Content="Terminal"/>` to `WorkspaceModeBox`.
- Add a new `Border x:Name="TerminalPanel"` in the main center column.
- `TerminalPanel` contains a top compact bar, a full-size `wv2:WebView2 x:Name="TerminalChartWebView"`, and a small status/button strip.
- In Terminal mode, hide `GroupList` dashboard content and show `TerminalPanel`.

Use this structural shape:

```xml
<Grid x:Name="TerminalPanel" Visibility="Collapsed">
  <Grid.RowDefinitions>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="*"/>
    <RowDefinition Height="Auto"/>
  </Grid.RowDefinitions>
  <Grid Grid.Row="0">
    <TextBlock x:Name="TerminalHeaderText" Text="Synthetic terminal"/>
    <Button Content="Build Terminal" Click="OpenTerminal_Click"/>
  </Grid>
  <wv2:WebView2 Grid.Row="1" x:Name="TerminalChartWebView" DefaultBackgroundColor="#050914"/>
  <TextBlock Grid.Row="2" x:Name="TerminalStatusText" Text="Load stocks, then open Terminal."/>
</Grid>
```

- [ ] **Step 5: Wire terminal WebView and panel switching**

Modify `MainWindow.xaml.cs`:

- Add fields:

```csharp
private bool _terminalChartReady;
private SyntheticBasket? _terminalBasket;
```

- Call `InitializeTerminalChartAsync()` from constructor.
- Implement `InitializeTerminalChartAsync()` analogous to `InitializeSyntheticChartAsync()` using `Assets/synthetic-terminal.html`.
- In `ApplyWorkspaceMode()`, show `TerminalPanel` when mode is `SyntheticTerminalWorkspace.ModeName`; hide dashboard `GroupList` scroll content in Terminal mode.
- Add `OpenTerminal_Click` that:
  - picks the selected synthetic block or first stock group
  - fetches candles for up to 36 stocks in that block
  - calls `SyntheticTerminalSelector.SelectBest(...)`
  - stores `_terminalBasket`
  - sends `SyntheticTerminalChartPayload.Build(_terminalBasket)` to `window.renderTerminal(...)`

- [ ] **Step 6: Run build**

Run:

```powershell
dotnet build desktop\CAPETF.Desktop\CAPETF.Desktop.csproj -c Release
dotnet run --project desktop\CAPETF.Desktop.Tests\CAPETF.Desktop.Tests.csproj
```

Expected: build succeeds and tests pass.

- [ ] **Step 7: Commit**

```powershell
git add desktop/CAPETF.Desktop/MainWindow.xaml desktop/CAPETF.Desktop/MainWindow.xaml.cs desktop/CAPETF.Desktop/SyntheticTerminalModels.cs desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs
git commit -m "Add synthetic terminal workspace"
```

---

### Task 6: Terminal-Only Real-Time Streaming

**Files:**
- Modify: `desktop/CAPETF.Desktop/MainWindow.xaml.cs`
- Modify: `desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs`

**Interfaces:**
- Consumes: `SyntheticTerminalLiveUpdate.Apply(...)`
- Produces:
  - terminal streaming subscription uses only `_terminalBasket.Components.Select(component => component.Instrument.Epic)`
  - terminal quote handler updates only terminal chart payload

- [ ] **Step 1: Add test for terminal epics**

Add this method and call to `RunAll()`:

```csharp
private static void TerminalStreamingEpicsUseOnlySelectedSyntheticComponents()
{
    var basket = new SyntheticBasket { Symbol = "SYN-ONLY" };
    basket.Components.Add(new SyntheticComponent(new MarketInstrument { Epic = "A" }, 34m, 10m, 1m));
    basket.Components.Add(new SyntheticComponent(new MarketInstrument { Epic = "B" }, 33m, 10m, 1m));
    basket.Components.Add(new SyntheticComponent(new MarketInstrument { Epic = "A" }, 33m, 10m, 1m));

    var epics = SyntheticTerminalWorkspace.StreamingEpics(basket);

    if (!epics.SequenceEqual(new[] { "A", "B" }))
    {
        throw new Exception("terminal streaming must subscribe only distinct selected component epics");
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet run --project desktop\CAPETF.Desktop.Tests\CAPETF.Desktop.Tests.csproj
```

Expected: compile failure because `StreamingEpics` does not exist.

- [ ] **Step 3: Implement terminal epic selection**

Add to `SyntheticTerminalWorkspace`:

```csharp
public static IReadOnlyList<string> StreamingEpics(SyntheticBasket basket) =>
    basket.Components
        .Select(component => component.Instrument.Epic)
        .Where(epic => !string.IsNullOrWhiteSpace(epic))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Take(40)
        .ToList();
```

- [ ] **Step 4: Wire stream button behavior**

Modify `StreamVisible_Click`:

- If workspace mode is `Terminal` and `_terminalBasket` is not null:
  - connect streaming if needed
  - subscribe quotes/OHLC only for `SyntheticTerminalWorkspace.StreamingEpics(_terminalBasket)`
  - set `ConnectionText` to `Streaming synthetic <symbol>`
  - return without using expanded dashboard groups

Modify `Streaming_QuoteReceived`:

- Keep existing dashboard update path for non-Terminal mode.
- If `_terminalBasket` is not null, call `SyntheticTerminalLiveUpdate.Apply(_terminalBasket, update)`.
- If result has payload and `TerminalChartWebView` is ready, call `window.updateTerminal(payloadJson)`.

- [ ] **Step 5: Run tests and build**

Run:

```powershell
dotnet run --project desktop\CAPETF.Desktop.Tests\CAPETF.Desktop.Tests.csproj
dotnet build desktop\CAPETF.Desktop\CAPETF.Desktop.csproj -c Release
```

Expected: tests pass and build succeeds.

- [ ] **Step 6: Commit**

```powershell
git add desktop/CAPETF.Desktop/MainWindow.xaml.cs desktop/CAPETF.Desktop/SyntheticTerminalModels.cs desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs
git commit -m "Stream selected synthetic terminal components"
```

---

### Task 7: Packaging and Verification

**Files:**
- Modify only if needed:
  - `desktop/CAPETF.Desktop/build-installer.ps1`
  - `desktop/CAPETF.Desktop/README.md`

**Interfaces:**
- Consumes all prior tasks.
- Produces updated `artifacts/CAPETF-Realtime-win-x64.zip`.

- [ ] **Step 1: Run full verification**

Run:

```powershell
dotnet run --project desktop\CAPETF.Desktop.Tests\CAPETF.Desktop.Tests.csproj
dotnet build desktop\CAPETF.Desktop\CAPETF.Desktop.csproj -c Release
node tests\dashboard_quality_dip.test.js
& 'C:\Users\jaeku\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' -m unittest discover tests
git diff --check
```

Expected:

- synthetic desktop tests pass
- desktop Release build succeeds with 0 errors
- dashboard JS tests pass
- Python tests pass
- `git diff --check` exits 0

- [ ] **Step 2: Rebuild portable package**

Run:

```powershell
powershell -ExecutionPolicy Bypass -File desktop\CAPETF.Desktop\build-installer.ps1
```

Expected:

- If Inno Setup is missing, `artifacts\CAPETF-Realtime-win-x64.zip` is created.
- If Inno Setup is installed, installer artifact is created in `artifacts`.

- [ ] **Step 3: Manual smoke test**

Run:

```powershell
Start-Process -FilePath "C:\Users\jaeku\OneDrive\Documents\GitHub\CAPETF\desktop\CAPETF.Desktop\publish\win-x64\CAPETF.exe"
```

Manual expected behavior:

- Connect with saved Capital.com credentials.
- Search stocks.
- Choose workspace `Terminal`.
- Click `Build Terminal`.
- A large chart appears.
- MA lines appear when enough candles exist.
- Component panel shows 3 to 4 stocks with weights.
- Click `Stream visible`.
- Connection text changes to streaming synthetic instrument.

- [ ] **Step 4: Commit packaging/docs if changed**

If only artifacts changed and artifacts are tracked, commit them. If artifacts are untracked/ignored, report the file path.

```powershell
git status --short
git add <changed tracked files>
git commit -m "Package synthetic terminal build"
```
