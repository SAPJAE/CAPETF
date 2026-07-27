using CAPETF.Desktop;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace CAPETF.Desktop.Tests;

public static class SyntheticBasketBuilderTests
{
    public static void RunAll()
    {
        TerminalOperationStateRejectsDuplicatesAndTracksProgress();
        TerminalOperationStageResetsCompletedTotalsForIndeterminateWork();
        TerminalProgressPercentUsesOneWayBinding();
        TerminalProgressPanelAvoidsWebViewAirspace();
        CapComTerminalOperationGuardCompletesFailsAndRestoresControls();
        NewOperationCancelsAndSupersedesEarlierWork();
        IncompleteOhlcRowsAreExcluded();
        CapitalPricePathSupportsDatedHistoryWindows();
        CapitalHistoryPagingWindowsMatchCapitalResolutions();
        CapitalHistoryPagingRetainsSuccessfulRowsAtTerminalBoundary();
        CapitalHistoryPagingDoesNotSwallowAuthFailure();
        SyntheticHistoryServiceMapsTerminalTimeframesToCapitalResolutions();
        SyntheticHistoryServiceAggregatesHourlyCandlesLocally();
        SyntheticHistoryServiceAggregatesTradingSessionsAndRejectsGaps();
        CachedIntradayLoadersAggregateOnlyConsecutiveHourlyRuns();
        LegacyWeeklyCacheNormalizesAllObservationsAndBuildsSelectedBasket();
        BundledPalantirWeeklyCacheHasDistinctMultiYearKeys();
        SelectedHistoryRebuildKeepsTheExactSelectedEpics();
        SelectedHistoryRebuildRejectsMissingSelectedLeg();
        SelectedHistoryValidationUsesDailyAndWeeklyAlignmentKeys();
        SelectedHistoryMergeUsesApiPrecedenceAndCachedWeeklyCoverage();
        SelectedHistoryLoaderMergesTheActiveResolutionCache();
        InverseVolatilityWeightsSumToOneHundred();
        InverseVolatilityWeightsRespectCapsAndMinimums();
        SyntheticCandlesUsePriceStabilizedOhlc();
        SyntheticIndexStartsAtOneHundredOnFirstSharedCandle();
        SyntheticFormulaUsesEqualNotionalWeights();
        SyntheticFormulaUsesPriceStabilizedMultipliers();
        SyntheticCandlesHandleDuplicateCachedDates();
        SyntheticCandlesDoNotCreateArtificialGapsAcrossSparseSharedWeeks();
        SyntheticCandlesUseTimestampIntersectionForIntradayHistory();
        SyntheticCandlesKeepFullSharedTimestampHistory();
        SyntheticBuilderAcceptsConfiguredIntradayMinimum();
        SyntheticBasketsDoNotMixCurrencies();
        SyntheticBasketsKeepBlankCurrenciesTogether();
        SyntheticBasketsAllowBlankCurrenciesInsideSelectedBlock();
        VolatilityAnnualizationUsesRequestedPeriodsPerYear();
        TrailingReturnsUseIntervalAwareHorizonsFromFinalCandle();
        LiveQuoteUpdatesBasketPriceAndTimestamp();
        FirstLiveQuoteUsesLatestHistoricalComponentPrice();
        SyntheticBasketsUseCallerSuppliedCandidates();
        StockTypeMatchingIsCaseInsensitive();
        EtfUniverseRecognitionAndIsolation();
        KnownEtfEpicsOverrideCapitalShareType();
        EtfMetadataMergeUsesCapitalDetailsForGrouping();
        EtfMetadataMergeReappliesCurrentApiEligibility();
        EtfMetadataMergeUsesDeterministicFallbacks();
        EtfMetadataMergeDerivesRegionFromCapitalCountry();
        EncryptedEtfCatalogIncludesLoadedEtfEpics();
        EtfMetadataEnrichmentRequiresConnectedEtfLoad();
        FailedEtfCatalogUsesEtfSpecificApiFallbackOnce();
        EncryptedEtfCacheKeepsOnlyEtfInstruments();
        SimilarityPrefersCorrelatedPricePathsOverVolatilityOnlyNeighbors();
        SyntheticComponentEpicsTakePrecedenceOverVisibleInstruments();
        SyntheticQuoteDistinguishesMatchFromCandleChange();
        SamePriceSyntheticQuoteRefreshesMetadataWithoutChangingCandle();
        SyntheticQuoteUsesComponentPriceWhenDashboardInstrumentIsAbsent();
        SyntheticComponentDisplayPriceFallsBackToBaseline();
        SyntheticTerminalPayloadIncludesCandlesComponentsCurrencyAndMas();
        SyntheticTerminalPayloadIncludesSelectionBasis();
        TerminalPayloadUsesComponentIdentityAndExplicitQuoteFreshness();
        LegacySyntheticDetailsExposeBidAskAndStalenessOnly();
        SyntheticTerminalSelectorChoosesHighestSimilarityBasket();
        SyntheticTerminalSelectorUsesThreeYearComparisonWindow();
        SyntheticTerminalSelectorReturnsFullSelectedHistory();
        SyntheticTerminalSelectorPenalizesVolatilityMismatch();
        SyntheticTerminalHistoryLoadCandidatesScanPastTheFirstSparseRows();
        SeededSyntheticSelectorBuildsNikeBasket();
        SeededSyntheticSelectorPrefersExactNikeTickerOverNameContains();
        SeededSyntheticSelectorMatchesCapitalSuffixSymbol();
        SeededSyntheticSelectorBuildsSapFromEncryptedChunks();
        SeededSyntheticSelectorBuildsSapFromEncryptedChunksForIntradayIntervals();
        SeededSyntheticSelectorDoesNotResolveShortTickersByLooseNameContains();
        SyntheticTerminalLiveUpdateReturnsIncrementalTick();
        StreamingQuoteClearsMissingAndZeroSides();
        StreamingQuoteClearsUnavailableSidesWithoutUsablePrice();
        SyntheticTerminalHtmlExposesRequiredFunctions();
        SyntheticTerminalHtmlShowsBidAndAskWithoutLastPrice();
        SyntheticTerminalHtmlUsesPackagedChartLibrary();
        SyntheticTerminalHtmlRejectsKLineChartRuntime();
        SyntheticDrawingWorkspaceRuntimeCoordinatesManagerStateAndPersistence();
        SyntheticTerminalHtmlUsesV3LightweightChartsTerminal();
        SyntheticTerminalHtmlUsesV5SeriesApiAndChartSideTools();
        SyntheticDrawingsAssetPublishesWithProjectContentEntry();
        SyntheticDrawingsRuntimeValidatesRecordsMeasurementAndHistory();
        SyntheticDrawingsRuntimeExercisesReviewRegressions();
        SyntheticTerminalHtmlExposesResizableRailAndPersistentDrawingTools();
        SyntheticTerminalHtmlDisablesNativeLastCloseDecorations();
        SyntheticTerminalHtmlResetsTransientDrawingStateBeforeRestore();
        SyntheticTerminalHtmlCoalescesIncrementalTicksAndUsesStableDrawingIdentity();
        CapComTerminalUsesTask6PayloadBridge();
        SyntheticTerminalHtmlExposesResizeFunction();
        SyntheticTerminalHtmlExposesDecisionChartControls();
        SyntheticTerminalHtmlExposesV2TerminalControls();
        TerminalOrderPreviewUsesProductionSizingBridge();
        CapComTerminalUsesClearActionLabelsAndSymbolDropdown();
        SeedSearchOptionsIncludePalantirByNameAndSymbolAcrossBlocks();
        SeededSyntheticBuildsDoNotUseGenericHistoryFallback();
        EmptyCachedHistoryLoadsBoundedApiCandidatesAndBuildsBasket();
        CapComTerminalIntradayMinimumsFitCachedHourlyHistory();
        StockRefreshFetchesDeepHourlyHistory();
        WeeklyProducerPreservesFullDates();
        DesktopDefaultSearchDoesNotFilterStocksByEtf();
        MainWindowTerminalFiltersStocksBeforeGenericSelection();
        CapComTerminalUsesEtfCatalogForFilteringAndMetadata();
        DesktopResizesTerminalChartWhenWorkspaceOpens();
        DesktopTerminalWorkspaceExposesChartFirstControls();
        DesktopTerminalWorkspaceExposesV2ProfessionalControls();
        CapComTerminalStartsWithoutDevExpressStockSharpRuntimeCrash();
        CapComTerminalShowsActionableConnectionFailures();
        CapitalApiClientAllowsReconnectAfterRequests();
        CapitalApiClientParsesMarketDetailsSnapshotAndDealingRules();
        CapitalStreamingClientRejectsClosedSocketsAndWindowRecreates();
        CapitalStreamingClientReportsRemoteClose();
        StockChunkLoaderPrefersLegacyWhenChunksAreSmallerThanLegacy();
        CapComTerminalLoadsFullEncryptedStockChunks();
        TerminalWorkspaceModeNameIsAvailable();
        TerminalStreamingEpicsUseOnlySelectedSyntheticComponents();
        SyntheticStrategiesRankExpectedSetups();
        SyntheticStrategiesExposeBuildOptions();
        SyntheticStrategiesReturnClosestFallbackCandidates();
        SyntheticQuoteUsesFormulaMultipliersForBidAsk();
        SyntheticQuoteTreatsMissingOrZeroSidesAsUnavailable();
        SyntheticOrderSizingUsesCapitalDealRules();
        AdaptiveDisplayMultiplierPreservesSmallNonzeroValues();
        ExecutablePreviewUsesCurrentEqualNotionalAndDealRules();
        ExecutablePreviewRoundsUpToCapitalDealMinimumAndIncrement();
        ExecutableOrderPreviewUsesCurrentSideQuotesAndReportsImbalance();
        SavedSyntheticBasketStorePersistsFormulaDetails();
        SavedSyntheticBasketStoreDeletesSelectedBasket();
        SavedBasketDeletionCoordinatorTracksSelectionAndPreservesDisplayedModels();
        SavedBasketDeletionUiContractIsPresent();
        FinalStaticAcceptanceChecks();
    }

    private static void NewOperationCancelsAndSupersedesEarlierWork()
    {
        using var coordinator = new LatestOperationCoordinator();
        var first = coordinator.Begin();
        var second = coordinator.Begin();

        if (!first.Token.IsCancellationRequested) throw new Exception("a newer search or build must cancel the prior operation");
        if (coordinator.IsCurrent(first)) throw new Exception("stale operation results must not remain current");
        if (!coordinator.IsCurrent(second)) throw new Exception("the newest operation must own UI result updates");
    }

    private static void IncompleteOhlcRowsAreExcluded()
    {
        const string json =
            """
            {
              "prices": [
                {
                  "snapshotTimeUTC": "2026-01-01T00:00:00Z",
                  "openPrice": { "bid": 99 },
                  "highPrice": { "bid": 102 },
                  "lowPrice": { "bid": 98 },
                  "closePrice": { "bid": 101 }
                },
                {
                  "snapshotTimeUTC": "2026-01-02T00:00:00Z",
                  "closePrice": { "bid": 103 }
                }
              ]
            }
            """;

        var rows = CapitalApiClient.ParseOhlcPrices(json);

        if (rows.Count != 1) throw new Exception("OHLC rows missing open, high, or low must be excluded");
        AssertNear(99m, rows[0].Open, "complete OHLC open should be retained");
        AssertNear(102m, rows[0].High, "complete OHLC high should be retained");
        AssertNear(98m, rows[0].Low, "complete OHLC low should be retained");
        AssertNear(101m, rows[0].Close, "complete OHLC close should be retained");
    }

    private static void CapitalPricePathSupportsDatedHistoryWindows()
    {
        var path = CapitalApiClient.BuildPricesPath(
            "NKE",
            "DAY",
            1000,
            DateTimeOffset.Parse("2020-01-01T00:00:00Z"),
            DateTimeOffset.Parse("2022-01-01T00:00:00Z"));

        foreach (var required in new[]
        {
            "api/v1/prices/NKE?",
            "resolution=DAY",
            "max=1000",
            "from=2020-01-01T00%3A00%3A00",
            "to=2022-01-01T00%3A00%3A00",
        })
        {
            if (!path.Contains(required, StringComparison.Ordinal))
            {
                throw new Exception($"dated history price path missing {required}: {path}");
            }
        }
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

    private static void SyntheticCandlesUsePriceStabilizedOhlc()
    {
        var a = CreateStock("A", "A");
        var b = CreateStock("B", "B");
        var c = CreateStock("C", "C");
        var day = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var candles = new Dictionary<string, IReadOnlyList<OhlcPoint>>
        {
            ["A"] = CreateCandles(day, 10m, 12m, 9m, 11m),
            ["B"] = CreateCandles(day, 20m, 22m, 19m, 21m),
            ["C"] = CreateCandles(day, 30m, 32m, 29m, 31m)
        };
        var result = SyntheticBasketBuilder.Build("US / USD / Tech", [a, b, c], candles, maxBaskets: 1);
        var first = result.Baskets[0].Candles[0];
        var last = result.Baskets[0].Candles[^1];
        AssertNear(100m, first.Close, "price-stabilized synthetic close should normalize the first shared basket value to 100");
        if (last.Close == 100m) throw new Exception("price-stabilized synthetic close must not normalize the latest basket value to 100");
        if (last.High <= last.Close || last.Low >= last.Close)
        {
            throw new Exception("price-stabilized OHLC should preserve component high and low movement around close");
        }
    }

    private static void TerminalOperationStateRejectsDuplicatesAndTracksProgress()
    {
        var state = new TerminalOperationState();

        AssertTrue(state.TryBegin("Loading history", 3), "the first operation should start");
        AssertFalse(state.TryBegin("Loading history", 3), "a second operation should be rejected while the first is active");
        state.Report(2);
        AssertNear(66.67m, state.Percent, "progress should report the completed proportion", 0.01m);
        state.Report(8);
        AssertNear(100m, state.Percent, "progress should clamp at the known total");
        state.Complete("History loaded");
        AssertFalse(state.IsBusy, "completion should release the operation guard");

        AssertTrue(state.TryBegin("Loading details"), "a completed operation should allow another operation to start");
        state.Fail("Capital.com did not return details");
        AssertFalse(state.IsBusy, "failure should release the operation guard");
        AssertEqual("Capital.com did not return details", state.ErrorMessage, "failure should retain an actionable error message");
    }

    private static void TerminalOperationStageResetsCompletedTotalsForIndeterminateWork()
    {
        var state = new TerminalOperationState();

        AssertTrue(state.TryBegin("Scanning cached history", 500), "a determinate operation should start");
        state.Report(500);
        AssertNear(100m, state.Percent, "the completed determinate stage should reach one hundred percent");

        state.BeginStage("Selecting basket");

        AssertEqual("Selecting basket", state.OperationName, "the new stage should replace the operation label");
        AssertEqual<int?>(null, state.Total, "an indeterminate stage should clear the previous total");
        AssertEqual(0, state.Current, "an indeterminate stage should reset completed work");
        AssertTrue(state.IsIndeterminate, "a stage with no total should use indeterminate progress");
    }

    private static void TerminalProgressPercentUsesOneWayBinding()
    {
        var percent = typeof(TerminalOperationState).GetProperty(nameof(TerminalOperationState.Percent));
        AssertTrue(percent is not null && !percent.CanWrite, "terminal operation percent must remain a calculated read-only value");

        var xaml = File.ReadAllText(SourcePath("desktop", "CAPETF.Desktop", "CapComTerminalWindow.xaml"));
        var progressStart = xaml.IndexOf("<ProgressBar x:Name=\"OperationProgressBar\"", StringComparison.Ordinal);
        var progressEnd = xaml.IndexOf("/>", progressStart, StringComparison.Ordinal);
        if (progressStart < 0 || progressEnd < progressStart) throw new Exception("terminal operation progress bar must exist");

        var progressBar = xaml[progressStart..(progressEnd + 2)];
        AssertTrue(progressBar.Contains("Value=\"{Binding Percent, Mode=OneWay}\"", StringComparison.Ordinal),
            "terminal operation progress must bind its read-only Percent value one-way");
    }

    private static void TerminalProgressPanelAvoidsWebViewAirspace()
    {
        var xaml = File.ReadAllText(SourcePath("desktop", "CAPETF.Desktop", "CapComTerminalWindow.xaml"));
        if (!xaml.Contains("<Border Grid.Row=\"2\" x:Name=\"OperationProgressPanel\"", StringComparison.Ordinal) ||
            !xaml.Contains("<Border Grid.Row=\"3\" Background=\"{StaticResource PanelBrush}\"", StringComparison.Ordinal))
        {
            throw new Exception("terminal progress must occupy a dedicated root row outside the WebView HwndHost region");
        }
    }

    private static void CapComTerminalOperationGuardCompletesFailsAndRestoresControls()
    {
        var source = File.ReadAllText(SourcePath("desktop", "CAPETF.Desktop", "CapComTerminalWindow.xaml.cs"));
        var guardStart = source.IndexOf("private async Task<bool> RunOperationAsync", StringComparison.Ordinal);
        var guardEnd = source.IndexOf("private void SetOperationControlsEnabled", guardStart, StringComparison.Ordinal);
        if (guardStart < 0 || guardEnd < 0) throw new Exception("terminal operation guard must remain a focused helper");
        var guard = source[guardStart..guardEnd];

        AssertTrue(guard.Contains("_operationState.Complete();", StringComparison.Ordinal), "the operation guard should complete successful work");
        AssertTrue(guard.Contains("_operationState.Fail(ex.Message);", StringComparison.Ordinal), "the operation guard should fail unsuccessful work");
        AssertTrue(guard.Contains("finally", StringComparison.Ordinal), "the operation guard should restore controls in finally");
        AssertTrue(guard.Contains("SetOperationControlsEnabled(true);", StringComparison.Ordinal), "the operation guard should re-enable controls after success or failure");

        var selectingStage = source.IndexOf("_operationState.BeginStage(\"Selecting basket\")", StringComparison.Ordinal);
        var yieldBeforeSelection = source.IndexOf("await Task.Yield();", selectingStage, StringComparison.Ordinal);
        var backgroundSelection = source.IndexOf("await Task.Run", yieldBeforeSelection, StringComparison.Ordinal);
        if (selectingStage < 0 || yieldBeforeSelection < selectingStage || backgroundSelection < yieldBeforeSelection)
        {
            throw new Exception("basket selection must yield after starting its indeterminate progress stage");
        }
    }

    private static void SyntheticHistoryServiceMapsTerminalTimeframesToCapitalResolutions()
    {
        AssertEqual("HOUR", SyntheticHistoryService.RequestResolution("2H"), "2H source");
        AssertEqual("HOUR", SyntheticHistoryService.RequestResolution("6H"), "6H source");
        AssertEqual("HOUR", SyntheticHistoryService.RequestResolution("4H"), "4H source");
        AssertEqual("DAY", SyntheticHistoryService.RequestResolution("Daily"), "daily source");
        AssertEqual("WEEK", SyntheticHistoryService.RequestResolution("Weekly"), "weekly source");
    }

    private static void CapitalHistoryPagingWindowsMatchCapitalResolutions()
    {
        var historicalWindow = typeof(CapitalApiClient).GetMethod(
            "HistoricalWindow",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        if (historicalWindow is null) throw new Exception("Capital API client must define historical paging windows");

        AssertEqual(TimeSpan.FromDays(30), (TimeSpan)historicalWindow.Invoke(null, ["HOUR"])!, "hourly history window");
        AssertEqual(TimeSpan.FromDays(120), (TimeSpan)historicalWindow.Invoke(null, ["HOUR_4"])!, "four-hour history window");
        AssertEqual(TimeSpan.FromDays(365), (TimeSpan)historicalWindow.Invoke(null, ["DAY"])!, "daily history window");
        AssertEqual(TimeSpan.FromDays(3650), (TimeSpan)historicalWindow.Invoke(null, ["WEEK"])!, "weekly history window");
    }

    private static void CapitalHistoryPagingRetainsSuccessfulRowsAtTerminalBoundary()
    {
        var handler = new HistoryPagingHandler(HttpStatusCode.BadRequest, "{\"errorCode\":\"error.invalid.from\"}");
        using var client = new CapitalApiClient(handler);
        client.LoginAsync(TestCredentials()).GetAwaiter().GetResult();

        var rows = client.GetAllAvailableOhlcPricesAsync("TEST", "DAY").GetAwaiter().GetResult();

        AssertEqual(2, rows.Count, "terminal history boundary must retain all successful pages");
        AssertEqual(DateTimeOffset.Parse("2026-06-01T00:00:00Z"), rows[0].Time, "older successful history page");
        AssertEqual(DateTimeOffset.Parse("2026-07-01T00:00:00Z"), rows[1].Time, "newer successful history page");
    }

    private static void CapitalHistoryPagingDoesNotSwallowAuthFailure()
    {
        var handler = new HistoryPagingHandler(HttpStatusCode.Unauthorized, "{\"errorCode\":\"error.security.client-token-invalid\"}");
        using var client = new CapitalApiClient(handler);
        client.LoginAsync(TestCredentials()).GetAwaiter().GetResult();

        try
        {
            client.GetAllAvailableOhlcPricesAsync("TEST", "DAY").GetAwaiter().GetResult();
            throw new Exception("history paging must propagate authentication failures");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("401", StringComparison.Ordinal))
        {
        }
    }

    private static ApiCredentials TestCredentials() => new()
    {
        UseDemo = true,
        Identifier = "user@example.com",
        Password = "password",
        ApiKey = "key",
    };

    private static void SyntheticHistoryServiceAggregatesHourlyCandlesLocally()
    {
        var start = DateTimeOffset.Parse("2026-07-20T12:00:00Z");
        var hourly = Enumerable.Range(0, 6)
            .Select(index => new OhlcPoint(
                start.AddHours(index),
                10m + index,
                12m + index,
                9m + index,
                11m + index))
            .ToList();

        var twoHour = SyntheticHistoryService.Transform(hourly, "2H");
        AssertEqual(3, twoHour.Count, "2H local aggregation count");
        AssertEqual(start.AddHours(1), twoHour[0].Time, "2H candle time");
        AssertNear(10m, twoHour[0].Open, "2H open must use first hourly candle");
        AssertNear(13m, twoHour[0].High, "2H high must span both hourly candles");
        AssertNear(9m, twoHour[0].Low, "2H low must span both hourly candles");
        AssertNear(12m, twoHour[0].Close, "2H close must use final hourly candle");

        var sixHour = SyntheticHistoryService.Transform(hourly, "6H");
        AssertEqual(1, sixHour.Count, "6H local aggregation count");
        AssertEqual(start.AddHours(5), sixHour[0].Time, "6H candle time");
        AssertNear(10m, sixHour[0].Open, "6H open must use first hourly candle");
        AssertNear(17m, sixHour[0].High, "6H high must span all hourly candles");
        AssertNear(9m, sixHour[0].Low, "6H low must span all hourly candles");
        AssertNear(16m, sixHour[0].Close, "6H close must use final hourly candle");
    }

    private static void SelectedHistoryRebuildKeepsTheExactSelectedEpics()
    {
        var start = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var selected = new[]
        {
            CreateStock("SELECTED-A", "Selected A"),
            CreateStock("SELECTED-B", "Selected B"),
            CreateStock("SELECTED-C", "Selected C"),
        };
        var candles = selected.ToDictionary(
            instrument => instrument.Epic,
            instrument => CreateVariableCandles(start, instrument.Epic == "SELECTED-A" ? 100m : instrument.Epic == "SELECTED-B" ? 150m : 225m));
        var history = new HistoryLoadResult(candles, start, start.AddDays(119), 120);

        var basket = SyntheticHistoryService.BuildSelected(
            "US / USD / Tech",
            selected,
            history,
            "Daily",
            periodsPerYear: 252,
            minimumCandles: 120);

        if (basket is null) throw new Exception("selected-leg history should rebuild a basket");
        AssertEqual(
            string.Join(",", selected.Select(instrument => instrument.Epic).OrderBy(epic => epic)),
            string.Join(",", basket.Components.Select(component => component.Instrument.Epic).OrderBy(epic => epic)),
            "refreshed history must rebuild from the exact selected epics");
    }

    private static void SelectedHistoryRebuildRejectsMissingSelectedLeg()
    {
        var start = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var selected = new[]
        {
            CreateStock("REQUIRED-A", "Required A"),
            CreateStock("REQUIRED-B", "Required B"),
            CreateStock("REQUIRED-C", "Required C"),
            CreateStock("REQUIRED-D", "Required D"),
        };
        var candles = new Dictionary<string, IReadOnlyList<OhlcPoint>>
        {
            ["REQUIRED-A"] = CreateVariableCandles(start, 100m),
            ["REQUIRED-B"] = CreateVariableCandles(start, 150m),
            ["REQUIRED-C"] = CreateVariableCandles(start, 225m),
            ["REQUIRED-D"] = CreateVariableCandles(start, 300m).Take(10).ToList(),
        };

        var basket = SyntheticHistoryService.BuildSelected(
            "US / USD / Tech",
            selected,
            new HistoryLoadResult(candles, start, start.AddDays(119), 120),
            "Daily",
            periodsPerYear: 252,
            minimumCandles: 120);

        if (basket is not null)
        {
            throw new Exception("a selected-leg rebuild must not silently drop a requested leg with insufficient history");
        }
    }

    private static void SelectedHistoryValidationUsesDailyAndWeeklyAlignmentKeys()
    {
        var selected = new[]
        {
            CreateStock("OFFSET-A", "Offset A"),
            CreateStock("OFFSET-B", "Offset B"),
            CreateStock("OFFSET-C", "Offset C"),
        };
        var starts = new[]
        {
            DateTimeOffset.Parse("2026-01-05T16:00:00-05:00"),
            DateTimeOffset.Parse("2026-01-05T22:00:00+01:00"),
            DateTimeOffset.Parse("2026-01-05T21:00:00Z"),
        };

        IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>> DailyRows() => selected
            .Select((instrument, index) => new
            {
                instrument.Epic,
                Rows = (IReadOnlyList<OhlcPoint>)Enumerable.Range(0, 3)
                    .Select(day => FlatCandle(starts[index].AddDays(day), 100m + day + index))
                    .ToList(),
            })
            .ToDictionary(item => item.Epic, item => item.Rows, StringComparer.OrdinalIgnoreCase);

        var daily = SyntheticHistoryService.BuildSelected(
            "US / USD / Tech", selected, new HistoryLoadResult(DailyRows(), null, null, 3),
            timeframe: "Daily", periodsPerYear: 252, minimumCandles: 3);
        if (daily is null) throw new Exception("daily validation must align matching calendar dates across market timestamp offsets");

        var weeklyRows = DailyRows().ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<OhlcPoint>)pair.Value.Select((row, index) => row with { Time = row.Time.AddDays(index * 6) }).ToList(),
            StringComparer.OrdinalIgnoreCase);
        var weekly = SyntheticHistoryService.BuildSelected(
            "US / USD / Tech", selected, new HistoryLoadResult(weeklyRows, null, null, 3),
            timeframe: "Weekly", periodsPerYear: 52, minimumCandles: 3);
        if (weekly is null) throw new Exception("weekly validation must align matching calendar weeks across market timestamp offsets");
    }

