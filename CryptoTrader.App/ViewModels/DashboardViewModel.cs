using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CryptoTrader.Shared.Models;
using CryptoTrader.App.Services;
using System.IO;
using Avalonia.Threading;

namespace CryptoTrader.App.ViewModels;

public class DashboardViewModel : ViewModelBase, IDisposable
{
    private readonly ApiClient _api;
    private readonly LanguageService _lang;
    private readonly CurrencyService _currency;
    private readonly RealTimeService _realTime;
    private readonly SettingsService _settings;

    public DashboardViewModel()
    {
        _api = new ApiClient();
        _lang = LanguageService.Instance;
        _currency = CurrencyService.Instance;
        _realTime = RealTimeService.Instance;
        _settings = SettingsService.Instance;
        CryptoPrices = new ObservableCollection<CryptoCurrency>();

        // Use shared auth token
        var token = NavigationService.Instance.AuthToken;
        if (!string.IsNullOrEmpty(token))
        {
            _api.SetAuthToken(token);
        }

        _lang.LanguageChanged += (s, e) => OnPropertyChanged(nameof(L));
        _currency.CurrencyChanged += (s, e) => RefreshFormattedValues();

        // Subscribe to real-time price updates
        _realTime.OnPricesUpdated += OnRealTimePricesUpdated;
        _realTime.OnConnectionStatusChanged += OnConnectionStatusChanged;
        _realTime.OnError += OnRealTimeError;

        // Load data when created
        _ = LoadDataAsync();

        // Connect to real-time service if auto-connect is enabled
        if (_settings.AutoConnect)
        {
            _ = ConnectRealTimeAsync();
        }
    }

    public LanguageService L => _lang;
    public CurrencyService Currency => _currency;
    public ObservableCollection<CryptoCurrency> CryptoPrices { get; }

    private CryptoCurrency? _selectedCrypto;
    public CryptoCurrency? SelectedCrypto
    {
        get => _selectedCrypto;
        set => SetProperty(ref _selectedCrypto, value);
    }

    private decimal _totalPortfolioValue;
    public decimal TotalPortfolioValue
    {
        get => _totalPortfolioValue;
        set { SetProperty(ref _totalPortfolioValue, value); OnPropertyChanged(nameof(TotalPortfolioValueFormatted)); }
    }
    public string TotalPortfolioValueFormatted => _currency.Format(TotalPortfolioValue);

    private decimal _totalProfitLoss;
    public decimal TotalProfitLoss
    {
        get => _totalProfitLoss;
        set { SetProperty(ref _totalProfitLoss, value); OnPropertyChanged(nameof(TotalProfitLossFormatted)); OnPropertyChanged(nameof(IsProfitPositive)); }
    }
    public string TotalProfitLossFormatted => _currency.FormatProfitLoss(TotalProfitLoss);
    public bool IsProfitPositive => TotalProfitLoss >= 0;

