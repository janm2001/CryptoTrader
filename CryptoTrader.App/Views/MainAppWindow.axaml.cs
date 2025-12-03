using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CryptoTrader.App.ViewModels;

namespace CryptoTrader.App.Views;

public partial class MainAppWindow : Window
{
    private MainAppViewModel ViewModel => (MainAppViewModel)DataContext!;
    private readonly string _username;
    private string _currentView = "Dashboard";

    public MainAppWindow(string username)
    {
        InitializeComponent();
        _username = username;
        DataContext = new MainAppViewModel(username);
        
        // Load dashboard by default
        Loaded += (s, e) => NavigateToDashboard(null, null!);
    }

    /// <summary>
    /// Refreshes the sidebar after settings change
    /// </summary>
    public async Task RefreshSidebarAsync()
    {
        // Refresh the ViewModel's language bindings
        ViewModel.OnPropertyChanged(nameof(ViewModel.L));
        
        // Reload profile picture
        await ViewModel.LoadProfilePictureAsync();
        
        // Refresh the current view to apply new settings
        RefreshCurrentView();
    }

    /// <summary>
    /// Refreshes just the profile picture in sidebar
    /// </summary>
    public async Task RefreshProfilePictureAsync()
    {
        await ViewModel.LoadProfilePictureAsync();
    }

    /// <summary>
    /// Refreshes the current view to apply new settings
    /// </summary>
    private void RefreshCurrentView()
    {
        switch (_currentView)
        {
            case "Dashboard":
                ContentArea.Content = new DashboardView();
                break;
            case "Portfolio":
                ContentArea.Content = new PortfolioView();
                break;
            case "Market":
                ContentArea.Content = new MarketView();
                break;
            case "Transactions":
                ContentArea.Content = new TransactionsView();
                break;
            case "Settings":
                ContentArea.Content = new SettingsView();
                break;
            case "Admin":
                ContentArea.Content = new AdminView();
                break;
        }
    }

    private void NavigateToDashboard(object? sender, RoutedEventArgs e)
    {
        ContentArea.Content = new DashboardView();
        _currentView = "Dashboard";
        UpdateNavSelection("Dashboard");
    }

    private void NavigateToPortfolio(object? sender, RoutedEventArgs e)
    {
        ContentArea.Content = new PortfolioView();
        _currentView = "Portfolio";
        UpdateNavSelection("Portfolio");
    }

    private void NavigateToMarket(object? sender, RoutedEventArgs e)
    {
        ContentArea.Content = new MarketView();
        _currentView = "Market";
        UpdateNavSelection("Market");
    }

    private void NavigateToTransactions(object? sender, RoutedEventArgs e)
    {
        ContentArea.Content = new TransactionsView();
        _currentView = "Transactions";
        UpdateNavSelection("Transactions");
    }

    private void NavigateToSettings(object? sender, RoutedEventArgs e)
    {
        ContentArea.Content = new SettingsView();
        _currentView = "Settings";
        UpdateNavSelection("Settings");
    }

    private void NavigateToAdmin(object? sender, RoutedEventArgs e)
    {
        ContentArea.Content = new AdminView();
        _currentView = "Admin";
        UpdateNavSelection("Admin");
    }

    private void UpdateNavSelection(string selected)
    {
        // Remove active class from all nav buttons
        NavDashboard.Classes.Remove("active");
        NavPortfolio.Classes.Remove("active");
        NavMarket.Classes.Remove("active");
        NavTransactions.Classes.Remove("active");
        NavSettings.Classes.Remove("active");
        NavAdmin.Classes.Remove("active");

        // Add active class to selected
        switch (selected)
        {
            case "Dashboard": NavDashboard.Classes.Add("active"); break;
            case "Portfolio": NavPortfolio.Classes.Add("active"); break;
            case "Market": NavMarket.Classes.Add("active"); break;
            case "Transactions": NavTransactions.Classes.Add("active"); break;
            case "Settings": NavSettings.Classes.Add("active"); break;
            case "Admin": NavAdmin.Classes.Add("active"); break;
        }
    }

    private async void OnLogout(object? sender, RoutedEventArgs e)
    {
        await ViewModel.LogoutAsync();
        var loginWindow = new LoginWindow();
        loginWindow.Show();
        Close();
    }
}
