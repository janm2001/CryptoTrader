using System;
using Avalonia.Controls;

namespace CryptoTrader.App.Services;

/// <summary>
/// Service for managing navigation between views
/// </summary>
public class NavigationService
{
    private static NavigationService? _instance;
    public static NavigationService Instance => _instance ??= new NavigationService();

    public event EventHandler<string>? NavigationRequested;
    public event EventHandler? LogoutRequested;

    public string CurrentUsername { get; set; } = "";
    public bool IsAdmin { get; set; }

    public void NavigateTo(string viewName)
    {
        NavigationRequested?.Invoke(this, viewName);
    }

    public void RequestLogout()
    {
        CurrentUsername = "";
        IsAdmin = false;
        LogoutRequested?.Invoke(this, EventArgs.Empty);
    }

    public void SetUserInfo(string username, bool isAdmin)
    {
        CurrentUsername = username;
        IsAdmin = isAdmin;
    }
}
