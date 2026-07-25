using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CAPETF.Desktop;

public partial class CapComTerminalWindow : Window
{
    private readonly CredentialStore _credentialStore = new();
    private readonly CapitalApiClient _api = new();
    private readonly List<MarketInstrument> _instruments = [];
    private readonly ObservableCollection<TerminalComponentRow> _components = [];
    private SyntheticBasket? _basket;

    public CapComTerminalWindow()
    {
        InitializeComponent();
        ComponentsList.ItemsSource = _components;
        LoadSavedCredentials();
        InitializeChartHost();
        SizeChanged += (_, _) => RenderNativeCandles();
    }

    private void LoadSavedCredentials()
    {
        var saved = _credentialStore.Load();
        ConnectionText.Text = saved is null ? "no saved Capital.com keys" : "saved keys loaded";
    }

    private async void Connect_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var saved = _credentialStore.Load();
            if (saved is null)
            {
                MessageBox.Show("Open the existing CAPETF dashboard once and save Capital.com keys locally.", "cap.com Terminal", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ConnectionText.Text = "connecting...";
            await _api.LoginAsync(saved);
            ConnectionText.Text = "connected";
            StatusText.Text = "Connected to Capital.com.";
        }
        catch (Exception ex)
        {
            ConnectionText.Text = "connection failed";
            StatusText.Text = ex.Message;
        }
    }

    private async void LoadStocks_Click(object sender, RoutedEventArgs e)
    {
        await EnsureConnectedAsync();
        StatusText.Text = "Loading Capital.com stocks...";
        _instruments.Clear();
        var markets = await _api.SearchMarketsAsync(SearchBox.Text.Trim());
        foreach (var item in markets.Where(CapitalInstrumentTypes.IsStock).Take(240))
        {
            _instruments.Add(item);
        }

        RebuildBlocks();
        StatusText.Text = $"{_instruments.Count} stocks loaded.";
    }

    private async void BuildSynthetic_Click(object sender, RoutedEventArgs e)
    {
        await EnsureConnectedAsync();
        if (_instruments.Count == 0)
        {
            StatusText.Text = "Load stocks before building a synthetic symbol.";
            return;
        }

        var block = SelectedBlock();
        var candidates = SyntheticTerminalSelector.HistoryLoadCandidates(block, _instruments, limit: 80);
        var resolution = SelectedResolution();
        var requestResolution = RequestResolution(resolution);
        var periodsPerYear = PeriodsPerYear(resolution);
        var maxCandles = MaxCandles(resolution);
        var minCandles = MinimumCandles(resolution);
        var candles = new Dictionary<string, IReadOnlyList<OhlcPoint>>();
        var checkedCount = 0;

        StatusText.Text = $"Scanning {candidates.Count} stocks for {block}...";
        foreach (var item in candidates)
        {
            checkedCount++;
            try
            {
                var rows = await _api.GetOhlcPricesAsync(item.Epic, requestResolution, maxCandles);
                rows = TransformCandles(rows, resolution);
                if (rows.Count >= minCandles)
                {
                    candles[item.Epic] = rows;
                    StatusText.Text = $"History loaded: {candles.Count} usable of {checkedCount} checked.";
                }
            }
            catch
            {
                item.Status = "History n/a";
            }
        }

        _basket = SyntheticTerminalSelector.SelectBest(block, candidates, candles, periodsPerYear);
        if (_basket is null)
        {
            StatusText.Text = $"No synthetic basket could be built. {candles.Count} symbols had usable history.";
            return;
        }

        RenderSyntheticChart(_basket);
        StatusText.Text = $"{_basket.Symbol}: {_basket.Components.Count} legs, similarity {_basket.SimilarityScore:0.##}, average volatility {_basket.AverageVolatilityPct:0.##}%.";
    }

    private async void StreamSynthetic_Click(object sender, RoutedEventArgs e)
    {
        await EnsureConnectedAsync();
        StatusText.Text = _basket is null
            ? "Build a synthetic symbol before streaming."
            : "Streaming hook is ready; live order placement is still disabled.";
    }

    private void BuyPreview_Click(object sender, RoutedEventArgs e) => PreviewSyntheticOrder("BUY");

    private void SellPreview_Click(object sender, RoutedEventArgs e) => PreviewSyntheticOrder("SELL");

    private void CandleType_SelectionChanged(object sender, SelectionChangedEventArgs e) => RenderNativeCandles();

