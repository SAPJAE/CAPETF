using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace CAPETF.Desktop;

public partial class CapComTerminalWindow : Window
{
    private readonly CredentialStore _credentialStore = new();
    private readonly SavedSyntheticBasketStore _savedBasketStore = new();
    private readonly CapitalApiClient _api = new();
    private readonly SyntheticHistoryService _history;
    private readonly List<MarketInstrument> _instruments = [];
    private readonly ObservableCollection<TerminalComponentRow> _components = [];
    private readonly Dictionary<TerminalUniverseKind, IReadOnlyList<MarketInstrument>> _instrumentsByUniverse = [];
    private readonly Dictionary<TerminalUniverseKind, IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>>> _candlesByUniverse = [];
    private readonly Dictionary<TerminalUniverseKind, IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>>>> _candlesByUniverseByResolution = [];
    private EtfDataLoadResult? _etfCache;
    private IReadOnlySet<string> _knownEtfEpics = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>> _cachedCandlesByEpic =
        new Dictionary<string, IReadOnlyList<OhlcPoint>>(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>>> _cachedCandlesByEpicByResolution =
        new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>>>(StringComparer.OrdinalIgnoreCase);
    private CapitalStreamingClient? _streaming;
    private SyntheticBasket? _basket;
    private bool _loadingSavedBaskets;
    private SyntheticTerminalPayload? _pendingPayload;
    private bool _chartReady;

    public CapComTerminalWindow()
    {
        InitializeComponent();
        _history = new SyntheticHistoryService(_api);
        StrategyBox.ItemsSource = SyntheticStrategyCatalog.All;
        StrategyBox.DisplayMemberPath = nameof(SyntheticStrategy.Label);
        StrategyBox.SelectedValuePath = nameof(SyntheticStrategy.Kind);
        StrategyBox.SelectedIndex = 0;
        ComponentsList.ItemsSource = _components;
        LoadSavedCredentials();
        RefreshSavedBaskets();
        _ = InitializeChartHostAsync();
        SizeChanged += async (_, _) => await InvokeTerminalScriptAsync("window.resizeTerminal && window.resizeTerminal();");
    }

    private void LoadSavedCredentials()
    {
        var saved = _credentialStore.Load();
        ConnectionText.Text = saved is null ? "no saved Capital.com keys" : $"saved keys loaded for {SavedCredentialLabel(saved)}";
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

            ConnectionText.Text = $"connecting to {SavedCredentialLabel(saved)}...";
            await _api.LoginAsync(saved);
            ConnectionText.Text = $"connected to {SavedCredentialLabel(saved)}";
            StatusText.Text = $"Connected to {SavedCredentialLabel(saved)}. Loading universe...";
            await LoadStocksAsync();
        }
        catch (Exception ex)
        {
            ConnectionText.Text = $"Connection failed: {ex.Message}";
            StatusText.Text = ex.Message;
        }
    }

    private Task LoadStocksAsync() => LoadUniverseAsync(SelectedUniverse());

    private async Task LoadUniverseAsync(TerminalUniverseKind universe)
    {
        try
        {
            var etfCache = EnsureEtfCatalogLoaded();
            if (_instrumentsByUniverse.TryGetValue(universe, out var instruments))
            {
                ApplyUniverse(universe, instruments, _candlesByUniverse[universe], _candlesByUniverseByResolution[universe]);
                StatusText.Text = $"{_instruments.Count} {UniverseLabel(universe).ToLowerInvariant()} loaded from the selected universe cache.";
                return;
            }

            if (universe == TerminalUniverseKind.Stocks)
            {
                StatusText.Text = "Loading cached stock chunks...";
                var cached = DashboardStockChunkLoader.LoadStocks();
                if (cached.Instruments.Count > 0)
                {
                    ApplyUniverse(
                        universe,
                        cached.Instruments.Where(item => TerminalUniverse.Accepts(universe, item, _knownEtfEpics)).ToList(),
                        cached.OhlcByEpic,
                        cached.OhlcByEpicAndResolution ?? EmptyCandlesByResolution());
                    var source = cached.SourceAsOf is null ? "" : $" Source date {cached.SourceAsOf:yyyy-MM-dd}.";
                    StatusText.Text = $"{_instruments.Count} stocks loaded from {cached.ChunkCount} cached stock chunks.{source}";
                    return;
                }
            }
            else
            {
                StatusText.Text = "Loading cached ETFs...";
                if (etfCache.Instruments.Count > 0)
                {
                    var enriched = await EnrichEtfMetadataAsync(etfCache.Instruments);
                    ApplyUniverse(universe, enriched, etfCache.OhlcByEpic, etfCache.OhlcByEpicAndResolution);
                    var source = etfCache.SourceAsOf is null ? "" : $" Source date {etfCache.SourceAsOf:yyyy-MM-dd}.";
                    StatusText.Text = $"{_instruments.Count} ETFs loaded from the cached ETF file.{source}";
                    return;
                }
            }

            await LoadStocksFromApiAsync();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Cached {UniverseLabel(universe).ToLowerInvariant()} load failed; trying Capital.com API. {ex.Message}";
            await LoadStocksFromApiAsync();
        }
    }

