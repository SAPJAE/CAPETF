using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace CAPETF.Desktop;

public partial class CapComTerminalWindow : Window
{
    private readonly CredentialStore _credentialStore = new();
    private readonly CapitalApiClient _api = new();
    private readonly List<MarketInstrument> _instruments = [];
    private readonly ObservableCollection<TerminalComponentRow> _components = [];
    private IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>> _cachedCandlesByEpic =
        new Dictionary<string, IReadOnlyList<OhlcPoint>>(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>>> _cachedCandlesByEpicByResolution =
        new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>>>(StringComparer.OrdinalIgnoreCase);
    private CapitalStreamingClient? _streaming;
    private SyntheticBasket? _basket;
    private SyntheticTerminalPayload? _pendingPayload;
    private bool _chartReady;

    public CapComTerminalWindow()
    {
        InitializeComponent();
        ComponentsList.ItemsSource = _components;
        LoadSavedCredentials();
        _ = InitializeChartHostAsync();
        SizeChanged += async (_, _) => await InvokeTerminalScriptAsync("window.resizeTerminal && window.resizeTerminal();");
    }

    private void LoadSavedCredentials()
    {
        var saved = _credentialStore.Load();
        ConnectionText.Text = saved is null ? "no saved Capital.com keys" : "saved keys loaded";
    }

    private async void Connect_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var saved = _credentialStore.Load();
            if (saved is null)
            {
                MessageBox.Show("Open the existing CAPETF dashboard once and save Capital.com keys locally.", "cap.com Terminal", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ConnectionText.Text = "connecting...";
            await _api.LoginAsync(saved);
            ConnectionText.Text = "connected";
            StatusText.Text = "Connected to Capital.com.";
        }
        catch (Exception ex)
        {
            ConnectionText.Text = "connection failed";
            StatusText.Text = ex.Message;
        }
    }

    private async void LoadStocks_Click(object sender, RoutedEventArgs e)
    {
        await LoadStocksAsync();
    }

    private async Task LoadStocksAsync()
    {
        try
        {
            StatusText.Text = "Loading cached stock chunks...";
            var cached = DashboardStockChunkLoader.LoadStocks();
            if (cached.Instruments.Count > 0)
            {
                _instruments.Clear();
                _instruments.AddRange(cached.Instruments.Where(CapitalInstrumentTypes.IsStock));
                _cachedCandlesByEpic = cached.OhlcByEpic;
                _cachedCandlesByEpicByResolution = cached.OhlcByEpicAndResolution ??
                    new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>>>(StringComparer.OrdinalIgnoreCase);
                RebuildBlocks();
                var source = cached.SourceAsOf is null ? "" : $" Source date {cached.SourceAsOf:yyyy-MM-dd}.";
                StatusText.Text = $"{_instruments.Count} stocks loaded from {cached.ChunkCount} cached stock chunks.{source}";
                return;
            }

            await LoadStocksFromApiAsync();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Cached chunk load failed; trying Capital.com API. {ex.Message}";
            await LoadStocksFromApiAsync();
        }
    }

    private async void BuildSynthetic_Click(object sender, RoutedEventArgs e)
    {
        await BuildSyntheticAsync();
    }

    private async void BuildNikeSample_Click(object sender, RoutedEventArgs e)
    {
        SearchBox.Text = "NKE";
        if (_instruments.Count == 0)
        {
            await LoadStocksAsync();
        }

        await BuildSyntheticAsync();
    }

    private async Task BuildSyntheticAsync()
    {
        if (_instruments.Count == 0)
        {
            StatusText.Text = "Load stocks before building a synthetic symbol.";
            return;
        }

        var block = SelectedBlock();
        var resolution = SelectedResolution();
        var periodsPerYear = PeriodsPerYear(resolution);
        var minCandles = MinimumCandles(resolution);
        var candidates = SyntheticTerminalSelector.HistoryLoadCandidates(block, _instruments, limit: 500);
        var activeCachedCandles = CachedCandlesForResolution(resolution);
        var candles = BuildCachedCandles(candidates, activeCachedCandles, minCandles, candidateLimit: 500);
        var seedText = SearchBox.Text.Trim();
        var seededCandles = new Dictionary<string, IReadOnlyList<OhlcPoint>>(
            activeCachedCandles.Count > 0 ? activeCachedCandles : candles,
            StringComparer.OrdinalIgnoreCase);

        IReadOnlyList<MarketInstrument> selectionCandidates = SelectSyntheticCandidates(candidates, candles, maxSelection: 36);

        if (candles.Count == 0 || selectionCandidates.Count < 3)
        {
            candles = await LoadApiCandlesAsync(block, candidates.Take(80).ToList(), resolution, minCandles);
            selectionCandidates = SelectSyntheticCandidates(candidates, candles, maxSelection: 36);
            seededCandles = new Dictionary<string, IReadOnlyList<OhlcPoint>>(candles, StringComparer.OrdinalIgnoreCase);
        }

        if (!string.IsNullOrWhiteSpace(seedText))
        {
            var seed = SeededSyntheticSelector.ResolveSeed(seedText, block, _instruments);
            if (seed is not null &&
                (!seededCandles.TryGetValue(seed.Epic, out var seedRows) || seedRows.Count < minCandles))
            {
                var seedLabel = string.IsNullOrWhiteSpace(seed.Symbol) ? seed.Epic : seed.Symbol;
                StatusText.Text = $"Loading Capital.com history for {seedLabel}...";
                var loadedRows = await LoadApiCandlesForInstrumentAsync(seed, resolution, minCandles);
                if (loadedRows.Count >= minCandles)
                {
                    seededCandles[seed.Epic] = loadedRows;
                    candles = new Dictionary<string, IReadOnlyList<OhlcPoint>>(candles, StringComparer.OrdinalIgnoreCase)
                    {
                        [seed.Epic] = loadedRows,
                    };
                }
            }
        }

        StatusText.Text = string.IsNullOrWhiteSpace(seedText)
            ? $"Selecting basket from {selectionCandidates.Count} similar-history candidates..."
            : $"Selecting seeded basket for {seedText}...";
        _basket = !string.IsNullOrWhiteSpace(seedText)
            ? await Task.Run(() => SeededSyntheticSelector.SelectSeededBasket(
                seedText,
                block,
                _instruments,
                seededCandles,
                periodsPerYear))
            : await Task.Run(() => SyntheticTerminalSelector.SelectBest(block, selectionCandidates, candles, periodsPerYear));
        if (_basket is null)
        {
            await ClearTerminalChartAsync();
            StatusText.Text = $"No synthetic basket could be built. {candles.Count} symbols had usable history.";
            return;
        }

        await RenderSyntheticChartAsync(_basket);
        StatusText.Text = $"{_basket.Symbol}: {_basket.Components.Count} legs, similarity {_basket.SimilarityScore:0.##}, average volatility {_basket.AverageVolatilityPct:0.##}%.";
    }

    private static IReadOnlyList<MarketInstrument> SelectSyntheticCandidates(
        IReadOnlyList<MarketInstrument> candidates,
        IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>> candles,
        int maxSelection)
    {
        return candidates
            .Where(item => candles.ContainsKey(item.Epic))
            .OrderBy(item => item.Price is null ? 1 : 0)
            .ThenByDescending(item => candles.TryGetValue(item.Epic, out var rows) ? rows.Count : 0)
            .ThenBy(item => string.IsNullOrWhiteSpace(item.Sector) || item.Sector == "All" ? 1 : 0)
            .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .Take(maxSelection)
            .ToList();
    }

    private async Task<IReadOnlyList<OhlcPoint>> LoadApiCandlesForInstrumentAsync(
        MarketInstrument instrument,
        string resolution,
        int minCandles)
    {
        try
        {
            await EnsureConnectedAsync();
            var rows = await _api.GetAllAvailableOhlcPricesAsync(instrument.Epic, RequestResolution(resolution));
            rows = TransformCandles(rows, resolution);
            return rows.Count >= minCandles ? rows : [];
        }
        catch (Exception ex)
        {
            instrument.Status = $"History n/a: {ex.Message}";
            return [];
        }
    }

    private async Task LoadStocksFromApiAsync()
    {
        await EnsureConnectedAsync();
        StatusText.Text = "Loading Capital.com stocks...";
        _instruments.Clear();
        _cachedCandlesByEpic = new Dictionary<string, IReadOnlyList<OhlcPoint>>(StringComparer.OrdinalIgnoreCase);
        _cachedCandlesByEpicByResolution =
            new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>>>(StringComparer.OrdinalIgnoreCase);
        var markets = await _api.SearchMarketsAsync(SearchBox.Text.Trim());
        foreach (var item in markets.Where(CapitalInstrumentTypes.IsStock))
        {
            _instruments.Add(item);
        }

        RebuildBlocks();
        StatusText.Text = $"{_instruments.Count} stocks loaded from Capital.com API.";
    }

    private IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>> BuildCachedCandles(
        IReadOnlyList<MarketInstrument> candidates,
        IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>> source,
        int minCandles,
        int candidateLimit)
    {
        var candles = new Dictionary<string, IReadOnlyList<OhlcPoint>>();
        var checkedCount = 0;

        StatusText.Text = $"Scanning cached history for {Math.Min(candidates.Count, candidateLimit)} stocks...";
        foreach (var item in candidates.Take(candidateLimit))
        {
            checkedCount++;
            if (source.TryGetValue(item.Epic, out var rows) && rows.Count >= minCandles)
            {
                candles[item.Epic] = rows;
            }
        }

        StatusText.Text = $"Cached history loaded: {candles.Count} usable of {checkedCount} checked.";
        return candles;
    }

    private IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>> CachedCandlesForResolution(string resolution)
    {
        if (_cachedCandlesByEpicByResolution.Count == 0) return _cachedCandlesByEpic;

        var result = new Dictionary<string, IReadOnlyList<OhlcPoint>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (epic, byResolution) in _cachedCandlesByEpicByResolution)
        {
            if (byResolution.TryGetValue(resolution, out var rows) && rows.Count >= 2)
            {
                result[epic] = rows;
            }
        }

        return result.Count > 0 ? result : _cachedCandlesByEpic;
    }