    private static void SelectedHistoryMergeUsesApiPrecedenceAndCachedWeeklyCoverage()
    {
        var start = DateTimeOffset.Parse("2023-01-02T00:00:00Z");
        var selected = new[]
        {
            CreateStock("SELECTED-A", "Selected A"),
            CreateStock("SELECTED-B", "Selected B"),
            CreateStock("SELECTED-C", "Selected C"),
        };
        IReadOnlyList<OhlcPoint> WeeklyRows(decimal initial) => Enumerable.Range(0, 120)
            .Select(index =>
            {
                var close = initial + index + (index % 2 == 0 ? 1m : -1m);
                return FlatCandle(start.AddDays(index * 7), close);
            })
            .ToList();

        var cached = selected.ToDictionary(
            item => item.Epic,
            item => WeeklyRows(item.Epic == "SELECTED-A" ? 100m : item.Epic == "SELECTED-B" ? 200m : 300m),
            StringComparer.OrdinalIgnoreCase);
        cached["UNRELATED"] = WeeklyRows(400m);
        var apiWinningRow = FlatCandle(start.AddDays(119 * 7 + 1), 777m);
        var api = new HistoryLoadResult(
            new Dictionary<string, IReadOnlyList<OhlcPoint>>(StringComparer.OrdinalIgnoreCase)
            {
                ["SELECTED-A"] = [apiWinningRow],
                ["SELECTED-C"] = [FlatCandle(start.AddDays(119 * 7 + 2), 333m)],
                ["UNRELATED"] = [FlatCandle(start, 999m)],
            },
            null,
            null,
            0);

        var merged = SyntheticHistoryService.MergeSelectedHistory(selected, "Weekly", api, cached);

        AssertEqual(3, merged.CandlesByEpic.Count, "selected history merge must not leak unrelated epics");
        AssertEqual(120, merged.CandlesByEpic["SELECTED-A"].Count, "API rows must overlay cached timeframe keys rather than duplicate them");
        AssertNear(777m, merged.CandlesByEpic["SELECTED-A"][^1].Close, "API history must win for duplicate weekly keys");
        AssertEqual(120, merged.CandlesByEpic["SELECTED-B"].Count, "empty API history must retain cached selected-leg coverage");
        AssertEqual(120, merged.SharedCount, "shared metadata must be recalculated from the merged strict intersection");
        AssertEqual(start, merged.SharedStart, "merged shared start");
        AssertEqual(apiWinningRow.Time, merged.SharedEnd, "merged shared end must use the winning API timestamp");

        var basket = SyntheticHistoryService.BuildSelected(
            "US / USD / Tech", selected, merged, "Weekly", periodsPerYear: 52, minimumCandles: 120);
        if (basket is null) throw new Exception("cached selected-leg weekly coverage must keep the basket buildable when API history is partial");
    }

    private static void SelectedHistoryLoaderMergesTheActiveResolutionCache()
    {
        var source = File.ReadAllText(SourcePath("desktop", "CAPETF.Desktop", "CapComTerminalWindow.xaml.cs"));
        var start = source.IndexOf("private async Task<HistoryLoadResult> LoadSelectedHistoryAsync", StringComparison.Ordinal);
        var end = source.IndexOf("private async Task<HistoryLoadResult> LoadCandidateHistoryAsync", start, StringComparison.Ordinal);
        var method = source[start..end];

        AssertTrue(method.Contains("MergeSelectedHistory", StringComparison.Ordinal),
            "the shared selected-history loader must merge API and cached history");
        AssertTrue(method.Contains("CachedCandlesForResolution(resolution)", StringComparison.Ordinal),
            "the selected-history merge must use only the active resolution cache");
    }

    private static void SyntheticHistoryServiceAggregatesTradingSessionsAndRejectsGaps()
    {
        var nonMidnightStart = DateTimeOffset.Parse("2026-07-20T09:30:00+02:00");
        var nonMidnight = SyntheticHistoryService.Transform(HourlyCandles(nonMidnightStart, 6), "6H");
        AssertEqual(1, nonMidnight.Count, "a six-hour trading session must aggregate from its actual first bar");
        AssertEqual(nonMidnightStart.AddHours(5), nonMidnight[0].Time, "session aggregate must retain the final source timestamp");

        var dstRows = new[]
        {
            DateTimeOffset.Parse("2026-03-08T00:00:00-05:00"),
            DateTimeOffset.Parse("2026-03-08T01:00:00-05:00"),
            DateTimeOffset.Parse("2026-03-08T03:00:00-04:00"),
            DateTimeOffset.Parse("2026-03-08T04:00:00-04:00"),
            DateTimeOffset.Parse("2026-03-08T05:00:00-04:00"),
            DateTimeOffset.Parse("2026-03-08T06:00:00-04:00"),
        }.Select((time, index) => FlatCandle(time, 100m + index)).ToList();
        var dst = SyntheticHistoryService.Transform(dstRows, "6H");
        AssertEqual(1, dst.Count, "DST clock changes must not break consecutive hourly trading bars");
        AssertEqual(dstRows[^1].Time, dst[0].Time, "DST aggregate must retain the final source offset and timestamp");

        var gapStart = DateTimeOffset.Parse("2026-07-20T09:00:00Z");
        var gapRows = new[] { 0, 1, 3, 4, 5, 6 }
            .Select(offset => FlatCandle(gapStart.AddHours(offset), 100m + offset))
            .ToList();
        var afterGap = SyntheticHistoryService.Transform(gapRows, "2H");
        AssertEqual(3, afterGap.Count, "complete consecutive groups on either side of a gap must remain available");
        if (afterGap.Any(candle => candle.Time == gapStart.AddHours(3)))
        {
            throw new Exception("a 2H candle must not bridge the missing hourly bar");
        }
    }

    private static void CachedIntradayLoadersAggregateOnlyConsecutiveHourlyRuns()
    {
        var firstSession = new[]
        {
            DateTimeOffset.Parse("2026-03-08T00:00:00-05:00"),
            DateTimeOffset.Parse("2026-03-08T01:00:00-05:00"),
            DateTimeOffset.Parse("2026-03-08T03:00:00-04:00"),
            DateTimeOffset.Parse("2026-03-08T04:00:00-04:00"),
            DateTimeOffset.Parse("2026-03-08T05:00:00-04:00"),
            DateTimeOffset.Parse("2026-03-08T06:00:00-04:00"),
        };
        var secondStart = DateTimeOffset.Parse("2026-03-09T09:30:00-04:00");
        var secondSession = Enumerable.Range(0, 6).Select(offset => secondStart.AddHours(offset)).ToArray();
        var gapStart = DateTimeOffset.Parse("2026-03-10T09:30:00-04:00");
        var gapSession = new[] { 0, 1, 3, 4, 5, 6 }.Select(offset => gapStart.AddHours(offset)).ToArray();
        var times = firstSession.Concat(secondSession).Concat(gapSession).ToList();
        var json = JsonSerializer.Serialize(new
        {
            hourlyPoints = times.Select((time, index) => new { d = time.ToString("O"), p = 100m + index }).ToList(),
        });
        using var document = JsonDocument.Parse(json);

        var stock = DashboardStockChunkLoader.BuildSyntheticCandlesByResolution(document.RootElement);
        var etf = DashboardEtfDataLoader.BuildCandlesByResolution(document.RootElement);
        var serviceRows = times.Select((time, index) => FlatCandle(time, 100m + index)).ToList();
        var expected = new Dictionary<string, IReadOnlyList<DateTimeOffset>>(StringComparer.OrdinalIgnoreCase)
        {
            ["2H"] = [firstSession[1], firstSession[3], firstSession[5], secondSession[1], secondSession[3], secondSession[5], gapSession[1], gapSession[3], gapSession[5]],
            ["4H"] = [firstSession[3], secondSession[3], gapSession[5]],
            ["6H"] = [firstSession[5], secondSession[5]],
        };

        foreach (var interval in new[] { "2H", "4H", "6H" })
        {
            AssertCandleTimes(expected[interval], stock[interval], $"stock cached {interval}");
            AssertCandleTimes(expected[interval], etf[interval], $"ETF cached {interval}");
            AssertCandleTimes(expected[interval], SyntheticHistoryService.Transform(serviceRows, interval), $"service {interval}");
        }

        var selected = new[]
        {
            CreateStock("SELECTED-A", "Selected A"),
            CreateStock("SELECTED-B", "Selected B"),
            CreateStock("SELECTED-C", "Selected C"),
        };
        var cached = new Dictionary<string, IReadOnlyList<OhlcPoint>>(StringComparer.OrdinalIgnoreCase)
        {
            ["SELECTED-A"] = stock["4H"],
            ["SELECTED-B"] = etf["4H"],
            ["SELECTED-C"] = stock["4H"],
        };
        var apiOverride = FlatCandle(expected["4H"][0], 999m);
        var merged = SyntheticHistoryService.MergeSelectedHistory(
            selected,
            "4H",
            new HistoryLoadResult(
                new Dictionary<string, IReadOnlyList<OhlcPoint>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["SELECTED-A"] = [apiOverride],
                },
                null,
                null,
                0),
            cached);

