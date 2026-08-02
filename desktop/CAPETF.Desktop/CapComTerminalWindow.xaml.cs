using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
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
    private readonly SyntheticHistorySessionCache _historySessionCache = new();
    private readonly SyntheticMarginPreviewService _marginPreview;
    private readonly SyntheticPreflightMarketSnapshotLoader _preflightMarketSnapshots;
    private readonly SyntheticTradingHostCoordinator _tradingCoordinator;
    private readonly SyntheticRiskPlanStore _riskPlanStore = new(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CAPETF",
        "synthetic-risk-plans.json"));
    private readonly TerminalActivityLog _activityLog = new(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CAPETF",
        "terminal-activity.json"));
    private readonly TerminalUniverseCache _universeCache = new(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CAPETF",
        "universe-cache"));
    private readonly SyntheticTradingWindowLifecycleCoordinator _tradingLifecycle = new();
    private readonly TerminalOperationState _operationState = new();
    private readonly TerminalStatusArbiter _statusArbiter;
    private readonly ActiveSyntheticBasketState _activeBasket = new();
    private readonly SyntheticRealtimeBarBuilder _realtimeBars = new();
    private readonly WindowLifetime _windowLifetime = new();
    private readonly SemaphoreSlim _brokerRefreshGate = new(1, 1);
    private readonly List<MarketInstrument> _instruments = [];
    private readonly ObservableCollection<TerminalComponentRow> _components = [];
    private readonly TerminalUniverseUiCoordinator _universeUi = new();
    private readonly Dictionary<TerminalUniverseKind, TerminalUniverseAccumulator> _universeAccumulators = [];
    private readonly TerminalUniverseRefreshGate _universeRefreshGate = new();
    private readonly SemaphoreSlim _universeDiscoveryRequestGate = new(1, 1);
    private readonly Dictionary<TerminalUniverseKind, IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>>> _candlesByUniverse = [];
    private readonly Dictionary<TerminalUniverseKind, IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>>>> _candlesByUniverseByResolution = [];
    private readonly EtfCatalogCache _etfCatalog = new();
    private CryptoMarketMetadataEnricher _cryptoMetadataEnricher;
    private IReadOnlySet<string> _knownEtfEpics = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>> _cachedCandlesByEpic =
        new Dictionary<string, IReadOnlyList<OhlcPoint>>(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>>> _cachedCandlesByEpicByResolution =
        new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>>>(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<SyntheticExecutionRecord> _terminalExecutions = [];
    private CapitalStreamingClient? _streaming;
    private SyntheticBasket? _basket
    {
        get => _activeBasket.Basket;
        set
        {
            if (value is null) _activeBasket.Clear();
            else _activeBasket.Activate(value, value.Strategy);
        }
    }
    private bool _loadingSavedBaskets;
    private SyntheticTerminalPayload? _pendingPayload;
    private bool _chartReady;
    private bool _changingUniverseProgrammatically;
    private bool _streamReconnectScheduled;
    private string _terminalDrawingIdentity = "";
    private CancellationTokenSource? _marginPreviewRefresh;
    private CancellationTokenSource? _universeDiscoveryRefresh;
    private Task? _universeDiscoveryTask;
    private TerminalUniverseKind? _universeDiscoveryUniverse;
    private TerminalUniverseRefresh? _universeDiscoveryLease;
    private long _nextUniverseDiscoveryRequestTimestamp;
    private static readonly TimeSpan minimumUniverseDiscoveryRequestSpacing = TimeSpan.FromMilliseconds(250);
    private decimal _marginPreviewNotional = 1m;
    private readonly Task _brokerRefreshLoop;

    public CapComTerminalWindow()
    {
        InitializeComponent();
        _statusArbiter = new TerminalStatusArbiter(
            () => _operationState.IsBusy,
            status => StatusText.Text = status);
        _savedBasketDeletion = new SavedBasketDeletionCoordinator(_savedBasketStore);
        OperationProgressPanel.DataContext = _operationState;
        _history = new SyntheticHistoryService(_api);
        _marginPreview = new SyntheticMarginPreviewService(new CapitalApiSyntheticMarginDataSource(_api));
        _preflightMarketSnapshots = new SyntheticPreflightMarketSnapshotLoader(_api.GetMarketDetailsAsync);
        _cryptoMetadataEnricher = new CryptoMarketMetadataEnricher(_api.GetMarketDetailsAsync);
        _tradingCoordinator = SyntheticTradingComposition.CreateCoordinator(
            new CapitalTradingGateway(_api),
            SyntheticTradingComposition.DefaultExecutionStorePath(),
            () => _api.IsDemoTradingSession,
            cancellationToken => _api.GetOpenPositionsAsync(cancellationToken),
            currentAccountId: () => _api.Session?.CurrentAccountId ?? "");
        StrategyBox.ItemsSource = SyntheticStrategyCatalog.All;
        StrategyBox.DisplayMemberPath = nameof(SyntheticStrategy.Label);
        StrategyBox.SelectedValuePath = nameof(SyntheticStrategy.Kind);
        StrategyBox.SelectedIndex = 0;
        ComponentsList.ItemsSource = _components;
        LoadSavedCredentials();
        RefreshSavedBaskets();
        _ = InitializeChartHostAsync(_windowLifetime.Token);
        _brokerRefreshLoop = RunBrokerRefreshLoopAsync(_windowLifetime.Token);
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
            _cryptoMetadataEnricher = new CryptoMarketMetadataEnricher(_api.GetMarketDetailsAsync);
            await ResetMarginPreviewAfterLoginAsync();
            await PublishTerminalTradingModeAsync();
            await _tradingCoordinator.ReconnectAsync(PublishTerminalExecutionsAsync, cancellationToken);
            await PublishTerminalRiskPlansAsync();
            await PublishBrokerSnapshotAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            ConnectionText.Text = $"connected to {SavedCredentialLabel(saved)}";
            StatusText.Text = $"Connected to {SavedCredentialLabel(saved)}. Loading universe...";
            await LoadStocksAsync(cancellationToken);
        });
    }

    private Task LoadStocksAsync(CancellationToken cancellationToken) => LoadUniverseAsync(SelectedUniverse(), cancellationToken);

    private Task LoadUniverseAsync(TerminalUniverseKind universe, CancellationToken cancellationToken) =>
        LoadUniverseAsync(universe, cancellationToken, waitForDiscovery: false);

    private async Task LoadUniverseAsync(
        TerminalUniverseKind universe,
        CancellationToken cancellationToken,
        bool waitForDiscovery)
    {
        var refresh = BeginUniverseRefresh(universe, cancellationToken);
        IReadOnlyList<MarketInstrument> rawCachedEtfs = [];
        var cachedEtfCandles = EmptyCandles();
        var cachedEtfCandlesByResolution = EmptyCandlesByResolution();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            _universeUi.EnsureEtfCatalogFor(universe, () => _ = EnsureEtfCatalogLoaded());
            if (_universeUi.TryGetCached(universe, out var instruments) &&
                _candlesByUniverse.TryGetValue(universe, out var candles) &&
                _candlesByUniverseByResolution.TryGetValue(universe, out var candlesByResolution))
            {
                PublishUniverseSnapshot(
                    universe,
                    GetUniverseAccumulator(universe).PublishCached(instruments),
                    candles,
                    candlesByResolution,
                    refresh,
                    persist: false);
                StatusText.Text = $"{_instruments.Count} {UniverseLabel(universe).ToLowerInvariant()} loaded from the selected universe cache.";
                rawCachedEtfs = universe == TerminalUniverseKind.ETFs ? instruments : [];
                cachedEtfCandles = candles;
                cachedEtfCandlesByResolution = candlesByResolution;
                await StartUniverseDiscoveryAsync(
                    universe,
                    refresh,
                    waitForDiscovery,
                    rawCachedEtfs,
                    cachedEtfCandles,
                    cachedEtfCandlesByResolution);
                return;
            }

            var persisted = _universeCache.Load(universe);
            var accumulator = GetUniverseAccumulator(universe);
            if (persisted.Count > 0)
            {
                PublishUniverseSnapshot(
                    universe,
                    accumulator.PublishCached(persisted),
                    EmptyCandles(),
                    EmptyCandlesByResolution(),
                    refresh,
                    persist: false);
                StatusText.Text = $"{_instruments.Count} {UniverseLabel(universe).ToLowerInvariant()} loaded from the local merged cache.";
            }

            if (universe == TerminalUniverseKind.Stocks)
            {
                StatusText.Text = "Loading cached stock chunks...";
                var cached = DashboardStockChunkLoader.LoadStocks();
                if (cached.Instruments.Count > 0)
                {
                    var snapshot = persisted.Count > 0
                        ? accumulator.MergeCached(cached.Instruments.Where(item => TerminalUniverse.Accepts(universe, item, _knownEtfEpics)).ToList())
                        : accumulator.PublishCached(cached.Instruments.Where(item => TerminalUniverse.Accepts(universe, item, _knownEtfEpics)).ToList());
                    PublishUniverseSnapshot(
                        universe,
                        snapshot,
                        cached.OhlcByEpic,
                        cached.OhlcByEpicAndResolution ?? EmptyCandlesByResolution(),
                        refresh);
                    var source = cached.SourceAsOf is null ? "" : $" Source date {cached.SourceAsOf:yyyy-MM-dd}.";
                    StatusText.Text = $"{_instruments.Count} stocks loaded from {cached.ChunkCount} cached stock chunks.{source}";
                }
            }
            else
            {
                var etfCache = EnsureEtfCatalogLoaded();
                StatusText.Text = "Loading cached ETFs...";
                if (etfCache is not null && etfCache.Instruments.Count > 0)
                {
                    rawCachedEtfs = etfCache.Instruments
                        .Where(item => TerminalUniverse.Accepts(universe, item, _knownEtfEpics))
                        .ToList();
                    var snapshot = persisted.Count > 0
                        ? accumulator.MergeCached(rawCachedEtfs)
                        : accumulator.PublishCached(rawCachedEtfs);
                    PublishUniverseSnapshot(universe, snapshot, etfCache.OhlcByEpic, etfCache.OhlcByEpicAndResolution, refresh);
                    cachedEtfCandles = etfCache.OhlcByEpic;
                    cachedEtfCandlesByResolution = etfCache.OhlcByEpicAndResolution;
                    var source = etfCache.SourceAsOf is null ? "" : $" Source date {etfCache.SourceAsOf:yyyy-MM-dd}.";
                    StatusText.Text = $"{_instruments.Count} ETFs loaded from the cached ETF file.{source}";
                }
            }

            await StartUniverseDiscoveryAsync(
                universe,
                refresh,
                waitForDiscovery,
                rawCachedEtfs,
                cachedEtfCandles,
                cachedEtfCandlesByResolution);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Cached {UniverseLabel(universe).ToLowerInvariant()} load failed; refreshing from Capital.com. {ex.Message}";
            await StartUniverseDiscoveryAsync(
                universe,
                refresh,
                waitForDiscovery,
                rawCachedEtfs,
                cachedEtfCandles,
                cachedEtfCandlesByResolution);
        }
    }

    private TerminalUniverseAccumulator GetUniverseAccumulator(TerminalUniverseKind universe)
    {
        if (_universeAccumulators.TryGetValue(universe, out var accumulator)) return accumulator;
        accumulator = new TerminalUniverseAccumulator(universe);
        _universeAccumulators[universe] = accumulator;
        return accumulator;
    }

    private TerminalUniverseRefresh BeginUniverseRefresh(
        TerminalUniverseKind universe,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var refresh = _universeRefreshGate.Begin(universe);
        _universeDiscoveryRefresh?.Cancel();
        _universeDiscoveryRefresh = CancellationTokenSource.CreateLinkedTokenSource(_windowLifetime.Token, cancellationToken);
        _universeDiscoveryUniverse = universe;
        _universeDiscoveryLease = refresh;
        _universeDiscoveryTask = null;
        return refresh;
    }

    private Task StartUniverseDiscoveryAsync(
        TerminalUniverseKind universe,
        TerminalUniverseRefresh refresh,
        bool waitForDiscovery,
        IReadOnlyList<MarketInstrument> rawCachedEtfs,
        IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>> cachedEtfCandles,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>>> cachedEtfCandlesByResolution)
    {
        if (!_universeRefreshGate.IsCurrent(refresh) || _universeDiscoveryLease != refresh)
        {
            return Task.CompletedTask;
        }

        _universeDiscoveryTask ??= RefreshUniverseInBackgroundAsync(
            universe,
            refresh,
            rawCachedEtfs,
            cachedEtfCandles,
            cachedEtfCandlesByResolution,
            _universeDiscoveryRefresh?.Token ?? _windowLifetime.Token);
        return waitForDiscovery ? _universeDiscoveryTask : Task.CompletedTask;
    }

    private async Task RefreshUniverseInBackgroundAsync(
        TerminalUniverseKind universe,
        TerminalUniverseRefresh refresh,
        IReadOnlyList<MarketInstrument> rawCachedEtfs,
        IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>> cachedEtfCandles,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>>> cachedEtfCandlesByResolution,
        CancellationToken cancellationToken)
    {
        const int batchSize = 100;
        try
        {
            if (!_universeRefreshGate.IsCurrent(refresh)) return;
            AppendActivity(TerminalActivitySeverity.Info, "Universe discovery", $"Refreshing {UniverseLabel(universe).ToLowerInvariant()} in the background.", $"Refresh generation {refresh.Generation}.");
            await PublishTerminalActivityAsync();
            if (universe == TerminalUniverseKind.ETFs && rawCachedEtfs.Count > 0 &&
                TerminalUniverseLoadPolicy.RequiresEtfMetadataEnrichment(universe, rawCachedEtfs))
            {
                await EnrichCachedEtfUniverseInBackgroundAsync(
                    rawCachedEtfs,
                    cachedEtfCandles,
                    cachedEtfCandlesByResolution,
                    refresh,
                    cancellationToken);
            }

            if (!_universeRefreshGate.IsCurrent(refresh)) return;
            var markets = await SearchMarketsWithRetryAsync(universe, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (!_universeRefreshGate.IsCurrent(refresh)) return;
            var normalized = TerminalUniverseLoadPolicy.NormalizeApiFallback(universe, markets, _knownEtfEpics);
            if (universe == TerminalUniverseKind.Crypto && normalized.Any(CryptoMarketMetadataEnricher.NeedsEnrichment))
            {
                normalized = (await _cryptoMetadataEnricher.EnrichAsync(normalized, null, cancellationToken)).ToList();
                normalized = TerminalUniverseLoadPolicy.NormalizeApiFallback(universe, normalized, _knownEtfEpics);
            }

            if (!_universeRefreshGate.IsCurrent(refresh)) return;
            var accumulator = GetUniverseAccumulator(universe);
            var batches = normalized.Chunk(batchSize).ToArray();
            if (batches.Length == 0)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    if (!_universeRefreshGate.IsCurrent(refresh)) return;
                    PublishUniverseSnapshot(
                        universe,
                        accumulator.MergeDiscoveryBatch([], 0, isComplete: true),
                        CachedCandles(universe),
                        CachedCandlesByResolution(universe),
                        refresh);
                });
            }
            else
            {
                for (var index = 0; index < batches.Length; index++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!_universeRefreshGate.IsCurrent(refresh)) return;
                    var snapshot = accumulator.MergeDiscoveryBatch(
                        batches[index],
                        normalized.Count,
                        isComplete: index == batches.Length - 1);
                    if (!_universeRefreshGate.IsCurrent(refresh)) return;
                    await Dispatcher.InvokeAsync(() =>
                    {
                        if (!_universeRefreshGate.IsCurrent(refresh)) return;
                        PublishUniverseSnapshot(
                            universe,
                            snapshot,
                            CachedCandles(universe),
                            CachedCandlesByResolution(universe),
                            refresh);
                    });
                    await Task.Delay(TimeSpan.FromMilliseconds(40), cancellationToken);
                }
            }

            if (_universeRefreshGate.IsCurrent(refresh))
            {
                AppendActivity(TerminalActivitySeverity.Success, "Universe discovery", $"{normalized.Count} {UniverseLabel(universe).ToLowerInvariant()} discovered.");
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            AppendActivity(TerminalActivitySeverity.Info, "Universe discovery", $"{UniverseLabel(universe)} refresh cancelled.");
        }
        catch (Exception ex)
        {
            AppendActivity(TerminalActivitySeverity.Warning, "Universe discovery", $"{UniverseLabel(universe)} refresh failed.", ex.Message);
            if (!_windowLifetime.IsClosing && _universeRefreshGate.IsCurrent(refresh) && SelectedUniverse() == universe)
            {
                _statusArbiter.TryPublishBackground($"{UniverseLabel(universe)} refresh failed: {ex.Message}");
            }
        }
        finally
        {
            if (!_windowLifetime.IsClosing) await PublishTerminalActivityAsync();
        }
    }

    private async Task EnrichCachedEtfUniverseInBackgroundAsync(
        IReadOnlyList<MarketInstrument> rawCachedEtfs,
        IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>> candles,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>>> candlesByResolution,
        TerminalUniverseRefresh refresh,
        CancellationToken cancellationToken)
    {
        var enriched = await EnrichEtfMetadataAsync(rawCachedEtfs, cancellationToken, reportProgress: null);
        if (!_universeRefreshGate.IsCurrent(refresh)) return;
        var snapshot = GetUniverseAccumulator(TerminalUniverseKind.ETFs).ReplaceCachedMetadata(enriched);
        if (!_universeRefreshGate.IsCurrent(refresh)) return;
        await Dispatcher.InvokeAsync(() =>
        {
            if (!_universeRefreshGate.IsCurrent(refresh)) return;
            PublishUniverseSnapshot(TerminalUniverseKind.ETFs, snapshot, candles, candlesByResolution, refresh);
        });
    }

    private async Task<IReadOnlyList<MarketInstrument>> SearchMarketsWithRetryAsync(
        TerminalUniverseKind universe,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await _universeDiscoveryRequestGate.WaitAsync(cancellationToken);
                try
                {
                    await WaitForUniverseDiscoveryRequestSlotAsync(cancellationToken);
                    return await _api.SearchMarketsAsync(TerminalUniverseLoadPolicy.ApiSearchTerm(universe, ""), cancellationToken);
                }
                finally
                {
                    _universeDiscoveryRequestGate.Release();
                }
            }
            catch (Exception ex) when (TerminalUniverseDiscoveryRetryPolicy.ShouldRetry(ex, attempt))
            {
                await Task.Delay(TerminalUniverseDiscoveryRetryPolicy.BackoffForFailedAttempt(attempt), cancellationToken);
            }
        }
    }

    private async Task WaitForUniverseDiscoveryRequestSlotAsync(CancellationToken cancellationToken)
    {
        var remainingTicks = _nextUniverseDiscoveryRequestTimestamp - Stopwatch.GetTimestamp();
        if (remainingTicks > 0)
        {
            await Task.Delay(TimeSpan.FromSeconds((double)remainingTicks / Stopwatch.Frequency), cancellationToken);
        }
        _nextUniverseDiscoveryRequestTimestamp = Stopwatch.GetTimestamp() +
            (long)Math.Ceiling(minimumUniverseDiscoveryRequestSpacing.TotalSeconds * Stopwatch.Frequency);
    }

    private void PublishUniverseSnapshot(
        TerminalUniverseKind universe,
        TerminalUniverseSnapshot snapshot,
        IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>> candles,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>>> candlesByResolution,
        TerminalUniverseRefresh refresh,
        bool persist = true)
    {
        if (!_universeRefreshGate.IsCurrent(refresh)) return;
        if (SelectedUniverse() == universe)
        {
            var selection = GetUniverseAccumulator(universe).PreserveSelection(CaptureUniverseSelection(), snapshot);
            ApplyUniverse(universe, snapshot.Instruments, candles, candlesByResolution, selection);
            if (snapshot.Progress.Stage == TerminalUniverseStage.Discovering)
            {
                _statusArbiter.TryPublishBackground(
                    $"Discovering {UniverseLabel(universe).ToLowerInvariant()}: {snapshot.Progress.Discovered}/{snapshot.Progress.TotalDiscovered}.");
            }
        }

        if (!persist || !_universeRefreshGate.IsCurrent(refresh)) return;
        try
        {
            _universeCache.Save(universe, snapshot.Instruments);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>> CachedCandles(TerminalUniverseKind universe) =>
        _candlesByUniverse.TryGetValue(universe, out var candles) ? candles : EmptyCandles();

    private IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>>> CachedCandlesByResolution(TerminalUniverseKind universe) =>
        _candlesByUniverseByResolution.TryGetValue(universe, out var candles) ? candles : EmptyCandlesByResolution();

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
        if (strategy == SyntheticStrategyKind.ManualFormula)
        {
            await BuildManualSyntheticAsync(block, resolution, minCandles, cancellationToken);
            return;
        }
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
        _operationState.BeginStage("Applying Capital deal rules");
        await Task.Yield();
        _basket = SyntheticHistoryService.BuildSelected(
            block,
            selectedComponents,
            selectedHistory,
            resolution,
            periodsPerYear,
            minCandles);
        if (_basket is null)
        {
            await ClearTerminalChartAsync();
            StatusText.Text = "The selected legs cannot form an approximately equal, executable one-lot formula within Capital.com deal rules.";
            return;
        }
        _basket.UniverseKind = SelectedUniverse();
        _activeBasket.Activate(_basket, strategy);
        _operationState.BeginStage("Rendering synthetic chart");
        await Task.Yield();
        await RenderSyntheticChartAsync(_basket);
        var buildStatus = $"{_basket.Symbol}: {_basket.Components.Count} legs, {HistoryRange(selectedHistory)}, similarity {_basket.SimilarityScore:0.##}, average volatility {_basket.AverageVolatilityPct:0.##}%.";
        StatusText.Text = buildStatus;
        await TryStartStreamingCurrentBasketAsync(buildStatus, cancellationToken);
    }

    private async Task BuildManualSyntheticAsync(
        string block,
        string resolution,
        int minimumCandles,
        CancellationToken cancellationToken)
    {
        if (SelectedUniverse() != TerminalUniverseKind.Crypto)
        {
            throw new InvalidOperationException("Manual formulas require the Crypto universe.");
        }

        var formula = ManualSyntheticFormula.Parse(ManualFormulaBox.Text);
        var selectedComponents = ManualSyntheticBasketFactory.Resolve(block, formula, _instruments);
        StatusText.Text = $"Loading {resolution} history for manual formula...";
        var selectedHistory = await LoadSelectedHistoryAsync(
            selectedComponents,
            resolution,
            cancellationToken,
            exactTimestamps: true);
        var sharedRange = ManualSyntheticBasketFactory.ExactSharedRange(
            selectedHistory.CandlesByEpic,
            selectedComponents.Select(component => component.Epic));
        if (sharedRange.Count < minimumCandles)
        {
            throw new InvalidOperationException(
                $"The manual formula legs have only {sharedRange.Count} exact shared {resolution} timestamps; {minimumCandles} are required.");
        }

        _operationState.BeginStage("Building manual formula");
        var symbol = formula.IsCryptoPreset ? "SYN-CRYPTO-ETHBTC-01" : "SYN-CRYPTO-MANUAL-01";
        _basket = await Task.Run(() => ManualSyntheticBasketFactory.Create(
            symbol,
            block,
            formula,
            _instruments,
            selectedHistory.CandlesByEpic,
            resolution,
            minimumCandles), cancellationToken);
        _activeBasket.Activate(_basket, SyntheticStrategyKind.ManualFormula);
        cancellationToken.ThrowIfCancellationRequested();

        await RefreshBasketMarketDetailsAsync(_basket, cancellationToken);
        _operationState.BeginStage("Rendering synthetic chart");
        await RenderSyntheticChartAsync(_basket);
        var buildStatus = $"{_basket.Symbol}: {_basket.Components.Count} manual legs, {BasketHistoryRange(_basket)}, direct formula scale.";
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

        var name = _activeBasket.SuggestedSavedBasketName();
        _savedBasketStore.Save(_activeBasket.CreateSavedBasket(name, SelectedUniverse()));
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
        var universeResolution = await ResolveBasketUniverseAsync(saved, cancellationToken);
        var universe = universeResolution.Universe;
        await EnsureBasketUniverseAsync(universe, cancellationToken);
        await EnsureBasketLegsAvailableAsync(
            universeResolution,
            saved.Components.Select(component => component.Epic).ToArray(),
            cancellationToken);

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

        var selectedHistory = await LoadSelectedHistoryAsync(
            selectedInstruments,
            resolution,
            cancellationToken,
            exactTimestamps: saved.Strategy == SyntheticStrategyKind.ManualFormula);
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
        _basket.UniverseKind = universe;
        _activeBasket.Activate(_basket, restored.Strategy);
        StrategyBox.SelectedValue = restored.Strategy;
        if (restored.Strategy == SyntheticStrategyKind.ManualFormula)
        {
            ManualFormulaBox.Text = ManualSyntheticFormula.Format(saved.Components);
        }
        await RefreshBasketMarketDetailsAsync(_basket, cancellationToken);
        _operationState.BeginStage("Rendering synthetic chart");
        await Task.Yield();
        await RenderSyntheticChartAsync(_basket);
        var loadStatus = $"Loaded saved basket {saved.Name}: {_basket.Components.Count} legs, {BasketHistoryRange(_basket)}.";
        StatusText.Text = loadStatus;
        await TryStartStreamingCurrentBasketAsync(loadStatus, cancellationToken);
    }

    private async Task LoadExecutionBasketAsync(string executionId, CancellationToken cancellationToken)
    {
        var execution = _terminalExecutions.FirstOrDefault(record =>
            string.Equals(record.ExecutionId, executionId, StringComparison.Ordinal));
        if (execution is null)
        {
            throw new InvalidOperationException("The selected execution is no longer available. Refresh positions and try again.");
        }
        var universeResolution = await ResolveBasketUniverseAsync(execution, cancellationToken);
        var universe = universeResolution.Universe;
        await EnsureBasketUniverseAsync(universe, cancellationToken);
        await EnsureBasketLegsAvailableAsync(
            universeResolution,
            execution.Legs.Select(leg => leg.Epic).ToArray(),
            cancellationToken);

        var saved = SyntheticExecutionBasketSnapshot.Create(execution, _instruments);
        var selectedInstruments = saved.Components
            .Select(component => _instruments.Single(instrument =>
                string.Equals(instrument.Epic, component.Epic, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        var resolution = SelectedResolution();
        var history = await LoadSelectedHistoryAsync(
            selectedInstruments,
            resolution,
            cancellationToken,
            exactTimestamps: saved.Strategy == SyntheticStrategyKind.ManualFormula);
        _operationState.BeginStage("Restoring executed basket");
        await Task.Yield();
        var restored = SavedSyntheticBasketRestorer.Restore(
            saved,
            selectedInstruments,
            history,
            resolution,
            PeriodsPerYear(resolution),
            MinimumCandles(resolution));
        if (restored is null)
        {
            throw new InvalidOperationException("The executed basket has no usable shared history for this timeframe.");
        }

        _basket = restored.Basket;
        _basket.UniverseKind = universe;
        _activeBasket.Activate(_basket, restored.Strategy);
        StrategyBox.SelectedValue = restored.Strategy;
        await RefreshBasketMarketDetailsAsync(_basket, cancellationToken);
        _operationState.BeginStage("Rendering executed basket");
        await Task.Yield();
        await RenderSyntheticChartAsync(
            _basket,
            SyntheticTerminalWorkspace.ExecutionDrawingIdentity(execution.ExecutionId));
        var status = $"Loaded executed basket {_basket.Symbol}: {_basket.Components.Count} legs, {BasketHistoryRange(_basket)}.";
        StatusText.Text = status;
        await TryStartStreamingCurrentBasketAsync(status, cancellationToken);
        await PublishBrokerSnapshotAsync(cancellationToken);
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
        LoadUniverseAsync(SelectedUniverse(), cancellationToken, waitForDiscovery: true);

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
        CancellationToken cancellationToken,
        bool exactTimestamps = false)
    {
        await EnsureConnectedAsync(cancellationToken);
        var missingComponents = _historySessionCache.Missing(selectedComponents, resolution);
        if (missingComponents.Count > 0)
        {
            _operationState.BeginStage($"Loading missing {resolution} history", missingComponents.Count);
            var progress = new Progress<HistoryLoadProgress>(update =>
            {
                if (_windowLifetime.IsClosing) return;
                _operationState.Report($"Loading missing {resolution} history", update.CompletedComponents, update.TotalComponents);
                StatusText.Text = $"Loading {resolution} history for uncached leg {update.CompletedComponents} of {update.TotalComponents}: {update.Epic}.";
            });
            var apiHistory = await _history.LoadSelectedAsync(missingComponents, resolution, progress, cancellationToken);
            _historySessionCache.Store(resolution, apiHistory);
        }
        else
        {
            _operationState.BeginStage($"Reusing cached {resolution} history");
            StatusText.Text = $"Reusing cached {resolution} history for {selectedComponents.Count} legs.";
        }

        cancellationToken.ThrowIfCancellationRequested();
        var sessionHistory = new HistoryLoadResult(
            _historySessionCache.Get(selectedComponents, resolution),
            null,
            null,
            0);
        return exactTimestamps
            ? SyntheticHistoryService.MergeSelectedManualHistory(
                selectedComponents,
                resolution,
                sessionHistory,
                CachedCandlesForResolution(resolution))
            : SyntheticHistoryService.MergeSelectedHistory(
                selectedComponents,
                resolution,
                sessionHistory,
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

    private static string BasketHistoryRange(SyntheticBasket basket) => basket.Candles.Count > 0
        ? $"{basket.Candles.Count} shared candles from {basket.Candles[0].Time:yyyy-MM-dd} to {basket.Candles[^1].Time:yyyy-MM-dd}"
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
        await EnsureConnectedAsync(cancellationToken);
        if (_basket is null) return;
        var subscriptionCount = _basket.Strategy == SyntheticStrategyKind.ManualFormula &&
            SyntheticRealtimeBarBuilder.UsesNativeOhlc(SelectedResolution()) ? 2 : 1;
        _operationState.BeginStage("Starting live stream", subscriptionCount);

        if (_streaming is null || !_streaming.IsConnected)
        {
            if (_streaming is not null) await _streaming.DisposeAsync();
            cancellationToken.ThrowIfCancellationRequested();
            var streaming = new CapitalStreamingClient();
            streaming.QuoteReceived += Streaming_QuoteReceived;
            streaming.OhlcReceived += Streaming_OhlcReceived;
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
        await SyntheticStreamingSubscription.SubscribeAsync(
            _streaming,
            _api.Session!,
            _basket,
            SelectedResolution(),
            cancellationToken);
        _operationState.Report("Starting live stream", subscriptionCount, subscriptionCount);
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
        if (details.MaxDealSize is > 0) target.MaxDealSize = details.MaxDealSize;
        target.MarketModes = details.MarketModes;
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
        var history = await LoadSelectedHistoryAsync(
            selectedComponents,
            resolution,
            cancellationToken,
            exactTimestamps: existingBasket.Strategy == SyntheticStrategyKind.ManualFormula);
        _operationState.BeginStage("Building selected basket");
        await Task.Yield();
        var rebuilt = _activeBasket.RebuildHistory(
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
        _basket.UniverseKind = existingBasket.UniverseKind;
        await RefreshBasketMarketDetailsAsync(_basket, cancellationToken);
        _operationState.BeginStage("Rendering synthetic chart");
        await Task.Yield();
        await RenderSyntheticChartAsync(_basket, _terminalDrawingIdentity);
        var reloadStatus = $"{_basket.Symbol}: reloaded {resolution} history for the same {_basket.Components.Count} legs, {BasketHistoryRange(_basket)}.";
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
                SelectedResolution(),
                _terminalDrawingIdentity);
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
        await PublishTerminalRiskPlansAsync();
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
        AppendActivity(TerminalActivitySeverity.Info, operationName, "Started");
        try
        {
            var completed = await TerminalOperationExecution.RunAsync(
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
                    AppendActivity(TerminalActivitySeverity.Error, operationName, ex.Message, ex.ToString());
                    _operationState.Fail(ex.Message);
                    ConnectionText.Text = operationName.StartsWith("Connecting", StringComparison.Ordinal)
                        ? $"Connection failed: {ex.Message}"
                        : ConnectionText.Text;
                    StatusText.Text = ex.Message;
                }));
            if (completed) AppendActivity(TerminalActivitySeverity.Success, operationName, "Completed");
            return completed;
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
            if (!_windowLifetime.IsClosing) await PublishTerminalActivityAsync();
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

    private void RebuildBlocks(TerminalUniverseSelection? selection = null)
    {
        var controls = _universeUi.BuildControls(_instruments);
        var selectedBlock = !string.IsNullOrWhiteSpace(selection?.Block) &&
                            controls.Blocks.Contains(selection.Block, StringComparer.OrdinalIgnoreCase)
            ? selection.Block
            : controls.SelectedBlock;
        BlockBox.ItemsSource = controls.Blocks;
        BlockBox.SelectedItem = selectedBlock;
        ApplySeedOptions(_universeUi.BuildSeedOptions(_instruments, selectedBlock));
        if (selection is not null) SearchBox.Text = selection.SeedText;
    }

    private void RefreshSavedBaskets(string? selectedName = null) =>
        RefreshSavedBaskets(_savedBasketStore.LoadAll(), selectedName);

    private void RefreshSavedBaskets(IReadOnlyList<SavedSyntheticBasket> saved, string? selectedName = null)
    {
        _loadingSavedBaskets = true;
        try
        {
            SavedBasketsBox.ItemsSource = saved;
            SavedBasketsBox.DisplayMemberPath = nameof(SavedSyntheticBasket.DisplayLabel);
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
        var options = _universeUi.BuildSeedOptions(_instruments, SelectedBlock());
        ApplySeedOptions(options);
    }

    private void ApplySeedOptions(IReadOnlyList<string> options)
    {
        if (SearchBox is null) return;
        var current = SearchBox.Text;
        SearchBox.ItemsSource = options;
        SearchBox.Text = current;
    }

    private void BlockBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => RebuildSeedOptions();

    private void StrategyBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ManualFormulaBox is null || SearchBox is null) return;
        var isManual = SelectedStrategy() == SyntheticStrategyKind.ManualFormula;
        ManualFormulaBox.Visibility = isManual ? Visibility.Visible : Visibility.Collapsed;
        SearchBox.Visibility = isManual ? Visibility.Collapsed : Visibility.Visible;
        if (isManual && string.IsNullOrWhiteSpace(ManualFormulaBox.Text))
        {
            ManualFormulaBox.Text = ManualSyntheticFormula.CryptoPreset;
        }
    }

    private async void UniverseBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_api.Session is null || _changingUniverseProgrammatically) return;
        var universe = SelectedUniverse();
        await RunOperationAsync($"Loading {UniverseLabel(universe).ToLowerInvariant()} universe", async cancellationToken =>
        {
            await _universeUi.SwitchAsync(
                ClearTerminalChartAsync,
                () => LoadUniverseAsync(universe, cancellationToken));
        });
    }

    private async void Streaming_OhlcReceived(object? sender, CapitalOhlcUpdate update)
    {
        if (_windowLifetime.IsClosing) return;
        await Dispatcher.InvokeAsync(() =>
        {
            if (_windowLifetime.IsClosing || _basket is null || !_realtimeBars.Apply(_basket, update)) return;
            var observedAt = DateTimeOffset.UtcNow;
            var tick = SyntheticTerminalLiveUpdate.BuildTick(
                _basket,
                _basket.Candles[^1],
                observedAt,
                _terminalDrawingIdentity);
            _ = SendTerminalTickAsync(tick);
            StatusText.Text = $"{_basket.Symbol}: ongoing {SelectedResolution()} candle {update.Time.ToLocalTime():yyyy-MM-dd HH:mm}.";
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
        (UniverseBox.SelectedItem as ComboBoxItem)?.Content?.ToString() switch
        {
            "Stocks" => TerminalUniverseKind.Stocks,
            "ETFs" => TerminalUniverseKind.ETFs,
            "Crypto" => TerminalUniverseKind.Crypto,
            _ => TerminalUniverseKind.Stocks,
        };

    private async Task EnsureBasketUniverseAsync(
        TerminalUniverseKind universe,
        CancellationToken cancellationToken)
    {
        await _universeUi.EnsureActiveAsync(
            SelectedUniverse(),
            universe,
            _instruments,
            _knownEtfEpics,
            SelectUniverse,
            ClearTerminalChartAsync,
            selected => LoadUniverseAsync(selected, cancellationToken));
    }

    private async Task<SyntheticBasketUniverseResolution> ResolveBasketUniverseAsync(
        SavedSyntheticBasket saved,
        CancellationToken cancellationToken)
    {
        if (!SyntheticBasketUniverseResolver.TryResolve(saved, _knownEtfEpics, out _))
        {
            EnsureEtfCatalogLoaded();
        }
        return await SyntheticBasketUniverseResolver.ResolveAsync(
            saved,
            _knownEtfEpics,
            CachedUniverseInstruments(),
            ProbeInstrumentAsync,
            cancellationToken);
    }

    private async Task<SyntheticBasketUniverseResolution> ResolveBasketUniverseAsync(
        SyntheticExecutionRecord execution,
        CancellationToken cancellationToken)
    {
        if (!SyntheticBasketUniverseResolver.TryResolve(execution, _knownEtfEpics, out _))
        {
            EnsureEtfCatalogLoaded();
        }
        return await SyntheticBasketUniverseResolver.ResolveAsync(
            execution,
            _knownEtfEpics,
            CachedUniverseInstruments(),
            ProbeInstrumentAsync,
            cancellationToken);
    }

    private IReadOnlyDictionary<TerminalUniverseKind, IReadOnlyList<MarketInstrument>> CachedUniverseInstruments()
    {
        var result = new Dictionary<TerminalUniverseKind, IReadOnlyList<MarketInstrument>>();
        foreach (var universe in Enum.GetValues<TerminalUniverseKind>())
        {
            if (_universeUi.TryGetCached(universe, out var instruments)) result[universe] = instruments;
        }
        if (!result.ContainsKey(SelectedUniverse()) && _instruments.Count > 0)
        {
            result[SelectedUniverse()] = _instruments.ToArray();
        }
        return result;
    }

    private async Task<MarketInstrument?> ProbeInstrumentAsync(string epic, CancellationToken cancellationToken)
    {
        await EnsureConnectedAsync(cancellationToken);
        return await _api.GetMarketDetailsAsync(epic, cancellationToken);
    }

    private async Task EnsureBasketLegsAvailableAsync(
        SyntheticBasketUniverseResolution resolution,
        IReadOnlyList<string> requiredEpics,
        CancellationToken cancellationToken)
    {
        var instruments = _instruments
            .Concat(resolution.Instruments)
            .Where(instrument => !string.IsNullOrWhiteSpace(instrument.Epic))
            .GroupBy(instrument => instrument.Epic, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
        var available = instruments.Select(instrument => instrument.Epic).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var epic in requiredEpics.Where(epic => !available.Contains(epic)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var instrument = await ProbeInstrumentAsync(epic, cancellationToken);
            if (instrument is null || !string.Equals(instrument.Epic?.Trim(), epic, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Basket leg {epic} is missing from the resolved {UniverseLabel(resolution.Universe)} universe.");
            }
            instruments.Add(instrument);
            available.Add(epic);
        }

        var invalid = requiredEpics.FirstOrDefault(epic =>
        {
            var instrument = instruments.First(candidate =>
                string.Equals(candidate.Epic, epic, StringComparison.OrdinalIgnoreCase));
            return !TerminalUniverse.Accepts(resolution.Universe, instrument, _knownEtfEpics);
        });
        if (invalid is not null)
        {
            throw new InvalidOperationException(
                $"Basket leg {invalid} does not match the resolved {UniverseLabel(resolution.Universe)} universe type.");
        }

        if (instruments.Count != _instruments.Count)
        {
            ApplyUniverse(
                resolution.Universe,
                instruments,
                _cachedCandlesByEpic,
                _cachedCandlesByEpicByResolution);
        }
    }

    private void SelectUniverse(TerminalUniverseKind universe)
    {
        if (SelectedUniverse() == universe) return;
        _changingUniverseProgrammatically = true;
        try
        {
            var label = UniverseLabel(universe);
            UniverseBox.SelectedItem = UniverseBox.Items
                .OfType<ComboBoxItem>()
                .First(item => string.Equals(item.Content?.ToString(), label, StringComparison.Ordinal));
        }
        finally
        {
            _changingUniverseProgrammatically = false;
        }
    }

    private static string UniverseLabel(TerminalUniverseKind universe) =>
        universe switch
        {
            TerminalUniverseKind.Stocks => "Stocks",
            TerminalUniverseKind.ETFs => "ETFs",
            TerminalUniverseKind.Crypto => "Crypto",
            _ => throw new ArgumentOutOfRangeException(nameof(universe), universe, null),
        };

    private EtfDataLoadResult? EnsureEtfCatalogLoaded()
    {
        var cache = _etfCatalog.LoadOnce(() => DashboardEtfDataLoader.LoadEtfs());
        _knownEtfEpics = _etfCatalog.KnownEtfEpics;
        return cache;
    }

    private async Task<IReadOnlyList<MarketInstrument>> EnrichEtfMetadataAsync(
        IReadOnlyList<MarketInstrument> instruments,
        CancellationToken cancellationToken,
        Action<int, int>? reportProgress = null)
    {
        await EnsureConnectedAsync(cancellationToken);

        var enriched = new List<MarketInstrument>(instruments.Count);
        reportProgress?.Invoke(0, instruments.Count);
        var completed = 0;
        foreach (var instrument in instruments)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_knownEtfEpics.Contains(instrument.Epic) || !EtfMetadataMerger.NeedsEnrichment(instrument))
            {
                enriched.Add(instrument);
                completed++;
                reportProgress?.Invoke(completed, instruments.Count);
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
                reportProgress?.Invoke(completed, instruments.Count);
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
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>>> candlesByResolution,
        TerminalUniverseSelection? selection = null)
    {
        selection ??= CaptureUniverseSelection();
        var accepted = instruments.Where(item => TerminalUniverse.Accepts(universe, item, _knownEtfEpics)).ToList();
        if (universe == TerminalUniverseKind.Crypto)
        {
            accepted = TerminalCryptoUniverseGrouping.Normalize(accepted).ToList();
        }
        _universeUi.Cache(universe, accepted);
        _candlesByUniverse[universe] = candles;
        _candlesByUniverseByResolution[universe] = candlesByResolution;
        _instruments.Clear();
        _instruments.AddRange(accepted);
        _cachedCandlesByEpic = candles;
        _cachedCandlesByEpicByResolution = candlesByResolution;
        RebuildBlocks(selection);
    }

    private TerminalUniverseSelection CaptureUniverseSelection() =>
        new(SelectedBlock(), SearchBox?.Text ?? "");

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

    private static SyntheticBasket RenameBasket(SyntheticBasket source, string symbol, string block)
    {
        var renamed = new SyntheticBasket
        {
            Symbol = symbol,
            Block = block,
            UniverseKind = source.UniverseKind,
            Strategy = source.Strategy,
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

    private async Task RenderSyntheticChartAsync(SyntheticBasket basket, string? drawingIdentity = null)
    {
        var previousDrawingIdentity = _terminalDrawingIdentity;
        _terminalDrawingIdentity = string.IsNullOrWhiteSpace(drawingIdentity)
            ? SyntheticTerminalChartPayload.DrawingIdentity(basket)
            : drawingIdentity.Trim();
        _realtimeBars.Reset(basket, SelectedResolution());
        var payload = SyntheticTerminalChartPayload.Build(basket, drawingIdentity: _terminalDrawingIdentity);
        if (!string.Equals(previousDrawingIdentity, _terminalDrawingIdentity, StringComparison.Ordinal)
            && payload.SuggestedBasketQuantity is decimal suggestedQuantity
            && suggestedQuantity > 0m)
        {
            _marginPreviewNotional = suggestedQuantity;
        }
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
        decimal syntheticLots,
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
        var preferences = await _api.GetAccountPreferencesAsync(cancellationToken);
        if (!preferences.HedgingMode && _api.IsDemoTradingSession)
        {
            _operationState.BeginStage("Enabling demo hedging mode");
            _windowLifetime.TryApply(() => StatusText.Text = "Enabling Capital.com demo hedging mode for synthetic execution...");
            await _api.SetHedgingModeAsync(true, cancellationToken);
            preferences = await _api.GetAccountPreferencesAsync(cancellationToken);
        }
        var accountId = _api.Session?.CurrentAccountId ?? "";
        _marginPreview.InvalidateCaches();
        var margin = await _marginPreview.BuildAsync(freshBasket, syntheticLots, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        var result = SyntheticTradePreflight.Build(new SyntheticPreflightInput(
            _api.IsDemoTradingSession,
            SyntheticTerminalChartPayload.DrawingIdentity(freshBasket),
            freshBasket,
            side,
            syntheticLots,
            DateTimeOffset.UtcNow,
            margin,
            accountId,
            preferences.HedgingMode,
            basket.UniverseKind ?? SelectedUniverse(),
            syntheticLots));
        result = _tradingCoordinator.RegisterPreflight(result);
        await PublishTerminalPreflightAsync(result);
    }

    private async Task ExecuteSyntheticBasketAsync(Guid ticketId)
    {
        var cancellationToken = _windowLifetime.Token;
        if (cancellationToken.IsCancellationRequested) return;
        const string operationName = "Executing synthetic basket";
        if (!_operationState.TryBegin(operationName))
        {
            _windowLifetime.TryApply(() => StatusText.Text = $"{_operationState.Label} is already running.");
            return;
        }

        SyntheticHostExecution? execution = null;
        SyntheticTradingWindowLifecycleCoordinator.TrackedOperation? trackedOperation = null;
        try
        {
            var frozenTicket = _tradingCoordinator.GetRegisteredTicket(ticketId);
            await RevalidateExecutionTicketAsync(frozenTicket, cancellationToken);
            execution = _tradingCoordinator.BeginExecution(ticketId);
            trackedOperation = _tradingLifecycle.BeginOperation();
        }
        catch (Exception ex)
        {
            _tradingCoordinator.DiscardTicket(ticketId);
            execution?.Dispose();
            _operationState.Fail(ex.Message);
            _windowLifetime.TryApply(() => StatusText.Text = ex.Message);
            await PublishTerminalExecutionErrorAsync(ex.Message);
            return;
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
                await PublishBrokerSnapshotAsync(token);
                await ActivateExecutedBasketChartAsync(execution.Ticket.TicketId, token);
            },
            cancellationToken);
        var finished = FinishSyntheticExecutionAsync(execution, operation);
        trackedOperation.Track(finished);
        await finished;
    }

    private async Task ActivateExecutedBasketChartAsync(string ticketId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_basket is null || string.IsNullOrWhiteSpace(ticketId)) return;
        var matches = _terminalExecutions
            .Where(record => string.Equals(record.TicketId, ticketId, StringComparison.Ordinal))
            .ToArray();
        if (matches.Length != 1) return;
        var execution = matches[0];
        if (!string.Equals(
                execution.BasketId,
                SyntheticTerminalChartPayload.DrawingIdentity(_basket),
                StringComparison.Ordinal)) return;

        await RenderSyntheticChartAsync(
            _basket,
            SyntheticTerminalWorkspace.ExecutionDrawingIdentity(execution.ExecutionId));
    }

    private async Task RevalidateExecutionTicketAsync(
        SyntheticExecutionTicket frozenTicket,
        CancellationToken cancellationToken)
    {
        var basket = _basket ?? throw new InvalidOperationException("The confirmed basket is no longer loaded.");
        if (!string.Equals(
                frozenTicket.BasketId,
                SyntheticTerminalChartPayload.DrawingIdentity(basket),
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The loaded basket changed after confirmation. Run preflight again.");
        }

        await EnsureConnectedAsync(cancellationToken);
        _operationState.BeginStage("Final market checks", basket.Components.Count);
        var snapshotResult = await _preflightMarketSnapshots.LoadAsync(
            basket,
            cancellationToken,
            (completed, total) => _operationState.Report("Final market checks", completed, total));
        if (snapshotResult.Basket is null)
        {
            SyntheticExecutionTicketRevalidation.Validate(
                frozenTicket,
                new SyntheticPreflightResult(false, null, snapshotResult.Failures));
            return;
        }

        var currentBasket = snapshotResult.Basket;
        var preferences = await _api.GetAccountPreferencesAsync(cancellationToken);
        _marginPreview.InvalidateCaches();
        var syntheticLots = frozenTicket.BasketQuantity
            ?? throw new InvalidOperationException("The confirmed ticket does not contain a synthetic lot count.");
        var margin = await _marginPreview.BuildAsync(currentBasket, syntheticLots, cancellationToken);
        var current = SyntheticTradePreflight.Build(new SyntheticPreflightInput(
            _api.IsDemoTradingSession,
            SyntheticTerminalChartPayload.DrawingIdentity(currentBasket),
            currentBasket,
            frozenTicket.Side,
            syntheticLots,
            DateTimeOffset.UtcNow,
            margin,
            _api.Session?.CurrentAccountId ?? "",
            preferences.HedgingMode,
            frozenTicket.UniverseKind,
            syntheticLots));
        SyntheticExecutionTicketRevalidation.Validate(frozenTicket, current);
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

    private async Task RefreshSyntheticExecutionsAsync(CancellationToken cancellationToken)
    {
        await _tradingCoordinator.RefreshAsync(PublishTerminalExecutionsAsync, cancellationToken);
        await PublishBrokerSnapshotAsync(cancellationToken);
    }

    private async Task CloseSyntheticBasketAsync(string executionId, CancellationToken cancellationToken)
    {
        var trackedOperation = _tradingLifecycle.BeginOperation();
        var operation = _tradingCoordinator.CloseAsync(
            executionId,
            PublishTerminalExecutionProgressAsync,
            PublishTerminalExecutionsAsync,
            trackedOperation.MarkMutationDispatched,
            cancellationToken);
        trackedOperation.Track(operation);
        await operation;
        await _tradingCoordinator.RefreshAsync(PublishTerminalExecutionsAsync, cancellationToken);
        await PublishBrokerSnapshotAsync(cancellationToken);
    }

    private async Task RunBrokerRefreshLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(3));
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                if (_api.Session is null || !_chartReady || _operationState.IsBusy) continue;
                await PublishBrokerSnapshotAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private async Task PublishBrokerSnapshotAsync(CancellationToken cancellationToken)
    {
        if (_api.Session is null || !await _brokerRefreshGate.WaitAsync(0, cancellationToken)) return;
        try
        {
            var snapshot = await _api.GetBrokerSnapshotAsync(cancellationToken);
            await PublishTerminalCallbackAsync("setTerminalBrokerSnapshot", snapshot);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            await PublishTerminalCallbackAsync("setTerminalBrokerSnapshot", new { Error = ex.Message });
        }
        finally
        {
            _brokerRefreshGate.Release();
        }
    }

    private Task PublishTerminalPreflightAsync(SyntheticPreflightResult result) =>
        PublishTerminalCallbackAsync("setTerminalPreflight", result);

    private async Task PublishTerminalExecutionsAsync(IReadOnlyList<SyntheticExecutionRecord> records)
    {
        _terminalExecutions = records.ToArray();
        await PublishTerminalCallbackAsync("setTerminalExecutions", records);
        await PublishTerminalRiskPlansAsync();
        if (string.IsNullOrWhiteSpace(_tradingCoordinator.PersistenceWarning)) return;
        _windowLifetime.TryApply(() => StatusText.Text = _tradingCoordinator.PersistenceWarning);
        await PublishTerminalExecutionErrorAsync(_tradingCoordinator.PersistenceWarning);
    }

    private Task PublishTerminalExecutionProgressAsync(SyntheticExecutionRecord record) =>
        PublishTerminalCallbackAsync("setTerminalExecutionProgress", record);

    private Task PublishTerminalExecutionErrorAsync(string error) =>
        PublishTerminalCallbackAsync("setTerminalExecutionProgress", new { Error = error });

    private Task PublishTerminalRiskPlansAsync(string executionId = "", long revision = 0) =>
        PublishTerminalCallbackAsync("setTerminalRiskPlans", new
        {
            ExecutionId = executionId,
            Revision = revision,
            Plans = _riskPlanStore.LoadAll()
        });

    private Task PublishTerminalRiskPlanErrorAsync(string executionId, long revision, string error) =>
        PublishTerminalCallbackAsync("setTerminalRiskPlanError", new
        {
            ExecutionId = executionId,
            Revision = revision,
            Error = error
        });

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
            await PublishTerminalRiskPlansAsync();
            await PublishTerminalActivityAsync();
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

    private void AppendActivity(
        TerminalActivitySeverity severity,
        string operation,
        string summary,
        string detail = "")
    {
        try
        {
            _activityLog.Append(severity, operation, summary, detail);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private Task PublishTerminalActivityAsync() =>
        PublishTerminalCallbackAsync("setTerminalActivity", _activityLog.Load());

    private async Task ClearTerminalActivityAsync()
    {
        _activityLog.Clear();
        await PublishTerminalActivityAsync();
    }

    private async Task ExportTerminalActivityAsync()
    {
        var exportPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            $"cap.com-terminal-activity-{DateTime.Now:yyyyMMdd-HHmmss}.json");
        _activityLog.Export(exportPath);
        AppendActivity(TerminalActivitySeverity.Success, "Activity Log", "Exported", exportPath);
        await PublishTerminalActivityAsync();
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
            var summary = await _marginPreview.BuildAsync(basket, basketNotional, requestToken);
            requestToken.ThrowIfCancellationRequested();
            if (!SyntheticMarginPreviewPublication.IsCurrent(requestToken, request, _marginPreviewRefresh, basket, _basket)) return;
            var json = JsonSerializer.Serialize(summary);
            await InvokeTerminalScriptAsync(
                $"window.setTerminalMarginPreview && window.setTerminalMarginPreview({json});");
            AppendActivity(TerminalActivitySeverity.Success, "Margin Preview", $"Updated for {basketNotional:0} synthetic lot(s)");
            await PublishTerminalActivityAsync();
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
            AppendActivity(TerminalActivitySeverity.Error, "Margin Preview", ex.Message, ex.ToString());
            await PublishTerminalActivityAsync();
        }
        finally
        {
            if (ReferenceEquals(_marginPreviewRefresh, request))
            {
                _marginPreviewRefresh = null;
                request.Dispose();
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
        _terminalDrawingIdentity = "";
        _realtimeBars.Clear();
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
                    token => PreflightSyntheticBasketAsync(preflight.Side, preflight.SyntheticLots, token));
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
            case SyntheticShowExecutionBasketRequest show:
                await RunOperationAsync(
                    "Loading executed synthetic basket",
                    token => LoadExecutionBasketAsync(show.ExecutionId, token));
                break;
            case SyntheticCancelMarginPreviewRequest:
                await ResetMarginPreviewContextAsync(
                    clearBasket: false,
                    reason: SyntheticMarginPreviewInput.InvalidReason,
                    releaseBusy: true);
                break;
            case SyntheticPreviewMarginsRequest previewMargins:
                if (!SyntheticMarginPreviewInput.TryValidate(
                        previewMargins.SyntheticLots,
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
                PreviewSyntheticOrder(previewOrder.Side, previewOrder.SyntheticLots);
                break;
            case SyntheticClearActivityRequest:
                await ClearTerminalActivityAsync();
                break;
            case SyntheticExportActivityRequest:
                await ExportTerminalActivityAsync();
                break;
            case SyntheticSetRiskPlanRequest setRiskPlan:
                await SetSyntheticRiskPlanAsync(setRiskPlan);
                break;
            case SyntheticClearRiskPlanRequest clearRiskPlan:
                await ClearSyntheticRiskPlanAsync(clearRiskPlan);
                break;
        }
    }

    private async Task SetSyntheticRiskPlanAsync(SyntheticSetRiskPlanRequest request)
    {
        var execution = _terminalExecutions.FirstOrDefault(record =>
            string.Equals(record.ExecutionId, request.ExecutionId, StringComparison.Ordinal));
        if (execution is null)
        {
            await PublishTerminalRiskPlanErrorAsync(request.ExecutionId, request.Revision, "Synthetic execution was not found.");
            return;
        }

        var entry = execution.Legs.Sum(leg => leg.Multiplier * (leg.FillLevel ?? leg.ReferencePrice));
        var validation = SyntheticRiskPlanValidation.Validate(
            execution.ExecutionId,
            execution.BasketId,
            execution.Side,
            entry,
            request.StopLoss,
            request.TakeProfit);
        if (!validation.IsValid)
        {
            await PublishTerminalRiskPlanErrorAsync(request.ExecutionId, request.Revision, validation.Error);
            return;
        }

        _riskPlanStore.Upsert(validation.Plan!);
        await PublishTerminalRiskPlansAsync(request.ExecutionId, request.Revision);
    }

    private async Task ClearSyntheticRiskPlanAsync(SyntheticClearRiskPlanRequest request)
    {
        var execution = _terminalExecutions.FirstOrDefault(record =>
            string.Equals(record.ExecutionId, request.ExecutionId, StringComparison.Ordinal));
        if (execution is null)
        {
            await PublishTerminalRiskPlanErrorAsync(request.ExecutionId, request.Revision, "Synthetic execution was not found.");
            return;
        }

        _riskPlanStore.Remove(request.ExecutionId);
        await PublishTerminalRiskPlansAsync(request.ExecutionId, request.Revision);
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

    private void PreviewSyntheticOrder(string side, decimal? requestedSyntheticLots = null)
    {
        if (_basket is null)
        {
            OrderPreviewText.Text = "Build a synthetic symbol first.";
            return;
        }

        decimal? inputNotional = requestedSyntheticLots;
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
            var preview = SyntheticOrderSizing.BuildSyntheticLotOrderPreview(_basket, side, basketNotional);
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
