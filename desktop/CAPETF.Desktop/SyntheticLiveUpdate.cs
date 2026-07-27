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

    public static SyntheticQuoteApplyResult ApplyQuote(
        SyntheticBasket basket,
        QuoteUpdate update,
        string? timeframe = null)
    {
        var component = basket.Components.FirstOrDefault(item => item.Instrument.Epic == update.Epic);
        if (component is null) return default;
        if (component.Instrument.LastTickAt is { } lastSourceTime &&
            update.Time.ToUniversalTime() < lastSourceTime.ToUniversalTime())
        {
            return default;
        }

        component.Instrument.Bid = update.Bid is > 0 ? update.Bid : null;
        component.Instrument.Offer = update.Offer is > 0 ? update.Offer : null;
        component.Instrument.LastTickAt = update.Time;
        basket.LastUpdated = update.Time;
        SyntheticQuoteCalculator.Refresh(basket);
        if (update.Price is null || update.Price <= 0) return new SyntheticQuoteApplyResult(true, false);

        var componentPreviousPrice = component.LastAppliedPrice ?? component.SyntheticBaselinePrice ?? component.Instrument.Price;
        component.Instrument.Price = update.Price;
        component.LastAppliedPrice = update.Price;
        component.NotifyInstrumentPriceChanged();
        if (componentPreviousPrice is null || componentPreviousPrice <= 0 || basket.Candles.Count == 0)
        {
            return new SyntheticQuoteApplyResult(true, false);
        }

        var candleRolled = StartCurrentCandleIfNeeded(basket, update.Time, timeframe);
        var last = basket.Candles[^1];
        var delta = (update.Price.Value - componentPreviousPrice.Value) * component.FormulaMultiplier;
        var updated = last with
        {
            High = Math.Max(last.High, last.Close + delta),
            Low = Math.Min(last.Low, last.Close + delta),
            Close = decimal.Round(last.Close + delta, 6),
        };
        var candleChanged = candleRolled || updated != last;
        if (updated != last)
        {
            basket.Candles[^1] = updated;
        }
        if (candleChanged) basket.BasketPrice = updated.Close;
        return new SyntheticQuoteApplyResult(true, candleChanged);
    }

    private static bool StartCurrentCandleIfNeeded(
        SyntheticBasket basket,
        DateTimeOffset quoteTime,
        string? timeframe)
    {
        if (string.IsNullOrWhiteSpace(timeframe) || basket.Candles.Count == 0) return false;

        var quoteBucket = CandleBucket(quoteTime, timeframe);
        var lastBucket = CandleBucket(basket.Candles[^1].Time, timeframe);
        if (quoteBucket <= lastBucket) return false;

        var previousClose = basket.Candles[^1].Close;
        basket.Candles.Add(new OhlcPoint(
            quoteBucket,
            previousClose,
            previousClose,
            previousClose,
            previousClose));
        return true;
    }

    private static DateTimeOffset CandleBucket(DateTimeOffset time, string timeframe)
    {
        var utc = time.ToUniversalTime();
        var day = new DateTimeOffset(utc.Year, utc.Month, utc.Day, 0, 0, 0, TimeSpan.Zero);
        if (timeframe.Equals("Weekly", StringComparison.OrdinalIgnoreCase))
        {
            var daysSinceMonday = ((int)day.DayOfWeek + 6) % 7;
            return day.AddDays(-daysSinceMonday);
        }

        if (timeframe.Equals("Daily", StringComparison.OrdinalIgnoreCase)) return day;

        var hours = timeframe.ToUpperInvariant() switch
        {
            "2H" => 2,
            "4H" => 4,
            "6H" => 6,
            _ => 0,
        };
        return hours == 0 ? utc : day.AddHours((utc.Hour / hours) * hours);
    }
}
