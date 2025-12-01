using System.Threading.Tasks;
using CryptoTrader.App.Services;

namespace CryptoTrader.App.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly LanguageService _lang;
    private readonly SettingsService _settings;
    private readonly ApiClient _api;

    public SettingsViewModel()
    {
        _lang = LanguageService.Instance;
        _settings = SettingsService.Instance;
        _api = new ApiClient();

        _lang.LanguageChanged += (s, e) => OnPropertyChanged(nameof(L));

        // Load current settings to UI
        LoadSettings();
    }

    public LanguageService L => _lang;

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
        _api.UpdateBaseAddress();

        IsLoading = false;
        IsSuccess = true;
        StatusMessage = _lang["SettingsSaved"];
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

        IsLoading = false;
        IsSuccess = true;
        StatusMessage = _lang["SettingsSaved"];
    }
}
