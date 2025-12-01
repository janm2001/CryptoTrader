using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CryptoTrader.Shared.Models;
using CryptoTrader.App.Services;
using System.IO;

namespace CryptoTrader.App.ViewModels;

public class DashboardViewModel : ViewModelBase
{
    private readonly ApiClient _api;
    private readonly LanguageService _lang;

    public DashboardViewModel()
    {
        _api = new ApiClient();
        _lang = LanguageService.Instance;
        CryptoPrices = new ObservableCollection<CryptoCurrency>();

        _lang.LanguageChanged += (s, e) => OnPropertyChanged(nameof(L));

        // Load data when created
        _ = LoadDataAsync();
    }

    public LanguageService L => _lang;
    public ObservableCollection<CryptoCurrency> CryptoPrices { get; }

    private decimal _totalPortfolioValue;
    public decimal TotalPortfolioValue
    {
        get => _totalPortfolioValue;
        set { SetProperty(ref _totalPortfolioValue, value); OnPropertyChanged(nameof(TotalPortfolioValueFormatted)); }
    }
    public string TotalPortfolioValueFormatted => $"${TotalPortfolioValue:N2}";

    private decimal _totalProfitLoss;
    public decimal TotalProfitLoss
    {
        get => _totalProfitLoss;
        set { SetProperty(ref _totalProfitLoss, value); OnPropertyChanged(nameof(TotalProfitLossFormatted)); OnPropertyChanged(nameof(IsProfitPositive)); }
    }
    public string TotalProfitLossFormatted => $"{(TotalProfitLoss >= 0 ? "+" : "")}{TotalProfitLoss:N2}";
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
            CryptoPrices.Clear();
            foreach (var price in prices.OrderBy(p => p.MarketCapRank).Take(10))
            {
                CryptoPrices.Add(price);
            }

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
