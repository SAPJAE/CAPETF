namespace CAPETF.Desktop;

public readonly record struct SyntheticQuoteApplyResult(bool Matched, bool CandleChanged);

public static class SyntheticLiveUpdate
{
    public static IReadOnlyList<string> PrioritizedEpics(
        IEnumerable<MarketInstrument> visible,
        IEnumerable<SyntheticBasket> baskets,
        int maximum = 40) =>
        baskets.SelectMany(basket => basket.Components)
            .Select(component => component.Instrument.Epic)
            .Concat(visible.Select(instrument => instrument.Epic))
            .Where(epic => !string.IsNullOrWhiteSpace(epic))
            .Distinct(StringComparer.Ordinal)
            .Take(maximum)
            .ToList();

    public static SyntheticQuoteApplyResult ApplyQuote(SyntheticBasket basket, QuoteUpdate update)
    {
        var component = basket.Components.FirstOrDefault(item => item.Instrument.Epic == update.Epic);
        if (component is null || update.Price is null || update.Price <= 0) return default;

        var componentPreviousPrice = component.LastAppliedPrice ?? component.SyntheticBaselinePrice ?? component.Instrument.Price;
        component.Instrument.Bid = update.Bid > 0 ? update.Bid : component.Instrument.Bid;
        component.Instrument.Offer = update.Offer > 0 ? update.Offer : component.Instrument.Offer;
        component.Instrument.Price = update.Price;
        component.LastAppliedPrice = update.Price;
        component.NotifyInstrumentPriceChanged();
        basket.LastUpdated = update.Time;
        SyntheticQuoteCalculator.Refresh(basket);
        if (componentPreviousPrice is null || componentPreviousPrice <= 0 || basket.Candles.Count == 0)
        {
            return new SyntheticQuoteApplyResult(true, false);
        }

        var last = basket.Candles[^1];
        var delta = (update.Price.Value - componentPreviousPrice.Value) * component.FormulaMultiplier;
        var updated = last with
        {
            High = Math.Max(last.High, last.Close + delta),
            Low = Math.Min(last.Low, last.Close + delta),
            Close = decimal.Round(last.Close + delta, 6),
        };
        var candleChanged = updated != last;
        if (candleChanged)
        {
            basket.Candles[^1] = updated;
            basket.BasketPrice = updated.Close;
        }
        return new SyntheticQuoteApplyResult(true, candleChanged);
    }
}
