using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CAPETF.Desktop;

public partial class MainWindow : Window
{
    private readonly CredentialStore _credentialStore = new();
    private readonly WorkspaceStore _workspaceStore = new();
    private readonly CapitalApiClient _api = new();
    private CapitalStreamingClient? _streaming;
    private readonly ObservableCollection<InstrumentGroup> _groups = [];
    private readonly List<MarketInstrument> _instruments = [];
    private readonly WorkspaceState _workspace;
    private MarketInstrument? _selected;
    private bool _syntheticChartReady;
    private bool _terminalChartReady;
    private readonly ObservableCollection<SyntheticBasket> _syntheticBaskets = [];
    private readonly LatestOperationCoordinator _dataOperations = new();
    private SyntheticBasket? _selectedSyntheticBasket;
    private SyntheticBasket? _terminalBasket;
    private static readonly GridLength NormalLeftWidth = new(292);
    private static readonly GridLength NormalRightWidth = new(380);

    public MainWindow()
    {
        InitializeComponent();
        _workspace = _workspaceStore.Load();
        GroupList.ItemsSource = _groups;
        SyntheticBasketList.ItemsSource = _syntheticBaskets;
        InitializeSyntheticChartAsync();
        InitializeTerminalChartAsync();
        LoadSavedCredentials();
        UpdateStats();
        ApplyWorkspaceMode();
    }

    private void LoadSavedCredentials()
    {
        var saved = _credentialStore.Load();
        if (saved is null) return;
        IdentifierBox.Text = saved.Identifier;
        PasswordBox.Password = saved.Password;
        ApiKeyBox.Password = saved.ApiKey;
        DemoCheck.IsChecked = saved.UseDemo;
        ConnectionText.Text = "Saved keys loaded";
    }

