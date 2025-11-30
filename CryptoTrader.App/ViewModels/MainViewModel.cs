using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Linq;
using CryptoTrader.Shared.Models;
using CryptoTrader.App.Services;
using System.IO;

namespace CryptoTrader.App.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly ApiClient _api;
    private readonly LanguageService _lang;
    private readonly SettingsService _settings;

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainViewModel()
    {
        _api = new ApiClient();
        _lang = LanguageService.Instance;
        _settings = SettingsService.Instance;

        // Subscribe to language changes
        _lang.LanguageChanged += (s, e) => OnPropertyChanged(nameof(L));

        // Initialize collections
        CryptoPrices = new ObservableCollection<CryptoCurrency>();
        Holdings = new ObservableCollection<CryptoHolding>();
        Transactions = new ObservableCollection<CryptoTransaction>();
        Users = new ObservableCollection<UserInfo>();

        // Load settings to UI
        LoadSettingsToUI();

        // Try auto-login
        _ = TryAutoLoginAsync();
    }

    #region Language Helper

    public LanguageService L => _lang;

    #endregion

    #region Navigation State

    private string _currentView = "Login";
    public string CurrentView
    {
        get => _currentView;
        set { _currentView = value; OnPropertyChanged(); UpdateViewVisibility(); }
    }

    private bool _showLoginView = true;
    public bool ShowLoginView
    {
        get => _showLoginView;
        set { _showLoginView = value; OnPropertyChanged(); }
    }

    private bool _showDashboardView;
    public bool ShowDashboardView
    {
        get => _showDashboardView;
        set { _showDashboardView = value; OnPropertyChanged(); }
    }

    private bool _showPortfolioView;
    public bool ShowPortfolioView
    {
        get => _showPortfolioView;
        set { _showPortfolioView = value; OnPropertyChanged(); }
    }

    private bool _showMarketView;
    public bool ShowMarketView
    {
        get => _showMarketView;
        set { _showMarketView = value; OnPropertyChanged(); }
    }

    private bool _showTransactionsView;
    public bool ShowTransactionsView
    {
        get => _showTransactionsView;
        set { _showTransactionsView = value; OnPropertyChanged(); }
    }

    private bool _showSettingsView;
    public bool ShowSettingsView
    {
        get => _showSettingsView;
        set { _showSettingsView = value; OnPropertyChanged(); }
    }

    private bool _showAdminView;
    public bool ShowAdminView
    {
        get => _showAdminView;
        set { _showAdminView = value; OnPropertyChanged(); }
    }

    private void UpdateViewVisibility()
    {
        ShowLoginView = CurrentView == "Login" || CurrentView == "Register";
        ShowDashboardView = CurrentView == "Dashboard";
        ShowPortfolioView = CurrentView == "Portfolio";
        ShowMarketView = CurrentView == "Market";
        ShowTransactionsView = CurrentView == "Transactions";
        ShowSettingsView = CurrentView == "Settings";
        ShowAdminView = CurrentView == "Admin";
    }

    public void NavigateTo(string view)
    {
        CurrentView = view;
        
        // Load data based on view
        switch (view)
        {
            case "Dashboard":
                _ = LoadDashboardDataAsync();
                break;
            case "Portfolio":
                _ = LoadPortfolioDataAsync();
                break;
            case "Market":
                _ = LoadMarketDataAsync();
                break;
            case "Transactions":
                _ = LoadTransactionsAsync();
                break;
            case "Admin":
                _ = LoadAdminDataAsync();
                break;
            case "Settings":
                LoadSettingsToUI();
                break;
        }
    }

    #endregion

    #region Authentication

    private bool _isLoggedIn;
    public bool IsLoggedIn
    {
        get => _isLoggedIn;
        set { _isLoggedIn = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShowNavigation)); }
    }

    private bool _isAdmin;
    public bool IsAdmin
    {
        get => _isAdmin;
        set { _isAdmin = value; OnPropertyChanged(); }
    }

    private string _currentUsername = "";
    public string CurrentUsername
    {
        get => _currentUsername;
        set { _currentUsername = value; OnPropertyChanged(); }
    }

    public bool ShowNavigation => IsLoggedIn;

    // Login form
    private string _loginUsername = "";
    public string LoginUsername
    {
        get => _loginUsername;
        set { _loginUsername = value; OnPropertyChanged(); }
    }

    private string _loginPassword = "";
    public string LoginPassword
    {
        get => _loginPassword;
        set { _loginPassword = value; OnPropertyChanged(); }
    }

    private bool _loginRememberMe;
    public bool LoginRememberMe
    {
        get => _loginRememberMe;
        set { _loginRememberMe = value; OnPropertyChanged(); }
    }

    // Register form
    private string _registerUsername = "";
    public string RegisterUsername
    {
        get => _registerUsername;
        set { _registerUsername = value; OnPropertyChanged(); }
    }

    private string _registerEmail = "";
    public string RegisterEmail
    {
        get => _registerEmail;
        set { _registerEmail = value; OnPropertyChanged(); }
    }

    private string _registerPassword = "";
    public string RegisterPassword
    {
        get => _registerPassword;
        set { _registerPassword = value; OnPropertyChanged(); }
    }

    private string _registerConfirmPassword = "";
    public string RegisterConfirmPassword
    {
        get => _registerConfirmPassword;
        set { _registerConfirmPassword = value; OnPropertyChanged(); }
    }

    private bool _showRegisterForm;
    public bool ShowRegisterForm
    {
        get => _showRegisterForm;
        set { _showRegisterForm = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShowLoginForm)); }
    }

    public bool ShowLoginForm => !ShowRegisterForm;

    private async Task TryAutoLoginAsync()
    {
        if (_settings.RememberMe && !string.IsNullOrEmpty(_settings.SavedToken))
        {
            IsLoading = true;
            var result = await _api.ValidateTokenAsync(_settings.SavedToken);
            IsLoading = false;
            
            if (result.Success && result.Session != null)
            {
                OnLoginSuccess(result.Session);
            }
        }
    }

    public async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(LoginUsername) || string.IsNullOrWhiteSpace(LoginPassword))
        {
            StatusMessage = _lang["Required"];
            return;
        }

        IsLoading = true;
        StatusMessage = "";

        var result = await _api.LoginAsync(new LoginRequest
        {
            Username = LoginUsername,
            Password = LoginPassword,
            RememberMe = LoginRememberMe
        });

        IsLoading = false;

        if (result.Success && result.Session != null)
        {
            OnLoginSuccess(result.Session);
        }
        else
        {
            StatusMessage = result.Message ?? _lang["LoginFailed"];
        }
    }

    public async Task RegisterAsync()
    {
        if (string.IsNullOrWhiteSpace(RegisterUsername) || RegisterUsername.Length < 3)
        {
            StatusMessage = _lang["UsernameTooShort"];
            return;
        }

        if (string.IsNullOrWhiteSpace(RegisterEmail) || !RegisterEmail.Contains('@'))
        {
            StatusMessage = _lang["InvalidEmail"];
            return;
        }

        if (string.IsNullOrWhiteSpace(RegisterPassword) || RegisterPassword.Length < 6)
        {
            StatusMessage = _lang["PasswordTooShort"];
            return;
        }

        if (RegisterPassword != RegisterConfirmPassword)
        {
            StatusMessage = _lang["PasswordsDontMatch"];
            return;
        }

        IsLoading = true;
        StatusMessage = "";

        var result = await _api.RegisterAsync(new RegisterRequest
        {
            Username = RegisterUsername,
            Email = RegisterEmail,
            Password = RegisterPassword,
            ConfirmPassword = RegisterConfirmPassword
        });

        IsLoading = false;

        if (result.Success && result.Session != null)
        {
            OnLoginSuccess(result.Session);
        }
        else
        {
            StatusMessage = result.Message ?? _lang["Error"];
        }
    }

    private void OnLoginSuccess(UserSession session)
    {
        IsLoggedIn = true;
        CurrentUsername = session.Username;
        
        // Check if admin
        _ = CheckUserRoleAsync();
        
        LoginPassword = "";
        RegisterPassword = "";
        RegisterConfirmPassword = "";
        
        NavigateTo("Dashboard");
    }

    private async Task CheckUserRoleAsync()
    {
        var users = await _api.GetAllUsersAsync();
        var currentUser = users.FirstOrDefault(u => u.Username == CurrentUsername);
        IsAdmin = currentUser?.Role == "Admin";
    }

    public async Task LogoutAsync()
    {
        await _api.LogoutAsync();
        IsLoggedIn = false;
        IsAdmin = false;
        CurrentUsername = "";
        LoginUsername = "";
        LoginPassword = "";
        Holdings.Clear();
        Transactions.Clear();
        Users.Clear();
        NavigateTo("Login");
    }

    #endregion

    #region Status/Loading

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; OnPropertyChanged(); }
    }

    private string _statusMessage = "";
    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasStatusMessage)); }
    }

    public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);

    private bool _isConnected;
    public bool IsConnected
    {
        get => _isConnected;
        set { _isConnected = value; OnPropertyChanged(); }
    }

    #endregion

    #region Dashboard Data

    private decimal _totalPortfolioValue;
    public decimal TotalPortfolioValue
    {
        get => _totalPortfolioValue;
        set { _totalPortfolioValue = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalPortfolioValueFormatted)); }
    }

    public string TotalPortfolioValueFormatted => $"${TotalPortfolioValue:N2}";

    private decimal _totalProfitLoss;
    public decimal TotalProfitLoss
    {
        get => _totalProfitLoss;
        set { _totalProfitLoss = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalProfitLossFormatted)); OnPropertyChanged(nameof(IsProfitPositive)); }
    }

    public string TotalProfitLossFormatted => $"{(TotalProfitLoss >= 0 ? "+" : "")}{TotalProfitLoss:N2}";
    public bool IsProfitPositive => TotalProfitLoss >= 0;

    private decimal _totalProfitLossPercentage;
    public decimal TotalProfitLossPercentage
    {
        get => _totalProfitLossPercentage;
        set { _totalProfitLossPercentage = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalProfitLossPercentageFormatted)); }
    }

    public string TotalProfitLossPercentageFormatted => $"{(TotalProfitLossPercentage >= 0 ? "+" : "")}{TotalProfitLossPercentage:N2}%";

    public async Task LoadDashboardDataAsync()
    {
        IsLoading = true;
        StatusMessage = "";

        try
        {
            // Load portfolio summary
            var summary = await _api.GetPortfolioSummaryAsync();
            if (summary != null)
            {
                TotalPortfolioValue = summary.TotalValue;
                TotalProfitLoss = summary.TotalProfitLoss;
                TotalProfitLossPercentage = summary.TotalProfitLossPercentage;
            }

            // Load top cryptos
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

    #endregion

    #region Market Data

    public ObservableCollection<CryptoCurrency> CryptoPrices { get; }

    private string _searchQuery = "";
    public string SearchQuery
    {
        get => _searchQuery;
        set { _searchQuery = value; OnPropertyChanged(); }
    }

    private CryptoCurrency? _selectedCrypto;
    public CryptoCurrency? SelectedCrypto
    {
        get => _selectedCrypto;
        set { _selectedCrypto = value; OnPropertyChanged(); }
    }

    public async Task LoadMarketDataAsync()
    {
        IsLoading = true;
        StatusMessage = "";

        try
        {
            var prices = await _api.GetCachedPricesAsync();
            CryptoPrices.Clear();
            foreach (var price in prices.OrderBy(p => p.MarketCapRank))
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

    public async Task SearchCryptosAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            await LoadMarketDataAsync();
            return;
        }

        IsLoading = true;

        var results = await _api.SearchCryptosAsync(SearchQuery);
        CryptoPrices.Clear();
        foreach (var crypto in results)
        {
            CryptoPrices.Add(crypto);
        }

        IsLoading = false;
    }

    public async Task RefreshPricesAsync()
    {
        await LoadMarketDataAsync();
    }

    #endregion

    #region Portfolio Data

    public ObservableCollection<CryptoHolding> Holdings { get; }

    private string _newHoldingCoinId = "";
    public string NewHoldingCoinId
    {
        get => _newHoldingCoinId;
        set { _newHoldingCoinId = value; OnPropertyChanged(); }
    }

    private string _newHoldingSymbol = "";
    public string NewHoldingSymbol
    {
        get => _newHoldingSymbol;
        set { _newHoldingSymbol = value; OnPropertyChanged(); }
    }

    private decimal _newHoldingAmount;
    public decimal NewHoldingAmount
    {
        get => _newHoldingAmount;
        set { _newHoldingAmount = value; OnPropertyChanged(); }
    }

    private decimal _newHoldingPurchasePrice;
    public decimal NewHoldingPurchasePrice
    {
        get => _newHoldingPurchasePrice;
        set { _newHoldingPurchasePrice = value; OnPropertyChanged(); }
    }

    private bool _showAddHoldingForm;
    public bool ShowAddHoldingForm
    {
        get => _showAddHoldingForm;
        set { _showAddHoldingForm = value; OnPropertyChanged(); }
    }

    public async Task LoadPortfolioDataAsync()
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
                TotalProfitLossPercentage = summary.TotalProfitLossPercentage;
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
            ClearHoldingForm();
            await LoadPortfolioDataAsync();
            StatusMessage = _lang["Success"];
        }
        else
        {
            StatusMessage = _lang["Error"];
        }

        IsLoading = false;
    }

    public async Task DeleteHoldingAsync(int holdingId)
    {
        IsLoading = true;

        var success = await _api.DeleteHoldingAsync(holdingId);

        if (success)
        {
            await LoadPortfolioDataAsync();
        }

        IsLoading = false;
    }

    private void ClearHoldingForm()
    {
        NewHoldingCoinId = "";
        NewHoldingSymbol = "";
        NewHoldingAmount = 0;
        NewHoldingPurchasePrice = 0;
    }

    #endregion

    #region Transactions Data

    public ObservableCollection<CryptoTransaction> Transactions { get; }

    private bool _newTransactionIsBuy = true;
    public bool NewTransactionIsBuy
    {
        get => _newTransactionIsBuy;
        set { _newTransactionIsBuy = value; OnPropertyChanged(); }
    }

    private string _newTransactionCoinId = "";
    public string NewTransactionCoinId
    {
        get => _newTransactionCoinId;
        set { _newTransactionCoinId = value; OnPropertyChanged(); }
    }

    private decimal _newTransactionAmount;
    public decimal NewTransactionAmount
    {
        get => _newTransactionAmount;
        set { _newTransactionAmount = value; OnPropertyChanged(); }
    }

    private decimal _newTransactionPricePerUnit;
    public decimal NewTransactionPricePerUnit
    {
        get => _newTransactionPricePerUnit;
        set { _newTransactionPricePerUnit = value; OnPropertyChanged(); }
    }

    private decimal _newTransactionFee;
    public decimal NewTransactionFee
    {
        get => _newTransactionFee;
        set { _newTransactionFee = value; OnPropertyChanged(); }
    }

    private string _newTransactionNotes = "";
    public string NewTransactionNotes
    {
        get => _newTransactionNotes;
        set { _newTransactionNotes = value; OnPropertyChanged(); }
    }

    private bool _showAddTransactionForm;
    public bool ShowAddTransactionForm
    {
        get => _showAddTransactionForm;
        set { _showAddTransactionForm = value; OnPropertyChanged(); }
    }

    public async Task LoadTransactionsAsync()
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
            ClearTransactionForm();
            await LoadTransactionsAsync();
            StatusMessage = _lang["Success"];
        }
        else
        {
            StatusMessage = _lang["Error"];
        }

        IsLoading = false;
    }

    private void ClearTransactionForm()
    {
        NewTransactionCoinId = "";
        NewTransactionAmount = 0;
        NewTransactionPricePerUnit = 0;
        NewTransactionFee = 0;
        NewTransactionNotes = "";
    }

    #endregion

    #region Admin Data

    public ObservableCollection<UserInfo> Users { get; }

    private SystemStats? _systemStats;
    public SystemStats? SystemStats
    {
        get => _systemStats;
        set { _systemStats = value; OnPropertyChanged(); }
    }

    public async Task LoadAdminDataAsync()
    {
        if (!IsAdmin) return;

        IsLoading = true;
        StatusMessage = "";

        try
        {
            var users = await _api.GetAllUsersAsync();
            Users.Clear();
            foreach (var user in users)
            {
                Users.Add(user);
            }

            SystemStats = await _api.GetSystemStatsAsync();
        }
        catch
        {
            StatusMessage = _lang["ConnectionError"];
        }

        IsLoading = false;
    }

    public async Task ChangeUserRoleAsync(int userId, string newRole)
    {
        IsLoading = true;

        var success = await _api.ChangeUserRoleAsync(userId, newRole);

        if (success)
        {
            await LoadAdminDataAsync();
        }

        IsLoading = false;
    }

    public async Task ToggleUserStatusAsync(int userId, bool isActive)
    {
        IsLoading = true;

        var success = await _api.ChangeUserStatusAsync(userId, isActive);

        if (success)
        {
            await LoadAdminDataAsync();
        }

        IsLoading = false;
    }

    public async Task AdminRefreshPricesAsync()
    {
        IsLoading = true;
        StatusMessage = "";

        var success = await _api.RefreshCryptoPricesAsync();

        StatusMessage = success ? _lang["Success"] : _lang["Error"];
        IsLoading = false;
    }

    #endregion

    #region Settings

    private string _selectedLanguage = "en";
    public string SelectedLanguage
    {
        get => _selectedLanguage;
        set 
        { 
            _selectedLanguage = value; 
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsEnglishSelected));
            OnPropertyChanged(nameof(IsCroatianSelected));
        }
    }

    public bool IsEnglishSelected
    {
        get => SelectedLanguage == "en";
        set { if (value) SelectedLanguage = "en"; }
    }

    public bool IsCroatianSelected
    {
        get => SelectedLanguage == "hr";
        set { if (value) SelectedLanguage = "hr"; }
    }

    private string _selectedTheme = "Dark";
    public string SelectedTheme
    {
        get => _selectedTheme;
        set { _selectedTheme = value; OnPropertyChanged(); }
    }

    private string _selectedCurrency = "USD";
    public string SelectedCurrency
    {
        get => _selectedCurrency;
        set { _selectedCurrency = value; OnPropertyChanged(); }
    }

    private string _serverAddress = "localhost";
    public string ServerAddress
    {
        get => _serverAddress;
        set { _serverAddress = value; OnPropertyChanged(); }
    }

    private int _serverPort = 5002;
    public int ServerPort
    {
        get => _serverPort;
        set { _serverPort = value; OnPropertyChanged(); }
    }

    private bool _autoConnect = true;
    public bool AutoConnect
    {
        get => _autoConnect;
        set { _autoConnect = value; OnPropertyChanged(); }
    }

    private bool _priceAlertsEnabled = true;
    public bool PriceAlertsEnabled
    {
        get => _priceAlertsEnabled;
        set { _priceAlertsEnabled = value; OnPropertyChanged(); }
    }

    private bool _soundEnabled = true;
    public bool SoundEnabled
    {
        get => _soundEnabled;
        set { _soundEnabled = value; OnPropertyChanged(); }
    }

    public void LoadSettingsToUI()
    {
        SelectedLanguage = _settings.Language;
        SelectedTheme = _settings.Theme;
        SelectedCurrency = _settings.DisplayCurrency;
        ServerAddress = _settings.ServerAddress;
        ServerPort = _settings.ServerHttpPort;
        AutoConnect = _settings.AutoConnect;
        PriceAlertsEnabled = _settings.PriceAlertsEnabled;
        SoundEnabled = _settings.SoundEnabled;
    }

    public void SaveSettingsFromUI()
    {
        _settings.Language = SelectedLanguage;
        _settings.Theme = SelectedTheme;
        _settings.DisplayCurrency = SelectedCurrency;
        _settings.ServerAddress = ServerAddress;
        _settings.ServerHttpPort = ServerPort;
        _settings.AutoConnect = AutoConnect;
        _settings.PriceAlertsEnabled = PriceAlertsEnabled;
        _settings.SoundEnabled = SoundEnabled;
        _settings.SaveSettings();

        _lang.CurrentLanguage = SelectedLanguage;
        _api.UpdateBaseAddress();
        StatusMessage = _lang["SettingsSaved"];
    }

    public void ResetSettingsToDefaults()
    {
        _settings.ResetToDefaults();
        LoadSettingsToUI();
        _lang.CurrentLanguage = SelectedLanguage;
        StatusMessage = _lang["SettingsSaved"];
    }

    #endregion

    #region Export

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

    public async Task ExportHoldingsToExcelAsync()
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

    public async Task ExportTransactionsToExcelAsync()
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

    public async Task ExportAdminReportAsync()
    {
        IsLoading = true;
        StatusMessage = "";

        var data = await _api.ExportAdminReportAsync();
        if (data != null)
        {
            await SaveExportFileAsync(data, "SystemReport", ".xlsx");
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

    #endregion

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
