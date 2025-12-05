using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CryptoTrader.App.Models;
using CryptoTrader.App.Services;
using CryptoTrader.Shared.Models;

namespace CryptoTrader.App.ViewModels;

public class DcaViewModel : ViewModelBase
{
    private readonly LanguageService _lang;
    private readonly DcaStorageService _dcaStorage;
    private readonly ApiClient _api;
    private readonly NavigationService _nav;
    private readonly CurrencyService _currency;

    public DcaViewModel()
    {
        _lang = LanguageService.Instance;
        _dcaStorage = DcaStorageService.Instance;
        _api = new ApiClient();
        _nav = NavigationService.Instance;
        _currency = CurrencyService.Instance;

        // Use shared auth token
        var token = _nav.AuthToken;
        if (!string.IsNullOrEmpty(token))
        {
            _api.SetAuthToken(token);
        }

        Plans = new ObservableCollection<DcaPlanDisplayItem>();
        AvailableCryptos = new ObservableCollection<CryptoOption>();
        Frequencies = new ObservableCollection<string> { "Daily", "Weekly", "BiWeekly", "Monthly" };
        DaysOfWeek = new ObservableCollection<string> { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };

        _lang.LanguageChanged += (s, e) => OnPropertyChanged(nameof(L));
        _dcaStorage.OnPlansChanged += async (s, e) => await LoadPlansAsync();
    }

    public LanguageService L => _lang;

    public ObservableCollection<DcaPlanDisplayItem> Plans { get; }
    public ObservableCollection<CryptoOption> AvailableCryptos { get; }
    public ObservableCollection<string> Frequencies { get; }
    public ObservableCollection<string> DaysOfWeek { get; }

    #region Properties

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

    private bool _isSuccess;
    public bool IsSuccess
    {
        get => _isSuccess;
        set => SetProperty(ref _isSuccess, value);
    }

    private DcaPlanDisplayItem? _selectedPlan;
    public DcaPlanDisplayItem? SelectedPlan
    {
        get => _selectedPlan;
        set { SetProperty(ref _selectedPlan, value); OnPropertyChanged(nameof(HasSelectedPlan)); }
    }
    public bool HasSelectedPlan => _selectedPlan != null;

    // Form fields for new/edit plan
    private string _planName = "";
    public string PlanName
    {
        get => _planName;
        set => SetProperty(ref _planName, value);
    }

    private CryptoOption? _selectedCrypto;
    public CryptoOption? SelectedCrypto
    {
        get => _selectedCrypto;
        set => SetProperty(ref _selectedCrypto, value);
    }

    private string _amount = "100";
    public string Amount
    {
        get => _amount;
        set => SetProperty(ref _amount, value);
    }

    private int _selectedFrequencyIndex;
    public int SelectedFrequencyIndex
    {
        get => _selectedFrequencyIndex;
        set { SetProperty(ref _selectedFrequencyIndex, value); OnPropertyChanged(nameof(ShowDayOfWeek)); OnPropertyChanged(nameof(ShowDayOfMonth)); }
    }

    private int _selectedDayOfWeekIndex = 1; // Monday
    public int SelectedDayOfWeekIndex
    {
        get => _selectedDayOfWeekIndex;
        set => SetProperty(ref _selectedDayOfWeekIndex, value);
    }

    private int _dayOfMonth = 1;
    public int DayOfMonth
    {
        get => _dayOfMonth;
        set => SetProperty(ref _dayOfMonth, Math.Max(1, Math.Min(28, value)));
    }

    public bool ShowDayOfWeek => SelectedFrequencyIndex == 1 || SelectedFrequencyIndex == 2; // Weekly or BiWeekly
    public bool ShowDayOfMonth => SelectedFrequencyIndex == 3; // Monthly

    private bool _isEditing;
    public bool IsEditing
    {
        get => _isEditing;
        set { SetProperty(ref _isEditing, value); OnPropertyChanged(nameof(FormTitle)); }
    }

    private string? _editingPlanId;

    public string FormTitle => IsEditing ? L["EditPlan"] : L["CreateNewPlan"];

