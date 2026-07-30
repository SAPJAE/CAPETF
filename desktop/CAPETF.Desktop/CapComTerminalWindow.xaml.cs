using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;

namespace CAPETF.Desktop;

public partial class CapComTerminalWindow : Window
{
    private readonly CredentialStore _credentialStore = new();
    private readonly SavedSyntheticBasketStore _savedBasketStore = new();
    private readonly SavedBasketDeletionCoordinator _savedBasketDeletion;
    private readonly CapitalApiClient _api = new();
    private readonly SyntheticHistoryService _history;
    private readonly SyntheticMarginPreviewService _marginPreview;
    private readonly SyntheticPreflightMarketSnapshotLoader _preflightMarketSnapshots;
    private readonly SyntheticTradingHostCoordinator _tradingCoordinator;
    private readonly SyntheticTradingWindowLifecycleCoordinator _tradingLifecycle = new();
    private readonly TerminalOperationState _operationState = new();
    private readonly WindowLifetime _windowLifetime = new();
    private readonly List<MarketInstrument> _instruments = [];
    private readonly ObservableCollection<TerminalComponentRow> _components = [];
    private readonly Dictionary<TerminalUniverseKind, IReadOnlyList<MarketInstrument>> _instrumentsByUniverse = [];
    private readonly Dictionary<TerminalUniverseKind, IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>>> _candlesByUniverse = [];
    private readonly Dictionary<TerminalUniverseKind, IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>>>> _candlesByUniverseByResolution = [];
    private readonly EtfCatalogCache _etfCatalog = new();
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
    private bool _streamReconnectScheduled;
    private CancellationTokenSource? _marginPreviewRefresh;
    private decimal _marginPreviewNotional = 300m;

    public CapComTerminalWindow()
    {
        InitializeComponent();
        _savedBasketDeletion = new SavedBasketDeletionCoordinator(_savedBasketStore);
        OperationProgressPanel.DataContext = _operationState;
        _history = new SyntheticHistoryService(_api);
        _marginPreview = new SyntheticMarginPreviewService(new CapitalApiSyntheticMarginDataSource(_api));
        _preflightMarketSnapshots = new SyntheticPreflightMarketSnapshotLoader(_api.GetMarketDetailsAsync);
        _tradingCoordinator = SyntheticTradingComposition.CreateCoordinator(
            new CapitalTradingGateway(_api),
            SyntheticTradingComposition.DefaultExecutionStorePath(),
            () => _api.IsDemoTradingSession,
            cancellationToken => _api.GetOpenPositionsAsync(cancellationToken));
        StrategyBox.ItemsSource = SyntheticStrategyCatalog.All;
        StrategyBox.DisplayMemberPath = nameof(SyntheticStrategy.Label);
        StrategyBox.SelectedValuePath = nameof(SyntheticStrategy.Kind);
        StrategyBox.SelectedIndex = 0;
        ComponentsList.ItemsSource = _components;
        LoadSavedCredentials();
        RefreshSavedBaskets();
        _ = InitializeChartHostAsync(_windowLifetime.Token);
        SizeChanged += async (_, _) => await InvokeTerminalScriptAsync("window.resizeTerminal && window.resizeTerminal();");
    }

    private void LoadSavedCredentials()
    {
        var saved = _credentialStore.Load();
        ConnectionText.Text = saved is null ? "no saved Capital.com keys" : $"saved keys loaded for {SavedCredentialLabel(saved)}";
        TradingModeText.Text = saved?.UseDemo == true ? "DEMO LOGIN REQUIRED" : "TRADING DISABLED";
    }

