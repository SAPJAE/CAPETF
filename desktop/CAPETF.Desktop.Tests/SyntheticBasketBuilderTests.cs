using CAPETF.Desktop;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace CAPETF.Desktop.Tests;

public static class SyntheticBasketBuilderTests
{
    public static void RunAll()
    {
        NewOperationCancelsAndSupersedesEarlierWork();
        IncompleteOhlcRowsAreExcluded();
        CapitalPricePathSupportsDatedHistoryWindows();
        CapitalHistoryPagingWindowsMatchCapitalResolutions();
        SyntheticHistoryServiceMapsTerminalTimeframesToCapitalResolutions();
        SyntheticHistoryServiceAggregatesHourlyCandlesLocally();
        SyntheticHistoryServiceUsesStableUtcBucketsAndRejectsGaps();
        SelectedHistoryRebuildKeepsTheExactSelectedEpics();
        SelectedHistoryRebuildRejectsMissingSelectedLeg();
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
        EtfMetadataMergeUsesDeterministicFallbacks();
        EtfMetadataMergeDerivesRegionFromCapitalCountry();
        EncryptedEtfCatalogIncludesLoadedEtfEpics();
        EncryptedEtfCacheKeepsOnlyEtfInstruments();
        SimilarityPrefersCorrelatedPricePathsOverVolatilityOnlyNeighbors();
        SyntheticComponentEpicsTakePrecedenceOverVisibleInstruments();
        SyntheticQuoteDistinguishesMatchFromCandleChange();
        SamePriceSyntheticQuoteRefreshesMetadataWithoutChangingCandle();
        SyntheticQuoteUsesComponentPriceWhenDashboardInstrumentIsAbsent();
        SyntheticComponentDisplayPriceFallsBackToBaseline();
        SyntheticTerminalPayloadIncludesCandlesComponentsCurrencyAndMas();
        SyntheticTerminalPayloadIncludesSelectionBasis();
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
        SyntheticTerminalLiveUpdateReturnsPayloadImmediately();
        StreamingQuoteClearsMissingAndZeroSides();
        StreamingQuoteClearsUnavailableSidesWithoutUsablePrice();
        SyntheticTerminalHtmlExposesRequiredFunctions();
        SyntheticTerminalHtmlShowsBidAndAskWithoutLastPrice();
        SyntheticTerminalHtmlUsesPackagedChartLibrary();
        SyntheticTerminalHtmlRejectsKLineChartRuntime();
        SyntheticTerminalHtmlUsesV3LightweightChartsTerminal();
        SyntheticTerminalHtmlUsesV5SeriesApiAndChartSideTools();
        SyntheticTerminalHtmlExposesResizeFunction();
        SyntheticTerminalHtmlExposesDecisionChartControls();
        SyntheticTerminalHtmlExposesV2TerminalControls();
        CapComTerminalUsesClearActionLabelsAndSymbolDropdown();
        SeedSearchOptionsIncludePalantirByNameAndSymbolAcrossBlocks();
        SeededSyntheticBuildsDoNotUseGenericHistoryFallback();
        CapComTerminalIntradayMinimumsFitCachedHourlyHistory();
        StockRefreshFetchesDeepHourlyHistory();
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
        ExecutablePreviewUsesCurrentEqualNotionalAndDealRules();
        ExecutablePreviewRoundsUpToCapitalDealMinimumAndIncrement();
        SavedSyntheticBasketStorePersistsFormulaDetails();
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

    private static void SyntheticHistoryServiceMapsTerminalTimeframesToCapitalResolutions()
    {
        AssertEqual("HOUR", SyntheticHistoryService.RequestResolution("2H"), "2H source");
        AssertEqual("HOUR", SyntheticHistoryService.RequestResolution("6H"), "6H source");
        AssertEqual("HOUR_4", SyntheticHistoryService.RequestResolution("4H"), "4H source");
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
            periodsPerYear: 252,
            minimumCandles: 120);

        if (basket is not null)
        {
            throw new Exception("a selected-leg rebuild must not silently drop a requested leg with insufficient history");
        }
    }

