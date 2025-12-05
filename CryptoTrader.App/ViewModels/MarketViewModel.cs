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

public class MarketViewModel : ViewModelBase, IDisposable
{
    private readonly ApiClient _api;
    private readonly LanguageService _lang;
    private readonly RealTimeService _realTime;
    private readonly SettingsService _settings;
    private List<CryptoCurrency> _allPrices = new();

    public MarketViewModel()
    {
        _api = new ApiClient();
        _lang = LanguageService.Instance;
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

        // Subscribe to real-time price updates
        _realTime.OnPricesUpdated += OnRealTimePricesUpdated;
        _realTime.OnConnectionStatusChanged += OnConnectionStatusChanged;

        _ = LoadDataAsync();

        // Connect to real-time if not already connected
        if (!_realTime.IsConnected && _settings.AutoConnect)
        {
            _ = ConnectRealTimeAsync();
        }
        else
        {
            IsRealTimeConnected = _realTime.IsConnected;
        }
    }

    public LanguageService L => _lang;
    public ObservableCollection<CryptoCurrency> CryptoPrices { get; }

    private string _searchQuery = "";
    public string SearchQuery
    {
        get => _searchQuery;
        set => SetProperty(ref _searchQuery, value);
    }

    private CryptoCurrency? _selectedCrypto;
    public CryptoCurrency? SelectedCrypto
    {
        get => _selectedCrypto;
        set => SetProperty(ref _selectedCrypto, value);
    }

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
            var prices = await _api.GetCachedPricesAsync();
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                CryptoPrices.Clear();
                foreach (var price in prices.OrderBy(p => p.MarketCapRank))
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

    public async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            await LoadDataAsync();
            return;
        }

        IsLoading = true;

        var results = await _api.SearchCryptosAsync(SearchQuery);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            CryptoPrices.Clear();
            foreach (var crypto in results)
            {
                CryptoPrices.Add(crypto);
            }
        });

        IsLoading = false;
    }

    public async Task ExportPricesAsync(string format = "excel")
    {
        IsLoading = true;
        StatusMessage = "";

        byte[]? data = null;
        string extension = ".xlsx";

        try
        {
            switch (format.ToLower())
            {
                case "excel":
                    data = await _api.ExportPricesToExcelAsync();
                    extension = ".xlsx";
                    break;
                case "xml":
                    data = await _api.ExportPricesToXmlAsync();
                    extension = ".xml";
                    break;
                case "pdf":
                    data = await _api.ExportMarketToPdfAsync();
                    extension = ".pdf";
                    break;
                case "binary":
                    data = await _api.ExportPricesToBinaryAsync();
                    extension = ".bin";
                    break;
            }

            if (data != null)
            {
                await SaveExportFileAsync(data, $"CryptoPrices_{format}", extension);
                StatusMessage = _lang["ExportSuccess"];
            }
            else
            {
                StatusMessage = _lang["Error"];
            }
        }
        catch
        {
            StatusMessage = _lang["Error"];
        }

        IsLoading = false;
    }

    private async Task ConnectRealTimeAsync()
    {
        try
        {
            await _realTime.ConnectTcpAsync(
                _settings.ServerAddress,
                _settings.ServerTcpPort,
                NavigationService.Instance.AuthToken
            );

            await _realTime.ConnectUdpAsync(
                _settings.ServerAddress,
                _settings.ServerUdpPort
            );

            IsRealTimeConnected = true;
            RealTimeStatus = _lang["Connected"];
        }
        catch
        {
            IsRealTimeConnected = false;
            RealTimeStatus = _lang["Disconnected"];
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
                    var index = CryptoPrices.IndexOf(existing);
                    existing.CurrentPrice = newPrice.CurrentPrice;
                    existing.PriceChangePercentage24h = newPrice.PriceChangePercentage24h;
                    existing.MarketCap = newPrice.MarketCap;
                    existing.TotalVolume = newPrice.TotalVolume;
                    CryptoPrices[index] = existing;
                }
            }
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

    private bool _isRealTimeConnected;
    public bool IsRealTimeConnected
    {
        get => _isRealTimeConnected;
        set => SetProperty(ref _isRealTimeConnected, value);
    }

    private string _realTimeStatus = "Disconnected";
    public string RealTimeStatus
    {
        get => _realTimeStatus;
        set => SetProperty(ref _realTimeStatus, value);
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
