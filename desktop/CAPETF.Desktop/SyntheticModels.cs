using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CAPETF.Desktop;

public sealed record OhlcPoint(DateTimeOffset Time, decimal Open, decimal High, decimal Low, decimal Close);

public sealed class SyntheticComponent(
    MarketInstrument instrument,
    decimal weight,
    decimal annualizedVolatilityPct,
    decimal fourYearReturnPct) : INotifyPropertyChanged
{
    private decimal? _syntheticBaselinePrice;

    public MarketInstrument Instrument { get; } = instrument;
    public decimal Weight { get; } = weight;
    public decimal AnnualizedVolatilityPct { get; } = annualizedVolatilityPct;
    public decimal FourYearReturnPct { get; } = fourYearReturnPct;
    public decimal FormulaMultiplier { get; set; } = weight / 100m;
    public decimal? FormulaReferencePrice { get; set; }
    public decimal? LastAppliedPrice { get; set; }

    public decimal? SyntheticBaselinePrice
    {
        get => _syntheticBaselinePrice;
        set
        {
            if (EqualityComparer<decimal?>.Default.Equals(_syntheticBaselinePrice, value)) return;
            _syntheticBaselinePrice = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DisplayPrice));
        }
    }

    public decimal? DisplayPrice => Instrument.Price ?? SyntheticBaselinePrice;

    public event PropertyChangedEventHandler? PropertyChanged;

    public void NotifyInstrumentPriceChanged() => OnPropertyChanged(nameof(DisplayPrice));

    private void OnPropertyChanged([CallerMemberName] string propertyName = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed class SyntheticBasket : INotifyPropertyChanged
{
    private decimal _basketPrice;
    private decimal? _bidPrice;
    private decimal? _askPrice;
    private decimal? _lastPrice;
    private DateTimeOffset? _lastUpdated;

    public string Symbol { get; init; } = "";
    public string Block { get; init; } = "";
    public decimal AverageVolatilityPct { get; init; }
    public decimal SimilarityScore { get; init; }
    public ObservableCollection<SyntheticComponent> Components { get; } = [];
    public ObservableCollection<OhlcPoint> Candles { get; } = [];

    public decimal BasketPrice
    {
        get => _basketPrice;
        set => SetField(ref _basketPrice, value);
    }

    public decimal? BidPrice
    {
        get => _bidPrice;
        set => SetField(ref _bidPrice, value);
    }

    public decimal? AskPrice
    {
        get => _askPrice;
        set => SetField(ref _askPrice, value);
    }

    public decimal? LastPrice
    {
        get => _lastPrice;
        set => SetField(ref _lastPrice, value);
    }

    public DateTimeOffset? LastUpdated
    {
        get => _lastUpdated;
        set => SetField(ref _lastUpdated, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed record SyntheticBuildResult(
    IReadOnlyList<SyntheticBasket> Baskets,
    string Message);

public static class SyntheticQuoteCalculator
{
    public static void Refresh(SyntheticBasket basket)
    {
        basket.BidPrice = SumStrict(basket.Components, component => component.Instrument.Bid);
        basket.AskPrice = SumStrict(basket.Components, component => component.Instrument.Offer);
        basket.LastPrice = SumStrict(basket.Components, ComponentLastPrice);
    }

    private static decimal? SumStrict(
        IEnumerable<SyntheticComponent> components,
        Func<SyntheticComponent, decimal?> priceSelector)
    {
        decimal total = 0m;
        var count = 0;
        foreach (var component in components)
        {
            var price = priceSelector(component);
            if (price is null || price <= 0) return null;
            total += price.Value * component.FormulaMultiplier;
            count++;
        }

        return count == 0 ? null : decimal.Round(total, 6);
    }

    private static decimal? ComponentLastPrice(SyntheticComponent component)
    {
        if (component.DisplayPrice is not null) return component.DisplayPrice;
        if (component.Instrument.Bid is not null && component.Instrument.Offer is not null)
        {
            return (component.Instrument.Bid.Value + component.Instrument.Offer.Value) / 2m;
        }

        return null;
    }
}