    private async Task<IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>>> LoadApiCandlesAsync(
        string block,
        IReadOnlyList<MarketInstrument> candidates,
        string resolution,
        int minCandles)
    {
        await EnsureConnectedAsync();
        var requestResolution = RequestResolution(resolution);
        var candles = new Dictionary<string, IReadOnlyList<OhlcPoint>>();
        var checkedCount = 0;

        StatusText.Text = $"Scanning Capital.com history for {candidates.Count} stocks in {block}...";
        foreach (var item in candidates)
        {
            checkedCount++;
            try
            {
                var rows = await _api.GetAllAvailableOhlcPricesAsync(item.Epic, requestResolution);
                rows = TransformCandles(rows, resolution);
                if (rows.Count >= minCandles)
                {
                    candles[item.Epic] = rows;
                    StatusText.Text = $"History loaded: {candles.Count} usable of {checkedCount} checked.";
                }
            }
            catch
            {
                item.Status = "History n/a";
            }
        }

        return candles;
    }

    private async void StreamSynthetic_Click(object sender, RoutedEventArgs e)
    {
        await EnsureConnectedAsync();
        if (_basket is null)
        {
            StatusText.Text = "Build a synthetic symbol before streaming.";
            return;
        }

        if (_streaming is null)
        {
            _streaming = new CapitalStreamingClient();
            _streaming.QuoteReceived += Streaming_QuoteReceived;
            _streaming.StatusChanged += (_, message) => Dispatcher.Invoke(() => ConnectionText.Text = message);
            await _streaming.ConnectAsync(_api.Session!);
        }

        var epics = SyntheticTerminalWorkspace.StreamingEpics(_basket);
        await _streaming.SubscribeQuotesAsync(_api.Session!, epics);
        await _streaming.SubscribeOhlcAsync(_api.Session!, epics, RequestResolution(SelectedResolution()));
        ConnectionText.Text = $"Streaming {_basket.Symbol}";
        StatusText.Text = $"Streaming {_basket.Symbol}: {epics.Count} component epics.";
    }

