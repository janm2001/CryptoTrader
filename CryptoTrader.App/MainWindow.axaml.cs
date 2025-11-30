using Avalonia.Controls;
using Avalonia.Interactivity;
using CryptoTrader.App.ViewModels;

namespace CryptoTrader.App;

public partial class MainWindow : Window
{
    private MainViewModel ViewModel => (MainViewModel)DataContext!;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }

    // Navigation
    private void NavigateToDashboard(object? sender, RoutedEventArgs e) => ViewModel.NavigateTo("Dashboard");
    private void NavigateToPortfolio(object? sender, RoutedEventArgs e) => ViewModel.NavigateTo("Portfolio");
    private void NavigateToMarket(object? sender, RoutedEventArgs e) => ViewModel.NavigateTo("Market");
    private void NavigateToTransactions(object? sender, RoutedEventArgs e) => ViewModel.NavigateTo("Transactions");
    private void NavigateToSettings(object? sender, RoutedEventArgs e) => ViewModel.NavigateTo("Settings");
    private void NavigateToAdmin(object? sender, RoutedEventArgs e) => ViewModel.NavigateTo("Admin");

    // Auth
    private async void OnLogin(object? sender, RoutedEventArgs e) => await ViewModel.LoginAsync();
    private async void OnRegister(object? sender, RoutedEventArgs e) => await ViewModel.RegisterAsync();
    private async void OnLogout(object? sender, RoutedEventArgs e) => await ViewModel.LogoutAsync();
    private void ShowLogin(object? sender, RoutedEventArgs e) => ViewModel.ShowRegisterForm = false;
    private void ShowRegister(object? sender, RoutedEventArgs e) => ViewModel.ShowRegisterForm = true;

    // Dashboard
    private async void OnRefreshDashboard(object? sender, RoutedEventArgs e) => await ViewModel.LoadDashboardDataAsync();

    // Portfolio
    private void OnShowAddHolding(object? sender, RoutedEventArgs e) => ViewModel.ShowAddHoldingForm = true;
    private async void OnAddHolding(object? sender, RoutedEventArgs e) => await ViewModel.AddHoldingAsync();
    private void OnCancelAddHolding(object? sender, RoutedEventArgs e) => ViewModel.ShowAddHoldingForm = false;
    private async void OnRefreshPortfolio(object? sender, RoutedEventArgs e) => await ViewModel.LoadPortfolioDataAsync();
    private async void OnExportHoldings(object? sender, RoutedEventArgs e) => await ViewModel.ExportHoldingsToExcelAsync();

    // Market
    private async void OnSearchCrypto(object? sender, RoutedEventArgs e) => await ViewModel.SearchCryptosAsync();
    private async void OnRefreshMarket(object? sender, RoutedEventArgs e) => await ViewModel.LoadMarketDataAsync();
    private async void OnExportPrices(object? sender, RoutedEventArgs e) => await ViewModel.ExportPricesToExcelAsync();

    // Transactions
    private void OnShowAddTransaction(object? sender, RoutedEventArgs e) => ViewModel.ShowAddTransactionForm = true;
    private async void OnAddTransaction(object? sender, RoutedEventArgs e) => await ViewModel.AddTransactionAsync();
    private void OnCancelAddTransaction(object? sender, RoutedEventArgs e) => ViewModel.ShowAddTransactionForm = false;
    private async void OnRefreshTransactions(object? sender, RoutedEventArgs e) => await ViewModel.LoadTransactionsAsync();
    private async void OnExportTransactions(object? sender, RoutedEventArgs e) => await ViewModel.ExportTransactionsToExcelAsync();

    // Settings
    private void OnSaveSettings(object? sender, RoutedEventArgs e) => ViewModel.SaveSettingsFromUI();
    private void OnResetSettings(object? sender, RoutedEventArgs e) => ViewModel.ResetSettingsToDefaults();

    // Admin
    private async void OnRefreshAdmin(object? sender, RoutedEventArgs e) => await ViewModel.LoadAdminDataAsync();
    private async void OnAdminRefreshPrices(object? sender, RoutedEventArgs e) => await ViewModel.AdminRefreshPricesAsync();
    private async void OnExportAdminReport(object? sender, RoutedEventArgs e) => await ViewModel.ExportAdminReportAsync();
}