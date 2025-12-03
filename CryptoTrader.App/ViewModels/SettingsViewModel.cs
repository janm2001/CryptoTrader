using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CryptoTrader.App.Services;

namespace CryptoTrader.App.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly LanguageService _lang;
    private readonly CurrencyService _currency;
    private readonly SettingsService _settings;
    private readonly ApiClient _api;

    public SettingsViewModel()
    {
        _lang = LanguageService.Instance;
        _currency = CurrencyService.Instance;
        _settings = SettingsService.Instance;
        _api = new ApiClient();

        // Use shared auth token
        var token = NavigationService.Instance.AuthToken;
        if (!string.IsNullOrEmpty(token))
        {
            _api.SetAuthToken(token);
        }

        _lang.LanguageChanged += (s, e) => OnPropertyChanged(nameof(L));
        _currency.CurrencyChanged += (s, e) => OnPropertyChanged(nameof(ExchangeRateInfo));

        // Load current settings to UI
        LoadSettings();
        
        // Load profile picture
        _ = LoadProfilePictureAsync();
    }

    public LanguageService L => _lang;

    #region Profile Picture

    private Bitmap? _profilePicture;
    public Bitmap? ProfilePicture
    {
        get => _profilePicture;
        set
        {
            SetProperty(ref _profilePicture, value);
            OnPropertyChanged(nameof(HasProfilePicture));
        }
    }

    public bool HasProfilePicture => _profilePicture != null;

    private string _profilePictureStatus = "";
    public string ProfilePictureStatus
    {
        get => _profilePictureStatus;
        set { SetProperty(ref _profilePictureStatus, value); OnPropertyChanged(nameof(HasProfilePictureStatus)); }
    }
    public bool HasProfilePictureStatus => !string.IsNullOrEmpty(ProfilePictureStatus);

    public async Task LoadProfilePictureAsync()
    {
        try
        {
            var imageData = await _api.GetProfilePictureAsync();
            if (imageData != null && imageData.Length > 0)
            {
                using var stream = new MemoryStream(imageData);
                ProfilePicture = new Bitmap(stream);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load profile picture: {ex.Message}");
        }
    }

    public async Task UploadProfilePictureAsync(byte[] imageData, string mimeType)
    {
        IsLoading = true;
        ProfilePictureStatus = "";

        try
        {
            var success = await _api.UploadProfilePictureAsync(imageData, mimeType);
            if (success)
            {
                // Reload the picture
                using var stream = new MemoryStream(imageData);
                ProfilePicture = new Bitmap(stream);
                ProfilePictureStatus = _lang["ImageUploaded"];
            }
            else
            {
                ProfilePictureStatus = _lang["Error"];
            }
        }
        catch (Exception ex)
        {
            ProfilePictureStatus = $"Error: {ex.Message}";
        }

        IsLoading = false;
    }

    public async Task RemoveProfilePictureAsync()
    {
        IsLoading = true;
        ProfilePictureStatus = "";

        try
        {
            var success = await _api.DeleteProfilePictureAsync();
            if (success)
            {
                ProfilePicture = null;
                ProfilePictureStatus = _lang["ImageRemoved"];
            }
            else
            {
                ProfilePictureStatus = _lang["Error"];
            }
        }
        catch (Exception ex)
        {
            ProfilePictureStatus = $"Error: {ex.Message}";
        }

        IsLoading = false;
    }

    public void SetProfilePictureStatus(string status, bool isSuccess)
    {
        ProfilePictureStatus = status;
        IsSuccess = isSuccess;
    }

    #endregion

    #region Language Settings

    private string _selectedLanguage = "en";
    public string SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            SetProperty(ref _selectedLanguage, value);
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

    #endregion

    #region Theme Settings

    private string _selectedTheme = "Dark";
    public string SelectedTheme
    {
        get => _selectedTheme;
        set => SetProperty(ref _selectedTheme, value);
    }

    public bool IsDarkTheme
    {
        get => SelectedTheme == "Dark";
        set { if (value) SelectedTheme = "Dark"; }
    }

    public bool IsLightTheme
    {
        get => SelectedTheme == "Light";
        set { if (value) SelectedTheme = "Light"; }
    }

    #endregion

    #region Currency Settings

    private string _selectedCurrency = "USD";
    public string SelectedCurrency
    {
        get => _selectedCurrency;
        set => SetProperty(ref _selectedCurrency, value);
    }

    public bool IsUsdCurrency
    {
        get => SelectedCurrency == "USD";
        set { if (value) SelectedCurrency = "USD"; }
    }

    public bool IsEurCurrency
    {
        get => SelectedCurrency == "EUR";
        set { if (value) SelectedCurrency = "EUR"; }
    }

    public string ExchangeRateInfo => _currency.GetExchangeRateInfo();

    #endregion

    #region Server Settings

    private string _serverAddress = "localhost";
    public string ServerAddress
    {
        get => _serverAddress;
        set => SetProperty(ref _serverAddress, value);
    }

    private int _serverPort = 5002;
    public int ServerPort
    {
        get => _serverPort;
        set => SetProperty(ref _serverPort, value);
    }

    private bool _autoConnect = true;
    public bool AutoConnect
    {
        get => _autoConnect;
        set => SetProperty(ref _autoConnect, value);
    }

    #endregion

    #region Notification Settings

    private bool _priceAlertsEnabled = true;
    public bool PriceAlertsEnabled
    {
        get => _priceAlertsEnabled;
        set => SetProperty(ref _priceAlertsEnabled, value);
    }

    private bool _soundEnabled = true;
    public bool SoundEnabled
    {
        get => _soundEnabled;
        set => SetProperty(ref _soundEnabled, value);
    }

    #endregion

    #region Status

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

    #endregion

    public void LoadSettings()
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

    public async Task SaveSettingsAsync()
    {
        IsLoading = true;
        StatusMessage = "";

        await Task.Run(() =>
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
        });

        _lang.CurrentLanguage = SelectedLanguage;
        _currency.CurrentCurrency = SelectedCurrency;
        
        // Refresh exchange rates when currency changes
        await _currency.RefreshExchangeRatesAsync();
        
        _api.UpdateBaseAddress();

        IsLoading = false;
        IsSuccess = true;
        StatusMessage = _lang["SettingsSaved"];
        
        OnPropertyChanged(nameof(ExchangeRateInfo));
    }

    public async Task ResetSettingsAsync()
    {
        IsLoading = true;
        StatusMessage = "";

        await Task.Run(() =>
        {
            _settings.ResetToDefaults();
        });

        LoadSettings();
        _lang.CurrentLanguage = SelectedLanguage;
        _currency.CurrentCurrency = SelectedCurrency;

        IsLoading = false;
        IsSuccess = true;
        StatusMessage = _lang["SettingsSaved"];
    }
}
