using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CryptoTrader.App.Services;
using Avalonia.Threading;

namespace CryptoTrader.App.ViewModels;

public class AdminViewModel : ViewModelBase
{
    private readonly LanguageService _lang;
    private readonly ApiClient _api;
    private readonly NavigationService _nav;

    public AdminViewModel()
    {
        _lang = LanguageService.Instance;
        _api = new ApiClient();
        _nav = NavigationService.Instance;

        // Use shared auth token
        var token = _nav.AuthToken;
        if (!string.IsNullOrEmpty(token))
        {
            _api.SetAuthToken(token);
        }

        Users = new ObservableCollection<UserInfo>();

        _lang.LanguageChanged += (s, e) => OnPropertyChanged(nameof(L));
    }

    public LanguageService L => _lang;

    public ObservableCollection<UserInfo> Users { get; }

    private int _totalUsers;
    public int TotalUsers
    {
        get => _totalUsers;
        set => SetProperty(ref _totalUsers, value);
    }

    private int _totalTransactions;
    public int TotalTransactions
    {
        get => _totalTransactions;
        set => SetProperty(ref _totalTransactions, value);
    }

    private int _totalHoldings;
    public int TotalHoldings
    {
        get => _totalHoldings;
        set => SetProperty(ref _totalHoldings, value);
    }

    private int _trackedCryptos;
    public int TrackedCryptos
    {
        get => _trackedCryptos;
        set => SetProperty(ref _trackedCryptos, value);
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

    public async Task LoadDataAsync()
    {
        IsLoading = true;
        StatusMessage = "";

        try
        {
            // Load users
            var users = await _api.GetAllUsersAsync();
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Users.Clear();
                if (users != null)
                {
                    foreach (var user in users)
                        Users.Add(user);
                }
                TotalUsers = Users.Count;
            });

            // Load stats
            var stats = await _api.GetSystemStatsAsync();
            if (stats != null)
            {
                TotalTransactions = stats.TotalTransactions;
                TotalHoldings = stats.TotalHoldings;
                TrackedCryptos = stats.TrackedCryptos;
            }
        }
        catch (Exception ex)
        {
            IsSuccess = false;
            StatusMessage = $"Error loading data: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task RefreshPricesAsync()
    {
        IsLoading = true;
        StatusMessage = "";

        try
        {
            var success = await _api.RefreshCryptoPricesAsync();
            if (success)
            {
                IsSuccess = true;
                StatusMessage = _lang["PricesRefreshed"];
            }
            else
            {
                IsSuccess = false;
                StatusMessage = "Failed to refresh prices";
            }
        }
        catch (Exception ex)
        {
            IsSuccess = false;
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task ToggleUserAdminAsync(UserInfo user)
    {
        if (user == null) return;

        IsLoading = true;
        StatusMessage = "";

        try
        {
            var newRole = user.Role == "Admin" ? "User" : "Admin";
            var success = await _api.ChangeUserRoleAsync(user.Id, newRole);
            if (success)
            {
                user.Role = newRole;
                IsSuccess = true;
                StatusMessage = $"User {user.Username} role updated";
                // Reload to refresh list
                await LoadDataAsync();
            }
            else
            {
                IsSuccess = false;
                StatusMessage = "Failed to update user";
            }
        }
        catch (Exception ex)
        {
            IsSuccess = false;
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task DeleteUserAsync(UserInfo user)
    {
        if (user == null) return;

        // Don't allow deleting yourself
        if (user.Username == _nav.CurrentUsername)
        {
            IsSuccess = false;
            StatusMessage = "Cannot delete your own account";
            return;
        }

        IsLoading = true;
        StatusMessage = "";

        try
        {
            var success = await _api.ChangeUserStatusAsync(user.Id, false);
            if (success)
            {
                Users.Remove(user);
                TotalUsers = Users.Count;
                IsSuccess = true;
                StatusMessage = $"User {user.Username} deactivated";
            }
            else
            {
                IsSuccess = false;
                StatusMessage = "Failed to deactivate user";
            }
        }
        catch (Exception ex)
        {
            IsSuccess = false;
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
