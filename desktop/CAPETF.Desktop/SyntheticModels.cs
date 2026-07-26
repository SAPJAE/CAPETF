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