    // Summary
    private int _totalPlans;
    public int TotalPlans
    {
        get => _totalPlans;
        set => SetProperty(ref _totalPlans, value);
    }

    private int _activePlans;
    public int ActivePlans
    {
        get => _activePlans;
        set => SetProperty(ref _activePlans, value);
    }

    private string _totalInvested = "$0.00";
    public string TotalInvested
    {
        get => _totalInvested;
        set => SetProperty(ref _totalInvested, value);
    }

    #endregion

    #region Load Data

    public async Task LoadDataAsync()
    {
        IsLoading = true;
        StatusMessage = "";

        try
        {
            // Load DCA plans
            await _dcaStorage.LoadAsync();
            await LoadPlansAsync();

            // Load available cryptos
            var cryptos = await _api.GetTopCryptosAsync(50);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                AvailableCryptos.Clear();
                foreach (var crypto in cryptos)
                {
                    AvailableCryptos.Add(new CryptoOption
                    {
                        CoinId = crypto.CoinId,
                        Symbol = crypto.Symbol,
                        Name = crypto.Name,
                        CurrentPrice = crypto.CurrentPrice
                    });
                }
            });

            UpdateSummary();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            IsSuccess = false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadPlansAsync()
    {
        var plans = _dcaStorage.GetAllPlans();
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Plans.Clear();
            foreach (var plan in plans)
            {
                Plans.Add(new DcaPlanDisplayItem(plan, _currency));
            }
        });
        UpdateSummary();
    }

    private void UpdateSummary()
    {
        var summary = _dcaStorage.GetSummary();
        TotalPlans = summary.TotalPlans;
        ActivePlans = summary.ActivePlans;
        TotalInvested = _currency.Format(summary.TotalInvested);
    }

    #endregion

    #region CRUD Operations

    public async Task CreatePlanAsync()
    {
        if (string.IsNullOrWhiteSpace(PlanName) || SelectedCrypto == null)
        {
            StatusMessage = L["FillAllFields"];
            IsSuccess = false;
            return;
        }

        if (!decimal.TryParse(Amount, out var amount) || amount <= 0)
        {
            StatusMessage = L["InvalidAmount"];
            IsSuccess = false;
            return;
        }

        IsLoading = true;

        try
        {
            var plan = new DcaPlan
            {
                Name = PlanName,
                CoinId = SelectedCrypto.CoinId,
                Symbol = SelectedCrypto.Symbol,
                Amount = amount,
                Frequency = (DcaFrequency)SelectedFrequencyIndex,
                DayOfWeek = (DayOfWeek)SelectedDayOfWeekIndex,
                DayOfMonth = DayOfMonth,
                IsActive = true
            };

            await _dcaStorage.CreatePlanAsync(plan);

            ClearForm();
            StatusMessage = L["PlanCreated"];
            IsSuccess = true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            IsSuccess = false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task UpdatePlanAsync()
    {
        if (string.IsNullOrEmpty(_editingPlanId))
            return;

        if (string.IsNullOrWhiteSpace(PlanName) || SelectedCrypto == null)
        {
            StatusMessage = L["FillAllFields"];
            IsSuccess = false;
            return;
        }

        if (!decimal.TryParse(Amount, out var amount) || amount <= 0)
        {
            StatusMessage = L["InvalidAmount"];
            IsSuccess = false;
            return;
        }

        IsLoading = true;

        try
        {
            var existingPlan = _dcaStorage.GetPlanById(_editingPlanId);
            if (existingPlan == null)
            {
                StatusMessage = "Plan not found";
                IsSuccess = false;
                return;
            }

            existingPlan.Name = PlanName;
            existingPlan.CoinId = SelectedCrypto.CoinId;
            existingPlan.Symbol = SelectedCrypto.Symbol;
            existingPlan.Amount = amount;
            existingPlan.Frequency = (DcaFrequency)SelectedFrequencyIndex;
            existingPlan.DayOfWeek = (DayOfWeek)SelectedDayOfWeekIndex;
            existingPlan.DayOfMonth = DayOfMonth;

            await _dcaStorage.UpdatePlanAsync(existingPlan);

            ClearForm();
            StatusMessage = L["PlanUpdated"];
            IsSuccess = true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            IsSuccess = false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task DeletePlanAsync(string planId)
    {
        IsLoading = true;

        try
        {
            await _dcaStorage.DeletePlanAsync(planId);
            StatusMessage = L["PlanDeleted"];
            IsSuccess = true;

            if (_editingPlanId == planId)
            {
                ClearForm();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            IsSuccess = false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task TogglePlanActiveAsync(string planId)
    {
        try
        {
            await _dcaStorage.TogglePlanActiveAsync(planId);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            IsSuccess = false;
        }
    }

    public void EditPlan(DcaPlanDisplayItem planItem)
    {
        var plan = _dcaStorage.GetPlanById(planItem.Id);
        if (plan == null) return;

        _editingPlanId = plan.Id;
        IsEditing = true;
        PlanName = plan.Name;
        Amount = plan.Amount.ToString();
        SelectedFrequencyIndex = (int)plan.Frequency;
        SelectedDayOfWeekIndex = (int)plan.DayOfWeek;
        DayOfMonth = plan.DayOfMonth;

        // Find and select the crypto
        SelectedCrypto = AvailableCryptos.FirstOrDefault(c => c.CoinId == plan.CoinId);
    }

    public void ClearForm()
    {
        _editingPlanId = null;
        IsEditing = false;
        PlanName = "";
        Amount = "100";
        SelectedFrequencyIndex = 1; // Weekly
        SelectedDayOfWeekIndex = 1; // Monday
        DayOfMonth = 1;
        SelectedCrypto = null;
    }

    #endregion

    #region Export/Import

    public async Task ExportToJsonAsync(string filePath)
    {
        try
        {
            await _dcaStorage.ExportToJsonAsync(filePath);
            StatusMessage = L["ExportSuccess"];
            IsSuccess = true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export failed: {ex.Message}";
            IsSuccess = false;
        }
    }

    public async Task ExportToXmlAsync(string filePath)
    {
        try
        {
            await _dcaStorage.ExportToXmlAsync(filePath);
            StatusMessage = L["ExportSuccess"];
            IsSuccess = true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export failed: {ex.Message}";
            IsSuccess = false;
        }
    }

    public async Task ImportFromJsonAsync(string filePath)
    {
        try
        {
            await _dcaStorage.ImportFromJsonAsync(filePath);
            StatusMessage = L["ImportSuccess"];
            IsSuccess = true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Import failed: {ex.Message}";
            IsSuccess = false;
        }
    }

    public async Task ImportFromXmlAsync(string filePath)
    {
        try
        {
            await _dcaStorage.ImportFromXmlAsync(filePath);
            StatusMessage = L["ImportSuccess"];
            IsSuccess = true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Import failed: {ex.Message}";
            IsSuccess = false;
        }
    }

    #endregion
}

/// <summary>
/// Display wrapper for DcaPlan with formatted values
/// </summary>
public class DcaPlanDisplayItem
{
    private readonly DcaPlan _plan;
    private readonly CurrencyService _currency;

    public DcaPlanDisplayItem(DcaPlan plan, CurrencyService currency)
    {
        _plan = plan;
        _currency = currency;
    }

    public string Id => _plan.Id;
    public string Name => _plan.Name;
    public string Symbol => _plan.Symbol.ToUpper();
    public string CoinId => _plan.CoinId;
    public string Amount => _currency.Format(_plan.Amount);
    public string Frequency => _plan.Frequency.ToString();
    public bool IsActive => _plan.IsActive;
    public string NextExecution => _plan.NextExecution.ToString("MMM dd, yyyy");
    public string TotalInvested => _currency.Format(_plan.TotalInvested);
    public string TotalCoinsBought => $"{_plan.TotalCoinsBought:N6}";
    public string AveragePrice => _plan.AveragePurchasePrice > 0 ? _currency.Format(_plan.AveragePurchasePrice) : "-";
    public int ExecutionCount => _plan.ExecutionCount;
    public string StatusColor => IsActive ? "#4ECB71" : "#FF6B6B";
    public string StatusText => IsActive ? "Active" : "Paused";
}
