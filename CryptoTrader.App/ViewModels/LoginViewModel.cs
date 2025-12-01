using System.Threading.Tasks;
using CryptoTrader.Shared.Models;
using CryptoTrader.App.Services;

namespace CryptoTrader.App.ViewModels;

public class LoginViewModel : ViewModelBase
{
    private readonly ApiClient _api;
    private readonly LanguageService _lang;
    private readonly SettingsService _settings;

    public LoginViewModel()
    {
        _api = new ApiClient();
        _lang = LanguageService.Instance;
        _settings = SettingsService.Instance;

        _lang.LanguageChanged += (s, e) => OnPropertyChanged(nameof(L));
    }

    public LanguageService L => _lang;

    // Login form
    private string _loginUsername = "";
    public string LoginUsername
    {
        get => _loginUsername;
        set => SetProperty(ref _loginUsername, value);
    }

    private string _loginPassword = "";
    public string LoginPassword
    {
        get => _loginPassword;
        set => SetProperty(ref _loginPassword, value);
    }

    private bool _loginRememberMe;
    public bool LoginRememberMe
    {
        get => _loginRememberMe;
        set => SetProperty(ref _loginRememberMe, value);
    }

    // Register form
    private string _registerUsername = "";
    public string RegisterUsername
    {
        get => _registerUsername;
        set => SetProperty(ref _registerUsername, value);
    }

    private string _registerEmail = "";
    public string RegisterEmail
    {
        get => _registerEmail;
        set => SetProperty(ref _registerEmail, value);
    }

    private string _registerPassword = "";
    public string RegisterPassword
    {
        get => _registerPassword;
        set => SetProperty(ref _registerPassword, value);
    }

    private string _registerConfirmPassword = "";
    public string RegisterConfirmPassword
    {
        get => _registerConfirmPassword;
        set => SetProperty(ref _registerConfirmPassword, value);
    }

    private bool _showRegisterForm;
    public bool ShowRegisterForm
    {
        get => _showRegisterForm;
        set { SetProperty(ref _showRegisterForm, value); OnPropertyChanged(nameof(ShowLoginForm)); }
    }

    public bool ShowLoginForm => !ShowRegisterForm;

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

    // Result after successful login
    public UserSession? LoggedInSession { get; private set; }
    public bool LoginSuccessful { get; private set; }

    public async Task<bool> TryAutoLoginAsync()
    {
        if (_settings.RememberMe && !string.IsNullOrEmpty(_settings.SavedToken))
        {
            IsLoading = true;
            var result = await _api.ValidateTokenAsync(_settings.SavedToken);
            IsLoading = false;

            if (result.Success && result.Session != null)
            {
                LoggedInSession = result.Session;
                LoginSuccessful = true;
                return true;
            }
        }
        return false;
    }

    public async Task<bool> LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(LoginUsername) || string.IsNullOrWhiteSpace(LoginPassword))
        {
            StatusMessage = _lang["Required"];
            return false;
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
            LoggedInSession = result.Session;
            LoginSuccessful = true;
            LoginPassword = "";
            return true;
        }
        else
        {
            StatusMessage = result.Message ?? _lang["LoginFailed"];
            return false;
        }
    }

    public async Task<bool> RegisterAsync()
    {
        if (string.IsNullOrWhiteSpace(RegisterUsername) || RegisterUsername.Length < 3)
        {
            StatusMessage = _lang["UsernameTooShort"];
            return false;
        }

        if (string.IsNullOrWhiteSpace(RegisterEmail) || !RegisterEmail.Contains('@'))
        {
            StatusMessage = _lang["InvalidEmail"];
            return false;
        }

        if (string.IsNullOrWhiteSpace(RegisterPassword) || RegisterPassword.Length < 6)
        {
            StatusMessage = _lang["PasswordTooShort"];
            return false;
        }

        if (RegisterPassword != RegisterConfirmPassword)
        {
            StatusMessage = _lang["PasswordsDontMatch"];
            return false;
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
            LoggedInSession = result.Session;
            LoginSuccessful = true;
            RegisterPassword = "";
            RegisterConfirmPassword = "";
            return true;
        }
        else
        {
            StatusMessage = result.Message ?? _lang["Error"];
            return false;
        }
    }
}
