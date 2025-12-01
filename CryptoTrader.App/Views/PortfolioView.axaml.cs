using Avalonia.Controls;
using Avalonia.Interactivity;
using CryptoTrader.App.ViewModels;

namespace CryptoTrader.App.Views;

public partial class PortfolioView : UserControl
{
    private PortfolioViewModel ViewModel => (PortfolioViewModel)DataContext!;

    public PortfolioView()
    {
        InitializeComponent();
        DataContext = new PortfolioViewModel();
    }

    private void OnShowAddHolding(object? sender, RoutedEventArgs e) => ViewModel.ShowAddHoldingForm = true;
    private async void OnAddHolding(object? sender, RoutedEventArgs e) => await ViewModel.AddHoldingAsync();
    private void OnCancelAddHolding(object? sender, RoutedEventArgs e) => ViewModel.ShowAddHoldingForm = false;
    private async void OnRefresh(object? sender, RoutedEventArgs e) => await ViewModel.LoadDataAsync();
    private async void OnExport(object? sender, RoutedEventArgs e) => await ViewModel.ExportHoldingsAsync();
}
