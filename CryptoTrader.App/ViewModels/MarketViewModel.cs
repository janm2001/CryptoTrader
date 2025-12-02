using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CryptoTrader.Shared.Models;
using CryptoTrader.App.Services;
using System.IO;
using Avalonia.Threading;

namespace CryptoTrader.App.ViewModels;

public class MarketViewModel : ViewModelBase
{
    private readonly ApiClient _api;
    private readonly LanguageService _lang;

    public MarketViewModel()
    {
        _api = new ApiClient();
        _lang = LanguageService.Instance;
        CryptoPrices = new ObservableCollection<CryptoCurrency>();

        // Use shared auth token
        var token = NavigationService.Instance.AuthToken;
        if (!string.IsNullOrEmpty(token))
        {
            _api.SetAuthToken(token);
        }

        _lang.LanguageChanged += (s, e) => OnPropertyChanged(nameof(L));

        _ = LoadDataAsync();
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

    public async Task ExportPricesAsync()
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
