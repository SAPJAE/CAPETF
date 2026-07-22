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
    private DateTimeOffset? _lastTickAt;
    private string _status = "";

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
        set => SetField(ref _bid, value);
    }

    public decimal? Offer
    {
        get => _offer;
        set => SetField(ref _offer, value);
    }

    public decimal? IntradayReturn
    {
        get => _intradayReturn;
        set => SetField(ref _intradayReturn, value);
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

    public event PropertyChangedEventHandler? PropertyChanged;

    private static string Normalize(string value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed record ChartPoint(DateTimeOffset Time, decimal Close);

public sealed record QuoteUpdate(string Epic, decimal? Bid, decimal? Offer, decimal? Price, DateTimeOffset Time);
