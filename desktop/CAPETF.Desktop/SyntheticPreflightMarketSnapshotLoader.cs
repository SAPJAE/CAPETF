namespace CAPETF.Desktop;

internal sealed record SyntheticPreflightMarketSnapshotResult(
    SyntheticBasket? Basket,
    IReadOnlyList<MarketInstrument> Snapshots,
    IReadOnlyList<SyntheticPreflightFailure> Failures);

internal sealed class SyntheticPreflightMarketSnapshotLoader
{
    private readonly Func<string, CancellationToken, Task<MarketInstrument?>> _getMarketDetails;

    public SyntheticPreflightMarketSnapshotLoader(
        Func<string, CancellationToken, Task<MarketInstrument?>> getMarketDetails)
    {
        _getMarketDetails = getMarketDetails ?? throw new ArgumentNullException(nameof(getMarketDetails));
    }

    public async Task<SyntheticPreflightMarketSnapshotResult> LoadAsync(
        SyntheticBasket source,
        CancellationToken cancellationToken,
        Action<int, int>? reportProgress = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        var snapshots = new List<MarketInstrument>(source.Components.Count);
        var freshComponents = new List<(SyntheticComponent Source, MarketInstrument Snapshot)>(source.Components.Count);
        var failures = new List<SyntheticPreflightFailure>();
        var completed = 0;

        foreach (var component in source.Components)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var epic = component.Instrument.Epic?.Trim() ?? "";
            try
            {
                if (string.IsNullOrWhiteSpace(epic))
                {
                    failures.Add(new SyntheticPreflightFailure("", "Current market details require an epic."));
                    continue;
                }

                var details = await _getMarketDetails(epic, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (details is null)
                {
                    failures.Add(new SyntheticPreflightFailure(epic, "Current market details were not returned."));
                    continue;
                }

                ApplyNewerStreamQuote(component.Instrument, details);
                snapshots.Add(details);
                var missing = MissingTradingMetadata(epic, details);
                if (missing.Count > 0)
                {
                    failures.Add(new SyntheticPreflightFailure(
                        epic,
                        $"Current market details are incomplete: {string.Join(", ", missing)}."));
                    continue;
                }

                freshComponents.Add((component, details));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                failures.Add(new SyntheticPreflightFailure(
                    epic,
                    $"Current market details refresh failed: {exception.Message}"));
            }
            finally
            {
                completed++;
                reportProgress?.Invoke(completed, source.Components.Count);
            }
        }

        if (failures.Count > 0 || freshComponents.Count != source.Components.Count)
        {
            return new SyntheticPreflightMarketSnapshotResult(
                null,
                Array.AsReadOnly(snapshots.ToArray()),
                Array.AsReadOnly(failures.ToArray()));
        }

        var basket = CloneBasketWithFreshComponents(source, freshComponents);
        return new SyntheticPreflightMarketSnapshotResult(
            basket,
            Array.AsReadOnly(snapshots.ToArray()),
            Array.Empty<SyntheticPreflightFailure>());
    }

    private static void ApplyNewerStreamQuote(MarketInstrument streamed, MarketInstrument details)
    {
        if (!string.Equals(streamed.Epic?.Trim(), details.Epic?.Trim(), StringComparison.OrdinalIgnoreCase) ||
            streamed.LastTickAt is not { } streamedAt ||
            streamed.Bid is not > 0m ||
            streamed.Offer is not > 0m ||
            details.LastTickAt is { } detailsAt && detailsAt.ToUniversalTime() >= streamedAt.ToUniversalTime())
        {
            return;
        }

        details.Bid = streamed.Bid;
        details.Offer = streamed.Offer;
        details.Price = streamed.Price is > 0m
            ? streamed.Price
            : (streamed.Bid.Value + streamed.Offer.Value) / 2m;
        details.LastTickAt = streamedAt;
    }

    private static IReadOnlyList<string> MissingTradingMetadata(string requestedEpic, MarketInstrument details)
    {
        var missing = new List<string>();
        if (!string.Equals(requestedEpic, details.Epic?.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            missing.Add("matching epic");
        }
        if (string.IsNullOrWhiteSpace(details.Status)) missing.Add("market status");
        if (details.Bid is not > 0m || details.Offer is not > 0m) missing.Add("bid/offer quote");
        if (details.LastTickAt is null) missing.Add("quote timestamp");
        if (string.IsNullOrWhiteSpace(details.Currency)) missing.Add("currency");
        if (details.LotSize is not > 0m) missing.Add("lot size");
        if (details.MinDealSize is not > 0m) missing.Add("minimum deal size");
        if (details.MinSizeIncrement is not > 0m) missing.Add("minimum size increment");
        if (details.MarginFactor is not > 0m) missing.Add("margin factor");
        if (!string.Equals(details.MarginFactorUnit?.Trim(), "PERCENTAGE", StringComparison.OrdinalIgnoreCase))
        {
            missing.Add("percentage margin unit");
        }
        return missing;
    }

    private static SyntheticBasket CloneBasketWithFreshComponents(
        SyntheticBasket source,
        IReadOnlyList<(SyntheticComponent Source, MarketInstrument Snapshot)> freshComponents)
    {
        var basket = new SyntheticBasket
        {
            Symbol = source.Symbol,
            Block = source.Block,
            UniverseKind = source.UniverseKind,
            Strategy = source.Strategy,
            AverageVolatilityPct = source.AverageVolatilityPct,
            SimilarityScore = source.SimilarityScore,
            BasketPrice = source.BasketPrice,
            LastPrice = source.LastPrice,
            LastUpdated = source.LastUpdated,
        };
        foreach (var (sourceComponent, snapshot) in freshComponents)
        {
            basket.Components.Add(new SyntheticComponent(
                snapshot,
                sourceComponent.Weight,
                sourceComponent.AnnualizedVolatilityPct,
                sourceComponent.FourYearReturnPct)
            {
                FormulaMultiplier = sourceComponent.FormulaMultiplier,
                FormulaReferencePrice = sourceComponent.FormulaReferencePrice,
                LastAppliedPrice = sourceComponent.LastAppliedPrice,
                SyntheticBaselinePrice = sourceComponent.SyntheticBaselinePrice,
            });
        }
        foreach (var candle in source.Candles)
        {
            basket.Candles.Add(candle);
        }

        SyntheticQuoteCalculator.Refresh(basket);
        return basket;
    }
}
