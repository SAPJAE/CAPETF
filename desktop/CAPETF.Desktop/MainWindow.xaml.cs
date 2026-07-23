using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CAPETF.Desktop;

public partial class MainWindow : Window
{
    private readonly CredentialStore _credentialStore = new();
    private readonly WorkspaceStore _workspaceStore = new();
    private readonly CapitalApiClient _api = new();
    private CapitalStreamingClient? _streaming;
    private readonly ObservableCollection<InstrumentGroup> _groups = [];
    private readonly List<MarketInstrument> _instruments = [];
    private readonly WorkspaceState _workspace;
    private MarketInstrument? _selected;

    public MainWindow()
    {
        InitializeComponent();
        _workspace = _workspaceStore.Load();
        GroupList.ItemsSource = _groups;
        LoadSavedCredentials();
        UpdateStats();
        ApplyWorkspaceMode();
    }

    private void LoadSavedCredentials()
    {
        var saved = _credentialStore.Load();
        if (saved is null) return;
        IdentifierBox.Text = saved.Identifier;
        PasswordBox.Password = saved.Password;
        ApiKeyBox.Password = saved.ApiKey;
        DemoCheck.IsChecked = saved.UseDemo;
        ConnectionText.Text = "Saved keys loaded";
    }

    private async void Connect_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ConnectionText.Text = "Connecting...";
            var credentials = ReadCredentials();
            ValidateCredentials(credentials);
            await _api.LoginAsync(credentials);
            ConnectionText.Text = "Connected";
            ResultText.Text = "Connected. Press Search to load instruments.";
        }
        catch (Exception ex)
        {
            ConnectionText.Text = "Connection failed";
            ResultText.Text = ex.Message;
            MessageBox.Show(ex.Message, "Capital.com connection", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void Disconnect_Click(object sender, RoutedEventArgs e)
    {
        if (_streaming is not null)
        {
            await _streaming.DisposeAsync();
            _streaming = null;
        }
        ConnectionText.Text = "Disconnected";
    }

    private void SaveCredentials_Click(object sender, RoutedEventArgs e)
    {
        _credentialStore.Save(ReadCredentials());
        ConnectionText.Text = "Keys saved locally";
    }

    private void ForgetCredentials_Click(object sender, RoutedEventArgs e)
    {
        _credentialStore.Clear();
        ConnectionText.Text = "Saved keys removed";
    }

    private async void Search_Click(object sender, RoutedEventArgs e) => await SearchAsync();

    private async void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) await SearchAsync();
    }

    private async Task SearchAsync()
    {
        try
        {
            ResultText.Text = "Searching Capital.com...";
            _groups.Clear();
            _instruments.Clear();

            var markets = await _api.SearchMarketsAsync(SearchBox.Text.Trim());
            var filtered = FilterDataset(markets).Take(240).ToList();
            foreach (var item in filtered)
            {
                ApplyWorkspaceState(item);
                _instruments.Add(item);
            }

            await LoadHistoryForVisibleAsync(_instruments.Take(40));
            RebuildGroups();
            ResultText.Text = _instruments.Count == 0
                ? "0 instruments found. Check Dataset vs Search, for example use Dataset ETFs with search ETF."
                : $"{_instruments.Count} instruments loaded. Expand a group, then start realtime for visible.";
            UpdatedText.Text = DateTime.Now.ToString("HH:mm:ss");
            UpdateDiscoveryStrip();
            UpdateStats();
        }
        catch (Exception ex)
        {
            ResultText.Text = "Search failed";
            MessageBox.Show(ex.Message, "Search", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task LoadHistoryForVisibleAsync(IEnumerable<MarketInstrument> instruments)
    {
        var resolution = SelectedResolution();
        foreach (var item in instruments)
        {
            try
            {
                var points = await _api.GetPricesAsync(item.Epic, resolution, resolution == "DAY" ? 260 : 96);
                item.Points.Clear();
                foreach (var point in points) item.Points.Add(point);
                UpdateDerivedValues(item);
            }
            catch
            {
                item.Status = "History n/a";
            }
        }
    }

    private void RebuildGroups()
    {
        _groups.Clear();
        foreach (var group in _instruments.GroupBy(item => item.Group).OrderBy(group => group.Key))
        {
            _groups.Add(new InstrumentGroup(group.Key, group.OrderBy(item => item.Name).ToList()));
        }
    }

    private IEnumerable<MarketInstrument> FilterDataset(IEnumerable<MarketInstrument> markets)
    {
        var selected = (DatasetBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Stocks";
        if (selected == "ETFs") return markets.Where(IsEtf);
        if (selected == "Stocks") return markets.Where(item => !IsEtf(item));
        return markets.Where(item => _workspace.WatchlistEpics.Contains(item.Epic));
    }

    private static bool IsEtf(MarketInstrument item)
    {
        var text = $"{item.Name} {item.Symbol} {item.Type}".ToLowerInvariant();
        return text.Contains("etf") || text.Contains("exchange traded");
    }

    private async void StreamVisible_Click(object sender, RoutedEventArgs e)
    {
        if (_api.Session is null || _streaming is null)
        {
            if (_api.Session is null)
            {
                MessageBox.Show("Connect to Capital.com first.", "Realtime", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _streaming = new CapitalStreamingClient();
            _streaming.QuoteReceived += Streaming_QuoteReceived;
            _streaming.StatusChanged += (_, message) => Dispatcher.Invoke(() => ConnectionText.Text = message);
            await _streaming.ConnectAsync(_api.Session);
        }

        var visible = _groups.Where(group => group.IsExpanded).SelectMany(group => group.Instruments).Take(40).ToList();
        if (!visible.Any())
        {
            visible = _instruments.Take(40).ToList();
        }

        await _streaming.SubscribeQuotesAsync(_api.Session, visible.Select(item => item.Epic));
        await _streaming.SubscribeOhlcAsync(_api.Session, visible.Select(item => item.Epic), SelectedResolution());
        ConnectionText.Text = $"Streaming {visible.Count} instruments";
    }

    private void Streaming_QuoteReceived(object? sender, QuoteUpdate update)
    {
        Dispatcher.Invoke(() =>
        {
            var item = _instruments.FirstOrDefault(instrument => instrument.Epic == update.Epic);
            if (item is null) return;
            item.Bid = update.Bid;
            item.Offer = update.Offer;
            item.Price = update.Price;
            item.LastTickAt = update.Time;
            item.Status = "Live";
            if (update.Price is not null)
            {
                item.Points.Add(new ChartPoint(update.Time, update.Price.Value));
                TrimPoints(item);
                UpdateDerivedValues(item);
                CheckAlert(item);
            }
            if (_selected == item) ShowSelected(item);
            RedrawCharts();
            UpdateStats();
        });
    }

    private string SelectedResolution()
    {
        var mode = (ChartModeBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Daily";
        return mode switch
        {
            "2H" or "6H" => "HOUR",
            "4H" => "HOUR_4",
            _ => "DAY",
        };
    }

    private void UpdateDerivedValues(MarketInstrument item)
    {
        if (item.Points.Count == 0) return;
        item.Price = item.Points[^1].Close;
        var first = item.Points[0].Close;
        item.IntradayReturn = first == 0 ? null : decimal.Round(((item.Price ?? first) / first - 1) * 100, 2);
        item.ChangePercent = item.IntradayReturn;
        item.Low = item.Points.Min(point => point.Close);
        item.High = item.Points.Max(point => point.Close);
        item.Sma20 = Average(item.Points.TakeLast(20));
        item.Sma50 = Average(item.Points.TakeLast(50));
    }

    private static decimal? Average(IEnumerable<ChartPoint> points)
    {
        var values = points.Select(point => point.Close).ToList();
        return values.Count == 0 ? null : decimal.Round(values.Average(), 4);
    }

    private static void TrimPoints(MarketInstrument item)
    {
        while (item.Points.Count > 180) item.Points.RemoveAt(0);
    }

    private ApiCredentials ReadCredentials() => new()
    {
        Identifier = IdentifierBox.Text.Trim(),
        Password = PasswordBox.Password,
        ApiKey = ApiKeyBox.Password,
        UseDemo = DemoCheck.IsChecked == true,
    };

    private static void ValidateCredentials(ApiCredentials credentials)
    {
        if (string.IsNullOrWhiteSpace(credentials.Identifier)) throw new InvalidOperationException("Identifier is required.");
        if (string.IsNullOrWhiteSpace(credentials.Password)) throw new InvalidOperationException("API password is required.");
        if (string.IsNullOrWhiteSpace(credentials.ApiKey)) throw new InvalidOperationException("API key is required.");
    }

    private void InstrumentCard_Click(object sender, MouseButtonEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is MarketInstrument item)
        {
            ShowSelected(item);
        }
    }

    private void ShowSelected(MarketInstrument item)
    {
        _selected = item;
        SelectedNameText.Text = item.Name;
        SelectedEpicText.Text = item.Epic;
        BidText.Text = item.Bid?.ToString("0.####") ?? "n/a";
        OfferText.Text = item.Offer?.ToString("0.####") ?? "n/a";
        PriceText.Text = item.Price?.ToString("0.####") ?? "n/a";
        DetailStatusText.Text = $"{item.Group} | {item.Status}";
        Sma20Text.Text = item.Sma20?.ToString("0.####") ?? "n/a";
        Sma50Text.Text = item.Sma50?.ToString("0.####") ?? "n/a";
        TrendText.Text = item.TrendLabel;
        AlertPriceBox.Text = item.AlertPrice?.ToString("0.####") ?? "";
        AlertStatusText.Text = item.AlertPrice is null ? "No alert set for this instrument." : $"Alert set at {item.AlertPrice:0.####}.";
        TicketSummaryText.Text = item.Price is null
            ? "Load a price before preparing a ticket preview."
            : $"{item.Name}: last {item.Price:0.####}. Set quantity, stop, and take profit for planning.";
        DrawChart(DetailChart, item.Points);
    }

    private async void ChartModeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_instruments.Any()) return;
        await LoadHistoryForVisibleAsync(_groups.Where(group => group.IsExpanded).SelectMany(group => group.Instruments).Take(40));
        RedrawCharts();
    }

    private void DatasetBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_instruments.Any()) RebuildGroups();
    }

    private void MiniChart_Loaded(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is MarketInstrument item && sender is Canvas canvas)
        {
            item.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName is nameof(MarketInstrument.Price) or nameof(MarketInstrument.IntradayReturn))
                {
                    Dispatcher.Invoke(() => DrawChart(canvas, item.Points));
                }
            };
            DrawChart(canvas, item.Points);
        }
    }

    private void MiniChart_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is MarketInstrument item && sender is Canvas canvas)
        {
            DrawChart(canvas, item.Points);
        }
    }

    private void DetailChart_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_selected is not null) DrawChart(DetailChart, _selected.Points);
    }

    private void RedrawCharts()
    {
        if (_selected is not null) DrawChart(DetailChart, _selected.Points);
    }

    private void ToggleWatchlist_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if ((sender as FrameworkElement)?.DataContext is not MarketInstrument item) return;
        item.IsWatchlisted = !item.IsWatchlisted;
        if (item.IsWatchlisted && !_workspace.WatchlistEpics.Contains(item.Epic)) _workspace.WatchlistEpics.Add(item.Epic);
        if (!item.IsWatchlisted) _workspace.WatchlistEpics.Remove(item.Epic);
        _workspaceStore.Save(_workspace);
        UpdateStats();
        RebuildGroups();
    }

    private void QuickAlert_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if ((sender as FrameworkElement)?.DataContext is not MarketInstrument item) return;
        ShowSelected(item);
        AlertPriceBox.Text = item.Price?.ToString("0.####") ?? "";
        AlertStatusText.Text = item.Price is null ? "Load a price before saving an alert." : "Review the trigger price and press Save alert.";
    }

    private void SaveAlert_Click(object sender, RoutedEventArgs e)
    {
        if (_selected is null)
        {
            AlertStatusText.Text = "Select an instrument first.";
            return;
        }
        if (!decimal.TryParse(AlertPriceBox.Text, out var alertPrice))
        {
            AlertStatusText.Text = "Enter a valid price.";
            return;
        }
        _selected.AlertPrice = alertPrice;
        _workspace.Alerts[_selected.Epic] = alertPrice;
        _workspaceStore.Save(_workspace);
        AlertStatusText.Text = $"Alert saved at {alertPrice:0.####}.";
    }

    private void ClearAlert_Click(object sender, RoutedEventArgs e)
    {
        if (_selected is null) return;
        _selected.AlertPrice = null;
        _workspace.Alerts.Remove(_selected.Epic);
        _workspaceStore.Save(_workspace);
        AlertPriceBox.Text = "";
        AlertStatusText.Text = "Alert cleared.";
    }

    private void ApplyWorkspaceState(MarketInstrument item)
    {
        item.IsWatchlisted = _workspace.WatchlistEpics.Contains(item.Epic);
        if (_workspace.Alerts.TryGetValue(item.Epic, out var alertPrice)) item.AlertPrice = alertPrice;
    }

    private void CheckAlert(MarketInstrument item)
    {
        if (item.AlertPrice is null || item.Price is null) return;
        if (item.Price >= item.AlertPrice)
        {
            AlertStatusText.Text = $"{item.Name} reached alert {item.AlertPrice:0.####}. Last {item.Price:0.####}.";
            System.Media.SystemSounds.Exclamation.Play();
        }
    }

    private void UpdateStats()
    {
        MarketCountText.Text = _instruments.Count.ToString();
        WatchCountText.Text = _workspace.WatchlistEpics.Count.ToString();
        LiveCountText.Text = _instruments.Count(item => item.Status == "Live").ToString();
    }

    private void UpdateDiscoveryStrip()
    {
        var priced = _instruments.Where(item => item.ChangePercent is not null).ToList();
        TopTradedText.Text = string.Join("  ", _instruments.Take(5).Select(item => string.IsNullOrWhiteSpace(item.Symbol) ? item.Epic : item.Symbol));
        TopRiserText.Text = priced.OrderByDescending(item => item.ChangePercent).Select(item => $"{ShortName(item)} {item.ChangePercent:+0.00;-0.00;0.00}%").FirstOrDefault() ?? "n/a";
        TopFallerText.Text = priced.OrderBy(item => item.ChangePercent).Select(item => $"{ShortName(item)} {item.ChangePercent:+0.00;-0.00;0.00}%").FirstOrDefault() ?? "n/a";
        MostVolatileText.Text = priced
            .OrderByDescending(item => item.High is not null && item.Low is not null && item.Low != 0 ? (item.High - item.Low) / item.Low : 0)
            .Select(item => ShortName(item))
            .FirstOrDefault() ?? "n/a";
    }

    private static string ShortName(MarketInstrument item)
    {
        var value = string.IsNullOrWhiteSpace(item.Symbol) ? item.Name : item.Symbol;
        return value.Length <= 18 ? value : value[..18] + "...";
    }

    private void WorkspaceModeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded && (WorkspaceTitleText is null || DiscoverStrip is null || ResultText is null)) return;
        ApplyWorkspaceMode();
    }

    private void ApplyWorkspaceMode()
    {
        if (WorkspaceTitleText is null || DiscoverStrip is null || ResultText is null) return;
        var mode = (WorkspaceModeBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Trade";
        WorkspaceTitleText.Text = mode switch
        {
            "Discover" => "Discover | movers, volatility, categories",
            "Charts" => "Charts | select any market row",
            "Portfolio" => "Portfolio | local watchlist and live exposure",
            "Calendar" => "Calendar | market events placeholder",
            "Alerts" => "Alerts | local price triggers",
            _ => "Trade",
        };
        DiscoverStrip.Visibility = mode is "Trade" or "Discover" ? Visibility.Visible : Visibility.Collapsed;
        ResultText.Text = mode switch
        {
            "Portfolio" => $"{_workspace.WatchlistEpics.Count} watchlist markets saved locally.",
            "Calendar" => "Calendar API is not wired yet; use this pane for future event integration.",
            "Alerts" => $"{_workspace.Alerts.Count} local price alerts saved.",
            "Charts" => "Select a market row to inspect the chart panel.",
            "Discover" => "Discovery strip uses loaded markets: top traded, riser, faller, volatility.",
            _ => ResultText.Text,
        };
    }

    private static void DrawChart(Canvas canvas, IReadOnlyList<ChartPoint> points)
    {
        canvas.Children.Clear();
        if (points.Count < 2 || canvas.ActualWidth <= 0 || canvas.ActualHeight <= 0) return;

        var width = canvas.ActualWidth;
        var height = canvas.ActualHeight;
        var pad = 8.0;
        var values = points.Select(point => (double)point.Close).ToArray();
        var min = values.Min();
        var max = values.Max();
        if (Math.Abs(max - min) < 0.000001)
        {
            min -= 1;
            max += 1;
        }

        var polyline = new Polyline
        {
            Stroke = values[^1] >= values[0] ? new SolidColorBrush(Color.FromRgb(22, 122, 90)) : new SolidColorBrush(Color.FromRgb(177, 66, 66)),
            StrokeThickness = 2,
        };

        for (var index = 0; index < points.Count; index++)
        {
            var x = pad + index / (double)(points.Count - 1) * (width - pad * 2);
            var y = pad + (1 - (values[index] - min) / (max - min)) * (height - pad * 2);
            polyline.Points.Add(new Point(x, y));
        }

        canvas.Children.Add(new Line { X1 = pad, X2 = width - pad, Y1 = height - pad, Y2 = height - pad, Stroke = Brushes.LightGray, StrokeThickness = 1 });
        canvas.Children.Add(polyline);
    }
}

public sealed class InstrumentGroup : INotifyPropertyChanged
{
    private bool _isExpanded;

    public InstrumentGroup(string header, IReadOnlyList<MarketInstrument> instruments)
    {
        Header = $"{header} ({instruments.Count})";
        Instruments = new ObservableCollection<MarketInstrument>(instruments);
    }

    public string Header { get; }
    public ObservableCollection<MarketInstrument> Instruments { get; }

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value) return;
            _isExpanded = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpanded)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