    private async void Connect_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ConnectionText.Text = "Connecting...";
            var credentials = ReadCredentials();
            ValidateCredentials(credentials);
            await _api.LoginAsync(credentials);
            ConnectionText.Text = "Connected";
            ResultText.Text = "Connected. Press Search to load instruments.";
        }
        catch (Exception ex)
        {
            ConnectionText.Text = "Connection failed";
            ResultText.Text = ex.Message;
            MessageBox.Show(ex.Message, "Capital.com connection", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void Disconnect_Click(object sender, RoutedEventArgs e)
    {
        if (_streaming is not null)
        {
            await _streaming.DisposeAsync();
            _streaming = null;
        }
        ConnectionText.Text = "Disconnected";
    }

    private void SaveCredentials_Click(object sender, RoutedEventArgs e)
    {
        _credentialStore.Save(ReadCredentials());
        ConnectionText.Text = "Keys saved locally";
    }

    private void ForgetCredentials_Click(object sender, RoutedEventArgs e)
    {
        _credentialStore.Clear();
        ConnectionText.Text = "Saved keys removed";
    }

    private async void Search_Click(object sender, RoutedEventArgs e) => await SearchAsync();

    private async void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) await SearchAsync();
    }

    private async Task SearchAsync()
    {
        var operation = _dataOperations.Begin();
        try
        {
            ResultText.Text = "Searching Capital.com...";
            SyntheticStatusText.Text = "Synthetic baskets cleared for new search.";
            _groups.Clear();
            ClearSyntheticResults();
            _instruments.Clear();

            var markets = await _api.SearchMarketsAsync(SearchBox.Text.Trim(), operation.Token);
            if (!_dataOperations.IsCurrent(operation)) return;
            var filtered = FilterDataset(markets).Take(240).ToList();
            foreach (var item in filtered)
            {
                ApplyWorkspaceState(item);
                _instruments.Add(item);
            }

            await LoadHistoryForVisibleAsync(_instruments.Take(40), operation);
            if (!_dataOperations.IsCurrent(operation)) return;
            RebuildGroups();
            RefreshSyntheticBlocks();
            ResultText.Text = _instruments.Count == 0
                ? "0 instruments found. Check Dataset vs Search, for example use Dataset ETFs with search ETF."
                : $"{_instruments.Count} instruments loaded. Expand a group, then start realtime for visible.";
            UpdatedText.Text = DateTime.Now.ToString("HH:mm:ss");
            UpdateDiscoveryStrip();
            UpdateStats();
            SyntheticStatusText.Text = _instruments.Any(CapitalInstrumentTypes.IsStock)
                ? "Stocks loaded. Choose a block and build synthetic baskets."
                : "No stock candidates found in this search.";
        }
        catch (OperationCanceledException) when (operation.Token.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            if (!_dataOperations.IsCurrent(operation)) return;
            ResultText.Text = "Search failed";
            MessageBox.Show(ex.Message, "Search", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task LoadHistoryForVisibleAsync(
        IEnumerable<MarketInstrument> instruments,
        OperationTicket? operation = null)
    {
        var resolution = SelectedResolution();
        var cancellationToken = operation?.Token ?? CancellationToken.None;
        foreach (var item in instruments)
        {
            try
            {
                var points = await _api.GetPricesAsync(item.Epic, resolution, resolution == "DAY" ? 260 : 96, cancellationToken);
                if (operation is { } current && !_dataOperations.IsCurrent(current)) return;
                item.Points.Clear();
                foreach (var point in points) item.Points.Add(point);
                UpdateDerivedValues(item);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                if (operation is { } current && !_dataOperations.IsCurrent(current)) return;
                item.Status = "History n/a";
            }
        }
    }

    private void RebuildGroups()
    {
        _groups.Clear();
        foreach (var group in _instruments.GroupBy(item => item.Group).OrderBy(group => group.Key))
        {
            _groups.Add(new InstrumentGroup(group.Key, group.OrderBy(item => item.Name).ToList()));
        }
    }

    private async void InitializeSyntheticChartAsync()
    {
        try
        {
            await SyntheticChartWebView.EnsureCoreWebView2Async();
            _syntheticChartReady = false;
            SyntheticChartWebView.NavigationCompleted += SyntheticChartWebView_NavigationCompleted;
            var chartPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "synthetic-chart.html");
            SyntheticChartWebView.Source = new Uri(chartPath);
        }
        catch (Exception ex)
        {
            SyntheticStatusText.Text = $"Synthetic chart unavailable: {ex.Message}";
        }
    }

    private void SyntheticChartWebView_NavigationCompleted(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
    {
        _syntheticChartReady = e.IsSuccess;
        if (!e.IsSuccess)
        {
            SyntheticStatusText.Text = "Synthetic chart navigation failed.";
            return;
        }

        if (_selectedSyntheticBasket is not null)
        {
            RenderSyntheticCandlesAsync(_selectedSyntheticBasket);
        }
    }

    private async void InitializeTerminalChartAsync()
    {
        try
        {
            await TerminalChartWebView.EnsureCoreWebView2Async();
            _terminalChartReady = false;
            TerminalChartWebView.CoreWebView2.ProcessFailed += (_, args) =>
                TerminalStatusText.Text = $"Terminal WebView failed: {args.ProcessFailedKind}";
            TerminalChartWebView.NavigationCompleted += TerminalChartWebView_NavigationCompleted;
            var chartPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "synthetic-terminal.html");
            TerminalChartWebView.Source = new Uri(chartPath);
        }
        catch (Exception ex)
        {
            TerminalStatusText.Text = $"Terminal chart unavailable: {ex.Message}";
        }
    }

    private async void TerminalChartWebView_NavigationCompleted(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
    {
        _terminalChartReady = e.IsSuccess;
        if (!e.IsSuccess)
        {
            TerminalStatusText.Text = "Terminal chart navigation failed.";
            return;
        }

        if (_terminalBasket is not null)
        {
            await RenderTerminalAsync(_terminalBasket, fit: true);
        }
    }

    private void RefreshSyntheticBlocks()
    {
        if (SyntheticBlockBox is null) return;
        var selected = SyntheticBlockBox.SelectedItem?.ToString();
        SyntheticBlockBox.Items.Clear();
        foreach (var block in _instruments.Where(CapitalInstrumentTypes.IsStock).Select(item => item.Group).Distinct().OrderBy(value => value))
        {
            SyntheticBlockBox.Items.Add(block);
        }
        if (selected is not null && SyntheticBlockBox.Items.Contains(selected)) SyntheticBlockBox.SelectedItem = selected;
        else if (SyntheticBlockBox.Items.Count > 0) SyntheticBlockBox.SelectedIndex = 0;
    }

    private async void BuildSynthetic_Click(object sender, RoutedEventArgs e)
    {
        if (SyntheticBlockBox.SelectedItem is not string block)
        {
            SyntheticStatusText.Text = "Select a block first.";
            return;
        }

        var timeframe = (SyntheticTimeframeBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Weekly";
        var resolution = timeframe == "Daily" ? "DAY" : "WEEK";
        var periodsPerYear = timeframe == "Daily" ? 252 : 52;
        var operation = _dataOperations.Begin();

        try
        {
            ClearSyntheticResults();
            SyntheticStatusText.Text = $"Loading all available {timeframe.ToLowerInvariant()} candles...";
            var instruments = _instruments
                .Where(item => item.Group == block && CapitalInstrumentTypes.IsStock(item))
                .Take(36)
                .ToList();
            var candles = new Dictionary<string, IReadOnlyList<OhlcPoint>>();
            foreach (var item in instruments)
            {
                try
                {
                    var rows = await _api.GetAllAvailableOhlcPricesAsync(item.Epic, resolution, operation.Token);
                    if (!_dataOperations.IsCurrent(operation)) return;
                    if (rows.Count >= 120) candles[item.Epic] = rows;
                }
                catch (OperationCanceledException) when (operation.Token.IsCancellationRequested)
                {
                    throw;
                }
                catch
                {
                    if (!_dataOperations.IsCurrent(operation)) return;
                    item.Status = "Synthetic history n/a";
                }
            }

            if (!_dataOperations.IsCurrent(operation)) return;
            var result = SyntheticBasketBuilder.Build(block, instruments, candles, periodsPerYear: periodsPerYear);
            if (!_dataOperations.IsCurrent(operation)) return;
            if (result.Baskets.Count == 0) ClearSyntheticResults();
            foreach (var basket in result.Baskets) _syntheticBaskets.Add(basket);
            SyntheticStatusText.Text = result.Message;
            if (_syntheticBaskets.Count > 0) SyntheticBasketList.SelectedIndex = 0;
        }
        catch (OperationCanceledException) when (operation.Token.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            if (!_dataOperations.IsCurrent(operation)) return;
            SyntheticStatusText.Text = "Synthetic build failed.";
            MessageBox.Show(ex.Message, "Synthetic baskets", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void OpenTerminal_Click(object sender, RoutedEventArgs e) => await BuildTerminalAsync();

    private async Task BuildTerminalAsync()
    {
        if (_api.Session is null)
        {
            MessageBox.Show("Connect to Capital.com first.", "Synthetic terminal", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var block = SelectedTerminalBlock();
        if (block is null)
        {
            TerminalStatusText.Text = "Load stock markets before building the terminal.";
            return;
        }

        var operation = _dataOperations.Begin();
        try
        {
            _terminalBasket = null;
            await ClearTerminalAsync();
            var instruments = SyntheticTerminalSelector.HistoryLoadCandidates(block, _instruments);
            var terminalResolution = SelectedTerminalResolution();
            var requestResolution = TerminalRequestResolution(terminalResolution);
            var minimumCandles = TerminalMinimumCandles(terminalResolution);
            var periodsPerYear = TerminalPeriodsPerYear(terminalResolution);
            TerminalStatusText.Text = $"Loading {TerminalTimeframeText().ToLowerInvariant()} candles for {block}: scanning up to {instruments.Count} stocks...";
            var candles = new Dictionary<string, IReadOnlyList<OhlcPoint>>();
            var checkedCount = 0;
            foreach (var item in instruments)
            {
                checkedCount++;
                try
                {
                    var rows = await _api.GetAllAvailableOhlcPricesAsync(item.Epic, requestResolution, operation.Token);
                    rows = TransformTerminalCandles(rows, terminalResolution);
                    if (!_dataOperations.IsCurrent(operation)) return;
                    if (rows.Count >= minimumCandles) candles[item.Epic] = rows;
                    TerminalStatusText.Text = $"Loading {TerminalTimeframeText().ToLowerInvariant()} candles for {block}: {candles.Count} usable of {checkedCount} checked...";
                    if (candles.Count >= 32 && checkedCount >= 40) break;
                }
                catch (OperationCanceledException) when (operation.Token.IsCancellationRequested)
                {
                    throw;
                }
                catch
                {
                    if (!_dataOperations.IsCurrent(operation)) return;
                    item.Status = "Terminal history n/a";
                }
            }

            if (!_dataOperations.IsCurrent(operation)) return;
            var basket = SyntheticTerminalSelector.SelectBest(block, instruments, candles, periodsPerYear, minimumCandles);
            if (basket is null)
            {
                TerminalStatusText.Text = $"No terminal synthetic instrument could be built for {block}. Checked {checkedCount} stocks; {candles.Count} had enough {TerminalTimeframeText().ToLowerInvariant()} history.";
                return;
            }

            _terminalBasket = basket;
            TerminalHeaderText.Text = $"{basket.Symbol} | {basket.Block}";
            TerminalStatusText.Text = $"{basket.Symbol}: {basket.Components.Count} components, {TerminalTimeframeText()} chart, similarity {basket.SimilarityScore:0.##}, avg vol {basket.AverageVolatilityPct:0.##}%.";
            await RenderTerminalAsync(basket, fit: true);
            await SetTerminalChartModeAsync();
            await SetTerminalIntervalAsync();
            await SetTerminalIndicatorAsync();
        }
        catch (OperationCanceledException) when (operation.Token.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            if (!_dataOperations.IsCurrent(operation)) return;
            TerminalStatusText.Text = "Terminal build failed.";
            MessageBox.Show(ex.Message, "Synthetic terminal", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private string TerminalTimeframeText() =>
        (TerminalTimeframeBox?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Weekly";

    private string SelectedTerminalResolution() =>
        TerminalTimeframeText() switch
        {
            "Daily" => "DAY",
            "4H" => "HOUR_4",
            "2H" => "HOUR_2",
            "6H" => "HOUR_6",
            _ => "WEEK",
        };

    private static string TerminalRequestResolution(string terminalResolution) =>
        terminalResolution is "HOUR_2" or "HOUR_6" ? "HOUR" : terminalResolution;

    private static int TerminalMinimumCandles(string terminalResolution) =>
        terminalResolution == "WEEK" ? 120 : 120;

    private static int TerminalPeriodsPerYear(string terminalResolution) =>
        terminalResolution switch
        {
            "DAY" => 252,
            "HOUR_2" => 252 * 12,
            "HOUR_4" => 252 * 6,
            "HOUR_6" => 252 * 4,
            _ => 52,
        };

    private static IReadOnlyList<OhlcPoint> TransformTerminalCandles(IReadOnlyList<OhlcPoint> source, string terminalResolution) =>
        terminalResolution switch
        {
            "HOUR_2" => AggregateCandles(source, 2),
            "HOUR_6" => AggregateCandles(source, 6),
            _ => source.OrderBy(candle => candle.Time).ToList(),
        };

    private static IReadOnlyList<OhlcPoint> AggregateCandles(IReadOnlyList<OhlcPoint> source, int bucketSize)
    {
        var ordered = source.OrderBy(candle => candle.Time).ToList();
        var result = new List<OhlcPoint>();
        for (var index = 0; index + bucketSize <= ordered.Count; index += bucketSize)
        {
            var bucket = ordered.Skip(index).Take(bucketSize).ToList();
            result.Add(new OhlcPoint(
                bucket[^1].Time,
                bucket[0].Open,
                bucket.Max(candle => candle.High),
                bucket.Min(candle => candle.Low),
                bucket[^1].Close));
        }
        return result;
    }

    private string? SelectedTerminalBlock()
    {
        if (SyntheticBlockBox.SelectedItem is string selected && _instruments.Any(item => item.Group == selected))
        {
            return selected;
        }
        return _instruments
            .Where(CapitalInstrumentTypes.IsStock)
            .GroupBy(item => item.Group)
            .OrderByDescending(group => group.Count())
            .Select(group => group.Key)
            .FirstOrDefault();
    }

    private void ClearSyntheticResults()
    {
        _selectedSyntheticBasket = null;
        SyntheticBasketList.SelectedItem = null;
        _syntheticBaskets.Clear();
        SyntheticDetailText.Text = "Select a synthetic symbol.";
        SyntheticComponentList.ItemsSource = null;
        ClearSyntheticCandlesAsync();
    }

    private async void ClearSyntheticCandlesAsync()
    {
        if (!_syntheticChartReady || SyntheticChartWebView.CoreWebView2 is null) return;
        await SyntheticChartWebView.ExecuteScriptAsync("window.renderSyntheticCandles([]);");
    }

    private void SyntheticBasketList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SyntheticBasketList.SelectedItem is not SyntheticBasket basket) return;
        _selectedSyntheticBasket = basket;
        SyntheticComponentList.ItemsSource = basket.Components;
        ShowSyntheticDetails(basket);
        RenderSyntheticCandlesAsync(basket);
    }

    private void ShowSyntheticDetails(SyntheticBasket basket)
    {
        var updated = basket.LastUpdated?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "n/a";
        SyntheticDetailText.Text =
            $"{basket.Symbol} | {basket.Block} | {basket.BasketPrice:0.####} | avg vol {basket.AverageVolatilityPct:0.##}% | similarity {basket.SimilarityScore:0.##} | updated {updated}";
    }

    private async void RenderSyntheticCandlesAsync(SyntheticBasket basket)
    {
        if (!_syntheticChartReady || SyntheticChartWebView.CoreWebView2 is null) return;
        try
        {
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
        catch
        {
            SyntheticStatusText.Text = "Synthetic chart update failed.";
        }
    }

    private async Task ClearTerminalAsync()
    {
        if (!_terminalChartReady || TerminalChartWebView.CoreWebView2 is null) return;
        await TerminalChartWebView.ExecuteScriptAsync("window.clearTerminal();");
    }

    private async Task RenderTerminalAsync(SyntheticBasket basket, bool fit)
    {
        await RenderTerminalPayloadAsync(SyntheticTerminalChartPayload.Build(basket), fit);
    }

    private async Task RenderTerminalPayloadAsync(SyntheticTerminalPayload payload, bool fit)
    {
        if (!_terminalChartReady || TerminalChartWebView.CoreWebView2 is null) return;
        try
        {
            var json = JsonSerializer.Serialize(payload);
            var function = fit ? "renderTerminal" : "updateTerminal";
            await TerminalChartWebView.ExecuteScriptAsync($"window.{function}({json});");
            await ResizeTerminalChartAsync();
        }
        catch
        {
            TerminalStatusText.Text = "Terminal chart update failed.";
        }
    }

    private async Task ResizeTerminalChartAsync()
    {
        if (!_terminalChartReady || TerminalChartWebView.CoreWebView2 is null) return;
        try
        {
            await TerminalChartWebView.ExecuteScriptAsync("window.resizeTerminal && window.resizeTerminal();");
        }
        catch
        {
            TerminalStatusText.Text = "Terminal chart resize failed.";
        }
    }

    private async Task SetTerminalChartModeAsync()
    {
        if (!_terminalChartReady || TerminalChartWebView.CoreWebView2 is null || TerminalCandleTypeBox is null) return;
        var selected = (TerminalCandleTypeBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Candles";
        var mode = selected switch
        {
            "Heikin Ashi" => "heikin",
            "Line" => "line",
            _ => "candles",
        };
        var json = JsonSerializer.Serialize(mode);
        await TerminalChartWebView.ExecuteScriptAsync($"window.setTerminalChartMode && window.setTerminalChartMode({json});");
    }

    private async Task ToggleTerminalMaAsync(int period, bool visible)
    {
        if (!_terminalChartReady || TerminalChartWebView.CoreWebView2 is null) return;
        await TerminalChartWebView.ExecuteScriptAsync($"window.toggleTerminalMa && window.toggleTerminalMa({period}, {visible.ToString().ToLowerInvariant()});");
    }

    private async void TerminalCandleTypeBox_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        await SetTerminalChartModeAsync();

    private string TerminalIntervalText() =>
        (TerminalIntervalBox?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "1W";

    private string TerminalIndicatorText() =>
        (TerminalIndicatorBox?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "MA";

    private async Task SetTerminalIntervalAsync()
    {
        if (!_terminalChartReady || TerminalChartWebView.CoreWebView2 is null || TerminalIntervalBox is null) return;
        var json = JsonSerializer.Serialize(TerminalIntervalText());
        await TerminalChartWebView.ExecuteScriptAsync($"window.setTerminalInterval && window.setTerminalInterval({json});");
    }

    private async Task SetTerminalIndicatorAsync()
    {
        if (!_terminalChartReady || TerminalChartWebView.CoreWebView2 is null || TerminalIndicatorBox is null) return;
        var json = JsonSerializer.Serialize(TerminalIndicatorText());
        await TerminalChartWebView.ExecuteScriptAsync($"window.setTerminalIndicator && window.setTerminalIndicator({json});");
    }

    private async Task SetTerminalDrawingToolAsync(string tool)
    {
        if (!_terminalChartReady || TerminalChartWebView.CoreWebView2 is null) return;
        var json = JsonSerializer.Serialize(tool);
        await TerminalChartWebView.ExecuteScriptAsync($"window.setTerminalDrawingTool && window.setTerminalDrawingTool({json});");
    }

    private async Task PlaceSyntheticPreviewOrderAsync(string side)
    {
        if (!_terminalChartReady || TerminalChartWebView.CoreWebView2 is null) return;
        var json = JsonSerializer.Serialize(side);
        await TerminalChartWebView.ExecuteScriptAsync($"window.placeSyntheticPreviewOrder && window.placeSyntheticPreviewOrder({json}, 1);");
    }

    private async void TerminalIntervalBox_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        await SetTerminalIntervalAsync();

    private async void TerminalIndicatorBox_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        await SetTerminalIndicatorAsync();

    private async void TerminalMaCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox checkBox) return;
        var period = checkBox == TerminalMa20Check ? 20 : checkBox == TerminalMa50Check ? 50 : 200;
        await ToggleTerminalMaAsync(period, checkBox.IsChecked == true);
    }

    private async void ToggleTerminalComponents_Click(object sender, RoutedEventArgs e)
    {
        if (!_terminalChartReady || TerminalChartWebView.CoreWebView2 is null) return;
        await TerminalChartWebView.ExecuteScriptAsync("window.toggleTerminalComponents && window.toggleTerminalComponents();");
        await ResizeTerminalChartAsync();
    }

    private async void FitTerminalChart_Click(object sender, RoutedEventArgs e)
    {
        if (!_terminalChartReady || TerminalChartWebView.CoreWebView2 is null) return;
        await TerminalChartWebView.ExecuteScriptAsync("window.fitTerminalChart && window.fitTerminalChart();");
    }

    private async void TerminalBuyPreview_Click(object sender, RoutedEventArgs e) =>
        await PlaceSyntheticPreviewOrderAsync("buy");

    private async void TerminalSellPreview_Click(object sender, RoutedEventArgs e) =>
        await PlaceSyntheticPreviewOrderAsync("sell");

    private async void TerminalResetView_Click(object sender, RoutedEventArgs e)
    {
        await SetTerminalDrawingToolAsync("");
        await SetTerminalIntervalAsync();
        await SetTerminalIndicatorAsync();
        await ResizeTerminalChartAsync();
        if (_terminalChartReady && TerminalChartWebView.CoreWebView2 is not null)
        {
            await TerminalChartWebView.ExecuteScriptAsync("window.fitTerminalChart && window.fitTerminalChart();");
        }
    }

    private async void TerminalTimeframeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded || _terminalBasket is null || CurrentWorkspaceMode() != SyntheticTerminalWorkspace.ModeName) return;
        await BuildTerminalAsync();
    }

    private IEnumerable<MarketInstrument> FilterDataset(IEnumerable<MarketInstrument> markets)
    {
        var selected = (DatasetBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Stocks";
        if (selected == "ETFs") return markets.Where(IsEtf);
        if (selected == "Stocks") return markets.Where(CapitalInstrumentTypes.IsStock);
        return markets.Where(item => _workspace.WatchlistEpics.Contains(item.Epic));
    }

    private static bool IsEtf(MarketInstrument item)
    {
        var text = $"{item.Name} {item.Symbol} {item.Type}".ToLowerInvariant();
        return text.Contains("etf") || text.Contains("exchange traded");
    }

    private async void StreamVisible_Click(object sender, RoutedEventArgs e)
    {
        if (_api.Session is null || _streaming is null)
        {
            if (_api.Session is null)
            {
                MessageBox.Show("Connect to Capital.com first.", "Realtime", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _streaming = new CapitalStreamingClient();
            _streaming.QuoteReceived += Streaming_QuoteReceived;
            _streaming.StatusChanged += (_, message) => Dispatcher.Invoke(() => ConnectionText.Text = message);
            await _streaming.ConnectAsync(_api.Session);
        }

        if (CurrentWorkspaceMode() == SyntheticTerminalWorkspace.ModeName)
        {
            await StartTerminalStreamingAsync();
            return;
        }

        var visible = _groups.Where(group => group.IsExpanded).SelectMany(group => group.Instruments).Take(40).ToList();
        if (!visible.Any())
        {
            visible = _instruments.Take(40).ToList();
        }

        var epics = SyntheticLiveUpdate.PrioritizedEpics(visible, _syntheticBaskets);
        await _streaming.SubscribeQuotesAsync(_api.Session, epics);
        await _streaming.SubscribeOhlcAsync(_api.Session, epics, SelectedResolution());
        ConnectionText.Text = $"Streaming {epics.Count} instruments";
    }

    private async void StreamTerminal_Click(object sender, RoutedEventArgs e)
    {
        if (_api.Session is null)
        {
            MessageBox.Show("Connect to Capital.com first.", "Realtime", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (_streaming is null)
        {
            _streaming = new CapitalStreamingClient();
            _streaming.QuoteReceived += Streaming_QuoteReceived;
            _streaming.StatusChanged += (_, message) => Dispatcher.Invoke(() => ConnectionText.Text = message);
            await _streaming.ConnectAsync(_api.Session);
        }

        await StartTerminalStreamingAsync();
    }

    private async Task StartTerminalStreamingAsync()
    {
        if (_api.Session is null) return;
        if (_terminalBasket is null)
        {
            TerminalStatusText.Text = "Build a terminal synthetic instrument before streaming.";
            return;
        }

        var terminalEpics = SyntheticTerminalWorkspace.StreamingEpics(_terminalBasket);
        await _streaming!.SubscribeQuotesAsync(_api.Session, terminalEpics);
        await _streaming.SubscribeOhlcAsync(_api.Session, terminalEpics, TerminalRequestResolution(SelectedTerminalResolution()));
        ConnectionText.Text = $"Streaming synthetic {_terminalBasket.Symbol}";
        TerminalStatusText.Text = $"Streaming {_terminalBasket.Symbol}: {terminalEpics.Count} component epics.";
    }

    private void UpdateSyntheticBasketsForQuote(QuoteUpdate update)
    {
        var selectedBasketMatched = false;
        var selectedBasketChanged = false;
        foreach (var basket in _syntheticBaskets)
        {
            var result = SyntheticLiveUpdate.ApplyQuote(basket, update);
            if (!ReferenceEquals(basket, _selectedSyntheticBasket)) continue;
            selectedBasketMatched |= result.Matched;
            selectedBasketChanged |= result.CandleChanged;
        }
        if (_selectedSyntheticBasket is not null && selectedBasketMatched)
        {
            ShowSyntheticDetails(_selectedSyntheticBasket);
            if (selectedBasketChanged) RenderSyntheticCandlesAsync(_selectedSyntheticBasket);
        }
    }

    private async void Streaming_QuoteReceived(object? sender, QuoteUpdate update)
    {
        SyntheticTerminalPayload? terminalPayload = null;
        await Dispatcher.InvokeAsync(() =>
        {
            if (CurrentWorkspaceMode() == SyntheticTerminalWorkspace.ModeName && _terminalBasket is not null)
            {
                var result = SyntheticTerminalLiveUpdate.Apply(_terminalBasket, update);
                if (result.Matched)
                {
                    terminalPayload = result.Payload;
                    TerminalHeaderText.Text = $"{_terminalBasket.Symbol} | {_terminalBasket.Block}";
                    TerminalStatusText.Text = $"{_terminalBasket.Symbol}: last {_terminalBasket.BasketPrice:0.####}, tick {update.Time.ToLocalTime():HH:mm:ss}.";
                }
                return;
            }

            var item = _instruments.FirstOrDefault(instrument => instrument.Epic == update.Epic);
            if (item is not null)
            {
                item.Bid = update.Bid;
                item.Offer = update.Offer;
                item.Price = update.Price;
                item.LastTickAt = update.Time;
                item.Status = "Live";
                if (update.Price is not null)
                {
                    item.Points.Add(new ChartPoint(update.Time, update.Price.Value));
                    TrimPoints(item);
                    UpdateDerivedValues(item);
                    CheckAlert(item);
                }
                if (_selected == item) ShowSelected(item);
                RedrawCharts();
                UpdateStats();
            }
            UpdateSyntheticBasketsForQuote(update);
        });

        if (terminalPayload is not null)
        {
            var renderTask = await Dispatcher.InvokeAsync(() => RenderTerminalPayloadAsync(terminalPayload, fit: false));
            await renderTask;
        }
    }

    private string SelectedResolution()
    {
        var mode = (ChartModeBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Daily";
        return mode switch
        {
            "2H" or "6H" => "HOUR",
            "4H" => "HOUR_4",
            _ => "DAY",
        };
    }

    private void UpdateDerivedValues(MarketInstrument item)
    {
        if (item.Points.Count == 0) return;
        item.Price = item.Points[^1].Close;
        var first = item.Points[0].Close;
        item.IntradayReturn = first == 0 ? null : decimal.Round(((item.Price ?? first) / first - 1) * 100, 2);
        item.ChangePercent = item.IntradayReturn;
        item.Low = item.Points.Min(point => point.Close);
        item.High = item.Points.Max(point => point.Close);
        item.Sma20 = Average(item.Points.TakeLast(20));
        item.Sma50 = Average(item.Points.TakeLast(50));
    }

    private static decimal? Average(IEnumerable<ChartPoint> points)
    {
        var values = points.Select(point => point.Close).ToList();
        return values.Count == 0 ? null : decimal.Round(values.Average(), 4);
    }

    private static void TrimPoints(MarketInstrument item)
    {
        while (item.Points.Count > 180) item.Points.RemoveAt(0);
    }

    private ApiCredentials ReadCredentials() => new()
    {
        Identifier = IdentifierBox.Text.Trim(),
        Password = PasswordBox.Password,
        ApiKey = ApiKeyBox.Password,
        UseDemo = DemoCheck.IsChecked == true,
    };

    private static void ValidateCredentials(ApiCredentials credentials)
    {
        if (string.IsNullOrWhiteSpace(credentials.Identifier)) throw new InvalidOperationException("Identifier is required.");
        if (string.IsNullOrWhiteSpace(credentials.Password)) throw new InvalidOperationException("API password is required.");
        if (string.IsNullOrWhiteSpace(credentials.ApiKey)) throw new InvalidOperationException("API key is required.");
    }

    private void InstrumentCard_Click(object sender, MouseButtonEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is MarketInstrument item)
        {
            ShowSelected(item);
        }
    }

    private void ShowSelected(MarketInstrument item)
    {
        _selected = item;
        SelectedNameText.Text = item.Name;
        SelectedEpicText.Text = item.Epic;
        BidText.Text = item.Bid?.ToString("0.####") ?? "n/a";
        OfferText.Text = item.Offer?.ToString("0.####") ?? "n/a";
        PriceText.Text = item.Price?.ToString("0.####") ?? "n/a";
        DetailStatusText.Text = $"{item.Group} | {item.Status}";
        Sma20Text.Text = item.Sma20?.ToString("0.####") ?? "n/a";
        Sma50Text.Text = item.Sma50?.ToString("0.####") ?? "n/a";
        TrendText.Text = item.TrendLabel;
        AlertPriceBox.Text = item.AlertPrice?.ToString("0.####") ?? "";
        AlertStatusText.Text = item.AlertPrice is null ? "No alert set for this instrument." : $"Alert set at {item.AlertPrice:0.####}.";
        TicketSummaryText.Text = item.Price is null
            ? "Load a price before preparing a ticket preview."
            : $"{item.Name}: last {item.Price:0.####}. Set quantity, stop, and take profit for planning.";
        DrawChart(DetailChart, item.Points);
    }

    private async void ChartModeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_instruments.Any()) return;
        await LoadHistoryForVisibleAsync(_groups.Where(group => group.IsExpanded).SelectMany(group => group.Instruments).Take(40));
        RedrawCharts();
    }

    private void DatasetBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_instruments.Any()) RebuildGroups();
    }

    private void MiniChart_Loaded(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is MarketInstrument item && sender is Canvas canvas)
        {
            item.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName is nameof(MarketInstrument.Price) or nameof(MarketInstrument.IntradayReturn))
                {
                    Dispatcher.Invoke(() => DrawChart(canvas, item.Points));
                }
            };
            DrawChart(canvas, item.Points);
        }
    }

    private void MiniChart_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is MarketInstrument item && sender is Canvas canvas)
        {
            DrawChart(canvas, item.Points);
        }
    }

    private void DetailChart_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_selected is not null) DrawChart(DetailChart, _selected.Points);
    }

    private void RedrawCharts()
    {
        if (_selected is not null) DrawChart(DetailChart, _selected.Points);
    }

    private void ToggleWatchlist_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if ((sender as FrameworkElement)?.DataContext is not MarketInstrument item) return;
        item.IsWatchlisted = !item.IsWatchlisted;
        if (item.IsWatchlisted && !_workspace.WatchlistEpics.Contains(item.Epic)) _workspace.WatchlistEpics.Add(item.Epic);
        if (!item.IsWatchlisted) _workspace.WatchlistEpics.Remove(item.Epic);
        _workspaceStore.Save(_workspace);
        UpdateStats();
        RebuildGroups();
    }

    private void QuickAlert_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if ((sender as FrameworkElement)?.DataContext is not MarketInstrument item) return;
        ShowSelected(item);
        AlertPriceBox.Text = item.Price?.ToString("0.####") ?? "";
        AlertStatusText.Text = item.Price is null ? "Load a price before saving an alert." : "Review the trigger price and press Save alert.";
    }

    private void SaveAlert_Click(object sender, RoutedEventArgs e)
    {
        if (_selected is null)
        {
            AlertStatusText.Text = "Select an instrument first.";
            return;
        }
        if (!decimal.TryParse(AlertPriceBox.Text, out var alertPrice))
        {
            AlertStatusText.Text = "Enter a valid price.";
            return;
        }
        _selected.AlertPrice = alertPrice;
        _workspace.Alerts[_selected.Epic] = alertPrice;
        _workspaceStore.Save(_workspace);
        AlertStatusText.Text = $"Alert saved at {alertPrice:0.####}.";
    }

    private void ClearAlert_Click(object sender, RoutedEventArgs e)
    {
        if (_selected is null) return;
        _selected.AlertPrice = null;
        _workspace.Alerts.Remove(_selected.Epic);
        _workspaceStore.Save(_workspace);
        AlertPriceBox.Text = "";
        AlertStatusText.Text = "Alert cleared.";
    }

    private void ApplyWorkspaceState(MarketInstrument item)
    {
        item.IsWatchlisted = _workspace.WatchlistEpics.Contains(item.Epic);
        if (_workspace.Alerts.TryGetValue(item.Epic, out var alertPrice)) item.AlertPrice = alertPrice;
    }

    private void CheckAlert(MarketInstrument item)
    {
        if (item.AlertPrice is null || item.Price is null) return;
        if (item.Price >= item.AlertPrice)
        {
            AlertStatusText.Text = $"{item.Name} reached alert {item.AlertPrice:0.####}. Last {item.Price:0.####}.";
            System.Media.SystemSounds.Exclamation.Play();
        }
    }

    private void UpdateStats()
    {
        MarketCountText.Text = _instruments.Count.ToString();
        WatchCountText.Text = _workspace.WatchlistEpics.Count.ToString();
        LiveCountText.Text = _instruments.Count(item => item.Status == "Live").ToString();
    }

    private void UpdateDiscoveryStrip()
    {
        var priced = _instruments.Where(item => item.ChangePercent is not null).ToList();
        TopTradedText.Text = string.Join("  ", _instruments.Take(5).Select(item => string.IsNullOrWhiteSpace(item.Symbol) ? item.Epic : item.Symbol));
        TopRiserText.Text = priced.OrderByDescending(item => item.ChangePercent).Select(item => $"{ShortName(item)} {item.ChangePercent:+0.00;-0.00;0.00}%").FirstOrDefault() ?? "n/a";
        TopFallerText.Text = priced.OrderBy(item => item.ChangePercent).Select(item => $"{ShortName(item)} {item.ChangePercent:+0.00;-0.00;0.00}%").FirstOrDefault() ?? "n/a";
        MostVolatileText.Text = priced
            .OrderByDescending(item => item.High is not null && item.Low is not null && item.Low != 0 ? (item.High - item.Low) / item.Low : 0)
            .Select(item => ShortName(item))
            .FirstOrDefault() ?? "n/a";
    }

    private static string ShortName(MarketInstrument item)
    {
        var value = string.IsNullOrWhiteSpace(item.Symbol) ? item.Name : item.Symbol;
        return value.Length <= 18 ? value : value[..18] + "...";
    }

    private void WorkspaceModeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded && (WorkspaceTitleText is null || DiscoverStrip is null || ResultText is null)) return;
        ApplyWorkspaceMode();
    }

    private void ApplyWorkspaceMode()
    {
        if (WorkspaceTitleText is null || DiscoverStrip is null || ResultText is null || SyntheticPanel is null) return;
        var mode = CurrentWorkspaceMode();
        WorkspaceTitleText.Text = mode switch
        {
            "Discover" => "Discover | movers, volatility, categories",
            "Synthetic" => "Synthetic | volatility-weighted stock baskets",
            "Terminal" => "Terminal | full-screen synthetic instrument",
            "Charts" => "Charts | select any market row",
            "Portfolio" => "Portfolio | local watchlist and live exposure",
            "Calendar" => "Calendar | market events placeholder",
            "Alerts" => "Alerts | local price triggers",
            _ => "Trade",
        };
        DiscoverStrip.Visibility = mode is "Trade" or "Discover" ? Visibility.Visible : Visibility.Collapsed;
        SyntheticPanel.Visibility = mode == "Synthetic" ? Visibility.Visible : Visibility.Collapsed;
        DashboardScrollViewer.Visibility = mode == SyntheticTerminalWorkspace.ModeName ? Visibility.Collapsed : Visibility.Visible;
        TerminalPanel.Visibility = mode == SyntheticTerminalWorkspace.ModeName ? Visibility.Visible : Visibility.Collapsed;
        ApplyTerminalLayout(mode == SyntheticTerminalWorkspace.ModeName);
        if (mode == "Synthetic") RefreshSyntheticBlocks();
        if (mode == SyntheticTerminalWorkspace.ModeName)
        {
            RefreshSyntheticBlocks();
            _ = ResizeTerminalChartAsync();
            if (_terminalBasket is not null) _ = RenderTerminalAsync(_terminalBasket, fit: true);
        }
        ResultText.Text = mode switch
        {
            "Portfolio" => $"{_workspace.WatchlistEpics.Count} watchlist markets saved locally.",
            "Calendar" => "Calendar API is not wired yet; use this pane for future event integration.",
            "Alerts" => $"{_workspace.Alerts.Count} local price alerts saved.",
            "Charts" => "Select a market row to inspect the chart panel.",
            "Discover" => "Discovery strip uses loaded markets: top traded, riser, faller, volatility.",
            "Synthetic" => "Load stocks, choose a block, then build synthetic baskets.",
            "Terminal" => "Load stocks, open Terminal, then build one chart-first synthetic instrument.",
            _ => ResultText.Text,
        };
    }

    private void ApplyTerminalLayout(bool terminalMode)
    {
        if (LeftColumn is null || CenterColumn is null || RightColumn is null) return;
        LeftColumn.Width = terminalMode ? new GridLength(0) : NormalLeftWidth;
        RightColumn.Width = terminalMode ? new GridLength(0) : NormalRightWidth;
        CenterColumn.Width = new GridLength(1, GridUnitType.Star);
    }

    private string CurrentWorkspaceMode() =>
        (WorkspaceModeBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Trade";

    private static void DrawChart(Canvas canvas, IReadOnlyList<ChartPoint> points)
    {
        canvas.Children.Clear();
        if (points.Count < 2 || canvas.ActualWidth <= 0 || canvas.ActualHeight <= 0) return;

        var width = canvas.ActualWidth;
        var height = canvas.ActualHeight;
        var pad = 8.0;
        var values = points.Select(point => (double)point.Close).ToArray();
        var min = values.Min();
        var max = values.Max();
        if (Math.Abs(max - min) < 0.000001)
        {
            min -= 1;
            max += 1;
        }

        var polyline = new Polyline
        {
            Stroke = values[^1] >= values[0] ? new SolidColorBrush(Color.FromRgb(22, 122, 90)) : new SolidColorBrush(Color.FromRgb(177, 66, 66)),
            StrokeThickness = 2,
        };

        for (var index = 0; index < points.Count; index++)
        {
            var x = pad + index / (double)(points.Count - 1) * (width - pad * 2);
            var y = pad + (1 - (values[index] - min) / (max - min)) * (height - pad * 2);
            polyline.Points.Add(new Point(x, y));
        }

        canvas.Children.Add(new Line { X1 = pad, X2 = width - pad, Y1 = height - pad, Y2 = height - pad, Stroke = Brushes.LightGray, StrokeThickness = 1 });
        canvas.Children.Add(polyline);
    }

    protected override void OnClosed(EventArgs e)
    {
        _dataOperations.Dispose();
        base.OnClosed(e);
    }
}

public sealed class InstrumentGroup : INotifyPropertyChanged
{
    private bool _isExpanded;

    public InstrumentGroup(string header, IReadOnlyList<MarketInstrument> instruments)
    {
        Header = $"{header} ({instruments.Count})";
        Instruments = new ObservableCollection<MarketInstrument>(instruments);
    }

    public string Header { get; }
    public ObservableCollection<MarketInstrument> Instruments { get; }

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value) return;
            _isExpanded = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpanded)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