    private decimal _totalProfitLossPercentage;
    public decimal TotalProfitLossPercentage
    {
        get => _totalProfitLossPercentage;
        set { SetProperty(ref _totalProfitLossPercentage, value); OnPropertyChanged(nameof(TotalProfitLossPercentageFormatted)); }
    }
    public string TotalProfitLossPercentageFormatted => $"{(TotalProfitLossPercentage >= 0 ? "+" : "")}{TotalProfitLossPercentage:N2}%";

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    private string _statusMessage = "";
    public string StatusMessage
    {
        get => _statusMessage;
        set { SetProperty(ref _statusMessage, value); OnPropertyChanged(nameof(HasStatusMessage)); }
    }
    public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);

    private bool _isConnected;
    public bool IsConnected
    {
        get => _isConnected;
        set => SetProperty(ref _isConnected, value);
    }

    public async Task LoadDataAsync()
    {
        IsLoading = true;
        StatusMessage = "";

        try
        {
            var summary = await _api.GetPortfolioSummaryAsync();
            if (summary != null)
            {
                TotalPortfolioValue = summary.TotalValue;
                TotalProfitLoss = summary.TotalProfitLoss;
                TotalProfitLossPercentage = summary.TotalProfitLossPercentage;
            }

            var prices = await _api.GetCachedPricesAsync();
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                CryptoPrices.Clear();
                foreach (var price in prices.OrderBy(p => p.MarketCapRank).Take(10))
                {
                    CryptoPrices.Add(price);
                }
            });

            IsConnected = true;
        }
        catch
        {
            IsConnected = false;
            StatusMessage = _lang["ConnectionError"];
        }

        IsLoading = false;
    }

    public async Task ExportPricesToExcelAsync()
    {
        IsLoading = true;
        StatusMessage = "";

        var data = await _api.ExportPricesToExcelAsync();
        if (data != null)
        {
            await SaveExportFileAsync(data, "CryptoPrices", ".xlsx");
            StatusMessage = _lang["ExportSuccess"];
        }
        else
        {
            StatusMessage = _lang["Error"];
        }

        IsLoading = false;
    }

    private void RefreshFormattedValues()
    {
        OnPropertyChanged(nameof(TotalPortfolioValueFormatted));
        OnPropertyChanged(nameof(TotalProfitLossFormatted));
    }

    private async Task ConnectRealTimeAsync()
    {
        try
        {
            // Try TCP first for bi-directional communication
            await _realTime.ConnectTcpAsync(
                _settings.ServerAddress,
                _settings.ServerTcpPort,
                NavigationService.Instance.AuthToken
            );

            // Also connect UDP for broadcast updates
            await _realTime.ConnectUdpAsync(
                _settings.ServerAddress,
                _settings.ServerUdpPort
            );

            RealTimeStatus = _lang["Connected"];
            IsRealTimeConnected = true;
        }
        catch
        {
            RealTimeStatus = _lang["Disconnected"];
            IsRealTimeConnected = false;
        }
    }

    private void OnRealTimePricesUpdated(object? sender, List<CryptoCurrency> prices)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            foreach (var newPrice in prices)
            {
                var existing = CryptoPrices.FirstOrDefault(p => p.CoinId == newPrice.CoinId);
                if (existing != null)
                {
                    // Update existing price with animation potential
                    var index = CryptoPrices.IndexOf(existing);
                    existing.CurrentPrice = newPrice.CurrentPrice;
                    existing.PriceChangePercentage24h = newPrice.PriceChangePercentage24h;
                    existing.MarketCap = newPrice.MarketCap;
                    existing.TotalVolume = newPrice.TotalVolume;
                    
                    // Trigger UI update
                    CryptoPrices[index] = existing;
                }
            }

            // Update last update time
            LastUpdateTime = DateTime.Now.ToString("HH:mm:ss");
        });
    }

    private void OnConnectionStatusChanged(object? sender, string status)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            RealTimeStatus = status;
            IsRealTimeConnected = _realTime.IsConnected;
        });
    }

    private void OnRealTimeError(object? sender, string error)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            StatusMessage = error;
            IsRealTimeConnected = false;
        });
    }

    private string _realTimeStatus = "Disconnected";
    public string RealTimeStatus
    {
        get => _realTimeStatus;
        set => SetProperty(ref _realTimeStatus, value);
    }

    private bool _isRealTimeConnected;
    public bool IsRealTimeConnected
    {
        get => _isRealTimeConnected;
        set => SetProperty(ref _isRealTimeConnected, value);
    }

    private string _lastUpdateTime = "-";
    public string LastUpdateTime
    {
        get => _lastUpdateTime;
        set => SetProperty(ref _lastUpdateTime, value);
    }

    public void Dispose()
    {
        _realTime.OnPricesUpdated -= OnRealTimePricesUpdated;
        _realTime.OnConnectionStatusChanged -= OnConnectionStatusChanged;
        _realTime.OnError -= OnRealTimeError;
    }

    private async Task SaveExportFileAsync(byte[] data, string baseName, string extension)
    {
        var downloadsFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        downloadsFolder = Path.Combine(downloadsFolder, "Downloads");

        if (!Directory.Exists(downloadsFolder))
        {
            downloadsFolder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        }

        var fileName = $"{baseName}_{DateTime.Now:yyyyMMdd_HHmmss}{extension}";
        var filePath = Path.Combine(downloadsFolder, fileName);

        await File.WriteAllBytesAsync(filePath, data);
    }
}
