using Avalonia.Controls;
using Avalonia.Interactivity;
using CryptoTrader.App.ViewModels;

namespace CryptoTrader.App.Views;

public partial class MainAppWindow : Window
{
    private MainAppViewModel ViewModel => (MainAppViewModel)DataContext!;
    private readonly string _username;

    public MainAppWindow(string username)
    {
        InitializeComponent();
        _username = username;
        DataContext = new MainAppViewModel(username);
        
        // Load dashboard by default
        Loaded += (s, e) => NavigateToDashboard(null, null!);
    }

    private void NavigateToDashboard(object? sender, RoutedEventArgs e)
    {
        ContentArea.Content = new DashboardView();
        UpdateNavSelection("Dashboard");
    }

    private void NavigateToPortfolio(object? sender, RoutedEventArgs e)
    {
        ContentArea.Content = new PortfolioView();
        UpdateNavSelection("Portfolio");
    }

    private void NavigateToMarket(object? sender, RoutedEventArgs e)
    {
        ContentArea.Content = new MarketView();
        UpdateNavSelection("Market");
    }

    private void NavigateToTransactions(object? sender, RoutedEventArgs e)
    {
        ContentArea.Content = new TransactionsView();
        UpdateNavSelection("Transactions");
    }

    private void NavigateToSettings(object? sender, RoutedEventArgs e)
    {
        ContentArea.Content = new SettingsView();
        UpdateNavSelection("Settings");
    }

    private void NavigateToAdmin(object? sender, RoutedEventArgs e)
    {
        ContentArea.Content = new AdminView();
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
