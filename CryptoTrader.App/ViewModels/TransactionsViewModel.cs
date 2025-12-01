using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CryptoTrader.Shared.Models;
using CryptoTrader.App.Services;
using System.IO;

namespace CryptoTrader.App.ViewModels;

public class TransactionsViewModel : ViewModelBase
{
    private readonly ApiClient _api;
    private readonly LanguageService _lang;

    public TransactionsViewModel()
    {
        _api = new ApiClient();
        _lang = LanguageService.Instance;
        Transactions = new ObservableCollection<CryptoTransaction>();

        _lang.LanguageChanged += (s, e) => OnPropertyChanged(nameof(L));

        _ = LoadDataAsync();
    }

    public LanguageService L => _lang;
    public ObservableCollection<CryptoTransaction> Transactions { get; }

    private bool _newTransactionIsBuy = true;
    public bool NewTransactionIsBuy
    {
        get => _newTransactionIsBuy;
        set => SetProperty(ref _newTransactionIsBuy, value);
    }

    private string _newTransactionCoinId = "";
    public string NewTransactionCoinId
    {
        get => _newTransactionCoinId;
        set => SetProperty(ref _newTransactionCoinId, value);
    }

    private decimal _newTransactionAmount;
    public decimal NewTransactionAmount
    {
        get => _newTransactionAmount;
        set => SetProperty(ref _newTransactionAmount, value);
    }

    private decimal _newTransactionPricePerUnit;
    public decimal NewTransactionPricePerUnit
    {
        get => _newTransactionPricePerUnit;
        set => SetProperty(ref _newTransactionPricePerUnit, value);
    }

    private decimal _newTransactionFee;
    public decimal NewTransactionFee
    {
        get => _newTransactionFee;
        set => SetProperty(ref _newTransactionFee, value);
    }

    private string _newTransactionNotes = "";
    public string NewTransactionNotes
    {
        get => _newTransactionNotes;
        set => SetProperty(ref _newTransactionNotes, value);
    }

    private bool _showAddTransactionForm;
    public bool ShowAddTransactionForm
    {
        get => _showAddTransactionForm;
        set => SetProperty(ref _showAddTransactionForm, value);
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
            var transactions = await _api.GetTransactionsAsync();
            Transactions.Clear();
            foreach (var tx in transactions)
            {
                Transactions.Add(tx);
            }
        }
        catch
        {
            StatusMessage = _lang["ConnectionError"];
        }

        IsLoading = false;
    }

    public async Task AddTransactionAsync()
    {
        if (string.IsNullOrWhiteSpace(NewTransactionCoinId) || NewTransactionAmount <= 0)
        {
            StatusMessage = _lang["InvalidAmount"];
            return;
        }

        IsLoading = true;

        var transaction = new CryptoTransaction
        {
            Type = NewTransactionIsBuy ? TransactionType.Buy : TransactionType.Sell,
            CoinId = NewTransactionCoinId.ToLower(),
            Symbol = NewTransactionCoinId.ToUpper(),
            Amount = NewTransactionAmount,
            PricePerUnit = NewTransactionPricePerUnit,
            Fee = NewTransactionFee,
            Notes = NewTransactionNotes,
            Timestamp = DateTime.UtcNow
        };

        var success = await _api.AddTransactionAsync(transaction);

        if (success)
        {
            ShowAddTransactionForm = false;
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
        NewTransactionCoinId = "";
        NewTransactionAmount = 0;
        NewTransactionPricePerUnit = 0;
        NewTransactionFee = 0;
        NewTransactionNotes = "";
    }

    public async Task ExportTransactionsAsync()
    {
        IsLoading = true;
        StatusMessage = "";

        var data = await _api.ExportTransactionsToExcelAsync();
        if (data != null)
        {
            await SaveExportFileAsync(data, "Transactions", ".xlsx");
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