    private void BuyPreview_Click(object sender, RoutedEventArgs e) => PreviewSyntheticOrder("BUY");

    private void SellPreview_Click(object sender, RoutedEventArgs e) => PreviewSyntheticOrder("SELL");

    private async void CandleType_SelectionChanged(object sender, SelectionChangedEventArgs e) => await SetTerminalChartModeAsync();

    private async void Resolution_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        await SetTerminalIntervalAsync();
        if (_basket is not null && _instruments.Count > 0)
        {
            await BuildSyntheticAsync();
        }
    }

    private async void Streaming_QuoteReceived(object? sender, QuoteUpdate update)
    {
        await Dispatcher.InvokeAsync(() =>
        {
            if (_basket is null) return;
            var result = SyntheticTerminalLiveUpdate.Apply(_basket, update);
            if (!result.Matched) return;
            if (result.Payload is not null) _ = SendTerminalPayloadAsync(result.Payload, liveUpdate: true);
            StatusText.Text = $"{_basket.Symbol}: live {_basket.BasketPrice:0.####}, tick {update.Time.ToLocalTime():HH:mm:ss}.";
        });
    }

    private async Task EnsureConnectedAsync()
    {
        if (_api.Session is not null) return;
        var saved = _credentialStore.Load();
        if (saved is null) throw new InvalidOperationException("No saved Capital.com keys found.");
        await _api.LoginAsync(saved);
        ConnectionText.Text = "connected";
    }

    private void RebuildBlocks()
    {
        var blocks = _instruments
            .Where(CapitalInstrumentTypes.IsStock)
            .GroupBy(item => item.Group)
            .OrderByDescending(group => group.Count())
            .Select(group => $"{group.Key}")
            .ToList();

        BlockBox.ItemsSource = blocks;
        if (blocks.Count > 0) BlockBox.SelectedIndex = 0;
    }

    private string SelectedBlock() =>
        BlockBox.SelectedItem?.ToString() ?? _instruments.FirstOrDefault()?.Group ?? "US / USD / Other";

    private string SelectedResolution() =>
        (ResolutionBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Weekly";

    private static string RequestResolution(string resolution) =>
        resolution is "2H" or "6H" ? "HOUR" :
        resolution == "4H" ? "HOUR_4" :
        resolution == "Daily" ? "DAY" :
        "WEEK";

    private static int MinimumCandles(string resolution) => resolution is "2H" or "4H" or "6H" ? 60 : 120;

    private static int PeriodsPerYear(string resolution) =>
        resolution switch
        {
            "2H" => 252 * 4,
            "4H" => 252 * 2,
            "6H" => 252,
            "Daily" => 252,
            _ => 52,
        };

    private static IReadOnlyList<OhlcPoint> TransformCandles(IReadOnlyList<OhlcPoint> source, string resolution) =>
        resolution switch
        {
            "2H" => AggregateCandles(source, 2),
            "6H" => AggregateCandles(source, 6),
            _ => source,
        };

    private static IReadOnlyList<OhlcPoint> AggregateCandles(IReadOnlyList<OhlcPoint> source, int bucketSize)
    {
        var ordered = source.OrderBy(point => point.Time).ToList();
        var result = new List<OhlcPoint>();
        for (var index = 0; index + bucketSize <= ordered.Count; index += bucketSize)
        {
            var bucket = ordered.Skip(index).Take(bucketSize).ToList();
            result.Add(new OhlcPoint(
                bucket[^1].Time,
                bucket[0].Open,
                bucket.Max(point => point.High),
                bucket.Min(point => point.Low),
                bucket[^1].Close));
        }

        return result;
    }

    private async Task InitializeChartHostAsync()
    {
        try
        {
            var terminalPath = Path.Combine(AppContext.BaseDirectory, "Assets", "synthetic-terminal.html");
            if (!File.Exists(terminalPath))
            {
                StatusText.Text = $"Chart file missing: {terminalPath}";
                return;
            }

            await TerminalWebView.EnsureCoreWebView2Async();
            TerminalWebView.NavigationCompleted += async (_, _) =>
            {
                _chartReady = true;
                await InvokeTerminalScriptAsync("window.resizeTerminal && window.resizeTerminal();");
                if (_pendingPayload is not null)
                {
                    await SendTerminalPayloadAsync(_pendingPayload, liveUpdate: false);
                }
            };
            TerminalWebView.Source = new Uri(terminalPath);
            StatusText.Text = "Interactive chart host loading.";
        }
        catch (Exception ex)
        {
            _chartReady = false;
            StatusText.Text = $"Interactive chart host failed: {ex.Message}";
        }
    }

    private async Task RenderSyntheticChartAsync(SyntheticBasket basket)
    {
        var payload = SyntheticTerminalChartPayload.Build(basket);
        SymbolText.Text = $"{payload.Symbol}  {payload.Block}";
        ChartMetaText.Text = $"{payload.CurrencyLabel} | last {basket.BasketPrice:0.####}";
        SyntheticFormulaText.Text = string.Join(Environment.NewLine + "+ ", basket.Components.Select(component =>
            $"{component.Weight / 100m:0.0000} * {component.Instrument.Epic}"));

        _components.Clear();
        foreach (var component in payload.Components) _components.Add(component);

        await SendTerminalPayloadAsync(payload, liveUpdate: false);
        await SetTerminalChartModeAsync();
        await SetTerminalIntervalAsync();
    }

    private async Task SendTerminalPayloadAsync(SyntheticTerminalPayload payload, bool liveUpdate)
    {
        _pendingPayload = payload;
        if (!_chartReady || TerminalWebView.CoreWebView2 is null) return;

        var json = JsonSerializer.Serialize(payload);
        if (liveUpdate)
        {
            await InvokeTerminalScriptAsync($"window.updateTerminal && window.updateTerminal({json});");
        }
        else
        {
            await InvokeTerminalScriptAsync($"window.renderTerminal && window.renderTerminal({json});");
        }
    }

    private async Task ClearTerminalChartAsync()
    {
        _pendingPayload = null;
        SymbolText.Text = "No synthetic symbol";
        ChartMetaText.Text = "No synthetic basket could be built for the current seed and block.";
        SyntheticFormulaText.Text = "No formula yet.";
        OrderPreviewText.Text = "Preview only. No live orders are sent.";
        _components.Clear();
        await InvokeTerminalScriptAsync("window.clearTerminal && window.clearTerminal();");
    }

    private async Task InvokeTerminalScriptAsync(string script)
    {
        try
        {
            if (_chartReady && TerminalWebView.CoreWebView2 is not null)
            {
                await TerminalWebView.ExecuteScriptAsync(script);
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Chart command failed: {ex.Message}";
        }
    }

    private async Task SetTerminalChartModeAsync()
    {
        var mode = SelectedCandleType() == "Heikin Ashi" ? "heikin" : "candles";
        await InvokeTerminalScriptAsync($"window.setTerminalChartMode && window.setTerminalChartMode('{mode}');");
    }

    private string SelectedCandleType() =>
        (CandleTypeBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Candles";

    private async Task SetTerminalIntervalAsync()
    {
        var interval = SelectedResolution() switch
        {
            "Daily" => "1D",
            "6H" => "6H",
            "4H" => "4H",
            "2H" => "2H",
            _ => "1W",
        };
        await InvokeTerminalScriptAsync($"window.setTerminalInterval && window.setTerminalInterval('{interval}');");
    }

    private async void FitChart_Click(object sender, RoutedEventArgs e) => await FitTerminalChartAsync();

    private async void ToggleTicket_Click(object sender, RoutedEventArgs e) =>
        await InvokeTerminalScriptAsync("window.toggleTerminalComponents && window.toggleTerminalComponents();");

    private async Task FitTerminalChartAsync()
    {
        await InvokeTerminalScriptAsync("window.fitTerminalChart && window.fitTerminalChart();");
    }

    private void PreviewSyntheticOrder(string side)
    {
        if (_basket is null)
        {
            OrderPreviewText.Text = "Build a synthetic symbol first.";
            return;
        }

        _ = decimal.TryParse(QuantityBox.Text, out var quantity);
        if (quantity <= 0) quantity = 1m;
        OrderPreviewText.Text = string.Join(Environment.NewLine, _basket.Components.Select(component =>
            $"{side} {quantity * component.Weight / 100m:0.####} x {component.Instrument.Epic}"));
        _ = InvokeTerminalScriptAsync($"window.placeSyntheticPreviewOrder && window.placeSyntheticPreviewOrder('{side}', {quantity});");
    }

    protected override async void OnClosed(EventArgs e)
    {
        if (_streaming is not null)
        {
            await _streaming.DisposeAsync();
        }

        _api.Dispose();
        base.OnClosed(e);
    }
}
