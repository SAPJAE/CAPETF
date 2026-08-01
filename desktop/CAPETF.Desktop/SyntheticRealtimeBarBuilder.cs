namespace CAPETF.Desktop;

public sealed class SyntheticRealtimeBarBuilder
{
    private readonly Dictionary<string, CapitalOhlcUpdate> _componentBars =
        new(StringComparer.OrdinalIgnoreCase);
    private SyntheticBasket? _basket;
    private string _timeframe = "";

    public void Reset(SyntheticBasket basket, string timeframe)
    {
        ArgumentNullException.ThrowIfNull(basket);
        _basket = basket;
        _timeframe = timeframe?.Trim() ?? "";
        _componentBars.Clear();
    }

    public void Clear()
    {
        _basket = null;
        _timeframe = "";
        _componentBars.Clear();
    }

    public bool Apply(SyntheticBasket basket, CapitalOhlcUpdate update)
    {
        ArgumentNullException.ThrowIfNull(basket);
        ArgumentNullException.ThrowIfNull(update);
        if (_timeframe is not ("4H" or "Daily" or "Weekly") ||
            !ReferenceEquals(_basket, basket) ||
            !string.Equals(update.Resolution, StreamingResolution(_timeframe), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var component = basket.Components.FirstOrDefault(candidate =>
            string.Equals(candidate.Instrument.Epic, update.Epic, StringComparison.OrdinalIgnoreCase));
        if (component is null) return false;
        if (_componentBars.TryGetValue(update.Epic, out var previous) && update.Time < previous.Time)
        {
            return false;
        }
        _componentBars[update.Epic] = update;

        var bars = new List<(decimal Multiplier, OhlcPoint Candle)>(basket.Components.Count);
        string? sharedKey = null;
        foreach (var basketComponent in basket.Components)
        {
            if (!_componentBars.TryGetValue(basketComponent.Instrument.Epic, out var componentBar))
            {
                return false;
            }
            var key = SyntheticHistoryService.AlignmentKey(componentBar.Time, _timeframe);
            if (sharedKey is not null && !string.Equals(sharedKey, key, StringComparison.Ordinal))
            {
                return false;
            }
            sharedKey = key;
            bars.Add((basketComponent.FormulaMultiplier, componentBar.Candle));
        }

        var barTime = _componentBars[basket.Components[0].Instrument.Epic].Time;
        var synthetic = ManualSyntheticBasketFactory.BuildCandle(barTime, bars);
        if (basket.Candles.Count == 0)
        {
            basket.Candles.Add(synthetic);
        }
        else
        {
            var last = basket.Candles[^1];
            var lastKey = SyntheticHistoryService.AlignmentKey(last.Time, _timeframe);
            if (string.Equals(lastKey, sharedKey, StringComparison.Ordinal))
            {
                if (last == synthetic) return false;
                basket.Candles[^1] = synthetic;
            }
            else
            {
                if (synthetic.Time <= last.Time) return false;
                basket.Candles.Add(synthetic);
            }
        }

        basket.BasketPrice = synthetic.Close;
        basket.LastUpdated = update.Time;
        return true;
    }

    public static string StreamingResolution(string timeframe) => timeframe switch
    {
        "4H" => "HOUR_4",
        "Daily" => "DAY",
        "Weekly" => "WEEK",
        _ => "HOUR",
    };
}
