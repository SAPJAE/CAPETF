using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CAPETF.Desktop;

public sealed class ApiCredentials
{
    public string Identifier { get; set; } = "";
    public string Password { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public bool UseDemo { get; set; } = true;
}

public sealed class CapitalSession
{
    public string Cst { get; init; } = "";
    public string SecurityToken { get; init; } = "";
    public bool UseDemo { get; init; }
}

public sealed class MarketInstrument : INotifyPropertyChanged
{
    private decimal? _price;
    private decimal? _bid;
    private decimal? _offer;
    private decimal? _intradayReturn;
    private decimal? _changePercent;
    private decimal? _low;
    private decimal? _high;
    private decimal? _sma20;
    private decimal? _sma50;
    private decimal? _alertPrice;
    private DateTimeOffset? _lastTickAt;
    private string _status = "";
    private bool _isWatchlisted;

    public string Epic { get; init; } = "";
    public string Name { get; init; } = "";
    public string Symbol { get; init; } = "";
    public string Type { get; init; } = "";
    public string Currency { get; init; } = "";
    public string Country { get; init; } = "";
    public string Sector { get; init; } = "";
    public string Region { get; init; } = "";
    public ObservableCollection<ChartPoint> Points { get; } = [];

    public decimal? Price
    {
        get => _price;
        set => SetField(ref _price, value);
    }

    public decimal? Bid
    {
        get => _bid;
        set
        {
            SetField(ref _bid, value);
            OnPropertyChanged(nameof(Spread));
        }
    }

    public decimal? Offer
    {
        get => _offer;
        set
        {
            SetField(ref _offer, value);
            OnPropertyChanged(nameof(Spread));
        }
    }

    public decimal? IntradayReturn
    {
        get => _intradayReturn;
        set => SetField(ref _intradayReturn, value);
    }

    public decimal? ChangePercent
    {
        get => _changePercent;
        set => SetField(ref _changePercent, value);
    }

    public decimal? Low
    {
        get => _low;
        set => SetField(ref _low, value);
    }

    public decimal? High
    {
        get => _high;
        set => SetField(ref _high, value);
    }

    public decimal? Sma20
    {
        get => _sma20;
        set
        {
            SetField(ref _sma20, value);
            OnPropertyChanged(nameof(TrendLabel));
        }
    }

    public decimal? Sma50
    {
        get => _sma50;
        set
        {
            SetField(ref _sma50, value);
            OnPropertyChanged(nameof(TrendLabel));
        }
    }

    public decimal? AlertPrice
    {
        get => _alertPrice;
        set
        {
            SetField(ref _alertPrice, value);
            OnPropertyChanged(nameof(AlertText));
        }
    }

    public bool IsWatchlisted
    {
        get => _isWatchlisted;
        set
        {
            SetField(ref _isWatchlisted, value);
            OnPropertyChanged(nameof(WatchlistMarker));
        }
    }

    public DateTimeOffset? LastTickAt
    {
        get => _lastTickAt;
        set => SetField(ref _lastTickAt, value);
    }

    public string Status
    {
        get => _status;
        set => SetField(ref _status, value);
    }

    public string Group => $"{Normalize(Region, "Other")} / {Normalize(Currency, "Currency")} / {Normalize(Sector, "Sector")}";
    public string TrendLabel => Sma20 is null || Sma50 is null ? "Building" : Sma20 >= Sma50 ? "Uptrend" : "Weak";
    public decimal? Spread => Bid is not null && Offer is not null ? Offer - Bid : null;
    public string WatchlistMarker => IsWatchlisted ? "Saved" : "Watch";
    public string AlertText => AlertPrice is null ? "No alert" : $"Alert {AlertPrice:0.####}";
    public string Category => string.IsNullOrWhiteSpace(Type) ? "Market" : Type;

    public event PropertyChangedEventHandler? PropertyChanged;

    private static string Normalize(string value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        OnPropertyChanged(propertyName);
    }

    private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed record ChartPoint(DateTimeOffset Time, decimal Close);

public sealed record QuoteUpdate(string Epic, decimal? Bid, decimal? Offer, decimal? Price, DateTimeOffset Time);
