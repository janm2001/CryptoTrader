using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CryptoTrader.Shared.Models;
using CryptoTrader.App.Services;
using System.IO;
using Avalonia.Media;
using Avalonia.Threading;

namespace CryptoTrader.App.ViewModels;

public class PortfolioViewModel : ViewModelBase
{
    private readonly ApiClient _api;
    private readonly LanguageService _lang;
    private readonly CurrencyService _currency;

    public PortfolioViewModel()
    {
        _api = new ApiClient();
        _lang = LanguageService.Instance;
        _currency = CurrencyService.Instance;
        Holdings = new ObservableCollection<CryptoHolding>();

        // Use shared auth token
        var token = NavigationService.Instance.AuthToken;
        if (!string.IsNullOrEmpty(token))
        {
            _api.SetAuthToken(token);
        }

        _lang.LanguageChanged += (s, e) => OnPropertyChanged(nameof(L));
        _currency.CurrencyChanged += (s, e) => RefreshFormattedValues();

        _ = LoadDataAsync();
    }

    public LanguageService L => _lang;
    public CurrencyService Currency => _currency;
    public ObservableCollection<CryptoHolding> Holdings { get; }

    private decimal _balance;
    public decimal Balance
    {
        get => _balance;
        set { SetProperty(ref _balance, value); OnPropertyChanged(nameof(BalanceFormatted)); OnPropertyChanged(nameof(TotalAssetsFormatted)); }
    }
    public string BalanceFormatted => _currency.Format(Balance);

    private decimal _totalPortfolioValue;
    public decimal TotalPortfolioValue
    {
        get => _totalPortfolioValue;
        set { SetProperty(ref _totalPortfolioValue, value); OnPropertyChanged(nameof(TotalPortfolioValueFormatted)); OnPropertyChanged(nameof(TotalAssetsFormatted)); }
    }
    public string TotalPortfolioValueFormatted => _currency.Format(TotalPortfolioValue);

    private decimal _totalProfitLoss;
    public decimal TotalProfitLoss
    {
        get => _totalProfitLoss;
        set { SetProperty(ref _totalProfitLoss, value); OnPropertyChanged(nameof(TotalProfitLossFormatted)); OnPropertyChanged(nameof(ProfitLossColor)); }
    }
    public string TotalProfitLossFormatted => _currency.FormatProfitLoss(TotalProfitLoss);
    public IBrush ProfitLossColor => TotalProfitLoss >= 0 
        ? new SolidColorBrush(Color.Parse("#4ECB71")) 
        : new SolidColorBrush(Color.Parse("#FF6B6B"));

    public string TotalAssetsFormatted => _currency.Format(Balance + TotalPortfolioValue);

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

    public async Task LoadDataAsync()
    {
        IsLoading = true;
        StatusMessage = "";

        try
        {
            var holdings = await _api.GetHoldingsAsync();
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Holdings.Clear();
                foreach (var holding in holdings)
                {
                    Holdings.Add(holding);
                }
            });

            var summary = await _api.GetPortfolioSummaryAsync();
            if (summary != null)
            {
                TotalPortfolioValue = summary.TotalValue;
                TotalProfitLoss = summary.TotalProfitLoss;
                Balance = summary.Balance;
            }
        }
        catch
        {
            StatusMessage = _lang["ConnectionError"];
        }

        IsLoading = false;
    }

    private void RefreshFormattedValues()
    {
        OnPropertyChanged(nameof(BalanceFormatted));
        OnPropertyChanged(nameof(TotalPortfolioValueFormatted));
        OnPropertyChanged(nameof(TotalProfitLossFormatted));
        OnPropertyChanged(nameof(TotalAssetsFormatted));
    }

    public async Task ExportHoldingsAsync()
    {
        IsLoading = true;
        StatusMessage = "";

        var data = await _api.ExportHoldingsToExcelAsync();
        if (data != null)
        {
            await SaveExportFileAsync(data, "MyHoldings", ".xlsx");
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