    private async void Connect_Click(object sender, RoutedEventArgs e)
    {
        var saved = _credentialStore.Load();
        if (saved is null)
        {
            MessageBox.Show("Open the existing CAPETF dashboard once and save Capital.com keys locally.", "cap.com Terminal", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        await RunOperationAsync("Connecting to Capital.com", async cancellationToken =>
        {
            ConnectionText.Text = $"connecting to {SavedCredentialLabel(saved)}...";
            await _api.LoginAsync(saved, cancellationToken);
            await ResetMarginPreviewAfterLoginAsync();
            await PublishTerminalTradingModeAsync();
            await _tradingCoordinator.ReconnectAsync(PublishTerminalExecutionsAsync, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            ConnectionText.Text = $"connected to {SavedCredentialLabel(saved)}";
            StatusText.Text = $"Connected to {SavedCredentialLabel(saved)}. Loading universe...";
            await LoadStocksAsync(cancellationToken);
        });
    }

    private Task LoadStocksAsync(CancellationToken cancellationToken) => LoadUniverseAsync(SelectedUniverse(), cancellationToken);

    private async Task LoadUniverseAsync(TerminalUniverseKind universe, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
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
                if (etfCache is not null && etfCache.Instruments.Count > 0)
                {
                    var enriched = TerminalUniverseLoadPolicy.RequiresEtfMetadataEnrichment(universe, etfCache.Instruments)
                        ? await EnrichEtfMetadataAsync(etfCache.Instruments, cancellationToken)
                        : etfCache.Instruments;
                    ApplyUniverse(universe, enriched, etfCache.OhlcByEpic, etfCache.OhlcByEpicAndResolution);
                    var source = etfCache.SourceAsOf is null ? "" : $" Source date {etfCache.SourceAsOf:yyyy-MM-dd}.";
                    StatusText.Text = $"{_instruments.Count} ETFs loaded from the cached ETF file.{source}";
                    return;
                }
            }

            await LoadStocksFromApiAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Cached {UniverseLabel(universe).ToLowerInvariant()} load failed; trying Capital.com API. {ex.Message}";
            await LoadStocksFromApiAsync(cancellationToken);
        }
    }

    private async void BuildSynthetic_Click(object sender, RoutedEventArgs e)
    {
        await RunOperationAsync("Building synthetic basket", BuildSyntheticAsync);
    }

    private async Task BuildSyntheticAsync(CancellationToken cancellationToken)
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
        var candles = BuildCachedCandles(candidates, activeCachedCandles, resolution, minCandles, candidateLimit: 500, cancellationToken);
        var seedText = SeedText();
        candles = await SyntheticTerminalBuildPolicy.LoadCandidateHistoryFallbackAsync(
            strategy,
            seedText,
            candidates,
            candles,
            maximumCandidates: 12,
            selected => LoadCandidateHistoryAsync(selected, resolution, cancellationToken));
        var isSeededSimilarBuild =
            strategy == SyntheticStrategyKind.SimilarToSelectedSymbol &&
            !string.IsNullOrWhiteSpace(seedText);
        var seededCandles = new Dictionary<string, IReadOnlyList<OhlcPoint>>(
            activeCachedCandles.Count > 0 ? activeCachedCandles : candles,
            StringComparer.OrdinalIgnoreCase);

        IReadOnlyList<MarketInstrument> selectionCandidates = SelectSyntheticCandidates(candidates, candles, maxSelection: 36);
        if (strategy != SyntheticStrategyKind.SimilarToSelectedSymbol)
        {
            var longTermCandles = resolution == "Weekly" ? candles : CachedCandlesForResolution("Weekly");
            selectionCandidates = SyntheticStrategyCandidatePool.Select(
                strategy,
                candidates,
                candles,
                periodsPerYear,
                longTermCandles,
                fallbackPeriodsPerYear: 52);
        }

        if (isSeededSimilarBuild)
        {
            var seed = SeededSyntheticSelector.ResolveSeed(seedText, block, _instruments);
            if (seed is not null &&
                (!seededCandles.TryGetValue(seed.Epic, out var seedRows) ||
                 SyntheticHistoryService.DistinctAlignmentKeyCount(seedRows, resolution) < minCandles))
            {
                var seedLabel = string.IsNullOrWhiteSpace(seed.Symbol) ? seed.Epic : seed.Symbol;
                StatusText.Text = $"Loading Capital.com history for {seedLabel}...";
                var seedHistory = await LoadSelectedHistoryAsync([seed], resolution, cancellationToken);
                var loadedRows = seedHistory.CandlesByEpic.TryGetValue(seed.Epic, out var loaded) ? loaded : [];
                if (SyntheticHistoryService.DistinctAlignmentKeyCount(loadedRows, resolution) >= minCandles)
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
        _operationState.BeginStage("Selecting basket");
        await Task.Yield();
        _basket = strategy == SyntheticStrategyKind.SimilarToSelectedSymbol && !string.IsNullOrWhiteSpace(seedText)
            ? await Task.Run(() => SeededSyntheticSelector.SelectSeededBasket(
                seedText,
                block,
                _instruments,
                seededCandles,
                periodsPerYear,
                minCandles), cancellationToken)
            : await Task.Run(() => SyntheticTerminalSelector.SelectBest(block, selectionCandidates, candles, periodsPerYear, minCandles), cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (_basket is null)
        {
            await ClearTerminalChartAsync();
            var usableHistoryCount = isSeededSimilarBuild
                ? seededCandles.Count(pair => SyntheticHistoryService.DistinctAlignmentKeyCount(pair.Value, resolution) >= minCandles)
                : candles.Count;
            StatusText.Text = $"No synthetic basket could be built. {usableHistoryCount} symbols had usable {resolution} history in {block}.";
            return;
        }

        var selectedComponents = _basket.Components.Select(component => component.Instrument).ToList();
        var selectedHistory = await LoadSelectedHistoryAsync(selectedComponents, resolution, cancellationToken);
        _operationState.BeginStage("Building selected basket");
        await Task.Yield();
        _basket = SyntheticHistoryService.BuildSelected(block, selectedComponents, selectedHistory, resolution, periodsPerYear, minCandles);
        if (_basket is null)
        {
            await ClearTerminalChartAsync();
            StatusText.Text = $"The selected legs have no usable shared {resolution} history.";
            return;
        }

        await RefreshBasketMarketDetailsAsync(_basket, cancellationToken);
        _operationState.BeginStage("Rendering synthetic chart");
        await Task.Yield();
        await RenderSyntheticChartAsync(_basket);
        var buildStatus = $"{_basket.Symbol}: {_basket.Components.Count} legs, {HistoryRange(selectedHistory)}, similarity {_basket.SimilarityScore:0.##}, average volatility {_basket.AverageVolatilityPct:0.##}%.";
        StatusText.Text = buildStatus;
        await TryStartStreamingCurrentBasketAsync(buildStatus, cancellationToken);
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
        UpdateDeleteBasketButtonState();
        if (_loadingSavedBaskets || SavedBasketsBox.SelectedItem is not SavedSyntheticBasket saved) return;
        await RunOperationAsync($"Loading saved basket {saved.Name}", cancellationToken => LoadSavedBasketAsync(saved, cancellationToken));
    }

    private void DeleteBasket_Click(object sender, RoutedEventArgs e)
    {
        if (SavedBasketsBox.SelectedItem is not SavedSyntheticBasket saved)
        {
            UpdateDeleteBasketButtonState();
            StatusText.Text = "Select a saved basket to delete.";
            return;
        }

        var result = MessageBox.Show($"Delete saved basket {saved.Name}?", "Delete Saved Basket", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        var deletion = _savedBasketDeletion.DeleteConfirmed(saved, _basket, _pendingPayload);
        if (!deletion.Deleted)
        {
            StatusText.Text = $"Could not delete saved basket {saved.Name}.";
            RefreshSavedBaskets(deletion.SavedBaskets);
            return;
        }

        RefreshSavedBaskets(deletion.SavedBaskets);
        StatusText.Text = $"Deleted saved basket {saved.Name}.";
    }

    private async Task LoadSavedBasketAsync(SavedSyntheticBasket saved, CancellationToken cancellationToken)
    {
        if (_instruments.Count == 0)
        {
            await LoadStocksAsync(cancellationToken);
        }

        var resolution = SelectedResolution();
        var periodsPerYear = PeriodsPerYear(resolution);
        var minCandles = MinimumCandles(resolution);
        var resolvedInstruments = saved.Components
            .Select(component => _instruments.FirstOrDefault(instrument => string.Equals(instrument.Epic, component.Epic, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        if (resolvedInstruments.Count != saved.Components.Count || resolvedInstruments.Any(instrument => instrument is null))
        {
            await ClearTerminalChartAsync();
            StatusText.Text = $"Saved basket {saved.Name} could not be loaded because some leg symbols are missing from the universe.";
            return;
        }
        var selectedInstruments = resolvedInstruments.Select(instrument => instrument!).ToList();

        var selectedHistory = await LoadSelectedHistoryAsync(selectedInstruments, resolution, cancellationToken);
        _operationState.BeginStage("Building selected basket");
        await Task.Yield();
        var restored = SavedSyntheticBasketRestorer.Restore(
            saved,
            selectedInstruments,
            selectedHistory,
            resolution,
            periodsPerYear,
            minCandles);
        if (restored is null)
        {
            await ClearTerminalChartAsync();
            StatusText.Text = $"Saved basket {saved.Name} has no usable current history.";
            return;
        }

        _basket = restored.Basket;
        StrategyBox.SelectedValue = restored.Strategy;
        await RefreshBasketMarketDetailsAsync(_basket, cancellationToken);
        _operationState.BeginStage("Rendering synthetic chart");
        await Task.Yield();
        await RenderSyntheticChartAsync(_basket);
        var loadStatus = $"Loaded saved basket {saved.Name}: {_basket.Components.Count} legs, {HistoryRange(selectedHistory)}.";
        StatusText.Text = loadStatus;
        await TryStartStreamingCurrentBasketAsync(loadStatus, cancellationToken);
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

    private Task LoadStocksFromApiAsync(CancellationToken cancellationToken) =>
        LoadUniverseFromApiAsync(SelectedUniverse(), cancellationToken);

    private async Task LoadUniverseFromApiAsync(TerminalUniverseKind universe, CancellationToken cancellationToken)
    {
        await EnsureConnectedAsync(cancellationToken);
        EnsureEtfCatalogLoaded();
        StatusText.Text = $"Loading Capital.com {UniverseLabel(universe).ToLowerInvariant()}...";
        var markets = await _api.SearchMarketsAsync(TerminalUniverseLoadPolicy.ApiSearchTerm(universe, SeedText()), cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        var normalized = TerminalUniverseLoadPolicy.NormalizeApiFallback(universe, markets, _knownEtfEpics);
        ApplyUniverse(universe, normalized, EmptyCandles(), EmptyCandlesByResolution());
        StatusText.Text = $"{_instruments.Count} {UniverseLabel(universe).ToLowerInvariant()} loaded from Capital.com API.";
    }

    private IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>> BuildCachedCandles(
        IReadOnlyList<MarketInstrument> candidates,
        IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>> source,
        string resolution,
        int minCandles,
        int candidateLimit,
        CancellationToken cancellationToken)
    {
        var candles = new Dictionary<string, IReadOnlyList<OhlcPoint>>();
        var checkedCount = 0;

        var total = Math.Min(candidates.Count, candidateLimit);
        _operationState.BeginStage("Scanning cached history", total);
        StatusText.Text = $"Scanning cached history for {total} stocks...";
        foreach (var item in candidates.Take(candidateLimit))
        {
            cancellationToken.ThrowIfCancellationRequested();
            checkedCount++;
            _operationState.Report("Scanning cached history", checkedCount, total);
            if (source.TryGetValue(item.Epic, out var rows) &&
                SyntheticHistoryService.DistinctAlignmentKeyCount(rows, resolution) >= minCandles)
            {
                candles[item.Epic] = rows;
            }
        }

        StatusText.Text = $"Cached history loaded: {candles.Count} usable of {checkedCount} checked.";
        return candles;
    }

    private IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>> CachedCandlesForResolution(string resolution)
    {
        if (_cachedCandlesByEpicByResolution.Count == 0)
        {
            return resolution == "Weekly" ? _cachedCandlesByEpic : EmptyCandles();
        }

        var result = new Dictionary<string, IReadOnlyList<OhlcPoint>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (epic, byResolution) in _cachedCandlesByEpicByResolution)
        {
            if (byResolution.TryGetValue(resolution, out var rows) &&
                SyntheticHistoryService.DistinctAlignmentKeyCount(rows, resolution) >= 2)
            {
                result[epic] = rows;
            }
        }

        return result;
    }

    private async Task<HistoryLoadResult> LoadSelectedHistoryAsync(
        IReadOnlyList<MarketInstrument> selectedComponents,
        string resolution,
        CancellationToken cancellationToken)
    {
        await EnsureConnectedAsync(cancellationToken);
        _operationState.BeginStage($"Loading full {resolution} history", selectedComponents.Count);
        var progress = new Progress<HistoryLoadProgress>(update =>
        {
            if (_windowLifetime.IsClosing) return;
            _operationState.Report($"Loading full {resolution} history", update.CompletedComponents, update.TotalComponents);
            StatusText.Text = $"Loading full {resolution} history for selected leg {update.CompletedComponents} of {update.TotalComponents}: {update.Epic}.";
        });
        var apiHistory = await _history.LoadSelectedAsync(selectedComponents, resolution, progress, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        return SyntheticHistoryService.MergeSelectedHistory(
            selectedComponents,
            resolution,
            apiHistory,
            CachedCandlesForResolution(resolution));
    }

    private async Task<HistoryLoadResult> LoadCandidateHistoryAsync(
        IReadOnlyList<MarketInstrument> candidates,
        string resolution,
        CancellationToken cancellationToken)
    {
        await EnsureConnectedAsync(cancellationToken);
        _operationState.BeginStage($"Loading candidate {resolution} history", candidates.Count);
        var progress = new Progress<HistoryLoadProgress>(update =>
        {
            if (_windowLifetime.IsClosing) return;
            _operationState.Report($"Loading candidate {resolution} history", update.CompletedComponents, update.TotalComponents);
            StatusText.Text = $"Loading Capital.com {resolution} history for candidate {update.CompletedComponents} of {update.TotalComponents}: {update.Epic}.";
        });
        return await _history.LoadSelectedAsync(candidates, resolution, progress, cancellationToken);
    }

    private static string HistoryRange(HistoryLoadResult history) =>
        history.SharedStart is not null && history.SharedEnd is not null
            ? $"{history.SharedCount} shared candles from {history.SharedStart:yyyy-MM-dd} to {history.SharedEnd:yyyy-MM-dd}"
            : "no shared candles";

    private async Task TryStartStreamingCurrentBasketAsync(string baseStatus, CancellationToken cancellationToken)
    {
        if (_basket is null)
        {
            return;
        }

        await TerminalOperationExecution.WrapStreamingStartAsync(
            () => StartStreamingCurrentBasketAsync(cancellationToken),
            baseStatus,
            cancellationToken);
    }

    private async Task StartStreamingCurrentBasketAsync(CancellationToken cancellationToken)
    {
        _operationState.BeginStage("Starting live stream", 2);
        await EnsureConnectedAsync(cancellationToken);
        if (_basket is null) return;

        if (_streaming is null || !_streaming.IsConnected)
        {
            if (_streaming is not null) await _streaming.DisposeAsync();
            cancellationToken.ThrowIfCancellationRequested();
            var streaming = new CapitalStreamingClient();
            streaming.QuoteReceived += Streaming_QuoteReceived;
            streaming.StatusChanged += (_, message) =>
            {
                if (_windowLifetime.IsClosing) return;
                _ = Dispatcher.InvokeAsync(() => _windowLifetime.TryApply(() => ConnectionText.Text = message));
            };
            streaming.Disconnected += Streaming_Disconnected;
            await streaming.ConnectAsync(_api.Session!, cancellationToken);
            _streaming = streaming;
        }

        var epics = SyntheticTerminalWorkspace.StreamingEpics(_basket);
        await _streaming.SubscribeQuotesAsync(_api.Session!, epics, cancellationToken);
        _operationState.Report("Starting live stream", 1, 2);
        await _streaming.SubscribeOhlcAsync(_api.Session!, epics, SyntheticHistoryService.RequestResolution(SelectedResolution()), cancellationToken);
        _operationState.Report("Starting live stream", 2, 2);
        ConnectionText.Text = $"Streaming {_basket.Symbol}";
        StatusText.Text = $"Streaming {_basket.Symbol}: {epics.Count} component epics.";
    }

    private void Streaming_Disconnected(object? sender, string message)
    {
        if (_windowLifetime.IsClosing) return;
        _ = Dispatcher.InvokeAsync(async () => await ReconnectStreamingAsync(message));
    }

    private async Task ReconnectStreamingAsync(string message)
    {
        if (_windowLifetime.IsClosing || _streamReconnectScheduled || _basket is null || _api.Session is null || _operationState.IsBusy) return;
        _streamReconnectScheduled = true;
        try
        {
            ConnectionText.Text = message;
            await RunOperationAsync("Reconnecting live stream", StartStreamingCurrentBasketAsync);
        }
        finally
        {
            _streamReconnectScheduled = false;
        }
    }

    private async Task RefreshBasketMarketDetailsAsync(SyntheticBasket basket, CancellationToken cancellationToken)
    {
        try
        {
            await EnsureConnectedAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return;
        }

        _operationState.BeginStage("Loading market details", basket.Components.Count);
        var completed = 0;
        foreach (var component in basket.Components)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var details = await _api.GetMarketDetailsAsync(component.Instrument.Epic, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (details is not null)
                {
                    ApplyMarketDetails(component.Instrument, details);
                    component.NotifyInstrumentPriceChanged();
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                component.Instrument.Status = $"Market snapshot n/a: {ex.Message}";
            }
            finally
            {
                completed++;
                _operationState.Report("Loading market details", completed, basket.Components.Count);
            }
        }

        SyntheticQuoteCalculator.Refresh(basket);
        SyntheticLiveUpdate.ApplyCurrentSyntheticQuote(basket, DateTimeOffset.UtcNow, SelectedResolution());
    }

    private static void ApplyMarketDetails(MarketInstrument target, MarketInstrument details)
    {
        var canApplyQuote = target.LastTickAt is null ||
            details.LastTickAt is { } snapshotTime &&
            snapshotTime.ToUniversalTime() >= target.LastTickAt.Value.ToUniversalTime();
        if (canApplyQuote)
        {
            target.Bid = details.Bid is > 0 ? details.Bid : null;
            target.Offer = details.Offer is > 0 ? details.Offer : null;
            if (details.Price is > 0) target.Price = details.Price;
            target.LastTickAt = details.LastTickAt;
        }

        if (string.IsNullOrWhiteSpace(target.Currency) && !string.IsNullOrWhiteSpace(details.Currency))
        {
            target.Currency = details.Currency.Trim();
        }
        if (details.LotSize is > 0) target.LotSize = details.LotSize;
        if (details.MinDealSize is > 0) target.MinDealSize = details.MinDealSize;
        if (details.MinSizeIncrement is > 0) target.MinSizeIncrement = details.MinSizeIncrement;
        if (details.MarginFactor is not null) target.MarginFactor = details.MarginFactor;
        if (!string.IsNullOrWhiteSpace(details.MarginFactorUnit)) target.MarginFactorUnit = details.MarginFactorUnit;
        if (!string.IsNullOrWhiteSpace(details.Status)) target.Status = details.Status;
    }

    private void BuyPreview_Click(object sender, RoutedEventArgs e) => PreviewSyntheticOrder("BUY");

    private void SellPreview_Click(object sender, RoutedEventArgs e) => PreviewSyntheticOrder("SELL");

    private async void CandleType_SelectionChanged(object sender, SelectionChangedEventArgs e) => await SetTerminalChartModeAsync();

    private async void Resolution_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_basket is null)
        {
            await SetTerminalIntervalAsync();
            return;
        }

        var basket = _basket;
        await RunOperationAsync($"Reloading {SelectedResolution()} history", async cancellationToken =>
        {
            await SetTerminalIntervalAsync();
            await ReloadSelectedBasketHistoryAsync(basket, cancellationToken);
        });
    }

    private async Task ReloadSelectedBasketHistoryAsync(SyntheticBasket existingBasket, CancellationToken cancellationToken)
    {
        var resolution = SelectedResolution();
        var selectedComponents = existingBasket.Components.Select(component => component.Instrument).ToList();
        var history = await LoadSelectedHistoryAsync(selectedComponents, resolution, cancellationToken);
        _operationState.BeginStage("Building selected basket");
        await Task.Yield();
        var rebuilt = SyntheticHistoryService.BuildSelected(
            existingBasket.Block,
            selectedComponents,
            history,
            resolution,
            PeriodsPerYear(resolution),
            MinimumCandles(resolution));
        if (rebuilt is null)
        {
            StatusText.Text = $"The selected legs have no usable shared {resolution} history.";
            return;
        }

        _basket = RenameBasket(rebuilt, existingBasket.Symbol, existingBasket.Block);
        await RefreshBasketMarketDetailsAsync(_basket, cancellationToken);
        _operationState.BeginStage("Rendering synthetic chart");
        await Task.Yield();
        await RenderSyntheticChartAsync(_basket);
        var reloadStatus = $"{_basket.Symbol}: reloaded {resolution} history for the same {_basket.Components.Count} legs, {HistoryRange(history)}.";
        StatusText.Text = reloadStatus;
        await TryStartStreamingCurrentBasketAsync(reloadStatus, cancellationToken);
    }

    private async void Streaming_QuoteReceived(object? sender, QuoteUpdate update)
    {
        if (_windowLifetime.IsClosing) return;
        await Dispatcher.InvokeAsync(() =>
        {
            if (_windowLifetime.IsClosing || _basket is null) return;
            var observedAt = DateTimeOffset.UtcNow;
            var result = SyntheticTerminalLiveUpdate.Apply(
                _basket,
                update,
                observedAt,
                SelectedResolution());
            if (!result.Matched) return;
            if (result.Tick is not null) _ = SendTerminalTickAsync(result.Tick);
            ChartMetaText.Text = $"{_pendingPayload?.CurrencyLabel ?? ""} | bid {FormatQuote(_basket.BidPrice)} | ask {FormatQuote(_basket.AskPrice)}";
            var quoteStatus = SyntheticTerminalChartPayload.BasketQuoteStatus(_basket, observedAt);
            StatusText.Text = $"{_basket.Symbol}: bid {FormatQuote(_basket.BidPrice)}, ask {FormatQuote(_basket.AskPrice)}, {quoteStatus}, tick {update.Time.ToLocalTime():HH:mm:ss}.";
            ScheduleMarginPreview(_marginPreviewNotional);
        });
    }

    private async Task EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_api.Session is not null) return;
        var saved = _credentialStore.Load();
        if (saved is null) throw new InvalidOperationException("No saved Capital.com keys found.");
        await _api.LoginAsync(saved, cancellationToken);
        await ResetMarginPreviewAfterLoginAsync();
        await PublishTerminalTradingModeAsync();
        await _tradingCoordinator.ReconnectAsync(PublishTerminalExecutionsAsync, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        ConnectionText.Text = "connected";
    }

    private Task ResetMarginPreviewAfterLoginAsync()
    {
        _marginPreview.InvalidateCaches();
        return ResetMarginPreviewContextAsync(
            clearBasket: false,
            reason: "Margin preview reset after login. Awaiting refresh.");
    }

    private async Task<bool> RunOperationAsync(string operationName, Func<CancellationToken, Task> action, int? total = null)
    {
        var cancellationToken = _windowLifetime.Token;
        if (cancellationToken.IsCancellationRequested) return false;
        if (!_operationState.TryBegin(operationName, total))
        {
            _windowLifetime.TryApply(() => StatusText.Text = $"{_operationState.Label} is already running.");
            return false;
        }

        return await RunStartedOperationAsync(operationName, action, cancellationToken);
    }

    private async Task<bool> RunStartedOperationAsync(
        string operationName,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken)
    {
        var controlsDisabled = false;
        try
        {
            return await TerminalOperationExecution.RunAsync(
                async token =>
                {
                    token.ThrowIfCancellationRequested();
                    if (!_windowLifetime.TryApply(() =>
                        {
                            SetOperationControlsEnabled(false);
                            controlsDisabled = true;
                        }))
                    {
                        token.ThrowIfCancellationRequested();
                        return;
                    }

                    await SetTerminalBusyAsync(true, operationName);
                    token.ThrowIfCancellationRequested();
                    await action(token);
                },
                cancellationToken,
                () => _windowLifetime.TryApply(() => _operationState.Complete()),
                ex => _windowLifetime.TryApply(() =>
                {
                    _operationState.Fail(ex.Message);
                    ConnectionText.Text = operationName.StartsWith("Connecting", StringComparison.Ordinal)
                        ? $"Connection failed: {ex.Message}"
                        : ConnectionText.Text;
                    StatusText.Text = ex.Message;
                }));
        }
        finally
        {
            if (controlsDisabled && !_windowLifetime.IsClosing)
            {
                try
                {
                    await SetTerminalBusyAsync(false);
                    _windowLifetime.TryApply(() => SetOperationControlsEnabled(true));
                }
                catch (Exception) when (cancellationToken.IsCancellationRequested)
                {
                }
            }
        }
    }

    private void SetOperationControlsEnabled(bool enabled)
    {
        ConnectButton.IsEnabled = enabled;
        BuildBasketButton.IsEnabled = enabled;
        UniverseBox.IsEnabled = enabled;
        BlockBox.IsEnabled = enabled;
        StrategyBox.IsEnabled = enabled;
        SavedBasketsBox.IsEnabled = enabled;
        if (enabled) UpdateDeleteBasketButtonState();
        else DeleteBasketButton.IsEnabled = false;
        ResolutionBox.IsEnabled = enabled;
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

    private void RefreshSavedBaskets(string? selectedName = null) =>
        RefreshSavedBaskets(_savedBasketStore.LoadAll(), selectedName);

    private void RefreshSavedBaskets(IReadOnlyList<SavedSyntheticBasket> saved, string? selectedName = null)
    {
        _loadingSavedBaskets = true;
        try
        {
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
            UpdateDeleteBasketButtonState();
        }
    }

    private void UpdateDeleteBasketButtonState()
    {
        DeleteBasketButton.IsEnabled = _savedBasketDeletion.IsDeleteEnabled(SavedBasketsBox.SelectedItem as SavedSyntheticBasket);
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
        var universe = SelectedUniverse();
        await RunOperationAsync($"Loading {UniverseLabel(universe).ToLowerInvariant()} universe", async cancellationToken =>
        {
            await LoadUniverseAsync(universe, cancellationToken);
            _basket = null;
            await ClearTerminalChartAsync();
        });
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

    private EtfDataLoadResult? EnsureEtfCatalogLoaded()
    {
        var cache = _etfCatalog.LoadOnce(() => DashboardEtfDataLoader.LoadEtfs());
        _knownEtfEpics = _etfCatalog.KnownEtfEpics;
        return cache;
    }

    private async Task<IReadOnlyList<MarketInstrument>> EnrichEtfMetadataAsync(
        IReadOnlyList<MarketInstrument> instruments,
        CancellationToken cancellationToken)
    {
        await EnsureConnectedAsync(cancellationToken);

        var enriched = new List<MarketInstrument>(instruments.Count);
        _operationState.BeginStage("Loading ETF market details", instruments.Count);
        var completed = 0;
        foreach (var instrument in instruments)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_knownEtfEpics.Contains(instrument.Epic) || !EtfMetadataMerger.NeedsEnrichment(instrument))
            {
                enriched.Add(instrument);
                completed++;
                _operationState.Report("Loading ETF market details", completed, instruments.Count);
                continue;
            }

            try
            {
                var details = await _api.GetMarketDetailsAsync(instrument.Epic, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                enriched.Add(details is null ? instrument : EtfMetadataMerger.Merge(instrument, details));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                enriched.Add(instrument);
            }
            finally
            {
                completed++;
                _operationState.Report("Loading ETF market details", completed, instruments.Count);
            }
        }

        return enriched
            .Where(item => TerminalUniverse.Accepts(TerminalUniverseKind.ETFs, item, _knownEtfEpics))
            .ToList();
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

    private async Task InitializeChartHostAsync(CancellationToken cancellationToken)
    {
        await TerminalOperationExecution.RunAsync(
            async token =>
            {
                token.ThrowIfCancellationRequested();
                var terminalPath = Path.Combine(AppContext.BaseDirectory, "Assets", "synthetic-terminal.html");
                if (!File.Exists(terminalPath))
                {
                    _windowLifetime.TryApply(() => StatusText.Text = $"Chart file missing: {terminalPath}");
                    return;
                }

                var environment = await WebViewRuntimeProfile.CreateEnvironmentAsync(token);
                await TerminalWebView.EnsureCoreWebView2Async(environment);
                token.ThrowIfCancellationRequested();
                if (!_windowLifetime.TryApply(() =>
                {
                    TerminalWebView.CoreWebView2.WebMessageReceived += TerminalWebMessageReceived;
                    TerminalWebView.NavigationCompleted += async (_, _) =>
                    {
                        if (!_windowLifetime.TryApply(() => _chartReady = true)) return;
                        await InvokeTerminalScriptAsync("window.resizeTerminal && window.resizeTerminal();");
                        if (_windowLifetime.IsClosing) return;
                        if (_pendingPayload is not null)
                        {
                            await SendTerminalPayloadAsync(_pendingPayload);
                        }
                        await PublishInitialTradingStateAsync();
                    };
                    TerminalWebView.Source = new Uri(terminalPath);
                    StatusText.Text = "Interactive chart host loading.";
                })) return;
            },
            cancellationToken,
            () => { },
            ex => _windowLifetime.TryApply(() =>
            {
                _chartReady = false;
                StatusText.Text = $"Interactive chart host failed: {ex.Message}";
            }));
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

        await SendTerminalPayloadAsync(payload);
        await SetTerminalChartModeAsync();
        ScheduleMarginPreview(_marginPreviewNotional);
        await SetTerminalIntervalAsync();
    }

    private async Task SendTerminalPayloadAsync(SyntheticTerminalPayload payload)
    {
        _pendingPayload = payload;
        if (!_chartReady || TerminalWebView.CoreWebView2 is null) return;

        var json = JsonSerializer.Serialize(payload);
        await InvokeTerminalScriptAsync($"window.setTerminalData ? window.setTerminalData({json}) : window.renderTerminal && window.renderTerminal({json});");
    }

    private async Task SendTerminalTickAsync(SyntheticTerminalTickPayload tick)
    {
        if (!_chartReady || TerminalWebView.CoreWebView2 is null) return;
        var json = JsonSerializer.Serialize(tick);
        await InvokeTerminalScriptAsync($"window.updateTerminalTick && window.updateTerminalTick({json});");
    }

    private Task SetTerminalBusyAsync(bool busy, string? operationName = null)
    {
        var label = JsonSerializer.Serialize(operationName ?? string.Empty);
        return InvokeTerminalScriptAsync($"window.setTerminalBusy && window.setTerminalBusy({busy.ToString().ToLowerInvariant()}, {label});");
    }

    private async Task PreflightSyntheticBasketAsync(
        string side,
        decimal basketNotional,
        CancellationToken cancellationToken)
    {
        if (_basket is null)
        {
            await PublishTerminalPreflightAsync(new SyntheticPreflightResult(
                false,
                null,
                [new SyntheticPreflightFailure("", "Build a synthetic basket before preflight.")]));
            return;
        }

        var basket = _basket;
        await EnsureConnectedAsync(cancellationToken);
        _operationState.BeginStage("Refreshing preflight market details", basket.Components.Count);
        var snapshotResult = await _preflightMarketSnapshots.LoadAsync(
            basket,
            cancellationToken,
            (completed, total) => _operationState.Report("Refreshing preflight market details", completed, total));
        if (snapshotResult.Basket is null)
        {
            await PublishTerminalPreflightAsync(new SyntheticPreflightResult(
                false,
                null,
                snapshotResult.Failures));
            return;
        }

        var freshBasket = snapshotResult.Basket;
        _marginPreview.InvalidateCaches();
        var margin = await _marginPreview.BuildAsync(freshBasket, basketNotional, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        var result = SyntheticTradePreflight.Build(new SyntheticPreflightInput(
            _api.IsDemoTradingSession,
            SyntheticTerminalChartPayload.DrawingIdentity(freshBasket),
            freshBasket,
            side,
            basketNotional,
            DateTimeOffset.UtcNow,
            margin));
        result = _tradingCoordinator.RegisterPreflight(result);
        await PublishTerminalPreflightAsync(result);
    }

    private Task ExecuteSyntheticBasketAsync(Guid ticketId)
    {
        var cancellationToken = _windowLifetime.Token;
        if (cancellationToken.IsCancellationRequested) return Task.CompletedTask;
        const string operationName = "Executing synthetic basket";
        if (!_operationState.TryBegin(operationName))
        {
            _windowLifetime.TryApply(() => StatusText.Text = $"{_operationState.Label} is already running.");
            return Task.CompletedTask;
        }

        SyntheticHostExecution? execution = null;
        SyntheticTradingWindowLifecycleCoordinator.TrackedOperation? trackedOperation = null;
        try
        {
            execution = _tradingCoordinator.BeginExecution(ticketId);
            trackedOperation = _tradingLifecycle.BeginOperation();
        }
        catch (Exception ex)
        {
            execution?.Dispose();
            _operationState.Fail(ex.Message);
            _windowLifetime.TryApply(() => StatusText.Text = ex.Message);
            return PublishTerminalExecutionErrorAsync(ex.Message);
        }

        var operation = RunStartedOperationAsync(
            operationName,
            async token =>
            {
                await _tradingCoordinator.ExecuteAsync(
                    execution,
                    PublishTerminalExecutionProgressAsync,
                    PublishTerminalExecutionsAsync,
                    trackedOperation.MarkMutationDispatched,
                    token);
                _marginPreview.InvalidateCaches();
            },
            cancellationToken);
        var finished = FinishSyntheticExecutionAsync(execution, operation);
        trackedOperation.Track(finished);
        return finished;
    }

    private static async Task FinishSyntheticExecutionAsync(
        SyntheticHostExecution execution,
        Task operation)
    {
        try
        {
            await operation;
        }
        finally
        {
            execution.Dispose();
        }
    }

    private Task RefreshSyntheticExecutionsAsync(CancellationToken cancellationToken) =>
        _tradingCoordinator.RefreshAsync(PublishTerminalExecutionsAsync, cancellationToken);

    private Task CloseSyntheticBasketAsync(string executionId, CancellationToken cancellationToken)
    {
        var trackedOperation = _tradingLifecycle.BeginOperation();
        var operation = _tradingCoordinator.CloseAsync(
            executionId,
            PublishTerminalExecutionProgressAsync,
            PublishTerminalExecutionsAsync,
            trackedOperation.MarkMutationDispatched,
            cancellationToken);
        trackedOperation.Track(operation);
        return operation;
    }

    private Task PublishTerminalPreflightAsync(SyntheticPreflightResult result) =>
        PublishTerminalCallbackAsync("setTerminalPreflight", result);

    private Task PublishTerminalExecutionsAsync(IReadOnlyList<SyntheticExecutionRecord> records) =>
        PublishTerminalCallbackAsync("setTerminalExecutions", records);

    private Task PublishTerminalExecutionProgressAsync(SyntheticExecutionRecord record) =>
        PublishTerminalCallbackAsync("setTerminalExecutionProgress", record);

    private Task PublishTerminalExecutionErrorAsync(string error) =>
        PublishTerminalCallbackAsync("setTerminalExecutionProgress", new { Error = error });

    private Task PublishTerminalTradingModeAsync()
    {
        var session = _api.Session;
        var isDemo = session is not null && _api.IsDemoTradingSession;
        TradingModeText.Text = isDemo ? "DEMO TRADING" : "TRADING DISABLED";
        TradingModeText.Foreground = isDemo
            ? System.Windows.Media.Brushes.LightGreen
            : System.Windows.Media.Brushes.OrangeRed;
        return PublishTerminalCallbackAsync("setTerminalTradingMode", new
        {
            IsDemo = isDemo,
            IsExecutionEnabled = isDemo,
            AccountId = session?.CurrentAccountId ?? "",
            AccountCurrency = session?.AccountCurrency ?? "",
            Label = isDemo ? "DEMO TRADING" : "TRADING DISABLED",
        });
    }

    private async Task PublishInitialTradingStateAsync()
    {
        try
        {
            await PublishTerminalTradingModeAsync();
            await _tradingCoordinator.PublishStoredAsync(PublishTerminalExecutionsAsync, _windowLifetime.Token);
        }
        catch (OperationCanceledException) when (_windowLifetime.IsClosing)
        {
        }
        catch (Exception ex)
        {
            _windowLifetime.TryApply(() => StatusText.Text = $"Execution history could not be loaded: {ex.Message}");
        }
    }

    private Task PublishTerminalCallbackAsync<T>(string callback, T payload)
    {
        var json = JsonSerializer.Serialize(payload);
        return InvokeTerminalScriptAsync($"window.{callback} && window.{callback}({json});");
    }

    private void ScheduleMarginPreview(decimal basketNotional)
    {
        if (_basket is null || _windowLifetime.IsClosing) return;
        if (!SyntheticMarginPreviewInput.TryValidate(basketNotional, out var validatedNotional, out _)) return;
        _marginPreviewNotional = validatedNotional;
        CancelMarginPreviewRequest();
        var request = CancellationTokenSource.CreateLinkedTokenSource(_windowLifetime.Token);
        var requestToken = request.Token;
        _marginPreviewRefresh = request;
        _ = RefreshMarginPreviewAsync(_basket, _marginPreviewNotional, request, requestToken);
    }

    private async Task RefreshMarginPreviewAsync(
        SyntheticBasket basket,
        decimal basketNotional,
        CancellationTokenSource request,
        CancellationToken requestToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500), requestToken);
            await SetTerminalBusyAsync(true, "Refreshing margin preview");
            var summary = await _marginPreview.BuildAsync(basket, basketNotional, requestToken);
            requestToken.ThrowIfCancellationRequested();
            if (!SyntheticMarginPreviewPublication.IsCurrent(requestToken, request, _marginPreviewRefresh, basket, _basket)) return;
            var json = JsonSerializer.Serialize(summary);
            await InvokeTerminalScriptAsync(
                $"window.setTerminalMarginPreview && window.setTerminalMarginPreview({json});");
        }
        catch (OperationCanceledException) when (requestToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            if (!SyntheticMarginPreviewPublication.IsCurrent(requestToken, request, _marginPreviewRefresh, basket, _basket)) return;
            var json = JsonSerializer.Serialize(new { Error = ex.Message });
            await InvokeTerminalScriptAsync(
                $"window.setTerminalMarginPreview && window.setTerminalMarginPreview({json});");
        }
        finally
        {
            if (ReferenceEquals(_marginPreviewRefresh, request))
            {
                _marginPreviewRefresh = null;
                request.Dispose();
                if (!_windowLifetime.IsClosing) await SetTerminalBusyAsync(false);
            }
        }
    }

    private static string FormatQuote(decimal? value) => value?.ToString("0.#####") ?? "n/a";

    private static string SavedCredentialLabel(ApiCredentials credentials) =>
        credentials.UseDemo ? "Capital.com demo" : "Capital.com live";

    private void CancelMarginPreviewRequest()
    {
        var request = _marginPreviewRefresh;
        _marginPreviewRefresh = null;
        if (request is null) return;
        try
        {
            request.Cancel();
        }
        finally
        {
            request.Dispose();
        }
    }

    private async Task ResetMarginPreviewContextAsync(bool clearBasket, string reason, bool releaseBusy = false)
    {
        CancelMarginPreviewRequest();
        if (clearBasket) _basket = null;
        if (_windowLifetime.IsClosing) return;
        if (releaseBusy) await SetTerminalBusyAsync(false);
        var json = JsonSerializer.Serialize(reason);
        await InvokeTerminalScriptAsync(
            $"window.resetTerminalMarginPreview && window.resetTerminalMarginPreview({json});");
    }

    private async Task ClearTerminalChartAsync()
    {
        await ResetMarginPreviewContextAsync(
            clearBasket: true,
            reason: "Build a synthetic basket to preview margin.");
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
        if (_windowLifetime.IsClosing) return;
        try
        {
            if (_chartReady && TerminalWebView.CoreWebView2 is not null)
            {
                await TerminalWebView.ExecuteScriptAsync(script);
            }
        }
        catch (Exception ex)
        {
            _windowLifetime.TryApply(() => StatusText.Text = $"Chart command failed: {ex.Message}");
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

    private async void TerminalWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        if (_windowLifetime.IsClosing) return;
        try
        {
            await SyntheticTradingBrowserMessageHandler.HandleAsync(
                e.WebMessageAsJson,
                HandleTerminalBrowserRequestAsync,
                RejectTerminalBrowserRequestAsync);
        }
        catch (Exception ex) when (SyntheticTradingBrowserRequestParser.IsSemanticJsonException(ex))
        {
            await RejectTerminalBrowserRequestAsync($"Browser request was rejected: {ex.Message}");
        }
        catch (Exception ex)
        {
            await RejectTerminalBrowserRequestAsync($"Browser request failed: {ex.Message}");
        }
    }

    private async Task HandleTerminalBrowserRequestAsync(SyntheticTradingBrowserRequest request)
    {
        switch (request)
        {
            case SyntheticPreflightBasketRequest preflight:
                await RunOperationAsync(
                    "Preflighting synthetic basket",
                    token => PreflightSyntheticBasketAsync(preflight.Side, preflight.BasketNotional, token));
                break;
            case SyntheticExecuteBasketRequest execute:
                await ExecuteSyntheticBasketAsync(execute.TicketId);
                break;
            case SyntheticRefreshExecutionsRequest:
                await RunOperationAsync("Refreshing synthetic executions", RefreshSyntheticExecutionsAsync);
                break;
            case SyntheticCloseBasketRequest close:
                await RunOperationAsync(
                    "Closing synthetic basket",
                    token => CloseSyntheticBasketAsync(close.ExecutionId, token));
                break;
            case SyntheticCancelMarginPreviewRequest:
                await ResetMarginPreviewContextAsync(
                    clearBasket: false,
                    reason: SyntheticMarginPreviewInput.InvalidReason,
                    releaseBusy: true);
                break;
            case SyntheticPreviewMarginsRequest previewMargins:
                if (!SyntheticMarginPreviewInput.TryValidate(
                        previewMargins.BasketNotional,
                        out var validatedNotional,
                        out var inputError))
                {
                    await ResetMarginPreviewContextAsync(
                        clearBasket: false,
                        reason: inputError,
                        releaseBusy: true);
                    break;
                }
                ScheduleMarginPreview(validatedNotional);
                break;
            case SyntheticPreviewOrderRequest previewOrder:
                PreviewSyntheticOrder(previewOrder.Side, previewOrder.BasketNotional);
                break;
        }
    }

    private async Task RejectTerminalBrowserRequestAsync(string error)
    {
        _windowLifetime.TryApply(() => StatusText.Text = error);
        try
        {
            await PublishTerminalExecutionErrorAsync(error);
        }
        catch (Exception ex)
        {
            _windowLifetime.TryApply(() => StatusText.Text = $"{error} ({ex.Message})");
        }
    }

    private void PreviewSyntheticOrder(string side, decimal? requestedBasketNotional = null)
    {
        if (_basket is null)
        {
            OrderPreviewText.Text = "Build a synthetic symbol first.";
            return;
        }

        decimal? inputNotional = requestedBasketNotional;
        if (inputNotional is null && decimal.TryParse(QuantityBox.Text, out var enteredNotional))
        {
            inputNotional = enteredNotional;
        }
        if (!SyntheticMarginPreviewInput.TryValidate(inputNotional, out var basketNotional, out var inputError))
        {
            OrderPreviewText.Text = inputError;
            var errorJson = JsonSerializer.Serialize(new { Error = inputError });
            _ = InvokeTerminalScriptAsync($"window.setTerminalOrderPreview && window.setTerminalOrderPreview({errorJson});");
            return;
        }
        try
        {
            var preview = SyntheticOrderSizing.BuildExecutableOrderPreview(_basket, side, basketNotional);
            var rows = preview.Legs.Select(leg =>
                $"{leg.Side} {leg.Quantity:0.####} x {leg.Epic} @ {leg.ReferencePrice:0.#####} = {leg.Notional:0.##} ({leg.WeightImbalancePct:+0.##;-0.##;0}pp)");
            OrderPreviewText.Text = $"Executable {preview.TotalExecutableNotional:0.##}; max imbalance {preview.MaxAbsoluteWeightImbalancePct:0.##}pp" +
                                    Environment.NewLine + string.Join(Environment.NewLine, rows);
            var json = JsonSerializer.Serialize(preview);
            _ = InvokeTerminalScriptAsync($"window.setTerminalOrderPreview && window.setTerminalOrderPreview({json});");
        }
        catch (Exception ex)
        {
            OrderPreviewText.Text = ex.Message;
            var json = JsonSerializer.Serialize(new { Error = ex.Message });
            _ = InvokeTerminalScriptAsync($"window.setTerminalOrderPreview && window.setTerminalOrderPreview({json});");
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        if (e.Cancel) return;
        if (!_tradingLifecycle.RequestClose(
                () =>
                {
                    CancelMarginPreviewRequest();
                    _tradingCoordinator.CancelPendingOperations();
                    _windowLifetime.BeginClosing();
                },
                () =>
                {
                    if (!Dispatcher.HasShutdownStarted)
                    {
                        _ = Dispatcher.InvokeAsync(Close);
                    }
                }))
        {
            e.Cancel = true;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        try
        {
            _streaming?.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
        catch
        {
        }
        finally
        {
            _tradingCoordinator.Dispose();
            _api.Dispose();
            _windowLifetime.Dispose();
            base.OnClosed(e);
        }
    }
}
