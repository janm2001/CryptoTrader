using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CryptoTrader.App.ViewModels;
using CryptoTrader.Shared.Models;

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
    
    private async void OnExportExcel(object? sender, RoutedEventArgs e) => await ViewModel.ExportPricesAsync("excel");
    private async void OnExportXml(object? sender, RoutedEventArgs e) => await ViewModel.ExportPricesAsync("xml");
    private async void OnExportPdf(object? sender, RoutedEventArgs e) => await ViewModel.ExportPricesAsync("pdf");
    private async void OnExportBinary(object? sender, RoutedEventArgs e) => await ViewModel.ExportPricesAsync("binary");

    private void OnCryptoDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (CryptoGrid.SelectedItem is CryptoCurrency crypto)
        {
            var detailWindow = new CryptoDetailWindow(crypto);
            var parentWindow = TopLevel.GetTopLevel(this) as Window;
            if (parentWindow != null)
            {
                detailWindow.Show(parentWindow);
            }
            else
            {
                detailWindow.Show();
            }
        }
    }
}
