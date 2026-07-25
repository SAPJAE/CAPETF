# Local Synthetic Stock Baskets Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a private local `Synthetic` tab to the CAPETF Windows app that builds 3 to 4 stock weighted synthetic symbols from Capital.com live/local data, using volatility-led ratios and candlestick charts.

**Architecture:** Keep synthetic logic out of `MainWindow.xaml.cs` by adding focused model and service files. Extend the existing `CapitalApiClient` to return OHLC candles, build synthetic baskets in a pure service, and let the WPF window bind generated baskets to a new tab. Chart rendering uses a packaged local WebView2 page with TradingView Lightweight Charts; WPF sends synthetic candles into the page, and the page never calls Capital.com directly.

**Tech Stack:** .NET WPF, C# records/services, existing Capital.com REST client, Microsoft WebView2, TradingView Lightweight Charts, MSTest-style or simple executable unit tests using the existing project build.

## Global Constraints

- The feature runs only in the local Windows app.
- No API key, password, CST token, or security token is written to GitHub or to a public web page.
- Stocks from different currencies are not mixed in one synthetic symbol.
- Synthetic symbols contain 3 to 4 stocks.
- Weights are chosen mainly from similar volatility percentages.
- Use inverse-volatility weighting with 45% maximum component weight and 10% minimum component weight.
- Synthetic candles are analytical basket approximations, not exchange-traded instruments.
- Chart supports daily and weekly views first.

---

## File Structure

- Create `desktop/CAPETF.Desktop/SyntheticModels.cs`: focused synthetic model records (`OhlcPoint`, `SyntheticComponent`, `SyntheticBasket`, `SyntheticBuildResult`).
- Create `desktop/CAPETF.Desktop/SyntheticBasketBuilder.cs`: pure basket construction, volatility calculation, similarity grouping, weight normalization, synthetic candle calculation.
- Modify `desktop/CAPETF.Desktop/CapitalApiClient.cs`: add `GetOhlcPricesAsync` returning full OHLC rows while keeping `GetPricesAsync` for existing line charts.
- Create `desktop/CAPETF.Desktop/Assets/synthetic-chart.html`: local TradingView Lightweight Charts host page.
- Modify `desktop/CAPETF.Desktop/MainWindow.xaml`: add `Synthetic` workspace option, block selector, basket list, component table, and WebView2 chart surface.
- Modify `desktop/CAPETF.Desktop/MainWindow.xaml.cs`: load block candidates, request OHLC history, call `SyntheticBasketBuilder`, bind results, and send candle JSON to WebView2.
- Modify `desktop/CAPETF.Desktop/CAPETF.Desktop.csproj`: add WebView2 package and copy the chart HTML asset to output.

---

### Task 1: Add OHLC Models and Capital API Candle Fetch

**Files:**
- Create: `desktop/CAPETF.Desktop/SyntheticModels.cs`
- Modify: `desktop/CAPETF.Desktop/CapitalApiClient.cs`
- Test: Release build; pure synthetic math tests are introduced in Task 2

**Interfaces:**
- Produces: `public sealed record OhlcPoint(DateTimeOffset Time, decimal Open, decimal High, decimal Low, decimal Close);`
- Produces: `CapitalApiClient.GetOhlcPricesAsync(string epic, string resolution, int max, CancellationToken cancellationToken = default): Task<IReadOnlyList<OhlcPoint>>`
- Consumes: existing Capital.com session and `ReadPrice(JsonElement row, string name)` helper.

- [ ] **Step 1: Add the model records**

Create `desktop/CAPETF.Desktop/SyntheticModels.cs`:

```csharp
using System.Collections.ObjectModel;

namespace CAPETF.Desktop;

public sealed record OhlcPoint(DateTimeOffset Time, decimal Open, decimal High, decimal Low, decimal Close);

public sealed record SyntheticComponent(
    MarketInstrument Instrument,
    decimal Weight,
    decimal AnnualizedVolatilityPct,
    decimal FourYearReturnPct);

public sealed class SyntheticBasket
{
    public string Symbol { get; init; } = "";
    public string Block { get; init; } = "";
    public decimal BasketPrice { get; init; }
    public decimal AverageVolatilityPct { get; init; }
    public decimal SimilarityScore { get; init; }
    public ObservableCollection<SyntheticComponent> Components { get; } = [];
    public ObservableCollection<OhlcPoint> Candles { get; } = [];
    public DateTimeOffset? LastUpdated => Candles.Count == 0 ? null : Candles[^1].Time;
}

public sealed record SyntheticBuildResult(
    IReadOnlyList<SyntheticBasket> Baskets,
    string Message);
```