    private async void BuildSynthetic_Click(object sender, RoutedEventArgs e)
    {
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
        var strategy = SelectedStrategy();
        var periodsPerYear = PeriodsPerYear(resolution);
        var minCandles = MinimumCandles(resolution);
        var candidates = SyntheticTerminalSelector.HistoryLoadCandidates(block, _instruments.Where(item => TerminalUniverse.Accepts(SelectedUniverse(), item, _knownEtfEpics)).ToList(), limit: 500);
        var activeCachedCandles = CachedCandlesForResolution(resolution);
        var candles = BuildCachedCandles(candidates, activeCachedCandles, minCandles, candidateLimit: 500);
        var seedText = SeedText();
        var isSeededSimilarBuild =
            strategy == SyntheticStrategyKind.SimilarToSelectedSymbol &&
            !string.IsNullOrWhiteSpace(seedText);
        var seededCandles = new Dictionary<string, IReadOnlyList<OhlcPoint>>(
            activeCachedCandles.Count > 0 ? activeCachedCandles : candles,
            StringComparer.OrdinalIgnoreCase);

        IReadOnlyList<MarketInstrument> selectionCandidates = SelectSyntheticCandidates(candidates, candles, maxSelection: 36);
        if (strategy != SyntheticStrategyKind.SimilarToSelectedSymbol)
        {
            selectionCandidates = SelectStrategyCandidates(strategy, candidates, candles, periodsPerYear, maxSelection: 36);
        }

        if (isSeededSimilarBuild)
        {
            var seed = SeededSyntheticSelector.ResolveSeed(seedText, block, _instruments);
            if (seed is not null &&
                (!seededCandles.TryGetValue(seed.Epic, out var seedRows) || seedRows.Count < minCandles))
            {
                var seedLabel = string.IsNullOrWhiteSpace(seed.Symbol) ? seed.Epic : seed.Symbol;
                StatusText.Text = $"Loading Capital.com history for {seedLabel}...";
                var seedHistory = await LoadSelectedHistoryAsync([seed], resolution);
                var loadedRows = seedHistory.CandlesByEpic.TryGetValue(seed.Epic, out var loaded) ? loaded : [];
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
            ? $"Selecting {StrategyLabel(strategy)} basket from {selectionCandidates.Count} candidates..."
            : strategy == SyntheticStrategyKind.SimilarToSelectedSymbol
                ? $"Selecting seeded basket for {seedText}..."
                : $"Selecting {StrategyLabel(strategy)} basket from {selectionCandidates.Count} candidates...";
        _basket = strategy == SyntheticStrategyKind.SimilarToSelectedSymbol && !string.IsNullOrWhiteSpace(seedText)
            ? await Task.Run(() => SeededSyntheticSelector.SelectSeededBasket(
                seedText,
                block,
                _instruments,
                seededCandles,
                periodsPerYear,
                minCandles))
            : await Task.Run(() => SyntheticTerminalSelector.SelectBest(block, selectionCandidates, candles, periodsPerYear, minCandles));
        if (_basket is null)
        {
            await ClearTerminalChartAsync();
            var usableHistoryCount = isSeededSimilarBuild
                ? seededCandles.Count(pair => pair.Value.Count >= minCandles)
                : candles.Count;
            StatusText.Text = $"No synthetic basket could be built. {usableHistoryCount} symbols had usable {resolution} history in {block}.";
            return;
        }

        var selectedComponents = _basket.Components.Select(component => component.Instrument).ToList();
        var selectedHistory = await LoadSelectedHistoryAsync(selectedComponents, resolution);
        _basket = SyntheticHistoryService.BuildSelected(block, selectedComponents, selectedHistory, periodsPerYear, minCandles);
        if (_basket is null)
        {
            await ClearTerminalChartAsync();
            StatusText.Text = $"The selected legs have no usable shared {resolution} history.";
            return;
        }

        await RefreshBasketMarketDetailsAsync(_basket);
        await RenderSyntheticChartAsync(_basket);
        var buildStatus = $"{_basket.Symbol}: {_basket.Components.Count} legs, {HistoryRange(selectedHistory)}, similarity {_basket.SimilarityScore:0.##}, average volatility {_basket.AverageVolatilityPct:0.##}%.";
        StatusText.Text = buildStatus;
        await TryStartStreamingCurrentBasketAsync(buildStatus);
    }

    private void SaveBasket_Click(object sender, RoutedEventArgs e)
    {
        if (_basket is null)
        {
            StatusText.Text = "Build a synthetic basket before saving.";
            return;
        }

        var name = SuggestedSavedBasketName(_basket, SelectedStrategy());
        _savedBasketStore.Save(SavedSyntheticBasket.FromBasket(name, SelectedStrategy(), _basket));
        RefreshSavedBaskets(name);
        StatusText.Text = $"Saved basket {name}.";
    }

    private async void SavedBaskets_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loadingSavedBaskets || SavedBasketsBox.SelectedItem is not SavedSyntheticBasket saved) return;
        await LoadSavedBasketAsync(saved);
    }

