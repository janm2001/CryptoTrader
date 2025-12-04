using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CryptoTrader.App.Models;
using CryptoTrader.App.Services;
using CryptoTrader.Shared.Models;

namespace CryptoTrader.App.Views;

public partial class CryptoDetailWindow : Window
{
    private readonly ApiClient _api;
    private readonly CurrencyService _currency;
    private readonly CryptoCurrency _crypto;
    private List<PricePoint> _priceHistory = new();

    public CryptoDetailWindow()
    {
        InitializeComponent();
        _api = new ApiClient();
        _currency = CurrencyService.Instance;
        _crypto = new CryptoCurrency();
    }

    public CryptoDetailWindow(CryptoCurrency crypto)
    {
        InitializeComponent();
        _api = new ApiClient();
        _currency = CurrencyService.Instance;
        _crypto = crypto;

        // Use shared auth token
        var token = NavigationService.Instance.AuthToken;
        if (!string.IsNullOrEmpty(token))
        {
            _api.SetAuthToken(token);
        }

        Loaded += async (s, e) => await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            // Set basic info
            Title = $"{_crypto.Name} ({_crypto.Symbol.ToUpper()})";
            CoinNameText.Text = _crypto.Name;
            CoinSymbolText.Text = _crypto.Symbol.ToUpper();
            CurrentPriceText.Text = _currency.Format(_crypto.CurrentPrice);
            RankText.Text = $"#{_crypto.MarketCapRank}";

            // Price change
            var priceChange = _crypto.PriceChange24h;
            var priceChangePercent = _crypto.PriceChangePercentage24h;
            var isPositive = priceChangePercent >= 0;
            var changeColor = new SolidColorBrush(isPositive ? Color.Parse("#90EE90") : Color.Parse("#FF6B6B"));

            PriceChangeText.Text = $"{(isPositive ? "+" : "")}{_currency.Format(priceChange)}";
            PriceChangeText.Foreground = changeColor;
            PriceChangePercentText.Text = $"({(isPositive ? "+" : "")}{priceChangePercent:N2}%)";
            PriceChangePercentText.Foreground = changeColor;

            // Stats
            MarketCapText.Text = FormatLargeNumber(_crypto.MarketCap);
            VolumeText.Text = FormatLargeNumber(_crypto.TotalVolume);
            HighText.Text = _currency.Format(_crypto.High24h);
            LowText.Text = _currency.Format(_crypto.Low24h);

            // Load coin image
            await LoadCoinImageAsync();

            // Try to load real price history from database, fall back to simulated if none exists
            await LoadPriceHistoryAsync();

            // Draw chart
            DrawChart();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading crypto details: {ex.Message}");
        }
    }

    private async Task LoadPriceHistoryAsync()
    {
        try
        {
            // Try to get real price history from the database
            var history = await _api.GetPriceHistoryAsync(_crypto.CoinId, 30);

            if (history.Count > 0)
            {
                // Use real data
                _priceHistory = history.Select(h => new PricePoint
                {
                    Time = h.Timestamp,
                    Price = h.Price
                }).OrderBy(p => p.Time).ToList();

                Console.WriteLine($"Loaded {_priceHistory.Count} real price history points for {_crypto.CoinId}");
            }
            else
            {
                // No historical data yet, generate simulated data
                Console.WriteLine($"No price history found for {_crypto.CoinId}, using simulated data");
                GenerateSimulatedPriceHistory();
            }

            // Update the list display
            UpdatePriceHistoryDisplay();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading price history: {ex.Message}, using simulated data");
            GenerateSimulatedPriceHistory();
            UpdatePriceHistoryDisplay();
        }
    }

    private void UpdatePriceHistoryDisplay()
    {
        var displayItems = _priceHistory.Select(p => new PriceDisplayItem
        {
            TimeText = p.Time.ToString("MM/dd HH:mm"),
            PriceText = _currency.Format(p.Price)
        }).Reverse().ToList();

        PriceHistoryList.ItemsSource = displayItems;
    }

    private async Task LoadCoinImageAsync()
    {
        try
        {
            if (!string.IsNullOrEmpty(_crypto.ImageUrl))
            {
                using var httpClient = new System.Net.Http.HttpClient();
                var imageBytes = await httpClient.GetByteArrayAsync(_crypto.ImageUrl);
                using var stream = new System.IO.MemoryStream(imageBytes);
                CoinImage.Source = new Bitmap(stream);
            }
        }
        catch
        {
            // Failed to load image, leave it empty
        }
    }

    private void GenerateSimulatedPriceHistory()
    {
        _priceHistory.Clear();
        var random = new Random();
        var currentPrice = _crypto.CurrentPrice;
        var volatility = currentPrice * 0.02m; // 2% volatility

        // Generate 24 hours of data points
        for (int i = 24; i >= 0; i--)
        {
            var time = DateTime.Now.AddHours(-i);
            var variation = (decimal)(random.NextDouble() - 0.5) * 2 * volatility;
            var price = currentPrice + variation;

            // Make sure price trends towards current price at the end
            if (i <= 2)
            {
                price = currentPrice + (variation * (i / 3m));
            }

            _priceHistory.Add(new PricePoint
            {
                Time = time,
                Price = Math.Max(price, currentPrice * 0.9m) // Don't go below 90% of current
            });
        }

        // Sort by time
        _priceHistory = _priceHistory.OrderBy(p => p.Time).ToList();
    }

    private void DrawChart()
    {
        ChartCanvas.Children.Clear();

        if (_priceHistory.Count < 2) return;

        // Wait for layout
        ChartCanvas.Loaded += (s, e) => DrawChartInternal();
        if (ChartCanvas.Bounds.Width > 0)
        {
            DrawChartInternal();
        }
    }

    private void DrawChartInternal()
    {
        ChartCanvas.Children.Clear();

        var width = ChartCanvas.Bounds.Width;
        var height = ChartCanvas.Bounds.Height;

        if (width <= 0 || height <= 0 || _priceHistory.Count < 2) return;

        var padding = 20;
        var chartWidth = width - padding * 2;
        var chartHeight = height - padding * 2;

        var minPrice = _priceHistory.Min(p => p.Price);
        var maxPrice = _priceHistory.Max(p => p.Price);
        var priceRange = maxPrice - minPrice;
        if (priceRange == 0) priceRange = 1;

        // Determine if overall trend is positive
        var isPositive = _priceHistory.Last().Price >= _priceHistory.First().Price;
        var lineColor = isPositive ? Color.Parse("#4ECB71") : Color.Parse("#FF6B6B");
        var fillColor = isPositive ? Color.Parse("#1A4ECB71") : Color.Parse("#1AFF6B6B");

        // Create points for the line
        var points = new List<Point>();
        for (int i = 0; i < _priceHistory.Count; i++)
        {
            var x = padding + (i * chartWidth / (_priceHistory.Count - 1));
            var y = padding + (double)((maxPrice - _priceHistory[i].Price) / priceRange) * chartHeight;
            points.Add(new Point(x, y));
        }

        // Draw fill area
        var fillPoints = new List<Point>(points);
        fillPoints.Add(new Point(padding + chartWidth, padding + chartHeight));
        fillPoints.Add(new Point(padding, padding + chartHeight));

        var fillPolygon = new Polygon
        {
            Points = fillPoints,
            Fill = new SolidColorBrush(fillColor)
        };
        ChartCanvas.Children.Add(fillPolygon);

        // Draw line
        var polyline = new Polyline
        {
            Points = points,
            Stroke = new SolidColorBrush(lineColor),
            StrokeThickness = 2
        };
        ChartCanvas.Children.Add(polyline);

        // Draw grid lines
        for (int i = 0; i <= 4; i++)
        {
            var y = padding + (i * chartHeight / 4);
            var gridLine = new Line
            {
                StartPoint = new Point(padding, y),
                EndPoint = new Point(padding + chartWidth, y),
                Stroke = new SolidColorBrush(Color.Parse("#2d2d4a")),
                StrokeThickness = 1
            };
            ChartCanvas.Children.Add(gridLine);

            // Price label
            var price = maxPrice - (priceRange * i / 4);
            var priceText = new TextBlock
            {
                Text = _currency.Format(price),
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.Parse("#808090"))
            };
            Canvas.SetLeft(priceText, 5);
            Canvas.SetTop(priceText, y - 7);
            ChartCanvas.Children.Add(priceText);
        }

        // Draw time labels
        var timeLabels = new[] { 0, _priceHistory.Count / 2, _priceHistory.Count - 1 };
        foreach (var idx in timeLabels)
        {
            if (idx < _priceHistory.Count)
            {
                var x = padding + (idx * chartWidth / (_priceHistory.Count - 1));
                var timeText = new TextBlock
                {
                    Text = _priceHistory[idx].Time.ToString("HH:mm"),
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.Parse("#808090"))
                };
                Canvas.SetLeft(timeText, x - 15);
                Canvas.SetTop(timeText, height - 15);
                ChartCanvas.Children.Add(timeText);
            }
        }

        // Draw data points
        foreach (var point in points)
        {
            var dot = new Ellipse
            {
                Width = 6,
                Height = 6,
                Fill = new SolidColorBrush(lineColor)
            };
            Canvas.SetLeft(dot, point.X - 3);
            Canvas.SetTop(dot, point.Y - 3);
            ChartCanvas.Children.Add(dot);
        }
    }

    private string FormatLargeNumber(decimal value)
    {
        if (value >= 1_000_000_000_000)
            return $"${value / 1_000_000_000_000:N2}T";
        if (value >= 1_000_000_000)
            return $"${value / 1_000_000_000:N2}B";
        if (value >= 1_000_000)
            return $"${value / 1_000_000:N2}M";
        if (value >= 1_000)
            return $"${value / 1_000:N2}K";
        return $"${value:N2}";
    }

    private async void OnBuyClick(object? sender, RoutedEventArgs e)
    {
        var buyWindow = new BuyWindow(cryptoCurrency: _crypto);
        await buyWindow.ShowDialog(this);
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
