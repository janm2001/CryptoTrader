using System.Linq;
using System.Threading.Tasks;
using CryptoTrader.App.Services;

namespace CryptoTrader.App.ViewModels;

public class MainAppViewModel : ViewModelBase
{
    private readonly ApiClient _api;
    private readonly LanguageService _lang;

    public MainAppViewModel(string username)
    {
        _api = new ApiClient();
        _lang = LanguageService.Instance;
        CurrentUsername = username;

        // Use shared auth token
        var token = NavigationService.Instance.AuthToken;
        if (!string.IsNullOrEmpty(token))
        {
            _api.SetAuthToken(token);
        }

        _lang.LanguageChanged += (s, e) => OnPropertyChanged(nameof(L));

        // Check if admin from session
        CheckUserRole();
    }

    public LanguageService L => _lang;

    private string _currentUsername = "";
    public string CurrentUsername
    {
        get => _currentUsername;
        set => SetProperty(ref _currentUsername, value);
    }

    private bool _isAdmin;
    public bool IsAdmin
    {
        get => _isAdmin;
        set => SetProperty(ref _isAdmin, value);
    }

    private void CheckUserRole()
    {
        // Get from current session if available
        var session = _api.CurrentSession;
        if (session != null)
        {
            IsAdmin = session.IsAdmin;
        }
        else
        {
            // Fallback to NavigationService
            IsAdmin = NavigationService.Instance.IsAdmin;
        }
    }

    public async Task LogoutAsync()
    {
        await _api.LogoutAsync();
        NavigationService.Instance.ClearSession();
    }
}
