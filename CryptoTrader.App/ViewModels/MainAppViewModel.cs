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

        _lang.LanguageChanged += (s, e) => OnPropertyChanged(nameof(L));

        // Check if admin
        _ = CheckUserRoleAsync();
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

    private async Task CheckUserRoleAsync()
    {
        var users = await _api.GetAllUsersAsync();
        var currentUser = users.FirstOrDefault(u => u.Username == CurrentUsername);
        IsAdmin = currentUser?.Role == "Admin";
    }

    public async Task LogoutAsync()
    {
        await _api.LogoutAsync();
    }
}