- [ ] **Step 2: Extend the API client with OHLC fetch**

In `desktop/CAPETF.Desktop/CapitalApiClient.cs`, add this public method after `GetPricesAsync`:

```csharp
public async Task<IReadOnlyList<OhlcPoint>> GetOhlcPricesAsync(string epic, string resolution, int max, CancellationToken cancellationToken = default)
{
    EnsureSession();
    using var doc = await GetJsonAsync($"api/v1/prices/{Uri.EscapeDataString(epic)}?resolution={resolution}&max={max}", cancellationToken);
    if (!doc.RootElement.TryGetProperty("prices", out var prices) || prices.ValueKind != JsonValueKind.Array)
    {
        return [];
    }

    var rows = new List<OhlcPoint>();
    foreach (var row in prices.EnumerateArray())
    {
        var time = ReadString(row, "snapshotTimeUTC") ?? ReadString(row, "snapshotTime") ?? ReadString(row, "time");
        var close = ReadPrice(row, "closePrice") ?? ReadPrice(row, "lastTradedPrice");
        var open = ReadPrice(row, "openPrice") ?? close;
        var high = ReadPrice(row, "highPrice") ?? close;
        var low = ReadPrice(row, "lowPrice") ?? close;
        if (time is null || open is null || high is null || low is null || close is null) continue;
        if (open <= 0 || high <= 0 || low <= 0 || close <= 0) continue;
        if (DateTimeOffset.TryParse(time, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
        {
            rows.Add(new OhlcPoint(parsed, open.Value, high.Value, low.Value, close.Value));
        }
    }

    return rows.OrderBy(point => point.Time).ToList();
}
```

- [ ] **Step 3: Build**

Run:

```powershell
dotnet build desktop\CAPETF.Desktop\CAPETF.Desktop.csproj -c Release
```

Expected: build succeeds with `0 Error(s)`.

- [ ] **Step 4: Commit**

```powershell
git add desktop\CAPETF.Desktop\SyntheticModels.cs desktop\CAPETF.Desktop\CapitalApiClient.cs
git commit -m "Add Capital OHLC candle models"
```

---

### Task 2: Implement Volatility-Led Synthetic Basket Builder

**Files:**
- Create: `desktop/CAPETF.Desktop/SyntheticBasketBuilder.cs`
- Test: `desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs` if a test project exists; otherwise create a small console-free test harness under `desktop/CAPETF.Desktop.Tests`

**Interfaces:**
- Consumes: `OhlcPoint`, `MarketInstrument`, `SyntheticBasket`, `SyntheticComponent`.
- Produces: `SyntheticBasketBuilder.Build(string block, IReadOnlyList<MarketInstrument> instruments, IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>> candles, int maxBaskets = 12): SyntheticBuildResult`
- Produces: `SyntheticBasketBuilder.CalculateInverseVolatilityWeights(IReadOnlyList<decimal> volatilities): IReadOnlyList<decimal>`

- [ ] **Step 1: Write failing tests for weight caps and minimums**

Create `desktop/CAPETF.Desktop.Tests/SyntheticBasketBuilderTests.cs`:

```csharp
using CAPETF.Desktop;

namespace CAPETF.Desktop.Tests;

public static class SyntheticBasketBuilderTests
{
    public static void RunAll()
    {
        InverseVolatilityWeightsSumToOneHundred();
        InverseVolatilityWeightsRespectCapsAndMinimums();
        SyntheticCandlesUseWeightedOhlc();
    }

    private static void InverseVolatilityWeightsSumToOneHundred()
    {
        var weights = SyntheticBasketBuilder.CalculateInverseVolatilityWeights([20m, 20m, 20m, 20m]);
        AssertNear(100m, weights.Sum(), "weights should sum to 100");
        AssertNear(25m, weights[0], "equal volatility should equal-weight");
    }

    private static void InverseVolatilityWeightsRespectCapsAndMinimums()
    {
        var weights = SyntheticBasketBuilder.CalculateInverseVolatilityWeights([5m, 40m, 45m, 50m]);
        if (weights.Any(weight => weight > 45m)) throw new Exception("weight cap exceeded");
        if (weights.Any(weight => weight < 10m)) throw new Exception("minimum weight breached");
        AssertNear(100m, weights.Sum(), "capped weights should sum to 100");
    }

    private static void SyntheticCandlesUseWeightedOhlc()
    {
        var a = new MarketInstrument { Epic = "A", Name = "A", Currency = "USD", Region = "US", Sector = "Tech" };
        var b = new MarketInstrument { Epic = "B", Name = "B", Currency = "USD", Region = "US", Sector = "Tech" };
        var day = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var candles = new Dictionary<string, IReadOnlyList<OhlcPoint>>
        {
            ["A"] = [new(day, 10m, 12m, 9m, 11m), new(day.AddDays(1), 11m, 13m, 10m, 12m)],
            ["B"] = [new(day, 20m, 22m, 19m, 21m), new(day.AddDays(1), 21m, 23m, 20m, 22m)]
        };
        var result = SyntheticBasketBuilder.Build("US / USD / Tech", [a, b], candles, maxBaskets: 1);
        var first = result.Baskets[0].Candles[0];
        AssertNear(15m, first.Open, "weighted open should use component opens");
        AssertNear(17m, first.High, "weighted high should use component highs");
        AssertNear(14m, first.Low, "weighted low should use component lows");
        AssertNear(16m, first.Close, "weighted close should use component closes");
    }

    private static void AssertNear(decimal expected, decimal actual, string message)
    {
        if (Math.Abs(expected - actual) > 0.0001m) throw new Exception($"{message}. Expected {expected}, got {actual}");
    }
}
```

- [ ] **Step 2: Add a minimal test runner if no test project exists**

Create `desktop/CAPETF.Desktop.Tests/CAPETF.Desktop.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\CAPETF.Desktop\CAPETF.Desktop.csproj" />
  </ItemGroup>
</Project>
```

Create `desktop/CAPETF.Desktop.Tests/Program.cs`:

```csharp
using CAPETF.Desktop.Tests;

SyntheticBasketBuilderTests.RunAll();
Console.WriteLine("SyntheticBasketBuilder tests passed");
```

- [ ] **Step 3: Run tests and verify failure**

Run:

```powershell
dotnet run --project desktop\CAPETF.Desktop.Tests\CAPETF.Desktop.Tests.csproj
```

Expected: compile fails because `SyntheticBasketBuilder` does not exist.

- [ ] **Step 4: Implement the builder**

Create `desktop/CAPETF.Desktop/SyntheticBasketBuilder.cs`:

```csharp
namespace CAPETF.Desktop;

public static class SyntheticBasketBuilder
{
    public static SyntheticBuildResult Build(
        string block,
        IReadOnlyList<MarketInstrument> instruments,
        IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>> candles,
        int maxBaskets = 12)
    {
        var candidates = instruments
            .Where(item => !string.IsNullOrWhiteSpace(item.Epic))
            .Where(item => candles.TryGetValue(item.Epic, out var rows) && rows.Count >= 120)
            .Select(item => new Candidate(item, candles[item.Epic], AnnualizedVolatilityPct(candles[item.Epic]), FourYearReturnPct(candles[item.Epic])))
            .Where(item => item.VolatilityPct > 0)
            .OrderBy(item => item.VolatilityPct)
            .ThenBy(item => item.Instrument.Name)
            .ToList();

        if (candidates.Count < 3)
        {
            return new SyntheticBuildResult([], "Not enough stocks with stable price history for this block.");
        }

        var baskets = new List<SyntheticBasket>();
        var cursor = 0;
        while (cursor + 2 < candidates.Count && baskets.Count < maxBaskets)
        {
            var cluster = candidates.Skip(cursor).Take(Math.Min(4, candidates.Count - cursor)).ToList();
            if (cluster.Count < 3) break;
            var weights = CalculateInverseVolatilityWeights(cluster.Select(item => item.VolatilityPct).ToList());
            var basket = new SyntheticBasket
            {
                Symbol = $"SYN-{NormalizeSymbol(block)}-{baskets.Count + 1:00}",
                Block = block,
                AverageVolatilityPct = decimal.Round(cluster.Average(item => item.VolatilityPct), 2),
                SimilarityScore = decimal.Round(100m - VolatilitySpread(cluster), 2),
            };

            for (var index = 0; index < cluster.Count; index++)
            {
                basket.Components.Add(new SyntheticComponent(cluster[index].Instrument, weights[index], cluster[index].VolatilityPct, cluster[index].FourYearReturnPct));
            }

            foreach (var candle in BuildCandles(cluster, weights))
            {
                basket.Candles.Add(candle);
            }

            basket.BasketPrice = basket.Candles.Count == 0 ? 0 : basket.Candles[^1].Close;
            if (basket.Candles.Count >= 2) baskets.Add(basket);
            cursor += cluster.Count;
        }

        return new SyntheticBuildResult(baskets, baskets.Count == 0 ? "No synthetic baskets could be formed." : $"{baskets.Count} synthetic baskets built.");
    }

    public static IReadOnlyList<decimal> CalculateInverseVolatilityWeights(IReadOnlyList<decimal> volatilities)
    {
        if (volatilities.Count == 0) return [];
        var raw = volatilities.Select(value => value <= 0 ? 0m : 1m / value).ToList();
        var sum = raw.Sum();
        var weights = sum == 0 ? Enumerable.Repeat(100m / volatilities.Count, volatilities.Count).ToList() : raw.Select(value => value / sum * 100m).ToList();
        return ApplyWeightBounds(weights, 10m, 45m);
    }

    private static IReadOnlyList<decimal> ApplyWeightBounds(IReadOnlyList<decimal> source, decimal minimum, decimal maximum)
    {
        var weights = source.Select(value => Math.Clamp(value, minimum, maximum)).ToList();
        for (var iteration = 0; iteration < 12; iteration++)
        {
            var diff = 100m - weights.Sum();
            if (Math.Abs(diff) < 0.0001m) break;
            var adjustable = weights.Select((value, index) => new { value, index })
                .Where(item => diff > 0 ? item.value < maximum : item.value > minimum)
                .ToList();
            if (adjustable.Count == 0) break;
            var step = diff / adjustable.Count;
            foreach (var item in adjustable)
            {
                weights[item.index] = Math.Clamp(weights[item.index] + step, minimum, maximum);
            }
        }
        return weights.Select(value => decimal.Round(value, 4)).ToList();
    }

    private static IEnumerable<OhlcPoint> BuildCandles(IReadOnlyList<Candidate> cluster, IReadOnlyList<decimal> weights)
    {
        var dates = cluster.SelectMany(item => item.Candles.Select(candle => candle.Time.Date)).GroupBy(date => date)
            .Where(group => group.Count() == cluster.Count).Select(group => group.Key).OrderBy(date => date).ToList();
        foreach (var date in dates)
        {
            decimal open = 0, high = 0, low = 0, close = 0;
            DateTimeOffset time = default;
            for (var index = 0; index < cluster.Count; index++)
            {
                var candle = cluster[index].Candles.First(row => row.Time.Date == date);
                var weight = weights[index] / 100m;
                open += candle.Open * weight;
                high += candle.High * weight;
                low += candle.Low * weight;
                close += candle.Close * weight;
                time = candle.Time;
            }
            yield return new OhlcPoint(time, decimal.Round(open, 6), decimal.Round(high, 6), decimal.Round(low, 6), decimal.Round(close, 6));
        }
    }

    private static decimal AnnualizedVolatilityPct(IReadOnlyList<OhlcPoint> candles)
    {
        var returns = candles.Zip(candles.Skip(1), (previous, current) => previous.Close <= 0 ? 0m : (current.Close / previous.Close) - 1m).ToList();
        if (returns.Count < 2) return 0m;
        var average = returns.Average();
        var variance = returns.Select(value => Math.Pow((double)(value - average), 2)).Average();
        return decimal.Round((decimal)Math.Sqrt(variance) * (decimal)Math.Sqrt(52) * 100m, 4);
    }

    private static decimal FourYearReturnPct(IReadOnlyList<OhlcPoint> candles)
    {
        if (candles.Count < 2 || candles[0].Close <= 0) return 0m;
        return decimal.Round((candles[^1].Close / candles[0].Close - 1m) * 100m, 2);
    }

    private static decimal VolatilitySpread(IReadOnlyList<Candidate> cluster) => cluster.Max(item => item.VolatilityPct) - cluster.Min(item => item.VolatilityPct);

    private static string NormalizeSymbol(string block)
    {
        var chars = block.ToUpperInvariant().Where(char.IsLetterOrDigit).Take(14).ToArray();
        return chars.Length == 0 ? "BLOCK" : new string(chars);
    }

    private sealed record Candidate(MarketInstrument Instrument, IReadOnlyList<OhlcPoint> Candles, decimal VolatilityPct, decimal FourYearReturnPct);
}
```

- [ ] **Step 5: Run tests and build**

Run:

```powershell
dotnet run --project desktop\CAPETF.Desktop.Tests\CAPETF.Desktop.Tests.csproj
dotnet build desktop\CAPETF.Desktop\CAPETF.Desktop.csproj -c Release
```

Expected: test runner prints `SyntheticBasketBuilder tests passed`; app build succeeds with `0 Error(s)`.

- [ ] **Step 6: Commit**

```powershell
git add desktop\CAPETF.Desktop\SyntheticBasketBuilder.cs desktop\CAPETF.Desktop.Tests
git commit -m "Add volatility weighted synthetic basket builder"
```

---

### Task 3: Add Synthetic Tab UI and TradingView Lightweight Chart Rendering

**Files:**
- Create: `desktop/CAPETF.Desktop/Assets/synthetic-chart.html`
- Modify: `desktop/CAPETF.Desktop/CAPETF.Desktop.csproj`
- Modify: `desktop/CAPETF.Desktop/MainWindow.xaml`
- Modify: `desktop/CAPETF.Desktop/MainWindow.xaml.cs`

**Interfaces:**
- Consumes: `SyntheticBasket`, `OhlcPoint`, `SyntheticBuildResult`.
- Produces: user-visible `Synthetic` workspace mode with block selector, build button, basket list, component list, and local TradingView Lightweight Charts candlestick chart.

- [ ] **Step 1: Add WebView2 package and chart asset**

In `desktop/CAPETF.Desktop/CAPETF.Desktop.csproj`, update the package `ItemGroup` and add the content asset:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Web.WebView2" Version="1.0.2739.15" />
  <PackageReference Include="System.Security.Cryptography.ProtectedData" Version="8.0.0" />
</ItemGroup>

<ItemGroup>
  <Content Include="Assets\synthetic-chart.html">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```

Create `desktop/CAPETF.Desktop/Assets/synthetic-chart.html`:

```html
<!doctype html>
<html>
<head>
  <meta charset="utf-8">
  <meta http-equiv="Content-Security-Policy" content="default-src 'self' https://unpkg.com; script-src 'self' 'unsafe-inline' https://unpkg.com; style-src 'unsafe-inline';">
  <style>
    html, body, #chart { width: 100%; height: 100%; margin: 0; background: #0f172a; overflow: hidden; }
  </style>
  <script src="https://unpkg.com/lightweight-charts@4.2.0/dist/lightweight-charts.standalone.production.js"></script>
</head>
<body>
  <div id="chart"></div>
  <script>
    const chart = LightweightCharts.createChart(document.getElementById('chart'), {
      layout: { background: { color: '#0f172a' }, textColor: '#cbd5e1' },
      grid: { vertLines: { color: '#1e293b' }, horzLines: { color: '#1e293b' } },
      rightPriceScale: { borderColor: '#334155' },
      timeScale: { borderColor: '#334155', timeVisible: false },
    });
    const candles = chart.addCandlestickSeries({
      upColor: '#22c55e',
      downColor: '#ef4444',
      borderUpColor: '#22c55e',
      borderDownColor: '#ef4444',
      wickUpColor: '#86efac',
      wickDownColor: '#fca5a5',
    });
    window.renderSyntheticCandles = function (rows) {
      candles.setData(rows || []);
      chart.timeScale().fitContent();
    };
    window.addEventListener('resize', () => chart.resize(window.innerWidth, window.innerHeight));
  </script>
</body>
</html>
```

- [ ] **Step 2: Add XAML controls**

In `desktop/CAPETF.Desktop/MainWindow.xaml`, add the WebView2 namespace to the `Window` element:

```xml
xmlns:wv2="clr-namespace:Microsoft.Web.WebView2.Wpf;assembly=Microsoft.Web.WebView2.Wpf"
```

Add `Synthetic` to `WorkspaceModeBox`:

```xml
<ComboBoxItem Content="Synthetic"/>
```

Add a synthetic panel above `GroupList` inside the center column `DockPanel`, after `DiscoverStrip`:

```xml
<Border x:Name="SyntheticPanel" Background="#0F172A" BorderBrush="{StaticResource LineBrush}" BorderThickness="1" CornerRadius="8" Padding="10" Margin="0,8,0,0" Visibility="Collapsed">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="2*"/>
            <ColumnDefinition Width="Auto"/>
            <ColumnDefinition Width="2*"/>
        </Grid.ColumnDefinitions>
        <StackPanel>
            <TextBlock Text="Synthetic block" Foreground="{StaticResource MutedBrush}"/>
            <ComboBox x:Name="SyntheticBlockBox"/>
        </StackPanel>
        <Button Grid.Column="1" Content="Build" Click="BuildSynthetic_Click" Margin="12,20,12,0"/>
        <StackPanel Grid.Column="2">
            <TextBlock Text="Status" Foreground="{StaticResource MutedBrush}"/>
            <TextBlock x:Name="SyntheticStatusText" Text="Load stocks, then build synthetic baskets." TextWrapping="Wrap"/>
        </StackPanel>
    </Grid>