    private async Task LoadSavedBasketAsync(SavedSyntheticBasket saved)
    {
        if (_instruments.Count == 0)
        {
            await LoadStocksAsync();
        }

        var resolution = SelectedResolution();
        var periodsPerYear = PeriodsPerYear(resolution);
        var minCandles = MinimumCandles(resolution);
        var selectedInstruments = saved.Components
            .Select(component => _instruments.FirstOrDefault(instrument => string.Equals(instrument.Epic, component.Epic, StringComparison.OrdinalIgnoreCase)))
            .OfType<MarketInstrument>()
            .ToList();
        if (selectedInstruments.Count < 3)
        {
            StatusText.Text = $"Saved basket {saved.Name} could not be loaded because some leg symbols are missing from the universe.";
            return;
        }

        var selectedHistory = await LoadSelectedHistoryAsync(selectedInstruments, resolution);
        _basket = SyntheticHistoryService.BuildSelected(saved.Block, selectedInstruments, selectedHistory, periodsPerYear, minCandles);
        if (_basket is null)
        {
            await ClearTerminalChartAsync();
            StatusText.Text = $"Saved basket {saved.Name} has no usable current history.";
            return;
        }

        _basket = RenameBasket(_basket, saved.Symbol, saved.Block);
        await RefreshBasketMarketDetailsAsync(_basket);
        await RenderSyntheticChartAsync(_basket);
        var loadStatus = $"Loaded saved basket {saved.Name}: {_basket.Components.Count} legs, {HistoryRange(selectedHistory)}.";
        StatusText.Text = loadStatus;
        await TryStartStreamingCurrentBasketAsync(loadStatus);
    }

