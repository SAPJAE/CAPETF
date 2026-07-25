using CAPETF.Desktop;
using System.IO;

namespace CAPETF.Desktop.Tests;

public static class SyntheticBasketBuilderTests
{
    public static void RunAll()
    {
        NewOperationCancelsAndSupersedesEarlierWork();
        IncompleteOhlcRowsAreExcluded();
        InverseVolatilityWeightsSumToOneHundred();
        InverseVolatilityWeightsRespectCapsAndMinimums();
        SyntheticCandlesUseWeightedOhlc();
        SyntheticBasketsDoNotMixCurrencies();
        SyntheticBasketsKeepBlankCurrenciesTogether();
        SyntheticBasketsAllowBlankCurrenciesInsideSelectedBlock();
        VolatilityAnnualizationUsesRequestedPeriodsPerYear();
        TrailingReturnsUseIntervalAwareHorizonsFromFinalCandle();
        LiveQuoteUpdatesBasketPriceAndTimestamp();
        SyntheticBuildPreservesLivePriceAndSeedsHistoricalBaseline();
        SyntheticBasketsExcludeNonStockMarkets();
        StockTypeMatchingIsCaseInsensitive();
        SimilarityPrefersCorrelatedPricePathsOverVolatilityOnlyNeighbors();
        SyntheticComponentEpicsTakePrecedenceOverVisibleInstruments();
        SyntheticQuoteDistinguishesMatchFromCandleChange();
        SamePriceSyntheticQuoteRefreshesMetadataWithoutChangingCandle();
        SyntheticQuoteUsesComponentPriceWhenDashboardInstrumentIsAbsent();
        SyntheticComponentDisplayPriceFallsBackToBaseline();
        SyntheticTerminalPayloadIncludesCandlesComponentsCurrencyAndMas();
        SyntheticTerminalSelectorChoosesHighestSimilarityBasket();
        SyntheticTerminalSelectorUsesThreeYearComparisonWindow();
        SyntheticTerminalSelectorPenalizesVolatilityMismatch();
        SyntheticTerminalHistoryLoadCandidatesScanPastTheFirstSparseRows();
        SyntheticTerminalLiveUpdateReturnsPayloadImmediately();
        SyntheticTerminalHtmlExposesRequiredFunctions();
        SyntheticTerminalHtmlUsesPackagedChartLibrary();
        SyntheticTerminalHtmlUsesPackagedKLineChartLibrary();
        SyntheticTerminalHtmlExposesResizeFunction();
        SyntheticTerminalHtmlExposesDecisionChartControls();
        SyntheticTerminalHtmlExposesV2TerminalControls();
        DesktopDefaultSearchDoesNotFilterStocksByEtf();
        DesktopResizesTerminalChartWhenWorkspaceOpens();
        DesktopTerminalWorkspaceExposesChartFirstControls();
        DesktopTerminalWorkspaceExposesV2ProfessionalControls();
        CapComTerminalStartsWithoutDevExpressStockSharpRuntimeCrash();
        TerminalWorkspaceModeNameIsAvailable();
        TerminalStreamingEpicsUseOnlySelectedSyntheticComponents();
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
        AssertNear(20m, first.Open, "weighted open should use component opens");
        AssertNear(22m, first.High, "weighted high should use component highs");
        AssertNear(19m, first.Low, "weighted low should use component lows");
        AssertNear(21m, first.Close, "weighted close should use component closes");
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

    private static void SyntheticBasketsExcludeNonStockMarkets()
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

        var result = SyntheticBasketBuilder.Build("Non-stock block", instruments, candles);

        if (result.Baskets.Count != 0)
        {
            throw new Exception("Capital markets whose instrument type is not SHARES must not form synthetic baskets");
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

    private static void SyntheticBuildPreservesLivePriceAndSeedsHistoricalBaseline()
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
        var priorBasketClose = basket.Candles[^1].Close;
        var quote = new QuoteUpdate(component.Instrument.Epic, null, null, historicalClose + 5m, day.AddDays(121));

        AssertNear(
            500m + instruments.IndexOf(component.Instrument),
            component.Instrument.Price ?? 0m,
            "basket build must preserve an existing live/display component price");
        AssertNear(
            historicalClose,
            component.SyntheticBaselinePrice ?? 0m,
            "basket build should keep the historical close in a separate synthetic baseline");

        if (!SyntheticLiveUpdate.ApplyQuote(basket, quote).CandleChanged)
        {
            throw new Exception("the first live quote must use the fetched historical close without rewinding the display price");
        }

        AssertNear(
            priorBasketClose + 5m * component.Weight / 100m,
            basket.Candles[^1].Close,
            "first live quote should advance from the historical component close");
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
        if (payload.Ma20.Count == 0 || payload.Ma50.Count == 0 || payload.Ma200.Count == 0)
        {
            throw new Exception("terminal payload must include MA 20, MA 50, and MA 200 when enough candles exist");
        }
        AssertNear(310.5m, payload.Ma20[^1].Value, "MA20 must average the last 20 closes");
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

    private static void SyntheticTerminalHtmlUsesPackagedKLineChartLibrary()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "synthetic-terminal.html");
        if (!File.Exists(path)) throw new Exception("terminal chart HTML must be copied to output");
        var html = File.ReadAllText(path);
        if (!html.Contains("klinecharts.min.js", StringComparison.Ordinal))
        {
            throw new Exception("terminal V2 HTML must use the packaged KLineChart asset");
        }

        var scriptPath = Path.Combine(AppContext.BaseDirectory, "Assets", "klinecharts.min.js");
        if (!File.Exists(scriptPath))
        {
            throw new Exception("packaged KLineChart asset must be copied to output");
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
            "line",
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
            "klinecharts.init",
            "window.setTerminalInterval",
            "window.setTerminalIndicator",
            "window.setTerminalDrawingTool",
            "window.placeSyntheticPreviewOrder",
            "terminal-order-ticket",
            "SYNTHETIC FORMULA",
            "VOL MATCH"
        })
        {
            if (!html.Contains(required, StringComparison.Ordinal))
            {
                throw new Exception($"terminal V2 HTML missing expected control {required}");
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
            "x:Name=\"StockSharpChartHost\"",
            "x:Name=\"SyntheticFormulaText\"",
            "Click=\"BuildSynthetic_Click\"",
            "Click=\"BuyPreview_Click\"",
            "Click=\"SellPreview_Click\"",
            "x:Name=\"CandleTypeBox\"",
            "CandleType_SelectionChanged",
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
            "DisplayCandles",
            "DrawPriceAxisLabels",
            "DrawDateAxisLabels",
            "NativeCandleCanvas is null",
            "Heikin Ashi",
        })
        {
            if (!source.Contains(required, StringComparison.Ordinal))
            {
                throw new Exception($"cap.com Terminal source missing {required}");
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

    private static MarketInstrument CreateStock(string epic, string name) => new()
    {
        Epic = epic,
        Name = name,
        Type = "SHARES",
        Currency = "USD",
        Region = "US",
        Sector = "Tech",
    };

    private static void AssertNear(decimal expected, decimal actual, string message, decimal tolerance = 0.0001m)
    {
        if (Math.Abs(expected - actual) > tolerance) throw new Exception($"{message}. Expected {expected}, got {actual}");
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
}