    private static void SyntheticHistoryServiceUsesStableUtcBucketsAndRejectsGaps()
    {
        var start = DateTimeOffset.Parse("2026-07-20T09:00:00Z");
        var firstLeg = HourlyCandles(start, 4);
        var secondLeg = HourlyCandles(start.AddHours(1), 4);

        var firstTwoHour = SyntheticHistoryService.Transform(firstLeg, "2H");
        var secondTwoHour = SyntheticHistoryService.Transform(secondLeg, "2H");
        var sharedTimes = firstTwoHour.Select(candle => candle.Time).Intersect(secondTwoHour.Select(candle => candle.Time)).ToList();

        AssertEqual(1, sharedTimes.Count, "different source offsets must retain their shared 2H UTC bucket");
        AssertEqual(start.AddHours(2), sharedTimes[0], "2H buckets must use a deterministic UTC bucket-end timestamp");

        var twoHourWithGap = SyntheticHistoryService.Transform(
            HourlyCandles(start.AddHours(1), 4).Where(candle => candle.Time != start.AddHours(2)).ToList(),
            "2H");
        AssertEqual(1, twoHourWithGap.Count, "a missing hourly candle must omit its 2H bucket");
        AssertEqual(start.AddHours(4), twoHourWithGap[0].Time, "the next complete 2H bucket must remain available");

        var sixHourWithGap = SyntheticHistoryService.Transform(
            HourlyCandles(start.AddHours(3), 12).Where(candle => candle.Time != start.AddHours(5)).ToList(),
            "6H");
        AssertEqual(1, sixHourWithGap.Count, "a missing hourly candle must omit its 6H bucket");
        AssertEqual(start.AddHours(14), sixHourWithGap[0].Time, "the next complete 6H bucket must use its UTC bucket-end timestamp");
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
        AssertEqual("CLOSED", merged.Status, "ETF metadata merge should preserve the cached closed status");
        AssertNear(100m, merged.Price ?? 0m, "ETF metadata merge should retain the cached price");
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
            "TerminalUniverse.Accepts(universe, item, _knownEtfEpics)",
            "EnrichEtfMetadataAsync",
            "EtfMetadataMerger.Merge",
        })
        {
            if (!source.Contains(required, StringComparison.Ordinal))
            {
                throw new Exception($"cap.com Terminal must use the ETF catalog for filtering and metadata enrichment: {required}");
            }
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

    private static void SyntheticTerminalLiveUpdateReturnsPayloadImmediately()
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
        if (result.Payload is null) throw new Exception("terminal live update must return a fresh chart payload");
        AssertNear(12m, result.Payload.Candles[^1].Close, "terminal payload must contain the updated synthetic close");
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
            "attachPrimitive",
            "class HorizontalLinePrimitive",
            "class TrendLinePrimitive",
            "id=\"chart-tool-dock\"",
            "data-tool=\"crosshair\"",
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

        AssertNear(0.06m, SyntheticOrderSizing.DisplayMultiplier(component), "formula display should be rounded to two decimals");
        AssertNear(1m, SyntheticOrderSizing.ExecutableLegQuantity(component, 1m), "executable size must respect Capital.com min deal size");
        AssertNear(1.3m, SyntheticOrderSizing.ExecutableLegQuantity(component, 20m), "executable size must round up to Capital.com min size increment");

        var freeSized = new SyntheticComponent(new MarketInstrument { Epic = "BB" }, 33.3333m, 0m, 0m)
        {
            FormulaMultiplier = 3.93081368m,
        };
        AssertNear(3.93m, SyntheticOrderSizing.DisplayMultiplier(freeSized), "display rounding should not require deal rules");
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

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new Exception($"{message}. Expected {expected}, got {actual}");
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
}
