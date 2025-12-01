using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CryptoTrader.Shared.Models;
using CryptoTrader.App.Services;
using System.IO;

namespace CryptoTrader.App.ViewModels;

public class PortfolioViewModel : ViewModelBase
{
    private readonly ApiClient _api;
    private readonly LanguageService _lang;

    public PortfolioViewModel()
    {
        _api = new ApiClient();
        _lang = LanguageService.Instance;
        Holdings = new ObservableCollection<CryptoHolding>();

        _lang.LanguageChanged += (s, e) => OnPropertyChanged(nameof(L));

        _ = LoadDataAsync();
    }

    public LanguageService L => _lang;
    public ObservableCollection<CryptoHolding> Holdings { get; }

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
        set { SetProperty(ref _totalProfitLoss, value); OnPropertyChanged(nameof(TotalProfitLossFormatted)); }
    }
    public string TotalProfitLossFormatted => $"{(TotalProfitLoss >= 0 ? "+" : "")}{TotalProfitLoss:N2}";

    private string _newHoldingCoinId = "";
    public string NewHoldingCoinId
    {
        get => _newHoldingCoinId;
        set => SetProperty(ref _newHoldingCoinId, value);
    }

    private string _newHoldingSymbol = "";
    public string NewHoldingSymbol
    {
        get => _newHoldingSymbol;
        set => SetProperty(ref _newHoldingSymbol, value);
    }

    private decimal _newHoldingAmount;
    public decimal NewHoldingAmount
    {
        get => _newHoldingAmount;
        set => SetProperty(ref _newHoldingAmount, value);
    }

    private decimal _newHoldingPurchasePrice;
    public decimal NewHoldingPurchasePrice
    {
        get => _newHoldingPurchasePrice;
        set => SetProperty(ref _newHoldingPurchasePrice, value);
    }

    private bool _showAddHoldingForm;
    public bool ShowAddHoldingForm
    {
        get => _showAddHoldingForm;
        set => SetProperty(ref _showAddHoldingForm, value);
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

    public async Task LoadDataAsync()
    {
        IsLoading = true;
        StatusMessage = "";

        try
        {
            var holdings = await _api.GetHoldingsAsync();
            Holdings.Clear();
            foreach (var holding in holdings)
            {
                Holdings.Add(holding);
            }

            var summary = await _api.GetPortfolioSummaryAsync();
            if (summary != null)
            {
                TotalPortfolioValue = summary.TotalValue;
                TotalProfitLoss = summary.TotalProfitLoss;
            }
        }
        catch
        {
            StatusMessage = _lang["ConnectionError"];
        }

        IsLoading = false;
    }

    public async Task AddHoldingAsync()
    {
        if (string.IsNullOrWhiteSpace(NewHoldingCoinId) || NewHoldingAmount <= 0)
        {
            StatusMessage = _lang["InvalidAmount"];
            return;
        }

        IsLoading = true;

        var holding = new CryptoHolding
        {
            CoinId = NewHoldingCoinId.ToLower(),
            Symbol = string.IsNullOrEmpty(NewHoldingSymbol) ? NewHoldingCoinId.ToUpper() : NewHoldingSymbol.ToUpper(),
            Amount = NewHoldingAmount,
            PurchasePrice = NewHoldingPurchasePrice,
            PurchaseDate = DateTime.UtcNow
        };

        var success = await _api.AddHoldingAsync(holding);

        if (success)
        {
            ShowAddHoldingForm = false;
            ClearForm();
            await LoadDataAsync();
            StatusMessage = _lang["Success"];
        }
        else
        {
            StatusMessage = _lang["Error"];
        }

        IsLoading = false;
    }

    private void ClearForm()
    {
        NewHoldingCoinId = "";
        NewHoldingSymbol = "";
        NewHoldingAmount = 0;
        NewHoldingPurchasePrice = 0;
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
