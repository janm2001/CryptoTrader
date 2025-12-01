using Avalonia.Controls;
using Avalonia.Interactivity;
using CryptoTrader.App.ViewModels;

namespace CryptoTrader.App.Views;

public partial class MarketView : UserControl
{
    private MarketViewModel ViewModel => (MarketViewModel)DataContext!;

    public MarketView()
    {
        InitializeComponent();
        DataContext = new MarketViewModel();
    }

    private async void OnSearch(object? sender, RoutedEventArgs e) => await ViewModel.SearchAsync();
    private async void OnRefresh(object? sender, RoutedEventArgs e) => await ViewModel.LoadDataAsync();
    private async void OnExport(object? sender, RoutedEventArgs e) => await ViewModel.ExportPricesAsync();
}