    private static IReadOnlyList<MarketInstrument> SelectStrategyCandidates(
        SyntheticStrategyKind strategy,
        IReadOnlyList<MarketInstrument> candidates,
        IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>> candles,
        int periodsPerYear,
        int maxSelection)
    {
        var ranked = SyntheticStrategyRanker.Rank(strategy, candidates, candles, periodsPerYear, maxSelection);
        return ranked.Count >= 3 ? ranked.Select(rank => rank.Instrument).ToList() : [];
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

    private Task LoadStocksFromApiAsync() => LoadUniverseFromApiAsync(SelectedUniverse());

    private async Task LoadUniverseFromApiAsync(TerminalUniverseKind universe)
    {
        await EnsureConnectedAsync();
        EnsureEtfCatalogLoaded();
        StatusText.Text = $"Loading Capital.com {UniverseLabel(universe).ToLowerInvariant()}...";
        var markets = await _api.SearchMarketsAsync(SeedText());
        ApplyUniverse(universe, markets.Where(item => TerminalUniverse.Accepts(universe, item, _knownEtfEpics)).ToList(), EmptyCandles(), EmptyCandlesByResolution());
        StatusText.Text = $"{_instruments.Count} {UniverseLabel(universe).ToLowerInvariant()} loaded from Capital.com API.";
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

    private async Task<HistoryLoadResult> LoadSelectedHistoryAsync(
        IReadOnlyList<MarketInstrument> selectedComponents,
        string resolution)
    {
        await EnsureConnectedAsync();
        var progress = new Progress<HistoryLoadProgress>(update =>
        {
            StatusText.Text = $"Loading full {resolution} history for selected leg {update.CompletedComponents} of {update.TotalComponents}: {update.Epic}.";
        });
        return await _history.LoadSelectedAsync(selectedComponents, resolution, progress);
    }

    private static string HistoryRange(HistoryLoadResult history) =>
        history.SharedStart is not null && history.SharedEnd is not null
            ? $"{history.SharedCount} shared candles from {history.SharedStart:yyyy-MM-dd} to {history.SharedEnd:yyyy-MM-dd}"
            : "no shared candles";

    private async Task TryStartStreamingCurrentBasketAsync(string baseStatus)
    {
        if (_basket is null)
        {
            return;
        }

        try
        {
            await StartStreamingCurrentBasketAsync();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"{baseStatus} Live prices unavailable: {ex.Message}";
        }
    }

    private async Task StartStreamingCurrentBasketAsync()
    {
        await EnsureConnectedAsync();
        if (_basket is null) return;

        if (_streaming is null)
        {
            _streaming = new CapitalStreamingClient();
            _streaming.QuoteReceived += Streaming_QuoteReceived;
            _streaming.StatusChanged += (_, message) => Dispatcher.Invoke(() => ConnectionText.Text = message);
            await _streaming.ConnectAsync(_api.Session!);
        }

        var epics = SyntheticTerminalWorkspace.StreamingEpics(_basket);
        await _streaming.SubscribeQuotesAsync(_api.Session!, epics);
        await _streaming.SubscribeOhlcAsync(_api.Session!, epics, SyntheticHistoryService.RequestResolution(SelectedResolution()));
        ConnectionText.Text = $"Streaming {_basket.Symbol}";
        StatusText.Text = $"Streaming {_basket.Symbol}: {epics.Count} component epics.";
    }

    private async Task RefreshBasketMarketDetailsAsync(SyntheticBasket basket)
    {
        try
        {
            await EnsureConnectedAsync();
        }
        catch
        {
            return;
        }

        foreach (var component in basket.Components)
        {
            try
            {
                var details = await _api.GetMarketDetailsAsync(component.Instrument.Epic);
                if (details is not null)
                {
                    ApplyMarketDetails(component.Instrument, details);
                    component.NotifyInstrumentPriceChanged();
                }
            }
            catch (Exception ex)
            {
                component.Instrument.Status = $"Market snapshot n/a: {ex.Message}";
            }
        }

        SyntheticQuoteCalculator.Refresh(basket);
    }

    private static void ApplyMarketDetails(MarketInstrument target, MarketInstrument details)
    {
        if (details.Bid is > 0) target.Bid = details.Bid;
        if (details.Offer is > 0) target.Offer = details.Offer;
        if (details.Price is > 0)
        {
            target.Price = details.Price;
            target.LastTickAt = DateTimeOffset.UtcNow;
        }

        if (details.LotSize is > 0) target.LotSize = details.LotSize;
        if (details.MinDealSize is > 0) target.MinDealSize = details.MinDealSize;
        if (details.MinSizeIncrement is > 0) target.MinSizeIncrement = details.MinSizeIncrement;
        if (!string.IsNullOrWhiteSpace(details.Status)) target.Status = details.Status;
    }

    private void BuyPreview_Click(object sender, RoutedEventArgs e) => PreviewSyntheticOrder("BUY");

    private void SellPreview_Click(object sender, RoutedEventArgs e) => PreviewSyntheticOrder("SELL");

    private async void CandleType_SelectionChanged(object sender, SelectionChangedEventArgs e) => await SetTerminalChartModeAsync();

    private async void Resolution_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        await SetTerminalIntervalAsync();
        if (_basket is not null)
        {
            await ReloadSelectedBasketHistoryAsync(_basket);
        }
    }

    private async Task ReloadSelectedBasketHistoryAsync(SyntheticBasket existingBasket)
    {
        var resolution = SelectedResolution();
        var selectedComponents = existingBasket.Components.Select(component => component.Instrument).ToList();
        var history = await LoadSelectedHistoryAsync(selectedComponents, resolution);
        var rebuilt = SyntheticHistoryService.BuildSelected(
            existingBasket.Block,
            selectedComponents,
            history,
            PeriodsPerYear(resolution),
            MinimumCandles(resolution));
        if (rebuilt is null)
        {
            StatusText.Text = $"The selected legs have no usable shared {resolution} history.";
            return;
        }

        _basket = RenameBasket(rebuilt, existingBasket.Symbol, existingBasket.Block);
        await RefreshBasketMarketDetailsAsync(_basket);
        await RenderSyntheticChartAsync(_basket);
        var reloadStatus = $"{_basket.Symbol}: reloaded {resolution} history for the same {_basket.Components.Count} legs, {HistoryRange(history)}.";
        StatusText.Text = reloadStatus;
        await TryStartStreamingCurrentBasketAsync(reloadStatus);
    }

    private async void Streaming_QuoteReceived(object? sender, QuoteUpdate update)
    {
        await Dispatcher.InvokeAsync(() =>
        {
            if (_basket is null) return;
            var result = SyntheticTerminalLiveUpdate.Apply(_basket, update);
            if (!result.Matched) return;
            if (result.Payload is not null) _ = SendTerminalPayloadAsync(result.Payload, liveUpdate: true);
            ChartMetaText.Text = $"{result.Payload?.CurrencyLabel ?? ""} | bid {FormatQuote(_basket.BidPrice)} | ask {FormatQuote(_basket.AskPrice)}";
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
            .GroupBy(item => item.Group)
            .OrderByDescending(group => group.Count())
            .Select(group => $"{group.Key}")
            .ToList();

        BlockBox.ItemsSource = blocks;
        if (blocks.Count > 0) BlockBox.SelectedIndex = 0;
        RebuildSeedOptions();
    }

    private void RefreshSavedBaskets(string? selectedName = null)
    {
        _loadingSavedBaskets = true;
        try
        {
            var saved = _savedBasketStore.LoadAll();
            SavedBasketsBox.ItemsSource = saved;
            SavedBasketsBox.DisplayMemberPath = nameof(SavedSyntheticBasket.Name);
            SavedBasketsBox.SelectedValuePath = nameof(SavedSyntheticBasket.Id);
            if (!string.IsNullOrWhiteSpace(selectedName))
            {
                SavedBasketsBox.SelectedItem = saved.FirstOrDefault(item => string.Equals(item.Name, selectedName, StringComparison.OrdinalIgnoreCase));
            }
        }
        finally
        {
            _loadingSavedBaskets = false;
        }
    }

    private void RebuildSeedOptions()
    {
        if (SearchBox is null) return;
        var current = SearchBox.Text;
        var block = SelectedBlock();
        var options = SeedSearchOptionBuilder.BuildOptions(_instruments, block);
        SearchBox.ItemsSource = options;
        SearchBox.Text = current;
    }

    private void BlockBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => RebuildSeedOptions();

    private async void UniverseBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_api.Session is null) return;
        await LoadUniverseAsync(SelectedUniverse());
        _basket = null;
        await ClearTerminalChartAsync();
    }

    private string SeedText()
    {
        var text = SearchBox.Text.Trim();
        var delimiter = text.IndexOf(" | ", StringComparison.Ordinal);
        return delimiter > 0 ? text[..delimiter].Trim() : text;
    }

    private string SelectedBlock() =>
        BlockBox.SelectedItem?.ToString() ?? _instruments.FirstOrDefault()?.Group ?? "US / USD / Other";

    private TerminalUniverseKind SelectedUniverse() =>
        (UniverseBox.SelectedItem as ComboBoxItem)?.Content?.ToString() == "ETFs"
            ? TerminalUniverseKind.ETFs
            : TerminalUniverseKind.Stocks;

    private static string UniverseLabel(TerminalUniverseKind universe) =>
        universe == TerminalUniverseKind.ETFs ? "ETFs" : "Stocks";

    private EtfDataLoadResult EnsureEtfCatalogLoaded()
    {
        _etfCache ??= DashboardEtfDataLoader.LoadEtfs();
        _knownEtfEpics = _etfCache.KnownEtfEpics;
        return _etfCache;
    }

    private async Task<IReadOnlyList<MarketInstrument>> EnrichEtfMetadataAsync(IReadOnlyList<MarketInstrument> instruments)
    {
        if (_api.Session is null) return instruments;

        var enriched = new List<MarketInstrument>(instruments.Count);
        foreach (var instrument in instruments)
        {
            if (!_knownEtfEpics.Contains(instrument.Epic) || !EtfMetadataMerger.NeedsEnrichment(instrument))
            {
                enriched.Add(instrument);
                continue;
            }

            try
            {
                var details = await _api.GetMarketDetailsAsync(instrument.Epic);
                enriched.Add(details is null ? instrument : EtfMetadataMerger.Merge(instrument, details));
            }
            catch
            {
                enriched.Add(instrument);
            }
        }

        return enriched;
    }

    private void ApplyUniverse(
        TerminalUniverseKind universe,
        IReadOnlyList<MarketInstrument> instruments,
        IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>> candles,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>>> candlesByResolution)
    {
        var accepted = instruments.Where(item => TerminalUniverse.Accepts(universe, item, _knownEtfEpics)).ToList();
        _instrumentsByUniverse[universe] = accepted;
        _candlesByUniverse[universe] = candles;
        _candlesByUniverseByResolution[universe] = candlesByResolution;
        _instruments.Clear();
        _instruments.AddRange(accepted);
        _cachedCandlesByEpic = candles;
        _cachedCandlesByEpicByResolution = candlesByResolution;
        RebuildBlocks();
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>> EmptyCandles() =>
        new Dictionary<string, IReadOnlyList<OhlcPoint>>(StringComparer.OrdinalIgnoreCase);

    private static IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>>> EmptyCandlesByResolution() =>
        new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>>>(StringComparer.OrdinalIgnoreCase);

    private string SelectedResolution() =>
        (ResolutionBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Weekly";

    private SyntheticStrategyKind SelectedStrategy() =>
        StrategyBox.SelectedValue is SyntheticStrategyKind kind ? kind : SyntheticStrategyKind.SimilarToSelectedSymbol;

    private static string StrategyLabel(SyntheticStrategyKind kind) =>
        SyntheticStrategyCatalog.All.FirstOrDefault(strategy => strategy.Kind == kind)?.Label ?? kind.ToString();

    private static string SuggestedSavedBasketName(SyntheticBasket basket, SyntheticStrategyKind strategy)
    {
        var suffix = strategy == SyntheticStrategyKind.SimilarToSelectedSymbol ? "SIMILAR" : strategy.ToString().ToUpperInvariant();
        return $"{basket.Symbol}-{suffix}";
    }

    private static SyntheticBasket RenameBasket(SyntheticBasket source, string symbol, string block)
    {
        var renamed = new SyntheticBasket
        {
            Symbol = symbol,
            Block = block,
            AverageVolatilityPct = source.AverageVolatilityPct,
            SimilarityScore = source.SimilarityScore,
            BasketPrice = source.BasketPrice,
            BidPrice = source.BidPrice,
            AskPrice = source.AskPrice,
            LastPrice = source.LastPrice,
            LastUpdated = source.LastUpdated,
        };
        foreach (var component in source.Components) renamed.Components.Add(component);
        foreach (var candle in source.Candles) renamed.Candles.Add(candle);
        return renamed;
    }

    private static int MinimumCandles(string resolution) =>
        resolution switch
        {
            "2H" => 30,
            "4H" => 16,
            "6H" => 10,
            _ => 120,
        };

    private static int PeriodsPerYear(string resolution) =>
        resolution switch
        {
            "2H" => 252 * 4,
            "4H" => 252 * 2,
            "6H" => 252,
            "Daily" => 252,
            _ => 52,
        };

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
        ChartMetaText.Text = $"{payload.CurrencyLabel} | bid {FormatQuote(payload.BidPrice)} | ask {FormatQuote(payload.AskPrice)}";
        SyntheticFormulaText.Text = string.Join(Environment.NewLine + "+ ", basket.Components.Select(component =>
            $"{SyntheticOrderSizing.FormatDisplayMultiplier(component)} * {component.Instrument.Epic}"));

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

    private static string FormatQuote(decimal? value) => value?.ToString("0.#####") ?? "n/a";

    private static string SavedCredentialLabel(ApiCredentials credentials) =>
        credentials.UseDemo ? "Capital.com demo" : "Capital.com live";

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

    private async void GoToRealtime_Click(object sender, RoutedEventArgs e) =>
        await InvokeTerminalScriptAsync("window.goToRealtime && window.goToRealtime();");

    private async void ZoomIn_Click(object sender, RoutedEventArgs e) =>
        await InvokeTerminalScriptAsync("window.zoomTerminal && window.zoomTerminal(1.25);");

    private async void ZoomOut_Click(object sender, RoutedEventArgs e) =>
        await InvokeTerminalScriptAsync("window.zoomTerminal && window.zoomTerminal(0.8);");

    private async void PanLeft_Click(object sender, RoutedEventArgs e) =>
        await InvokeTerminalScriptAsync("window.panTerminal && window.panTerminal(-25);");

    private async void PanRight_Click(object sender, RoutedEventArgs e) =>
        await InvokeTerminalScriptAsync("window.panTerminal && window.panTerminal(25);");

    private async void ResetChart_Click(object sender, RoutedEventArgs e) =>
        await InvokeTerminalScriptAsync("window.resetTerminalView && window.resetTerminalView();");

    private async void ToggleTicket_Click(object sender, RoutedEventArgs e) =>
        await InvokeTerminalScriptAsync("window.toggleTerminalComponents && window.toggleTerminalComponents();");

    private async void PriceLines_Changed(object sender, RoutedEventArgs e)
    {
        var visible = PriceLinesCheck?.IsChecked == true ? "true" : "false";
        await InvokeTerminalScriptAsync($"window.togglePriceLines && window.togglePriceLines({visible});");
    }

    private async void Ma_Changed(object sender, RoutedEventArgs e)
    {
        if (sender == TerminalMa20Check) await ToggleTerminalMaAsync(20, TerminalMa20Check.IsChecked == true);
        if (sender == TerminalMa50Check) await ToggleTerminalMaAsync(50, TerminalMa50Check.IsChecked == true);
        if (sender == TerminalMa200Check) await ToggleTerminalMaAsync(200, TerminalMa200Check.IsChecked == true);
    }

    private async Task ToggleTerminalMaAsync(int period, bool visible)
    {
        await InvokeTerminalScriptAsync($"window.toggleTerminalMa && window.toggleTerminalMa({period}, {visible.ToString().ToLowerInvariant()});");
    }

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
        {
            var raw = quantity * component.FormulaMultiplier;
            var legSide = raw >= 0 ? side : side == "BUY" ? "SELL" : "BUY";
            var executable = SyntheticOrderSizing.ExecutableLegQuantity(component, quantity);
            return $"{legSide} {executable:0.####} x {component.Instrument.Epic} (calc {raw:0.##})";
        }));
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