</Border>
```

Add a `Synthetic` tab in the right-side `TabControl`:

```xml
<TabItem Header="Synthetic">
    <Grid Margin="0,10,0,0">
        <Grid.RowDefinitions>
            <RowDefinition Height="180"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="260"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>
        <ListBox x:Name="SyntheticBasketList" SelectionChanged="SyntheticBasketList_SelectionChanged" DisplayMemberPath="Symbol"/>
        <TextBlock Grid.Row="1" x:Name="SyntheticDetailText" Text="Select a synthetic symbol." Foreground="{StaticResource MutedBrush}" Margin="0,8,0,8" TextWrapping="Wrap"/>
        <wv2:WebView2 Grid.Row="2" x:Name="SyntheticChartWebView" DefaultBackgroundColor="#0F172A"/>
        <ItemsControl Grid.Row="3" x:Name="SyntheticComponentList" Margin="0,10,0,0">
            <ItemsControl.ItemTemplate>
                <DataTemplate>
                    <Grid Margin="0,0,0,6">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*"/>
                            <ColumnDefinition Width="80"/>
                            <ColumnDefinition Width="90"/>
                        </Grid.ColumnDefinitions>
                        <TextBlock Text="{Binding Instrument.Name}" TextTrimming="CharacterEllipsis"/>
                        <TextBlock Grid.Column="1" Text="{Binding Weight, StringFormat={}{0:0.##}%}" HorizontalAlignment="Right"/>
                        <TextBlock Grid.Column="2" Text="{Binding AnnualizedVolatilityPct, StringFormat={}{0:0.##}% vol}" HorizontalAlignment="Right" Foreground="{StaticResource MutedBrush}"/>
                    </Grid>
                </DataTemplate>
            </ItemsControl.ItemTemplate>
        </ItemsControl>
    </Grid>
</TabItem>
```

- [ ] **Step 3: Add backing fields, initialize the chart, and refresh blocks**

In `MainWindow.xaml.cs`, add fields:

```csharp
private bool _syntheticChartReady;
private readonly ObservableCollection<SyntheticBasket> _syntheticBaskets = [];
private SyntheticBasket? _selectedSyntheticBasket;
```

In the constructor after `GroupList.ItemsSource = _groups;`:

```csharp
SyntheticBasketList.ItemsSource = _syntheticBaskets;
InitializeSyntheticChartAsync();
```

Add:

```csharp
private async void InitializeSyntheticChartAsync()
{
    try
    {
        await SyntheticChartWebView.EnsureCoreWebView2Async();
        var chartPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "synthetic-chart.html");
        SyntheticChartWebView.Source = new Uri(chartPath);
        _syntheticChartReady = true;
    }
    catch (Exception ex)
    {
        SyntheticStatusText.Text = $"Synthetic chart unavailable: {ex.Message}";
    }
}