        AssertEqual(3, merged.SharedCount, "partial intraday API history must retain only valid cached shared buckets");
        AssertNear(999m, merged.CandlesByEpic["SELECTED-A"].Single(row => row.Time == apiOverride.Time).Close,
            "partial API history must win without replacing valid cached 4H coverage");
        if (merged.CandlesByEpic.Values.SelectMany(rows => rows).Any(row => row.Time == gapSession[2]))
        {
            throw new Exception("partial API merge must not promote a cached bucket spanning a missing hourly bar");
        }
    }

    private static void AssertCandleTimes(
        IReadOnlyList<DateTimeOffset> expected,
        IReadOnlyList<OhlcPoint> actual,
        string message)
    {
        AssertEqual(
            string.Join("|", expected.Select(time => time.ToUniversalTime().ToString("O"))),
            string.Join("|", actual.Select(row => row.Time.ToUniversalTime().ToString("O"))),
            message);
    }

    private static void LegacyWeeklyCacheNormalizesAllObservationsAndBuildsSelectedBasket()
    {
        var actualWeeks = Enumerable.Range(0, 156)
            .Select(index => DateTimeOffset.Parse("2023-07-03T00:00:00Z").AddDays(index * 7))
            .ToList();
        var legacyLabels = actualWeeks.Select(time => time.ToString("yyyy-MM")).ToList();
        IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>> LoadStock(decimal baseline)
        {
            var json = JsonSerializer.Serialize(new
            {
                weeklyPoints = legacyLabels.Select((label, index) => new
                {
                    d = label,
                    p = baseline + index + (index % 2 == 0 ? 1m : -1m),
                }),
            });
            using var document = JsonDocument.Parse(json);
            return DashboardStockChunkLoader.BuildSyntheticCandlesByResolution(document.RootElement);
        }
        IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>> LoadEtf(decimal baseline)
        {
            var json = JsonSerializer.Serialize(new
            {
                weeklyPoints = legacyLabels.Select((label, index) => new
                {
                    d = label,
                    p = baseline + index + (index % 2 == 0 ? 1m : -1m),
                }),
            });
            using var document = JsonDocument.Parse(json);
            return DashboardEtfDataLoader.BuildCandlesByResolution(document.RootElement);
        }
        DateTime WeeklyKey(DateTimeOffset time)
        {
            var date = time.Date;
            return date.AddDays(-(((int)date.DayOfWeek + 6) % 7));
        }
        void AssertLegacyRows(IReadOnlyList<OhlcPoint> rows, decimal baseline, string source)
        {
            AssertEqual(156, rows.Count, $"{source} legacy weekly row count");
            AssertEqual(156, rows.Select(row => WeeklyKey(row.Time)).Distinct().Count(), $"{source} distinct weekly key count");
            AssertTrue(rows.Zip(rows.Skip(1), (left, right) => left.Time < right.Time).All(value => value),
                $"{source} normalized weekly timestamps must be strictly ordered");
            for (var index = 0; index < rows.Count; index++)
            {
                AssertEqual(legacyLabels[index], rows[index].Time.ToString("yyyy-MM"), $"{source} row must stay in its labeled month");
                AssertNear(baseline + index + (index % 2 == 0 ? 1m : -1m), rows[index].Close,
                    $"{source} normalization must preserve source prices");
            }
        }

        var stockA = LoadStock(100m)["Weekly"];
        var etfB = LoadEtf(200m)["Weekly"];
        var stockC = LoadStock(300m)["Weekly"];
        AssertLegacyRows(stockA, 100m, "stock");
        AssertLegacyRows(etfB, 200m, "ETF");

        var selected = new[]
        {
            CreateStock("LEG-A", "Leg A"),
            CreateStock("LEG-B", "Leg B"),
            CreateStock("LEG-C", "Leg C"),
        };
        var cached = new Dictionary<string, IReadOnlyList<OhlcPoint>>(StringComparer.OrdinalIgnoreCase)
        {
            ["LEG-A"] = stockA,
            ["LEG-B"] = etfB,
            ["LEG-C"] = stockC,
        };
        var apiRow = stockA[100] with { Time = stockA[100].Time.AddDays(1), Open = 999m, High = 999m, Low = 999m, Close = 999m };
        var merged = SyntheticHistoryService.MergeSelectedHistory(
            selected,
            "Weekly",
            new HistoryLoadResult(
                new Dictionary<string, IReadOnlyList<OhlcPoint>>(StringComparer.OrdinalIgnoreCase) { ["LEG-A"] = [apiRow] },
                null,
                null,
                0),
            cached);
        AssertEqual(156, merged.SharedCount, "normalized legacy histories must retain a 156-week strict intersection");
        AssertNear(999m, merged.CandlesByEpic["LEG-A"].Single(row => WeeklyKey(row.Time) == WeeklyKey(apiRow.Time)).Close,
            "API rows must remain authoritative over normalized legacy cache rows");
        var basket = SyntheticHistoryService.BuildSelected("US / USD / Tech", selected, merged, "Weekly", 52, 120);
        if (basket is null || basket.Candles.Count < 120)
        {
            throw new Exception("a normalized three-leg legacy weekly cache must build a multi-year selected basket");
        }

        var malformed = legacyLabels.Select((label, index) => FlatCandle(
            DateTimeOffset.Parse($"{label}-01T00:00:00Z"), 100m + index)).ToList();
        var malformedByEpic = selected.ToDictionary(
            item => item.Epic,
            _ => (IReadOnlyList<OhlcPoint>)malformed,
            StringComparer.OrdinalIgnoreCase);
        AssertEqual(0, SyntheticBasketBuilder.Build("US / USD / Tech", selected, malformedByEpic, 1, 52, 120).Baskets.Count,
            "candidate eligibility must count distinct weekly keys rather than duplicated raw rows");
        if (SyntheticHistoryService.BuildSelected(
                "US / USD / Tech",
                selected,
                new HistoryLoadResult(malformedByEpic, null, null, 36),
                "Weekly",
                52,
                120) is not null)
        {
            throw new Exception("BuildSelected minimums must count distinct weekly keys rather than duplicated raw rows");
        }
    }

    private static void BundledPalantirWeeklyCacheHasDistinctMultiYearKeys()
    {
        DateTime WeeklyKey(DateTimeOffset time)
        {
            var date = time.Date;
            return date.AddDays(-(((int)date.DayOfWeek + 6) % 7));
        }
        var cache = DashboardStockChunkLoader.LoadStocks();
        if (cache.Instruments.Count == 0 || cache.OhlcByEpicAndResolution is null) return;
        var weekly = cache.OhlcByEpicAndResolution
            .Where(pair => pair.Value.TryGetValue("Weekly", out var rows) && rows.Count >= 120)
            .ToDictionary(pair => pair.Key, pair => pair.Value["Weekly"], StringComparer.OrdinalIgnoreCase);
        foreach (var epic in new[] { "PLTR", "CVNA", "HOOD" })
        {
            AssertTrue(weekly.TryGetValue(epic, out var rows), $"bundled cache must include normalized {epic} weekly history");
            AssertEqual(rows!.Count, rows.Select(row => WeeklyKey(row.Time)).Distinct().Count(), $"bundled {epic} weekly keys must be unique");
            AssertTrue(rows.Count >= 120, $"bundled {epic} must retain multi-year weekly history");
        }

        var basket = SeededSyntheticSelector.SelectSeededBasket("Palantir", "", cache.Instruments, weekly, 52, 120);
        if (basket is null || basket.Candles.Count < 120)
        {
            throw new Exception("bundled normalized Palantir weekly history must build a multi-year three-leg basket");
        }
    }

    private static void WeeklyProducerPreservesFullDates()
    {
        var source = File.ReadAllText(SourcePath("scripts", "update_capital_etfs.py"));
        AssertTrue(source.Contains("label_len = 10 if period == \"weekly\" else 7", StringComparison.Ordinal),
            "stock and ETF producer must preserve full dates for weekly points while retaining monthly labels");
    }

    private static void SyntheticIndexStartsAtOneHundredOnFirstSharedCandle()
    {
        var day = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var result = SyntheticBasketBuilder.Build(
            "US / USD / Tech",
            [CreateStock("A", "A"), CreateStock("B", "B"), CreateStock("C", "C")],
            new Dictionary<string, IReadOnlyList<OhlcPoint>>
            {
                ["A"] =
                [
                    FlatCandle(day.AddDays(1), 100m),
                    FlatCandle(day.AddDays(2), 110m),
                    FlatCandle(day.AddDays(3), 120m),
                ],
                ["B"] =
                [
                    FlatCandle(day, 40m),
                    FlatCandle(day.AddDays(2), 50m),
                    FlatCandle(day.AddDays(3), 60m),
                ],
                ["C"] =
                [
                    FlatCandle(day.AddDays(-1), 20m),
                    FlatCandle(day.AddDays(2), 25m),
                    FlatCandle(day.AddDays(3), 20m),
                ],
            },
            maxBaskets: 1,
            periodsPerYear: 252,
            minimumCandles: 2);

        var basket = result.Baskets.Single();
        AssertNear(100m, basket.Candles[0].Close, "first shared candle must be the index base");
        if (basket.Candles[^1].Close == 100m)
        {
            throw new Exception("latest candle must not be rebased to 100");
        }
        if (basket.Candles.Count != 2)
        {
            throw new Exception("only shared timestamps may be rendered");
        }
    }

    private static void SyntheticFormulaUsesEqualNotionalWeights()
    {
        var a = CreateStock("EQ-A", "Equal A");
        var b = CreateStock("EQ-B", "Equal B");
        var c = CreateStock("EQ-C", "Equal C");
        var day = DateTimeOffset.Parse("2024-01-01T00:00:00Z");
        var candles = new Dictionary<string, IReadOnlyList<OhlcPoint>>
        {
            ["EQ-A"] = CreateReturnCandles(day, [0.01m, -0.01m, 0.02m]),
            ["EQ-B"] = CreateReturnCandles(day, [0.04m, -0.04m, 0.02m]),
            ["EQ-C"] = CreateReturnCandles(day, [0.10m, -0.10m, 0.02m]),
        };

        var result = SyntheticBasketBuilder.Build("US / USD / Tech", [a, b, c], candles, maxBaskets: 1);
        var basket = result.Baskets.Single();

        foreach (var component in basket.Components)
        {
            AssertNear(100m / 3m, component.Weight, "synthetic price formula should equal-weight every leg", 0.0001m);
        }
    }

    private static void SyntheticFormulaUsesPriceStabilizedMultipliers()
    {
        var a = CreateStock("PX-A", "Price 56");
        var b = CreateStock("PX-B", "Price 127");
        var c = CreateStock("PX-C", "Price 162");
        var day = DateTimeOffset.Parse("2024-01-01T00:00:00Z");
        var candles = new Dictionary<string, IReadOnlyList<OhlcPoint>>
        {
            ["PX-A"] = CreatePricedTrendCandles(day, 56m),
            ["PX-B"] = CreatePricedTrendCandles(day, 127m),
            ["PX-C"] = CreatePricedTrendCandles(day, 162m),
        };

        var result = SyntheticBasketBuilder.Build("US / USD / Tech", [a, b, c], candles, maxBaskets: 1);
        var basket = result.Baskets.Single();

        AssertNear(100m, basket.Candles[0].Close, "stabilized formula should set the first shared synthetic value to 100");
        foreach (var component in basket.Components)
        {
            var referenceClose = candles[component.Instrument.Epic][0].Close;
            var expectedMultiplier = component.Weight / referenceClose;
            AssertNear(expectedMultiplier, component.FormulaMultiplier, "formula multiplier should equal target notional divided by component reference price", 0.000001m);
            AssertNear(component.Weight, component.FormulaMultiplier * referenceClose, "each leg should contribute its allocation at the reference close", 0.0001m);
            AssertNear(referenceClose, component.FormulaReferencePrice ?? 0m, "formula reference price should use the shared baseline price");
            AssertNear(referenceClose, component.SyntheticBaselinePrice ?? 0m, "synthetic baseline price should use the shared baseline price");
        }
    }

    private static void SyntheticCandlesHandleDuplicateCachedDates()
    {
        var a = CreateStock("DUP-A", "Duplicate A");
        var b = CreateStock("DUP-B", "Duplicate B");
        var c = CreateStock("DUP-C", "Duplicate C");
        var day = DateTimeOffset.Parse("2024-01-01T00:00:00Z");
        var duplicateA = CreateCandles(day, 10m, 11m, 9m, 10.5m).ToList();
        duplicateA.Insert(4, duplicateA[4] with { Close = duplicateA[4].Close + 1m });
        var missingC = CreateCandles(day, 30m, 31m, 29m, 30.5m).Where((_, index) => index != 4).ToList();
        missingC.Add(missingC[^1] with { Time = missingC[^1].Time.AddDays(1) });
        var candles = new Dictionary<string, IReadOnlyList<OhlcPoint>>
        {
            ["DUP-A"] = duplicateA,
            ["DUP-B"] = CreateCandles(day, 20m, 21m, 19m, 20.5m),
            ["DUP-C"] = missingC,
        };

        var result = SyntheticBasketBuilder.Build("US / USD / Tech", [a, b, c], candles, maxBaskets: 1);

        if (result.Baskets.Count != 1) throw new Exception("duplicate cached dates must not prevent a valid basket");
        if (result.Baskets[0].Candles.Count < 100) throw new Exception("duplicate cached dates must still produce aligned synthetic candles");
    }

    private static void SyntheticCandlesDoNotCreateArtificialGapsAcrossSparseSharedWeeks()
    {
        var a = CreateStock("GAP-A", "Gap A");
        var b = CreateStock("GAP-B", "Gap B");
        var c = CreateStock("GAP-C", "Gap C");
        var week = DateTimeOffset.Parse("2026-01-02T00:00:00Z");
        var candles = new Dictionary<string, IReadOnlyList<OhlcPoint>>
        {
            ["GAP-A"] =
            [
                new OhlcPoint(week, 100m, 100m, 100m, 100m),
                new OhlcPoint(week.AddDays(14), 100m, 120m, 100m, 120m),
                new OhlcPoint(week.AddDays(21), 120m, 122m, 120m, 122m),
            ],
            ["GAP-B"] =
            [
                new OhlcPoint(week, 200m, 200m, 200m, 200m),
                new OhlcPoint(week.AddDays(7), 200m, 260m, 200m, 260m),
                new OhlcPoint(week.AddDays(14), 260m, 262m, 260m, 262m),
            ],
            ["GAP-C"] =
            [
                new OhlcPoint(week, 300m, 300m, 300m, 300m),
                new OhlcPoint(week.AddDays(14), 300m, 330m, 300m, 330m),
                new OhlcPoint(week.AddDays(21), 330m, 333m, 330m, 333m),
            ],
        };

        var result = SyntheticBasketBuilder.Build("US / USD / All", [a, b, c], candles, maxBaskets: 1, minimumCandles: 2);
        var basket = result.Baskets.Single();

        if (basket.Candles.Count != 2) throw new Exception("sparse weekly test should produce two shared synthetic candles");
        AssertNear(basket.Candles[0].Close, basket.Candles[1].Open, "synthetic candle open should continue from the prior synthetic close across sparse shared weeks");
        if (basket.Candles[1].High < Math.Max(basket.Candles[1].Open, basket.Candles[1].Close) ||
            basket.Candles[1].Low > Math.Min(basket.Candles[1].Open, basket.Candles[1].Close))
        {
            throw new Exception("gap normalization must keep OHLC high/low enclosing open and close");
        }
    }

    private static void SyntheticCandlesUseTimestampIntersectionForIntradayHistory()
    {
        var a = CreateStock("INTRA-A", "Intraday A");
        var b = CreateStock("INTRA-B", "Intraday B");
        var c = CreateStock("INTRA-C", "Intraday C");
        var start = DateTimeOffset.Parse("2026-01-01T08:00:00Z");
        var candles = new Dictionary<string, IReadOnlyList<OhlcPoint>>
        {
            ["INTRA-A"] = CreateIntradayCandles(start, 56m, 120, TimeSpan.FromHours(1)),
            ["INTRA-B"] = CreateIntradayCandles(start, 127m, 120, TimeSpan.FromHours(1)),
            ["INTRA-C"] = CreateIntradayCandles(start, 162m, 120, TimeSpan.FromHours(1)),
        };

        var result = SyntheticBasketBuilder.Build("US / USD / Tech", [a, b, c], candles, maxBaskets: 1, periodsPerYear: 252 * 4);
        var basket = result.Baskets.Single();

        if (basket.Candles.Count != 120)
        {
            throw new Exception($"intraday synthetic candles must preserve every shared timestamp, got {basket.Candles.Count}");
        }

        if (basket.Candles[0].Time != start || basket.Candles[^1].Time != start.AddHours(119))
        {
            throw new Exception("intraday synthetic candles must keep exact shared candle times");
        }
    }

    private static void SyntheticCandlesKeepFullSharedTimestampHistory()
    {
        var a = CreateStock("FULL-A", "Full A");
        var b = CreateStock("FULL-B", "Full B");
        var c = CreateStock("FULL-C", "Full C");
        var sharedStart = DateTimeOffset.Parse("2023-02-01T00:00:00Z");
        var candles = new Dictionary<string, IReadOnlyList<OhlcPoint>>
        {
            ["FULL-A"] = CreateIntradayCandles(sharedStart.AddDays(-21), 80m, 340, TimeSpan.FromDays(7)),
            ["FULL-B"] = CreateIntradayCandles(sharedStart, 120m, 320, TimeSpan.FromDays(7)),
            ["FULL-C"] = CreateIntradayCandles(sharedStart, 160m, 330, TimeSpan.FromDays(7)),
        };

        var result = SyntheticBasketBuilder.Build("US / USD / Tech", [a, b, c], candles, maxBaskets: 1);
        var basket = result.Baskets.Single();

        if (basket.Candles.Count != 320)
        {
            throw new Exception($"synthetic basket should keep the full intersected history, got {basket.Candles.Count}");
        }

        if (basket.Candles[0].Time != sharedStart || basket.Candles[^1].Time != sharedStart.AddDays(7 * 319))
        {
            throw new Exception("synthetic basket should use the shared timestamp intersection across all legs");
        }
    }

    private static void SyntheticBuilderAcceptsConfiguredIntradayMinimum()
    {
        var a = CreateStock("MIN-A", "Minimum A");
        var b = CreateStock("MIN-B", "Minimum B");
        var c = CreateStock("MIN-C", "Minimum C");
        var start = DateTimeOffset.Parse("2026-01-01T08:00:00Z");
        var candles = new Dictionary<string, IReadOnlyList<OhlcPoint>>
        {
            ["MIN-A"] = CreateIntradayCandles(start, 90m, 60, TimeSpan.FromHours(2)),
            ["MIN-B"] = CreateIntradayCandles(start, 100m, 60, TimeSpan.FromHours(2)),
            ["MIN-C"] = CreateIntradayCandles(start, 110m, 60, TimeSpan.FromHours(2)),
        };

        var result = SyntheticBasketBuilder.Build("US / USD / Tech", [a, b, c], candles, maxBaskets: 1, periodsPerYear: 252 * 4, minimumCandles: 60);

        if (result.Baskets.Count != 1 || result.Baskets[0].Candles.Count != 60)
        {
            throw new Exception("intraday basket builder should honor the configured minimum candle count");
        }
    }

    private static void SyntheticBasketsDoNotMixCurrencies()
    {
        var day = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var currencies = new[] { "USD", "EUR", "GBP" };
        var instruments = Enumerable.Range(0, 9)
            .Select(index => new MarketInstrument
            {
                Epic = $"M{index}",
                Name = $"M{index}",
                Type = "SHARES",
                Currency = currencies[index % currencies.Length],
                Region = "Global",
                Sector = "Tech"
            })
            .ToList();
        var candles = instruments.ToDictionary(
            instrument => instrument.Epic,
            instrument => CreateVariableCandles(day, 100m));

        var result = SyntheticBasketBuilder.Build("Global / Mixed / Tech", instruments, candles, maxBaskets: 3);

        if (result.Baskets.Count != 3) throw new Exception("eligible currencies should form three baskets");
        if (result.Baskets.Any(basket => basket.Components.Select(component => component.Instrument.Currency).Distinct().Count() != 1))
        {
            throw new Exception("synthetic basket components must share one currency");
        }
    }

    private static void VolatilityAnnualizationUsesRequestedPeriodsPerYear()
    {
        var day = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var instruments = Enumerable.Range(0, 3)
            .Select(index => CreateStock($"V{index}", $"V{index}"))
            .ToList();
        var candles = instruments.ToDictionary(
            instrument => instrument.Epic,
            instrument => CreateVariableCandles(day, 100m));

        var weekly = SyntheticBasketBuilder.Build("US / USD / Tech", instruments, candles, maxBaskets: 1);
        var daily = SyntheticBasketBuilder.Build("US / USD / Tech", instruments, candles, maxBaskets: 1, periodsPerYear: 252);
        var expectedRatio = (decimal)Math.Sqrt(252d / 52d);
        var actualRatio = daily.Baskets[0].AverageVolatilityPct / weekly.Baskets[0].AverageVolatilityPct;

        AssertNear(expectedRatio, actualRatio, "annualized volatility should scale with the square root of periods per year", 0.01m);
    }

    private static void SyntheticBasketsKeepBlankCurrenciesTogether()
    {
        var day = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var currencies = new[] { "", "USD" };
        var instruments = currencies.SelectMany((currency, currencyIndex) =>
                Enumerable.Range(0, 3).Select(index => new MarketInstrument
            {
                Epic = $"CURRENCY-{currencyIndex}-{index}",
                Name = $"Currency {currencyIndex} {index}",
                Type = "SHARES",
                Currency = currency,
                Region = "Global",
                Sector = "Tech"
            }))
            .ToList();
        var candles = instruments.ToDictionary(
            instrument => instrument.Epic,
            instrument => CreateVariableCandles(day, 100m));

        var result = SyntheticBasketBuilder.Build("Global / Mixed Currency / Tech", instruments, candles, maxBaskets: 3);

        if (result.Baskets.Count != 2) throw new Exception("blank currency stocks should build only within their own fallback bucket");
        if (result.Baskets.Any(basket =>
                basket.Components.Select(component => string.IsNullOrWhiteSpace(component.Instrument.Currency) ? "BLANK" : component.Instrument.Currency).Distinct().Count() != 1))
        {
            throw new Exception("blank currency stocks must not mix with known-currency stocks");
        }
    }

    private static void SyntheticBasketsAllowBlankCurrenciesInsideSelectedBlock()
    {
        var day = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var instruments = Enumerable.Range(0, 3)
            .Select(index => new MarketInstrument
            {
                Epic = $"BLANK-{index}",
                Name = $"Blank {index}",
                Type = "SHARES",
                Currency = "",
                Region = "US",
                Sector = "Technology"
            })
            .ToList();
        var candles = instruments.ToDictionary(
            instrument => instrument.Epic,
            instrument => CreateVariableCandles(day, 100m));

        var result = SyntheticBasketBuilder.Build("US / Currency / Technology", instruments, candles, maxBaskets: 1);

        if (result.Baskets.Count != 1) throw new Exception("blank Capital.com currency should not block synthetic baskets inside the selected UI block");
    }

    private static void SimilarityPrefersCorrelatedPricePathsOverVolatilityOnlyNeighbors()
    {
        var day = DateTimeOffset.Parse("2022-01-01T00:00:00Z");
        var correlatedReturns = new[] { 0.02m, -0.01m, 0.015m, -0.025m, 0.01m, 0.005m };
        var inverseReturns = correlatedReturns.Select(value => -value).ToArray();
        var instruments = new[]
        {
            CreateStock("UNRELATED", "A unrelated"),
            CreateStock("CORRELATED-1", "B correlated"),
            CreateStock("CORRELATED-2", "C correlated"),
            CreateStock("CORRELATED-3", "D correlated"),
            CreateStock("CORRELATED-4", "E correlated"),
        };
        var candles = instruments.ToDictionary(
            instrument => instrument.Epic,
            instrument => CreateReturnCandles(day, instrument.Epic == "UNRELATED" ? inverseReturns : correlatedReturns));

        var result = SyntheticBasketBuilder.Build("US / USD / Tech", instruments, candles, maxBaskets: 1);
        var componentEpics = result.Baskets.Single().Components.Select(component => component.Instrument.Epic).ToHashSet();

        if (componentEpics.Contains("UNRELATED") || componentEpics.Count != 4)
        {
            throw new Exception("correlated price paths should rank ahead of an unrelated shape with similar volatility");
        }
    }

    private static void SyntheticBasketsUseCallerSuppliedCandidates()
    {
        var day = DateTimeOffset.Parse("2022-01-01T00:00:00Z");
        var instruments = new[]
        {
            new MarketInstrument { Epic = "INDEX", Name = "Index", Type = "INDICES", Currency = "USD" },
            new MarketInstrument { Epic = "CURRENCY", Name = "Currency", Type = "CURRENCIES", Currency = "USD" },
            new MarketInstrument { Epic = "COMMODITY", Name = "Commodity", Type = "COMMODITIES", Currency = "USD" },
        };
        var candles = instruments.ToDictionary(
            instrument => instrument.Epic,
            instrument => CreateVariableCandles(day, 100m));

        var result = SyntheticBasketBuilder.Build("Caller-supplied block", instruments, candles);

        if (result.Baskets.Count != 1 || result.Baskets[0].Components.Count != instruments.Length)
        {
            throw new Exception("generic basket building must use the candidates supplied by its caller");
        }
    }

    private static void TrailingReturnsUseIntervalAwareHorizonsFromFinalCandle()
    {
        var day = DateTimeOffset.Parse("2022-01-01T00:00:00Z");
        var candles = Enumerable.Range(0, 300)
            .Select(index =>
            {
                var close = 100m + index;
                return new OhlcPoint(day.AddDays(index), close, close, close, close);
            })
            .ToList();

        var weekly = SyntheticBasketBuilder.TrailingReturnsPct(candles, 52);
        AssertNear((399m / 347m - 1m) * 100m, weekly[0], "weekly one-year return should use 52 candles before the final candle");
        AssertNear((399m / 373m - 1m) * 100m, weekly[1], "weekly six-month return should use 26 candles before the final candle");
        AssertNear((399m / 386m - 1m) * 100m, weekly[2], "weekly three-month return should use 13 candles before the final candle");

        var daily = SyntheticBasketBuilder.TrailingReturnsPct(candles, 252);
        AssertNear((399m / 147m - 1m) * 100m, daily[0], "daily one-year return should use 252 candles before the final candle");
        AssertNear((399m / 273m - 1m) * 100m, daily[1], "daily six-month return should use 126 candles before the final candle");
        AssertNear((399m / 336m - 1m) * 100m, daily[2], "daily three-month return should use 63 candles before the final candle");
    }

    private static void FirstLiveQuoteUsesLatestHistoricalComponentPrice()
    {
        var day = DateTimeOffset.Parse("2022-01-01T00:00:00Z");
        var instruments = Enumerable.Range(0, 3)
            .Select(index => CreateStock($"BASELINE-{index}", $"Baseline {index}"))
            .ToList();
        foreach (var instrument in instruments) instrument.Price = 500m + instruments.IndexOf(instrument);
        var candles = instruments.ToDictionary(
            instrument => instrument.Epic,
            instrument => CreateVariableCandles(day, 100m + instruments.IndexOf(instrument) * 10m));
        var result = SyntheticBasketBuilder.Build("US / USD / Tech", instruments, candles, maxBaskets: 1);
        var basket = result.Baskets.Single();
        var component = basket.Components[0];
        var historicalClose = candles[component.Instrument.Epic][^1].Close;
        var sharedBaselineClose = candles[component.Instrument.Epic][0].Close;
        var priorBasketClose = basket.Candles[^1].Close;
        var quote = new QuoteUpdate(component.Instrument.Epic, null, null, historicalClose + 5m, day.AddDays(121));

        AssertNear(
            500m + instruments.IndexOf(component.Instrument),
            component.Instrument.Price ?? 0m,
            "basket build must preserve an existing live/display component price");
        AssertNear(
            sharedBaselineClose,
            component.SyntheticBaselinePrice ?? 0m,
            "basket build should keep the shared historical baseline in a separate synthetic baseline");

        if (!SyntheticLiveUpdate.ApplyQuote(basket, quote).CandleChanged)
        {
            throw new Exception("the first live quote must use the fetched historical close without rewinding the display price");
        }

        AssertNear(
            priorBasketClose + 5m * component.FormulaMultiplier,
            basket.Candles[^1].Close,
            "first live quote should advance from the latest historical component price");
    }

    private static void StockTypeMatchingIsCaseInsensitive()
    {
        var day = DateTimeOffset.Parse("2022-01-01T00:00:00Z");
        var types = new[] { "shares", "Shares", " SHARES " };
        var instruments = types.Select((type, index) => new MarketInstrument
        {
            Epic = $"CASE-{index}",
            Name = $"Case {index}",
            Type = type,
            Currency = "USD",
        }).ToList();
        var candles = instruments.ToDictionary(
            instrument => instrument.Epic,
            instrument => CreateVariableCandles(day, 100m));

        var result = SyntheticBasketBuilder.Build("Case-insensitive stocks", instruments, candles);

        if (result.Baskets.Count != 1) throw new Exception("SHARES instrument matching must be case-insensitive");
    }

    private static void EtfUniverseRecognitionAndIsolation()
    {
        var etf = new MarketInstrument { Epic = "ETF", Type = "ETF" };
        var etfs = new MarketInstrument { Epic = "ETFS", Type = "ETFS" };
        var etfThree = new MarketInstrument { Epic = "ETF-THREE", Type = "ETF" };
        var stock = new MarketInstrument { Epic = "STOCK", Type = "SHARES" };
        var closedEtf = new MarketInstrument { Epic = "CLOSED-ETF", Type = "ETF", Status = "CLOSED" };
        var closeOnlyEtf = new MarketInstrument { Epic = "CLOSE-ONLY-ETF", Type = "ETF", Status = "CLOSE_ONLY" };
        var obsoleteEtf = new MarketInstrument { Epic = "OBSOLETE-ETF", Type = "ETF", Status = "OBSOLETE" };

        if (!CapitalInstrumentTypes.IsEtf(etf)) throw new Exception("ETF type must be recognized as an ETF");
        if (!CapitalInstrumentTypes.IsEtf(etfs)) throw new Exception("ETFS type must be recognized as an ETF");
        if (CapitalInstrumentTypes.IsEtf(stock)) throw new Exception("SHARES must remain stock-only");
        if (!TerminalUniverse.Accepts(TerminalUniverseKind.ETFs, etf)) throw new Exception("ETF universe must accept ETFs");
        if (TerminalUniverse.Accepts(TerminalUniverseKind.ETFs, stock)) throw new Exception("ETF universe must exclude stocks");
        if (!TerminalUniverse.Accepts(TerminalUniverseKind.Stocks, stock)) throw new Exception("stock universe must accept stocks");
        if (TerminalUniverse.Accepts(TerminalUniverseKind.Stocks, etf)) throw new Exception("stock universe must exclude ETFs");
        if (!TerminalUniverse.Accepts(TerminalUniverseKind.ETFs, closedEtf)) throw new Exception("closed ETFs must remain eligible");
        if (TerminalUniverse.Accepts(TerminalUniverseKind.ETFs, closeOnlyEtf)) throw new Exception("close-only ETFs must be excluded");
        if (TerminalUniverse.Accepts(TerminalUniverseKind.ETFs, obsoleteEtf)) throw new Exception("obsolete ETFs must be excluded");

        var etfCandidates = new[] { etf, etfs, etfThree, stock }
            .Where(item => TerminalUniverse.Accepts(TerminalUniverseKind.ETFs, item))
            .ToList();
        var candles = etfCandidates.ToDictionary(
            item => item.Epic,
            item => CreateVariableCandles(DateTimeOffset.Parse("2024-01-01T00:00:00Z"), 100m));
        var basket = SyntheticBasketBuilder.Build("ETF / USD / All", etfCandidates, candles, maxBaskets: 1).Baskets.Single();
        if (basket.Components.Any(component => !CapitalInstrumentTypes.IsEtf(component.Instrument)))
        {
            throw new Exception("ETF basket requests must exclude stock components");
        }
    }

    private static void EncryptedEtfCacheKeepsOnlyEtfInstruments()
    {
        var cached = DashboardEtfDataLoader.LoadEtfs();
        if (cached.Instruments.Count == 0)
        {
            throw new Exception("encrypted ETF cache must load ETF instruments");
        }

        if (cached.Instruments.Any(item => !CapitalInstrumentTypes.IsEtf(item)))
        {
            throw new Exception("encrypted ETF cache must not include stock instruments");
        }
        if (cached.Instruments.Any(item => !TerminalUniverse.Accepts(TerminalUniverseKind.ETFs, item)))
        {
            throw new Exception("encrypted ETF cache must keep only ETF-universe candidates");
        }
        if (cached.OhlcByEpic.Count == 0)
        {
            throw new Exception("encrypted ETF cache must retain chart history");
        }
    }

    private static void KnownEtfEpicsOverrideCapitalShareType()
    {
        var knownEtfEpics = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ETF-CFD" };
        var knownEtf = new MarketInstrument { Epic = "ETF-CFD", Type = "SHARES", Status = "CLOSED" };
        var ordinaryShare = new MarketInstrument { Epic = "ORDINARY-SHARE", Type = "SHARES", Status = "CLOSED" };

        if (!TerminalUniverse.Accepts(TerminalUniverseKind.ETFs, knownEtf, knownEtfEpics))
        {
            throw new Exception("known ETF epics must be accepted as ETFs even when Capital.com reports SHARES");
        }
        if (TerminalUniverse.Accepts(TerminalUniverseKind.Stocks, knownEtf, knownEtfEpics))
        {
            throw new Exception("known ETF epics must be excluded from the stock universe");
        }
        if (!TerminalUniverse.Accepts(TerminalUniverseKind.Stocks, ordinaryShare, knownEtfEpics))
        {
            throw new Exception("ordinary SHARES epics must remain stock-only");
        }
        if (TerminalUniverse.Accepts(TerminalUniverseKind.ETFs, ordinaryShare, knownEtfEpics))
        {
            throw new Exception("ordinary SHARES epics must be excluded from the ETF universe");
        }
    }

    private static void EtfMetadataMergeUsesCapitalDetailsForGrouping()
    {
        var cached = new MarketInstrument
        {
            Epic = "ETF-CFD",
            Name = "Cached ETF",
            Type = "ETF",
            Status = "CLOSED",
            Price = 100m,
        };
        var details = new MarketInstrument
        {
            Epic = "ETF-CFD",
            Type = "SHARES",
            Country = "Ireland",
            Region = "Europe",
            Currency = "EUR",
            Sector = "Equity ETFs",
            Status = "TRADEABLE",
            Price = 101m,
        };

        var merged = EtfMetadataMerger.Merge(cached, details);

        AssertEqual("Ireland", merged.Country, "ETF metadata merge should use Capital.com country");
        AssertEqual("Europe", merged.Region, "ETF metadata merge should use Capital.com region");
        AssertEqual("EUR", merged.Currency, "ETF metadata merge should use Capital.com currency");
        AssertEqual("Equity ETFs", merged.Sector, "ETF metadata merge should use Capital.com sector");
        AssertEqual("Europe / EUR / Equity ETFs", merged.Group, "ETF metadata merge should build a meaningful group");
        AssertEqual("TRADEABLE", merged.Status, "ETF metadata merge should prefer current Capital.com status");
        AssertNear(100m, merged.Price ?? 0m, "ETF metadata merge should retain the cached price");
    }

    private static void EtfMetadataMergeReappliesCurrentApiEligibility()
    {
        var merged = EtfMetadataMerger.Merge(
            new MarketInstrument { Epic = "ETF-CFD", Type = "ETF", Status = "CLOSED" },
            new MarketInstrument { Epic = "ETF-CFD", Type = "SHARES", Status = "CLOSE_ONLY" });

        AssertEqual("CLOSE_ONLY", merged.Status, "current API close-only status must replace cached status");
        if (TerminalUniverse.Accepts(TerminalUniverseKind.ETFs, merged))
        {
            throw new Exception("ETF metadata enrichment must reapply eligibility after current status merge");
        }
    }

    private static void EtfMetadataMergeUsesDeterministicFallbacks()
    {
        var merged = EtfMetadataMerger.Merge(
            new MarketInstrument { Epic = "ETF-CFD", Type = "ETF", Sector = "All" },
            new MarketInstrument { Epic = "ETF-CFD", Type = "SHARES" });

        AssertEqual("Other / Currency / All", merged.Group, "ETF metadata fallback group should remain deterministic without Capital metadata");
    }

    private static void EncryptedEtfCatalogIncludesLoadedEtfEpics()
    {
        var cached = DashboardEtfDataLoader.LoadEtfs();
        if (cached.Instruments.Any(item => !cached.KnownEtfEpics.Contains(item.Epic)))
        {
            throw new Exception("the ETF cache catalog must retain every loaded ETF epic as authoritative identity");
        }
    }

    private static void EtfMetadataEnrichmentRequiresConnectedEtfLoad()
    {
        var missingMetadata = new MarketInstrument { Epic = "ETF-CFD", Type = "ETF", Sector = "All" };

        if (!TerminalUniverseLoadPolicy.RequiresEtfMetadataEnrichment(TerminalUniverseKind.ETFs, [missingMetadata]))
        {
            throw new Exception("ETF universe loads with missing metadata must require connected enrichment");
        }
        if (TerminalUniverseLoadPolicy.RequiresEtfMetadataEnrichment(TerminalUniverseKind.Stocks, [missingMetadata]))
        {
            throw new Exception("stock universe loads must not trigger ETF metadata enrichment");
        }
    }

    private static void FailedEtfCatalogUsesEtfSpecificApiFallbackOnce()
    {
        var catalog = new EtfCatalogCache();
        var attempts = 0;
        var first = catalog.LoadOnce(() =>
        {
            attempts++;
            throw new InvalidOperationException("cache unavailable");
        });
        var second = catalog.LoadOnce(() =>
        {
            attempts++;
            throw new InvalidOperationException("must not retry during fallback");
        });

        if (first is not null || second is not null || attempts != 1 || catalog.KnownEtfEpics.Count != 0)
        {
            throw new Exception("a failed ETF catalog must become an empty one-shot fallback catalog");
        }
        AssertEqual("ETF", TerminalUniverseLoadPolicy.ApiSearchTerm(TerminalUniverseKind.ETFs, "AAPL"), "ETF fallback search term");

        var fallbackMarkets = new[]
        {
            new MarketInstrument { Epic = "ETF-CFD", Name = "Global Equity ETF", Type = "SHARES", Status = "CLOSED" },
            new MarketInstrument { Epic = "STOCK", Name = "Ordinary Company", Type = "SHARES", Status = "CLOSED" },
        };
        var etfs = TerminalUniverseLoadPolicy.NormalizeApiFallback(TerminalUniverseKind.ETFs, fallbackMarkets, catalog.KnownEtfEpics);
        if (etfs.Count != 1 || etfs[0].Epic != "ETF-CFD" || !CapitalInstrumentTypes.IsEtf(etfs[0]))
        {
            throw new Exception("ETF fallback must normalize only ETF-specific search results into the ETF universe");
        }
        var stocks = TerminalUniverseLoadPolicy.NormalizeApiFallback(
            TerminalUniverseKind.Stocks,
            fallbackMarkets,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ETF-CFD" });
        if (stocks.Count != 1 || stocks[0].Epic != "STOCK")
        {
            throw new Exception("stock fallback must exclude ETF catalog epics when the catalog is available");
        }
    }

    private static void EtfMetadataMergeDerivesRegionFromCapitalCountry()
    {
        var merged = EtfMetadataMerger.Merge(
            new MarketInstrument { Epic = "ETF-CFD", Type = "ETF", Sector = "All" },
            new MarketInstrument { Epic = "ETF-CFD", Type = "SHARES", Country = "United States", Currency = "USD" });

        AssertEqual("US / USD / All", merged.Group, "ETF metadata merge should derive a deterministic region from Capital country when region is absent");
    }

    private static void MainWindowTerminalFiltersStocksBeforeGenericSelection()
    {
        var source = File.ReadAllText(SourcePath("desktop", "CAPETF.Desktop", "MainWindow.xaml.cs"));
        var stockFilter = source.IndexOf("var stockInstruments = _instruments.Where(CapitalInstrumentTypes.IsStock).ToList();", StringComparison.Ordinal);
        var genericSelection = source.IndexOf("SyntheticTerminalSelector.HistoryLoadCandidates(block, stockInstruments)", StringComparison.Ordinal);
        if (stockFilter < 0 || genericSelection < stockFilter)
        {
            throw new Exception("MainWindow terminal must filter stocks before calling the generic history candidate selector");
        }
    }

    private static void CapComTerminalUsesEtfCatalogForFilteringAndMetadata()
    {
        var source = File.ReadAllText(SourcePath("desktop", "CAPETF.Desktop", "CapComTerminalWindow.xaml.cs"));
        foreach (var required in new[]
        {
            "EnsureEtfCatalogLoaded",
            "EtfCatalogCache",
            "TerminalUniverse.Accepts(universe, item, _knownEtfEpics)",
            "EnrichEtfMetadataAsync",
            "EtfMetadataMerger.Merge",
            "TerminalUniverseLoadPolicy.RequiresEtfMetadataEnrichment",
            "TerminalUniverseLoadPolicy.ApiSearchTerm",
            "TerminalUniverseLoadPolicy.NormalizeApiFallback",
        })
        {
            if (!source.Contains(required, StringComparison.Ordinal))
            {
                throw new Exception($"cap.com Terminal must use the ETF catalog for filtering and metadata enrichment: {required}");
            }
        }

        var enrichStart = source.IndexOf("private async Task<IReadOnlyList<MarketInstrument>> EnrichEtfMetadataAsync", StringComparison.Ordinal);
        var ensureConnection = source.IndexOf("await EnsureConnectedAsync();", enrichStart, StringComparison.Ordinal);
        var enrichmentLoop = source.IndexOf("var enriched = new List<MarketInstrument>", enrichStart, StringComparison.Ordinal);
        if (enrichStart < 0 || ensureConnection < enrichStart || enrichmentLoop < ensureConnection)
        {
            throw new Exception("connected ETF universe loading must ensure a session before metadata enrichment");
        }
    }

    private static void LiveQuoteUpdatesBasketPriceAndTimestamp()
    {
        var historicalTime = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var quoteTime = DateTimeOffset.Parse("2026-01-02T12:34:56Z");
        var basket = CreateLiveBasket("LIVE-METADATA", "LIVE-COMPONENT", 10m, historicalTime);
        var changedProperties = new HashSet<string>();
        if (basket is not System.ComponentModel.INotifyPropertyChanged notifications)
        {
            throw new Exception("synthetic basket price and update time must support UI change notification");
        }
        notifications.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName ?? "");

        SyntheticLiveUpdate.ApplyQuote(
            basket,
            new QuoteUpdate("LIVE-COMPONENT", null, null, 12m, quoteTime));

        AssertNear(12m, basket.BasketPrice, "basket price should reflect the live candle close");
        if (basket.LastUpdated != quoteTime) throw new Exception("basket update time should reflect the live quote timestamp");
        if (!changedProperties.Contains(nameof(SyntheticBasket.BasketPrice)) ||
            !changedProperties.Contains(nameof(SyntheticBasket.LastUpdated)))
        {
            throw new Exception("live basket metadata changes must notify the UI");
        }
    }

    private static void SyntheticComponentEpicsTakePrecedenceOverVisibleInstruments()
    {
        var visible = Enumerable.Range(0, 40)
            .Select(index => new MarketInstrument { Epic = $"VISIBLE-{index}" })
            .ToList();
        var basket = new SyntheticBasket();
        basket.Components.Add(new SyntheticComponent(new MarketInstrument { Epic = "SYNTHETIC-A" }, 50m, 0m, 0m));
        basket.Components.Add(new SyntheticComponent(new MarketInstrument { Epic = "VISIBLE-0" }, 50m, 0m, 0m));

        var epics = SyntheticLiveUpdate.PrioritizedEpics(visible, [basket]);

        if (epics.Count != 40) throw new Exception("streaming epics should remain capped at 40");
        if (epics[0] != "SYNTHETIC-A") throw new Exception("synthetic component epics should be subscribed before visible instruments");
        if (epics.Count(epic => epic == "VISIBLE-0") != 1) throw new Exception("streaming epics should be deduplicated");
    }

    private static void SyntheticQuoteDistinguishesMatchFromCandleChange()
    {
        var selected = CreateLiveBasket("SELECTED", "COMPONENT-A", 10m);
        var unrelated = CreateLiveBasket("UNRELATED", "COMPONENT-B", 20m);
        var quote = new QuoteUpdate("COMPONENT-B", null, null, 22m, DateTimeOffset.UtcNow);

        var selectedResult = SyntheticLiveUpdate.ApplyQuote(selected, quote);
        var unrelatedResult = SyntheticLiveUpdate.ApplyQuote(unrelated, quote);

        if (selectedResult.Matched || selectedResult.CandleChanged)
        {
            throw new Exception("unrelated quotes must not report a selected synthetic basket match");
        }
        if (!unrelatedResult.Matched || !unrelatedResult.CandleChanged)
        {
            throw new Exception("price-changing matching quotes should report both a match and a changed candle");
        }
    }

    private static void SamePriceSyntheticQuoteRefreshesMetadataWithoutChangingCandle()
    {
        var historicalTime = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var quoteTime = historicalTime.AddMinutes(5);
        var basket = CreateLiveBasket("SELECTED", "COMPONENT-A", 10m, historicalTime);
        var originalCandle = basket.Candles[^1];
        var quote = new QuoteUpdate("COMPONENT-A", null, null, 10m, quoteTime);
        var result = SyntheticLiveUpdate.ApplyQuote(basket, quote);

        if (!result.Matched)
        {
            throw new Exception("same-price quotes must still report a matching synthetic component");
        }
        if (result.CandleChanged)
        {
            throw new Exception("unchanged quotes must not report a changed synthetic candle");
        }
        if (!ReferenceEquals(basket.Candles[^1], originalCandle))
        {
            throw new Exception("same-price quotes must leave the candle collection untouched");
        }
        if (basket.LastUpdated != quoteTime) throw new Exception("same-price quotes must refresh selected detail metadata");
    }

    private static void SyntheticQuoteUsesComponentPriceWhenDashboardInstrumentIsAbsent()
    {
        var basket = CreateLiveBasket("RETIRED-MARKET", "RETIRED-COMPONENT", 10m);
        var quote = new QuoteUpdate("RETIRED-COMPONENT", null, null, 12m, DateTimeOffset.UtcNow);

        if (!SyntheticLiveUpdate.ApplyQuote(basket, quote).CandleChanged)
        {
            throw new Exception("synthetic components must update even when the dashboard instrument is absent");
        }

        AssertNear(12m, basket.Candles[^1].Close, "synthetic candle should use the component's prior price");
    }

    private static void SyntheticComponentDisplayPriceFallsBackToBaseline()
    {
        var instrument = new MarketInstrument { Epic = "BASELINE-ONLY", Name = "Baseline Only" };
        var component = new SyntheticComponent(instrument, 100m, 0m, 0m)
        {
            SyntheticBaselinePrice = 42.25m,
        };
        var changed = new HashSet<string>();
        component.PropertyChanged += (_, args) => changed.Add(args.PropertyName ?? "");

        AssertNear(42.25m, component.DisplayPrice ?? 0m, "component display price should fall back to synthetic baseline");

        instrument.Price = 43.5m;
        component.NotifyInstrumentPriceChanged();

        AssertNear(43.5m, component.DisplayPrice ?? 0m, "component display price should prefer live instrument price");
        if (!changed.Contains(nameof(SyntheticComponent.DisplayPrice)))
        {
            throw new Exception("component display price changes must notify the UI");
        }
    }

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
        using var payloadJson = JsonDocument.Parse(JsonSerializer.Serialize(payload));
        if (payloadJson.RootElement.TryGetProperty("LastPrice", out _)
            || payloadJson.RootElement.GetProperty("Components")[0].TryGetProperty("Last", out _))
        {
            throw new Exception("terminal payload must expose bid/ask without synthetic last-price display fields");
        }
        AssertNear(0.6m, payload.Components[0].FormulaMultiplier, "component row must include executable formula multiplier");
        AssertNear(0.6m, payload.Components[0].DisplayMultiplier, "component row should expose rounded formula display multiplier");
        if (payload.Ma20.Count == 0 || payload.Ma50.Count == 0 || payload.Ma200.Count == 0)
        {
            throw new Exception("terminal payload must include MA 20, MA 50, and MA 200 when enough candles exist");
        }
        AssertNear(310.5m, payload.Ma20[^1].Value, "MA20 must average the last 20 closes");
    }

    private static void SyntheticTerminalPayloadIncludesSelectionBasis()
    {
        var basket = new SyntheticBasket
        {
            Symbol = "SYN-BASIS",
            Block = "US / USD / Tech",
            AverageVolatilityPct = 22.5m,
            SimilarityScore = 88.1m,
            BasketPrice = 100m,
            LastUpdated = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
        };
        basket.Candles.Add(new OhlcPoint(DateTimeOffset.Parse("2026-01-01T00:00:00Z"), 100m, 101m, 99m, 100m));
        basket.Components.Add(new SyntheticComponent(
            new MarketInstrument { Epic = "AAA", Name = "Anchor Inc", Symbol = "AAA", Currency = "USD", Price = 100m },
            33.3333m,
            21.4m,
            54.2m));
        basket.Components.Add(new SyntheticComponent(
            new MarketInstrument { Epic = "BBB", Name = "Peer Inc", Symbol = "BBB", Currency = "USD", Price = 50m },
            33.3333m,
            22.1m,
            49.3m));

        var payload = SyntheticTerminalChartPayload.Build(basket);

        if (!payload.SelectionBasis.Contains("similar price path", StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception("terminal payload must explain the selection basis");
        }
        if (payload.Components[0].Role != "Anchor") throw new Exception("first component should be labelled as the anchor leg");
        if (payload.Components[1].Role != "Peer") throw new Exception("later components should be labelled as peer legs");
        AssertNear(21.4m, payload.Components[0].AnnualizedVolatilityPct, "component row should expose annualized volatility");
        AssertNear(54.2m, payload.Components[0].FourYearReturnPct, "component row should expose four-year return");
    }

    private static void TerminalPayloadUsesComponentIdentityAndExplicitQuoteFreshness()
    {
        var now = DateTimeOffset.Parse("2026-07-26T12:00:00Z");
        var basket = new SyntheticBasket { Symbol = "SYN-IDENTITY", Block = "US / USD / Tech" };
        basket.Components.Add(new SyntheticComponent(
            new MarketInstrument { Epic = "B", Currency = "USD", LastTickAt = now.AddMinutes(-10) }, 50m, 0m, 0m));
        basket.Components.Add(new SyntheticComponent(
            new MarketInstrument { Epic = "A", Currency = "USD", LastTickAt = now.AddMinutes(-1) }, 50m, 0m, 0m));

        var payload = SyntheticTerminalChartPayload.Build(basket, now);

        AssertEqual("SYN-IDENTITY|A|B", payload.DrawingIdentity, "drawing identity must include the stable sorted component set");
        AssertEqual("stale", payload.Components[0].QuoteStatus, "old component quote must be explicitly stale");
        AssertEqual("fresh", payload.Components[1].QuoteStatus, "recent component quote must be explicitly fresh");
        AssertEqual(now.AddMinutes(-1), payload.Components[1].QuoteTimestamp, "component quote timestamp must remain machine-readable");
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(payload));
        if (json.RootElement.GetProperty("Components")[0].TryGetProperty("LastTickText", out _))
        {
            throw new Exception("terminal component payload must expose quote status instead of a synthetic last/tick display field");
        }
    }

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

    private static void SyntheticTerminalSelectorReturnsFullSelectedHistory()
    {
        var day = DateTimeOffset.Parse("2018-01-01T00:00:00Z");
        var instruments = Enumerable.Range(0, 3)
            .Select(index => CreateStock($"FULL-{index}", $"Full {index}"))
            .ToList();
        var candles = instruments.ToDictionary(
            instrument => instrument.Epic,
            instrument => CreateLongReturnCandles(day, [0.01m, -0.004m, 0.006m], 320));

        var selected = SyntheticTerminalSelector.SelectBest("US / USD / Tech", instruments, candles, 52);

        if (selected is null) throw new Exception("terminal selector must return a full-history basket");
        if (selected.Candles.Count != 320)
        {
            throw new Exception($"terminal chart should keep full selected history, got {selected.Candles.Count} candles");
        }
    }

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

    private static void SyntheticTerminalHistoryLoadCandidatesScanPastTheFirstSparseRows()
    {
        var instruments = Enumerable.Range(0, 80)
            .Select(index => new MarketInstrument
            {
                Epic = $"SCAN-{index:000}",
                Name = $"Scan {index:000}",
                Type = "SHARES",
            })
            .ToList();

        var selected = SyntheticTerminalSelector.HistoryLoadCandidates("Other / Currency / Sector", instruments, limit: 64);

        if (selected.Count != 64) throw new Exception("terminal history loading should scan past the first 36 broad search results");
        if (!selected.Any(item => item.Epic == "SCAN-063")) throw new Exception("terminal history loading should include deeper candidates in broad groups");
        if (selected.Any(item => item.Epic == "SCAN-064")) throw new Exception("terminal history loading should respect the requested scan limit");
    }

    private static void LegacySyntheticDetailsExposeBidAskAndStalenessOnly()
    {
        var source = File.ReadAllText(SourcePath("desktop", "CAPETF.Desktop", "MainWindow.xaml.cs"));
        var start = source.IndexOf("private void ShowSyntheticDetails", StringComparison.Ordinal);
        var end = source.IndexOf("private async void RenderSyntheticCandlesAsync", start, StringComparison.Ordinal);
        var method = source[start..end];

        AssertFalse(method.Contains("BasketPrice", StringComparison.Ordinal), "legacy synthetic details must not expose synthetic last price");
        AssertTrue(method.Contains("BidPrice", StringComparison.Ordinal) && method.Contains("AskPrice", StringComparison.Ordinal),
            "legacy synthetic details must display bid and ask");
        AssertTrue(method.Contains("stale components", StringComparison.Ordinal), "legacy synthetic details must expose quote staleness");
    }

    private static void SyntheticTerminalLiveUpdateReturnsIncrementalTick()
    {
        var candleTime = DateTimeOffset.Parse("2026-07-25T00:00:00Z");
        var basket = new SyntheticBasket { Symbol = "SYN-LIVE", Block = "US / USD / Technology", BasketPrice = 10m, LastUpdated = candleTime };
        basket.Components.Add(new SyntheticComponent(new MarketInstrument { Epic = "LIVE-A", Price = 10m }, 100m, 0m, 0m)
        {
            SyntheticBaselinePrice = 10m,
        });
        basket.Candles.Add(new OhlcPoint(candleTime, 10m, 10m, 10m, 10m));

        var result = SyntheticTerminalLiveUpdate.Apply(
            basket,
            new QuoteUpdate("LIVE-A", 12m, 12.2m, 12m, DateTimeOffset.Parse("2026-07-25T00:01:00Z")));

        if (!result.Matched) throw new Exception("terminal live update must report matching component ticks");
        if (!result.CandleChanged) throw new Exception("terminal live update must update the current synthetic candle");
        if (result.Tick is null) throw new Exception("terminal live update must return a compact tick payload");
        if (result.Tick.Candle is null) throw new Exception("a changed live candle must be included in the compact tick payload");
        AssertNear(12m, result.Tick.Candle.Close, "terminal tick must contain the updated synthetic close");
        AssertEqual(1, result.Tick.ComponentQuotes.Count, "terminal tick should carry component quote metadata without full chart history");
        AssertEqual(DateTimeOffset.Parse("2026-07-25T00:01:00Z"), result.Tick.ComponentQuotes[0].QuoteTimestamp,
            "stream quote timestamp must flow into the terminal tick");
    }

    private static void FinalStaticAcceptanceChecks()
    {
        var terminalXaml = File.ReadAllText(SourcePath("desktop", "CAPETF.Desktop", "CapComTerminalWindow.xaml"));
        foreach (var required in new[] { "x:Name=\"UniverseBox\"", "x:Name=\"OperationProgressBar\"" })
        {
            if (!terminalXaml.Contains(required, StringComparison.Ordinal))
            {
                throw new Exception($"terminal XAML must retain final acceptance control {required}");
            }
        }

        var assetsPath = Path.Combine(AppContext.BaseDirectory, "Assets");
        foreach (var required in new[] { "synthetic-terminal.html", "lightweight-charts.standalone.production.js" })
        {
            if (!File.Exists(Path.Combine(assetsPath, required)))
            {
                throw new Exception($"desktop output must include local chart asset {required}");
            }
        }

        if (!File.Exists(Path.Combine(AppContext.BaseDirectory, "data", "etfs.enc.json")))
        {
            throw new Exception("desktop output must include encrypted ETF data");
        }

        var html = File.ReadAllText(Path.Combine(assetsPath, "synthetic-terminal.html"));
        if (!html.Contains("<span>CAPETF Terminal V4</span>", StringComparison.Ordinal))
        {
            throw new Exception("terminal HTML footer must identify Terminal V4");
        }
    }

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

    private static void SyntheticTerminalHtmlShowsBidAndAskWithoutLastPrice()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "synthetic-terminal.html");
        if (!File.Exists(path)) throw new Exception("terminal chart HTML must be copied to output");
        var html = File.ReadAllText(path);

        if (!html.Contains("Bid ${money(bid)}  Ask ${money(ask)}", StringComparison.Ordinal))
        {
            throw new Exception("terminal chart HTML must render bid and ask metadata");
        }

        foreach (var forbidden in new[]
        {
            "const syntheticLast",
            "id=\"last-price\"",
            "Last ${money(syntheticLast)}",
            "addPriceLine(syntheticLast, 'Last'",
            "getField(component, 'Last', 'last', null)",
        })
        {
            if (html.Contains(forbidden, StringComparison.Ordinal))
            {
                throw new Exception($"terminal chart HTML must not render a synthetic last price: {forbidden}");
            }
        }
    }

    private static void SeededSyntheticSelectorBuildsNikeBasket()
    {
        var day = DateTimeOffset.Parse("2024-01-01T00:00:00Z");
        var instruments = new[]
        {
            CreateSeedStock("NKE", "Nike Inc"),
            CreateSeedStock("LULU", "Lululemon Athletica"),
            CreateSeedStock("DECK", "Deckers Outdoor"),
            CreateSeedStock("ADBE", "Adobe Systems Inc"),
            CreateSeedStock("BAH", "Booz Allen Hamilton"),
        };
        var candles = instruments.ToDictionary(
            instrument => instrument.Epic,
            instrument => CreateVariableCandles(day, 100m));

        var basket = SeededSyntheticSelector.SelectSeededBasket("NKE", "US / USD / All", instruments, candles, periodsPerYear: 52);

        if (basket is null) throw new Exception("Nike seed should build a synthetic basket");
        if (basket.Symbol != "SYN-NKE-01") throw new Exception("Nike seeded basket should use an NKE synthetic symbol");
        if (basket.Components.Count != 3) throw new Exception("Nike seeded basket should contain exactly three legs");
        if (basket.Components.All(component => component.Instrument.Epic != "NKE")) throw new Exception("Nike seeded basket must include NKE");
        if (basket.Components.Any(component => component.Instrument.Epic == "ADBE")) throw new Exception("Nike seeded basket should prefer apparel peers over unrelated software stocks");
        if (basket.Components.Any(component => component.Instrument.Epic == "BAH")) throw new Exception("Nike seeded basket should not treat generic holding-company names as apparel peers");
    }

    private static void SeededSyntheticSelectorPrefersExactNikeTickerOverNameContains()
    {
        var day = DateTimeOffset.Parse("2024-01-01T00:00:00Z");
        var instruments = new[]
        {
            CreateSeedStock("EAT", "Brinker"),
            CreateSeedStock("AZO", "AutoZone Inc"),
            CreateSeedStock("NKE", "Nike Inc"),
            CreateSeedStock("LULU", "Lululemon Athletica"),
            CreateSeedStock("DECK", "Deckers Outdoor"),
        };
        var candles = instruments.ToDictionary(
            instrument => instrument.Epic,
            instrument => CreateVariableCandles(day, 100m));

        var basket = SeededSyntheticSelector.SelectSeededBasket("NKE", "US / USD / All", instruments, candles, periodsPerYear: 52);

        if (basket is null) throw new Exception("exact NKE seed should build a basket");
        if (basket.Symbol != "SYN-NKE-01") throw new Exception("exact ticker match must outrank name contains match like Brinker");
        if (basket.Components.All(component => component.Instrument.Epic != "NKE")) throw new Exception("exact NKE seed must include Nike");
    }

    private static void SeededSyntheticSelectorDoesNotResolveShortTickersByLooseNameContains()
    {
        var day = DateTimeOffset.Parse("2024-01-01T00:00:00Z");
        var instruments = new[]
        {
            CreateSeedStock("AAA", "Sapphire Holdings"),
            CreateSeedStock("BBB", "Comparable One"),
            CreateSeedStock("CCC", "Comparable Two"),
        };
        var candles = instruments.ToDictionary(
            instrument => instrument.Epic,
            instrument => CreateVariableCandles(day, 100m));

        var basket = SeededSyntheticSelector.SelectSeededBasket("SAP", "US / USD / All", instruments, candles, periodsPerYear: 52);

        if (basket is not null) throw new Exception("short ticker-like seeds must not resolve by loose company-name contains matches");
    }

    private static void SeededSyntheticSelectorMatchesCapitalSuffixSymbol()
    {
        var day = DateTimeOffset.Parse("2024-01-01T00:00:00Z");
        var sap = new MarketInstrument
        {
            Epic = "SAPD",
            Name = "SAP",
            Symbol = "SAPd",
            Type = "SHARES",
            Currency = "EUR",
            Region = "Europe",
            Sector = "All",
        };
        var peerOne = new MarketInstrument
        {
            Epic = "ADS",
            Name = "Adidas",
            Symbol = "ADS",
            Type = "SHARES",
            Currency = "EUR",
            Region = "Europe",
            Sector = "All",
        };
        var peerTwo = new MarketInstrument
        {
            Epic = "PUM",
            Name = "Puma",
            Symbol = "PUM",
            Type = "SHARES",
            Currency = "EUR",
            Region = "Europe",
            Sector = "All",
        };
        var instruments = new[] { sap, peerOne, peerTwo };
        var candles = instruments.ToDictionary(
            instrument => instrument.Epic,
            instrument => CreateVariableCandles(day, 100m));

        var basket = SeededSyntheticSelector.SelectSeededBasket("sap", "Europe / EUR / All", instruments, candles, periodsPerYear: 52);

        if (basket is null) throw new Exception("SAP seed should resolve Capital.com SAPd symbol");
        if (basket.Symbol != "SYN-SAPD-01") throw new Exception("SAP seed should build from the Capital.com SAPd instrument");
        if (basket.Components.All(component => component.Instrument.Epic != "SAPD")) throw new Exception("SAP basket must include SAPD");
    }

    private static void SeededSyntheticSelectorBuildsSapFromEncryptedChunks()
    {
        var cached = DashboardStockChunkLoader.LoadStocks();
        if (cached.Instruments.Count == 0) return;

        var weekly = cached.OhlcByEpicAndResolution?
            .Where(pair => pair.Value.TryGetValue("Weekly", out var rows) && rows.Count >= 120)
            .ToDictionary(pair => pair.Key, pair => pair.Value["Weekly"], StringComparer.OrdinalIgnoreCase)
            ?? cached.OhlcByEpic;

        var basket = SeededSyntheticSelector.SelectSeededBasket("sap", "Europe / EUR / All", cached.Instruments, weekly, periodsPerYear: 52);

        if (basket is null) throw new Exception("SAP should build from encrypted stock chunks");
        if (basket.Components.All(component => component.Instrument.Epic != "SAPD")) throw new Exception("encrypted SAP basket must include SAPD");
        if (!string.Equals(basket.Block, "Europe / EUR / All", StringComparison.OrdinalIgnoreCase)) throw new Exception("SAP basket should remain in Europe / EUR / All");
    }

    private static void SeededSyntheticSelectorBuildsSapFromEncryptedChunksForIntradayIntervals()
    {
        var cached = DashboardStockChunkLoader.LoadStocks();
        if (cached.Instruments.Count == 0 || cached.OhlcByEpicAndResolution is null) return;

        foreach (var (interval, minimumCandles, periodsPerYear) in new[] { ("2H", 30, 252 * 4), ("4H", 16, 252 * 2), ("6H", 10, 252) })
        {
            var candles = cached.OhlcByEpicAndResolution
                .Where(pair => pair.Value.TryGetValue(interval, out var rows) && rows.Count >= minimumCandles)
                .ToDictionary(pair => pair.Key, pair => pair.Value[interval], StringComparer.OrdinalIgnoreCase);
            if (candles.Count == 0 || !candles.ContainsKey("SAPD")) continue;

            var basket = SeededSyntheticSelector.SelectSeededBasket(
                "sap",
                "Europe / EUR / All",
                cached.Instruments,
                candles,
                periodsPerYear,
                minimumCandles);

            if (basket is null)
            {
                var sapRows = candles.TryGetValue("SAPD", out var rows) ? rows.Count : 0;
                var eurPeers = cached.Instruments.Count(item =>
                    item.Epic != "SAPD" &&
                    string.Equals(item.Currency, "EUR", StringComparison.OrdinalIgnoreCase) &&
                    candles.ContainsKey(item.Epic));
                throw new Exception($"SAP should build from encrypted stock chunks for {interval}; SAP rows {sapRows}, EUR peers {eurPeers}, candle symbols {candles.Count}");
            }
            if (basket.Components.All(component => component.Instrument.Epic != "SAPD")) throw new Exception($"encrypted SAP {interval} basket must include SAPD");
        }
    }

    private static void SeededSyntheticBuildsDoNotUseGenericHistoryFallback()
    {
        var shouldFallback = SyntheticTerminalBuildPolicy.ShouldUseGenericHistoryFallback(
            SyntheticStrategyKind.SimilarToSelectedSymbol,
            seedText: "sap",
            usableCachedCandles: 0,
            genericSelectionCandidateCount: 0);

        if (shouldFallback)
        {
            throw new Exception("seeded builds must preserve the full cached universe instead of replacing it with the generic API fallback");
        }

        shouldFallback = SyntheticTerminalBuildPolicy.ShouldUseGenericHistoryFallback(
            SyntheticStrategyKind.DipInsideUptrend,
            seedText: "",
            usableCachedCandles: 0,
            genericSelectionCandidateCount: 0);

        if (!shouldFallback)
        {
            throw new Exception("non-seeded strategy builds should still use the generic API fallback when cached history is empty");
        }
    }

    private static void EmptyCachedHistoryLoadsBoundedApiCandidatesAndBuildsBasket()
    {
        var start = DateTimeOffset.Parse("2025-01-01T00:00:00Z");
        var candidates = Enumerable.Range(0, 6)
            .Select(index => CreateStock($"API-{index}", $"API candidate {index}"))
            .ToList();
        var loadCalls = 0;

        var candles = SyntheticTerminalBuildPolicy.LoadCandidateHistoryFallbackAsync(
            SyntheticStrategyKind.DipInsideUptrend,
            seedText: "",
            candidates,
            new Dictionary<string, IReadOnlyList<OhlcPoint>>(StringComparer.OrdinalIgnoreCase),
            maximumCandidates: 4,
            selected =>
            {
                loadCalls++;
                AssertEqual(4, selected.Count, "API fallback history load must remain bounded");
                var loaded = selected.ToDictionary(
                    item => item.Epic,
                    item => CreateVariableCandles(start, 100m + selected.ToList().IndexOf(item) * 10m),
                    StringComparer.OrdinalIgnoreCase);
                return Task.FromResult(new HistoryLoadResult(loaded, start, start.AddDays(119), 120));
            }).GetAwaiter().GetResult();

        AssertEqual(1, loadCalls, "empty cache must trigger one bounded candidate-history load");
        var basket = SyntheticTerminalSelector.SelectBest("US / USD / Tech", candidates, candles, 252, 120);
        if (basket is null) throw new Exception("API fallback candidate history must support an end-to-end basket build");
    }

    private static void SeedSearchOptionsIncludePalantirByNameAndSymbolAcrossBlocks()
    {
        var palantir = new MarketInstrument
        {
            Epic = "PLTR",
            Symbol = "PLTR",
            Name = "Palantir Technologies Inc",
            Type = "SHARES",
            Region = "US",
            Currency = "USD",
            Sector = "",
        };
        var sap = new MarketInstrument
        {
            Epic = "SAPD",
            Symbol = "SAPd",
            Name = "SAP SE",
            Type = "SHARES",
            Region = "Europe",
            Currency = "EUR",
            Sector = "All",
        };

        var options = SeedSearchOptionBuilder.BuildOptions([palantir, sap], selectedBlock: "Europe / EUR / All");

        if (!options.Any(option => option.StartsWith("PLTR | Palantir Technologies Inc", StringComparison.OrdinalIgnoreCase)))
        {
            throw new Exception("seed dropdown must include a symbol-first Palantir option even outside the selected block");
        }

        if (!options.Any(option => option.StartsWith("Palantir Technologies Inc | PLTR", StringComparison.OrdinalIgnoreCase)))
        {
            throw new Exception("seed dropdown must include a name-first Palantir option so typing Palantir finds it");
        }
    }

    private static void SyntheticTerminalHtmlUsesPackagedChartLibrary()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "synthetic-terminal.html");
        if (!File.Exists(path)) throw new Exception("terminal chart HTML must be copied to output");
        var html = File.ReadAllText(path);
        if (html.Contains("unpkg.com", StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception("terminal chart HTML must use the packaged Lightweight Charts script, not a CDN");
        }
        var scriptPath = Path.Combine(AppContext.BaseDirectory, "Assets", "lightweight-charts.standalone.production.js");
        if (!File.Exists(scriptPath)) throw new Exception("packaged Lightweight Charts script must be copied to output");
    }

    private static void SyntheticTerminalHtmlRejectsKLineChartRuntime()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "synthetic-terminal.html");
        if (!File.Exists(path)) throw new Exception("terminal chart HTML must be copied to output");
        var html = File.ReadAllText(path);
        if (html.Contains("klinecharts.min.js", StringComparison.Ordinal) ||
            html.Contains("klinecharts.init", StringComparison.Ordinal))
        {
            throw new Exception("terminal V3 HTML must not use the previous KLineChart runtime");
        }
    }

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

    private static void SyntheticTerminalHtmlUsesV5SeriesApiAndChartSideTools()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "synthetic-terminal.html");
        if (!File.Exists(path)) throw new Exception("terminal chart HTML must be copied to output");
        var html = File.ReadAllText(path);
        foreach (var required in new[]
        {
            "LightweightCharts.version()",
            "chart.addSeries(LightweightCharts.CandlestickSeries",
            "chart.addSeries(LightweightCharts.LineSeries",
            "CapComDrawings.createManager",
            "id=\"drawing-tool-rail\"",
            "data-tool=\"cursor\"",
            "data-tool=\"trend\"",
            "data-tool=\"hline\"",
            "window.setTerminalTool",
            "window.clearTerminalDrawings",
        })
        {
            if (!html.Contains(required, StringComparison.Ordinal))
            {
                throw new Exception($"terminal chart HTML must use Lightweight Charts v5-style chart tools: missing {required}");
            }
        }

        if (html.Contains("addCandlestickSeries", StringComparison.Ordinal) ||
            html.Contains("addLineSeries", StringComparison.Ordinal))
        {
            throw new Exception("terminal chart should use the v5 addSeries API, not legacy addCandlestickSeries/addLineSeries helpers");
        }
    }

    private static void SyntheticTerminalHtmlExposesResizeFunction()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "synthetic-terminal.html");
        if (!File.Exists(path)) throw new Exception("terminal chart HTML must be copied to output");
        var html = File.ReadAllText(path);
        if (!html.Contains("window.resizeTerminal", StringComparison.Ordinal))
        {
            throw new Exception("terminal chart HTML must expose window.resizeTerminal so WPF can resize a chart initialized while hidden");
        }
    }

    private static void SyntheticTerminalHtmlExposesDecisionChartControls()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "synthetic-terminal.html");
        if (!File.Exists(path)) throw new Exception("terminal chart HTML must be copied to output");
        var html = File.ReadAllText(path);
        foreach (var required in new[]
        {
            "window.setTerminalChartMode",
            "window.toggleTerminalMa",
            "window.toggleTerminalComponents",
            "window.fitTerminalChart",
            "heikin",
            "LineSeries",
            "subscribeCrosshairMove",
            "MA20",
            "MA50",
            "MA200",
        })
        {
            if (!html.Contains(required, StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception($"terminal chart HTML missing decision-chart control {required}");
            }
        }
    }

    private static void SyntheticTerminalHtmlExposesV2TerminalControls()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "synthetic-terminal.html");
        if (!File.Exists(path)) throw new Exception("terminal chart HTML must be copied to output");
        var html = File.ReadAllText(path);
        foreach (var required in new[]
        {
            "LightweightCharts.createChart",
            "window.setTerminalInterval",
            "window.setTerminalChartMode",
            "window.toggleTerminalMa",
            "window.placeSyntheticPreviewOrder",
            "order-ticket",
            "SYNTHETIC FORMULA",
            "VOL MATCH"
        })
        {
            if (!html.Contains(required, StringComparison.Ordinal))
            {
                throw new Exception($"terminal V3 HTML missing expected control {required}");
            }
        }
    }

    private static void SyntheticDrawingsAssetPublishesWithProjectContentEntry()
    {
        var sourcePath = SourcePath("desktop", "CAPETF.Desktop", "Assets", "synthetic-drawings.js");
        var outputPath = Path.Combine(AppContext.BaseDirectory, "Assets", "synthetic-drawings.js");
        var lucideSourcePath = SourcePath("desktop", "CAPETF.Desktop", "Assets", "lucide.min.js");
        var lucideLicensePath = SourcePath("desktop", "CAPETF.Desktop", "Assets", "lucide.LICENSE.txt");
        var lucideOutputPath = Path.Combine(AppContext.BaseDirectory, "Assets", "lucide.min.js");
        var projectPath = SourcePath("desktop", "CAPETF.Desktop", "CAPETF.Desktop.csproj");
        var project = File.ReadAllText(projectPath);
        var missing = new List<string>();

        if (!File.Exists(sourcePath)) missing.Add("source asset desktop/CAPETF.Desktop/Assets/synthetic-drawings.js");
        if (!File.Exists(outputPath)) missing.Add("copied output asset Assets/synthetic-drawings.js");
        if (!File.Exists(lucideSourcePath)) missing.Add("source asset desktop/CAPETF.Desktop/Assets/lucide.min.js");
        if (!File.Exists(lucideLicensePath)) missing.Add("Lucide license notice desktop/CAPETF.Desktop/Assets/lucide.LICENSE.txt");
        if (!File.Exists(lucideOutputPath)) missing.Add("copied output asset Assets/lucide.min.js");
        if (!project.Contains("<Content Include=\"Assets\\synthetic-drawings.js\">", StringComparison.Ordinal))
        {
            missing.Add("CAPETF.Desktop.csproj synthetic-drawings.js content entry");
        }
        if (!project.Contains("<Content Include=\"Assets\\lucide.min.js\">", StringComparison.Ordinal) ||
            !project.Contains("<Content Include=\"Assets\\lucide.LICENSE.txt\">", StringComparison.Ordinal))
        {
            missing.Add("CAPETF.Desktop.csproj Lucide bundle/license content entries");
        }

        if (missing.Count > 0)
        {
            throw new Exception($"drawing manager packaging contract missing: {string.Join(", ", missing)}");
        }

    }

    private static void SyntheticDrawingsRuntimeValidatesRecordsMeasurementAndHistory()
    {
        var modulePath = SourcePath("desktop", "CAPETF.Desktop", "Assets", "synthetic-drawings.js");
        const string script = """
            const assert = require('node:assert/strict');
            global.window = globalThis;
            require(process.argv[1]);

            const api = window.CapComDrawings;
            assert.ok(api);
            for (const name of ['calculateMeasurement', 'validateRecord', 'sanitizeRecords', 'createManager']) {
              assert.equal(typeof api[name], 'function', `missing API ${name}`);
            }

            const p1 = { time: 100, price: 100 };
            const p2 = { time: 300, price: 110 };
            const common = { style: { color: '#22c55e', lineWidth: 2, lineStyle: 'solid' }, visible: true, locked: false };
            const records = [
              { ...common, id: 'trend-1', type: 'trend', p1, p2 },
              { ...common, id: 'ray-1', type: 'ray', p1, p2 },
              { ...common, id: 'hline-1', type: 'hline', price: 105 },
              { ...common, id: 'vline-1', type: 'vline', time: 200 },
              { ...common, id: 'fib-1', type: 'fib', p1, p2 },
              { ...common, id: 'rectangle-1', type: 'rectangle', p1, p2 },
              { ...common, id: 'brush-1', type: 'brush', points: [p1, p2] },
              { ...common, id: 'text-1', type: 'text', point: p1, text: 'A bounded note' },
              { ...common, id: 'measure-1', type: 'measure', p1, p2 },
            ];

            assert.deepEqual(records.map(record => record.type),
              ['trend', 'ray', 'hline', 'vline', 'fib', 'rectangle', 'brush', 'text', 'measure']);
            assert.ok(records.every(record => api.validateRecord(record)), 'all nine record types must validate');

            const sanitized = api.sanitizeRecords([...records, null, { id: 'bad', type: 'unknown' }]);
            assert.equal(sanitized.length, 9);
            assert.notEqual(sanitized[0], records[0], 'sanitization must return JSON-safe clones');
            assert.doesNotThrow(() => JSON.stringify(sanitized));
            const longText = api.sanitizeRecords([{ ...records[7], text: 'x'.repeat(2000) }]);
            assert.equal(longText.length, 1);
            assert.ok(longText[0].text.length <= 500, 'text annotations must be bounded');

            const cyclic = { id: 'cycle', type: 'hline', price: 100 };
            cyclic.self = cyclic;
            for (const malformed of [undefined, null, 42, cyclic, { id: 'nan', type: 'hline', price: NaN }]) {
              assert.doesNotThrow(() => api.validateRecord(malformed));
              assert.equal(api.validateRecord(malformed), false);
            }
            assert.doesNotThrow(() => api.sanitizeRecords([cyclic, null]));
            assert.deepEqual(api.sanitizeRecords([cyclic, null]), []);

            const measurement = api.calculateMeasurement(p1, p2, [50, 100, 200, 300, 400]);
            assert.deepEqual(measurement, {
              startPrice: 100,
              endPrice: 110,
              priceDelta: 10,
              percentDelta: 10,
              bars: 3,
              elapsedMs: 200000,
            });
            const reverse = api.calculateMeasurement(p2, p1, [50, 100, 200, 300, 400]);
            assert.equal(reverse.bars, 3, 'bar count must be inclusive regardless of anchor direction');
            assert.equal(reverse.elapsedMs, -200000, 'elapsed time must preserve anchor direction');
            assert.equal(api.calculateMeasurement({ time: 100, price: 0 }, p2, [100, 300]).percentDelta, null);

            const handlers = new Map();
            const container = {
              addEventListener(name, handler) { handlers.set(name, handler); },
              removeEventListener(name) { handlers.delete(name); },
              getBoundingClientRect() { return { left: 0, top: 0, width: 800, height: 400 }; },
              setPointerCapture() {},
              releasePointerCapture() {},
            };
            const timeScale = {
              timeToCoordinate(time) { return Number(time); },
              coordinateToTime(x) { return x; },
            };
            let attached = null;
            const chart = { timeScale() { return timeScale; } };
            const series = {
              priceToCoordinate(price) { return Number(price); },
              coordinateToPrice(y) { return y; },
              attachPrimitive(primitive) { attached = primitive; },
              detachPrimitive(primitive) { if (attached === primitive) attached = null; },
            };
            const manager = api.createManager({ chart, series, container, orderedTimes: [100, 200, 300] });
            for (const method of ['setTool', 'setRecords', 'getRecords', 'undo', 'redo', 'deleteSelected', 'clear',
              'setLocked', 'setVisible', 'setMagnet', 'setStyle', 'dispose']) {
              assert.equal(typeof manager[method], 'function', `missing manager method ${method}`);
            }
            assert.ok(attached, 'manager must attach an internal series primitive');
            manager.setRecords([records[0]]);
            manager.setVisible(false);
            assert.equal(manager.undo(), true);
            let strokeCount = 0;
            const context = {
              save() {}, restore() {}, setLineDash() {}, beginPath() {}, moveTo() {}, lineTo() {},
              stroke() { strokeCount += 1; }, fillRect() {}, strokeRect() {}, fillText() {}, arc() {}, fill() {},
              measureText() { return { width: 0 }; },
            };
            attached.paneViews()[0].renderer().draw({
              useBitmapCoordinateSpace(draw) {
                draw({ context, horizontalPixelRatio: 1, verticalPixelRatio: 1, bitmapSize: { width: 800, height: 400 } });
              },
            });
            assert.ok(strokeCount > 0, 'undoing global hide must make restored drawings render again');

            manager.setRecords([records[0]]);
            for (let index = 0; index < 120; index += 1) manager.setLocked(index % 2 === 0);
            let undoCount = 0;
            while (manager.undo()) undoCount += 1;
            assert.equal(undoCount, 100, 'undo history must be bounded at 100 states while retaining at least 50');
            let redoCount = 0;
            while (manager.redo()) redoCount += 1;
            assert.equal(redoCount, 100, 'every retained undo snapshot must be redoable');
            manager.dispose();
            assert.equal(attached, null, 'dispose must detach the internal primitive');
            """;

        RunNodeDrawingContract(modulePath, script, "drawing API/measurement/history");
    }

    private static void SyntheticDrawingsRuntimeExercisesReviewRegressions()
    {
        var modulePath = SourcePath("desktop", "CAPETF.Desktop", "Assets", "synthetic-drawings.js");
        const string script = """
            const assert = require('node:assert/strict');
            const windowHandlers = new Map();
            global.window = globalThis;
            global.addEventListener = (name, handler) => {
              if (!windowHandlers.has(name)) windowHandlers.set(name, new Set());
              windowHandlers.get(name).add(handler);
            };
            global.removeEventListener = (name, handler) => windowHandlers.get(name)?.delete(handler);
            require(process.argv[1]);

            const api = window.CapComDrawings;
            const failures = [];
            const common = {
              style: { color: '#22c55e', fillColor: 'rgba(34, 197, 94, .12)', lineWidth: 2, lineStyle: 'solid' },
              visible: true,
              locked: false,
            };
            const trend = {
              ...common,
              id: 'trend-review',
              type: 'trend',
              p1: { time: 100, price: 100 },
              p2: { time: 300, price: 110 },
            };

            function test(name, body) {
              try { body(); }
              catch (error) { failures.push(`${name}: ${error.message}`); }
            }

            function makeContainer() {
              const handlers = new Map();
              return {
                handlers,
                addEventListener(name, handler) { handlers.set(name, handler); },
                removeEventListener(name, handler) {
                  if (handlers.get(name) === handler) handlers.delete(name);
                },
                getBoundingClientRect() { return { left: 0, top: 0, width: 800, height: 400 }; },
                setPointerCapture() {},
                releasePointerCapture() {},
              };
            }

            function makeEnvironment(options = {}) {
              const container = makeContainer();
              let primitive = null;
              let requestUpdates = 0;
              let detachCount = 0;
              const timeScale = {
                timeToCoordinate(time) { return typeof time === 'number' ? time : null; },
                coordinateToTime(x) { return x >= 0 && x <= 800 ? x : null; },
              };
              const chart = { timeScale() { return timeScale; } };
              const series = {
                priceToCoordinate(price) { return Number.isFinite(Number(price)) ? Number(price) : null; },
                coordinateToPrice(y) { return y >= 0 && y <= 400 ? y : null; },
                attachPrimitive(value) {
                  if (options.attachThrows) throw new Error('attach failed');
                  primitive = value;
                  value.attached({ requestUpdate() { requestUpdates += 1; } });
                },
                detachPrimitive(value) {
                  if (primitive === value) primitive = null;
                  detachCount += 1;
                  value.detached();
                },
              };
              const manager = api.createManager({ chart, series, container, orderedTimes: [100, 200, 300] });
              return {
                container,
                manager,
                get primitive() { return primitive; },
                get requestUpdates() { return requestUpdates; },
                get detachCount() { return detachCount; },
              };
            }

            function pointer(x, y, extras = {}) {
              return {
                clientX: x,
                clientY: y,
                button: 0,
                pointerId: 7,
                target: { tagName: 'DIV', isContentEditable: false },
                defaultPrevented: false,
                preventDefault() { this.defaultPrevented = true; },
                ...extras,
              };
            }

            function dispatchContainer(environment, name, event) {
              const handler = environment.container.handlers.get(name);
              assert.equal(typeof handler, 'function', `missing ${name} handler`);
              handler(event);
            }

            function dispatchWindow(name, event) {
              for (const handler of [...(windowHandlers.get(name) || [])]) handler(event);
            }

            function render(environment, horizontalPixelRatio, verticalPixelRatio) {
              const strokes = [];
              let path = [];
              const context = {
                lineWidth: 0,
                save() {}, restore() {}, setLineDash() {},
                beginPath() { path = []; },
                moveTo(x, y) { path.push(['moveTo', x, y]); },
                lineTo(x, y) { path.push(['lineTo', x, y]); },
                stroke() { strokes.push({ lineWidth: this.lineWidth, path: path.map(entry => [...entry]) }); },
                fillRect() {}, strokeRect() {}, fillText() {}, arc() {}, fill() {},
                measureText() { return { width: 0 }; },
              };
              const primitive = environment.primitive;
              assert.ok(primitive, 'primitive must remain attached');
              primitive.updateAllViews();
              primitive.paneViews()[0].renderer().draw({
                useBitmapCoordinateSpace(draw) {
                  draw({
                    context,
                    horizontalPixelRatio,
                    verticalPixelRatio,
                    bitmapSize: { width: 800 * horizontalPixelRatio, height: 400 * verticalPixelRatio },
                  });
                },
              });
              return strokes;
            }

            function withEnvironment(body) {
              const environment = makeEnvironment();
              try { body(environment); }
              finally { environment.manager.dispose(); }
            }

            test('rejects a brush when any nested point is malformed', () => {
              const malformed = {
                ...common,
                id: 'brush-malformed',
                type: 'brush',
                points: [{ time: 100, price: 100 }, null, { time: 300, price: 110 }],
              };
              assert.equal(api.validateRecord(malformed), false);
              assert.deepEqual(api.sanitizeRecords([malformed]), []);
            });

            test('rejects impossible business-day timestamps', () => {
              const invalidDay = { year: 2025, month: 2, day: 31 };
              assert.equal(api.validateRecord({ ...common, id: 'bad-day', type: 'vline', time: invalidDay }), false);
              assert.equal(api.validateRecord({
                ...trend,
                p1: { time: invalidDay, price: 100 },
              }), false);
              assert.equal(api.calculateMeasurement(
                { time: invalidDay, price: 100 },
                { time: { year: 2025, month: 3, day: 2 }, price: 110 },
                [invalidDay]), null);
            });

            test('drives primitive attached/update/detached lifecycle', () => {
              const environment = makeEnvironment();
              const attachedPrimitive = environment.primitive;
              environment.manager.setRecords([trend]);
              assert.ok(environment.requestUpdates > 0, 'record changes must request a primitive update');
              environment.manager.dispose();
              const updatesAfterDispose = environment.requestUpdates;
              environment.manager.setRecords([trend]);
              assert.equal(environment.requestUpdates, updatesAfterDispose, 'detached primitive must stop requesting updates');
              assert.equal(environment.detachCount, 1);
              assert.equal(environment.primitive, null);
              assert.ok(attachedPrimitive.paneViews().length > 0);
            });

            test('places and drags drawings through hit-tested handlers', () => withEnvironment(environment => {
              environment.manager.setTool('trend');
              dispatchContainer(environment, 'click', pointer(100, 100));
              dispatchContainer(environment, 'click', pointer(300, 110));
              assert.deepEqual(environment.manager.getRecords().map(record => record.type), ['trend']);

              environment.manager.setTool('cursor');
              dispatchContainer(environment, 'pointerdown', pointer(200, 105));
              dispatchContainer(environment, 'pointermove', pointer(210, 125));
              dispatchContainer(environment, 'pointerup', pointer(210, 125));
              let moved = environment.manager.getRecords()[0];
              assert.deepEqual({ p1: moved.p1, p2: moved.p2 }, {
                p1: { time: 110, price: 120 },
                p2: { time: 310, price: 130 },
              });

              dispatchContainer(environment, 'pointerdown', pointer(110, 120));
              dispatchContainer(environment, 'pointermove', pointer(130, 140));
              dispatchContainer(environment, 'pointerup', pointer(130, 140));
              moved = environment.manager.getRecords()[0];
              assert.deepEqual(moved.p1, { time: 130, price: 140 });
              assert.deepEqual(moved.p2, { time: 310, price: 130 });
            }));

            test('pointer cancellation discards brush placement without history', () => withEnvironment(environment => {
              environment.manager.setTool('brush');
              dispatchContainer(environment, 'pointerdown', pointer(100, 100));
              dispatchContainer(environment, 'pointermove', pointer(120, 120));
              dispatchContainer(environment, 'pointercancel', pointer(120, 120));
              assert.deepEqual({ records: environment.manager.getRecords(), undo: environment.manager.undo() }, {
                records: [],
                undo: false,
              });
            }));

            test('pointer cancellation rolls back dragging without history', () => withEnvironment(environment => {
              environment.manager.setRecords([trend]);
              const before = environment.manager.getRecords();
              dispatchContainer(environment, 'pointerdown', pointer(200, 105));
              dispatchContainer(environment, 'pointermove', pointer(220, 125));
              dispatchContainer(environment, 'pointercancel', pointer(220, 125));
              assert.deepEqual({ records: environment.manager.getRecords(), undo: environment.manager.undo() }, {
                records: before,
                undo: false,
              });
            }));

            test('renders a vertical ray at non-unit pixel ratios', () => withEnvironment(environment => {
              environment.manager.setRecords([{
                ...common,
                id: 'vertical-ray',
                type: 'ray',
                p1: { time: 100, price: 100 },
                p2: { time: 100, price: 200 },
              }]);
              const strokes = render(environment, 2, 3);
              assert.equal(strokes.length, 1);
              assert.deepEqual(strokes[0].path, [
                ['moveTo', 200, 300],
                ['lineTo', 200, 1200],
              ]);
            }));

            test('uses horizontal pixel ratio for vertical stroke width', () => withEnvironment(environment => {
              environment.manager.setRecords([{
                ...common,
                id: 'vertical-line',
                type: 'vline',
                time: 100,
              }]);
              const strokes = render(environment, 2, 3);
              assert.equal(strokes.length, 1);
              assert.equal(strokes[0].lineWidth, 4);
            }));

            test('ignores deletion shortcuts from editable targets', () => withEnvironment(environment => {
              for (const target of [
                { tagName: 'INPUT', isContentEditable: false },
                { tagName: 'TEXTAREA', isContentEditable: false },
                { tagName: 'SELECT', isContentEditable: false },
                { tagName: 'DIV', isContentEditable: true },
              ]) {
                environment.manager.setRecords([trend]);
                dispatchContainer(environment, 'pointerdown', pointer(200, 105));
                dispatchContainer(environment, 'pointerup', pointer(200, 105));
                const event = { key: 'Delete', target, defaultPrevented: false, preventDefault() { this.defaultPrevented = true; } };
                dispatchWindow('keydown', event);
                assert.equal(environment.manager.getRecords().length, 1, `${target.tagName} must retain drawing`);
                assert.equal(event.defaultPrevented, false);
              }

              environment.manager.setRecords([trend]);
              dispatchContainer(environment, 'pointerdown', pointer(200, 105));
              dispatchContainer(environment, 'pointerup', pointer(200, 105));
              const chartEvent = {
                key: 'Backspace',
                target: { tagName: 'DIV', isContentEditable: false },
                defaultPrevented: false,
                preventDefault() { this.defaultPrevented = true; },
              };
              dispatchWindow('keydown', chartEvent);
              assert.equal(environment.manager.getRecords().length, 0);
              assert.equal(chartEvent.defaultPrevented, true);
            }));

            test('removes listeners if primitive attachment throws', () => {
              const container = makeContainer();
              const chart = { timeScale() { return {}; } };
              const series = { attachPrimitive() { throw new Error('attach failed'); } };
              assert.throws(() => api.createManager({ chart, series, container }), /attach failed/);
              assert.equal(container.handlers.size, 0);
              assert.equal(windowHandlers.get('keydown')?.size || 0, 0);
            });

            if (failures.length > 0) {
              throw new Error(`Round 1 drawing regressions:\n${failures.join('\n')}`);
            }
            """;

        RunNodeDrawingContract(modulePath, script, "drawing interactions/review regressions");
    }

    private static void SyntheticDrawingWorkspaceRuntimeCoordinatesManagerStateAndPersistence()
    {
        var modulePath = SourcePath("desktop", "CAPETF.Desktop", "Assets", "synthetic-drawings.js");
        const string script = """
            const assert = require('node:assert/strict');
            global.window = globalThis;
            global.addEventListener = () => {};
            global.removeEventListener = () => {};
            require(process.argv[1]);

            const api = window.CapComDrawings;
            for (const name of ['formatMeasurement', 'drawingStorageKey', 'loadStoredRecords',
              'persistStoredRecords', 'confirmClear', 'normalizeAnnotationText']) {
              assert.equal(typeof api[name], 'function', `missing workspace helper ${name}`);
            }

            const common = {
              style: { color: '#22c55e', fillColor: 'rgba(34, 197, 94, .12)', lineWidth: 2, lineStyle: 'solid' },
              visible: true,
              locked: false,
            };
            const trend = {
              ...common,
              id: 'persisted-trend',
              type: 'trend',
              p1: { time: 100, price: 100 },
              p2: { time: 300, price: 110 },
            };
            const measure = { ...trend, id: 'measure-context', type: 'measure' };
            const values = new Map();
            const storage = {
              getItem(key) { return values.get(key) ?? null; },
              setItem(key, value) { values.set(key, value); },
            };
            values.set('capcom-terminal-drawings:basket-a', JSON.stringify([trend, { type: 'bad' }]));
            assert.equal(api.drawingStorageKey('basket-a'), 'capcom-terminal-drawings:basket-a');
            assert.deepEqual(api.loadStoredRecords(storage, 'basket-a'), [trend], 'identity load must sanitize records');
            assert.deepEqual(api.loadStoredRecords(storage, 'missing'), []);
            values.set('capcom-terminal-drawings:broken', '{');
            assert.deepEqual(api.loadStoredRecords(storage, 'broken'), [], 'malformed storage must be ignored');
            assert.equal(api.persistStoredRecords(storage, 'basket-b', [trend, { type: 'bad' }]), true);
            assert.deepEqual(JSON.parse(values.get('capcom-terminal-drawings:basket-b')), [trend]);

            const handlers = new Map();
            const container = {
              addEventListener(name, handler) { handlers.set(name, handler); },
              removeEventListener(name) { handlers.delete(name); },
              getBoundingClientRect() { return { left: 0, top: 0, width: 800, height: 400 }; },
              setPointerCapture() {},
              releasePointerCapture() {},
            };
            const timeScale = {
              timeToCoordinate(time) { return Number(time); },
              coordinateToTime(x) { return x; },
            };
            const chart = { timeScale() { return timeScale; } };
            let attached = null;
            const series = {
              priceToCoordinate(price) { return Number(price); },
              coordinateToPrice(y) { return y; },
              attachPrimitive(primitive) { attached = primitive; },
              detachPrimitive() {},
            };
            const states = [];
            const changedRecords = [];
            const manager = api.createManager({
              chart,
              series,
              container,
              orderedTimes: [100, 300],
              onStateChanged(state) { states.push(state); },
              onRecordsChanged(records) { changedRecords.push(records); },
            });
            for (const method of ['getState', 'updateContext', 'cancel']) {
              assert.equal(typeof manager[method], 'function', `missing workspace manager method ${method}`);
            }
            assert.deepEqual(manager.getState(), {
              tool: 'cursor', selectedId: null, selectedStyle: null, magnet: false,
              locked: false, visible: true, canUndo: false, canRedo: false, recordCount: 0,
            });

            manager.setRecords([measure, { type: 'bad' }]);
            assert.equal(manager.getState().recordCount, 1);
            assert.equal(manager.getRecords()[0].bars, 2);
            manager.updateContext({ orderedTimes: [100, 200, 300] });
            assert.equal(manager.getRecords()[0].bars, 3, 'candle context refresh must update measurements');
            assert.ok(changedRecords.length >= 2, 'context refresh must publish JSON-safe records');
            const fills = [];
            const context = {
              save() {}, restore() {}, setLineDash() {}, beginPath() {}, moveTo() {}, lineTo() {}, stroke() {},
              strokeRect() {}, fillText() {}, arc() {}, fill() {},
              fillRect(x, y, width, height) { fills.push({ x, y, width, height }); },
              measureText() { return { width: 760 }; },
            };
            assert.doesNotThrow(() => attached.paneViews()[0].renderer().draw({
              useBitmapCoordinateSpace(draw) {
                draw({ context, horizontalPixelRatio: 1, verticalPixelRatio: 1, bitmapSize: { width: 800, height: 400 } });
              },
            }), 'measurement label renderer must use manager precision state');
            const labelFill = fills[fills.length - 1];
            assert.ok(labelFill.x >= 0 && labelFill.x + labelFill.width <= 800,
              'measurement label must remain inside the viewport');
            manager.setMagnet(true);
            assert.equal(manager.getState().magnet, true);
            manager.setLocked(true);
            assert.equal(manager.getState().canUndo, true);
            assert.equal(manager.undo(), true);
            assert.equal(manager.getState().canRedo, true, 'undo/redo disabled state must be observable');
            assert.ok(states.length > 0, 'manager changes must notify state observers');

            let confirms = 0;
            assert.equal(api.confirmClear(manager, () => { confirms += 1; return false; }), false);
            assert.equal(manager.getRecords().length, 1, 'declined clear must retain drawings');
            assert.equal(api.confirmClear(manager, () => { confirms += 1; return true; }), true);
            assert.equal(confirms, 2);
            assert.equal(manager.getRecords().length, 0);
            assert.equal(api.confirmClear(manager, () => { throw new Error('must not confirm empty records'); }), false);

            assert.equal(api.normalizeAnnotationText('   '), null);
            assert.equal(api.normalizeAnnotationText('x'.repeat(700)).length, 500);
            manager.setTool('text', api.normalizeAnnotationText('A note'));
            handlers.get('click')({ clientX: 100, clientY: 100 });
            assert.equal(manager.getRecords()[0].text, 'A note');
            manager.setTool('text', api.normalizeAnnotationText('cancelled'));
            manager.cancel();
            assert.equal(manager.getState().tool, 'cursor');
            assert.equal(manager.getRecords().length, 1, 'cancelled text placement must create no drawing');

            assert.deepEqual(api.formatMeasurement({
              startPrice: 100,
              endPrice: 110,
              priceDelta: 10,
              percentDelta: 10,
              bars: 3,
              elapsedMs: 7200000,
            }, 5), {
              label: '100.00000 -> 110.00000  +10.00000 (+10.00%)  3 bars  2h',
              tone: 'positive',
            });
            assert.deepEqual(api.formatMeasurement({
              startPrice: 0,
              endPrice: 0,
              priceDelta: 0,
              percentDelta: null,
              bars: 1,
              elapsedMs: 0,
            }, 5), {
              label: '0.00000 -> 0.00000  +0.00000 (n/a)  1 bar  0s',
              tone: 'neutral',
            });
            manager.dispose();
            """;

        RunNodeDrawingContract(modulePath, script, "drawing workspace manager integration");
    }

    private static void RunNodeDrawingContract(string modulePath, string script, string contractName)
    {
        var startInfo = new ProcessStartInfo("node")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add("-e");
        startInfo.ArgumentList.Add(script);
        startInfo.ArgumentList.Add(modulePath);

        using var process = Process.Start(startInfo) ?? throw new Exception($"could not start Node.js {contractName} test");
        var standardOutput = process.StandardOutput.ReadToEnd();
        var standardError = process.StandardError.ReadToEnd();
        if (!process.WaitForExit(30_000))
        {
            process.Kill(entireProcessTree: true);
            throw new Exception($"Node.js {contractName} test timed out");
        }
        if (process.ExitCode != 0)
        {
            throw new Exception($"Node.js {contractName} test failed ({process.ExitCode}): {standardError}{standardOutput}");
        }
    }

    private static void TerminalOrderPreviewUsesProductionSizingBridge()
    {
        var source = File.ReadAllText(SourcePath("desktop", "CAPETF.Desktop", "CapComTerminalWindow.xaml.cs"));
        foreach (var required in new[]
        {
            "CoreWebView2.WebMessageReceived += TerminalWebMessageReceived",
            "SyntheticOrderSizing.BuildExecutableOrderPreview",
            "window.setTerminalOrderPreview",
        })
        {
            if (!source.Contains(required, StringComparison.Ordinal))
            {
                throw new Exception($"terminal order preview host bridge missing {required}");
            }
        }

        var html = File.ReadAllText(SourcePath("desktop", "CAPETF.Desktop", "Assets", "synthetic-terminal.html"));
        foreach (var required in new[]
        {
            "window.chrome.webview.postMessage",
            "window.setTerminalOrderPreview",
            "TotalExecutableNotional",
            "WeightImbalancePct",
        })
        {
            if (!html.Contains(required, StringComparison.Ordinal))
            {
                throw new Exception($"terminal order preview HTML bridge missing {required}");
            }
        }

        if (html.Contains("function executableLegQuantity", StringComparison.Ordinal) ||
            html.Contains("multiplier * formulaMultiplier", StringComparison.Ordinal))
        {
            throw new Exception("terminal HTML must not independently calculate executable quantities from chart formula multipliers");
        }

        var dashboardSource = File.ReadAllText(SourcePath("desktop", "CAPETF.Desktop", "MainWindow.xaml.cs"));
        foreach (var required in new[]
        {
            "TerminalChartWebView.CoreWebView2.WebMessageReceived += TerminalWebMessageReceived",
            "SyntheticOrderSizing.BuildExecutableOrderPreview",
            "window.setTerminalOrderPreview",
        })
        {
            if (!dashboardSource.Contains(required, StringComparison.Ordinal))
            {
                throw new Exception($"dashboard terminal order preview host bridge missing {required}");
            }
        }
    }

    private static void DesktopDefaultSearchDoesNotFilterStocksByEtf()
    {
        var xaml = File.ReadAllText(SourcePath("desktop", "CAPETF.Desktop", "MainWindow.xaml"));
        if (xaml.Contains("x:Name=\"DatasetBox\"", StringComparison.Ordinal) &&
            xaml.Contains("<ComboBoxItem Content=\"Stocks\" IsSelected=\"True\"/>", StringComparison.Ordinal) &&
            xaml.Contains("x:Name=\"SearchBox\" Text=\"ETF\"", StringComparison.Ordinal))
        {
            throw new Exception("desktop default search must not use ETF while the selected dataset is Stocks");
        }
    }

    private static void DesktopResizesTerminalChartWhenWorkspaceOpens()
    {
        var source = File.ReadAllText(SourcePath("desktop", "CAPETF.Desktop", "MainWindow.xaml.cs"));
        if (!source.Contains("ResizeTerminalChartAsync", StringComparison.Ordinal) ||
            !source.Contains("window.resizeTerminal", StringComparison.Ordinal) ||
            !source.Contains("_ = ResizeTerminalChartAsync();", StringComparison.Ordinal))
        {
            throw new Exception("desktop must resize the terminal chart after making the hidden terminal workspace visible");
        }
    }

    private static void DesktopTerminalWorkspaceExposesChartFirstControls()
    {
        var xaml = File.ReadAllText(SourcePath("desktop", "CAPETF.Desktop", "MainWindow.xaml"));
        foreach (var required in new[]
        {
            "x:Name=\"LeftColumn\"",
            "x:Name=\"RightColumn\"",
            "x:Name=\"TerminalTimeframeBox\"",
            "x:Name=\"TerminalCandleTypeBox\"",
            "x:Name=\"TerminalMa20Check\"",
            "x:Name=\"TerminalMa50Check\"",
            "x:Name=\"TerminalMa200Check\"",
            "Click=\"StreamTerminal_Click\"",
            "Click=\"FitTerminalChart_Click\"",
            "Click=\"ToggleTerminalComponents_Click\"",
        })
        {
            if (!xaml.Contains(required, StringComparison.Ordinal))
            {
                throw new Exception($"desktop terminal XAML missing chart-first control {required}");
            }
        }

        var source = File.ReadAllText(SourcePath("desktop", "CAPETF.Desktop", "MainWindow.xaml.cs"));
        foreach (var required in new[]
        {
            "ApplyTerminalLayout",
            "SelectedTerminalResolution",
            "SetTerminalChartModeAsync",
            "window.setTerminalChartMode",
            "window.toggleTerminalMa",
            "window.toggleTerminalComponents",
            "window.fitTerminalChart",
        })
        {
            if (!source.Contains(required, StringComparison.Ordinal))
            {
                throw new Exception($"desktop terminal source missing chart-first wiring {required}");
            }
        }
    }

    private static void DesktopTerminalWorkspaceExposesV2ProfessionalControls()
    {
        var xaml = File.ReadAllText(SourcePath("desktop", "CAPETF.Desktop", "MainWindow.xaml"));
        foreach (var required in new[]
        {
            "x:Name=\"TerminalIntervalBox\"",
            "x:Name=\"TerminalIndicatorBox\"",
            "Click=\"TerminalBuyPreview_Click\"",
            "Click=\"TerminalSellPreview_Click\"",
            "Click=\"TerminalResetView_Click\"",
        })
        {
            if (!xaml.Contains(required, StringComparison.Ordinal))
            {
                throw new Exception($"desktop terminal V2 XAML missing expected control {required}");
            }
        }

        var source = File.ReadAllText(SourcePath("desktop", "CAPETF.Desktop", "MainWindow.xaml.cs"));
        foreach (var required in new[]
        {
            "SetTerminalIntervalAsync",
            "SetTerminalIndicatorAsync",
            "PlaceSyntheticPreviewOrderAsync",
            "window.setTerminalInterval",
            "window.setTerminalIndicator",
            "window.placeSyntheticPreviewOrder",
        })
        {
            if (!source.Contains(required, StringComparison.Ordinal))
            {
                throw new Exception($"desktop terminal V2 source missing expected wiring {required}");
            }
        }
    }

    private static void CapComTerminalUsesClearActionLabelsAndSymbolDropdown()
    {
        var xaml = File.ReadAllText(SourcePath("desktop", "CAPETF.Desktop", "CapComTerminalWindow.xaml"));
        foreach (var required in new[]
        {
            "Content=\"Build Basket\"",
            "Content=\"Legs / Formula\"",
            "Content=\"Live\"",
            "Content=\"Zoom +\"",
            "Content=\"Zoom -\"",
            "Content=\"Reset\"",
            "Content=\"Save Basket\"",
            "ComboBox x:Name=\"SearchBox\"",
            "ComboBox x:Name=\"StrategyBox\"",
            "ComboBox x:Name=\"SavedBasketsBox\"",
            "x:Name=\"TerminalMa20Check\"",
            "x:Name=\"TerminalMa50Check\"",
            "x:Name=\"TerminalMa200Check\"",
            "x:Name=\"PriceLinesCheck\"",
            "IsEditable=\"True\"",
            "SelectionChanged=\"BlockBox_SelectionChanged\"",
            "SelectionChanged=\"SavedBaskets_SelectionChanged\"",
        })
        {
            if (!xaml.Contains(required, StringComparison.Ordinal))
            {
                throw new Exception($"cap.com Terminal XAML missing clear terminal UI element {required}");
            }
        }

        foreach (var misleading in new[]
        {
            "Content=\"Load Universe\"",
            "Content=\"Start Live Prices\"",
            "Content=\"Load Stocks\"",
            "Content=\"Build Synthetic\"",
            "Content=\"Nike Sample\"",
            "Content=\"Stream\"",
            "Content=\"Ticket\"",
            "Click=\"BuildNikeSample_Click\"",
        })
        {
            if (xaml.Contains(misleading, StringComparison.Ordinal))
            {
                throw new Exception($"cap.com Terminal XAML still has misleading element {misleading}");
            }
        }

        var source = File.ReadAllText(SourcePath("desktop", "CAPETF.Desktop", "CapComTerminalWindow.xaml.cs"));
        foreach (var required in new[]
        {
            "RebuildSeedOptions",
            "SeedText",
            "SelectedStrategy",
            "SelectStrategyCandidates",
            "SyntheticStrategyRanker.Rank",
            "GoToRealtime_Click",
            "window.goToRealtime",
            "ZoomIn_Click",
            "ZoomOut_Click",
            "PanLeft_Click",
            "PanRight_Click",
            "ResetChart_Click",
            "PriceLines_Changed",
            "Ma_Changed",
            "window.zoomTerminal",
            "window.panTerminal",
            "window.resetTerminalView",
            "window.togglePriceLines",
            "SaveBasket_Click",
            "SavedBaskets_SelectionChanged",
            "SavedSyntheticBasketStore",
            "LoadSavedBasketAsync",
            "await LoadStocksAsync();",
            "StartStreamingCurrentBasketAsync",
            "BlockBox_SelectionChanged",
            "SearchBox.ItemsSource",
        })
        {
            if (!source.Contains(required, StringComparison.Ordinal))
            {
                throw new Exception($"cap.com Terminal source missing dropdown seed wiring {required}");
            }
        }
        if (source.Contains("BuildNikeSample_Click", StringComparison.Ordinal) ||
            source.Contains("SearchBox.Text = \"NKE\"", StringComparison.Ordinal))
        {
            throw new Exception("NKE sample shortcut must be removed from cap.com Terminal");
        }

        var blockIndex = xaml.IndexOf("x:Name=\"BlockBox\"", StringComparison.Ordinal);
        var searchIndex = xaml.IndexOf("x:Name=\"SearchBox\"", StringComparison.Ordinal);
        var connectIndex = xaml.IndexOf("Click=\"Connect_Click\"", StringComparison.Ordinal);
        if (blockIndex < 0 || searchIndex < 0 || connectIndex < 0 || blockIndex > searchIndex || blockIndex > connectIndex)
        {
            throw new Exception("block selector must be the first working field in the cap.com Terminal toolbar");
        }

        var html = File.ReadAllText(SourcePath("desktop", "CAPETF.Desktop", "Assets", "synthetic-terminal.html"));
        foreach (var required in new[]
        {
            "Selection Basis",
            "Legs / Formula",
            "Annualized vol",
            "4Y return",
            "Role",
            "FormulaMultiplier",
            "DisplayMultiplier",
            "MinDealSize",
            "MinSizeIncrement",
            "FormulaReferencePrice",
            "goToRealtime",
            "scrollToRealTime",
            "window.zoomTerminal",
            "window.panTerminal",
            "window.resetTerminalView",
            "window.togglePriceLines",
            "createPriceLine",
            "removePriceLine",
            "flex-direction: column",
            "overflow-wrap: anywhere",
            "SYNTHETIC_PRICE_FLOOR = -10",
            "autoscaleInfoProvider",
        })
        {
            if (!html.Contains(required, StringComparison.Ordinal))
            {
                throw new Exception($"terminal HTML missing selection-basis label {required}");
            }
        }
        if (html.Contains(">Ticket<", StringComparison.Ordinal))
        {
            throw new Exception("terminal HTML should use Legs / Formula instead of Ticket");
        }
    }

    private static void CapComTerminalIntradayMinimumsFitCachedHourlyHistory()
    {
        var source = File.ReadAllText(SourcePath("desktop", "CAPETF.Desktop", "CapComTerminalWindow.xaml.cs"));
        foreach (var required in new[]
        {
            "\"2H\" => 30",
            "\"4H\" => 16",
            "\"6H\" => 10",
        })
        {
            if (!source.Contains(required, StringComparison.Ordinal))
            {
                throw new Exception($"cap.com Terminal intraday minimums should allow cached hourly data to build: missing {required}");
            }
        }
    }

    private static void StockRefreshFetchesDeepHourlyHistory()
    {
        var source = File.ReadAllText(SourcePath("scripts", "update_capital_etfs.py"));
        foreach (var required in new[]
        {
            "def fetch_hourly_prices(client, epic, max_points=1000):",
            "prices_path(epic, \"HOUR\", max_points=max_points",
            "\"hourlyPoints\": intraday_points(hourly_rows or [])",
        })
        {
            if (!source.Contains(required, StringComparison.Ordinal))
            {
                throw new Exception($"stock refresh should keep enough hourly history for 2H/4H/6H charts: missing {required}");
            }
        }
    }

    private static void CapComTerminalStartsWithoutDevExpressStockSharpRuntimeCrash()
    {
        var app = File.ReadAllText(SourcePath("desktop", "CAPETF.Desktop", "App.xaml"));
        if (!app.Contains("StartupUri=\"CapComTerminalWindow.xaml\"", StringComparison.Ordinal))
        {
            throw new Exception("cap.com Terminal must start in the native terminal window");
        }

        var project = File.ReadAllText(SourcePath("desktop", "CAPETF.Desktop", "CAPETF.Desktop.csproj"));
        if (!project.Contains("StockSharp.Xaml.Charting", StringComparison.Ordinal))
        {
            throw new Exception("cap.com Terminal must keep StockSharp documented as the rejected DevExpress-backed option");
        }

        var xamlPath = SourcePath("desktop", "CAPETF.Desktop", "CapComTerminalWindow.xaml");
        if (!File.Exists(xamlPath)) throw new Exception("cap.com Terminal window XAML must exist");
        var xaml = File.ReadAllText(xamlPath);
        foreach (var required in new[]
        {
            "Title=\"cap.com Terminal\"",
            "WindowState=\"Maximized\"",
            "x:Name=\"TerminalWebView\"",
            "x:Name=\"SyntheticFormulaText\"",
            "Click=\"BuildSynthetic_Click\"",
            "Click=\"BuyPreview_Click\"",
            "Click=\"SellPreview_Click\"",
            "x:Name=\"CandleTypeBox\"",
            "CandleType_SelectionChanged",
            "Click=\"FitChart_Click\"",
            "Click=\"ToggleTicket_Click\"",
            "AutomationProperties.Name=\"Seed symbol\"",
        })
        {
            if (!xaml.Contains(required, StringComparison.Ordinal))
            {
                throw new Exception($"cap.com Terminal XAML missing {required}");
            }
        }

        var sourcePath = SourcePath("desktop", "CAPETF.Desktop", "CapComTerminalWindow.xaml.cs");
        if (!File.Exists(sourcePath)) throw new Exception("cap.com Terminal code-behind must exist");
        var source = File.ReadAllText(sourcePath);
        foreach (var rejected in new[]
        {
            "using StockSharp.",
            "new StockSharp.Xaml.Charting.Chart",
            ".TimeFrame()",
            "CandleStates.Finished",
        })
        {
            if (source.Contains(rejected, StringComparison.Ordinal))
            {
                throw new Exception($"cap.com Terminal must not load DevExpress-backed StockSharp runtime path: {rejected}");
            }
        }

        foreach (var required in new[]
        {
            "RenderSyntheticChart",
            "SyntheticTerminalChartPayload.Build",
            "PreviewSyntheticOrder",
            "RefreshBasketMarketDetailsAsync",
            "GetMarketDetailsAsync",
            "ApplyMarketDetails",
            "InitializeChartHostAsync",
            "SendTerminalPayloadAsync",
            "window.renderTerminal",
            "window.updateTerminal",
            "window.clearTerminal",
            "ClearTerminalChartAsync",
            "window.fitTerminalChart",
            "Heikin Ashi",
        })
        {
            if (!source.Contains(required, StringComparison.Ordinal))
            {
                throw new Exception($"cap.com Terminal source missing {required}");
            }
        }
    }

    private static void CapComTerminalShowsActionableConnectionFailures()
    {
        var source = File.ReadAllText(SourcePath("desktop", "CAPETF.Desktop", "CapComTerminalWindow.xaml.cs"));
        foreach (var required in new[]
        {
            "SavedCredentialLabel",
            "Connection failed:",
            "Capital.com demo",
            "Capital.com live",
        })
        {
            if (!source.Contains(required, StringComparison.Ordinal))
            {
                throw new Exception($"cap.com Terminal connection diagnostics missing {required}");
            }
        }
    }

    private static void CapitalApiClientAllowsReconnectAfterRequests()
    {
        var handler = new StubCapitalHandler();
        using var client = new CapitalApiClient(handler);
        var credentials = new ApiCredentials
        {
            UseDemo = true,
            Identifier = "user@example.com",
            Password = "password",
            ApiKey = "key",
        };

        client.LoginAsync(credentials).GetAwaiter().GetResult();
        client.SearchMarketsAsync("PLTR").GetAwaiter().GetResult();
        client.LoginAsync(credentials).GetAwaiter().GetResult();

        if (handler.RequestUris.Count(uri => uri.EndsWith("/api/v1/session", StringComparison.OrdinalIgnoreCase)) != 2)
        {
            throw new Exception("Capital API client should allow reconnecting on the same instance after requests");
        }
    }

    private static void CapitalApiClientParsesMarketDetailsSnapshotAndDealingRules()
    {
        const string json =
            """
            {
              "instrument": {
                "epic": "AMD",
                "symbol": "AMD",
                "name": "Advanced Micro Devices",
                "type": "SHARES",
                "currency": "USD",
                "country": "United States",
                "region": "US",
                "sector": "Technology",
                "lotSize": 1
              },
              "dealingRules": {
                "minDealSize": { "unit": "POINTS", "value": 1 },
                "minSizeIncrement": { "unit": "POINTS", "value": 0.1 }
              },
              "snapshot": {
                "marketStatus": "TRADEABLE",
                "bid": 158.12,
                "offer": 158.18
              }
            }
            """;

        var details = CapitalApiClient.ParseMarketDetails(json);

        if (details is null) throw new Exception("market details should parse a valid Capital.com market response");
        if (details.Epic != "AMD") throw new Exception("market details should parse instrument epic");
        AssertNear(158.12m, details.Bid ?? 0m, "market details should parse current bid");
        AssertNear(158.18m, details.Offer ?? 0m, "market details should parse current ask");
        AssertNear(158.15m, details.Price ?? 0m, "market details should set midpoint price");
        AssertNear(1m, details.LotSize ?? 0m, "market details should parse lot size");
        AssertNear(1m, details.MinDealSize ?? 0m, "market details should parse min deal size");
        AssertNear(0.1m, details.MinSizeIncrement ?? 0m, "market details should parse min size increment");
        if (details.Status != "TRADEABLE") throw new Exception("market details should parse market status");
        AssertEqual("United States", details.Country, "market details should parse country");
        AssertEqual("US", details.Region, "market details should parse region");
        AssertEqual("Technology", details.Sector, "market details should parse sector");
    }

    private static void CapitalStreamingClientRejectsClosedSocketsAndWindowRecreates()
    {
        var socket = new FakeCapitalStreamingSocket(WebSocketState.Closed);
        var client = new CapitalStreamingClient(socket);
        var statuses = new List<string>();
        client.StatusChanged += (_, status) => statuses.Add(status);

        if (client.IsConnected) throw new Exception("closed streaming socket must not report connected");
        try
        {
            client.SubscribeQuotesAsync(new CapitalSession { Cst = "cst", SecurityToken = "token" }, ["A"]).GetAwaiter().GetResult();
            throw new Exception("subscribe on a closed socket must fail");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not open", StringComparison.OrdinalIgnoreCase))
        {
        }
        client.DisposeAsync().AsTask().GetAwaiter().GetResult();
        if (!statuses.Any(status => status.Contains("disconnected", StringComparison.OrdinalIgnoreCase)))
        {
            throw new Exception("closed streaming socket must report disconnected status");
        }

        var source = File.ReadAllText(SourcePath("desktop", "CAPETF.Desktop", "CapComTerminalWindow.xaml.cs"));
        foreach (var required in new[]
        {
            "_streaming is null || !_streaming.IsConnected",
            "await _streaming.DisposeAsync()",
            "new CapitalStreamingClient()",
            "streaming.Disconnected += Streaming_Disconnected",
            "ReconnectStreamingAsync",
            "SubscribeQuotesAsync",
            "SubscribeOhlcAsync",
        })
        {
            if (!source.Contains(required, StringComparison.Ordinal))
            {
                throw new Exception($"terminal streaming recreation/resubscription contract missing {required}");
            }
        }
    }

    private static void CapitalStreamingClientReportsRemoteClose()
    {
        var socket = new FakeCapitalStreamingSocket(WebSocketState.None, closeOnReceive: true);
        var client = new CapitalStreamingClient(socket);
        using var disconnected = new ManualResetEventSlim();
        var reason = "";
        client.Disconnected += (_, message) =>
        {
            reason = message;
            disconnected.Set();
        };

        client.ConnectAsync(new CapitalSession { Cst = "cst", SecurityToken = "token" }).GetAwaiter().GetResult();
        if (!disconnected.Wait(TimeSpan.FromSeconds(2))) throw new Exception("remote socket close must raise a disconnect event");
        if (!reason.Contains("closed", StringComparison.OrdinalIgnoreCase)) throw new Exception($"disconnect reason must identify socket close, got {reason}");
        if (client.IsConnected) throw new Exception("remote-close reader completion must clear connected state");
        client.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    private static void SyntheticTerminalHtmlExposesResizableRailAndPersistentDrawingTools()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "synthetic-terminal.html");
        if (!File.Exists(path)) throw new Exception("terminal chart HTML must be copied to output");
        var html = File.ReadAllText(path);
        var drawings = File.ReadAllText(SourcePath("desktop", "CAPETF.Desktop", "Assets", "synthetic-drawings.js"));

        foreach (var required in new[]
        {
            "id=\"component-splitter\"",
            "pointerdown",
            "pointermove",
            "pointerup",
            "capcom-terminal-rail-width",
            "localStorage",
            "grid-template-columns: minmax(0, 1fr) 6px var(--rail-width)",
            "id=\"drawing-tool-rail\"",
            "data-tool=\"cursor\"",
            "data-tool=\"trend\"",
            "data-tool=\"ray\"",
            "data-tool=\"hline\"",
            "data-tool=\"vline\"",
            "data-tool=\"fib\"",
            "data-tool=\"rectangle\"",
            "data-tool=\"brush\"",
            "data-tool=\"text\"",
            "data-tool=\"measure\"",
            "title=\"Cursor / select\"",
            "title=\"Fibonacci retracement\"",
            "title=\"Price/date percentage measure\"",
            "id=\"undo-drawing\"",
            "id=\"redo-drawing\"",
            "id=\"magnet-drawings\"",
            "id=\"lock-drawings\"",
            "id=\"visibility-drawings\"",
            "id=\"clear-drawings\"",
            "aria-pressed=\"false\"",
            "id=\"drawing-style-bar\"",
            "id=\"drawing-color\"",
            "id=\"drawing-width\"",
            "data-line-style=\"solid\"",
            "data-line-style=\"dashed\"",
            "data-line-style=\"dotted\"",
            "id=\"text-annotation-overlay\"",
            "maxlength=\"500\"",
            "window.confirm('Clear all drawings?')",
            "CapComDrawings.createManager",
            "drawingManager.setRecords",
            "drawingManager.getRecords",
            "drawingManager.setTool",
            "drawingManager.undo",
            "drawingManager.redo",
            "drawingManager.setMagnet",
            "drawingManager.setLocked",
            "drawingManager.setVisible",
            "drawingManager.setStyle",
            "drawingManager.updateContext",
            "onRecordsChanged",
            "onSelectionChanged",
            "onStateChanged",
            "synthetic-drawings.js",
            "Shared ${sharedCandleRange()}",
            "Bid ${money(bid)}  Ask ${money(ask)}",
        })
        {
            if (!html.Contains(required, StringComparison.Ordinal))
            {
                throw new Exception($"terminal chart HTML missing Task 6 workspace contract: {required}");
            }
        }

        if (!drawings.Contains("capcom-terminal-drawings:", StringComparison.Ordinal))
        {
            throw new Exception("drawing manager must retain the stable per-basket persistence key prefix");
        }

        foreach (var forbidden in new[]
        {
            "id=\"last-price\"",
            "title: 'Last'",
            "Last ${",
            ">X</button>",
            ">TL</button>",
            ">HL</button>",
            ">VL</button>",
            ">RAY</button>",
            ">RECT</button>",
            "class HorizontalLinePrimitive",
            "class VerticalLinePrimitive",
            "class TrendLinePrimitive",
            "class RayPrimitive",
            "class RectanglePrimitive",
            "function drawingPrimitive",
            "function attachDrawing",
            "function handleChartClick",
            "chart.subscribeClick(handleChartClick)",
        })
        {
            if (html.Contains(forbidden, StringComparison.Ordinal))
            {
                throw new Exception($"terminal chart HTML must not expose last-price metadata or price lines: {forbidden}");
            }
        }

        var chartScript = html.IndexOf("lightweight-charts.standalone.production.js", StringComparison.Ordinal);
        var drawingScript = html.IndexOf("synthetic-drawings.js", StringComparison.Ordinal);
        var initialization = html.IndexOf("const chartRoot", StringComparison.Ordinal);
        if (chartScript < 0 || drawingScript <= chartScript || initialization <= drawingScript)
        {
            throw new Exception("terminal must load the local drawing manager after Lightweight Charts and before initialization");
        }
    }

    private static void SyntheticTerminalHtmlDisablesNativeLastCloseDecorations()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "synthetic-terminal.html");
        if (!File.Exists(path)) throw new Exception("terminal chart HTML must be copied to output");
        var html = File.ReadAllText(path);
        var candleStart = html.IndexOf("chart.addSeries(LightweightCharts.CandlestickSeries", StringComparison.Ordinal);
        var candleEnd = html.IndexOf("maSeries = {", candleStart, StringComparison.Ordinal);
        if (candleStart < 0 || candleEnd < candleStart) throw new Exception("terminal chart HTML must define the candlestick series before moving-average series");
        var candleOptions = html[candleStart..candleEnd];

        foreach (var required in new[] { "priceLineVisible: false", "lastValueVisible: false" })
        {
            if (!candleOptions.Contains(required, StringComparison.Ordinal))
            {
                throw new Exception($"candlestick series must disable native last-close decoration: {required}");
            }
        }

        foreach (var forbidden in new[] { "priceLineVisible: true", "lastValueVisible: true" })
        {
            if (candleOptions.Contains(forbidden, StringComparison.Ordinal))
            {
                throw new Exception($"candlestick series must not enable native last-close decoration: {forbidden}");
            }
        }
    }

    private static void SyntheticTerminalHtmlResetsTransientDrawingStateBeforeRestore()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "synthetic-terminal.html");
        if (!File.Exists(path)) throw new Exception("terminal chart HTML must be copied to output");
        var html = File.ReadAllText(path);
        var restoreStart = html.IndexOf("function restoreDrawingsForIdentity", StringComparison.Ordinal);
        var restoreEnd = html.IndexOf("function renderChart", restoreStart, StringComparison.Ordinal);
        if (restoreStart < 0 || restoreEnd < restoreStart) throw new Exception("terminal chart HTML must retain a focused manager restore helper");
        var restore = html[restoreStart..restoreEnd];
        var identitySet = restore.IndexOf("drawingIdentity = identity || '';", StringComparison.Ordinal);
        var sanitizeLoad = restore.IndexOf("CapComDrawings.loadStoredRecords", StringComparison.Ordinal);
        var recordSet = restore.IndexOf("drawingManager.setRecords", StringComparison.Ordinal);

        if (identitySet < 0 || sanitizeLoad < identitySet || recordSet < sanitizeLoad)
        {
            throw new Exception("drawing identity restore must set identity, sanitize stored records, then update the manager");
        }
    }

    private static void SyntheticTerminalHtmlCoalescesIncrementalTicksAndUsesStableDrawingIdentity()
    {
        var html = File.ReadAllText(SourcePath("desktop", "CAPETF.Desktop", "Assets", "synthetic-terminal.html"));
        foreach (var required in new[]
        {
            "requestAnimationFrame(flushTerminalTick)",
            "candleSeries.update",
            "maSeries[period].update",
            "DrawingIdentity",
            "QuoteStatus",
            "QuoteTimestamp",
            "toPrecision(6)",
        })
        {
            if (!html.Contains(required, StringComparison.Ordinal))
            {
                throw new Exception($"terminal incremental tick/drawing contract missing {required}");
            }
        }

        if (html.Contains("LastTickText", StringComparison.Ordinal))
        {
            throw new Exception("terminal HTML must show explicit quote freshness instead of a last/tick display field");
        }

        var tickStart = html.IndexOf("function flushTerminalTick", StringComparison.Ordinal);
        var tickEnd = html.IndexOf("window.setTerminalBusy", tickStart, StringComparison.Ordinal);
        if (tickStart < 0 || tickEnd < tickStart) throw new Exception("terminal incremental tick handler must remain a focused block");
        var tickBlock = html[tickStart..tickEnd];
        if (tickBlock.Contains("setData", StringComparison.Ordinal))
        {
            throw new Exception("terminal live tick handler must not rebuild full chart series with setData");
        }
    }

    private static void CapComTerminalUsesTask6PayloadBridge()
    {
        var source = File.ReadAllText(SourcePath("desktop", "CAPETF.Desktop", "CapComTerminalWindow.xaml.cs"));
        foreach (var required in new[]
        {
            "window.setTerminalBusy",
            "window.setTerminalData",
            "window.updateTerminalTick",
        })
        {
            if (!source.Contains(required, StringComparison.Ordinal))
            {
                throw new Exception($"terminal host must use the Task 6 payload bridge: {required}");
            }
        }
    }

    private static void StockChunkLoaderPrefersLegacyWhenChunksAreSmallerThanLegacy()
    {
        var chosen = DashboardStockChunkLoader.SelectBestStockDataFiles(
            [
                new StockDataFileCandidate("stocks-000.enc.json", 326_940),
                new StockDataFileCandidate("stocks-001.enc.json", 328_285),
            ],
            new StockDataFileCandidate("stocks.enc.json", 38_406_649));

        if (chosen.Count != 1 || chosen[0].Path != "stocks.enc.json")
        {
            throw new Exception("loader should prefer the full legacy stock file when refreshed chunks are clearly partial");
        }
    }

    private static void CapComTerminalLoadsFullEncryptedStockChunks()
    {
        var loaderPath = SourcePath("desktop", "CAPETF.Desktop", "DashboardStockChunkLoader.cs");
        if (!File.Exists(loaderPath)) throw new Exception("cap.com Terminal must include a stock chunk loader");
        var loader = File.ReadAllText(loaderPath);
        foreach (var required in new[]
        {
            "stocks-*.enc.json",
            "stocks.enc.json",
            "Rfc2898DeriveBytes.Pbkdf2",
            "AesGcm",
            "LoadStocks",
            "OhlcByEpic",
            "weeklyPoints",
            "dailyPoints",
            "hourlyPoints",
            "BuildSyntheticCandles",
        })
        {
            if (!loader.Contains(required, StringComparison.Ordinal))
            {
                throw new Exception($"stock chunk loader missing {required}");
            }
        }

        var project = File.ReadAllText(SourcePath("desktop", "CAPETF.Desktop", "CAPETF.Desktop.csproj"));
        if (!project.Contains("..\\..\\data\\stocks-*.enc.json", StringComparison.Ordinal) ||
            !project.Contains("..\\..\\data\\stocks.enc.json", StringComparison.Ordinal) ||
            !project.Contains("Link=\"data\\%(Filename)%(Extension)\"", StringComparison.Ordinal) ||
            !project.Contains("<CopyToPublishDirectory>Always</CopyToPublishDirectory>", StringComparison.Ordinal))
        {
            throw new Exception("desktop package must include encrypted stock chunks");
        }

        var terminal = File.ReadAllText(SourcePath("desktop", "CAPETF.Desktop", "CapComTerminalWindow.xaml.cs"));
        foreach (var required in new[]
        {
            "DashboardStockChunkLoader.LoadStocks",
            "_cachedCandlesByEpic",
            "cached stock chunks",
            "LoadStocksFromApiAsync",
            "candidateLimit: 500",
            "maxSelection: 36",
            "Task.Run(() => SyntheticTerminalSelector.SelectBest",
            "CapitalStreamingClient",
            "SubscribeQuotesAsync",
            "SyntheticTerminalLiveUpdate.Apply",
            "RebuildSeedOptions",
            "SeedText",
        })
        {
            if (!terminal.Contains(required, StringComparison.Ordinal))
            {
                throw new Exception($"cap.com Terminal full stock loading missing {required}");
            }
        }
    }

    private static void TerminalWorkspaceModeNameIsAvailable()
    {
        if (SyntheticTerminalWorkspace.ModeName != "Terminal")
        {
            throw new Exception("terminal workspace mode must be named Terminal");
        }
    }

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

    private static void SyntheticStrategiesRankExpectedSetups()
    {
        var day = DateTimeOffset.Parse("2022-01-01T00:00:00Z");
        var instruments = new[]
        {
            CreateSeedStock("DIP", "Dip Inside Uptrend"),
            CreateSeedStock("BELOW200", "Below 200 MA"),
            CreateSeedStock("LOW2Y", "Below Two Year Low"),
            CreateSeedStock("ATH", "Above All Time High"),
            CreateSeedStock("BREAK", "Breakout Candidate"),
        };
        var candles = new Dictionary<string, IReadOnlyList<OhlcPoint>>
        {
            ["DIP"] = CreateDipInsideUptrendCandles(day),
            ["BELOW200"] = CreateBelowMaCandles(day),
            ["LOW2Y"] = CreateBelowTwoYearLowCandles(day),
            ["ATH"] = CreateAllTimeHighCandles(day),
            ["BREAK"] = CreateBreakoutCandles(day),
        };

        AssertStrategyTop(SyntheticStrategyKind.DipInsideUptrend, "DIP", instruments, candles);
        AssertStrategyTop(SyntheticStrategyKind.BelowMa200, "BELOW200", instruments, candles);
        AssertStrategyTop(SyntheticStrategyKind.BelowTwoYearLow, "LOW2Y", instruments, candles);
        AssertStrategyTop(SyntheticStrategyKind.AboveAllTimeHigh, "ATH", instruments, candles);
        AssertStrategyTop(SyntheticStrategyKind.BreakoutCandidate, "BREAK", instruments, candles);
    }

    private static void SyntheticStrategiesExposeBuildOptions()
    {
        var kinds = SyntheticStrategyCatalog.All.Select(strategy => strategy.Kind).ToHashSet();
        foreach (var required in new[]
        {
            SyntheticStrategyKind.SimilarToSelectedSymbol,
            SyntheticStrategyKind.BelowMa200,
            SyntheticStrategyKind.BelowTwoYearLow,
            SyntheticStrategyKind.NearTwoYearLow,
            SyntheticStrategyKind.AboveAllTimeHigh,
            SyntheticStrategyKind.BreakoutCandidate,
            SyntheticStrategyKind.DipInsideUptrend,
            SyntheticStrategyKind.HighMomentum,
            SyntheticStrategyKind.MeanReversion,
        })
        {
            if (!kinds.Contains(required)) throw new Exception($"strategy catalog missing {required}");
        }
    }

    private static void SyntheticStrategiesReturnClosestFallbackCandidates()
    {
        var day = DateTimeOffset.Parse("2022-01-01T00:00:00Z");
        var maInstruments = new[]
        {
            CreateSeedStock("NEAR-MA", "Near MA"),
            CreateSeedStock("FAR-MA", "Far MA"),
            CreateSeedStock("MID-MA", "Mid MA"),
        };
        var lowInstruments = new[]
        {
            CreateSeedStock("NEAR-LOW", "Near Low"),
            CreateSeedStock("FAR-LOW", "Far Low"),
            CreateSeedStock("MID-LOW", "Mid Low"),
        };
        var candles = new Dictionary<string, IReadOnlyList<OhlcPoint>>
        {
            ["NEAR-MA"] = CreateAboveMaCandles(day, finalOffsetPct: 1m),
            ["FAR-MA"] = CreateAboveMaCandles(day, finalOffsetPct: 20m),
            ["MID-MA"] = CreateAboveMaCandles(day, finalOffsetPct: 8m),
            ["NEAR-LOW"] = CreateNearTwoYearLowCandles(day, distancePct: 2m),
            ["FAR-LOW"] = CreateNearTwoYearLowCandles(day, distancePct: 25m),
            ["MID-LOW"] = CreateNearTwoYearLowCandles(day, distancePct: 10m),
        };

        var belowMa = SyntheticStrategyRanker.Rank(SyntheticStrategyKind.BelowMa200, maInstruments, candles, periodsPerYear: 52, maximum: 4);
        var belowLow = SyntheticStrategyRanker.Rank(SyntheticStrategyKind.BelowTwoYearLow, lowInstruments, candles, periodsPerYear: 52, maximum: 4);

        if (belowMa.Count < 3) throw new Exception("below-MA strategy should return closest fallback candidates when strict matches are scarce");
        if (belowMa[0].Instrument.Epic != "NEAR-MA") throw new Exception("below-MA fallback should prefer the closest candidate");
        if (belowLow.Count < 3) throw new Exception("below-2Y-low strategy should return closest fallback candidates when strict matches are scarce");
        if (belowLow[0].Instrument.Epic != "NEAR-LOW") throw new Exception("below-2Y-low fallback should prefer the closest candidate");
    }

    private static void SyntheticQuoteUsesFormulaMultipliersForBidAsk()
    {
        var basket = new SyntheticBasket { Symbol = "SYN-QUOTE" };
        basket.Components.Add(new SyntheticComponent(
            new MarketInstrument { Epic = "QA", Bid = 99m, Offer = 101m, Price = 100m },
            50m,
            0m,
            0m)
        {
            FormulaMultiplier = 0.5m,
        });
        basket.Components.Add(new SyntheticComponent(
            new MarketInstrument { Epic = "QB", Bid = 198m, Offer = 202m, Price = 200m },
            50m,
            0m,
            0m)
        {
            FormulaMultiplier = 0.25m,
        });

        SyntheticQuoteCalculator.Refresh(basket);
        AssertNear(99m, basket.BidPrice ?? 0m, "synthetic bid should use formula multipliers");
        AssertNear(101m, basket.AskPrice ?? 0m, "synthetic ask should use formula multipliers");

        var payload = SyntheticTerminalChartPayload.Build(basket);
        AssertNear(99m, payload.BidPrice ?? 0m, "terminal payload should expose synthetic bid");
        AssertNear(101m, payload.AskPrice ?? 0m, "terminal payload should expose synthetic ask");

        SyntheticTerminalLiveUpdate.Apply(basket, new QuoteUpdate("QA", 109m, 111m, 110m, DateTimeOffset.UtcNow));
        AssertNear(104m, basket.BidPrice ?? 0m, "live quote should recalculate synthetic bid");
        AssertNear(106m, basket.AskPrice ?? 0m, "live quote should recalculate synthetic ask");
    }

    private static void SyntheticQuoteTreatsMissingOrZeroSidesAsUnavailable()
    {
        var basket = new SyntheticBasket { Symbol = "SYN-ZERO" };
        basket.Components.Add(new SyntheticComponent(
            new MarketInstrument { Epic = "ZA", Bid = 99m, Offer = 101m, Price = 100m },
            50m,
            0m,
            0m)
        {
            FormulaMultiplier = 0.5m,
        });
        basket.Components.Add(new SyntheticComponent(
            new MarketInstrument { Epic = "ZB", Bid = 198m, Offer = 202m, Price = 200m },
            50m,
            0m,
            0m)
        {
            FormulaMultiplier = 0.25m,
        });

        basket.Components[0].Instrument.Bid = null;
        SyntheticQuoteCalculator.Refresh(basket);
        if (basket.BidPrice is not null) throw new Exception("a missing component bid must make the synthetic bid unavailable");
        AssertNear(101m, basket.AskPrice ?? 0m, "a missing bid must not zero a valid synthetic ask");

        basket.Components[0].Instrument.Bid = 99m;
        basket.Components[0].Instrument.Offer = 0m;
        SyntheticQuoteCalculator.Refresh(basket);
        AssertNear(99m, basket.BidPrice ?? 0m, "a zero offer must not zero a valid synthetic bid");
        if (basket.AskPrice is not null) throw new Exception("a zero component offer must make the synthetic ask unavailable");

        var zeroBid = new SyntheticBasket { Symbol = "SYN-ZERO-BID" };
        zeroBid.Components.Add(new SyntheticComponent(new MarketInstrument { Epic = "ZX", Bid = 0m, Offer = 100m }, 100m, 0m, 0m)
        {
            FormulaMultiplier = 1m,
        });
        SyntheticQuoteCalculator.Refresh(zeroBid);
        if (zeroBid.BidPrice is not null)
        {
            throw new Exception("a zero bid must display as unavailable, not as a tradable synthetic price");
        }
    }

    private static void StreamingQuoteClearsMissingAndZeroSides()
    {
        var basket = new SyntheticBasket { Symbol = "SYN-STREAM" };
        basket.Components.Add(new SyntheticComponent(
            new MarketInstrument { Epic = "STREAM-A", Bid = 99m, Offer = 101m, Price = 100m },
            50m,
            0m,
            0m)
        {
            FormulaMultiplier = 0.5m,
        });
        basket.Components.Add(new SyntheticComponent(
            new MarketInstrument { Epic = "STREAM-B", Bid = 198m, Offer = 202m, Price = 200m },
            50m,
            0m,
            0m)
        {
            FormulaMultiplier = 0.25m,
        });
        SyntheticQuoteCalculator.Refresh(basket);

        var result = SyntheticLiveUpdate.ApplyQuote(
            basket,
            new QuoteUpdate("STREAM-A", 0m, null, 100m, DateTimeOffset.UtcNow));

        if (!result.Matched) throw new Exception("matching streaming quote must update the synthetic component");
        if (basket.Components[0].Instrument.Bid is not null || basket.Components[0].Instrument.Offer is not null)
        {
            throw new Exception("zero or missing streaming quote sides must clear stale component quote state");
        }
        if (basket.BidPrice is not null || basket.AskPrice is not null)
        {
            throw new Exception("zero or missing streaming quote sides must make synthetic bid and ask unavailable");
        }
    }

    private static void StreamingQuoteClearsUnavailableSidesWithoutUsablePrice()
    {
        var time = DateTimeOffset.Parse("2026-07-26T12:00:00Z");
        var basket = new SyntheticBasket { Symbol = "SYN-INVALID-STREAM", BasketPrice = 100m, LastUpdated = time };
        basket.Components.Add(new SyntheticComponent(
            new MarketInstrument { Epic = "INVALID-STREAM", Bid = 99m, Offer = 101m, Price = 100m },
            100m,
            0m,
            0m)
        {
            FormulaMultiplier = 1m,
            SyntheticBaselinePrice = 100m,
        });
        basket.Candles.Add(new OhlcPoint(time, 100m, 100m, 100m, 100m));
        SyntheticQuoteCalculator.Refresh(basket);

        var result = SyntheticLiveUpdate.ApplyQuote(
            basket,
            new QuoteUpdate("INVALID-STREAM", 0m, 0m, 0m, time.AddSeconds(1)));

        if (!result.Matched) throw new Exception("a matching invalid-price tick must still refresh quote availability");
        if (result.CandleChanged) throw new Exception("an invalid derived price must not change the live candle");
        if (basket.BidPrice is not null || basket.AskPrice is not null)
        {
            throw new Exception("an invalid-price tick with zero quote sides must make synthetic bid and ask unavailable");
        }
        AssertNear(100m, basket.Candles[^1].Close, "an invalid derived price must retain the existing candle close");
    }

    private static void SyntheticOrderSizingUsesCapitalDealRules()
    {
        var instrument = new MarketInstrument
        {
            Epic = "AMD",
            MinDealSize = 1m,
            MinSizeIncrement = 0.1m,
        };
        var component = new SyntheticComponent(instrument, 33.3333m, 0m, 0m)
        {
            FormulaMultiplier = 0.06383855m,
        };

        AssertNear(0.06383855m, SyntheticOrderSizing.DisplayMultiplier(component), "formula display payload should preserve its multiplier");
        AssertNear(1m, SyntheticOrderSizing.ExecutableLegQuantity(component, 1m), "executable size must respect Capital.com min deal size");
        AssertNear(1.3m, SyntheticOrderSizing.ExecutableLegQuantity(component, 20m), "executable size must round up to Capital.com min size increment");

        var freeSized = new SyntheticComponent(new MarketInstrument { Epic = "BB" }, 33.3333m, 0m, 0m)
        {
            FormulaMultiplier = 3.93081368m,
        };
        AssertNear(3.93081368m, SyntheticOrderSizing.DisplayMultiplier(freeSized), "formula display payload should remain separate from deal rules");
    }

    private static void AdaptiveDisplayMultiplierPreservesSmallNonzeroValues()
    {
        var component = new SyntheticComponent(new MarketInstrument { Epic = "SMALL" }, 25m, 0m, 0m)
        {
            FormulaMultiplier = 0.000012345678m,
        };

        AssertNear(component.FormulaMultiplier, SyntheticOrderSizing.DisplayMultiplier(component),
            "display payload must preserve the chart multiplier", 0.0000000001m);
        var formatted = SyntheticOrderSizing.FormatDisplayMultiplier(component);
        AssertEqual("1.2345678E-05", formatted, "adaptive multiplier formatting must preserve small nonzero values");
    }

    private static void ExecutablePreviewUsesCurrentEqualNotionalAndDealRules()
    {
        var component = new SyntheticComponent(
            new MarketInstrument { Epic = "A", Bid = 49m, Offer = 51m, MinDealSize = 0.1m, MinSizeIncrement = 0.1m },
            100m / 3m,
            0m,
            0m)
        {
            FormulaMultiplier = 9.99m,
        };

        var preview = SyntheticOrderSizing.ExecutableLegPreview(component, 300m, 50m);

        AssertNear(2m, preview.Quantity, "one-third of 300 at price 50 is quantity 2");
        AssertNear(100m, preview.Notional, "preview notional must use executable quantity");
        AssertNear(100m / 3m, preview.WeightPct, "preview weight must reflect the executable notional");
    }

    private static void ExecutablePreviewRoundsUpToCapitalDealMinimumAndIncrement()
    {
        var minimumComponent = new SyntheticComponent(
            new MarketInstrument { Epic = "MIN", MinDealSize = 1m, MinSizeIncrement = 0.1m },
            10m,
            0m,
            0m);
        var minimumPreview = SyntheticOrderSizing.ExecutableLegPreview(minimumComponent, 300m, 50m);
        AssertNear(1m, minimumPreview.Quantity, "preview quantity must round up to Capital.com minimum deal size");
        AssertNear(50m, minimumPreview.Notional, "minimum quantity must determine preview notional");

        var incrementComponent = new SyntheticComponent(
            new MarketInstrument { Epic = "INC", MinDealSize = 0.1m, MinSizeIncrement = 0.25m },
            34m,
            0m,
            0m);
        var incrementPreview = SyntheticOrderSizing.ExecutableLegPreview(incrementComponent, 300m, 50m);
        AssertNear(2.25m, incrementPreview.Quantity, "preview quantity must round upward to Capital.com size increment");
        AssertNear(112.5m, incrementPreview.Notional, "increment-rounded quantity must determine preview notional");
    }

    private static void ExecutableOrderPreviewUsesCurrentSideQuotesAndReportsImbalance()
    {
        var basket = new SyntheticBasket { Symbol = "SYN-ORDER" };
        basket.Components.Add(new SyntheticComponent(
            new MarketInstrument { Epic = "A", Bid = 49m, Offer = 51m, MinDealSize = 1m, MinSizeIncrement = 1m },
            50m, 0m, 0m));
        basket.Components.Add(new SyntheticComponent(
            new MarketInstrument { Epic = "B", Bid = 24m, Offer = 26m, MinDealSize = 1m, MinSizeIncrement = 1m },
            30m, 0m, 0m));
        basket.Components.Add(new SyntheticComponent(
            new MarketInstrument { Epic = "C", Bid = 9.8m, Offer = 10.2m, MinDealSize = 0.1m, MinSizeIncrement = 0.1m },
            20m, 0m, 0m));

        var preview = SyntheticOrderSizing.BuildExecutableOrderPreview(basket, "BUY", 300m);

        AssertEqual("BUY", preview.Side, "order preview side");
        AssertNear(300m, preview.RequestedBasketNotional, "requested basket notional");
        AssertNear(317.18m, preview.TotalExecutableNotional, "total executable notional must use rounded leg quantities", 0.001m);
        AssertNear(51m, preview.Legs[0].ReferencePrice, "buy preview must use the current offer");
        AssertNear(3m, preview.Legs[0].Quantity, "first leg must round up to its deal increment");
        AssertNear(153m, preview.Legs[0].Notional, "first leg notional must use the executable quantity");
        if (preview.MaxAbsoluteWeightImbalancePct <= 1m)
        {
            throw new Exception("order preview must report weight imbalance introduced by dealing-rule rounding");
        }
    }

    private static void SavedSyntheticBasketStorePersistsFormulaDetails()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"capetf-saved-{Guid.NewGuid():N}");
        try
        {
            var store = new SavedSyntheticBasketStore(folder);
            var basket = new SyntheticBasket
            {
                Symbol = "SYN-SAP-DIP-01",
                Block = "Europe / EUR / All",
                AverageVolatilityPct = 21.5m,
                SimilarityScore = 88.2m,
                BasketPrice = 100m,
                LastUpdated = DateTimeOffset.Parse("2026-07-26T10:00:00Z"),
            };
            basket.Components.Add(new SyntheticComponent(new MarketInstrument { Epic = "SAPD", Name = "SAP", Currency = "EUR" }, 33.3333m, 20m, 10m)
            {
                FormulaMultiplier = 0.2625m,
                FormulaReferencePrice = 127m,
                SyntheticBaselinePrice = 127m,
            });
            basket.Components.Add(new SyntheticComponent(new MarketInstrument { Epic = "CTS", Name = "CTS Eventim", Currency = "EUR" }, 33.3333m, 19m, 9m)
            {
                FormulaMultiplier = 0.5952m,
                FormulaReferencePrice = 56m,
                SyntheticBaselinePrice = 56m,
            });

            store.Save(SavedSyntheticBasket.FromBasket("My SAP basket", SyntheticStrategyKind.DipInsideUptrend, basket));
            var saved = store.LoadAll().Single();

            if (saved.Name != "My SAP basket") throw new Exception("saved basket should preserve user name");
            if (saved.Strategy != SyntheticStrategyKind.DipInsideUptrend) throw new Exception("saved basket should preserve strategy");
            if (saved.Components.Count != 2) throw new Exception("saved basket should preserve component count");
            AssertNear(0.2625m, saved.Components[0].FormulaMultiplier, "saved basket should preserve formula multiplier");
            AssertNear(127m, saved.Components[0].ReferencePrice ?? 0m, "saved basket should preserve reference price");
        }
        finally
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
        }
    }

    private static void AssertStrategyTop(
        SyntheticStrategyKind strategy,
        string expectedEpic,
        IReadOnlyList<MarketInstrument> instruments,
        IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>> candles)
    {
        var top = SyntheticStrategyRanker.Rank(strategy, instruments, candles, periodsPerYear: 52, maximum: 5).FirstOrDefault();
        if (top?.Instrument.Epic != expectedEpic)
        {
            throw new Exception($"{strategy} should rank {expectedEpic} first, got {top?.Instrument.Epic ?? "none"}");
        }
    }

    private static SyntheticBasket CreateLiveBasket(string symbol, string epic, decimal close, DateTimeOffset? time = null)
    {
        var candleTime = time ?? DateTimeOffset.UtcNow;
        var basket = new SyntheticBasket { Symbol = symbol, BasketPrice = close, LastUpdated = candleTime };
        basket.Components.Add(new SyntheticComponent(new MarketInstrument { Epic = epic, Price = close }, 100m, 0m, 0m)
        {
            SyntheticBaselinePrice = close,
        });
        basket.Candles.Add(new OhlcPoint(candleTime, close, close, close, close));
        return basket;
    }

    private static IReadOnlyList<OhlcPoint> CreateCandles(DateTimeOffset day, decimal open, decimal high, decimal low, decimal close) =>
        Enumerable.Range(0, 120)
            .Select(index =>
            {
                var scale = 1m + index * 0.001m;
                return new OhlcPoint(day.AddDays(index), open * scale, high * scale, low * scale, close * scale);
            })
            .ToList();

    private static IReadOnlyList<OhlcPoint> CreateIntradayCandles(DateTimeOffset start, decimal finalClose, int count, TimeSpan step)
    {
        return Enumerable.Range(0, count)
            .Select(index =>
            {
                var scale = 0.75m + 0.25m * index / Math.Max(1, count - 1);
                var close = finalClose * scale;
                return new OhlcPoint(start.AddTicks(step.Ticks * index), close * 0.99m, close * 1.01m, close * 0.98m, close);
            })
            .ToList();
    }

    private static IReadOnlyList<OhlcPoint> HourlyCandles(DateTimeOffset start, int count) =>
        Enumerable.Range(0, count)
            .Select(index =>
            {
                var value = 100m + index;
                return new OhlcPoint(start.AddHours(index), value, value + 2m, value - 1m, value + 1m);
            })
            .ToList();

    private static IReadOnlyList<OhlcPoint> CreateVariableCandles(DateTimeOffset day, decimal close)
    {
        var returns = new[] { 0.01m, -0.02m, 0.03m, -0.01m };
        var price = close;
        return Enumerable.Range(0, 120)
            .Select(index =>
            {
                if (index > 0) price *= 1m + returns[(index - 1) % returns.Length];
                return new OhlcPoint(day.AddDays(index), price, price, price, price);
            })
            .ToList();
    }

    private static IReadOnlyList<OhlcPoint> CreatePricedTrendCandles(DateTimeOffset day, decimal finalClose)
    {
        const int count = 120;
        return Enumerable.Range(0, count)
            .Select(index =>
            {
                var scale = 0.7m + 0.3m * index / (count - 1);
                var close = finalClose * scale;
                return new OhlcPoint(day.AddDays(index), close * 0.99m, close * 1.01m, close * 0.98m, close);
            })
            .ToList();
    }

    private static IReadOnlyList<OhlcPoint> CreateBelowMaCandles(DateTimeOffset day) =>
        Enumerable.Range(0, 260)
            .Select(index =>
            {
                var close = index < 230 ? 120m + index * 0.03m : 80m - (index - 230) * 0.2m;
                return FlatCandle(day.AddDays(index), close);
            })
            .ToList();

    private static IReadOnlyList<OhlcPoint> CreateAboveMaCandles(DateTimeOffset day, decimal finalOffsetPct) =>
        Enumerable.Range(0, 260)
            .Select(index =>
            {
                var close = 100m;
                if (index == 259) close = 100m * (1m + finalOffsetPct / 100m);
                return FlatCandle(day.AddDays(index), close);
            })
            .ToList();

    private static IReadOnlyList<OhlcPoint> CreateBelowTwoYearLowCandles(DateTimeOffset day) =>
        Enumerable.Range(0, 260)
            .Select(index =>
            {
                var close = index == 259 ? 70m : 95m + (index % 30) * 0.2m;
                return FlatCandle(day.AddDays(index), close);
            })
            .ToList();

    private static IReadOnlyList<OhlcPoint> CreateNearTwoYearLowCandles(DateTimeOffset day, decimal distancePct) =>
        Enumerable.Range(0, 260)
            .Select(index =>
            {
                var close = index == 259 ? 100m * (1m + distancePct / 100m) : 100m + (index % 20) * 0.5m;
                return FlatCandle(day.AddDays(index), close);
            })
            .ToList();

    private static IReadOnlyList<OhlcPoint> CreateAllTimeHighCandles(DateTimeOffset day) =>
        Enumerable.Range(0, 260)
            .Select(index =>
            {
                var close = 70m + index * 0.35m;
                if (index == 259) close += 8m;
                return FlatCandle(day.AddDays(index), close);
            })
            .ToList();

    private static IReadOnlyList<OhlcPoint> CreateBreakoutCandles(DateTimeOffset day) =>
        Enumerable.Range(0, 260)
            .Select(index =>
            {
                var close = index < 220 ? 90m + index * 0.08m : 107m + (index - 220) * 0.08m;
                if (index == 259) close = 109.9m;
                return FlatCandle(day.AddDays(index), close);
            })
            .ToList();

    private static IReadOnlyList<OhlcPoint> CreateDipInsideUptrendCandles(DateTimeOffset day) =>
        Enumerable.Range(0, 260)
            .Select(index =>
            {
                var close = 80m + index * 0.22m;
                if (index > 232) close -= (index - 232) * 1.2m;
                return FlatCandle(day.AddDays(index), close);
            })
            .ToList();

    private static OhlcPoint FlatCandle(DateTimeOffset time, decimal close) =>
        new(time, close, close, close, close);

    private static IReadOnlyList<OhlcPoint> CreateReturnCandles(DateTimeOffset day, IReadOnlyList<decimal> returns)
    {
        var price = 100m;
        return Enumerable.Range(0, 120)
            .Select(index =>
            {
                if (index > 0) price *= 1m + returns[(index - 1) % returns.Count];
                return new OhlcPoint(day.AddDays(index), price, price, price, price);
            })
            .ToList();
    }

    private static IReadOnlyList<OhlcPoint> CreateLongReturnCandles(DateTimeOffset day, IReadOnlyList<decimal> returns, int count)
    {
        var price = 100m;
        return Enumerable.Range(0, count)
            .Select(index =>
            {
                if (index > 0) price *= 1m + returns[(index - 1) % returns.Count];
                return new OhlcPoint(day.AddDays(index * 7), price, price, price, price);
            })
            .ToList();
    }

    private static MarketInstrument CreateStock(string epic, string name) => new()
    {
        Epic = epic,
        Name = name,
        Type = "SHARES",
        Currency = "USD",
        Region = "US",
        Sector = "Tech",
    };

    private static MarketInstrument CreateSeedStock(string epic, string name) => new()
    {
        Epic = epic,
        Name = name,
        Symbol = epic,
        Type = "SHARES",
        Currency = "USD",
        Region = "US",
        Sector = "All",
    };

    private static void AssertNear(decimal expected, decimal actual, string message, decimal tolerance = 0.0001m)
    {
        if (Math.Abs(expected - actual) > tolerance) throw new Exception($"{message}. Expected {expected}, got {actual}");
    }

    private static void AssertTrue(bool value, string message)
    {
        if (!value) throw new Exception(message);
    }

    private static void AssertFalse(bool value, string message)
    {
        if (value) throw new Exception(message);
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new Exception($"{message}. Expected {expected}, got {actual}");
        }
    }

    private static void SavedSyntheticBasketStoreDeletesSelectedBasket()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"capetf-saved-delete-{Guid.NewGuid():N}");
        try
        {
            var store = new SavedSyntheticBasketStore(folder);
            var deleted = new SavedSyntheticBasket(
                "basket-to-delete",
                "Delete me",
                "SYN-DELETE-01",
                "US / USD / All",
                SyntheticStrategyKind.SimilarToSelectedSymbol,
                DateTimeOffset.Parse("2026-07-27T10:00:00Z"),
                DateTimeOffset.Parse("2026-07-27T10:00:00Z"),
                []);
            var retained = new SavedSyntheticBasket(
                "basket-to-keep",
                "Keep me",
                "SYN-KEEP-01",
                "US / USD / All",
                SyntheticStrategyKind.SimilarToSelectedSymbol,
                DateTimeOffset.Parse("2026-07-27T10:00:00Z"),
                DateTimeOffset.Parse("2026-07-27T10:00:00Z"),
                []);

            store.Save(deleted);
            store.Save(retained);

            if (!store.Delete("BASKET-TO-DELETE")) throw new Exception("deleting a saved basket by ID should return true");
            var remaining = store.LoadAll();
            if (remaining.Count != 1 || remaining[0].Id != retained.Id)
            {
                throw new Exception("deleting one saved basket should leave only the other basket");
            }

            if (store.Delete("unknown-basket")) throw new Exception("deleting an unknown saved basket should return false");
        }
        finally
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
        }
    }

    private static void SavedBasketDeletionCoordinatorTracksSelectionAndPreservesDisplayedModels()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"capetf-saved-delete-coordinator-{Guid.NewGuid():N}");
        try
        {
            var store = new SavedSyntheticBasketStore(folder);
            var deleted = new SavedSyntheticBasket(
                "basket-to-delete",
                "Delete me",
                "SYN-DELETE-01",
                "US / USD / All",
                SyntheticStrategyKind.SimilarToSelectedSymbol,
                DateTimeOffset.Parse("2026-07-27T10:00:00Z"),
                DateTimeOffset.Parse("2026-07-27T10:00:00Z"),
                []);
            var retained = new SavedSyntheticBasket(
                "basket-to-keep",
                "Keep me",
                "SYN-KEEP-01",
                "US / USD / All",
                SyntheticStrategyKind.SimilarToSelectedSymbol,
                DateTimeOffset.Parse("2026-07-27T10:00:00Z"),
                DateTimeOffset.Parse("2026-07-27T10:00:00Z"),
                []);
            store.Save(deleted);
            store.Save(retained);

            var coordinator = new SavedBasketDeletionCoordinator(store);
            AssertFalse(coordinator.IsDeleteEnabled(null), "deletion must remain disabled without a saved-basket selection");
            AssertTrue(coordinator.IsDeleteEnabled(deleted), "deletion must become enabled when a saved basket is selected");

            var currentBasket = new SyntheticBasket { Symbol = "SYN-CURRENT-01", Block = "US / USD / All" };
            var chartPayload = SyntheticTerminalChartPayload.Build(currentBasket);
            var result = coordinator.DeleteConfirmed(deleted, currentBasket, chartPayload);

            AssertTrue(result.Deleted, "confirmed deletion must remove the selected saved basket");
            AssertEqual(1, result.SavedBaskets.Count, "confirmed deletion must refresh saved state");
            AssertEqual(retained.Id, result.SavedBaskets[0].Id, "refreshed saved state must retain the other basket");
            if (!ReferenceEquals(currentBasket, result.CurrentBasket)) throw new Exception("confirmed deletion must preserve the active basket model");
            if (!ReferenceEquals(chartPayload, result.ChartPayload)) throw new Exception("confirmed deletion must preserve the displayed chart model");
        }
        finally
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
        }
    }

    private static void SavedBasketDeletionUiContractIsPresent()
    {
        var xaml = File.ReadAllText(SourcePath("desktop", "CAPETF.Desktop", "CapComTerminalWindow.xaml"));
        foreach (var required in new[]
        {
            "x:Name=\"DeleteBasketButton\"",
            "Content=\"Delete\"",
            "ToolTip=\"Delete the selected saved basket.\"",
            "Click=\"DeleteBasket_Click\"",
            "IsEnabled=\"False\"",
        })
        {
            if (!xaml.Contains(required, StringComparison.Ordinal))
            {
                throw new Exception($"saved basket deletion XAML missing {required}");
            }
        }

        var source = File.ReadAllText(SourcePath("desktop", "CAPETF.Desktop", "CapComTerminalWindow.xaml.cs"));
        foreach (var required in new[]
        {
            "DeleteBasket_Click",
            "MessageBox.Show($\"Delete saved basket {saved.Name}?\"",
            "_savedBasketDeletion.DeleteConfirmed(saved, _basket, _pendingPayload)",
            "RefreshSavedBaskets(deletion.SavedBaskets)",
            "_savedBasketDeletion.IsDeleteEnabled(SavedBasketsBox.SelectedItem as SavedSyntheticBasket)",
        })
        {
            if (!source.Contains(required, StringComparison.Ordinal))
            {
                throw new Exception($"saved basket deletion source missing {required}");
            }
        }
    }

    private static string SourcePath(params string[] parts)
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "index.html")))
        {
            directory = directory.Parent;
        }

        if (directory is null) throw new Exception("repository root could not be located");
        return Path.Combine([directory.FullName, .. parts]);
    }

    private sealed class StubCapitalHandler : HttpMessageHandler
    {
        public List<string> RequestUris { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUris.Add(request.RequestUri?.ToString() ?? "");
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    request.RequestUri?.AbsolutePath.EndsWith("/api/v1/session", StringComparison.OrdinalIgnoreCase) == true
                        ? "{}"
                        : "{\"markets\":[]}",
                    Encoding.UTF8,
                    "application/json"),
            };
            response.Headers.Add("CST", "cst-token");
            response.Headers.Add("X-SECURITY-TOKEN", "security-token");
            return Task.FromResult(response);
        }
    }

    private sealed class HistoryPagingHandler(HttpStatusCode terminalStatus, string terminalBody) : HttpMessageHandler
    {
        private int _priceRequestCount;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri?.AbsolutePath.EndsWith("/api/v1/session", StringComparison.OrdinalIgnoreCase) == true)
            {
                var login = JsonResponse(HttpStatusCode.OK, "{}");
                login.Headers.Add("CST", "cst-token");
                login.Headers.Add("X-SECURITY-TOKEN", "security-token");
                return Task.FromResult(login);
            }

            _priceRequestCount++;
            if (_priceRequestCount == 1)
            {
                return Task.FromResult(JsonResponse(HttpStatusCode.OK, PricePage("2026-07-01T00:00:00Z", 101m)));
            }
            if (_priceRequestCount == 2)
            {
                return Task.FromResult(JsonResponse(HttpStatusCode.OK, PricePage("2026-06-01T00:00:00Z", 99m)));
            }
            return Task.FromResult(JsonResponse(terminalStatus, terminalBody));
        }

        private static HttpResponseMessage JsonResponse(HttpStatusCode status, string body) => new(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };

        private static string PricePage(string timestamp, decimal close) => JsonSerializer.Serialize(new
        {
            prices = new[]
            {
                new
                {
                    snapshotTimeUTC = timestamp,
                    openPrice = new { bid = close - 1m },
                    highPrice = new { bid = close + 1m },
                    lowPrice = new { bid = close - 2m },
                    closePrice = new { bid = close },
                },
            },
        });
    }

    private sealed class FakeCapitalStreamingSocket(WebSocketState state, bool closeOnReceive = false) : ICapitalStreamingSocket
    {
        public WebSocketState State { get; private set; } = state;

        public Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
        {
            State = WebSocketState.Open;
            return Task.CompletedTask;
        }

        public Task SendAsync(ArraySegment<byte> bytes, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            if (closeOnReceive) State = WebSocketState.Closed;
            return Task.FromResult(new WebSocketReceiveResult(0, WebSocketMessageType.Close, true, WebSocketCloseStatus.NormalClosure, "closed"));
        }

        public Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            State = WebSocketState.Closed;
            return Task.CompletedTask;
        }

        public void Dispose() => State = WebSocketState.Closed;
    }
}