    private async Task EnsureConnectedAsync()
    {
        if (_api.Session is not null) return;
        var saved = _credentialStore.Load();
        if (saved is null) throw new InvalidOperationException("No saved Capital.com keys found.");
        await _api.LoginAsync(saved);
        ConnectionText.Text = "connected";
    }

    private void RebuildBlocks()
    {
        var blocks = _instruments
            .Where(CapitalInstrumentTypes.IsStock)
            .GroupBy(item => item.Group)
            .OrderByDescending(group => group.Count())
            .Select(group => $"{group.Key}")
            .ToList();

        BlockBox.ItemsSource = blocks;
        if (blocks.Count > 0) BlockBox.SelectedIndex = 0;
    }

    private string SelectedBlock() =>
        BlockBox.SelectedItem?.ToString() ?? _instruments.FirstOrDefault()?.Group ?? "US / USD / Other";

    private string SelectedResolution() =>
        (ResolutionBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Weekly";

    private static string RequestResolution(string resolution) =>
        resolution is "2H" or "6H" ? "HOUR" :
        resolution == "4H" ? "HOUR_4" :
        resolution == "Daily" ? "DAY" :
        "WEEK";

    private static int MaxCandles(string resolution) =>
        resolution switch
        {
            "2H" => 540,
            "4H" => 540,
            "6H" => 540,
            "Daily" => 900,
            _ => 260,
        };

    private static int MinimumCandles(string resolution) => resolution is "2H" or "4H" or "6H" ? 60 : 120;

    private static int PeriodsPerYear(string resolution) =>
        resolution switch
        {
            "2H" => 252 * 4,
            "4H" => 252 * 2,
            "6H" => 252,
            "Daily" => 252,
            _ => 52,
        };

    private static IReadOnlyList<OhlcPoint> TransformCandles(IReadOnlyList<OhlcPoint> source, string resolution) =>
        resolution switch
        {
            "2H" => AggregateCandles(source, 2),
            "6H" => AggregateCandles(source, 6),
            _ => source,
        };

    private static IReadOnlyList<OhlcPoint> AggregateCandles(IReadOnlyList<OhlcPoint> source, int bucketSize)
    {
        var ordered = source.OrderBy(point => point.Time).ToList();
        var result = new List<OhlcPoint>();
        for (var index = 0; index + bucketSize <= ordered.Count; index += bucketSize)
        {
            var bucket = ordered.Skip(index).Take(bucketSize).ToList();
            result.Add(new OhlcPoint(
                bucket[^1].Time,
                bucket[0].Open,
                bucket.Max(point => point.High),
                bucket.Min(point => point.Low),
                bucket[^1].Close));
        }

        return result;
    }

    private void InitializeChartHost()
    {
        StockSharpChartHost.Children.Clear();
        StatusText.Text = "Native chart host ready.";
    }

    private void RenderSyntheticChart(SyntheticBasket basket)
    {
        var payload = SyntheticTerminalChartPayload.Build(basket);
        SymbolText.Text = $"{payload.Symbol}  {payload.Block}";
        ChartMetaText.Text = $"{payload.CurrencyLabel} | last {basket.BasketPrice:0.####}";
        SyntheticFormulaText.Text = string.Join(Environment.NewLine + "+ ", basket.Components.Select(component =>
            $"{component.Weight / 100m:0.0000} * {component.Instrument.Epic}"));

        _components.Clear();
        foreach (var component in payload.Components) _components.Add(component);

        RenderNativeCandles();
    }

    private void RenderNativeCandles()
    {
        if (NativeCandleCanvas is null) return;

        NativeCandleCanvas.Children.Clear();
        if (_basket is null || _basket.Candles.Count < 2 || NativeCandleCanvas.ActualWidth < 80 || NativeCandleCanvas.ActualHeight < 80) return;

        var candles = DisplayCandles(_basket.Candles);
        var values = candles.SelectMany(point => new[] { point.High, point.Low }).Select(value => (double)value).ToList();
        var min = values.Min();
        var max = values.Max();
        var range = Math.Max(0.0001, max - min);
        var width = NativeCandleCanvas.ActualWidth;
        var height = NativeCandleCanvas.ActualHeight;
        var chartWidth = Math.Max(100, width - 64);
        var chartHeight = Math.Max(80, height - 30);
        var candleWidth = Math.Max(3, chartWidth / candles.Count * 0.58);

        double X(int index) => index * chartWidth / Math.Max(1, candles.Count - 1);
        double Y(decimal value) => chartHeight - (((double)value - min) / range * (chartHeight - 12)) + 4;

        DrawPriceAxisLabels(min, max, chartWidth, chartHeight);
        DrawDateAxisLabels(candles, chartWidth, chartHeight);

        for (var index = 0; index < candles.Count; index++)
        {
            var candle = candles[index];
            var x = X(index);
            var up = candle.Close >= candle.Open;
            var brush = up ? Brushes.LimeGreen : Brushes.Crimson;
            var wick = new Line
            {
                X1 = x,
                X2 = x,
                Y1 = Y(candle.High),
                Y2 = Y(candle.Low),
                Stroke = brush,
                StrokeThickness = 1
            };
            NativeCandleCanvas.Children.Add(wick);

            var openY = Y(candle.Open);
            var closeY = Y(candle.Close);
            var body = new Rectangle
            {
                Width = candleWidth,
                Height = Math.Max(2, Math.Abs(closeY - openY)),
                Fill = brush,
                Stroke = brush,
                StrokeThickness = 1
            };
            Canvas.SetLeft(body, x - candleWidth / 2);
            Canvas.SetTop(body, Math.Min(openY, closeY));
            NativeCandleCanvas.Children.Add(body);
        }
    }

    private List<OhlcPoint> DisplayCandles(IReadOnlyList<OhlcPoint> source)
    {
        var ordered = source.OrderBy(point => point.Time).ToList();
        if (SelectedCandleType() != "Heikin Ashi" || ordered.Count == 0) return ordered;

        var result = new List<OhlcPoint>(ordered.Count);
        decimal previousOpen = (ordered[0].Open + ordered[0].Close) / 2m;
        decimal previousClose = (ordered[0].Open + ordered[0].High + ordered[0].Low + ordered[0].Close) / 4m;
        foreach (var point in ordered)
        {
            var close = (point.Open + point.High + point.Low + point.Close) / 4m;
            var open = (previousOpen + previousClose) / 2m;
            var high = Math.Max(point.High, Math.Max(open, close));
            var low = Math.Min(point.Low, Math.Min(open, close));
            result.Add(new OhlcPoint(point.Time, open, high, low, close));
            previousOpen = open;
            previousClose = close;
        }

        return result;
    }

    private string SelectedCandleType() =>
        (CandleTypeBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Candles";

    private void DrawPriceAxisLabels(double min, double max, double chartWidth, double chartHeight)
    {
        var range = Math.Max(0.0001, max - min);
        for (var index = 0; index <= 4; index++)
        {
            var value = min + range * (4 - index) / 4;
            var y = 4 + index * (chartHeight - 12) / 4;
            NativeCandleCanvas.Children.Add(new Line
            {
                X1 = 0,
                X2 = chartWidth,
                Y1 = y,
                Y2 = y,
                Stroke = new SolidColorBrush(Color.FromArgb(64, 148, 163, 184)),
                StrokeThickness = 1
            });

            var label = new TextBlock
            {
                Text = value.ToString("0.####"),
                Foreground = new SolidColorBrush(Color.FromRgb(147, 164, 186)),
                FontSize = 11
            };
            Canvas.SetLeft(label, chartWidth + 8);
            Canvas.SetTop(label, y - 8);
            NativeCandleCanvas.Children.Add(label);
        }
    }

    private void DrawDateAxisLabels(IReadOnlyList<OhlcPoint> candles, double chartWidth, double chartHeight)
    {
        foreach (var index in new[] { 0, candles.Count / 2, candles.Count - 1 }.Distinct())
        {
            var label = new TextBlock
            {
                Text = candles[index].Time.ToString("yyyy-MM-dd"),
                Foreground = new SolidColorBrush(Color.FromRgb(147, 164, 186)),
                FontSize = 11
            };
            var x = index * chartWidth / Math.Max(1, candles.Count - 1);
            Canvas.SetLeft(label, Math.Max(0, Math.Min(chartWidth - 72, x - 34)));
            Canvas.SetTop(label, chartHeight + 8);
            NativeCandleCanvas.Children.Add(label);
        }
    }

    private void PreviewSyntheticOrder(string side)
    {
        if (_basket is null)
        {
            OrderPreviewText.Text = "Build a synthetic symbol first.";
            return;
        }

        _ = decimal.TryParse(QuantityBox.Text, out var quantity);
        if (quantity <= 0) quantity = 1m;
        OrderPreviewText.Text = string.Join(Environment.NewLine, _basket.Components.Select(component =>
            $"{side} {quantity * component.Weight / 100m:0.####} x {component.Instrument.Epic}"));
    }
}
