using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CryptoTrader.Shared.Models;
using CryptoTrader.Shared.Config;
using CryptoTrader.App.Services;

namespace CryptoTrader.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ApiClient _apiClient;
    private readonly UserSettings _settings;

    [ObservableProperty]
    private bool _isLoggedIn;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private UserSession? _currentSession;

    [ObservableProperty]
    private ObservableCollection<CryptoCurrency> _cryptoPrices = new();

    [ObservableProperty]
    private CryptoCurrency? _selectedCrypto;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    public MainViewModel()
    {
        _settings = new UserSettings();
        _apiClient = new ApiClient(_settings);
        
        // Try to restore session
        _ = TryRestoreSessionAsync();
    }

    private async Task TryRestoreSessionAsync()
    {
        if (_settings.RememberMe && !string.IsNullOrEmpty(_settings.SavedToken))
        {
            IsLoading = true;
            StatusMessage = "Restoring session...";
            
            var result = await _apiClient.ValidateTokenAsync(_settings.SavedToken);
            if (result.Success && result.Session != null)
            {
                CurrentSession = result.Session;
                IsLoggedIn = true;
                Username = result.Session.Username;
                StatusMessage = $"Welcome back, {Username}!";
                await LoadCryptoPricesAsync();
            }
            else
            {
                _settings.ClearAuthData();
                StatusMessage = "Session expired. Please login again.";
            }
            
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            StatusMessage = "Please enter username and password";
            return;
        }

        IsLoading = true;
        StatusMessage = "Logging in...";

        try
        {
            var result = await _apiClient.LoginAsync(new LoginRequest
            {
                Username = Username,
                Password = Password,
                RememberMe = true
            });

            if (result.Success && result.Session != null)
            {
                CurrentSession = result.Session;
                IsLoggedIn = true;
                Password = string.Empty;
                StatusMessage = $"Welcome, {Username}!";
                await LoadCryptoPricesAsync();
            }
            else
            {
                StatusMessage = result.Message;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Login failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            StatusMessage = "Please enter username and password";
            return;
        }

        IsLoading = true;
        StatusMessage = "Creating account...";

        try
        {
            var result = await _apiClient.RegisterAsync(new RegisterRequest
            {
                Username = Username,
                Email = $"{Username}@cryptotrader.local",
                Password = Password,
                ConfirmPassword = Password
            });

            if (result.Success && result.Session != null)
            {
                CurrentSession = result.Session;
                IsLoggedIn = true;
                Password = string.Empty;
                StatusMessage = $"Welcome, {Username}! Account created successfully.";
                await LoadCryptoPricesAsync();
            }
            else
            {
                StatusMessage = result.Message;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Registration failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        await _apiClient.LogoutAsync();
        IsLoggedIn = false;
        CurrentSession = null;
        Username = string.Empty;
        Password = string.Empty;
        CryptoPrices.Clear();
        StatusMessage = "Logged out successfully";
    }

    [RelayCommand]
    private async Task LoadCryptoPricesAsync()
    {
        IsLoading = true;
        StatusMessage = "Loading prices...";

        try
        {
            var prices = await _apiClient.GetTopCryptosAsync(50, _settings.Currency.ToLower());
            
            CryptoPrices.Clear();
            foreach (var crypto in prices)
            {
                CryptoPrices.Add(crypto);
            }
            
            StatusMessage = $"Loaded {prices.Count} cryptocurrencies";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load prices: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            await LoadCryptoPricesAsync();
            return;
        }

        IsLoading = true;
        StatusMessage = $"Searching for '{SearchQuery}'...";

        try
        {
            var results = await _apiClient.SearchCryptosAsync(SearchQuery);
            
            CryptoPrices.Clear();
            foreach (var crypto in results)
            {
                CryptoPrices.Add(crypto);
            }
            
            StatusMessage = $"Found {results.Count} results";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Search failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadCryptoPricesAsync();
    }
}