private void RefreshSyntheticBlocks()
{
    if (SyntheticBlockBox is null) return;
    var selected = SyntheticBlockBox.SelectedItem?.ToString();
    SyntheticBlockBox.Items.Clear();
    foreach (var block in _instruments.Where(item => !IsEtf(item)).Select(item => item.Group).Distinct().OrderBy(value => value))
    {
        SyntheticBlockBox.Items.Add(block);
    }
    if (selected is not null && SyntheticBlockBox.Items.Contains(selected)) SyntheticBlockBox.SelectedItem = selected;
    else if (SyntheticBlockBox.Items.Count > 0) SyntheticBlockBox.SelectedIndex = 0;
}
```

Call `RefreshSyntheticBlocks();` at the end of `SearchAsync()` after `RebuildGroups();`.

- [ ] **Step 4: Build synthetic baskets from loaded block**

Add:

```csharp
private async void BuildSynthetic_Click(object sender, RoutedEventArgs e)
{
    if (SyntheticBlockBox.SelectedItem is not string block)
    {
        SyntheticStatusText.Text = "Select a block first.";
        return;
    }

    try
    {
        SyntheticStatusText.Text = "Loading four-year candles...";
        var instruments = _instruments.Where(item => item.Group == block && !IsEtf(item)).Take(36).ToList();
        var candles = new Dictionary<string, IReadOnlyList<OhlcPoint>>();
        foreach (var item in instruments)
        {
            try
            {
                var rows = await _api.GetOhlcPricesAsync(item.Epic, "WEEK", 260);
                if (rows.Count >= 120) candles[item.Epic] = rows;
            }
            catch
            {
                item.Status = "Synthetic history n/a";
            }
        }

        var result = SyntheticBasketBuilder.Build(block, instruments, candles);
        _syntheticBaskets.Clear();
        foreach (var basket in result.Baskets) _syntheticBaskets.Add(basket);
        SyntheticStatusText.Text = result.Message;
        if (_syntheticBaskets.Count > 0) SyntheticBasketList.SelectedIndex = 0;
    }
    catch (Exception ex)
    {
        SyntheticStatusText.Text = "Synthetic build failed.";
        MessageBox.Show(ex.Message, "Synthetic baskets", MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}
```

- [ ] **Step 5: Render synthetic candles in TradingView Lightweight Charts**

Add:

```csharp
private void SyntheticBasketList_SelectionChanged(object sender, SelectionChangedEventArgs e)
{
    if (SyntheticBasketList.SelectedItem is not SyntheticBasket basket) return;
    _selectedSyntheticBasket = basket;
    SyntheticComponentList.ItemsSource = basket.Components;
    SyntheticDetailText.Text = $"{basket.Symbol} | {basket.Block} | {basket.BasketPrice:0.####} | avg vol {basket.AverageVolatilityPct:0.##}% | {basket.LastUpdated:yyyy-MM-dd}";
    RenderSyntheticCandlesAsync(basket);
}

private async void RenderSyntheticCandlesAsync(SyntheticBasket basket)
{
    if (!_syntheticChartReady || SyntheticChartWebView.CoreWebView2 is null) return;
    var rows = basket.Candles.Select(candle => new
    {
        time = candle.Time.ToUnixTimeSeconds(),
        open = candle.Open,
        high = candle.High,
        low = candle.Low,
        close = candle.Close,
    });
    var json = System.Text.Json.JsonSerializer.Serialize(rows);
    await SyntheticChartWebView.ExecuteScriptAsync($"window.renderSyntheticCandles({json});");
}
```

- [ ] **Step 6: Toggle panel visibility**

In `ApplyWorkspaceMode()`, add:

```csharp
SyntheticPanel.Visibility = mode == "Synthetic" ? Visibility.Visible : Visibility.Collapsed;
if (mode == "Synthetic") RefreshSyntheticBlocks();
```

Ensure `DiscoverStrip.Visibility` remains visible only for `Trade` or `Discover`.

- [ ] **Step 7: Build and manually verify**

Run:

```powershell
dotnet run --project desktop\CAPETF.Desktop\CAPETF.Desktop.csproj
```

Expected:
- app opens
- connect with saved/local Capital.com credentials
- search Stocks
- choose workspace `Synthetic`
- select a block such as `US / USD / Technology`
- press `Build`
- synthetic baskets appear with 3 to 4 components and a TradingView Lightweight Charts candlestick chart

- [ ] **Step 7: Commit**

```powershell
git add desktop\CAPETF.Desktop\CAPETF.Desktop.csproj desktop\CAPETF.Desktop\Assets\synthetic-chart.html desktop\CAPETF.Desktop\MainWindow.xaml desktop\CAPETF.Desktop\MainWindow.xaml.cs
git commit -m "Add synthetic basket workspace"
```

---

### Task 4: Wire Live Quote Updates Into Synthetic Baskets

**Files:**
- Modify: `desktop/CAPETF.Desktop/MainWindow.xaml.cs`
- Test: desktop test runner from Task 2 plus manual streaming verification

**Interfaces:**
- Consumes: existing `Streaming_QuoteReceived` and `_syntheticBaskets`.
- Produces: selected synthetic basket price updates when one of its components receives a live Capital.com quote.

- [ ] **Step 1: Add synthetic quote update helper**

Add to `MainWindow.xaml.cs`:

```csharp
private void UpdateSyntheticBasketsForQuote(QuoteUpdate update)
{
    foreach (var basket in _syntheticBaskets)
    {
        var component = basket.Components.FirstOrDefault(item => item.Instrument.Epic == update.Epic);
        if (component is null || update.Price is null) continue;
        var previousPrice = component.Instrument.Price;
        component.Instrument.Price = update.Price;
        if (previousPrice is null || previousPrice <= 0 || basket.Candles.Count == 0) continue;

        var last = basket.Candles[^1];
        var delta = (update.Price.Value - previousPrice.Value) * component.Weight / 100m;
        basket.Candles[^1] = last with
        {
            High = Math.Max(last.High, last.Close + delta),
            Low = Math.Min(last.Low, last.Close + delta),
            Close = decimal.Round(last.Close + delta, 6),
        };
    }
    if (_selectedSyntheticBasket is not null)
    {
        SyntheticDetailText.Text = $"{_selectedSyntheticBasket.Symbol} | {_selectedSyntheticBasket.Block} | {_selectedSyntheticBasket.Candles[^1].Close:0.####} | live | {DateTime.Now:HH:mm:ss}";
        RenderSyntheticCandlesAsync(_selectedSyntheticBasket);
    }
}
```

- [ ] **Step 2: Call helper from streaming**

In `Streaming_QuoteReceived`, after `UpdateStats();`, add:

```csharp
UpdateSyntheticBasketsForQuote(update);
```

- [ ] **Step 3: Ensure streaming subscribes to synthetic components**

In `StreamVisible_Click`, after visible instruments are selected, append selected synthetic component epics:

```csharp
var syntheticEpics = _syntheticBaskets.SelectMany(basket => basket.Components).Select(component => component.Instrument.Epic);
var epics = visible.Select(item => item.Epic).Concat(syntheticEpics).Distinct().Take(40).ToList();
await _streaming.SubscribeQuotesAsync(_api.Session, epics);
await _streaming.SubscribeOhlcAsync(_api.Session, epics, SelectedResolution());
ConnectionText.Text = $"Streaming {epics.Count} instruments";
```

Replace the old subscribe calls in that method with the block above.

- [ ] **Step 4: Run tests and app build**

Run:

```powershell
dotnet run --project desktop\CAPETF.Desktop.Tests\CAPETF.Desktop.Tests.csproj
dotnet build desktop\CAPETF.Desktop\CAPETF.Desktop.csproj -c Release
```

Expected: test runner passes; app build succeeds with `0 Error(s)`.

- [ ] **Step 5: Manual realtime verification**

Run:

```powershell
dotnet run --project desktop\CAPETF.Desktop\CAPETF.Desktop.csproj
```

Expected:
- build a synthetic basket
- click `Stream visible`
- when component quotes arrive, the synthetic selected chart last candle updates and detail text shows `live`

- [ ] **Step 6: Commit**

```powershell
git add desktop\CAPETF.Desktop\MainWindow.xaml.cs
git commit -m "Update synthetic baskets from live quotes"
```

---

### Task 5: Installer and Final Verification

**Files:**
- Modify only if build requires it: `desktop/CAPETF.Desktop/CAPETF.Desktop.csproj`, `desktop/CAPETF.Desktop/installer/CAPETF.iss`
- No code changes expected unless packaging misses new files.

**Interfaces:**
- Consumes: completed local app changes.
- Produces: verified Release build and installer packaging compatibility.

- [ ] **Step 1: Build Release**

Run:

```powershell
dotnet build desktop\CAPETF.Desktop\CAPETF.Desktop.csproj -c Release
```

Expected: build succeeds with `0 Error(s)`.

- [ ] **Step 2: Run test harness**

Run:

```powershell
dotnet run --project desktop\CAPETF.Desktop.Tests\CAPETF.Desktop.Tests.csproj
```

Expected: `SyntheticBasketBuilder tests passed`.

- [ ] **Step 3: Build installer if Inno Setup is available**

Run:

```powershell
powershell -ExecutionPolicy Bypass -File desktop\CAPETF.Desktop\build-installer.ps1
```

Expected: installer script completes or clearly reports that Inno Setup is missing.

- [ ] **Step 4: Review git diff**

Run:

```powershell
git status --short
git diff --check
```

Expected: only intentional files changed; `git diff --check` prints no errors.

- [ ] **Step 5: Final commit if packaging changes were needed**

If Task 5 changed installer/project files:

```powershell
git add desktop\CAPETF.Desktop\CAPETF.Desktop.csproj desktop\CAPETF.Desktop\installer\CAPETF.iss
git commit -m "Package synthetic basket workspace"
```
