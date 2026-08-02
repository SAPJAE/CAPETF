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
    public static void RunManualFormula()
    {
        ManualFormulaBuildsExactCryptoPresetWithoutEqualNotionalRewriting();
        ManualHistoryUsesExactUtcTimestampsAtDailyAndWeeklyResolutions();
        ManualCryptoHistoryAndRealtimeBarsUseDirectSharedFormula();
        AutomaticBasketIgnoresNativeOhlcEvents();
        ManualCryptoTimeframesReloadLongestSharedHistory();
        CapitalStreamingParsesOhlcEventsForManualCryptoBars();
        ManualCryptoBuildAndRestoreSubscribeBothEpicsAndUpdateResolution();
        ManualFormulaResolvesExactIdentifiersAndRejectsInvalidTerms();
        ManualFormulaResolutionIsBlockLocalAndTiered();
        ManualFormulaResolvesCapitalEpicPairSegmentsWithinSelectedBlock();
        ManualFormulaSaveRestorePreservesTwoLegStrategyAndExactMultipliers();
        SavedManualFormulaIdentityIncludesExactRatios();
        ManualBasketStrategyIdentitySurvivesDropdownChanges();
        SignedSyntheticQuotesUseExecutableBidAskSides();
        ManualFormulaEditorIsCompactConditionalAndBypassesAutomaticSelection();
    }

    public static void RunCryptoUniverse()
    {
        CryptoUniverseRecognizesOpenEligibleInstrumentsAndDeduplicatesEpics();
        CapitalApiClientUsesAllMarketsForEmptySearchAndParsesCryptoRows();
        CryptoMetadataEnrichmentBuildsAuthoritativeCurrencyGroupsAndResolvesThePreset();
        CryptoMetadataEnrichmentDeduplicatesRequestsAndCachesSuccessfulDetails();
        CryptoMetadataEnrichmentToleratesFailuresAndReappliesOpenableFiltering();
        CryptoMetadataEnrichmentHonorsCancellationReportsProgressAndBoundsConcurrency();
        CryptoMetadataEnrichmentPacesRequestsAndRetriesRateLimits();
        CryptoGroupingDerivesExplicitQuoteCurrencyWhenCapitalOmitsCurrency();
        CapComTerminalExposesGroupedCryptoUniverse();
        TerminalUniverseUiCoordinatorRestoresKnownEtfExclusionAfterCrypto();
        SavedAndOpenBasketsRestoreTheirUniverseInBothDirections();
        LegacyUniverseResolutionProbesUncataloguedEtfsAndRejectsUnresolvedRecords();
        TerminalUniverseUiCoordinatorCachesUniversesSeparately();
        TerminalUniverseUiCoordinatorClearsBeforeAFailedSwitchLoad();
        TerminalUniverseUiCoordinatorBuildsBlocksAndSeedsForTheActiveUniverse();
        TerminalUniverseAccumulatorPublishesCachedSnapshotBeforeDiscovery();
        TerminalUniverseAccumulatorMergesApiBatchesDeterministically();
        TerminalUniverseAccumulatorPreservesCurrentSelection();
        TerminalUniverseAccumulatorReportsStagedProgress();
        TerminalUniverseCacheRoundTripsMergedSnapshots();
        CapComTerminalProgressivelyDiscoversUniversesWithoutBlockingControls();
    }

    public static void RunAll()
    {
        TerminalOperationStateRejectsDuplicatesAndTracksProgress();
        TerminalOperationStageResetsCompletedTotalsForIndeterminateWork();
        TerminalProgressPercentUsesOneWayBinding();
        TerminalProgressPanelAvoidsWebViewAirspace();
        CapComTerminalOperationGuardCompletesFailsAndRestoresControls();
        NewOperationCancelsAndSupersedesEarlierWork();
        IncompleteOhlcRowsAreExcluded();
        MarketDetailsParseMarginMetadata();
        AccountsParseActiveAvailableFunds();
        SyntheticMarginPreviewEnrichesBlankBasketCurrency();
        SyntheticMarginPreviewRetainsExpiredAccountSnapshotAsStale();
        SyntheticMarginPreviewCachesUnavailableConversionBriefly();
        SyntheticMarginPreviewTreatsDemoAliasAsSameCurrency();
        SyntheticMarginPreviewUsesNormalizedDemoAliasForFxLookup();
        SyntheticMarginRejectsNullFactorAndNonpositiveConversion();
        SyntheticMarginUsesDefaultLotSizeForNullAndZero();
        CapComTerminalResetsMarginContextAndRejectsInvalidNotional();
        InvalidMarginInputCancelsHostPublicationOwnership();
        SyntheticTerminalMarginRuntimeExercisesFinalReviewRegressions();
        AccountsRejectPreferredFallbackWithoutCurrentAccountId();
        AccountsRejectMissingAvailableFunds();
        AccountsRejectNonNumericAvailableFunds();
        CapitalPricePathSupportsDatedHistoryWindows();
        CapitalHistoryPagingWindowsMatchCapitalResolutions();
        CapitalHistoryPagingRetainsSuccessfulRowsAtTerminalBoundary();
        CapitalHistoryPagingRetainsRowsWhenOlderHistoryIsNotFound();
        CapitalHistoryPagingDoesNotSwallowAuthFailure();
        SelectedHistoryUsesOneCapitalPagingAnchorAcrossAllLegs();
        SyntheticHistoryServiceMapsTerminalTimeframesToCapitalResolutions();
        SyntheticHistoryServiceClassifiesCancellationAuthServerAndUnavailableFailures();
        SyntheticHistoryServiceAggregatesHourlyCandlesLocally();
        SyntheticHistoryServiceAggregatesTradingSessionsAndRejectsGaps();
        IntradayAggregationUsesStableUtcBoundariesAcrossLegs();
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
        FirstLiveQuoteUsesFinalSharedCloseWithAsymmetricHistoryTails();
        SyntheticBasketsUseCallerSuppliedCandidates();
        StockTypeMatchingIsCaseInsensitive();
        RunCryptoUniverse();
        EtfUniverseRecognitionAndIsolation();
        KnownEtfEpicsOverrideCapitalShareType();
        EtfMetadataMergeUsesCapitalDetailsForGrouping();
        EtfMetadataMergeReappliesCurrentApiEligibility();
        EtfMetadataMergeUsesDeterministicFallbacks();
        EtfMetadataMergeDerivesRegionFromCapitalCountry();
        EtfMetadataMergeTreatsOtherRegionAsPlaceholder();
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
        TerminalPayloadDefinesOneFormulaAsOneSyntheticLot();
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
        LiveWeeklyQuoteStartsCurrentCandleInsteadOfRepaintingPriorWeek();
        LiveCandleCloseTracksCompleteSyntheticQuoteMidpoint();
        MarketSnapshotsConstructOngoingCandleBeforeFirstStreamTick();
        IncrementalAndNativeFreshnessUseIndependentUtcNow();
        StreamingQuoteClearsMissingAndZeroSides();
        StreamingQuoteClearsUnavailableSidesWithoutUsablePrice();
        OutOfOrderStreamingQuoteDoesNotMutateComponentState();
        SyntheticTerminalHtmlExposesRequiredFunctions();
        SyntheticTerminalHtmlShowsBidAndAskWithoutLastPrice();
        SyntheticTerminalHtmlUsesPackagedChartLibrary();
        SyntheticTerminalHtmlRejectsKLineChartRuntime();
        SyntheticDrawingsRuntimeExercisesReviewRegressions();
        SyntheticDrawingWorkspaceRuntimeCoordinatesManagerStateAndPersistence();
        SyntheticTerminalHtmlUsesV3LightweightChartsTerminal();
        SyntheticTerminalHtmlUsesV5SeriesApiAndChartSideTools();
        SyntheticDrawingsAssetPublishesWithProjectContentEntry();
        SyntheticDrawingsRuntimeValidatesRecordsMeasurementAndHistory();
        SyntheticTerminalHtmlExposesResizableRailAndPersistentDrawingTools();
        SyntheticTerminalHtmlDisablesNativeLastCloseDecorations();
        SyntheticTerminalHtmlCoalescesIncrementalTicksAndUsesStableDrawingIdentity();
        SyntheticTerminalRuntimeCoalescesTicksAndRejectsStaleBasketFrames();
        CapComTerminalUsesTask6PayloadBridge();
        SyntheticTerminalHtmlExposesResizeFunction();
        SyntheticTerminalHtmlExposesDecisionChartControls();
        SyntheticTerminalHtmlExposesV2TerminalControls();
        TerminalOrderPreviewUsesProductionSizingBridge();
        SyntheticTerminalMarginPreviewRendersAndRefreshes();
        SyntheticTerminalMarginPreviewMarksAnyMissingDisplayedTotalUnavailable();
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
        CapitalApiClientRejectsOffsetlessSnapshotForTradingFreshness();
        MarketSnapshotRejectsOlderSourceTimeAndClearsMissingSides();
        CapitalStreamingTimestampParserRequiresSourceTime();
        CapitalStreamingClientRejectsClosedSocketsAndWindowRecreates();
        CapitalStreamingClientReportsRemoteClose();
        CapitalStreamingCleanupIsBoundedAndIdempotent();
        WindowLifetimeCancelsAndRejectsLateCompletion();
        WindowLifetimeCancellationNeverEntersFailureUi();
        WebViewRuntimeProfileIsExternalAndExplicitlyConfigured();
        DesktopPublishAndThirdPartyPackagingContractsAreComplete();
        StockChunkLoaderPrefersLegacyWhenChunksAreSmallerThanLegacy();
        CapComTerminalLoadsFullEncryptedStockChunks();
        TerminalWorkspaceModeNameIsAvailable();
        TerminalStreamingEpicsUseOnlySelectedSyntheticComponents();
        SyntheticStrategiesRankExpectedSetups();
        SyntheticStrategiesExposeBuildOptions();
        SyntheticStrategiesReturnClosestFallbackCandidates();
        StrategyCandidatePoolKeepsOnlyTopSignalRanksForClustering();
        WeeklyStrategiesScaleMaPeriodsFromTradingDays();
        DipInsideUptrendBuildsFromBundledUsDailyUniverse();
        SyntheticQuoteUsesFormulaMultipliersForBidAsk();
        SyntheticQuoteTreatsMissingOrZeroSidesAsUnavailable();
        SyntheticOrderSizingUsesCapitalDealRules();
        SyntheticLotOrderPreviewMultipliesFormulaExactly();
        AdaptiveDisplayMultiplierPreservesSmallNonzeroValues();
        ExecutablePreviewUsesCurrentEqualNotionalAndDealRules();
        ExecutablePreviewRoundsUpToCapitalDealMinimumAndIncrement();
        ExecutableOrderPreviewUsesCurrentSideQuotesAndReportsImbalance();
        SyntheticMarginCalculatesBuyAndSellUsingExecutableLegs();
        SyntheticMarginUsesLotSizeWhenSizingExecutableNotional();
        SyntheticMarginReportsUnsupportedMarginUnitsAsUnavailable();
        SyntheticMarginCombinesAccountAvailability();
        SyntheticMarginRejectsAccountCurrencyMismatch();
        SyntheticMarginPreviewUsesSameCurrencyAndCachesAccount();
        SyntheticMarginPreviewUsesDirectMidpointAndRefreshesMissingMetadata();
        SyntheticMarginPreviewUsesReciprocalInverseQuote();
        SyntheticMarginPreviewRejectsMissingConversion();
        CapComTerminalRefreshesMarginPreviewContract();
        MarginPreviewPublicationRejectsSupersededFailures();
        SyntheticMarginPreviewRejectsReversedDirectPair();
        SyntheticMarginPreviewTriesEveryOrderedFxCandidate();
        SyntheticMarginPreviewInvalidatesAllCaches();
        CapComTerminalInvalidatesMarginCachesAfterEveryLogin();
        SyntheticMarginMetadataAttemptsCachePartialAndFailedResponses();
        SyntheticMarginMetadataAttemptsRetryAfterExpiry();
        SyntheticMarginMetadataRefreshIsSingleFlight();
        SyntheticMarginMetadataRefreshRemainsSingleFlightWhileRunningPastTtl();
        SyntheticMarginPreviewMatchesOrderedDescriptiveNameOnlyPair();
        SavedSyntheticBasketStorePersistsFormulaDetails();
        SavedBasketRestorePreservesExactFormulaAndRejectsMissingEpics();
        SavedBasketLoadUsesFaithfulFormulaRestorer();
        SavedSyntheticBasketStoreDeletesSelectedBasket();
        SavedBasketDeletionCoordinatorTracksSelectionAndPreservesDisplayedModels();
        SavedBasketDeletionUiContractIsPresent();
        RunManualFormula();
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

    private static void MarketDetailsParseMarginMetadata()
    {
        const string json = """
        {
          "instrument": {
            "epic": "SAPD", "name": "SAP", "currency": "EUR",
            "lotSize": 1, "marginFactor": 20, "marginFactorUnit": "PERCENTAGE"
          },
          "snapshot": { "bid": 127.10, "offer": 127.20 }
        }
        """;
        var result = CapitalApiClient.ParseMarketDetails(json)!;
        AssertNear(20m, result.MarginFactor ?? 0m, "margin factor");
        AssertEqual("PERCENTAGE", result.MarginFactorUnit, "margin unit");
    }

    private static void AccountsParseActiveAvailableFunds()
    {
        const string json = """
        { "accounts": [
          { "accountId": "other", "preferred": true, "currency": "GBP",
            "balance": { "available": 50 } },
          { "accountId": "active", "preferred": false, "currency": "USD",
            "balance": { "available": 1250.75 } }
        ] }
        """;
        var result = CapitalApiClient.ParseActiveAccount(json, "active", DateTimeOffset.UnixEpoch);
        AssertEqual("active", result.AccountId, "active account");
        AssertEqual("USD", result.Currency, "account currency");
        AssertNear(1250.75m, result.Available, "available funds");
    }

    private static void AccountsRejectPreferredFallbackWithoutCurrentAccountId()
    {
        const string json = """
        { "accounts": [
          { "accountId": "preferred", "preferred": true, "currency": "GBP",
            "balance": { "available": 50 } }
        ] }
        """;

        try
        {
            CapitalApiClient.ParseActiveAccount(json, "", DateTimeOffset.UnixEpoch);
            throw new Exception("a blank current account ID must not fall back to a preferred account");
        }
        catch (InvalidOperationException ex)
        {
            AssertTrue(ex.Message.Contains("current account", StringComparison.OrdinalIgnoreCase),
                "blank current account failure must identify the missing current account");
        }
    }

    private static void AccountsRejectMissingAvailableFunds()
    {
        const string json = """
        { "accounts": [
          { "accountId": "active", "preferred": false, "currency": "USD",
            "balance": {} }
        ] }
        """;

        try
        {
            CapitalApiClient.ParseActiveAccount(json, "active", DateTimeOffset.UnixEpoch);
            throw new Exception("missing balance.available must not become zero available funds");
        }
        catch (InvalidOperationException ex)
        {
            AssertTrue(ex.Message.Contains("available", StringComparison.OrdinalIgnoreCase),
                "missing balance failure must identify available funds");
        }
    }

    private static void AccountsRejectNonNumericAvailableFunds()
    {
        const string json = """
        { "accounts": [
          { "accountId": "active", "preferred": false, "currency": "USD",
            "balance": { "available": "unknown" } }
        ] }
        """;

        try
        {
            CapitalApiClient.ParseActiveAccount(json, "active", DateTimeOffset.UnixEpoch);
            throw new Exception("non-numeric balance.available must not become zero available funds");
        }
        catch (InvalidOperationException ex)
        {
            AssertTrue(ex.Message.Contains("available", StringComparison.OrdinalIgnoreCase),
                "non-numeric balance failure must identify available funds");
        }
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

        AssertTrue(guard.Contains("TerminalOperationExecution.RunAsync", StringComparison.Ordinal), "the operation guard should classify lifetime cancellation before applying UI outcomes");
        AssertTrue(guard.Contains("_operationState.Complete()", StringComparison.Ordinal), "the operation guard should complete successful work");
        AssertTrue(guard.Contains("_operationState.Fail(ex.Message);", StringComparison.Ordinal), "the operation guard should fail unsuccessful work");
        AssertTrue(guard.Contains("_windowLifetime.TryApply", StringComparison.Ordinal), "the operation guard should reject completion and failure UI after close begins");
        AssertTrue(guard.Contains("finally", StringComparison.Ordinal), "the operation guard should restore controls in finally");
        AssertTrue(guard.Contains("SetOperationControlsEnabled(true)", StringComparison.Ordinal), "the operation guard should re-enable controls after success or failure");

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

    private static void SyntheticHistoryServiceClassifiesCancellationAuthServerAndUnavailableFailures()
    {
        var component = new MarketInstrument { Epic = "HISTORY-FAILURE", Name = "History Failure" };

        using (var canceledClient = new CapitalApiClient(new HistoryFailureHandler(HttpStatusCode.OK, "{\"prices\":[]}")))
        {
            canceledClient.LoginAsync(TestCredentials()).GetAwaiter().GetResult();
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            try
            {
                new SyntheticHistoryService(canceledClient)
                    .LoadSelectedAsync([component], "Weekly", cancellationToken: cancellation.Token)
                    .GetAwaiter()
                    .GetResult();
                throw new Exception("history cancellation must propagate to the operation owner");
            }
            catch (OperationCanceledException)
            {
            }
        }

        AssertHistoryFailureEscapes(HttpStatusCode.Unauthorized, "{\"errorCode\":\"error.security.client-token-invalid\"}", "expired authentication");
        AssertHistoryFailureEscapes(HttpStatusCode.InternalServerError, "{\"errorCode\":\"error.server.internal\"}", "Capital.com server failure");

        using var unavailableClient = new CapitalApiClient(new HistoryFailureHandler(
            HttpStatusCode.NotFound,
            "{\"errorCode\":\"error.prices.not-found\"}"));
        unavailableClient.LoginAsync(TestCredentials()).GetAwaiter().GetResult();
        var unavailableComponent = new MarketInstrument { Epic = "HISTORY-NOT-FOUND", Name = "Unavailable History" };
        var unavailable = new SyntheticHistoryService(unavailableClient)
            .LoadSelectedAsync([unavailableComponent], "Weekly")
            .GetAwaiter()
            .GetResult();
        AssertEqual(0, unavailable.CandlesByEpic.Count, "an explicitly unavailable instrument may be omitted from loaded history");
        AssertTrue(unavailableComponent.Status.StartsWith("History n/a", StringComparison.Ordinal), "an unavailable instrument must be classified per leg");
    }

    private static void AssertHistoryFailureEscapes(HttpStatusCode status, string body, string label)
    {
        using var client = new CapitalApiClient(new HistoryFailureHandler(status, body));
        client.LoginAsync(TestCredentials()).GetAwaiter().GetResult();
        try
        {
            new SyntheticHistoryService(client)
                .LoadSelectedAsync([new MarketInstrument { Epic = "HISTORY-FAILURE" }], "Weekly")
                .GetAwaiter()
                .GetResult();
            throw new Exception($"{label} must escape the per-instrument history fallback");
        }
        catch (CapitalApiException ex) when (ex.StatusCode == status)
        {
        }
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
        foreach (var errorCode in new[] { "error.invalid.from", "error.invalid.daterange" })
        {
            var handler = new HistoryPagingHandler(HttpStatusCode.BadRequest, $"{{\"errorCode\":\"{errorCode}\"}}");
            using var client = new CapitalApiClient(handler);
            client.LoginAsync(TestCredentials()).GetAwaiter().GetResult();

            var rows = client.GetAllAvailableOhlcPricesAsync("TEST", "DAY").GetAwaiter().GetResult();

            AssertEqual(2, rows.Count, $"{errorCode} terminal history boundary must retain all successful pages");
            AssertEqual(DateTimeOffset.Parse("2026-06-01T00:00:00Z"), rows[0].Time, $"{errorCode} older successful history page");
            AssertEqual(DateTimeOffset.Parse("2026-07-01T00:00:00Z"), rows[1].Time, $"{errorCode} newer successful history page");
        }
    }

    private static void CapitalHistoryPagingRetainsRowsWhenOlderHistoryIsNotFound()
    {
        var handler = new HistoryPagingHandler(
            HttpStatusCode.NotFound,
            "{\"errorCode\":\"error.prices.not-found\"}");
        using var client = new CapitalApiClient(handler);
        client.LoginAsync(TestCredentials()).GetAwaiter().GetResult();

        var rows = client.GetAllAvailableOhlcPricesAsync("TEST", "DAY").GetAwaiter().GetResult();

        AssertEqual(2, rows.Count, "older-history not-found boundary must retain all successful pages");
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

    private static void SelectedHistoryUsesOneCapitalPagingAnchorAcrossAllLegs()
    {
        var handler = new SharedHistoryAnchorHandler();
        using var client = new CapitalApiClient(handler);
        client.LoginAsync(TestCredentials()).GetAwaiter().GetResult();

        new SyntheticHistoryService(client).LoadSelectedAsync(
                [new MarketInstrument { Epic = "ETH" }, new MarketInstrument { Epic = "BTC" }],
                "Weekly")
            .GetAwaiter()
            .GetResult();

        AssertEqual(2, handler.InitialToValues.Count, "both manual legs must issue an initial history request");
        AssertEqual(handler.InitialToValues[0], handler.InitialToValues[1], "all selected legs must share one Capital history paging anchor");
        AssertTrue(
            DateTimeOffset.Parse(handler.InitialToValues[0]) <= DateTimeOffset.UtcNow,
            "Capital history paging anchor must not be in the future");
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
        AssertEqual(0, nonMidnight.Count, "an incomplete UTC-aligned six-hour bucket must not shift to a symbol's first bar");

        var dstRows = new[]
        {
            DateTimeOffset.Parse("2026-03-08T00:00:00-05:00"),
            DateTimeOffset.Parse("2026-03-08T01:00:00-05:00"),
            DateTimeOffset.Parse("2026-03-08T03:00:00-04:00"),
            DateTimeOffset.Parse("2026-03-08T04:00:00-04:00"),
            DateTimeOffset.Parse("2026-03-08T05:00:00-04:00"),
            DateTimeOffset.Parse("2026-03-08T06:00:00-04:00"),
        }.Select((time, index) => FlatCandle(time, 100m + index)).ToList();
        var dst = SyntheticHistoryService.Transform(dstRows, "2H");
        AssertEqual(2, dst.Count, "DST clock changes must not break fixed UTC two-hour buckets");
        AssertEqual(dstRows[2].Time, dst[0].Time, "DST aggregate must retain the final source offset and timestamp");

        var gapStart = DateTimeOffset.Parse("2026-07-20T09:00:00Z");
        var gapRows = new[] { 0, 1, 3, 4, 5, 6 }
            .Select(offset => FlatCandle(gapStart.AddHours(offset), 100m + offset))
            .ToList();
        var afterGap = SyntheticHistoryService.Transform(gapRows, "2H");
        AssertEqual(2, afterGap.Count, "complete UTC-aligned groups on either side of a gap must remain available");
        if (afterGap.Any(candle => candle.Time == gapStart.AddHours(3)))
        {
            throw new Exception("a 2H candle must not bridge the missing hourly bar");
        }
    }

    private static void IntradayAggregationUsesStableUtcBoundariesAcrossLegs()
    {
        var start = DateTimeOffset.Parse("2026-07-20T00:00:00Z");
        var eth = HourlyCandles(start, 12);
        var btc = HourlyCandles(start.AddHours(1), 11);

        var ethTwoHour = SyntheticHistoryService.Transform(eth, "2H");
        var btcTwoHour = SyntheticHistoryService.Transform(btc, "2H");
        AssertSequence(
            ethTwoHour.Select(candle => candle.Time),
            start.AddHours(1), start.AddHours(3), start.AddHours(5),
            start.AddHours(7), start.AddHours(9), start.AddHours(11));
        AssertSequence(
            btcTwoHour.Select(candle => candle.Time),
            start.AddHours(3), start.AddHours(5), start.AddHours(7),
            start.AddHours(9), start.AddHours(11));

        var withGap = eth.Where(candle => candle.Time != start.AddHours(7)).ToList();
        var sixHour = SyntheticHistoryService.Transform(withGap, "6H");
        AssertSequence(
            sixHour.Select(candle => candle.Time),
            start.AddHours(5));
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
        var expected = new[] { "2H", "4H", "6H" }.ToDictionary(
            interval => interval,
            interval => (IReadOnlyList<DateTimeOffset>)SyntheticHistoryService.Transform(serviceRows, interval)
                .Select(row => row.Time)
                .ToList(),
            StringComparer.OrdinalIgnoreCase);

        foreach (var interval in new[] { "2H", "4H", "6H" })
        {
            var expectedCached = expected[interval].Count >= 2 ? expected[interval] : [];
            AssertCandleTimes(expectedCached, stock.GetValueOrDefault(interval) ?? [], $"stock cached {interval}");
            AssertCandleTimes(expectedCached, etf.GetValueOrDefault(interval) ?? [], $"ETF cached {interval}");
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
            ["SELECTED-A"] = stock["2H"],
            ["SELECTED-B"] = etf["2H"],
            ["SELECTED-C"] = stock["2H"],
        };
        var apiOverride = FlatCandle(expected["2H"][0], 999m);
        var merged = SyntheticHistoryService.MergeSelectedHistory(
            selected,
            "2H",
            new HistoryLoadResult(
                new Dictionary<string, IReadOnlyList<OhlcPoint>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["SELECTED-A"] = [apiOverride],
                },
                null,
                null,
                0),
            cached);

        AssertEqual(expected["2H"].Count, merged.SharedCount, "partial intraday API history must retain only valid cached shared buckets");
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

    private static void FirstLiveQuoteUsesFinalSharedCloseWithAsymmetricHistoryTails()
    {
        var day = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var instruments = new[]
        {
            CreateStock("ASYM-A", "Asymmetric A"),
            CreateStock("ASYM-B", "Asymmetric B"),
            CreateStock("ASYM-C", "Asymmetric C"),
        };
        var candles = new Dictionary<string, IReadOnlyList<OhlcPoint>>
        {
            ["ASYM-A"] =
            [
                FlatCandle(day, 100m),
                FlatCandle(day.AddDays(1), 110m),
                FlatCandle(day.AddDays(2), 120m),
            ],
            ["ASYM-B"] =
            [
                FlatCandle(day, 50m),
                FlatCandle(day.AddDays(1), 55m),
                FlatCandle(day.AddDays(3), 66m),
            ],
            ["ASYM-C"] =
            [
                FlatCandle(day, 25m),
                FlatCandle(day.AddDays(1), 30m),
                FlatCandle(day.AddDays(4), 39m),
            ],
        };

        var basket = SyntheticBasketBuilder.Build(
            "US / USD / Tech",
            instruments,
            candles,
            maxBaskets: 1,
            periodsPerYear: 252,
            minimumCandles: 2).Baskets.Single();
        var component = basket.Components.Single(item => item.Instrument.Epic == "ASYM-A");
        var chartClose = basket.Candles[^1].Close;
        var finalSharedComponentClose = candles["ASYM-A"][1].Close;
        const decimal firstLivePrice = 130m;

        var result = SyntheticLiveUpdate.ApplyQuote(
            basket,
            new QuoteUpdate("ASYM-A", 129m, 131m, firstLivePrice, day.AddDays(5)));

        AssertTrue(result.CandleChanged, "the first asymmetric-tail live quote must update the chart candle");
        AssertNear(
            chartClose + (firstLivePrice - finalSharedComponentClose) * component.FormulaMultiplier,
            basket.Candles[^1].Close,
            "the first live quote must include movement from the final shared chart candle, not the leg's private tail");
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

    private static void CryptoUniverseRecognizesOpenEligibleInstrumentsAndDeduplicatesEpics()
    {
        var openCrypto = new MarketInstrument { Epic = "CRYPTO.BTCUSD.CFD.IP", Type = "CRYPTOCURRENCIES", Status = "OPEN" };
        var closedCrypto = new MarketInstrument { Epic = "CRYPTO.ETHUSD.CFD.IP", Type = "CRYPTOCURRENCIES", Status = "CLOSED" };
        var closedViewOnlyCrypto = new MarketInstrument
        {
            Epic = "CRYPTO.CLOSED.VIEW",
            Type = "CRYPTOCURRENCIES",
            Status = "CLOSED",
            MarketModes = ["VIEW_ONLY"],
        };
        var closeOnlyCrypto = new MarketInstrument { Epic = "CRYPTO.CLOSE", Type = "CRYPTOCURRENCIES", Status = "CLOSE_ONLY" };
        var viewOnlyCrypto = new MarketInstrument { Epic = "CRYPTO.VIEW", Type = "CRYPTOCURRENCIES", Status = "VIEW_ONLY" };
        var reduceOnlyCrypto = new MarketInstrument { Epic = "CRYPTO.REDUCE", Type = "CRYPTOCURRENCIES", Status = "REDUCE_ONLY" };
        var disabledCrypto = new MarketInstrument { Epic = "CRYPTO.DISABLED", Type = "CRYPTOCURRENCIES", Status = "DISABLED" };
        var suspendedCrypto = new MarketInstrument { Epic = "CRYPTO.SUSPENDED", Type = "CRYPTOCURRENCIES", Status = "SUSPENDED" };
        var obsoleteCrypto = new MarketInstrument { Epic = "CRYPTO.OBSOLETE", Type = "CRYPTOCURRENCIES", Status = "OBSOLETE" };
        var nonOpenableCrypto = new MarketInstrument { Epic = "CRYPTO.NONOPEN", Type = "CRYPTOCURRENCIES", Status = "CANNOT_OPEN" };
        var stock = new MarketInstrument { Epic = "SHARE", Type = "SHARES", Status = "OPEN" };

        AssertTrue(CapitalInstrumentTypes.IsCrypto(openCrypto), "CRYPTOCURRENCIES must be recognized as crypto");
        AssertTrue(TerminalUniverse.Accepts(TerminalUniverseKind.Crypto, openCrypto), "the crypto universe must accept open crypto");
        AssertTrue(TerminalUniverse.Accepts(TerminalUniverseKind.Crypto, closedCrypto), "temporarily closed crypto must remain visible");
        AssertTrue(!TerminalUniverse.Accepts(TerminalUniverseKind.Crypto, closedViewOnlyCrypto),
            "a CLOSED crypto market with VIEW_ONLY mode must be excluded");
        AssertTrue(!TerminalUniverse.Accepts(TerminalUniverseKind.Crypto, closeOnlyCrypto), "close-only crypto must be excluded");
        AssertTrue(!TerminalUniverse.Accepts(TerminalUniverseKind.Crypto, viewOnlyCrypto), "view-only crypto must be excluded");
        AssertTrue(!TerminalUniverse.Accepts(TerminalUniverseKind.Crypto, reduceOnlyCrypto), "reduce-only crypto must be excluded");
        AssertTrue(!TerminalUniverse.Accepts(TerminalUniverseKind.Crypto, disabledCrypto), "disabled crypto must be excluded");
        AssertTrue(!TerminalUniverse.Accepts(TerminalUniverseKind.Crypto, suspendedCrypto), "suspended crypto must be excluded");
        AssertTrue(!TerminalUniverse.Accepts(TerminalUniverseKind.Crypto, obsoleteCrypto), "obsolete crypto must be excluded");
        AssertTrue(!TerminalUniverse.Accepts(TerminalUniverseKind.Crypto, nonOpenableCrypto), "non-openable crypto must be excluded");
        AssertTrue(!TerminalUniverse.Accepts(TerminalUniverseKind.Crypto, stock), "the crypto universe must exclude non-crypto instruments");
        AssertEqual("", TerminalUniverseLoadPolicy.ApiSearchTerm(TerminalUniverseKind.Crypto, "BTC"), "crypto fallback search term");

        var normalized = TerminalUniverseLoadPolicy.NormalizeApiFallback(
            TerminalUniverseKind.Crypto,
            [
                openCrypto,
                new MarketInstrument { Epic = "crypto.btcusd.cfd.ip", Type = "CRYPTOCURRENCIES", Status = "OPEN" },
                closedCrypto,
                closeOnlyCrypto,
                stock,
            ],
            new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        AssertEqual(2, normalized.Count, "crypto API fallback must filter ineligible instruments and collapse duplicate epics");
        AssertTrue(normalized.Select(item => item.Epic).Distinct(StringComparer.OrdinalIgnoreCase).Count() == normalized.Count,
            "crypto API fallback must return one instrument per epic");
    }

    private static void CapitalApiClientUsesAllMarketsForEmptySearchAndParsesCryptoRows()
    {
        var handler = new CryptoMarketsHandler();
        using var client = new CapitalApiClient(handler);
        client.LoginAsync(TestCredentials()).GetAwaiter().GetResult();

        var markets = client.SearchMarketsAsync("").GetAwaiter().GetResult();

        AssertEqual("/api/v1/markets", handler.MarketRequestUri?.AbsolutePath, "an empty market search must use the all-markets endpoint");
        AssertEqual(HttpMethod.Get, handler.MarketRequestMethod, "an empty market search must use GET");
        AssertEqual("", handler.MarketRequestUri?.Query, "an empty market search must omit searchTerm");
        AssertEqual(2, markets.Count, "the all-markets fixture must parse both crypto rows");

        var bitcoin = markets.Single(item => item.Epic == "CRYPTO.BTCUSD.CFD.IP");
        AssertEqual("CRYPTOCURRENCIES", bitcoin.Type, "BTC/USD type");
        AssertEqual("USD", bitcoin.Currency, "BTC/USD currency");
        AssertEqual("TRADEABLE", bitcoin.Status, "BTC/USD status");
        AssertNear(104000.5m, bitcoin.Bid ?? 0m, "BTC/USD bid");
        AssertNear(104001.5m, bitcoin.Offer ?? 0m, "BTC/USD offer");

        var ethereum = markets.Single(item => item.Epic == "CRYPTO.ETHUSD.CFD.IP");
        AssertEqual("CRYPTOCURRENCIES", ethereum.Type, "ETH/USD type");
        AssertEqual("USD", ethereum.Currency, "ETH/USD currency");
        AssertEqual("CLOSED", ethereum.Status, "ETH/USD status");
        AssertNear(3200.25m, ethereum.Bid ?? 0m, "ETH/USD bid");
        AssertNear(3201.25m, ethereum.Offer ?? 0m, "ETH/USD offer");
    }

    private static void CryptoMetadataEnrichmentBuildsAuthoritativeCurrencyGroupsAndResolvesThePreset()
    {
        var summaries = new[]
        {
            CryptoMetadataFixture("ETH-USD", "ETH/USD", "Ethereum / US Dollar", ""),
            CryptoMetadataFixture("BTC-USD", "BTC/USD", "Bitcoin / US Dollar", ""),
            CryptoMetadataFixture("BTC-EUR", "BTC/EUR", "Bitcoin / Euro", ""),
        };
        var details = new Dictionary<string, MarketInstrument>(StringComparer.OrdinalIgnoreCase)
        {
            ["ETH-USD"] = CryptoMetadataFixture("ETH-USD", "ETH/USD", "Ethereum / US Dollar", "USD", minDealSize: 0.01m),
            ["BTC-USD"] = CryptoMetadataFixture("BTC-USD", "BTC/USD", "Bitcoin / US Dollar", "USD", minDealSize: 0.001m),
            ["BTC-EUR"] = CryptoMetadataFixture("BTC-EUR", "BTC/EUR", "Bitcoin / Euro", "EUR", minDealSize: 0.001m),
        };

        var enriched = EnrichCryptoMetadata(
            summaries,
            (epic, _) => Task.FromResult<MarketInstrument?>(details[epic]));

        AssertSequence(enriched.Select(item => item.Currency), "USD", "USD", "EUR");
        var grouped = TerminalCryptoUniverseGrouping.Normalize(enriched);
        AssertSequence(
            grouped.Select(item => item.Group),
            "Crypto / USD / All",
            "Crypto / USD / All",
            "Crypto / EUR / All");
        var resolved = ManualSyntheticBasketFactory.Resolve(
            "Crypto / USD / All",
            ManualSyntheticFormula.Parse(ManualSyntheticFormula.CryptoPreset),
            grouped);
        AssertSequence(resolved.Select(item => item.Epic), "ETH-USD", "BTC-USD");
        AssertNear(0.01m, resolved[0].MinDealSize ?? 0m, "ETH/USD details must provide the authoritative minimum deal size");
        AssertNear(0.001m, resolved[1].MinDealSize ?? 0m, "BTC/USD details must provide the authoritative minimum deal size");
    }

    private static void CryptoMetadataEnrichmentDeduplicatesRequestsAndCachesSuccessfulDetails()
    {
        var requests = 0;
        Task<MarketInstrument?> Load(string epic, CancellationToken _)
        {
            requests++;
            return Task.FromResult<MarketInstrument?>(CryptoMetadataFixture(epic, "ETH/USD", "Ethereum / US Dollar", "USD"));
        }

        var enricher = CreateCryptoMetadataEnricher(Load, maximumConcurrency: 2);
        var duplicateRows = new[]
        {
            CryptoMetadataFixture("ETH-USD", "ETH/USD", "Ethereum / US Dollar", ""),
            CryptoMetadataFixture("eth-usd", "ETH/USD", "Ethereum / US Dollar", ""),
        };

        var first = EnrichCryptoMetadata(enricher, duplicateRows);
        var second = EnrichCryptoMetadata(enricher, [CryptoMetadataFixture("ETH-USD", "ETH/USD", "Ethereum / US Dollar", "")]);

        AssertEqual(1, requests, "duplicate crypto summaries and a repeated universe load must share one successful detail request");
        AssertSequence(first.Select(item => item.Currency), "USD", "USD");
        AssertEqual("USD", second.Single().Currency, "cached detail metadata must restore the quote currency");
    }

    private static void CryptoMetadataEnrichmentToleratesFailuresAndReappliesOpenableFiltering()
    {
        Task<MarketInstrument?> Load(string epic, CancellationToken _) => epic switch
        {
            "CLOSE-ONLY" => Task.FromResult<MarketInstrument?>(CryptoMetadataFixture("CLOSE-ONLY", "CLOSE/USD", "Close only crypto", "USD", status: "CLOSE_ONLY")),
            "UNAVAILABLE" => Task.FromException<MarketInstrument?>(new HttpRequestException("fixture detail failure")),
            _ => throw new InvalidOperationException($"Unexpected fixture epic {epic}"),
        };

        var enriched = EnrichCryptoMetadata(
            [
                CryptoMetadataFixture("CLOSE-ONLY", "CLOSE/USD", "Close only crypto", "", status: "TRADEABLE"),
                CryptoMetadataFixture("UNAVAILABLE", "UNAVAILABLE/USD", "Unavailable crypto", "", status: "TRADEABLE"),
            ],
            Load);

        AssertEqual("USD", enriched.Single(item => item.Epic == "CLOSE-ONLY").Currency, "successful detail metadata must be retained before filtering");
        AssertEqual("CLOSE_ONLY", enriched.Single(item => item.Epic == "CLOSE-ONLY").Status, "detail market status must replace the summary status");
        AssertEqual("", enriched.Single(item => item.Epic == "UNAVAILABLE").Currency, "a failed detail request must leave its summary safely unresolved");
        var accepted = TerminalUniverseLoadPolicy.NormalizeApiFallback(
            TerminalUniverseKind.Crypto,
            enriched,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        AssertSequence(accepted.Select(item => item.Epic), "UNAVAILABLE");
    }

    private static void CryptoMetadataEnrichmentHonorsCancellationReportsProgressAndBoundsConcurrency()
    {
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        var cancellationObserved = false;
        try
        {
            EnrichCryptoMetadata(
                [CryptoMetadataFixture("CANCELLED", "CANCEL/USD", "Cancelled crypto", "")],
                (_, _) => Task.FromResult<MarketInstrument?>(CryptoMetadataFixture("CANCELLED", "CANCEL/USD", "Cancelled crypto", "USD")),
                cancellationToken: cancelled.Token);
        }
        catch (OperationCanceledException)
        {
            cancellationObserved = true;
        }
        AssertTrue(cancellationObserved, "crypto metadata enrichment must propagate a requested cancellation");

        var running = 0;
        var peak = 0;
        var progress = new List<(int Completed, int Total)>();
        async Task<MarketInstrument?> Load(string epic, CancellationToken cancellationToken)
        {
            var active = Interlocked.Increment(ref running);
            lock (progress) peak = Math.Max(peak, active);
            try
            {
                await Task.Delay(20, cancellationToken);
                return CryptoMetadataFixture(epic, $"{epic}/USD", $"{epic} crypto", "USD");
            }
            finally
            {
                Interlocked.Decrement(ref running);
            }
        }

        var summaries = Enumerable.Range(1, 5)
            .Select(index => CryptoMetadataFixture($"CRYPTO-{index}", $"CRYPTO{index}/USD", $"Crypto {index}", ""))
            .ToList();
        var enriched = EnrichCryptoMetadata(
            summaries,
            Load,
            maximumConcurrency: 2,
            progress: point =>
            {
                lock (progress) progress.Add(point);
            });

        AssertEqual(5, enriched.Count, "all successful detail rows must remain available");
        AssertTrue(peak <= 2, "crypto metadata requests must not exceed the configured bounded concurrency");
        AssertEqual((0, 5), progress.First(), "crypto metadata progress must start before requests complete");
        AssertEqual((5, 5), progress.Last(), "crypto metadata progress must complete after all details settle");
    }

    private static void CryptoMetadataEnrichmentPacesRequestsAndRetriesRateLimits()
    {
        var starts = new List<long>();
        var attempts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var clock = Stopwatch.StartNew();
        Task<MarketInstrument?> Load(string epic, CancellationToken _)
        {
            lock (starts) starts.Add(clock.ElapsedMilliseconds);
            int attempt;
            lock (attempts)
            {
                attempts[epic] = attempts.GetValueOrDefault(epic) + 1;
                attempt = attempts[epic];
            }
            if (epic == "RATE-LIMITED" && attempt == 1)
            {
                throw new CapitalApiException(HttpStatusCode.TooManyRequests, "Too Many Requests", "{}");
            }
            return Task.FromResult<MarketInstrument?>(CryptoMetadataFixture(epic, $"{epic}/USD", epic, "USD"));
        }

        var enricher = new CryptoMarketMetadataEnricher(
            Load,
            maximumConcurrency: 4,
            minimumRequestSpacing: TimeSpan.FromMilliseconds(20),
            maximumAttempts: 2);
        var enriched = EnrichCryptoMetadata(
            enricher,
            [
                CryptoMetadataFixture("FIRST", "FIRST/USD", "First", ""),
                CryptoMetadataFixture("RATE-LIMITED", "RATE/USD", "Rate limited", ""),
                CryptoMetadataFixture("THIRD", "THIRD/USD", "Third", ""),
            ]);

        AssertEqual(3, enriched.Count(item => item.Currency == "USD"), "a rate-limited crypto detail must be retried and enriched");
        AssertEqual(2, attempts["RATE-LIMITED"], "HTTP 429 must use the bounded metadata retry path");
        var orderedStarts = starts.OrderBy(value => value).ToList();
        AssertTrue(
            orderedStarts.Zip(orderedStarts.Skip(1), (left, right) => right - left).All(delta => delta >= 15),
            "parallel metadata workers must preserve a global request-start interval");
    }

    private static CryptoMarketMetadataEnricher CreateCryptoMetadataEnricher(
        Func<string, CancellationToken, Task<MarketInstrument?>> loadDetails,
        int maximumConcurrency = 4)
        => new(loadDetails, maximumConcurrency, TimeSpan.Zero);

    private static IReadOnlyList<MarketInstrument> EnrichCryptoMetadata(
        IReadOnlyList<MarketInstrument> summaries,
        Func<string, CancellationToken, Task<MarketInstrument?>> loadDetails,
        int maximumConcurrency = 4,
        Action<(int Completed, int Total)>? progress = null,
        CancellationToken cancellationToken = default) =>
        EnrichCryptoMetadata(CreateCryptoMetadataEnricher(loadDetails, maximumConcurrency), summaries, progress, cancellationToken);

    private static IReadOnlyList<MarketInstrument> EnrichCryptoMetadata(
        CryptoMarketMetadataEnricher enricher,
        IReadOnlyList<MarketInstrument> summaries,
        Action<(int Completed, int Total)>? progress = null,
        CancellationToken cancellationToken = default)
    {
        Action<int, int>? report = progress is null ? null : (completed, total) => progress((completed, total));
        return enricher.EnrichAsync(summaries, report, cancellationToken).GetAwaiter().GetResult();
    }

    private static MarketInstrument CryptoMetadataFixture(
        string epic,
        string symbol,
        string name,
        string currency,
        string status = "TRADEABLE",
        decimal? minDealSize = null) =>
        new()
        {
            Epic = epic,
            Symbol = symbol,
            Name = name,
            Type = "CRYPTOCURRENCIES",
            Currency = currency,
            Status = status,
            LotSize = 1m,
            MinDealSize = minDealSize,
            MinSizeIncrement = minDealSize,
            MarginFactor = 50m,
            MarginFactorUnit = "PERCENTAGE",
            Bid = 1m,
            Offer = 2m,
        };

    private static void CryptoGroupingDerivesExplicitQuoteCurrencyWhenCapitalOmitsCurrency()
    {
        var normalized = TerminalCryptoUniverseGrouping.Normalize(
        [
            new MarketInstrument
            {
                Epic = "ETHUSD",
                Name = "Ethereum/USD",
                Symbol = "",
                Type = "CRYPTOCURRENCIES",
                Currency = "",
            },
            new MarketInstrument
            {
                Epic = "BTCUSD",
                Name = "Bitcoin/USD",
                Symbol = "",
                Type = "CRYPTOCURRENCIES",
                Currency = "",
            },
            new MarketInstrument
            {
                Epic = "UNKNOWN",
                Name = "Unlabelled coin",
                Symbol = "",
                Type = "CRYPTOCURRENCIES",
                Currency = "",
            },
        ]);

        AssertEqual("Crypto / USD / All", normalized[0].Group, "explicit Ethereum/USD label must derive USD quote currency");
        AssertEqual("Crypto / USD / All", normalized[1].Group, "explicit Bitcoin/USD label must derive USD quote currency");
        AssertEqual("Crypto / Currency / All", normalized[2].Group, "unlabelled Crypto must retain the missing-currency fallback");
    }

    private static void CapComTerminalExposesGroupedCryptoUniverse()
    {
        var xaml = File.ReadAllText(SourcePath("desktop", "CAPETF.Desktop", "CapComTerminalWindow.xaml"));
        if (!xaml.Contains("<ComboBoxItem Content=\"Crypto\"/>", StringComparison.Ordinal))
        {
            throw new Exception("terminal universe selector must expose Crypto");
        }

        var normalize = typeof(SyntheticTerminalWorkspace).Assembly
            .GetType("CAPETF.Desktop.TerminalCryptoUniverseGrouping")?
            .GetMethod("Normalize");
        if (normalize is null) throw new Exception("terminal crypto universe must normalize quote-currency groups");
        var crypto = (IReadOnlyList<MarketInstrument>)normalize.Invoke(null,
        [
            new[]
            {
                new MarketInstrument { Epic = "CRYPTO.BTCUSD.CFD.IP", Type = "CRYPTOCURRENCIES", Currency = "USD" },
                new MarketInstrument { Epic = "CRYPTO.BTCEUR.CFD.IP", Type = "CRYPTOCURRENCIES", Currency = "EUR" },
                new MarketInstrument { Epic = "CRYPTO.BTCUNKNOWN.CFD.IP", Type = "CRYPTOCURRENCIES" },
            },
        ])!;

        AssertEqual("Crypto / USD / All", crypto[0].Group, "crypto USD quote group");
        AssertEqual("Crypto / EUR / All", crypto[1].Group, "crypto EUR quote group");
        AssertEqual("Crypto / Currency / All", crypto[2].Group, "crypto missing quote fallback group");
    }

    private static void TerminalUniverseUiCoordinatorRestoresKnownEtfExclusionAfterCrypto()
    {
        var coordinator = new TerminalUniverseUiCoordinator();
        var catalogLoads = 0;
        coordinator.EnsureEtfCatalogFor(TerminalUniverseKind.Crypto, () => catalogLoads++);
        AssertEqual(0, catalogLoads, "crypto selection must not load the ETF catalog");
        coordinator.EnsureEtfCatalogFor(TerminalUniverseKind.Stocks, () => catalogLoads++);
        AssertEqual(1, catalogLoads, "switching from crypto to stocks must initialize the ETF catalog");

        var knownEtfs = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "KNOWN-ETF" };
        var candidates = new[]
        {
            new MarketInstrument { Epic = "KNOWN-ETF", Type = "SHARES", Status = "CLOSED" },
            new MarketInstrument { Epic = "ORDINARY-SHARE", Type = "SHARES", Status = "CLOSED" },
        }.Where(item => TerminalUniverse.Accepts(TerminalUniverseKind.Stocks, item, knownEtfs)).ToList();
        AssertEqual(1, candidates.Count, "known ETF shares must be excluded after a stock universe switch");
        AssertEqual("ORDINARY-SHARE", candidates[0].Epic, "ordinary shares must remain stock candidates");
    }

    private static void SavedAndOpenBasketsRestoreTheirUniverseInBothDirections()
    {
        var now = DateTimeOffset.Parse("2026-08-01T00:00:00Z");
        var knownEtfs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ETF-A", "ETF-B", "ETF-C",
        };
        SavedSyntheticBasket Saved(
            string id,
            string block,
            SyntheticStrategyKind strategy,
            IReadOnlyList<string> epics,
            TerminalUniverseKind? universe = null) =>
            new(
                id,
                id,
                $"SYN-{id}",
                block,
                strategy,
                now,
                now,
                epics.Select(epic => new SavedSyntheticComponent(epic, epic, "USD", 100m / epics.Count, 1m, 100m)).ToArray(),
                UniverseKind: universe);

        SyntheticExecutionRecord Execution(
            string id,
            IReadOnlyList<string> epics,
            decimal? basketQuantity = null,
            TerminalUniverseKind? universe = null) =>
            new(
                id,
                $"ticket-{id}",
                $"SYN-{id}",
                "BUY",
                300m,
                60m,
                "USD",
                now,
                now,
                SyntheticExecutionState.Open,
                epics.Select(epic => new SyntheticExecutionLegRecord(
                    epic, "BUY", 1m, 100m, basketQuantity ?? 1m, 100m, 20m, "USD",
                    SyntheticExecutionLegState.Open, "", $"deal-{epic}", "", 100m, "", now, now, null, now)).ToArray(),
                BasketQuantity: basketQuantity,
                UniverseKind: universe);

        var savedStock = Saved(
            "saved-stock", "US / USD / Technology", SyntheticStrategyKind.DipInsideUptrend,
            ["STOCK-A", "STOCK-B", "STOCK-C"], TerminalUniverseKind.Stocks);
        var legacySavedEtf = Saved(
            "saved-etf", "US / USD / All", SyntheticStrategyKind.MeanReversion,
            ["ETF-A", "ETF-B", "ETF-C"]);
        var legacySavedCrypto = Saved(
            "saved-crypto", "Crypto / USD / All", SyntheticStrategyKind.ManualFormula,
            ["ETH", "BTC"]);
        var stockExecution = Execution(
            "open-stock", ["STOCK-A", "STOCK-B", "STOCK-C"], universe: TerminalUniverseKind.Stocks);
        var etfExecution = Execution(
            "open-etf", ["ETF-A", "ETF-B", "ETF-C"], universe: TerminalUniverseKind.ETFs);
        var cryptoExecution = Execution(
            "open-crypto", ["ETH", "BTC"], basketQuantity: 1m, universe: TerminalUniverseKind.Crypto);

        AssertEqual(TerminalUniverseKind.Stocks, SyntheticBasketUniverseResolver.Resolve(savedStock, knownEtfs),
            "persisted stock universe identity");
        AssertEqual(TerminalUniverseKind.ETFs, SyntheticBasketUniverseResolver.Resolve(legacySavedEtf, knownEtfs),
            "legacy ETF universe identity uses known ETF membership");
        AssertEqual(TerminalUniverseKind.Crypto, SyntheticBasketUniverseResolver.Resolve(legacySavedCrypto, knownEtfs),
            "legacy manual Crypto block identity");
        AssertEqual(TerminalUniverseKind.Stocks, SyntheticBasketUniverseResolver.Resolve(stockExecution, knownEtfs),
            "open stock execution universe identity");
        AssertEqual(TerminalUniverseKind.ETFs, SyntheticBasketUniverseResolver.Resolve(etfExecution, knownEtfs),
            "open ETF execution universe identity");
        AssertEqual(TerminalUniverseKind.Crypto, SyntheticBasketUniverseResolver.Resolve(cryptoExecution, knownEtfs),
            "open manual crypto execution universe identity");

        var coordinator = new TerminalUniverseUiCoordinator();
        void AssertTransition(TerminalUniverseKind current, TerminalUniverseKind target, string label)
        {
            var selected = current;
            TerminalUniverseKind? loaded = null;
            var clears = 0;
            coordinator.EnsureActiveAsync(
                    current,
                    target,
                    [],
                    knownEtfs,
                    value => selected = value,
                    () =>
                    {
                        clears++;
                        return Task.CompletedTask;
                    },
                    value =>
                    {
                        loaded = value;
                        return Task.CompletedTask;
                    })
                .GetAwaiter().GetResult();
            AssertEqual(target, selected, $"{label} selects the restored universe");
            AssertEqual<TerminalUniverseKind?>(target, loaded, $"{label} loads the restored universe before leg resolution");
            AssertEqual(1, clears, $"{label} clears the prior universe state once");
        }

        AssertTransition(TerminalUniverseKind.Crypto, SyntheticBasketUniverseResolver.Resolve(savedStock, knownEtfs), "Crypto to saved Stocks");
        AssertTransition(TerminalUniverseKind.Crypto, SyntheticBasketUniverseResolver.Resolve(legacySavedEtf, knownEtfs), "Crypto to saved ETFs");
        AssertTransition(TerminalUniverseKind.Crypto, SyntheticBasketUniverseResolver.Resolve(stockExecution, knownEtfs), "Crypto to open Stocks");
        AssertTransition(TerminalUniverseKind.Crypto, SyntheticBasketUniverseResolver.Resolve(etfExecution, knownEtfs), "Crypto to open ETFs");
        AssertTransition(TerminalUniverseKind.Stocks, SyntheticBasketUniverseResolver.Resolve(legacySavedCrypto, knownEtfs), "Stocks to saved Crypto");
        AssertTransition(TerminalUniverseKind.ETFs, SyntheticBasketUniverseResolver.Resolve(cryptoExecution, knownEtfs), "ETFs to open Crypto");

        var legacyJson = JsonSerializer.Serialize(legacySavedEtf, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        });
        var deserializedLegacy = JsonSerializer.Deserialize<SavedSyntheticBasket>(legacyJson)
            ?? throw new Exception("legacy saved basket JSON should deserialize");
        AssertEqual<TerminalUniverseKind?>(null, deserializedLegacy.UniverseKind,
            "legacy saved JSON without UniverseKind remains compatible");
        AssertEqual(TerminalUniverseKind.ETFs, SyntheticBasketUniverseResolver.Resolve(deserializedLegacy, knownEtfs),
            "legacy saved JSON still restores the ETF universe");
    }

    private static void LegacyUniverseResolutionProbesUncataloguedEtfsAndRejectsUnresolvedRecords()
    {
        var now = DateTimeOffset.Parse("2026-08-01T00:00:00Z");
        var knownEtfs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var epics = new[] { "API-ETF-A", "API-ETF-B", "API-ETF-C" };
        var saved = new SavedSyntheticBasket(
            "legacy-api-etf",
            "Legacy API ETF",
            "SYN-API-ETF",
            "US / USD / All",
            SyntheticStrategyKind.MeanReversion,
            now,
            now,
            epics.Select(epic => new SavedSyntheticComponent(epic, epic, "USD", 100m / 3m, 1m, 100m)).ToArray());
        var execution = new SyntheticExecutionRecord(
            "legacy-api-etf-execution",
            "legacy-api-etf-ticket",
            "SYN-API-ETF",
            "BUY",
            300m,
            60m,
            "USD",
            now,
            now,
            SyntheticExecutionState.Open,
            epics.Select(epic => new SyntheticExecutionLegRecord(
                epic, "BUY", 1m, 100m, 1m, 100m, 20m, "USD",
                SyntheticExecutionLegState.Open, "", $"deal-{epic}", "", 100m, "", now, now, null, now)).ToArray());
        var caches = new Dictionary<TerminalUniverseKind, IReadOnlyList<MarketInstrument>>
        {
            [TerminalUniverseKind.Crypto] =
            [
                new MarketInstrument { Epic = "BTCUSD", Type = "CRYPTOCURRENCIES", Status = "TRADEABLE" },
            ],
        };
        var probes = new List<string>();
        Task<MarketInstrument?> Probe(string epic, CancellationToken _)
        {
            probes.Add(epic);
            return Task.FromResult<MarketInstrument?>(new MarketInstrument
            {
                Epic = epic,
                Name = $"{epic} Exchange Traded Fund",
                Type = "ETF",
                Currency = "USD",
                Status = "TRADEABLE",
            });
        }

        var typedCache = new Dictionary<TerminalUniverseKind, IReadOnlyList<MarketInstrument>>
        {
            [TerminalUniverseKind.Stocks] = epics.Select(epic => new MarketInstrument
            {
                Epic = epic,
                Type = "ETF",
                Status = "TRADEABLE",
            }).ToArray(),
        };
        var typedCacheResolution = SyntheticBasketUniverseResolver.ResolveAsync(
            saved,
            knownEtfs,
            typedCache,
            (_, _) => throw new Exception("typed cache resolution must not probe Capital metadata"))
            .GetAwaiter().GetResult();
        AssertEqual(TerminalUniverseKind.ETFs, typedCacheResolution.Universe,
            "Capital ETF type metadata overrides a stale non-ETF cache label");

        var savedResolution = SyntheticBasketUniverseResolver.ResolveAsync(
            saved, knownEtfs, caches, Probe).GetAwaiter().GetResult();
        AssertEqual(TerminalUniverseKind.ETFs, savedResolution.Universe,
            "an uncatalogued Capital ETF type must restore a legacy saved basket to ETFs");
        AssertSequence(savedResolution.Instruments.Select(instrument => instrument.Epic), epics);
        AssertSequence(probes, epics);
        var coordinator = new TerminalUniverseUiCoordinator();
        var selectedUniverse = TerminalUniverseKind.Crypto;
        TerminalUniverseKind? loadedUniverse = null;
        coordinator.EnsureActiveAsync(
                TerminalUniverseKind.Crypto,
                savedResolution.Universe,
                caches[TerminalUniverseKind.Crypto],
                knownEtfs,
                universe => selectedUniverse = universe,
                () => Task.CompletedTask,
                universe =>
                {
                    loadedUniverse = universe;
                    return Task.CompletedTask;
                })
            .GetAwaiter().GetResult();
        AssertEqual(TerminalUniverseKind.ETFs, selectedUniverse,
            "uncatalogued ETF restore switches the selector away from Crypto");
        AssertEqual<TerminalUniverseKind?>(TerminalUniverseKind.ETFs, loadedUniverse,
            "uncatalogued ETF restore loads ETFs before resolving legs");

        probes.Clear();
        var executionResolution = SyntheticBasketUniverseResolver.ResolveAsync(
            execution, knownEtfs, caches, Probe).GetAwaiter().GetResult();
        AssertEqual(TerminalUniverseKind.ETFs, executionResolution.Universe,
            "an uncatalogued Capital ETF type must restore a legacy open execution to ETFs");
        AssertSequence(executionResolution.Instruments.Select(instrument => instrument.Epic), epics);
        AssertSequence(probes, epics);

        var legacyExecutionJson = JsonSerializer.Serialize(execution, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        });
        var deserializedExecution = JsonSerializer.Deserialize<SyntheticExecutionRecord>(legacyExecutionJson)
            ?? throw new Exception("legacy execution JSON should deserialize");
        AssertEqual<TerminalUniverseKind?>(null, deserializedExecution.UniverseKind,
            "legacy execution JSON without UniverseKind remains compatible");

        var ambiguousCaches = new Dictionary<TerminalUniverseKind, IReadOnlyList<MarketInstrument>>
        {
            [TerminalUniverseKind.Stocks] = epics.Select(epic => new MarketInstrument { Epic = epic }).ToArray(),
            [TerminalUniverseKind.ETFs] = epics.Select(epic => new MarketInstrument { Epic = epic }).ToArray(),
        };
        AssertThrows<InvalidOperationException>(
            () => SyntheticBasketUniverseResolver.ResolveAsync(
                saved,
                knownEtfs,
                ambiguousCaches,
                (_, _) => Task.FromResult<MarketInstrument?>(null)).GetAwaiter().GetResult(),
            "ambiguous",
            "legacy epics present in multiple universe caches");

        AssertThrows<InvalidOperationException>(
            () => SyntheticBasketUniverseResolver.ResolveAsync(
                execution,
                knownEtfs,
                new Dictionary<TerminalUniverseKind, IReadOnlyList<MarketInstrument>>(),
                (_, _) => Task.FromResult<MarketInstrument?>(null)).GetAwaiter().GetResult(),
            "missing universe metadata",
            "legacy execution with no cache or Capital metadata");
    }

    private static void TerminalUniverseUiCoordinatorCachesUniversesSeparately()
    {
        var coordinator = new TerminalUniverseUiCoordinator();
        var stocks = new[] { new MarketInstrument { Epic = "STOCK", Type = "SHARES" } };
        var crypto = new[] { new MarketInstrument { Epic = "CRYPTO.BTCUSD.CFD.IP", Type = "CRYPTOCURRENCIES" } };
        coordinator.Cache(TerminalUniverseKind.Stocks, stocks);
        coordinator.Cache(TerminalUniverseKind.Crypto, crypto);

        AssertTrue(coordinator.TryGetCached(TerminalUniverseKind.Stocks, out var cachedStocks), "stock universe must have its own cache entry");
        AssertEqual("STOCK", cachedStocks[0].Epic, "stock cache contents");

        AssertTrue(coordinator.TryGetCached(TerminalUniverseKind.Crypto, out var cachedCrypto), "crypto universe must have its own cache entry");
        AssertEqual("CRYPTO.BTCUSD.CFD.IP", cachedCrypto[0].Epic, "crypto cache contents");
    }

    private static void TerminalUniverseUiCoordinatorClearsBeforeAFailedSwitchLoad()
    {
        var coordinator = new TerminalUniverseUiCoordinator();
        var clears = 0;
        try
        {
            coordinator.SwitchAsync(
                (Func<Task>)(() =>
                {
                    clears++;
                    return Task.CompletedTask;
                }),
                (Func<Task>)(() => Task.FromException(new InvalidOperationException("Capital API unavailable"))))
                .GetAwaiter().GetResult();
            throw new Exception("failed universe load must remain observable to the caller");
        }
        catch (InvalidOperationException ex) when (ex.Message == "Capital API unavailable")
        {
        }

        AssertEqual(1, clears, "switching universes must clear the prior basket and chart before a failed load");
    }

    private static void TerminalUniverseUiCoordinatorBuildsBlocksAndSeedsForTheActiveUniverse()
    {
        var coordinator = new TerminalUniverseUiCoordinator();
        var stocks = new[]
        {
            new MarketInstrument { Epic = "STOCK", Name = "Ordinary Share", Symbol = "STOCK", Type = "SHARES", Region = "US", Currency = "USD", Sector = "All" },
        };
        var crypto = new[]
        {
            new MarketInstrument { Epic = "BTCUSD", Name = "Bitcoin", Symbol = "BTC", Type = "CRYPTOCURRENCIES", Region = "Crypto", Currency = "USD", Sector = "All" },
            new MarketInstrument { Epic = "BTCEUR", Name = "Bitcoin EUR", Symbol = "BTCEUR", Type = "CRYPTOCURRENCIES", Region = "Crypto", Currency = "EUR", Sector = "All" },
        };
        coordinator.BuildControls(stocks);
        var controls = coordinator.BuildControls(crypto);
        var blocks = controls.Blocks;
        var seeds = controls.SeedOptions;

        AssertEqual("Crypto / EUR / All", blocks[0], "new universe blocks must be rebuilt from the active universe");
        AssertTrue(seeds.Any(seed => seed.Contains("BTCEUR | Bitcoin EUR | Crypto / EUR / All", StringComparison.Ordinal)),
            "new universe seed options must include instruments from the selected active block");
        AssertTrue(seeds.All(seed => !seed.Contains("STOCK", StringComparison.Ordinal)),
            "new universe seed options must not retain instruments from the prior universe");
    }

    private static void TerminalUniverseAccumulatorPublishesCachedSnapshotBeforeDiscovery()
    {
        var accumulator = new TerminalUniverseAccumulator(TerminalUniverseKind.Stocks);
        var snapshot = accumulator.PublishCached(
        [
            new MarketInstrument { Epic = "CACHE-A", Name = "Cached Alpha", Symbol = "CA", Type = "SHARES", Region = "US", Currency = "USD", Sector = "Technology" },
        ]);

        AssertEqual(1, snapshot.Instruments.Count, "cached instruments must publish immediately before API discovery");
        AssertEqual("CACHE-A", snapshot.Instruments[0].Epic, "cached snapshot keeps the cached epic");
        AssertEqual(TerminalUniverseStage.Cached, snapshot.Progress.Stage, "first snapshot must identify the cache stage");
        AssertTrue(!snapshot.Progress.IsComplete, "cached publication must not claim discovery is complete");
    }

    private static void TerminalUniverseAccumulatorMergesApiBatchesDeterministically()
    {
        var accumulator = new TerminalUniverseAccumulator(TerminalUniverseKind.Stocks);
        accumulator.PublishCached(
        [
            new MarketInstrument { Epic = "A", Name = "Zulu", Symbol = "A", Type = "SHARES", Region = "US", Currency = "USD", Sector = "Technology", Status = "CLOSED" },
            new MarketInstrument { Epic = "C", Name = "Charlie", Symbol = "C", Type = "SHARES", Region = "US", Currency = "USD", Sector = "Technology" },
        ]);

        var snapshot = accumulator.MergeDiscoveryBatch(
        [
            new MarketInstrument { Epic = "A", Name = "Alpha", Symbol = "A", Type = "SHARES", Region = "US", Currency = "USD", Sector = "Technology", Status = "TRADEABLE" },
            new MarketInstrument { Epic = "B", Name = "Bravo", Symbol = "B", Type = "SHARES", Region = "US", Currency = "USD", Sector = "Technology" },
        ], totalDiscovered: 4, isComplete: false);

        AssertEqual(3, snapshot.Instruments.Count, "API discoveries must deduplicate matching epics");
        AssertEqual("A", snapshot.Instruments[0].Epic, "merged snapshots must use a deterministic name and epic ordering");
        AssertEqual("B", snapshot.Instruments[1].Epic, "merged snapshots must use a deterministic name and epic ordering");
        AssertEqual("C", snapshot.Instruments[2].Epic, "merged snapshots must use a deterministic name and epic ordering");
        AssertEqual("TRADEABLE", snapshot.Instruments[0].Status, "new API data must replace stale cache metadata for duplicate epics");
    }

    private static void TerminalUniverseAccumulatorPreservesCurrentSelection()
    {
        var accumulator = new TerminalUniverseAccumulator(TerminalUniverseKind.Stocks);
        var snapshot = accumulator.PublishCached(
        [
            new MarketInstrument { Epic = "SAPd", Name = "SAP", Symbol = "SAP", Type = "SHARES", Region = "Europe", Currency = "EUR", Sector = "Technology" },
            new MarketInstrument { Epic = "AMD", Name = "Advanced Micro Devices", Symbol = "AMD", Type = "SHARES", Region = "US", Currency = "USD", Sector = "Technology" },
        ]);

        var preserved = accumulator.PreserveSelection(
            new TerminalUniverseSelection("Europe / EUR / Technology", "SAPd | SAP | Europe / EUR / Technology"),
            snapshot);

        AssertEqual("Europe / EUR / Technology", preserved.Block, "a still-valid block must remain selected after a staged publish");
        AssertEqual("SAPd | SAP | Europe / EUR / Technology", preserved.SeedText, "a still-valid seed must remain selected after a staged publish");
    }

    private static void TerminalUniverseAccumulatorReportsStagedProgress()
    {
        var accumulator = new TerminalUniverseAccumulator(TerminalUniverseKind.Stocks);
        accumulator.PublishCached([]);
        var partial = accumulator.MergeDiscoveryBatch(
        [new MarketInstrument { Epic = "A", Name = "Alpha", Type = "SHARES" }], totalDiscovered: 3, isComplete: false);
        var complete = accumulator.MergeDiscoveryBatch(
        [new MarketInstrument { Epic = "B", Name = "Bravo", Type = "SHARES" }], totalDiscovered: 3, isComplete: true);

        AssertEqual(TerminalUniverseStage.Discovering, partial.Progress.Stage, "partial API batches must report discovery progress");
        AssertEqual(1, partial.Progress.Discovered, "partial progress must report received API instruments");
        AssertEqual(3, partial.Progress.TotalDiscovered, "partial progress must retain the discovery total");
        AssertEqual(TerminalUniverseStage.Complete, complete.Progress.Stage, "final API batch must report completion");
        AssertTrue(complete.Progress.IsComplete, "final API batch must be marked complete");
    }

    private static void TerminalUniverseCacheRoundTripsMergedSnapshots()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"capetf-universe-cache-{Guid.NewGuid():N}");
        try
        {
            var cache = new TerminalUniverseCache(directory);
            cache.Save(TerminalUniverseKind.Stocks,
            [
                new MarketInstrument { Epic = "A", Name = "Alpha", Symbol = "A", Type = "SHARES", Region = "US", Currency = "USD", Sector = "Technology", Status = "TRADEABLE" },
            ]);
            var loaded = cache.Load(TerminalUniverseKind.Stocks);

            AssertEqual(1, loaded.Count, "the merged universe cache must persist staged API discoveries");
            AssertEqual("A", loaded[0].Epic, "the merged universe cache must retain the epic key");
            AssertEqual("TRADEABLE", loaded[0].Status, "the merged universe cache must retain current API metadata");
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    private static void CapComTerminalProgressivelyDiscoversUniversesWithoutBlockingControls()
    {
        var source = File.ReadAllText(SourcePath("desktop", "CAPETF.Desktop", "CapComTerminalWindow.xaml.cs"));
        foreach (var required in new[]
        {
            "TerminalUniverseCache",
            "StartUniverseDiscoveryAsync",
            "RefreshUniverseInBackgroundAsync",
            "MergeDiscoveryBatch",
            "batchSize = 100",
            "Task.Delay(TimeSpan.FromMilliseconds(40)",
            "PreserveSelection(CaptureUniverseSelection(), snapshot)",
            "waitForDiscovery: false",
        })
        {
            if (!source.Contains(required, StringComparison.Ordinal))
            {
                throw new Exception($"progressive universe loading missing {required}");
            }
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

    private static void EtfMetadataMergeTreatsOtherRegionAsPlaceholder()
    {
        var merged = EtfMetadataMerger.Merge(
            new MarketInstrument { Epic = "ETF-CFD", Type = "ETF", Country = "United States", Region = "Other", Currency = "USD", Sector = "All" },
            new MarketInstrument { Epic = "ETF-CFD", Type = "SHARES" });

        AssertEqual("US", merged.Region, "cached Other must not suppress the independent US country fallback");
        AssertEqual("US / USD / All", merged.Group, "the independent region placeholder fallback must flow into ETF grouping");
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
        var ensureConnection = source.IndexOf("await EnsureConnectedAsync(cancellationToken);", enrichStart, StringComparison.Ordinal);
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

    private static void TerminalPayloadDefinesOneFormulaAsOneSyntheticLot()
    {
        var basket = new SyntheticBasket
        {
            Symbol = "SYN-CRYPTO-ETHBTC-01",
            Block = "Crypto / USD / All",
            Strategy = SyntheticStrategyKind.ManualFormula,
            UniverseKind = TerminalUniverseKind.Crypto,
        };
        basket.Components.Add(new SyntheticComponent(new MarketInstrument
        {
            Epic = "CS.D.ETHUSD.CFD.IP",
            Currency = "USD",
            MinDealSize = 0.001m,
            MinSizeIncrement = 0.001m,
            MaxDealSize = 1000m,
        }, 50m, 0m, 0m) { FormulaMultiplier = 9m });
        basket.Components.Add(new SyntheticComponent(new MarketInstrument
        {
            Epic = "CS.D.BTCUSD.CFD.IP",
            Currency = "USD",
            MinDealSize = 0.0001m,
            MinSizeIncrement = 0.0001m,
            MaxDealSize = 100m,
        }, 50m, 0m, 0m) { FormulaMultiplier = 0.2m });

        var payload = SyntheticTerminalChartPayload.Build(basket);

        AssertEqual(1m, payload.SuggestedBasketQuantity,
            "one complete displayed formula must be one synthetic lot");
        var html = File.ReadAllText(SourcePath("desktop", "CAPETF.Desktop", "Assets", "synthetic-terminal.html"));
        AssertTrue(html.Contains("Synthetic lots", StringComparison.Ordinal),
            "the terminal must name the user input in synthetic lots");
        AssertTrue(html.Contains("identityChanged", StringComparison.Ordinal),
            "a timeframe refresh must not overwrite a user's quantity for the same synthetic identity");
        AssertTrue(html.Contains("id=\"quantity\" type=\"number\" value=\"1\" min=\"1\" step=\"1\"", StringComparison.Ordinal),
            "synthetic lot input must default to one positive whole lot");
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
        AssertEqual("stale", SyntheticTerminalChartPayload.QuoteStatus(now.AddSeconds(1), now), "future component timestamps must not be considered fresh");
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
        AssertNear(12.1m, result.Tick.Candle.Close, "terminal tick must contain the live synthetic bid/ask midpoint");
        AssertEqual(1, result.Tick.ComponentQuotes.Count, "terminal tick should carry component quote metadata without full chart history");
        AssertEqual(DateTimeOffset.Parse("2026-07-25T00:01:00Z"), result.Tick.ComponentQuotes[0].QuoteTimestamp,
            "stream quote timestamp must flow into the terminal tick");
    }

    private static void LiveWeeklyQuoteStartsCurrentCandleInsteadOfRepaintingPriorWeek()
    {
        var priorWeek = DateTimeOffset.Parse("2026-07-20T04:00:00Z");
        var currentWeekQuote = DateTimeOffset.Parse("2026-07-27T12:15:00Z");
        var basket = new SyntheticBasket { Symbol = "SYN-WEEKLY", Block = "Europe / EUR / All", BasketPrice = 100m };
        basket.Components.Add(new SyntheticComponent(new MarketInstrument { Epic = "SAPD", Price = 100m }, 100m, 0m, 0m)
        {
            SyntheticBaselinePrice = 100m,
            LastAppliedPrice = 100m,
        });
        basket.Candles.Add(new OhlcPoint(priorWeek, 98m, 104m, 96m, 100m));

        var result = SyntheticTerminalLiveUpdate.Apply(
            basket,
            new QuoteUpdate("SAPD", 109m, 111m, 110m, currentWeekQuote),
            timeframe: "Weekly");

        AssertEqual(2, basket.Candles.Count, "a quote in a new week must append the current weekly candle");
        AssertEqual(priorWeek, basket.Candles[0].Time, "live rollover must preserve the prior historical candle");
        AssertEqual(DateTimeOffset.Parse("2026-07-27T00:00:00Z"), basket.Candles[1].Time,
            "the current weekly candle must use the Monday UTC bucket");
        AssertNear(100m, basket.Candles[1].Open, "the new weekly candle must open at the prior synthetic close");
        AssertNear(110m, basket.Candles[1].Close, "the first quote must fill the new weekly candle");
        if (!result.CandleChanged || result.Tick?.Candle is null)
        {
            throw new Exception("weekly rollover must publish the appended candle to the chart");
        }
    }

    private static void LiveCandleCloseTracksCompleteSyntheticQuoteMidpoint()
    {
        var candleTime = DateTimeOffset.Parse("2026-07-28T00:00:00Z");
        var basket = new SyntheticBasket { Symbol = "SYN-ONGOING", BasketPrice = 88m };
        basket.Components.Add(new SyntheticComponent(
            new MarketInstrument { Epic = "LIVE-A", Bid = 79m, Offer = 81m, Price = 80m },
            50m,
            0m,
            0m)
        {
            FormulaMultiplier = 0.5m,
            SyntheticBaselinePrice = 80m,
            LastAppliedPrice = 80m,
        });
        basket.Components.Add(new SyntheticComponent(
            new MarketInstrument { Epic = "LIVE-B", Bid = 102m, Offer = 102m, Price = 102m },
            50m,
            0m,
            0m)
        {
            FormulaMultiplier = 0.5m,
            SyntheticBaselinePrice = 102m,
            LastAppliedPrice = 102m,
        });
        basket.Candles.Add(new OhlcPoint(candleTime, 88m, 89m, 87m, 88m));

        var result = SyntheticTerminalLiveUpdate.Apply(
            basket,
            new QuoteUpdate("LIVE-A", 80m, 82m, 81m, candleTime.AddHours(12)),
            timeframe: "Daily");

        AssertNear(91m, basket.BidPrice ?? 0m, "synthetic bid must include every current component quote");
        AssertNear(92m, basket.AskPrice ?? 0m, "synthetic ask must include every current component quote");
        AssertNear(91.5m, basket.Candles[^1].Close,
            "ongoing candle close must track the midpoint of the complete synthetic bid and ask");
        AssertNear(91.5m, basket.Candles[^1].High, "ongoing candle high must include the live synthetic midpoint");
        if (!result.CandleChanged || result.Tick?.Candle is null)
        {
            throw new Exception("a complete synthetic quote must publish the constructed ongoing candle");
        }
    }

    private static void MarketSnapshotsConstructOngoingCandleBeforeFirstStreamTick()
    {
        var priorDay = DateTimeOffset.Parse("2026-07-27T00:00:00Z");
        var currentDay = DateTimeOffset.Parse("2026-07-28T10:00:00Z");
        var basket = new SyntheticBasket { Symbol = "SYN-SNAPSHOT-BAR", BasketPrice = 88m };
        basket.Components.Add(new SyntheticComponent(
            new MarketInstrument { Epic = "SNAP-A", Bid = 80m, Offer = 82m, Price = 81m },
            50m, 0m, 0m)
        {
            FormulaMultiplier = 0.5m,
            SyntheticBaselinePrice = 81m,
            LastAppliedPrice = 81m,
        });
        basket.Components.Add(new SyntheticComponent(
            new MarketInstrument { Epic = "SNAP-B", Bid = 102m, Offer = 102m, Price = 102m },
            50m, 0m, 0m)
        {
            FormulaMultiplier = 0.5m,
            SyntheticBaselinePrice = 102m,
            LastAppliedPrice = 102m,
        });
        basket.Candles.Add(new OhlcPoint(priorDay, 87m, 89m, 86m, 88m));
        SyntheticQuoteCalculator.Refresh(basket);

        var changed = SyntheticLiveUpdate.ApplyCurrentSyntheticQuote(basket, currentDay, "Daily");

        if (!changed) throw new Exception("complete market snapshots must construct an ongoing candle without waiting for a tick");
        AssertEqual(2, basket.Candles.Count, "snapshot quote must append the current daily candle");
        AssertEqual(DateTimeOffset.Parse("2026-07-28T00:00:00Z"), basket.Candles[^1].Time,
            "snapshot candle must use the selected timeframe bucket");
        AssertNear(88m, basket.Candles[^1].Open, "snapshot candle must open at the prior close");
        AssertNear(91.5m, basket.Candles[^1].Close, "snapshot candle must close at the complete synthetic midpoint");
    }

    private static void IncrementalAndNativeFreshnessUseIndependentUtcNow()
    {
        var observedAt = DateTimeOffset.Parse("2026-07-27T12:00:00Z");
        AssertIncrementalFreshness(observedAt.AddMinutes(-6), observedAt, "stale", "stale components");
        AssertIncrementalFreshness(observedAt.AddSeconds(1), observedAt, "stale", "stale components");
        AssertIncrementalFreshness(observedAt.AddMinutes(-1), observedAt, "fresh", "quotes fresh");
    }

    private static void AssertIncrementalFreshness(
        DateTimeOffset sourceTime,
        DateTimeOffset observedAt,
        string expectedPayloadStatus,
        string expectedNativeStatus)
    {
        var basket = new SyntheticBasket { Symbol = "SYN-FRESHNESS", Block = "US / USD / Technology", BasketPrice = 10m };
        basket.Components.Add(new SyntheticComponent(new MarketInstrument { Epic = "FRESH-A", Price = 10m }, 100m, 0m, 0m)
        {
            SyntheticBaselinePrice = 10m,
        });

        var result = SyntheticTerminalLiveUpdate.Apply(
            basket,
            new QuoteUpdate("FRESH-A", 12m, 12.2m, 12m, sourceTime),
            observedAt);

        if (result.Tick is null) throw new Exception("matching source quote must produce an incremental payload");
        AssertEqual(expectedPayloadStatus, result.Tick.ComponentQuotes[0].QuoteStatus,
            "incremental quote freshness must compare source time with independent UTC now");
        AssertEqual(expectedNativeStatus, SyntheticTerminalChartPayload.BasketQuoteStatus(basket, observedAt),
            "native quote status must use the same independent freshness clock");
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
            "min-height: 32px",
            "flex: 0 0 32px",
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
              const timeScale = options.timeScale || {
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
              const manager = api.createManager({
                chart,
                series,
                container,
                orderedTimes: [100, 200, 300],
                ...(options.managerOptions || {}),
              });
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

            function target(tagName, parentElement = null, attributes = []) {
              return {
                tagName,
                parentElement,
                isContentEditable: false,
                hasAttribute(name) { return attributes.includes(name); },
              };
            }

            function fakeElement(tagName, ownerDocument) {
              const listeners = new Map();
              const classes = new Set(['hidden']);
              return {
                tagName,
                ownerDocument,
                parentElement: null,
                value: '',
                disabled: false,
                listeners,
                classList: {
                  add(name) { classes.add(name); },
                  remove(name) { classes.delete(name); },
                  contains(name) { return classes.has(name); },
                },
                addEventListener(name, handler) { listeners.set(name, handler); },
                removeEventListener(name, handler) {
                  if (listeners.get(name) === handler) listeners.delete(name);
                },
                dispatch(name, event = {}) {
                  listeners.get(name)?.({
                    target: this,
                    preventDefault() {},
                    stopPropagation() {},
                    ...event,
                  });
                },
                focus() { ownerDocument.activeElement = this; },
                contains(node) {
                  for (let current = node; current; current = current.parentElement) {
                    if (current === this) return true;
                  }
                  return false;
                },
                querySelectorAll() { return this.focusables || []; },
              };
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

            test('renders restored anchors at the nearest candle after a timeframe change', () => {
              const exactCoordinates = new Map([[100, 100], [200, 200], [300, 300]]);
              const environment = makeEnvironment({
                timeScale: {
                  timeToCoordinate(time) { return exactCoordinates.get(time) ?? null; },
                  coordinateToTime(x) { return x >= 0 && x <= 800 ? x : null; },
                },
              });
              try {
                environment.manager.setRecords([{
                  ...trend,
                  id: 'cross-timeframe-trend',
                  p1: { time: 110, price: 100 },
                  p2: { time: 290, price: 110 },
                }]);
                assert.deepEqual(render(environment, 1, 1).map(stroke => stroke.path), [[
                  ['moveTo', 100, 100],
                  ['lineTo', 300, 110],
                ]], 'restored drawing must remain visible when its original candle times are absent');
              } finally {
                environment.manager.dispose();
              }
            });

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

            test('cancels pending anchors when the drawing identity changes', () => withEnvironment(environment => {
              const values = new Map([
                ['capcom-terminal-drawings:basket-b', JSON.stringify([{ ...trend, id: 'basket-b-trend' }])],
              ]);
              const storage = { getItem(key) { return values.get(key) ?? null; } };
              environment.manager.setTool('trend');
              dispatchContainer(environment, 'click', pointer(100, 100));

              const identity = api.switchDrawingIdentity(
                environment.manager, storage, 'basket-a', 'basket-b');

              assert.equal(identity, 'basket-b');
              assert.equal(environment.manager.getState().tool, 'cursor');
              assert.deepEqual(environment.manager.getRecords().map(record => record.id), ['basket-b-trend']);
              dispatchContainer(environment, 'click', pointer(300, 110));
              assert.deepEqual(environment.manager.getRecords().map(record => record.id), ['basket-b-trend'],
                'basket A anchor must not complete inside basket B');
            }));

            test('excludes nested chart controls from drawing pointer and click input', () => withEnvironment(environment => {
              const button = target('BUTTON', environment.container);
              const buttonIcon = target('svg', button);
              const styleBar = target('DIV', environment.container, ['data-drawing-ui']);
              const colorInput = target('INPUT', styleBar);

              environment.manager.setTool('hline');
              dispatchContainer(environment, 'click', pointer(150, 150, { target: buttonIcon }));
              dispatchContainer(environment, 'click', pointer(160, 160, { target: colorInput }));
              assert.deepEqual(environment.manager.getRecords(), [], 'nested button/input clicks must not place drawings');

              environment.manager.setTool('brush');
              dispatchContainer(environment, 'pointerdown', pointer(90, 90, { target: buttonIcon }));
              dispatchContainer(environment, 'pointermove', pointer(110, 110, { target: buttonIcon }));
              dispatchContainer(environment, 'pointerup', pointer(110, 110, { target: buttonIcon }));
              assert.deepEqual(environment.manager.getRecords(), [], 'nested button pointer events must not start a draft');

              dispatchContainer(environment, 'pointerdown', pointer(100, 100));
              dispatchContainer(environment, 'pointermove', pointer(130, 130, { target: colorInput }));
              dispatchContainer(environment, 'pointerup', pointer(130, 130, { target: colorInput }));
              dispatchContainer(environment, 'click', pointer(130, 130, { target: colorInput }));
              assert.deepEqual(environment.manager.getRecords(), [], 'releasing over a nested input must cancel the draft');

              dispatchContainer(environment, 'pointerdown', pointer(100, 100));
              dispatchContainer(environment, 'pointermove', pointer(130, 130));
              dispatchContainer(environment, 'pointercancel', pointer(130, 130, { target: buttonIcon }));
              assert.deepEqual(environment.manager.getRecords(), [], 'nested control cancellation must discard the draft');
            }));

            test('context-only updates never publish unchanged records', () => {
              const changedRecords = [];
              const environment = makeEnvironment({
                managerOptions: { onRecordsChanged(records) { changedRecords.push(records); } },
              });
              try {
                environment.manager.setRecords([{ ...trend, id: 'measure-review', type: 'measure' }]);
                changedRecords.length = 0;
                environment.manager.updateContext({ orderedTimes: [100, 200, 300] });
                environment.manager.updateContext({ orderedTimes: [100, 150, 200, 300], pricePrecision: 5 });
                assert.equal(environment.manager.getRecords()[0].bars, 4);
                assert.equal(changedRecords.length, 0, 'live context frames must not trigger persistence callbacks');
              } finally {
                environment.manager.dispose();
              }
            });

            test('dense off-grid brushes use a cached nearest-time index', () => {
              const orderedTimes = Array.from({ length: 30000 }, (_, index) => index * 10).reverse();
              const environment = makeEnvironment({
                timeScale: {
                  timeToCoordinate(time) { return Number(time) % 10 === 0 ? Number(time) : null; },
                  coordinateToTime(x) { return x; },
                },
                managerOptions: { orderedTimes },
              });
              try {
                const points = Array.from({ length: 5000 }, (_, index) => ({
                  time: index * 10 + (index % 2 === 0 ? 4 : 6),
                  price: 100 + index / 100,
                }));
                environment.manager.setRecords([{ ...common, id: 'dense-brush', type: 'brush', points }]);
                const renderedX = [];
                const context = {
                  save() {}, restore() {}, setLineDash() {}, beginPath() {}, moveTo(x) { renderedX.push(x); },
                  lineTo(x) { renderedX.push(x); }, stroke() {}, fillRect() {}, strokeRect() {}, fillText() {},
                  arc() {}, fill() {}, measureText() { return { width: 0 }; },
                };
                const started = process.hrtime.bigint();
                environment.primitive.paneViews()[0].renderer().draw({
                  useBitmapCoordinateSpace(draw) {
                    draw({ context, horizontalPixelRatio: 1, verticalPixelRatio: 1, bitmapSize: { width: 800, height: 400 } });
                  },
                });
                const elapsedMs = Number(process.hrtime.bigint() - started) / 1e6;
                assert.equal(renderedX[0], 0, 'a lower-side off-grid point must snap to the preceding normalized candle');
                assert.equal(renderedX[1], 20, 'an upper-side off-grid point must snap to the following normalized candle');
                assert.ok(elapsedMs < 400, `dense brush nearest-time rendering took ${elapsedMs.toFixed(1)}ms`);
              } finally {
                environment.manager.dispose();
              }
            });

            test('text dialog traps Tab and restores focus for every close path', () => {
              const ownerDocument = { activeElement: null };
              const trigger = fakeElement('BUTTON', ownerDocument);
              const overlay = fakeElement('DIV', ownerDocument);
              const form = fakeElement('FORM', ownerDocument);
              const input = fakeElement('INPUT', ownerDocument);
              const submit = fakeElement('BUTTON', ownerDocument);
              const cancel = fakeElement('BUTTON', ownerDocument);
              form.parentElement = overlay;
              input.parentElement = form;
              submit.parentElement = form;
              cancel.parentElement = form;
              overlay.focusables = [input, submit, cancel];
              let submitted = null;
              let cancelled = 0;
              const dialog = api.createTextDialogController({
                overlay,
                form,
                input,
                cancelButton: cancel,
                onSubmit(text) { submitted = text; },
                onCancel() { cancelled += 1; },
              });

              dialog.open(trigger);
              assert.equal(ownerDocument.activeElement, input);
              ownerDocument.activeElement = cancel;
              let tabPrevented = false;
              overlay.dispatch('keydown', { key: 'Tab', preventDefault() { tabPrevented = true; } });
              assert.equal(ownerDocument.activeElement, input);
              assert.equal(tabPrevented, true);
              ownerDocument.activeElement = input;
              overlay.dispatch('keydown', { key: 'Tab', shiftKey: true, preventDefault() {} });
              assert.equal(ownerDocument.activeElement, cancel);
              input.value = '  Desk note  ';
              form.dispatch('submit');
              assert.equal(submitted, 'Desk note');
              assert.equal(ownerDocument.activeElement, trigger, 'submit must restore tool focus');

              dialog.open(trigger);
              cancel.dispatch('click');
              assert.equal(cancelled, 1);
              assert.equal(ownerDocument.activeElement, trigger, 'cancel button must restore tool focus');

              dialog.open(trigger);
              overlay.dispatch('keydown', { key: 'Escape' });
              assert.equal(cancelled, 2);
              assert.equal(ownerDocument.activeElement, trigger, 'Escape must restore tool focus');
              dialog.dispose();
            });

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
              'persistStoredRecords', 'confirmClear', 'normalizeAnnotationText', 'switchDrawingIdentity',
              'createTextDialogController']) {
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
            changedRecords.length = 0;
            manager.updateContext({ orderedTimes: [100, 200, 300] });
            manager.updateContext({ orderedTimes: [100, 200, 300], pricePrecision: 5 });
            assert.equal(manager.getRecords()[0].bars, 3, 'candle context refresh must update measurements');
            assert.equal(changedRecords.length, 0, 'context refresh must never publish unchanged records');
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
            "SyntheticOrderSizing.BuildSyntheticLotOrderPreview",
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

    private static void SyntheticTerminalMarginPreviewRendersAndRefreshes()
    {
        var html = File.ReadAllText(SourcePath("desktop", "CAPETF.Desktop", "Assets", "synthetic-terminal.html"));
        foreach (var required in new[]
        {
            "id=\"margin-summary\"",
            "aria-live=\"polite\"",
            "id=\"buy-margin\"",
            "id=\"sell-margin\"",
            "id=\"available-margin\"",
            "id=\"after-buy-margin\"",
            "id=\"after-sell-margin\"",
            "id=\"margin-legs\"",
            "window.setTerminalMarginPreview",
            "Unavailable",
            "value === null",
            "AccountCurrency",
            "AfterBuy",
            "AfterSell",
            "classList.toggle('negative'",
            "classList.toggle('stale'",
            "type: 'previewMargins'",
            "setTimeout",
            "quantity.addEventListener('input'",
        })
        {
            if (!html.Contains(required, StringComparison.Ordinal))
            {
                throw new Exception($"terminal margin preview HTML missing {required}");
            }
        }

        var requestCount = html.Split("scheduleMarginPreview();", StringSplitOptions.None).Length - 1;
        if (requestCount < 2)
        {
            throw new Exception("terminal margin preview must refresh after basket load, notional changes, and live ticks");
        }
    }

    private static void SyntheticTerminalMarginPreviewMarksAnyMissingDisplayedTotalUnavailable()
    {
        var html = File.ReadAllText(SourcePath("desktop", "CAPETF.Desktop", "Assets", "synthetic-terminal.html"));
        var predicateStart = html.IndexOf("const unavailable =", StringComparison.Ordinal);
        var predicateEnd = html.IndexOf("setMarginValue('buy-margin'", predicateStart, StringComparison.Ordinal);
        if (predicateStart < 0 || predicateEnd <= predicateStart)
        {
            throw new Exception("terminal margin preview unavailable predicate must remain available for regression coverage");
        }

        var predicate = html[predicateStart..predicateEnd];
        foreach (var required in new[]
        {
            "!isMarginNumber(buyMargin)",
            "!isMarginNumber(sellMargin)",
            "!isMarginNumber(available)",
            "!isMarginNumber(afterBuy)",
            "!isMarginNumber(afterSell)",
        })
        {
            if (!predicate.Contains(required, StringComparison.Ordinal))
            {
                throw new Exception($"missing terminal margin total must set the summary unavailable: {required}");
            }
        }
    }

    private static void CapComTerminalResetsMarginContextAndRejectsInvalidNotional()
    {
        var source = File.ReadAllText(SourcePath("desktop", "CAPETF.Desktop", "CapComTerminalWindow.xaml.cs"));
        foreach (var required in new[]
        {
            "ResetMarginPreviewContextAsync(",
            "clearBasket: true",
            "await ResetMarginPreviewAfterLoginAsync()",
            "_marginPreviewRefresh = null",
            "window.resetTerminalMarginPreview",
            "SyntheticMarginPreviewInput.TryValidate",
            "target.Currency = details.Currency",
        })
        {
            if (!source.Contains(required, StringComparison.Ordinal))
            {
                throw new Exception($"cap.com Terminal final margin reset/input contract missing {required}");
            }
        }

        foreach (var forbidden in new[]
        {
            "basketNotional > 0 ? basketNotional : 300m",
            "if (basketNotional <= 0) basketNotional = 300m",
        })
        {
            if (source.Contains(forbidden, StringComparison.Ordinal))
            {
                throw new Exception($"invalid visible notionals must never fall back to 300: {forbidden}");
            }
        }
    }

    private static void InvalidMarginInputCancelsHostPublicationOwnership()
    {
        var html = File.ReadAllText(SourcePath("desktop", "CAPETF.Desktop", "Assets", "synthetic-terminal.html"));
        AssertTrue(html.Contains("type: 'cancelMarginPreview'", StringComparison.Ordinal),
            "invalid browser notionals must explicitly cancel the host margin preview");

        var source = File.ReadAllText(SourcePath("desktop", "CAPETF.Desktop", "CapComTerminalWindow.xaml.cs"));
        var parserSource = File.ReadAllText(SourcePath("desktop", "CAPETF.Desktop", "SyntheticTradingHostCoordinator.cs"));
        AssertTrue(parserSource.Contains("case \"cancelMarginPreview\":", StringComparison.Ordinal),
            "the strict browser parser must accept the margin-cancel message");
        AssertTrue(source.Contains("case SyntheticCancelMarginPreviewRequest:", StringComparison.Ordinal),
            "the WPF host must route the typed browser margin-cancel request");
        var cancelStart = source.IndexOf("private void CancelMarginPreviewRequest()", StringComparison.Ordinal);
        var cancelEnd = source.IndexOf("private async Task ResetMarginPreviewContextAsync", cancelStart, StringComparison.Ordinal);
        if (cancelStart < 0 || cancelEnd <= cancelStart)
        {
            throw new Exception("the margin request cancellation helper must remain a focused block");
        }
        var cancelBlock = source[cancelStart..cancelEnd];
        foreach (var required in new[] { "_marginPreviewRefresh = null", "request.Cancel();", "request.Dispose();" })
        {
            AssertTrue(cancelBlock.Contains(required, StringComparison.Ordinal),
                $"host margin cancellation must relinquish, cancel, and dispose ownership: {required}");
        }

        var basket = CreateMarginPreviewBasket("USD");
        using var request = new CancellationTokenSource();
        var requestToken = request.Token;
        request.Cancel();
        request.Dispose();
        AssertFalse(
            SyntheticMarginPreviewPublication.IsCurrent(requestToken, request, request, basket, basket),
            "a canceled and disposed request owner must never publish a late margin result");
    }

    private static void SyntheticTerminalMarginRuntimeExercisesFinalReviewRegressions()
    {
        var htmlPath = SourcePath("desktop", "CAPETF.Desktop", "Assets", "synthetic-terminal.html");
        const string script = """
            const assert = require('node:assert/strict');
            const fs = require('node:fs');
            const vm = require('node:vm');

            class FakeClassList {
              constructor() { this.values = new Set(); }
              add(...values) { values.forEach(value => this.values.add(value)); }
              remove(...values) { values.forEach(value => this.values.delete(value)); }
              contains(value) { return this.values.has(value); }
              toggle(value, force) {
                const enabled = force === undefined ? !this.values.has(value) : !!force;
                if (enabled) this.values.add(value); else this.values.delete(value);
                return enabled;
              }
            }

            class FakeElement {
              constructor(id = '') {
                this.id = id;
                this.dataset = {};
                this.classList = new FakeClassList();
                this.style = { display: '', setProperty() {} };
                this.textContent = '';
                this.innerHTML = '';
                this.value = id === 'quantity' ? '300' : '';
                this.disabled = false;
                this.attributes = new Map();
                this.children = [];
              }
              addEventListener() {}
              removeEventListener() {}
              setAttribute(name, value) { this.attributes.set(name, String(value)); }
              getAttribute(name) { return this.attributes.get(name) || null; }
              getBoundingClientRect() { return { left: 0, top: 0, right: 800, width: 800, height: 600 }; }
              hasPointerCapture() { return false; }
              setPointerCapture() {}
              releasePointerCapture() {}
              focus() { document.activeElement = this; }
              appendChild(child) { this.children.push(child); return child; }
              replaceChildren(...children) { this.children = [...children]; this.textContent = ''; }
            }

            const elements = new Map();
            const element = id => {
              if (!elements.has(id)) elements.set(id, new FakeElement(id));
              return elements.get(id);
            };
            global.window = globalThis;
            global.document = {
              activeElement: null,
              body: new FakeElement('body'),
              documentElement: new FakeElement('html'),
              createElement() { return new FakeElement(); },
              getElementById: element,
              querySelectorAll() { return []; }
            };
            global.localStorage = { getItem() { return null; }, setItem() {}, removeItem() {} };
            global.confirm = () => true;
            global.addEventListener = () => {};
            global.removeEventListener = () => {};
            global.requestAnimationFrame = callback => { callback(); return 1; };
            global.cancelAnimationFrame = () => {};

            let nextTimer = 1;
            const timers = new Map();
            global.setTimeout = callback => {
              const id = nextTimer++;
              timers.set(id, callback);
              return id;
            };
            global.clearTimeout = id => timers.delete(id);
            function flushTimers() {
              const queued = Array.from(timers.values());
              timers.clear();
              queued.forEach(callback => callback());
            }

            const messages = [];
            global.chrome = { webview: { postMessage(message) { messages.push(message); } } };
            function makeSeries() {
              return {
                setData() {}, update() {}, createPriceLine() { return {}; }, removePriceLine() {},
                priceToCoordinate(value) { return Number(value); }, coordinateToPrice(value) { return Number(value); },
                attachPrimitive() {}, detachPrimitive() {}
              };
            }
            const timeScale = {
              fitContent() {}, scrollToRealTime() {}, applyOptions() {}, scrollPosition() { return 0; },
              scrollToPosition() {}, timeToCoordinate(value) { return Number(value); },
              coordinateToTime(value) { return Number(value); }
            };
            global.LightweightCharts = {
              CandlestickSeries: {}, LineSeries: {}, CrosshairMode: { Normal: 0 },
              version() { return 'test'; },
              createChart() {
                return {
                  addSeries() { return makeSeries(); }, timeScale() { return timeScale; },
                  priceScale() { return { applyOptions() {} }; }, subscribeCrosshairMove() {}, resize() {}
                };
              }
            };
            const fakeDrawingManager = {
              updateContext() {}, getRecords() { return []; }, setRecords() {}, setTool() { return true; },
              cancel() {}, undo() {}, redo() {}, setMagnet() {}, setLocked() {}, setVisible() {},
              setStyle() {}, clear() {}, getState() { return {}; }
            };
            global.CapComDrawings = {
              createManager() { return fakeDrawingManager; },
              createTextDialogController() { return { open() {}, close() {}, dispose() {} }; },
              switchDrawingIdentity(manager, storage, oldIdentity, nextIdentity) { return nextIdentity; },
              persistStoredRecords() { return true; }
            };
            global.lucide = { createIcons() {} };

            const html = fs.readFileSync(process.argv[1], 'utf8');
            const matches = Array.from(html.matchAll(/<script>([\s\S]*?)<\/script>/g));
            assert.ok(matches.length > 0, 'terminal inline script must exist');
            vm.runInThisContext(matches.at(-1)[1], { filename: process.argv[1] });

            const buy = {
              Side: 'BUY', IsAvailable: true, TotalMargin: 100,
              Legs: [{
                Side: 'SELL', Epic: 'HEDGE', ReferencePrice: 49.25, Quantity: 2,
                NativeNotional: 98.5, NativeCurrency: 'EUR', NativeMargin: 19.7,
                AccountCurrency: 'USD', MarginAccountCurrency: 21.67
              }]
            };
            const sell = {
              Side: 'SELL', IsAvailable: true, TotalMargin: 50,
              Legs: [{
                Side: 'BUY', Epic: 'HEDGE', ReferencePrice: 50.25, Quantity: 2,
                NativeNotional: 100.5, NativeCurrency: 'EUR', NativeMargin: 20.1,
                AccountCurrency: 'USD', MarginAccountCurrency: 22.11
              }]
            };
            const ready = {
              AccountCurrency: 'USD', Available: 500, AfterBuy: -5, AfterSell: 450,
              IsAccountStale: false, AccountError: '', Buy: buy, Sell: sell
            };

            window.setTerminalMarginPreview(ready);
            assert.equal(element('margin-summary').dataset.state, 'ready');
            assert.equal(element('available-margin').textContent, 'USD 500.00');
            assert.ok(element('after-buy-margin').classList.contains('negative'),
              'negative remaining funds must retain warning styling');
            const legText = element('margin-legs').children.map(child => child.textContent).join(' | ');
            assert.match(legText, /SELL 2 x HEDGE @ EUR 49\.25/,
              'leg rows must show effective side, quantity, and execution price');
            assert.match(legText, /notional EUR 98\.50/,
              'leg rows must show native notional');
            assert.match(legText, /margin EUR 19\.70.*account USD 21\.67/,
              'leg rows must show native and account-currency margin contribution');

            window.setTerminalMarginPreview({
              ...ready, IsAccountStale: true, AccountError: 'account refresh failed'
            });
            assert.equal(element('available-margin').textContent, 'USD 500.00',
              'stale account rendering must retain the last successful availability');
            assert.equal(element('margin-summary').dataset.state, 'stale');
            assert.match(element('margin-status-copy').textContent, /stale.*account refresh failed/i,
              'stale account rendering must show a visible stale reason');

            element('status').textContent = 'Quotes fresh';
            window.setTerminalBusy(true, 'Refreshing margin preview');
            assert.equal(element('status').textContent, 'Refreshing margin preview');
            window.setTerminalBusy(false);
            assert.equal(element('status').textContent, 'Quotes fresh',
              'releasing busy ownership must clear the refreshing label');

            window.setTerminalMarginPreview({ ...ready, Buy: { ...buy, TotalMargin: null } });
            assert.equal(element('buy-margin').textContent, 'Unavailable');
            assert.equal(element('margin-summary').dataset.state, 'unavailable');

            messages.length = 0;
            for (const invalid of ['', 'not-a-number', '0', '-10', '1.5']) {
              messages.length = 0;
              element('quantity').value = invalid;
              scheduleMarginPreview();
              flushTimers();
              assert.equal(messages.length, 1, `invalid synthetic lots ${invalid || 'blank'} must send one host cancellation`);
              assert.equal(messages[0].type, 'cancelMarginPreview');
              assert.equal(element('margin-summary').dataset.state, 'unavailable');
              assert.match(element('margin-status-copy').textContent, /positive whole number of synthetic lots/i);
            }

            messages.length = 0;
            element('quantity').value = '1';
            scheduleMarginPreview();
            element('quantity').value = '2';
            scheduleMarginPreview();
            flushTimers();
            assert.equal(messages.length, 1, 'a burst of valid synthetic-lot changes must debounce to one request');
            assert.equal(messages[0].syntheticLots, 2);

            messages.length = 0;
            element('quantity').value = '3';
            window.setTerminalData({
              Symbol: 'basket-a', DrawingIdentity: 'basket-a', Candles: [], Components: []
            });
            window.clearTerminal();
            flushTimers();
            assert.equal(messages.length, 0,
              'clearing the terminal must cancel its scheduled margin request');
            assert.equal(element('margin-summary').dataset.state, 'unavailable');
            assert.match(element('margin-status-copy').textContent, /build.*basket/i);
            """;

        RunNodeDrawingContract(htmlPath, script, "terminal margin final-review DOM");
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
            "SyntheticStrategyCandidatePool.Select",
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
            "await LoadStocksAsync(cancellationToken);",
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
        foreach (var removed in new[]
        {
            "Selection Basis",
            "id=\"selection-basis\"",
            "getElementById('selection-basis')",
        })
        {
            if (html.Contains(removed, StringComparison.Ordinal))
            {
                throw new Exception($"terminal HTML must not reserve permanent rail space for {removed}");
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
                "lotSize": 1,
                "marketModes": ["REGULAR"]
              },
              "dealingRules": {
                "minDealSize": { "unit": "POINTS", "value": 1 },
                "minSizeIncrement": { "unit": "POINTS", "value": 0.1 },
                "maxDealSize": { "unit": "POINTS", "value": 1000 }
              },
              "snapshot": {
                "marketStatus": "TRADEABLE",
                "updateTimeUTC": "2026-07-27T13:45:12.345",
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
        AssertNear(1000m, details.MaxDealSize ?? 0m, "market details should parse max deal size");
        AssertEqual("REGULAR", details.MarketModes.Single(), "market details should parse market modes");
        if (details.Status != "TRADEABLE") throw new Exception("market details should parse market status");
        AssertEqual("United States", details.Country, "market details should parse country");
        AssertEqual("US", details.Region, "market details should parse region");
        AssertEqual("Technology", details.Sector, "market details should parse sector");
        AssertEqual(DateTimeOffset.Parse("2026-07-27T13:45:12.345Z"), details.LastTickAt, "market details must retain the Capital.com source snapshot timestamp");
    }

    private static void CapitalApiClientRejectsOffsetlessSnapshotForTradingFreshness()
    {
        const string json =
            """
            {
              "instrument": { "epic": "AMD", "name": "Advanced Micro Devices" },
              "snapshot": {
                "marketStatus": "TRADEABLE",
                "updateTime": "2026-07-27T20:52:11.279",
                "bid": 484.16,
                "offer": 484.76
              }
            }
            """;

        var retrievedAt = DateTimeOffset.Parse("2026-07-27T16:52:12Z");
        var details = CapitalApiClient.ParseMarketDetails(json, retrievedAt);

        if (details is null) throw new Exception("market details should preserve a snapshot with account-local time");
        AssertNear(484.16m, details.Bid ?? 0m, "account-local snapshots should still supply bid");
        AssertNear(484.76m, details.Offer ?? 0m, "account-local snapshots should still supply ask");
        AssertEqual<DateTimeOffset?>(
            null,
            details.LastTickAt,
            "offset-less updateTime must not be reinterpreted as a fresh UTC quote");
    }

    private static void MarketSnapshotRejectsOlderSourceTimeAndClearsMissingSides()
    {
        var apply = typeof(CapComTerminalWindow).GetMethod(
            "ApplyMarketDetails",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            ?? throw new Exception("market details updater must remain available for snapshot ordering tests");
        var currentTime = DateTimeOffset.Parse("2026-07-27T14:00:00Z");
        var target = new MarketInstrument
        {
            Epic = "ORDERED-SNAPSHOT",
            Bid = 100m,
            Offer = 102m,
            Price = 101m,
            LastTickAt = currentTime,
        };

        apply.Invoke(null, [target, new MarketInstrument
        {
            Epic = target.Epic,
            Bid = 90m,
            Offer = 92m,
            Price = 91m,
            LastTickAt = currentTime.AddMinutes(-1),
        }]);

        AssertNear(100m, target.Bid ?? 0m, "an older market snapshot must not replace a newer bid");
        AssertNear(102m, target.Offer ?? 0m, "an older market snapshot must not replace a newer offer");
        AssertEqual(currentTime, target.LastTickAt, "an older market snapshot must not rewind quote source time");

        apply.Invoke(null, [target, new MarketInstrument
        {
            Epic = target.Epic,
            Bid = 103m,
            Offer = null,
            Price = 103m,
            LastTickAt = currentTime.AddMinutes(1),
        }]);

        AssertNear(103m, target.Bid ?? 0m, "a newer market snapshot must update its available bid");
        if (target.Offer is not null) throw new Exception("a newer market snapshot with no offer must clear the stale offer side");
        AssertEqual(currentTime.AddMinutes(1), target.LastTickAt, "a newer market snapshot must advance source time");
    }

    private static void CapitalStreamingTimestampParserRequiresSourceTime()
    {
        var readTimestamp = typeof(CapitalStreamingClient).GetMethod(
            "ReadTimestamp",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            ?? throw new Exception("stream timestamp parser must remain available");
        using var sourceDocument = JsonDocument.Parse("{\"timestamp\":1785169512345}");
        using var missingDocument = JsonDocument.Parse("{}");

        AssertEqual(
            DateTimeOffset.FromUnixTimeMilliseconds(1785169512345),
            readTimestamp.Invoke(null, [sourceDocument.RootElement]),
            "stream quotes must use the Capital.com source timestamp");
        if (readTimestamp.Invoke(null, [missingDocument.RootElement]) is not null)
        {
            throw new Exception("a stream quote without a source timestamp must not be stamped with local receipt time");
        }
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
            "SyntheticStreamingSubscription.SubscribeAsync",
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

    private static void CapitalStreamingCleanupIsBoundedAndIdempotent()
    {
        var socket = new FakeCapitalStreamingSocket(WebSocketState.Open, blockClose: true);
        var client = new CapitalStreamingClient(socket);
        var stopwatch = Stopwatch.StartNew();

        client.DisposeAsync().AsTask().GetAwaiter().GetResult();
        client.DisposeAsync().AsTask().GetAwaiter().GetResult();

        stopwatch.Stop();
        if (stopwatch.Elapsed > TimeSpan.FromSeconds(2))
        {
            throw new Exception($"stream cleanup must be bounded and idempotent; elapsed {stopwatch.Elapsed}");
        }
        AssertEqual(1, socket.DisposeCount, "stream cleanup must dispose the socket exactly once");
    }

    private static void WindowLifetimeCancelsAndRejectsLateCompletion()
    {
        using var lifetime = new WindowLifetime();
        var updates = 0;

        if (!lifetime.TryApply(() => updates++)) throw new Exception("an open window must accept UI completion");
        if (!lifetime.BeginClosing()) throw new Exception("the first close transition must win");
        if (lifetime.BeginClosing()) throw new Exception("the close transition must be idempotent");
        if (!lifetime.Token.IsCancellationRequested) throw new Exception("closing must cancel outstanding terminal operations");
        if (lifetime.TryApply(() => updates++)) throw new Exception("late UI completion must be rejected after close begins");
        AssertEqual(1, updates, "late completion must not mutate window state");

        var source = File.ReadAllText(SourcePath("desktop", "CAPETF.Desktop", "CapComTerminalWindow.xaml.cs"));
        foreach (var required in new[]
        {
            "protected override void OnClosing",
            "_windowLifetime.BeginClosing()",
            "_windowLifetime.Token",
            "Func<CancellationToken, Task>",
        })
        {
            if (!source.Contains(required, StringComparison.Ordinal))
            {
                throw new Exception($"terminal window lifecycle contract missing {required}");
            }
        }
        if (source.Contains("protected override async void OnClosed", StringComparison.Ordinal))
        {
            throw new Exception("window cleanup must not race through async-void OnClosed");
        }
    }

    private static void WindowLifetimeCancellationNeverEntersFailureUi()
    {
        using var lifetime = new CancellationTokenSource();
        var failureUiUpdates = 0;
        var completedUiUpdates = 0;

        var completed = TerminalOperationExecution.RunAsync(
            async _ =>
            {
                lifetime.Cancel();
                await Task.Yield();
                throw new InvalidOperationException("WebView disposed while the window was closing");
            },
            lifetime.Token,
            () => completedUiUpdates++,
            _ => failureUiUpdates++).GetAwaiter().GetResult();

        AssertFalse(completed, "a lifetime-cancelled operation must not report success");
        AssertEqual(0, completedUiUpdates, "a lifetime-cancelled operation must not enter completion UI");
        AssertEqual(0, failureUiUpdates, "a lifetime-cancelled operation must not enter failure UI");

        using var streamingCancellation = new CancellationTokenSource();
        var streamingStartInvoked = false;
        var cancellationPreserved = false;
        try
        {
            TerminalOperationExecution.WrapStreamingStartAsync(
                () =>
                {
                    streamingStartInvoked = true;
                    streamingCancellation.Cancel();
                    return Task.FromCanceled(streamingCancellation.Token);
                },
                "Basket ready.",
                streamingCancellation.Token).GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
            cancellationPreserved = true;
        }

        AssertTrue(streamingStartInvoked, "stream startup cancellation must traverse the wrapper");
        AssertTrue(cancellationPreserved, "stream startup wrapper must preserve OperationCanceledException during close");

        var source = File.ReadAllText(SourcePath("desktop", "CAPETF.Desktop", "CapComTerminalWindow.xaml.cs"));
        var chartStart = source.IndexOf("private async Task InitializeChartHostAsync", StringComparison.Ordinal);
        var chartEnd = source.IndexOf("private async Task RenderSyntheticChartAsync", chartStart, StringComparison.Ordinal);
        var chartInitialization = source[chartStart..chartEnd];
        AssertTrue(chartInitialization.Contains("TerminalOperationExecution.RunAsync", StringComparison.Ordinal),
            "chart initialization must use the tested lifetime-cancellation policy");
        AssertTrue(source.Contains("TerminalOperationExecution.WrapStreamingStartAsync", StringComparison.Ordinal),
            "stream startup must use the cancellation-preserving wrapper");
    }

    private static void WebViewRuntimeProfileIsExternalAndExplicitlyConfigured()
    {
        var localAppData = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "capetf-local-app-data"));
        var expected = Path.Combine(localAppData, "CAPETF", "WebView2");
        AssertEqual(expected, WebViewRuntimeProfile.GetUserDataFolder(localAppData), "WebView2 profile must have a stable per-user path");

        var terminal = File.ReadAllText(SourcePath("desktop", "CAPETF.Desktop", "CapComTerminalWindow.xaml.cs"));
        var dashboard = File.ReadAllText(SourcePath("desktop", "CAPETF.Desktop", "MainWindow.xaml.cs"));
        if (!terminal.Contains("WebViewRuntimeProfile.CreateEnvironmentAsync", StringComparison.Ordinal) ||
            !dashboard.Contains("WebViewRuntimeProfile.CreateEnvironmentAsync", StringComparison.Ordinal))
        {
            throw new Exception("every WebView host must pass the explicit external user-data environment to EnsureCoreWebView2Async");
        }
    }

    private static void DesktopPublishAndThirdPartyPackagingContractsAreComplete()
    {
        var projectPath = SourcePath("desktop", "CAPETF.Desktop", "CAPETF.Desktop.csproj");
        var project = File.ReadAllText(projectPath);
        var installer = File.ReadAllText(SourcePath("desktop", "CAPETF.Desktop", "build-installer.ps1"));
        var readme = File.ReadAllText(SourcePath("desktop", "README.md"));
        var html = File.ReadAllText(SourcePath("desktop", "CAPETF.Desktop", "Assets", "synthetic-terminal.html"));
        var chartLibrary = File.ReadAllText(SourcePath("desktop", "CAPETF.Desktop", "Assets", "lightweight-charts.standalone.production.js"));
        var assetDirectory = SourcePath("desktop", "CAPETF.Desktop", "Assets");

        foreach (var notice in new[]
        {
            "lightweight-charts.LICENSE.txt",
            "lightweight-charts.NOTICE.txt",
            "lucide.LICENSE.txt",
            "THIRD-PARTY-NOTICES.txt",
        })
        {
            var path = Path.Combine(assetDirectory, notice);
            if (!File.Exists(path) || new FileInfo(path).Length == 0) throw new Exception($"packaged third-party notice missing or empty: {notice}");
            if (!project.Contains($"Assets\\{notice}", StringComparison.Ordinal)) throw new Exception($"project publish content missing {notice}");
        }

        if (!html.Contains("attributionLogo: true", StringComparison.Ordinal) ||
            !chartLibrary.Contains("tradingview.com", StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception("the terminal must preserve a visible TradingView attribution logo/link");
        }
        if (File.Exists(Path.Combine(assetDirectory, "klinecharts.min.js")) ||
            project.Contains("klinecharts", StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception("unused KLineCharts code must not remain in source or publish content");
        }
        if (!installer.Contains("--self-contained true", StringComparison.Ordinal) ||
            !readme.Contains("WebView2 Runtime", StringComparison.Ordinal))
        {
            throw new Exception("publish command and prerequisite documentation must describe the self-contained package accurately");
        }
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
            "container-type: inline-size",
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
            "CapComDrawings.switchDrawingIdentity",
            "CapComDrawings.createTextDialogController",
            "data-drawing-ui",
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

        var responsiveFooter = ExtractCssBlock(html, "@container (max-width: 700px)");
        var footerRule = ExtractCssBlock(responsiveFooter, "#footer");
        var ohlcRule = ExtractCssBlock(responsiveFooter, "#ohlc");
        if (!footerRule.Contains("grid-template-rows: auto auto", StringComparison.Ordinal) ||
            !ohlcRule.Contains("grid-column: 1 / -1", StringComparison.Ordinal))
        {
            throw new Exception("compact footer layout declarations must be scoped to #footer and #ohlc inside the 700px container query");
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

    private static void SyntheticTerminalRuntimeCoalescesTicksAndRejectsStaleBasketFrames()
    {
        var htmlPath = SourcePath("desktop", "CAPETF.Desktop", "Assets", "synthetic-terminal.html");
        const string script = """
            const assert = require('node:assert/strict');
            const fs = require('node:fs');
            const vm = require('node:vm');

            class FakeClassList {
              constructor() { this.values = new Set(); }
              add(...values) { values.forEach(value => this.values.add(value)); }
              remove(...values) { values.forEach(value => this.values.delete(value)); }
              contains(value) { return this.values.has(value); }
              toggle(value, force) {
                const enabled = force === undefined ? !this.values.has(value) : !!force;
                if (enabled) this.values.add(value); else this.values.delete(value);
                return enabled;
              }
            }

            class FakeElement {
              constructor(id = '') {
                this.id = id;
                this.dataset = {};
                this.classList = new FakeClassList();
                this.style = { display: '', setProperty() {} };
                this.textContent = '';
                this.innerHTML = '';
                this.value = id === 'quantity' ? '300' : '';
                this.disabled = false;
                this.attributes = new Map();
              }
              addEventListener() {}
              removeEventListener() {}
              setAttribute(name, value) { this.attributes.set(name, String(value)); }
              getAttribute(name) { return this.attributes.get(name) || null; }
              getBoundingClientRect() { return { left: 0, top: 0, right: 800, width: 800, height: 600 }; }
              hasPointerCapture() { return false; }
              setPointerCapture() {}
              releasePointerCapture() {}
              focus() { document.activeElement = this; }
            }

            const elements = new Map();
            const element = id => {
              if (!elements.has(id)) elements.set(id, new FakeElement(id));
              return elements.get(id);
            };
            global.window = globalThis;
            global.document = {
              activeElement: null,
              body: new FakeElement('body'),
              documentElement: new FakeElement('html'),
              getElementById: element,
              querySelectorAll() { return []; }
            };
            global.localStorage = { getItem() { return null; }, setItem() {}, removeItem() {} };
            global.confirm = () => true;
            global.addEventListener = () => {};
            global.removeEventListener = () => {};

            let nextFrame = 1;
            const frames = new Map();
            global.requestAnimationFrame = callback => {
              const id = nextFrame++;
              frames.set(id, callback);
              return id;
            };
            global.cancelAnimationFrame = id => frames.delete(id);
            function flushFrames() {
              const queued = Array.from(frames.values());
              frames.clear();
              queued.forEach(callback => callback());
            }

            const candleSetData = [];
            const candleUpdates = [];
            let seriesCount = 0;
            function makeSeries() {
              const isCandle = seriesCount++ === 0;
              return {
                setData(rows) { if (isCandle) candleSetData.push(rows.map(row => ({ ...row }))); },
                update(row) { if (isCandle) candleUpdates.push({ ...row }); },
                createPriceLine() { return {}; },
                removePriceLine() {},
                priceToCoordinate(value) { return Number(value); },
                coordinateToPrice(value) { return Number(value); },
                attachPrimitive() {},
                detachPrimitive() {}
              };
            }
            const timeScale = {
              fitContent() {}, scrollToRealTime() {}, applyOptions() {}, scrollPosition() { return 0; },
              scrollToPosition() {}, timeToCoordinate(value) { return Number(value); },
              coordinateToTime(value) { return Number(value); }
            };
            global.LightweightCharts = {
              CandlestickSeries: {}, LineSeries: {}, CrosshairMode: { Normal: 0 },
              version() { return 'test'; },
              createChart() {
                return {
                  addSeries() { return makeSeries(); },
                  timeScale() { return timeScale; },
                  priceScale() { return { applyOptions() {} }; },
                  subscribeCrosshairMove() {}, resize() {}
                };
              }
            };
            const fakeDrawingManager = {
              updateContext() {}, getRecords() { return []; }, setRecords() {}, setTool() { return true; },
              cancel() {}, undo() {}, redo() {}, setMagnet() {}, setLocked() {}, setVisible() {},
              setStyle() {}, clear() {}, getState() { return {}; }
            };
            global.CapComDrawings = {
              createManager() { return fakeDrawingManager; },
              createTextDialogController() { return { open() {}, close() {}, dispose() {} }; },
              switchDrawingIdentity(manager, storage, oldIdentity, nextIdentity) { return nextIdentity; },
              persistStoredRecords() { return true; }
            };
            global.lucide = { createIcons() {} };

            const html = fs.readFileSync(process.argv[1], 'utf8');
            const matches = Array.from(html.matchAll(/<script>([\s\S]*?)<\/script>/g));
            assert.ok(matches.length > 0, 'terminal inline script must exist');
            vm.runInThisContext(matches.at(-1)[1], { filename: process.argv[1] });

            function makePayload(identity, close) {
              return {
                Symbol: identity,
                DrawingIdentity: identity,
                BidPrice: close - 1,
                AskPrice: close + 1,
                Candles: [{ Time: 100, Open: close, High: close, Low: close, Close: close }],
                Components: [{ Epic: `${identity}-LEG`, Bid: close - 1, Offer: close + 1 }]
              };
            }
            function makeTick(identity, candle, bid) {
              return {
                DrawingIdentity: identity,
                Candle: candle,
                BidPrice: bid,
                AskPrice: bid + 2,
                ComponentQuotes: [{ Epic: `${identity}-LEG`, Bid: bid, Offer: bid + 2, QuoteStatus: 'fresh' }]
              };
            }

            window.setTerminalData(makePayload('basket-a', 100));
            window.updateTerminalTick(makeTick('basket-a', { Time: 100, Open: 100, High: 112, Low: 99, Close: 110 }, 109));
            window.updateTerminalTick(makeTick('basket-a', null, 110));
            flushFrames();
            assert.equal(candleUpdates.at(-1)?.close, 110,
              'metadata-only coalescing must retain the preceding price-changing candle');

            candleUpdates.length = 0;
            window.updateTerminalTick(makeTick('basket-a', { Time: 100, Open: 100, High: 151, Low: 99, Close: 150 }, 149));
            window.setTerminalData(makePayload('basket-b', 200));
            flushFrames();
            assert.equal(candleUpdates.length, 0,
              'a full new-basket payload must cancel and invalidate the old pending frame');
            assert.equal(candleSetData.at(-1).at(-1).close, 200,
              'an old-basket pending tick must not mutate newly loaded chart data');
            """;

        RunNodeDrawingContract(htmlPath, script, "terminal tick coalescing/identity DOM");
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
            "SyntheticStreamingSubscription.SubscribeAsync",
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
        AssertStrategyTop(SyntheticStrategyKind.BelowMa200, "BELOW200", instruments, candles, periodsPerYear: 252);
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

    private static void StrategyCandidatePoolKeepsOnlyTopSignalRanksForClustering()
    {
        var day = DateTimeOffset.Parse("2023-01-02T00:00:00Z");
        var instruments = Enumerable.Range(0, 14)
            .Select(index => CreateSeedStock($"CA-{index:00}", $"Canada {index:00}"))
            .ToList();
        var candles = instruments.ToDictionary(
            instrument => instrument.Epic,
            instrument => CreateLongReturnCandles(
                day,
                [0.001m + instruments.IndexOf(instrument) * 0.0001m, -0.0005m, 0.0008m],
                160));

        var candidates = SyntheticStrategyCandidatePool.Select(
            SyntheticStrategyKind.HighMomentum,
            instruments,
            candles,
            primaryPeriodsPerYear: 52,
            candles,
            fallbackPeriodsPerYear: 52);
        var expected = SyntheticStrategyRanker.Rank(
            SyntheticStrategyKind.HighMomentum,
            instruments,
            candles,
            periodsPerYear: 52,
            maximum: SyntheticStrategyCandidatePool.MaximumCandidates);

        AssertEqual(8, candidates.Count, "a small market must not send its full universe into strategy clustering");
        AssertEqual(
            string.Join(',', expected.Select(rank => rank.Instrument.Epic)),
            string.Join(',', candidates.Select(instrument => instrument.Epic)),
            "strategy clustering candidates must preserve signal-rank priority");
    }

    private static void WeeklyStrategiesScaleMaPeriodsFromTradingDays()
    {
        var day = DateTimeOffset.Parse("2023-01-02T00:00:00Z");
        var instruments = Enumerable.Range(0, 3)
            .Select(index => CreateSeedStock($"WEEKLY-MA-{index}", $"Weekly MA {index}"))
            .ToList();
        var candles = instruments.ToDictionary(
            instrument => instrument.Epic,
            instrument => (IReadOnlyList<OhlcPoint>)Enumerable.Range(0, 156)
                .Select(index =>
                {
                    var close = index < 136 ? 120m + instruments.IndexOf(instrument) : 85m - (index - 136) * 0.2m;
                    return FlatCandle(day.AddDays(index * 7), close);
                })
                .ToList());

        var belowMa = SyntheticStrategyRanker.Rank(
            SyntheticStrategyKind.BelowMa200, instruments, candles, periodsPerYear: 52, maximum: 3);
        var meanReversion = SyntheticStrategyRanker.Rank(
            SyntheticStrategyKind.MeanReversion, instruments, candles, periodsPerYear: 52, maximum: 3);

        AssertEqual(3, belowMa.Count, "weekly MA200 strategy must use the 200-trading-day equivalent period");
        AssertEqual(3, meanReversion.Count, "weekly mean reversion must scale MA50 and MA200 to weekly periods");
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

    private static void DipInsideUptrendBuildsFromBundledUsDailyUniverse()
    {
        var cached = DashboardStockChunkLoader.LoadStocks();
        if (cached.Instruments.Count == 0 || cached.OhlcByEpicAndResolution is null) return;

        const string block = "US / USD / All";
        const int minimumCandles = 120;
        var candidates = SyntheticTerminalSelector.HistoryLoadCandidates(block, cached.Instruments, limit: 500);
        var daily = cached.OhlcByEpicAndResolution
            .Where(pair => pair.Value.TryGetValue("Daily", out var rows) &&
                           SyntheticHistoryService.DistinctAlignmentKeyCount(rows, "Daily") >= minimumCandles)
            .ToDictionary(pair => pair.Key, pair => pair.Value["Daily"], StringComparer.OrdinalIgnoreCase);
        var weekly = cached.OhlcByEpicAndResolution
            .Where(pair => pair.Value.TryGetValue("Weekly", out var rows) && rows.Count >= 2)
            .ToDictionary(pair => pair.Key, pair => pair.Value["Weekly"], StringComparer.OrdinalIgnoreCase);
        var ranked = SyntheticStrategyRanker.RankWithFallback(
            SyntheticStrategyKind.DipInsideUptrend,
            candidates,
            daily,
            primaryPeriodsPerYear: 252,
            weekly,
            fallbackPeriodsPerYear: 52,
            maximum: 36);
        var basket = SyntheticTerminalSelector.SelectBest(
            block,
            ranked.Select(rank => rank.Instrument).ToList(),
            daily,
            periodsPerYear: 252,
            minimumCandles: minimumCandles);

        if (basket is null)
        {
            var lengths = candidates
                .Where(candidate => daily.ContainsKey(candidate.Epic))
                .Select(candidate => daily[candidate.Epic].Count)
                .ToList();
            throw new Exception(
                $"dip-inside-uptrend must build from bundled US Daily data; " +
                $"candidates={candidates.Count}, usable={lengths.Count}, ranked={ranked.Count}, " +
                $"range={(lengths.Count == 0 ? "n/a" : $"{lengths.Min()}-{lengths.Max()}")}");
        }
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

    private static void SyntheticLotOrderPreviewMultipliesFormulaExactly()
    {
        var basket = new SyntheticBasket
        {
            Symbol = "SYN-CRYPTO-ETHBTC-01",
            Strategy = SyntheticStrategyKind.ManualFormula,
        };
        basket.Components.Add(new SyntheticComponent(new MarketInstrument
        {
            Epic = "ETHUSD", Bid = 2999m, Offer = 3001m,
            MinDealSize = 0.001m, MinSizeIncrement = 0.001m,
        }, 50m, 0m, 0m) { FormulaMultiplier = 9m });
        basket.Components.Add(new SyntheticComponent(new MarketInstrument
        {
            Epic = "BTCUSD", Bid = 59990m, Offer = 60010m,
            MinDealSize = 0.0001m, MinSizeIncrement = 0.0001m,
        }, 50m, 0m, 0m) { FormulaMultiplier = 0.2m });

        var one = SyntheticOrderSizing.BuildSyntheticLotOrderPreview(basket, "BUY", 1m);
        var three = SyntheticOrderSizing.BuildSyntheticLotOrderPreview(basket, "SELL", 3m);

        AssertEqual(1m, one.SyntheticLots, "one displayed formula is one synthetic lot");
        AssertNear(9m, one.Legs[0].Quantity, "one synthetic lot preserves ETH formula quantity");
        AssertNear(0.2m, one.Legs[1].Quantity, "one synthetic lot preserves BTC formula quantity");
        AssertEqual(3m, three.SyntheticLots, "requested synthetic lots remain explicit");
        AssertNear(27m, three.Legs[0].Quantity, "three lots multiply ETH formula quantity by three");
        AssertNear(0.6m, three.Legs[1].Quantity, "three lots multiply BTC formula quantity by three");
        AssertThrows<ArgumentOutOfRangeException>(
            () => SyntheticOrderSizing.BuildSyntheticLotOrderPreview(basket, "BUY", 0m),
            "positive whole number",
            "zero synthetic lots must be rejected");
        AssertThrows<ArgumentOutOfRangeException>(
            () => SyntheticOrderSizing.BuildSyntheticLotOrderPreview(basket, "BUY", 1.5m),
            "positive whole number",
            "fractional synthetic lots must be rejected");
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

    private static void SyntheticMarginCalculatesBuyAndSellUsingExecutableLegs()
    {
        var basket = new SyntheticBasket { Symbol = "SYN-MARGIN" };
        basket.Components.Add(new SyntheticComponent(new MarketInstrument
        {
            Epic = "LONG", Currency = "EUR", Bid = 99m, Offer = 101m,
            LotSize = 1m, MinDealSize = 1m, MinSizeIncrement = 1m,
            MarginFactor = 20m, MarginFactorUnit = "PERCENTAGE",
        }, 50m, 0m, 0m) { FormulaMultiplier = 0.5m });
        basket.Components.Add(new SyntheticComponent(new MarketInstrument
        {
            Epic = "HEDGE", Currency = "EUR", Bid = 49m, Offer = 51m,
            LotSize = 1m, MinDealSize = 1m, MinSizeIncrement = 1m,
            MarginFactor = 25m, MarginFactorUnit = "PERCENTAGE",
        }, 50m, 0m, 0m) { FormulaMultiplier = -0.5m });

        var buy = SyntheticMarginCalculator.CalculateSide(basket, "BUY", 300m, "USD", 1.10m);
        var sell = SyntheticMarginCalculator.CalculateSide(basket, "SELL", 300m, "USD", 1.10m);

        AssertTrue(buy.IsAvailable, "percentage margin factors must produce an available BUY preview");
        AssertEqual("BUY", buy.Legs[0].Side, "positive BUY leg side");
        AssertEqual("SELL", buy.Legs[1].Side, "negative BUY leg reverses side");
        AssertNear(101m, buy.Legs[0].ReferencePrice, "BUY must use the offer");
        AssertNear(49m, buy.Legs[1].ReferencePrice, "reversed BUY hedge must use the bid");
        AssertNear(202m, buy.Legs[0].NativeNotional, "BUY long uses rounded executable notional");
        AssertNear(40.4m, buy.Legs[0].NativeMargin, "percentage margin applies in native currency");
        AssertNear(44.44m, buy.Legs[0].MarginAccountCurrency, "native margin converts to account currency");
        AssertNear(98.34m, buy.TotalMargin ?? throw new Exception("available BUY preview must provide a total"), "BUY total sums converted executable leg margins");
        AssertNear(buy.Legs.Sum(x => x.MarginAccountCurrency), buy.TotalMargin ?? throw new Exception("available BUY preview must provide a total"), "BUY total sums leg margins");

        AssertTrue(sell.IsAvailable, "percentage margin factors must produce an available SELL preview");
        AssertEqual("SELL", sell.Legs[0].Side, "positive SELL leg side");
        AssertEqual("BUY", sell.Legs[1].Side, "negative SELL leg reverses side");
        AssertNear(85.635m, sell.TotalMargin ?? throw new Exception("available SELL preview must provide a total"), "SELL must use its own bid and offer execution prices");
    }

    private static void SyntheticMarginUsesLotSizeWhenSizingExecutableNotional()
    {
        var basket = new SyntheticBasket { Symbol = "SYN-LOT" };
        basket.Components.Add(new SyntheticComponent(new MarketInstrument
        {
            Epic = "LOT", Currency = "EUR", Bid = 12m, Offer = 12m,
            LotSize = 10m, MinDealSize = 1m, MinSizeIncrement = 0.5m,
            MarginFactor = 10m, MarginFactorUnit = "percentage",
        }, 100m, 0m, 0m));

        var preview = SyntheticMarginCalculator.CalculateSide(basket, "BUY", 150m, "USD", 2m);

        AssertNear(1.5m, preview.Legs[0].Quantity, "lot-aware sizing must round quantity up to the deal increment");
        AssertNear(180m, preview.Legs[0].NativeNotional, "lot-aware sizing must preserve executable notional");
        AssertNear(18m, preview.Legs[0].NativeMargin, "lot-aware native margin");
        AssertNear(36m, preview.TotalMargin ?? throw new Exception("available lot-aware preview must provide a total"), "lot-aware margin converts to account currency");
    }

    private static void SyntheticMarginRejectsNullFactorAndNonpositiveConversion()
    {
        var missingFactorBasket = CreateMarginPreviewBasket("USD");
        missingFactorBasket.Components[0].Instrument.MarginFactor = null;
        var missingFactor = SyntheticMarginCalculator.CalculateSide(
            missingFactorBasket,
            "BUY",
            100m,
            "USD",
            1m);

        AssertFalse(missingFactor.IsAvailable, "a null margin factor must make the side unavailable");
        AssertTrue(missingFactor.UnavailableReason.Contains("LEG", StringComparison.Ordinal),
            "a null margin factor must identify the affected epic");
        if (missingFactor.TotalMargin is not null)
        {
            throw new Exception("a null margin factor must not expose a numeric total");
        }

        foreach (var conversionRate in new[] { 0m, -1m })
        {
            var unavailable = SyntheticMarginCalculator.CalculateSide(
                CreateMarginPreviewBasket("EUR"),
                "BUY",
                100m,
                "USD",
                conversionRate);
            AssertFalse(unavailable.IsAvailable,
                $"conversion rate {conversionRate} must make the side unavailable");
            AssertTrue(unavailable.UnavailableReason.Contains("conversion", StringComparison.OrdinalIgnoreCase),
                $"conversion rate {conversionRate} must report a conversion error");
        }
    }

    private static void SyntheticMarginUsesDefaultLotSizeForNullAndZero()
    {
        foreach (var lotSize in new decimal?[] { null, 0m })
        {
            var basket = CreateMarginPreviewBasket("USD");
            var instrument = basket.Components[0].Instrument;
            instrument.Bid = 25m;
            instrument.Offer = 25m;
            instrument.LotSize = lotSize;

            var preview = SyntheticMarginCalculator.CalculateSide(basket, "BUY", 100m, "USD", 1m);

            AssertTrue(preview.IsAvailable, $"lot size {lotSize?.ToString() ?? "null"} must use the default lot");
            AssertNear(4m, preview.Legs[0].Quantity,
                $"lot size {lotSize?.ToString() ?? "null"} must size with an effective lot of one");
            AssertNear(100m, preview.Legs[0].NativeNotional,
                $"lot size {lotSize?.ToString() ?? "null"} must preserve executable notional");
            AssertNear(20m, preview.TotalMargin ?? throw new Exception("default-lot margin must be available"),
                $"lot size {lotSize?.ToString() ?? "null"} must use the same default in margin arithmetic");
        }
    }

    private static void SyntheticMarginReportsUnsupportedMarginUnitsAsUnavailable()
    {
        var basket = new SyntheticBasket { Symbol = "SYN-UNAVAILABLE" };
        basket.Components.Add(new SyntheticComponent(new MarketInstrument
        {
            Epic = "UNSUPPORTED", Currency = "EUR", Bid = 100m, Offer = 100m,
            MarginFactor = 20m, MarginFactorUnit = "POINTS",
        }, 100m, 0m, 0m));

        var preview = SyntheticMarginCalculator.CalculateSide(basket, "BUY", 100m, "USD", 1m);

        AssertFalse(preview.IsAvailable, "unsupported margin factor units must not produce a guessed margin");
        if (preview.TotalMargin is not null) throw new Exception("unavailable margin preview must not expose a numeric total");
        AssertTrue(preview.UnavailableReason.Contains("UNSUPPORTED", StringComparison.Ordinal),
            "unsupported margin result must name the affected epic");

        var summary = SyntheticMarginCalculator.Combine(
            new CapitalAccountSnapshot("active", "USD", 500m, DateTimeOffset.UnixEpoch),
            preview,
            preview);
        if (summary.Buy.TotalMargin is not null || summary.AfterBuy is not null)
        {
            throw new Exception("summary must preserve an unavailable margin as absent rather than zero");
        }
    }

    private static void SyntheticMarginCombinesAccountAvailability()
    {
        var basket = new SyntheticBasket { Symbol = "SYN-ACCOUNT" };
        basket.Components.Add(new SyntheticComponent(new MarketInstrument
        {
            Epic = "ACCOUNT", Currency = "USD", Bid = 100m, Offer = 100m,
            MarginFactor = 20m, MarginFactorUnit = "PERCENTAGE",
        }, 100m, 0m, 0m));
        var buy = SyntheticMarginCalculator.CalculateSide(basket, "BUY", 300m, "usd", 1m);
        var sell = SyntheticMarginCalculator.CalculateSide(basket, "SELL", 200m, "USD", 1m);
        var account = new CapitalAccountSnapshot("active", "USD", 500m, DateTimeOffset.UnixEpoch);

        var summary = SyntheticMarginCalculator.Combine(account, buy, sell);

        AssertNear(440m, summary.AfterBuy ?? throw new Exception("available BUY preview must provide AfterBuy"), "AfterBuy must subtract BUY total margin from available funds");
        AssertNear(460m, summary.AfterSell ?? throw new Exception("available SELL preview must provide AfterSell"), "AfterSell must subtract SELL total margin from available funds");
        AssertEqual("USD", summary.AccountCurrency, "summary uses the active account currency");
    }

    private static void SyntheticMarginRejectsAccountCurrencyMismatch()
    {
        var basket = new SyntheticBasket { Symbol = "SYN-CURRENCY" };
        basket.Components.Add(new SyntheticComponent(new MarketInstrument
        {
            Epic = "CURRENCY", Currency = "USD", Bid = 100m, Offer = 100m,
            MarginFactor = 20m, MarginFactorUnit = "PERCENTAGE",
        }, 100m, 0m, 0m));
        var buy = SyntheticMarginCalculator.CalculateSide(basket, "BUY", 100m, "USD", 1m);
        var sell = SyntheticMarginCalculator.CalculateSide(basket, "SELL", 100m, "EUR", 1m);
        var account = new CapitalAccountSnapshot("active", "USD", 500m, DateTimeOffset.UnixEpoch);

        try
        {
            SyntheticMarginCalculator.Combine(account, buy, sell);
        }
        catch (InvalidOperationException ex)
        {
            AssertTrue(ex.Message.Contains("currency", StringComparison.OrdinalIgnoreCase),
                "currency mismatch must identify the rejected account currency");
            return;
        }

        throw new Exception("Combine must reject a BUY or SELL preview in a different account currency");
    }

    private static void SyntheticMarginPreviewUsesSameCurrencyAndCachesAccount()
    {
        var source = new FakeSyntheticMarginDataSource
        {
            Account = new CapitalAccountSnapshot("active", "USD", 500m, DateTimeOffset.UnixEpoch),
        };
        var service = new SyntheticMarginPreviewService(source);
        var basket = CreateMarginPreviewBasket("USD");

        var first = service.BuildAsync(basket, 1m, CancellationToken.None).GetAwaiter().GetResult();
        var second = service.BuildAsync(basket, 1m, CancellationToken.None).GetAwaiter().GetResult();

        AssertNear(20m, first.Buy.TotalMargin ?? throw new Exception("same-currency BUY margin must be available"),
            "same-currency margin must use a conversion rate of one");
        AssertNear(20m, second.Sell.TotalMargin ?? throw new Exception("same-currency SELL margin must be available"),
            "same-currency SELL margin must use a conversion rate of one");
        AssertEqual(1, source.AccountRequestCount, "active account snapshots must be cached for repeated previews");
        AssertEqual(0, source.SearchQueries.Count, "same-currency previews must not search for a conversion market");
    }

    private static void SyntheticMarginPreviewTreatsDemoAliasAsSameCurrency()
    {
        var source = new FakeSyntheticMarginDataSource
        {
            Account = new CapitalAccountSnapshot("demo", "USDd", 500m, DateTimeOffset.UnixEpoch),
        };

        var summary = new SyntheticMarginPreviewService(source)
            .BuildAsync(CreateMarginPreviewBasket("USD"), 1m, CancellationToken.None).GetAwaiter().GetResult();

        AssertNear(20m, summary.Buy.TotalMargin ?? throw new Exception("USD/USDd margin must be available"),
            "USD and Capital demo alias USDd must use a conversion rate of one");
        AssertEqual(0, source.SearchQueries.Count,
            "USD to USDd must not search for a conversion market");
        AssertEqual("USDd", summary.AccountCurrency,
            "the summary must preserve the original Capital demo account currency label");
        AssertEqual("USDd", summary.Buy.AccountCurrency,
            "margin-side currency must preserve the original Capital demo account currency for Combine validation");
    }

    private static void SyntheticMarginPreviewUsesNormalizedDemoAliasForFxLookup()
    {
        var source = new FakeSyntheticMarginDataSource
        {
            Account = new CapitalAccountSnapshot("demo", "USDd", 500m, DateTimeOffset.UnixEpoch),
        };
        source.SearchResults["EUR/USD"] =
        [
            new MarketInstrument
            {
                Epic = "FX-EURUSD",
                Symbol = "EUR/USD",
                Name = "Euro / US Dollar",
                Type = "CURRENCIES",
            },
        ];
        source.MarketDetails["FX-EURUSD"] = new MarketInstrument
        {
            Epic = "FX-EURUSD",
            Bid = 1.09m,
            Offer = 1.11m,
        };

        var summary = new SyntheticMarginPreviewService(source)
            .BuildAsync(CreateMarginPreviewBasket("EUR"), 1m, CancellationToken.None).GetAwaiter().GetResult();

        AssertNear(22m, summary.Buy.TotalMargin ?? throw new Exception("EUR/USDd margin must be available"),
            "EUR to USDd must use the normalized EUR/USD midpoint");
        AssertEqual(1, source.SearchQueries.Count,
            "a direct normalized demo-currency conversion must issue one market search");
        AssertEqual("EUR/USD", source.SearchQueries[0],
            "EUR to USDd must query the Capital EUR/USD market identity");
        AssertEqual("USDd", summary.AccountCurrency,
            "FX normalization must not alter the displayed account currency");
        AssertEqual("USDd", summary.Buy.Legs[0].AccountCurrency,
            "converted legs must retain the original account currency label");
    }

    private static void SyntheticMarginPreviewEnrichesBlankBasketCurrency()
    {
        var source = new FakeSyntheticMarginDataSource
        {
            Account = new CapitalAccountSnapshot("active", "USD", 500m, DateTimeOffset.UnixEpoch),
        };
        source.MarketDetails["LEG"] = new MarketInstrument
        {
            Epic = "LEG",
            Currency = "USD",
            LotSize = 1m,
            MinDealSize = 1m,
            MinSizeIncrement = 1m,
            MarginFactor = 20m,
            MarginFactorUnit = "PERCENTAGE",
        };
        var basket = CreateMarginPreviewBasket("");

        var summary = new SyntheticMarginPreviewService(source)
            .BuildAsync(basket, 1m, CancellationToken.None).GetAwaiter().GetResult();

        AssertEqual("USD", basket.Components[0].Instrument.Currency,
            "market details must enrich a blank API-fallback basket currency");
        AssertTrue(summary.Buy.IsAvailable,
            "a basket enriched from market details must produce an available margin preview");
        AssertEqual(1, source.MarketDetailRequestCount("LEG"),
            "blank currency must trigger one market-details enrichment request");
    }

    private static void SyntheticMarginPreviewRetainsExpiredAccountSnapshotAsStale()
    {
        var now = DateTimeOffset.Parse("2026-07-28T12:00:00Z");
        var source = new FakeSyntheticMarginDataSource
        {
            Account = new CapitalAccountSnapshot("active", "USD", 500m, now),
        };
        var service = new SyntheticMarginPreviewService(source, () => now);
        var basket = CreateMarginPreviewBasket("USD");
        var fresh = service.BuildAsync(basket, 1m, CancellationToken.None).GetAwaiter().GetResult();
        AssertNear(500m, fresh.Available, "initial successful account availability");

        now = now.AddSeconds(11);
        source.AccountHandler = _ => Task.FromException<CapitalAccountSnapshot>(
            new InvalidOperationException("account refresh failed"));

        var stale = service.BuildAsync(basket, 1m, CancellationToken.None).GetAwaiter().GetResult();
        var serialized = JsonSerializer.Serialize(stale);

        AssertNear(500m, stale.Available,
            "an account refresh failure must retain the last successful availability");
        AssertTrue(serialized.Contains("\"IsAccountStale\":true", StringComparison.Ordinal),
            "retained account availability must be explicitly marked stale");
        AssertTrue(serialized.Contains("account refresh failed", StringComparison.Ordinal),
            "a stale account summary must carry the refresh error");
        AssertEqual(2, source.AccountRequestCount,
            "an expired account snapshot must attempt exactly one refresh before stale fallback");
    }

    private static void SyntheticMarginPreviewCachesUnavailableConversionBriefly()
    {
        var now = DateTimeOffset.Parse("2026-07-28T12:00:00Z");
        var source = new FakeSyntheticMarginDataSource
        {
            Account = new CapitalAccountSnapshot("active", "USD", 500m, now),
        };
        var service = new SyntheticMarginPreviewService(source, () => now);
        var basket = CreateMarginPreviewBasket("EUR");

        for (var attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                service.BuildAsync(basket, 1m, CancellationToken.None).GetAwaiter().GetResult();
                throw new Exception("an unavailable conversion must remain explicit");
            }
            catch (InvalidOperationException ex) when (ex.Message ==
                "Margin conversion EUR/USD is unavailable from Capital.com.")
            {
            }
        }

        AssertEqual(2, source.SearchQueries.Count,
            "a cached FX failure must suppress repeat direct and inverse searches during the short TTL");

        now = now.AddSeconds(6);
        try
        {
            service.BuildAsync(basket, 1m, CancellationToken.None).GetAwaiter().GetResult();
            throw new Exception("an unavailable conversion must remain explicit after cache expiry");
        }
        catch (InvalidOperationException ex) when (ex.Message ==
            "Margin conversion EUR/USD is unavailable from Capital.com.")
        {
        }

        AssertEqual(4, source.SearchQueries.Count,
            "an unavailable FX lookup must retry after its bounded failure TTL expires");
    }

    private static void SyntheticMarginPreviewUsesDirectMidpointAndRefreshesMissingMetadata()
    {
        var source = new FakeSyntheticMarginDataSource
        {
            Account = new CapitalAccountSnapshot("active", "USD", 500m, DateTimeOffset.UnixEpoch),
        };
        source.SearchResults["EUR/USD"] =
        [
            new MarketInstrument
            {
                Epic = "FX-EURUSD",
                Symbol = "EUR/USD",
                Name = "Euro / US Dollar",
                Type = "CURRENCIES",
            },
        ];
        source.MarketDetails["FX-EURUSD"] = new MarketInstrument
        {
            Epic = "FX-EURUSD",
            Bid = 1.09m,
            Offer = 1.11m,
        };
        source.MarketDetails["LEG"] = new MarketInstrument
        {
            Epic = "LEG",
            LotSize = 1m,
            MinDealSize = 1m,
            MinSizeIncrement = 1m,
            MarginFactor = 20m,
            MarginFactorUnit = "PERCENTAGE",
        };
        var basket = CreateMarginPreviewBasket("EUR", includeMarginMetadata: false);

        var summary = new SyntheticMarginPreviewService(source)
            .BuildAsync(basket, 1m, CancellationToken.None).GetAwaiter().GetResult();

        AssertNear(22m, summary.Buy.TotalMargin ?? throw new Exception("direct-conversion BUY margin must be available"),
            "EUR/USD bid 1.09 and offer 1.11 must use midpoint 1.10");
        AssertNear(20m, basket.Components[0].Instrument.MarginFactor ?? 0m,
            "missing leg margin metadata must be refreshed before calculation");
        AssertNear(1m, basket.Components[0].Instrument.MinSizeIncrement ?? 0m,
            "missing leg dealing rules must be refreshed before calculation");
    }

    private static void SyntheticMarginPreviewUsesReciprocalInverseQuote()
    {
        var source = new FakeSyntheticMarginDataSource
        {
            Account = new CapitalAccountSnapshot("active", "USD", 500m, DateTimeOffset.UnixEpoch),
        };
        source.SearchResults["USD/EUR"] =
        [
            new MarketInstrument
            {
                Epic = "FX-USDEUR",
                Symbol = "USD/EUR",
                Name = "US Dollar / Euro",
                Type = "CURRENCIES",
            },
        ];
        source.MarketDetails["FX-USDEUR"] = new MarketInstrument
        {
            Epic = "FX-USDEUR",
            Bid = 0.90m,
            Offer = 0.92m,
        };

        var summary = new SyntheticMarginPreviewService(source)
            .BuildAsync(CreateMarginPreviewBasket("EUR"), 1m, CancellationToken.None).GetAwaiter().GetResult();

        AssertNear(20m / 0.91m, summary.Buy.TotalMargin ?? throw new Exception("inverse-conversion BUY margin must be available"),
            "an inverse USD/EUR quote must use the reciprocal midpoint");
    }

    private static void SyntheticMarginPreviewRejectsMissingConversion()
    {
        var source = new FakeSyntheticMarginDataSource
        {
            Account = new CapitalAccountSnapshot("active", "USD", 500m, DateTimeOffset.UnixEpoch),
        };

        try
        {
            new SyntheticMarginPreviewService(source)
                .BuildAsync(CreateMarginPreviewBasket("EUR"), 1m, CancellationToken.None).GetAwaiter().GetResult();
        }
        catch (InvalidOperationException ex)
        {
            AssertEqual("Margin conversion EUR/USD is unavailable from Capital.com.", ex.Message,
                "missing direct and inverse conversion quotes must fail explicitly");
            return;
        }

        throw new Exception("missing conversion markets must not produce a guessed margin preview");
    }

    private static void CapComTerminalRefreshesMarginPreviewContract()
    {
        var source = File.ReadAllText(SourcePath("desktop", "CAPETF.Desktop", "CapComTerminalWindow.xaml.cs"));
        foreach (var required in new[]
        {
            "previewMargins",
            "SyntheticMarginPreviewService",
            ".BuildAsync(",
            "SetTerminalBusyAsync(true",
            "SetTerminalBusyAsync(false",
            "window.setTerminalMarginPreview",
            "Task.Delay(TimeSpan.FromMilliseconds(500)",
            "Cancel()",
        })
        {
            if (!source.Contains(required, StringComparison.Ordinal))
            {
                throw new Exception($"cap.com Terminal margin-preview orchestration missing {required}");
            }
        }
    }

    private static void MarginPreviewPublicationRejectsSupersededFailures()
    {
        var requestBasket = CreateMarginPreviewBasket("USD");
        var replacementBasket = CreateMarginPreviewBasket("USD");
        using var first = new CancellationTokenSource();
        using var replacement = new CancellationTokenSource();

        AssertFalse(
            SyntheticMarginPreviewPublication.IsCurrent(first.Token, first, replacement, requestBasket, requestBasket),
            "a non-cancellation failure must not publish after a replacement request owns the window");

        var firstToken = first.Token;
        first.Cancel();
        AssertFalse(
            SyntheticMarginPreviewPublication.IsCurrent(firstToken, first, first, requestBasket, requestBasket),
            "a canceled request must not publish a non-cancellation failure");

        AssertFalse(
            SyntheticMarginPreviewPublication.IsCurrent(replacement.Token, replacement, replacement, requestBasket, replacementBasket),
            "a request must not publish after the active basket identity changes");
        AssertTrue(
            SyntheticMarginPreviewPublication.IsCurrent(replacement.Token, replacement, replacement, requestBasket, requestBasket),
            "only the uncanceled owner for the same basket may publish a margin result");
    }

    private static void SyntheticMarginPreviewRejectsReversedDirectPair()
    {
        var source = new FakeSyntheticMarginDataSource
        {
            Account = new CapitalAccountSnapshot("active", "USD", 500m, DateTimeOffset.UnixEpoch),
        };
        var reversed = new MarketInstrument
        {
            Epic = "FX-USDEUR",
            Symbol = "USD/EUR",
            Name = "US Dollar / Euro",
            Type = "CURRENCIES",
        };
        source.SearchResults["EUR/USD"] = [reversed];
        source.SearchResults["USD/EUR"] = [reversed];
        source.MarketDetails[reversed.Epic] = new MarketInstrument
        {
            Epic = reversed.Epic,
            Bid = 0.90m,
            Offer = 0.92m,
        };

        var summary = new SyntheticMarginPreviewService(source)
            .BuildAsync(CreateMarginPreviewBasket("EUR"), 1m, CancellationToken.None).GetAwaiter().GetResult();

        AssertNear(20m / 0.91m, summary.Buy.TotalMargin ?? throw new Exception("inverse BUY margin must be available"),
            "a reversed symbol returned by direct search must be rejected and resolved through reciprocal inverse lookup");
        AssertEqual(1, source.MarketDetailRequestCount(reversed.Epic),
            "a reversed direct result must not fetch details until it is evaluated in inverse order");
    }

    private static void SyntheticMarginPreviewTriesEveryOrderedFxCandidate()
    {
        var source = new FakeSyntheticMarginDataSource
        {
            Account = new CapitalAccountSnapshot("active", "USD", 500m, DateTimeOffset.UnixEpoch),
        };
        source.SearchResults["EUR/USD"] =
        [
            new MarketInstrument { Epic = "FX-CLOSED", Symbol = "EUR/USD", Type = "CURRENCIES" },
            new MarketInstrument { Epic = "FX-LIVE", Symbol = "EURUSD", Type = "CURRENCIES" },
        ];
        source.MarketDetails["FX-CLOSED"] = new MarketInstrument
        {
            Epic = "FX-CLOSED",
            Bid = 1.08m,
            Offer = null,
        };
        source.MarketDetails["FX-LIVE"] = new MarketInstrument
        {
            Epic = "FX-LIVE",
            Bid = 1.09m,
            Offer = 1.11m,
        };

        var summary = new SyntheticMarginPreviewService(source)
            .BuildAsync(CreateMarginPreviewBasket("EUR"), 1m, CancellationToken.None).GetAwaiter().GetResult();

        AssertNear(22m, summary.Buy.TotalMargin ?? throw new Exception("later direct FX candidate must be usable"),
            "direct lookup must continue after an ordered candidate lacks a two-sided quote");
        AssertEqual(1, source.MarketDetailRequestCount("FX-CLOSED"), "first ordered candidate must be inspected once");
        AssertEqual(1, source.MarketDetailRequestCount("FX-LIVE"), "later ordered candidate must be inspected once");
    }

    private static void SyntheticMarginPreviewMatchesOrderedDescriptiveNameOnlyPair()
    {
        var source = new FakeSyntheticMarginDataSource
        {
            Account = new CapitalAccountSnapshot("active", "USD", 500m, DateTimeOffset.UnixEpoch),
        };
        source.SearchResults["EUR/USD"] =
        [
            new MarketInstrument
            {
                Epic = "FX-NAME-REVERSED",
                Symbol = "",
                Name = "US Dollar / Euro",
                Type = "CURRENCIES",
            },
            new MarketInstrument
            {
                Epic = "FX-NAME-DIRECT",
                Symbol = "",
                Name = "Euro / US Dollar",
                Type = "CURRENCIES",
            },
        ];
        source.MarketDetails["FX-NAME-REVERSED"] = new MarketInstrument
        {
            Epic = "FX-NAME-REVERSED",
            Bid = 0.90m,
            Offer = 0.92m,
        };
        source.MarketDetails["FX-NAME-DIRECT"] = new MarketInstrument
        {
            Epic = "FX-NAME-DIRECT",
            Bid = 1.09m,
            Offer = 1.11m,
        };

        var summary = new SyntheticMarginPreviewService(source)
            .BuildAsync(CreateMarginPreviewBasket("EUR"), 1m, CancellationToken.None).GetAwaiter().GetResult();

        AssertNear(22m, summary.Buy.TotalMargin ?? throw new Exception("descriptive name-only FX margin must be available"),
            "Euro / US Dollar must match EUR-to-USD when the symbol has no currency codes");
        AssertEqual(0, source.MarketDetailRequestCount("FX-NAME-REVERSED"),
            "US Dollar / Euro must not match the EUR-to-USD direct pass");
        AssertEqual(1, source.MarketDetailRequestCount("FX-NAME-DIRECT"),
            "the ordered descriptive name-only candidate must provide the direct midpoint");
    }

    private static void SyntheticMarginPreviewInvalidatesAllCaches()
    {
        var now = DateTimeOffset.Parse("2026-07-28T12:00:00Z");
        var source = new FakeSyntheticMarginDataSource
        {
            Account = new CapitalAccountSnapshot("session-one", "USD", 500m, now),
        };
        source.SearchResults["EUR/USD"] =
        [
            new MarketInstrument { Epic = "FX-EURUSD", Symbol = "EUR/USD", Type = "CURRENCIES" },
        ];
        source.MarketDetails["FX-EURUSD"] = new MarketInstrument
        {
            Epic = "FX-EURUSD",
            Bid = 1.09m,
            Offer = 1.11m,
        };
        source.MarketDetails["LEG"] = new MarketInstrument { Epic = "LEG", LotSize = 1m };
        var service = new SyntheticMarginPreviewService(source, () => now);
        var basket = CreateMarginPreviewBasket("EUR", includeMarginMetadata: false);

        var first = service.BuildAsync(basket, 1m, CancellationToken.None).GetAwaiter().GetResult();
        AssertNear(500m, first.Available, "first session account availability");

        source.Account = new CapitalAccountSnapshot("session-two", "USD", 900m, now);
        source.MarketDetails["FX-EURUSD"] = new MarketInstrument
        {
            Epic = "FX-EURUSD",
            Bid = 1.19m,
            Offer = 1.21m,
        };
        service.InvalidateCaches();
        var second = service.BuildAsync(basket, 1m, CancellationToken.None).GetAwaiter().GetResult();

        AssertNear(900m, second.Available, "cache invalidation must fetch the new session's active account");
        AssertEqual(2, source.AccountRequestCount, "active-account cache must not cross successful logins");
        AssertEqual(2, source.MarketDetailRequestCount("FX-EURUSD"), "conversion cache must be cleared after login");
        AssertEqual(2, source.MarketDetailRequestCount("LEG"), "metadata-attempt cache must be cleared after login");
    }

    private static void CapComTerminalInvalidatesMarginCachesAfterEveryLogin()
    {
        var source = File.ReadAllText(SourcePath("desktop", "CAPETF.Desktop", "CapComTerminalWindow.xaml.cs"));
        var loginCount = source.Split("await _api.LoginAsync", StringSplitOptions.None).Length - 1;
        var resetCount = source.Split("await ResetMarginPreviewAfterLoginAsync();", StringSplitOptions.None).Length - 1;

        AssertEqual(2, loginCount, "window login contract must cover explicit connect and ensure-connected login");
        AssertEqual(loginCount, resetCount, "every successful window login must immediately reset margin-preview state");
        AssertTrue(source.Contains("CancelMarginPreviewRequest();", StringComparison.Ordinal) &&
                   source.Contains("_marginPreviewRefresh = null", StringComparison.Ordinal),
            "a successful login must relinquish and cancel an in-flight preview from the prior session");
        AssertTrue(source.Contains("_marginPreview.InvalidateCaches();", StringComparison.Ordinal),
            "a successful login must invalidate all margin-preview caches");
    }

    private static void SyntheticMarginMetadataAttemptsCachePartialAndFailedResponses()
    {
        var partialSource = new FakeSyntheticMarginDataSource
        {
            Account = new CapitalAccountSnapshot("active", "USD", 500m, DateTimeOffset.UnixEpoch),
        };
        partialSource.MarketDetails["LEG"] = new MarketInstrument { Epic = "LEG", LotSize = 1m };
        var partialService = new SyntheticMarginPreviewService(partialSource);
        var partialBasket = CreateMarginPreviewBasket("USD", includeMarginMetadata: false);

        partialService.BuildAsync(partialBasket, 1m, CancellationToken.None).GetAwaiter().GetResult();
        partialService.BuildAsync(partialBasket, 1m, CancellationToken.None).GetAwaiter().GetResult();
        AssertEqual(1, partialSource.MarketDetailRequestCount("LEG"),
            "partial metadata attempts must suppress repeated details requests inside the attempt TTL");

        var failedSource = new FakeSyntheticMarginDataSource
        {
            Account = new CapitalAccountSnapshot("active", "USD", 500m, DateTimeOffset.UnixEpoch),
            MarketDetailsHandler = (epic, _) => epic == "LEG"
                ? Task.FromException<MarketInstrument?>(new InvalidOperationException("details failed"))
                : Task.FromResult<MarketInstrument?>(null),
        };
        var failedService = new SyntheticMarginPreviewService(failedSource);
        var failedBasket = CreateMarginPreviewBasket("USD", includeMarginMetadata: false);
        for (var attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                failedService.BuildAsync(failedBasket, 1m, CancellationToken.None).GetAwaiter().GetResult();
                throw new Exception("failed metadata request must remain observable to the preview caller");
            }
            catch (InvalidOperationException ex) when (ex.Message == "details failed")
            {
            }
        }
        AssertEqual(1, failedSource.MarketDetailRequestCount("LEG"),
            "failed metadata attempts must suppress repeated details requests inside the attempt TTL");
    }

    private static void SyntheticMarginMetadataAttemptsRetryAfterExpiry()
    {
        var now = DateTimeOffset.Parse("2026-07-28T12:00:00Z");
        var source = new FakeSyntheticMarginDataSource
        {
            Account = new CapitalAccountSnapshot("active", "USD", 500m, now),
        };
        source.MarketDetails["LEG"] = new MarketInstrument { Epic = "LEG", LotSize = 1m };
        var service = new SyntheticMarginPreviewService(source, () => now);
        var basket = CreateMarginPreviewBasket("USD", includeMarginMetadata: false);

        service.BuildAsync(basket, 1m, CancellationToken.None).GetAwaiter().GetResult();
        now = now.AddSeconds(29);
        service.BuildAsync(basket, 1m, CancellationToken.None).GetAwaiter().GetResult();
        AssertEqual(1, source.MarketDetailRequestCount("LEG"), "metadata attempt cache must remain active before 30 seconds");

        now = now.AddSeconds(2);
        service.BuildAsync(basket, 1m, CancellationToken.None).GetAwaiter().GetResult();
        AssertEqual(2, source.MarketDetailRequestCount("LEG"), "incomplete metadata must be retried after 30 seconds");
    }

    private static void SyntheticMarginMetadataRefreshIsSingleFlight()
    {
        var completion = new TaskCompletionSource<MarketInstrument?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var source = new FakeSyntheticMarginDataSource
        {
            Account = new CapitalAccountSnapshot("active", "USD", 500m, DateTimeOffset.UnixEpoch),
            MarketDetailsHandler = (epic, _) => epic == "LEG"
                ? completion.Task
                : Task.FromResult<MarketInstrument?>(null),
        };
        var service = new SyntheticMarginPreviewService(source);
        var basket = CreateMarginPreviewBasket("USD", includeMarginMetadata: false);
        var first = service.BuildAsync(basket, 1m, CancellationToken.None);
        var second = service.BuildAsync(basket, 1m, CancellationToken.None);

        try
        {
            AssertEqual(1, source.MarketDetailRequestCount("LEG"),
                "concurrent builds must share one metadata details request per epic");
        }
        finally
        {
            completion.TrySetResult(new MarketInstrument
            {
                Epic = "LEG",
                LotSize = 1m,
                MinDealSize = 1m,
                MinSizeIncrement = 1m,
                MarginFactor = 20m,
                MarginFactorUnit = "PERCENTAGE",
            });
        }

        Task.WhenAll(first, second).GetAwaiter().GetResult();
        AssertEqual(1, source.MarketDetailRequestCount("LEG"), "single-flight request count after both builds complete");
    }

    private static void SyntheticMarginMetadataRefreshRemainsSingleFlightWhileRunningPastTtl()
    {
        var now = DateTimeOffset.Parse("2026-07-28T12:00:00Z");
        var completion = new TaskCompletionSource<MarketInstrument?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var source = new FakeSyntheticMarginDataSource
        {
            Account = new CapitalAccountSnapshot("active", "USD", 500m, now),
            MarketDetailsHandler = (epic, _) => epic == "LEG"
                ? completion.Task
                : Task.FromResult<MarketInstrument?>(null),
        };
        var service = new SyntheticMarginPreviewService(source, () => now);
        var basket = CreateMarginPreviewBasket("USD", includeMarginMetadata: false);
        var first = service.BuildAsync(basket, 1m, CancellationToken.None);

        now = now.AddSeconds(31);
        var second = service.BuildAsync(basket, 1m, CancellationToken.None);
        try
        {
            AssertEqual(1, source.MarketDetailRequestCount("LEG"),
                "an incomplete metadata request must remain single-flight after the retry TTL elapses");
        }
        finally
        {
            completion.TrySetResult(new MarketInstrument
            {
                Epic = "LEG",
                LotSize = 1m,
                MinDealSize = 1m,
                MinSizeIncrement = 1m,
                MarginFactor = 20m,
                MarginFactorUnit = "PERCENTAGE",
            });
        }

        Task.WhenAll(first, second).GetAwaiter().GetResult();
        AssertEqual(1, source.MarketDetailRequestCount("LEG"),
            "the shared long-running metadata request must remain the only source call after completion");
    }

    private static SyntheticBasket CreateMarginPreviewBasket(string currency, bool includeMarginMetadata = true)
    {
        var basket = new SyntheticBasket { Symbol = "SYN-MARGIN-PREVIEW" };
        basket.Components.Add(new SyntheticComponent(new MarketInstrument
        {
            Epic = "LEG",
            Currency = currency,
            Bid = 100m,
            Offer = 100m,
            LotSize = includeMarginMetadata ? 1m : null,
            MinDealSize = includeMarginMetadata ? 1m : null,
            MinSizeIncrement = includeMarginMetadata ? 1m : null,
            MarginFactor = includeMarginMetadata ? 20m : null,
            MarginFactorUnit = includeMarginMetadata ? "PERCENTAGE" : "",
        }, 100m, 0m, 0m));
        return basket;
    }

    private static void OutOfOrderStreamingQuoteDoesNotMutateComponentState()
    {
        var sourceTime = DateTimeOffset.Parse("2026-07-27T14:00:00Z");
        var basket = CreateLiveBasket("SYN-ORDERED", "ORDERED-LEG", 100m, sourceTime);
        var component = basket.Components.Single();
        component.Instrument.Bid = 99m;
        component.Instrument.Offer = 101m;
        component.Instrument.LastTickAt = sourceTime;
        var candle = basket.Candles[^1];

        var result = SyntheticLiveUpdate.ApplyQuote(
            basket,
            new QuoteUpdate("ORDERED-LEG", 89m, 91m, 90m, sourceTime.AddSeconds(-1)));

        AssertFalse(result.Matched, "an out-of-order stream quote must be rejected before a terminal tick is emitted");
        AssertNear(99m, component.Instrument.Bid ?? 0m, "an out-of-order quote must not rewind bid state");
        AssertNear(101m, component.Instrument.Offer ?? 0m, "an out-of-order quote must not rewind offer state");
        AssertNear(100m, component.Instrument.Price ?? 0m, "an out-of-order quote must not rewind component price");
        AssertEqual(sourceTime, component.Instrument.LastTickAt, "an out-of-order quote must not rewind source time");
        AssertEqual(candle, basket.Candles[^1], "an out-of-order quote must not mutate the synthetic candle");
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
                UniverseKind = TerminalUniverseKind.ETFs,
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

            store.Save(SavedSyntheticBasket.FromBasket(
                "My SAP basket",
                SyntheticStrategyKind.DipInsideUptrend,
                basket));
            var saved = store.LoadAll().Single();

            if (saved.Name != "My SAP basket") throw new Exception("saved basket should preserve user name");
            if (saved.Strategy != SyntheticStrategyKind.DipInsideUptrend) throw new Exception("saved basket should preserve strategy");
            AssertEqual<TerminalUniverseKind?>(TerminalUniverseKind.ETFs, saved.UniverseKind,
                "saved basket should preserve explicit universe identity");
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
        IReadOnlyDictionary<string, IReadOnlyList<OhlcPoint>> candles,
        int periodsPerYear = 52)
    {
        var top = SyntheticStrategyRanker.Rank(strategy, instruments, candles, periodsPerYear, maximum: 5).FirstOrDefault();
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

    private static void SavedManualFormulaIdentityIncludesExactRatios()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"capetf-saved-ratios-{Guid.NewGuid():N}");
        try
        {
            SyntheticBasket Basket(decimal ethMultiplier, decimal btcMultiplier)
            {
                var basket = new SyntheticBasket
                {
                    Symbol = "SYN-CRYPTO-ETHBTC-01",
                    Block = "Crypto / USD / All",
                    UniverseKind = TerminalUniverseKind.Crypto,
                    BasketPrice = 100m,
                    LastUpdated = DateTimeOffset.Parse("2026-08-01T10:00:00Z"),
                };
                basket.Components.Add(new SyntheticComponent(
                    CreateCrypto("CS.D.ETHUSD.CFD.IP", "ETHUSD", "Ethereum", "USD", 1999m, 2001m),
                    50m, 10m, 0m)
                {
                    FormulaMultiplier = ethMultiplier,
                    FormulaReferencePrice = 2000m,
                });
                basket.Components.Add(new SyntheticComponent(
                    CreateCrypto("CS.D.BTCUSD.CFD.IP", "BTCUSD", "Bitcoin", "USD", 29990m, 30010m),
                    50m, 10m, 0m)
                {
                    FormulaMultiplier = btcMultiplier,
                    FormulaReferencePrice = 30000m,
                });
                return basket;
            }

            var store = new SavedSyntheticBasketStore(folder);
            store.Save(SavedSyntheticBasket.FromBasket(
                "ETH BTC manual", SyntheticStrategyKind.ManualFormula, Basket(9m, 0.2m)));
            store.Save(SavedSyntheticBasket.FromBasket(
                "ETH BTC manual", SyntheticStrategyKind.ManualFormula, Basket(1m, 0.1m)));

            var saved = store.LoadAll();
            AssertEqual(2, saved.Count, "manual formulas with the same epics but different ratios must not overwrite each other");
            AssertEqual(2, saved.Select(item => item.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                "manual formula IDs must include canonical ratio identity");
            AssertTrue(saved.Any(item => item.Components[0].FormulaMultiplier == 9m && item.Components[1].FormulaMultiplier == 0.2m),
                "the exact 9 ETH plus 0.2 BTC formula must remain staged");
            AssertTrue(saved.Any(item => item.Components[0].FormulaMultiplier == 1m && item.Components[1].FormulaMultiplier == 0.1m),
                "the second ETH BTC ratio must remain independently staged");
        }
        finally
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
        }
    }

    private static void ManualFormulaBuildsExactCryptoPresetWithoutEqualNotionalRewriting()
    {
        var day = DateTimeOffset.Parse("2026-07-01T00:00:00Z");
        var eth = CreateCrypto("CS.D.ETHUSD.CFD.IP", "ETHUSD", "Ethereum / US Dollar", "USD", 1999m, 2001m);
        var btc = CreateCrypto("CS.D.BTCUSD.CFD.IP", "BTCUSD", "Bitcoin / US Dollar", "USD", 29990m, 30010m);
        var formula = ManualSyntheticFormula.Parse("9 ETHUSD + 0.2 BTCUSD");
        var candles = new Dictionary<string, IReadOnlyList<OhlcPoint>>(StringComparer.OrdinalIgnoreCase)
        {
            [eth.Epic] =
            [
                new OhlcPoint(day, 100m, 110m, 90m, 105m),
                new OhlcPoint(day.AddDays(1), 200m, 220m, 180m, 210m),
                new OhlcPoint(day.AddDays(2), 300m, 330m, 270m, 315m),
            ],
            [btc.Epic] =
            [
                new OhlcPoint(day, 1000m, 1050m, 950m, 1020m),
                new OhlcPoint(day.AddDays(2), 1200m, 1260m, 1140m, 1230m),
            ],
        };

        var basket = ManualSyntheticBasketFactory.Create(
            "SYN-ETHBTC-01",
            "Crypto / USD / All",
            formula,
            [eth, btc],
            candles);

        AssertEqual(2, basket.Components.Count, "manual preset must contain exactly two legs");
        AssertEqual<TerminalUniverseKind?>(TerminalUniverseKind.Crypto, basket.UniverseKind,
            "manual basket build freezes Crypto universe identity");
        AssertEqual(eth.Epic, basket.Components[0].Instrument.Epic, "ETH leg must remain first");
        AssertEqual(btc.Epic, basket.Components[1].Instrument.Epic, "BTC leg must remain second");
        AssertEqual(9m, basket.Components[0].FormulaMultiplier, "ETH multiplier must remain exact");
        AssertEqual(0.2m, basket.Components[1].FormulaMultiplier, "BTC multiplier must remain exact");
        AssertEqual("USD", basket.Components.Select(component => component.Instrument.Currency).Distinct().Single(), "manual legs must retain USD");
        AssertEqual(2, basket.Candles.Count, "manual history must use strict timestamp intersection");
        AssertEqual(day.AddDays(2), basket.Candles[1].Time, "unshared timestamps must not be synthesized");
        AssertNear(1100m, basket.Candles[0].Open, "manual open must use direct source-price scale");
        AssertNear(1200m, basket.Candles[0].High, "manual high must use direct formula math");
        AssertNear(1000m, basket.Candles[0].Low, "manual low must use direct formula math");
        AssertNear(1149m, basket.Candles[0].Close, "manual close must use direct formula math");
        AssertNear(2940m, basket.Candles[1].Open, "manual candle opens must not be rebased or replaced with the prior close");
        AssertNear(3081m, basket.BasketPrice, "manual basket price must remain on direct formula scale");
        AssertNear(23989m, basket.BidPrice ?? 0m, "manual bid must use exact multipliers");
        AssertNear(24011m, basket.AskPrice ?? 0m, "manual ask must use exact multipliers");
    }

    private static void ManualHistoryUsesExactUtcTimestampsAtDailyAndWeeklyResolutions()
    {
        var monday = DateTimeOffset.Parse("2026-07-06T00:00:00Z");
        var eth = CreateCrypto("CS.D.ETHUSD.CFD.IP", "ETHUSD", "Ethereum", "USD", 1999m, 2001m);
        var btc = CreateCrypto("CS.D.BTCUSD.CFD.IP", "BTCUSD", "Bitcoin", "USD", 29990m, 30010m);
        var exact = monday.AddDays(14);
        var candles = new Dictionary<string, IReadOnlyList<OhlcPoint>>(StringComparer.OrdinalIgnoreCase)
        {
            [eth.Epic] =
            [
                FlatCandle(monday, 100m),
                FlatCandle(monday.AddDays(7), 101m),
                FlatCandle(exact, 102m),
            ],
            [btc.Epic] =
            [
                FlatCandle(monday.AddHours(12), 1000m),
                FlatCandle(monday.AddDays(7).AddHours(12), 1001m),
                FlatCandle(exact, 1002m),
            ],
        };

        foreach (var timeframe in new[] { "Daily", "Weekly" })
        {
            var merged = SyntheticHistoryService.MergeSelectedManualHistory(
                [eth, btc],
                timeframe,
                new HistoryLoadResult(candles, monday, exact, 3),
                new Dictionary<string, IReadOnlyList<OhlcPoint>>(StringComparer.OrdinalIgnoreCase));
            AssertEqual(1, merged.SharedCount,
                $"{timeframe} manual selected-history merge must count exact UTC matches only");
            AssertEqual(exact, merged.SharedStart,
                $"{timeframe} manual selected-history start must use the exact shared timestamp");
            AssertEqual(exact, merged.SharedEnd,
                $"{timeframe} manual selected-history end must use the exact shared timestamp");

            var basket = ManualSyntheticBasketFactory.Create(
                $"SYN-EXACT-{timeframe}",
                "Crypto / USD / All",
                ManualSyntheticFormula.Parse(ManualSyntheticFormula.CryptoPreset),
                [eth, btc],
                merged.CandlesByEpic,
                timeframe,
                minimumCandles: 1);

            AssertSequence(basket.Candles.Select(candle => candle.Time), exact);
            AssertNear(9m * 102m + 0.2m * 1002m, basket.Candles.Single().Close,
                $"{timeframe} manual history must not combine same-date or same-week timestamps");
        }
    }

    private static void ManualCryptoHistoryAndRealtimeBarsUseDirectSharedFormula()
    {
        var day = DateTimeOffset.Parse("2026-07-27T00:00:00Z");
        var eth = CreateCrypto("CS.D.ETHUSD.CFD.IP", "ETHUSD", "Ethereum / US Dollar", "USD", 1999m, 2001m);
        var btc = CreateCrypto("CS.D.BTCUSD.CFD.IP", "BTCUSD", "Bitcoin / US Dollar", "USD", 29990m, 30010m);
        var candles = new Dictionary<string, IReadOnlyList<OhlcPoint>>(StringComparer.OrdinalIgnoreCase)
        {
            [eth.Epic] =
            [
                new OhlcPoint(day, 100m, 110m, 90m, 105m),
                new OhlcPoint(day.AddDays(1), 200m, 220m, 180m, 210m),
                new OhlcPoint(day.AddDays(2), 300m, 330m, 270m, 315m),
            ],
            [btc.Epic] =
            [
                new OhlcPoint(day, 1000m, 1050m, 950m, 1020m),
                new OhlcPoint(day.AddDays(2), 1200m, 1260m, 1140m, 1230m),
            ],
        };
        var basket = ManualSyntheticBasketFactory.Create(
            "SYN-CRYPTO-ETHBTC-01",
            "Crypto / USD / All",
            ManualSyntheticFormula.Parse(ManualSyntheticFormula.CryptoPreset),
            [eth, btc],
            candles,
            "Daily",
            minimumCandles: 2);

        AssertSequence(basket.Candles.Select(candle => candle.Time), day, day.AddDays(2));
        AssertSequence(
            basket.Candles.Select(candle => (candle.Open, candle.High, candle.Low, candle.Close)),
            (1100m, 1200m, 1000m, 1149m),
            (2940m, 3222m, 2658m, 3081m));
        AssertNear(23989m, basket.BidPrice ?? 0m, "manual crypto bid is 9 * ETH bid + 0.2 * BTC bid");
        AssertNear(24011m, basket.AskPrice ?? 0m, "manual crypto ask is 9 * ETH offer + 0.2 * BTC offer");

        var historical = basket.Candles.ToArray();
        var quoteResult = SyntheticTerminalLiveUpdate.Apply(
            basket,
            new QuoteUpdate(eth.Epic, 2009m, 2011m, 2010m, day.AddDays(3).AddMinutes(1)),
            now: day.AddDays(3).AddMinutes(1),
            timeframe: "Daily");
        AssertTrue(quoteResult.Matched, "manual ETH tick must use the shared quote route");
        AssertEqual(historical.Length + 1, basket.Candles.Count, "a live tick appends only the ongoing candle");
        AssertSequence(basket.Candles.Take(historical.Length), historical);
        AssertNear(9m * 2009m + 0.2m * 29990m, basket.BidPrice ?? 0m, "live ETH tick refreshes formula bid");
        AssertNear(9m * 2011m + 0.2m * 30010m, basket.AskPrice ?? 0m, "live ETH tick refreshes formula ask");

        var builder = new SyntheticRealtimeBarBuilder();
        builder.Reset(basket, "Daily");
        var barTime = day.AddDays(3);
        AssertFalse(builder.Apply(basket, new CapitalOhlcUpdate(
            eth.Epic, "DAY", barTime, 400m, 440m, 360m, 420m)),
            "one component bar cannot synthesize an incomplete basket candle");
        AssertTrue(builder.Apply(basket, new CapitalOhlcUpdate(
            btc.Epic, "DAY", barTime, 1400m, 1470m, 1330m, 1435m)),
            "the second shared component bar completes the synthetic ongoing candle");
        AssertEqual(historical.Length + 1, basket.Candles.Count, "OHLC completion updates the ongoing candle without truncating history");
        AssertSequence(basket.Candles.Take(historical.Length), historical);
        AssertEqual(new OhlcPoint(barTime, 3880m, 4254m, 3506m, 4067m), basket.Candles[^1],
            "ongoing OHLC must use the direct 9 ETH + 0.2 BTC formula");

        AssertTrue(builder.Apply(basket, new CapitalOhlcUpdate(
            eth.Epic, "DAY", barTime, 400m, 450m, 350m, 425m)),
            "an updated component bar must replace only the current synthetic candle");
        AssertEqual(historical.Length + 1, basket.Candles.Count, "same-bucket OHLC updates cannot append duplicates");
        AssertSequence(basket.Candles.Take(historical.Length), historical);
        AssertEqual(new OhlcPoint(barTime, 3880m, 4344m, 3416m, 4112m), basket.Candles[^1],
            "same-bucket component changes must recompute direct synthetic OHLC");
    }

    private static void AutomaticBasketIgnoresNativeOhlcEvents()
    {
        var time = DateTimeOffset.Parse("2026-07-27T00:00:00Z");
        foreach (var (label, instrumentType) in new[] { ("stock", "SHARES"), ("ETF", "ETF") })
        {
            var basket = new SyntheticBasket
            {
                Symbol = $"SYN-AUTOMATIC-{label}",
                Strategy = SyntheticStrategyKind.DipInsideUptrend,
                BasketPrice = 100m,
                LastUpdated = time,
            };
            foreach (var (suffix, price, multiplier) in new[]
            {
                ("A", 100m, 0.4m),
                ("B", 200m, 0.2m),
                ("C", 300m, 0.1m),
            })
            {
                basket.Components.Add(new SyntheticComponent(
                    new MarketInstrument
                    {
                        Epic = $"{label}-{suffix}",
                        Name = $"{label} {suffix}",
                        Type = instrumentType,
                        Price = price,
                    },
                    100m / 3m, 10m, 10m) { FormulaMultiplier = multiplier });
            }
            basket.Candles.Add(FlatCandle(time, 100m));
            var originalCandles = basket.Candles.ToArray();
            var originalPrice = basket.BasketPrice;
            var originalUpdated = basket.LastUpdated;

            var builder = new SyntheticRealtimeBarBuilder();
            builder.Reset(basket, "Daily");
            foreach (var component in basket.Components)
            {
                AssertFalse(builder.Apply(basket, new CapitalOhlcUpdate(
                    component.Instrument.Epic, "DAY", time.AddDays(1), 110m, 120m, 100m, 115m)),
                    $"automatic {label} baskets must ignore native OHLC events");
            }
            AssertSequence(basket.Candles, originalCandles);
            AssertEqual(originalPrice, basket.BasketPrice, $"automatic {label} price must remain quote-driven");
            AssertEqual(originalUpdated, basket.LastUpdated, $"automatic {label} timestamp must remain quote-driven");

            var socket = new FakeCapitalStreamingSocket(WebSocketState.Open);
            var client = new CapitalStreamingClient(socket);
            SyntheticStreamingSubscription.SubscribeAsync(
                client,
                new CapitalSession { Cst = "fixture-cst", SecurityToken = "fixture-token" },
                basket,
                "Daily").GetAwaiter().GetResult();
            AssertEqual(1, socket.SentMessages.Count, $"automatic {label} streaming remains quote-only");
            AssertStreamingSubscription(
                socket.SentMessages.Single(),
                "marketData.subscribe",
                basket.Components.Select(component => component.Instrument.Epic).ToArray(),
                null);
        }
    }

    private static void ManualCryptoTimeframesReloadLongestSharedHistory()
    {
        var eth = CreateCrypto("CS.D.ETHUSD.CFD.IP", "ETHUSD", "Ethereum", "USD", 1999m, 2001m);
        var btc = CreateCrypto("CS.D.BTCUSD.CFD.IP", "BTCUSD", "Bitcoin", "USD", 29990m, 30010m);
        var formula = ManualSyntheticFormula.Parse(ManualSyntheticFormula.CryptoPreset);
        var initialDay = DateTimeOffset.Parse("2026-07-01T00:00:00Z");
        var initialRows = new[] { FlatCandle(initialDay, 100m), FlatCandle(initialDay.AddDays(1), 101m) };
        var active = new ActiveSyntheticBasketState();
        active.Activate(ManualSyntheticBasketFactory.Create(
            "SYN-CRYPTO-ETHBTC-01", "Crypto / USD / All", formula, [eth, btc],
            new Dictionary<string, IReadOnlyList<OhlcPoint>> { [eth.Epic] = initialRows, [btc.Epic] = initialRows },
            "Daily", 2), SyntheticStrategyKind.ManualFormula);

        foreach (var fixture in new[]
        {
            (Timeframe: "Weekly", Start: DateTimeOffset.Parse("2026-05-04T00:00:00Z"), Step: TimeSpan.FromDays(7), Shared: 5),
            (Timeframe: "Daily", Start: DateTimeOffset.Parse("2026-07-01T00:00:00Z"), Step: TimeSpan.FromDays(1), Shared: 7),
            (Timeframe: "4H", Start: DateTimeOffset.Parse("2026-07-28T00:00:00Z"), Step: TimeSpan.FromHours(4), Shared: 9),
        })
        {
            var ethRows = Enumerable.Range(0, fixture.Shared + 2)
                .Select(index => FlatCandle(fixture.Start.AddTicks(fixture.Step.Ticks * index), 100m + index)).ToList();
            var btcRows = Enumerable.Range(0, fixture.Shared)
                .Select(index => FlatCandle(fixture.Start.AddTicks(fixture.Step.Ticks * index), 1000m + index)).ToList();
            var history = new HistoryLoadResult(
                new Dictionary<string, IReadOnlyList<OhlcPoint>>(StringComparer.OrdinalIgnoreCase)
                {
                    [eth.Epic] = ethRows,
                    [btc.Epic] = btcRows,
                },
                fixture.Start,
                fixture.Start.AddTicks(fixture.Step.Ticks * (fixture.Shared - 1)),
                fixture.Shared);

            var rebuilt = active.RebuildHistory(history, fixture.Timeframe, periodsPerYear: 365, minimumCandles: 2)
                ?? throw new Exception($"manual {fixture.Timeframe} history should rebuild");
            AssertEqual("SYN-CRYPTO-ETHBTC-01", rebuilt.Symbol, "timeframe reload preserves manual basket symbol");
            AssertEqual(SyntheticStrategyKind.ManualFormula, rebuilt.Strategy, "timeframe reload preserves manual strategy");
            AssertSequence(rebuilt.Components.Select(component => component.Instrument.Epic), eth.Epic, btc.Epic);
            AssertSequence(rebuilt.Components.Select(component => component.FormulaMultiplier), 9m, 0.2m);
            AssertEqual(fixture.Shared, rebuilt.Candles.Count, $"{fixture.Timeframe} reload uses the longest strict shared history");
            active.Activate(rebuilt, SyntheticStrategyKind.ManualFormula);
        }
    }

    private static void CapitalStreamingParsesOhlcEventsForManualCryptoBars()
    {
        const string json =
            """
            {
              "status": "OK",
              "destination": "ohlc.event",
              "payload": {
                "resolution": "HOUR_4",
                "epic": "CS.D.ETHUSD.CFD.IP",
                "type": "classic",
                "priceType": "bid",
                "t": 1785542400000,
                "h": 3210.5,
                "l": 3160.25,
                "o": 3180.75,
                "c": 3201.5
              }
            }
            """;

        var update = CapitalStreamingClient.ParseOhlcUpdate(json)
            ?? throw new Exception("Capital OHLC event must parse");
        AssertEqual("CS.D.ETHUSD.CFD.IP", update.Epic, "streamed OHLC epic");
        AssertEqual("HOUR_4", update.Resolution, "streamed OHLC resolution");
        AssertEqual(DateTimeOffset.FromUnixTimeMilliseconds(1785542400000), update.Time, "streamed OHLC source time");
        AssertEqual(new OhlcPoint(update.Time, 3180.75m, 3210.5m, 3160.25m, 3201.5m), update.Candle,
            "streamed OHLC values");
        AssertEqual("HOUR_4", SyntheticRealtimeBarBuilder.StreamingResolution("4H"), "4H uses Capital native ongoing bars");
        AssertEqual("DAY", SyntheticRealtimeBarBuilder.StreamingResolution("Daily"), "daily ongoing bar resolution");
        AssertEqual("WEEK", SyntheticRealtimeBarBuilder.StreamingResolution("Weekly"), "weekly ongoing bar resolution");
        AssertEqual(null, CapitalStreamingClient.ParseOhlcUpdate(json.Replace("3210.5", "0", StringComparison.Ordinal)),
            "nonpositive OHLC events are ignored");
    }

    private static void ManualCryptoBuildAndRestoreSubscribeBothEpicsAndUpdateResolution()
    {
        var day = DateTimeOffset.Parse("2026-07-01T00:00:00Z");
        var eth = CreateCrypto("CS.D.ETHUSD.CFD.IP", "ETHUSD", "Ethereum", "USD", 1999m, 2001m);
        var btc = CreateCrypto("CS.D.BTCUSD.CFD.IP", "BTCUSD", "Bitcoin", "USD", 29990m, 30010m);
        var rows = new[] { FlatCandle(day, 100m), FlatCandle(day.AddDays(1), 101m) };
        var history = new HistoryLoadResult(
            new Dictionary<string, IReadOnlyList<OhlcPoint>>(StringComparer.OrdinalIgnoreCase)
            {
                [eth.Epic] = rows,
                [btc.Epic] = rows,
            }, day, day.AddDays(1), 2);
        var basket = ManualSyntheticBasketFactory.Create(
            "SYN-CRYPTO-ETHBTC-01", "Crypto / USD / All", ManualSyntheticFormula.Parse(ManualSyntheticFormula.CryptoPreset),
            [eth, btc], history.CandlesByEpic, "Daily", 2);
        var saved = SavedSyntheticBasket.FromBasket("ETH BTC", SyntheticStrategyKind.ManualFormula, basket);
        var restored = SavedSyntheticBasketRestorer.Restore(saved, [eth, btc], history, "Daily", 365, 2)
            ?? throw new Exception("saved manual crypto basket should restore");

        AssertSequence(SyntheticTerminalWorkspace.StreamingEpics(basket), eth.Epic, btc.Epic);
        AssertSequence(SyntheticTerminalWorkspace.StreamingEpics(restored.Basket), eth.Epic, btc.Epic);

        var socket = new FakeCapitalStreamingSocket(WebSocketState.Open);
        var client = new CapitalStreamingClient(socket);
        var session = new CapitalSession { Cst = "fixture-cst", SecurityToken = "fixture-token" };
        SyntheticStreamingSubscription.SubscribeAsync(client, session, basket, "Daily").GetAwaiter().GetResult();
        SyntheticStreamingSubscription.SubscribeAsync(client, session, restored.Basket, "4H").GetAwaiter().GetResult();

        AssertEqual(4, socket.SentMessages.Count, "build and saved restore must each send quote and OHLC subscriptions");
        AssertStreamingSubscription(socket.SentMessages[0], "marketData.subscribe", [eth.Epic, btc.Epic], null);
        AssertStreamingSubscription(socket.SentMessages[1], "OHLCMarketData.subscribe", [eth.Epic, btc.Epic], "DAY");
        AssertStreamingSubscription(socket.SentMessages[2], "marketData.subscribe", [eth.Epic, btc.Epic], null);
        AssertStreamingSubscription(socket.SentMessages[3], "OHLCMarketData.subscribe", [eth.Epic, btc.Epic], "HOUR_4");
    }

    private static void ManualFormulaResolvesExactIdentifiersAndRejectsInvalidTerms()
    {
        var day = DateTimeOffset.Parse("2026-07-01T00:00:00Z");
        var exactEpic = CreateCrypto("ETHUSD", "ETH-CAP", "Ethereum", "USD", 1m, 2m);
        var nameCollision = CreateCrypto("CS.D.ETHOTHER.CFD.IP", "ETH-OTHER", "ETHUSD", "USD", 1m, 2m);
        var btc = CreateCrypto("CS.D.BTCUSD.CFD.IP", "BTCUSD", "Bitcoin", "USD", 1m, 2m);
        var rows = new[] { FlatCandle(day, 100m), FlatCandle(day.AddDays(1), 101m) };
        var candles = new Dictionary<string, IReadOnlyList<OhlcPoint>>(StringComparer.OrdinalIgnoreCase)
        {
            [exactEpic.Epic] = rows,
            [nameCollision.Epic] = rows,
            [btc.Epic] = rows,
        };
        var precedence = ManualSyntheticBasketFactory.Create(
            "SYN-PRECEDENCE",
            "Crypto / USD / All",
            ManualSyntheticFormula.Parse("1 ETHUSD + 1 BTCUSD"),
            [exactEpic, nameCollision, btc],
            candles);
        AssertEqual(exactEpic.Epic, precedence.Components[0].Instrument.Epic, "exact epic or symbol must beat a name match");

        var slashEth = CreateCrypto("CS.D.ETHUSD.CFD.IP", "ETH/USD", "Ethereum / US Dollar", "USD", 1m, 2m);
        var slashBtc = CreateCrypto("CS.D.BTCUSD.CFD.IP", "BTC/USD", "Bitcoin / US Dollar", "USD", 1m, 2m);
        var slashCandles = new Dictionary<string, IReadOnlyList<OhlcPoint>>(StringComparer.OrdinalIgnoreCase)
        {
            [slashEth.Epic] = rows,
            [slashBtc.Epic] = rows,
        };
        var normalizedSymbols = ManualSyntheticBasketFactory.Create(
            "SYN-NORMALIZED",
            "Crypto / USD / All",
            ManualSyntheticFormula.Parse("9 ETHUSD + 0.2 BTCUSD"),
            [slashEth, slashBtc],
            slashCandles);
        AssertEqual(slashEth.Epic, normalizedSymbols.Components[0].Instrument.Epic, "ETHUSD preset must resolve slash-delimited Capital symbols");
        AssertEqual(slashBtc.Epic, normalizedSymbols.Components[1].Instrument.Epic, "BTCUSD preset must resolve slash-delimited Capital symbols");

        AssertThrows<FormatException>(
            () => ManualSyntheticFormula.Parse("0 ETHUSD + 0.2 BTCUSD"),
            "greater than zero",
            "zero manual multiplier");
        AssertThrows<FormatException>(
            () => ManualSyntheticFormula.Parse("-9 ETHUSD + 0.2 BTCUSD"),
            "greater than zero",
            "negative manual multiplier");
        AssertThrows<FormatException>(
            () => ManualSyntheticFormula.Parse("9 ETHUSD"),
            "two to four",
            "one-leg manual formula");
        AssertThrows<FormatException>(
            () => ManualSyntheticFormula.Parse("ETHUSD + 0.2 BTCUSD"),
            "multiplier",
            "missing manual multiplier");
        AssertThrows<FormatException>(
            () => ManualSyntheticFormula.Parse("9 ETHUSD + 0,2 BTCUSD"),
            "invalid",
            "comma decimal separator");
        AssertThrows<FormatException>(
            () => ManualSyntheticFormula.Parse("1,000 ETHUSD + 0.2 BTCUSD"),
            "invalid",
            "thousands group separator");
        AssertThrows<FormatException>(
            () => ManualSyntheticFormula.Parse("9e0 ETHUSD + 0.2 BTCUSD"),
            "invalid",
            "exponent multiplier syntax");
        AssertThrows<FormatException>(
            () => ManualSyntheticFormula.Parse("+9 ETHUSD + 0.2 BTCUSD"),
            "invalid",
            "explicit positive sign syntax");
        AssertThrows<FormatException>(
            () => ManualSyntheticFormula.Parse("1 A + 1 B + 1 C + 1 D + 1 E"),
            "two to four",
            "five-term manual formula");

        AssertThrows<InvalidOperationException>(
            () => ManualSyntheticBasketFactory.Create(
                "SYN-UNKNOWN",
                "Crypto / USD / All",
                ManualSyntheticFormula.Parse("9 UNKNOWN + 0.2 BTCUSD"),
                [exactEpic, nameCollision, btc],
                candles),
            "not found",
            "unknown manual instrument");
        AssertThrows<InvalidOperationException>(
            () => ManualSyntheticBasketFactory.Create(
                "SYN-DUPLICATE",
                "Crypto / USD / All",
                ManualSyntheticFormula.Parse("9 ETHUSD + 0.2 ETH-CAP"),
                [exactEpic, nameCollision, btc],
                candles),
            "duplicate",
            "duplicate resolved epic");

        var ambiguousOne = CreateCrypto("CRYPTO-ONE", "DUP", "Duplicate one", "USD", 1m, 2m);
        var ambiguousTwo = CreateCrypto("CRYPTO-TWO", "DUP", "Duplicate two", "USD", 1m, 2m);
        AssertThrows<InvalidOperationException>(
            () => ManualSyntheticBasketFactory.Create(
                "SYN-AMBIGUOUS",
                "Crypto / USD / All",
                ManualSyntheticFormula.Parse("1 DUP + 1 BTCUSD"),
                [ambiguousOne, ambiguousTwo, btc],
                candles),
            "ambiguous",
            "ambiguous manual instrument");

        var btcEur = CreateCrypto("CS.D.BTCEUR.CFD.IP", "BTCEUR", "Bitcoin Euro", "EUR", 1m, 2m);
        AssertThrows<InvalidOperationException>(
            () => ManualSyntheticBasketFactory.Create(
                "SYN-MIXED",
                "Crypto / USD / All",
                ManualSyntheticFormula.Parse("1 ETHUSD + 1 BTCEUR"),
                [exactEpic, btcEur],
                candles),
            "currency",
            "mixed-currency manual formula");

        var nonCryptoEth = new MarketInstrument
        {
            Epic = "ETHUSD",
            Symbol = "ETHUSD",
            Name = "Ethereum tracker",
            Type = "SHARES",
            Region = "US",
            Sector = "Technology",
            Currency = "USD",
        };
        AssertThrows<InvalidOperationException>(
            () => ManualSyntheticBasketFactory.Resolve(
                "Crypto / USD / All",
                ManualSyntheticFormula.Parse("9 ETHUSD + 0.2 BTCUSD"),
                [nonCryptoEth, btc]),
            "not a Crypto instrument",
            "non-crypto manual component");
    }

    private static void ManualFormulaSaveRestorePreservesTwoLegStrategyAndExactMultipliers()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"capetf-manual-saved-{Guid.NewGuid():N}");
        try
        {
            var day = DateTimeOffset.Parse("2026-07-01T00:00:00Z");
            var eth = CreateCrypto("CS.D.ETHUSD.CFD.IP", "ETHUSD", "Ethereum", "USD", 1999m, 2001m);
            var btc = CreateCrypto("CS.D.BTCUSD.CFD.IP", "BTCUSD", "Bitcoin", "USD", 29990m, 30010m);
            var candles = new Dictionary<string, IReadOnlyList<OhlcPoint>>(StringComparer.OrdinalIgnoreCase)
            {
                [eth.Epic] =
                [
                    new OhlcPoint(day, 100m, 110m, 90m, 105m),
                    new OhlcPoint(day.AddDays(1), 200m, 220m, 180m, 210m),
                ],
                [btc.Epic] =
                [
                    new OhlcPoint(day, 1000m, 1050m, 950m, 1020m),
                    new OhlcPoint(day.AddDays(1), 1200m, 1260m, 1140m, 1230m),
                ],
            };
            var basket = ManualSyntheticBasketFactory.Create(
                "SYN-CRYPTO-ETHBTC-01",
                "Crypto / USD / All",
                ManualSyntheticFormula.Parse("9 ETHUSD + 0.2 BTCUSD"),
                [eth, btc],
                candles);
            var store = new SavedSyntheticBasketStore(folder);
            store.Save(SavedSyntheticBasket.FromBasket("ETH BTC exact", SyntheticStrategyKind.ManualFormula, basket));

            var saved = store.LoadAll().Single();
            AssertEqual(SyntheticStrategyKind.ManualFormula, saved.Strategy, "manual strategy must persist");
            AssertEqual(9m, saved.Components[0].FormulaMultiplier, "saved ETH multiplier must remain exact");
            AssertEqual(0.2m, saved.Components[1].FormulaMultiplier, "saved BTC multiplier must remain exact");

            var history = new HistoryLoadResult(candles, day, day.AddDays(1), 2);
            var restored = SavedSyntheticBasketRestorer.Restore(saved, [eth, btc], history, "Daily", 252, 2)
                ?? throw new Exception("saved two-leg manual formula must restore");
            AssertEqual(SyntheticStrategyKind.ManualFormula, restored.Strategy, "restored manual strategy");
            AssertEqual(2, restored.Basket.Components.Count, "restored manual leg count");
            AssertEqual(9m, restored.Basket.Components[0].FormulaMultiplier, "restored ETH multiplier must remain exact");
            AssertEqual(0.2m, restored.Basket.Components[1].FormulaMultiplier, "restored BTC multiplier must remain exact");
            AssertNear(2040m, restored.Basket.Candles[1].Open, "restored manual formula must retain direct-scale candle opens");
        }
        finally
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
        }
    }

    private static void ManualFormulaResolutionIsBlockLocalAndTiered()
    {
        const string usdBlock = "Crypto / USD / All";
        var btc = CreateCrypto("BTC-USD-EPIC", "BTCUSD", "Bitcoin", "USD", 1m, 2m);
        var inBlockExactName = CreateCrypto("ETH-NAME-EPIC", "ETH-CAP", "ETHUSD", "USD", 1m, 2m);
        var outOfBlockExactSymbol = CreateCrypto("ETH-EUR-EPIC", "ETHUSD", "Ethereum Euro", "EUR", 1m, 2m);

        var blockFirst = ManualSyntheticBasketFactory.Resolve(
            usdBlock,
            ManualSyntheticFormula.Parse("9 ETHUSD + 0.2 BTCUSD"),
            [outOfBlockExactSymbol, inBlockExactName, btc]);
        AssertEqual(inBlockExactName.Epic, blockFirst[0].Epic, "out-of-block exact IDs must not suppress an in-block exact name");

        var normalizedSymbol = CreateCrypto("ETH-NORMALIZED-EPIC", "ETH/USD", "Ethereum normalized", "USD", 1m, 2m);
        var nameBeforeNormalized = ManualSyntheticBasketFactory.Resolve(
            usdBlock,
            ManualSyntheticFormula.Parse("9 ETHUSD + 0.2 BTCUSD"),
            [normalizedSymbol, inBlockExactName, btc]);
        AssertEqual(inBlockExactName.Epic, nameBeforeNormalized[0].Epic, "exact in-block name must precede normalized epic or symbol fallback");

        var duplicateName = CreateCrypto("ETH-NAME-EPIC-2", "ETH-SECOND", "ETHUSD", "USD", 1m, 2m);
        AssertThrows<InvalidOperationException>(
            () => ManualSyntheticBasketFactory.Resolve(
                usdBlock,
                ManualSyntheticFormula.Parse("9 ETHUSD + 0.2 BTCUSD"),
                [normalizedSymbol, inBlockExactName, duplicateName, btc]),
            "ambiguous",
            "exact-name tier ambiguity");

        var normalizedSymbolTwo = CreateCrypto("ETH-NORMALIZED-EPIC-2", "ETH-USD", "Ethereum normalized two", "USD", 1m, 2m);
        AssertThrows<InvalidOperationException>(
            () => ManualSyntheticBasketFactory.Resolve(
                usdBlock,
                ManualSyntheticFormula.Parse("9 ETHUSD + 0.2 BTCUSD"),
                [normalizedSymbol, normalizedSymbolTwo, btc]),
            "ambiguous",
            "normalized-ID tier ambiguity");
    }

    private static void ManualFormulaResolvesCapitalEpicPairSegmentsWithinSelectedBlock()
    {
        const string usdBlock = "Crypto / USD / All";
        var ethUsd = CreateCrypto("CS.D.ETHUSD.CFD.IP", "", "Ethereum / US Dollar", "USD", 1m, 2m);
        var btcUsd = CreateCrypto("CS.D.BTCUSD.CFD.IP", "", "Bitcoin / US Dollar", "USD", 1m, 2m);
        var unresolvedEthAlias = CreateCrypto("ETHUSD", "", "Ethereum", "Currency", 1m, 2m);

        var resolved = ManualSyntheticBasketFactory.Resolve(
            usdBlock,
            ManualSyntheticFormula.Parse("9 ETHUSD + 0.2 BTCUSD"),
            [unresolvedEthAlias, ethUsd, btcUsd]);

        AssertEqual(ethUsd.Epic, resolved[0].Epic, "ETHUSD must resolve from the Capital epic pair segment in the selected block");
        AssertEqual(btcUsd.Epic, resolved[1].Epic, "BTCUSD must resolve from the Capital epic pair segment in the selected block");
    }

    private static void SignedSyntheticQuotesUseExecutableBidAskSides()
    {
        var basket = new SyntheticBasket();
        basket.Components.Add(new SyntheticComponent(
            new MarketInstrument { Epic = "LONG", Bid = 99m, Offer = 101m }, 50m, 0m, 0m)
        {
            FormulaMultiplier = 2m,
        });
        basket.Components.Add(new SyntheticComponent(
            new MarketInstrument { Epic = "SHORT", Bid = 49m, Offer = 51m }, 50m, 0m, 0m)
        {
            FormulaMultiplier = -3m,
        });

        SyntheticQuoteCalculator.Refresh(basket);

        AssertNear(45m, basket.BidPrice ?? 0m, "signed bid must value short legs at their offer");
        AssertNear(55m, basket.AskPrice ?? 0m, "signed ask must value short legs at their bid");
    }

    private static void ManualBasketStrategyIdentitySurvivesDropdownChanges()
    {
        var day = DateTimeOffset.Parse("2026-07-01T00:00:00Z");
        var eth = CreateCrypto("CS.D.ETHUSD.CFD.IP", "ETHUSD", "Ethereum", "USD", 1999m, 2001m);
        var btc = CreateCrypto("CS.D.BTCUSD.CFD.IP", "BTCUSD", "Bitcoin", "USD", 29990m, 30010m);
        var candles = new Dictionary<string, IReadOnlyList<OhlcPoint>>(StringComparer.OrdinalIgnoreCase)
        {
            [eth.Epic] = [FlatCandle(day, 2000m), FlatCandle(day.AddDays(1), 2100m)],
            [btc.Epic] = [FlatCandle(day, 30000m), FlatCandle(day.AddDays(1), 31000m)],
        };
        var basket = ManualSyntheticBasketFactory.Create(
            "SYN-CRYPTO-ETHBTC-01",
            "Crypto / USD / All",
            ManualSyntheticFormula.Parse("9 ETHUSD + 0.2 BTCUSD"),
            [eth, btc],
            candles);
        var state = new ActiveSyntheticBasketState();
        state.Activate(basket, SyntheticStrategyKind.ManualFormula);

        var dropdownStrategyAfterBuild = SyntheticStrategyKind.MeanReversion;
        AssertEqual(SyntheticStrategyKind.MeanReversion, dropdownStrategyAfterBuild, "test must simulate a changed strategy dropdown");
        AssertEqual(SyntheticStrategyKind.ManualFormula, state.Strategy, "active basket strategy must not follow the dropdown");

        var suggestedName = state.SuggestedSavedBasketName();
        var saved = state.CreateSavedBasket(suggestedName);
        AssertEqual(SyntheticStrategyKind.ManualFormula, saved.Strategy, "save must use active basket strategy identity");
        AssertTrue(suggestedName.EndsWith("-MANUALFORMULA", StringComparison.Ordinal), "save name must use active basket strategy identity");

        var history = new HistoryLoadResult(candles, day, day.AddDays(1), 2);
        var rebuilt = state.RebuildHistory(history, "Daily", periodsPerYear: 252, minimumCandles: 2)
            ?? throw new Exception("active manual basket must rebuild from shared history");
        AssertEqual(SyntheticStrategyKind.ManualFormula, rebuilt.Strategy, "history reload must preserve active basket strategy");
        AssertEqual(9m, rebuilt.Components[0].FormulaMultiplier, "history reload must preserve ETH multiplier");
        AssertEqual(0.2m, rebuilt.Components[1].FormulaMultiplier, "history reload must preserve BTC multiplier");
    }

    private static void ManualFormulaEditorIsCompactConditionalAndBypassesAutomaticSelection()
    {
        var xaml = File.ReadAllText(SourcePath("desktop", "CAPETF.Desktop", "CapComTerminalWindow.xaml"));
        foreach (var required in new[]
        {
            "SelectionChanged=\"StrategyBox_SelectionChanged\"",
            "x:Name=\"ManualFormulaBox\"",
            "Text=\"9 ETHUSD + 0.2 BTCUSD\"",
            "Visibility=\"Collapsed\"",
        })
        {
            if (!xaml.Contains(required, StringComparison.Ordinal))
            {
                throw new Exception($"compact manual formula editor contract missing {required}");
            }
        }

        var source = File.ReadAllText(SourcePath("desktop", "CAPETF.Desktop", "CapComTerminalWindow.xaml.cs"));
        var manualBranch = source.IndexOf("strategy == SyntheticStrategyKind.ManualFormula", StringComparison.Ordinal);
        var automaticSelection = source.IndexOf("SyntheticTerminalSelector.HistoryLoadCandidates", StringComparison.Ordinal);
        if (manualBranch < 0 || automaticSelection < 0 || manualBranch > automaticSelection)
        {
            throw new Exception("manual formula construction must branch before automatic candidate strategy selection");
        }
        foreach (var required in new[]
        {
            "ManualSyntheticFormula.CryptoPreset",
            "ManualSyntheticBasketFactory.Create",
            "ManualFormulaBox.Visibility",
        })
        {
            if (!source.Contains(required, StringComparison.Ordinal))
            {
                throw new Exception($"manual formula terminal workflow missing {required}");
            }
        }

    }

    private static MarketInstrument CreateCrypto(
        string epic,
        string symbol,
        string name,
        string currency,
        decimal bid,
        decimal offer) => new()
    {
        Epic = epic,
        Symbol = symbol,
        Name = name,
        Type = "CRYPTOCURRENCIES",
        Region = "Crypto",
        Sector = "All",
        Currency = currency,
        Bid = bid,
        Offer = offer,
        Price = (bid + offer) / 2m,
    };

    private static TException AssertThrows<TException>(
        Action action,
        string expectedMessage,
        string context)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException exception)
        {
            if (!exception.Message.Contains(expectedMessage, StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception($"{context} must mention '{expectedMessage}', got '{exception.Message}'");
            }
            return exception;
        }
        catch (Exception exception)
        {
            throw new Exception($"{context} must throw {typeof(TException).Name}, got {exception.GetType().Name}: {exception.Message}");
        }

        throw new Exception($"{context} must throw {typeof(TException).Name}");
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

    private static void AssertSequence<T>(IEnumerable<T> actual, params T[] expected)
    {
        var actualRows = actual.ToArray();
        if (!actualRows.SequenceEqual(expected))
        {
            throw new Exception($"Sequence mismatch. Expected {string.Join(" | ", expected)}, got {string.Join(" | ", actualRows)}");
        }
    }

    private static void AssertStreamingSubscription(
        string json,
        string expectedDestination,
        IReadOnlyList<string> expectedEpics,
        string? expectedResolution)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        AssertEqual(expectedDestination, root.GetProperty("destination").GetString(), "stream subscription destination");
        AssertSequence(
            root.GetProperty("payload").GetProperty("epics").EnumerateArray().Select(value => value.GetString() ?? ""),
            expectedEpics.ToArray());
        if (expectedResolution is null)
        {
            AssertFalse(root.GetProperty("payload").TryGetProperty("resolutions", out _),
                "quote subscription must not contain an OHLC resolution");
        }
        else
        {
            AssertSequence(
                root.GetProperty("payload").GetProperty("resolutions").EnumerateArray().Select(value => value.GetString() ?? ""),
                expectedResolution);
        }
    }

    private static void AssertContains(string value, string expected, string message)
    {
        if (!value.Contains(expected, StringComparison.Ordinal))
        {
            throw new Exception($"{message}. Expected source to contain '{expected}'.");
        }
    }

    private static string SliceSource(string source, string start, string end)
    {
        var startIndex = source.IndexOf(start, StringComparison.Ordinal);
        var endIndex = source.IndexOf(end, startIndex + Math.Max(start.Length, 1), StringComparison.Ordinal);
        if (startIndex < 0 || endIndex <= startIndex)
        {
            throw new Exception($"Source boundaries missing: {start} -> {end}");
        }
        return source[startIndex..endIndex];
    }

    private static void SavedBasketRestorePreservesExactFormulaAndRejectsMissingEpics()
    {
        var day = DateTimeOffset.Parse("2026-07-01T00:00:00Z");
        var savedComponents = new[]
        {
            new SavedSyntheticComponent("RESTORE-D", "Saved D", "USD", 40m, 0.40m, 400m),
            new SavedSyntheticComponent("RESTORE-B", "Saved B", "USD", 20m, 0.20m, 200m),
            new SavedSyntheticComponent("RESTORE-A", "Saved A", "USD", 10m, 0.10m, 100m),
            new SavedSyntheticComponent("RESTORE-C", "Saved C", "USD", 30m, 0.30m, 300m),
        };
        var saved = new SavedSyntheticBasket(
            "RESTORE-FOUR",
            "Restore four legs",
            "SYN-RESTORE-EXACT",
            "US / USD / Technology",
            SyntheticStrategyKind.MeanReversion,
            day,
            day,
            savedComponents);
        var current = savedComponents.Select((component, index) => new MarketInstrument
        {
            Epic = component.Epic,
            Name = $"Current {component.Epic}",
            Currency = "USD",
            LotSize = index + 1,
        }).ToList();
        var history = current.ToDictionary(
            instrument => instrument.Epic,
            instrument => (IReadOnlyList<OhlcPoint>)new[]
            {
                FlatCandle(day, 100m + current.IndexOf(instrument) * 50m),
                FlatCandle(day.AddDays(1), 105m + current.IndexOf(instrument) * 50m),
                FlatCandle(day.AddDays(2), 111m + current.IndexOf(instrument) * 51m),
            },
            StringComparer.OrdinalIgnoreCase);
        var load = new HistoryLoadResult(history, day, day.AddDays(2), 3);

        var restored = SavedSyntheticBasketRestorer.Restore(saved, current, load, "Daily", 252, 2)
            ?? throw new Exception("a complete saved four-leg basket must restore");

        AssertEqual(saved.Symbol, restored.Basket.Symbol, "saved symbol must survive restore");
        AssertEqual(saved.Block, restored.Basket.Block, "saved block must survive restore");
        AssertEqual(saved.Strategy, restored.Strategy, "saved strategy must survive restore");
        AssertEqual(4, restored.Basket.Components.Count, "all saved legs must survive restore");
        AssertEqual(
            string.Join("|", savedComponents.Select(component => component.Epic)),
            string.Join("|", restored.Basket.Components.Select(component => component.Instrument.Epic)),
            "saved component order and exact set must survive restore");
        for (var index = 0; index < savedComponents.Length; index++)
        {
            var expected = savedComponents[index];
            var actual = restored.Basket.Components[index];
            AssertNear(expected.Weight, actual.Weight, $"saved weight {index} must survive restore");
            AssertNear(expected.FormulaMultiplier, actual.FormulaMultiplier, $"saved multiplier {index} must survive restore");
            AssertNear(expected.ReferencePrice ?? 0m, actual.FormulaReferencePrice ?? 0m, $"saved reference price {index} must survive restore");
            AssertEqual($"Current {expected.Epic}", actual.Instrument.Name, $"current instrument metadata {index} must be used");
        }

        var missingOne = SavedSyntheticBasketRestorer.Restore(saved, current.Take(3).ToList(), load, "Daily", 252, 2);
        if (missingOne is not null)
        {
            throw new Exception("a saved four-leg basket missing one current epic must fail instead of degrading to three legs");
        }
    }

    private static void SavedBasketLoadUsesFaithfulFormulaRestorer()
    {
        var source = File.ReadAllText(SourcePath("desktop", "CAPETF.Desktop", "CapComTerminalWindow.xaml.cs"));
        var start = source.IndexOf("private async Task LoadSavedBasketAsync", StringComparison.Ordinal);
        var end = source.IndexOf("private static IReadOnlyList<MarketInstrument> SelectSyntheticCandidates", start, StringComparison.Ordinal);
        if (start < 0 || end <= start) throw new Exception("saved basket load method must remain available");
        var loadBlock = source[start..end];

        foreach (var required in new[]
        {
            "SavedSyntheticBasketRestorer.Restore",
            "_basket = restored.Basket",
            "StrategyBox.SelectedValue = restored.Strategy",
        })
        {
            if (!loadBlock.Contains(required, StringComparison.Ordinal))
            {
                throw new Exception($"saved basket load must restore persisted formula identity: missing {required}");
            }
        }

        if (loadBlock.Contains("SyntheticHistoryService.BuildSelected", StringComparison.Ordinal) ||
            loadBlock.Contains(".OfType<MarketInstrument>()", StringComparison.Ordinal))
        {
            throw new Exception("saved basket load must not silently drop missing epics or rebuild a generic formula");
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

    private static string ExtractCssBlock(string css, string selector)
    {
        var selectorIndex = css.IndexOf(selector, StringComparison.Ordinal);
        if (selectorIndex < 0) throw new Exception($"CSS selector missing: {selector}");
        var openBrace = css.IndexOf('{', selectorIndex);
        if (openBrace < 0) throw new Exception($"CSS block missing opening brace: {selector}");
        var depth = 0;
        for (var index = openBrace; index < css.Length; index++)
        {
            if (css[index] == '{') depth++;
            if (css[index] != '}') continue;
            depth--;
            if (depth == 0) return css[(openBrace + 1)..index];
        }
        throw new Exception($"CSS block missing closing brace: {selector}");
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

    private sealed class CryptoMarketsHandler : HttpMessageHandler
    {
        public Uri? MarketRequestUri { get; private set; }
        public HttpMethod? MarketRequestMethod { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri?.AbsolutePath.EndsWith("/api/v1/session", StringComparison.OrdinalIgnoreCase) == true)
            {
                var login = JsonResponse("{}");
                login.Headers.Add("CST", "cst-token");
                login.Headers.Add("X-SECURITY-TOKEN", "security-token");
                return Task.FromResult(login);
            }

            MarketRequestUri = request.RequestUri;
            MarketRequestMethod = request.Method;
            return Task.FromResult(JsonResponse(
                """
                {
                  "markets": [
                    {
                      "epic": "CRYPTO.BTCUSD.CFD.IP",
                      "instrumentName": "Bitcoin / USD",
                      "symbol": "BTC/USD",
                      "instrumentType": "CRYPTOCURRENCIES",
                      "currency": "USD",
                      "marketStatus": "TRADEABLE",
                      "bid": 104000.5,
                      "offer": 104001.5
                    },
                    {
                      "epic": "CRYPTO.ETHUSD.CFD.IP",
                      "instrumentName": "Ethereum / USD",
                      "symbol": "ETH/USD",
                      "instrumentType": "CRYPTOCURRENCIES",
                      "currency": "USD",
                      "marketStatus": "CLOSED",
                      "bid": 3200.25,
                      "offer": 3201.25
                    }
                  ]
                }
                """));
        }

        private static HttpResponseMessage JsonResponse(string body) => new(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
    }

    private sealed class FakeSyntheticMarginDataSource : ISyntheticMarginDataSource
    {
        public CapitalAccountSnapshot Account { get; set; } =
            new("active", "USD", 0m, DateTimeOffset.UnixEpoch);
        public Dictionary<string, MarketInstrument> MarketDetails { get; } =
            new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, IReadOnlyList<MarketInstrument>> SearchResults { get; } =
            new(StringComparer.OrdinalIgnoreCase);
        public List<string> SearchQueries { get; } = [];
        public int AccountRequestCount { get; private set; }
        public Func<CancellationToken, Task<CapitalAccountSnapshot>>? AccountHandler { get; set; }
        public Func<string, CancellationToken, Task<MarketInstrument?>>? MarketDetailsHandler { get; init; }
        private Dictionary<string, int> MarketDetailRequestCounts { get; } =
            new(StringComparer.OrdinalIgnoreCase);

        public Task<CapitalAccountSnapshot> GetActiveAccountAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AccountRequestCount++;
            if (AccountHandler is not null) return AccountHandler(cancellationToken);
            return Task.FromResult(Account);
        }

        public Task<MarketInstrument?> GetMarketDetailsAsync(string epic, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lock (MarketDetailRequestCounts)
            {
                MarketDetailRequestCounts[epic] = MarketDetailRequestCount(epic) + 1;
            }
            if (MarketDetailsHandler is not null) return MarketDetailsHandler(epic, cancellationToken);
            MarketDetails.TryGetValue(epic, out var details);
            return Task.FromResult(details);
        }

        public int MarketDetailRequestCount(string epic)
        {
            lock (MarketDetailRequestCounts)
            {
                return MarketDetailRequestCounts.TryGetValue(epic, out var count) ? count : 0;
            }
        }

        public Task<IReadOnlyList<MarketInstrument>> SearchMarketsAsync(string query, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SearchQueries.Add(query);
            return Task.FromResult(SearchResults.TryGetValue(query, out var markets) ? markets : []);
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

    private sealed class HistoryFailureHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri?.AbsolutePath.EndsWith("/api/v1/session", StringComparison.OrdinalIgnoreCase) == true)
            {
                var login = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{}", Encoding.UTF8, "application/json"),
                };
                login.Headers.Add("CST", "cst-token");
                login.Headers.Add("X-SECURITY-TOKEN", "security-token");
                return Task.FromResult(login);
            }

            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
        }
    }

    private sealed class SharedHistoryAnchorHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, int> _requestsByEpic = new(StringComparer.OrdinalIgnoreCase);
        public List<string> InitialToValues { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri?.AbsolutePath.EndsWith("/api/v1/session", StringComparison.OrdinalIgnoreCase) == true)
            {
                var login = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{}", Encoding.UTF8, "application/json"),
                };
                login.Headers.Add("CST", "cst-token");
                login.Headers.Add("X-SECURITY-TOKEN", "security-token");
                return login;
            }

            var epic = request.RequestUri!.AbsolutePath.Split('/').Last();
            var requestNumber = _requestsByEpic.TryGetValue(epic, out var count) ? count + 1 : 1;
            _requestsByEpic[epic] = requestNumber;
            if (requestNumber > 1)
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("{\"errorCode\":\"error.invalid.from\"}", Encoding.UTF8, "application/json"),
                };
            }

            var query = System.Web.HttpUtility.ParseQueryString(request.RequestUri.Query);
            InitialToValues.Add(query["to"] ?? "");
            if (string.Equals(epic, "ETH", StringComparison.OrdinalIgnoreCase))
            {
                await Task.Delay(1200, cancellationToken);
            }
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    prices = new[]
                    {
                        new
                        {
                            snapshotTimeUTC = "2026-07-20T00:00:00Z",
                            openPrice = new { bid = 100m },
                            highPrice = new { bid = 101m },
                            lowPrice = new { bid = 99m },
                            closePrice = new { bid = 100m },
                        },
                    },
                }), Encoding.UTF8, "application/json"),
            };
        }
    }

    private sealed class FakeCapitalStreamingSocket(WebSocketState state, bool closeOnReceive = false, bool blockClose = false) : ICapitalStreamingSocket
    {
        public WebSocketState State { get; private set; } = state;
        public int DisposeCount { get; private set; }
        public List<string> SentMessages { get; } = [];

        public Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
        {
            State = WebSocketState.Open;
            return Task.CompletedTask;
        }

        public Task SendAsync(ArraySegment<byte> bytes, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            SentMessages.Add(Encoding.UTF8.GetString(bytes.Array!, bytes.Offset, bytes.Count));
            return Task.CompletedTask;
        }

        public Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            if (closeOnReceive) State = WebSocketState.Closed;
            return Task.FromResult(new WebSocketReceiveResult(0, WebSocketMessageType.Close, true, WebSocketCloseStatus.NormalClosure, "closed"));
        }

        public Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            if (blockClose) return Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            State = WebSocketState.Closed;
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            DisposeCount++;
            State = WebSocketState.Closed;
        }
    }
}
