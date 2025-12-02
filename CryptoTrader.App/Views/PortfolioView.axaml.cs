using Avalonia.Controls;
using Avalonia.Interactivity;
using CryptoTrader.App.ViewModels;
using CryptoTrader.App.Services;

namespace CryptoTrader.App.Views;

public partial class PortfolioView : UserControl
{
    private PortfolioViewModel ViewModel => (PortfolioViewModel)DataContext!;

    public PortfolioView()
    {
        InitializeComponent();
        DataContext = new PortfolioViewModel();
    }

    private async void OnBuyClick(object? sender, RoutedEventArgs e)
    {
        var buyWindow = new BuyWindow();

        // Show as dialog
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is Window parentWindow)
        {
            await buyWindow.ShowDialog(parentWindow);
            
            // Refresh data if purchase was made
            if (buyWindow.PurchaseCompleted)
            {
                await ViewModel.LoadDataAsync();
            }
        }
    }

    private async void OnSellClick(object? sender, RoutedEventArgs e)
    {
        var sellWindow = new SellWindow();

        // Show as dialog
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is Window parentWindow)
        {
            await sellWindow.ShowDialog(parentWindow);
            
            // Refresh data if sale was made
            if (sellWindow.SaleCompleted)
            {
                await ViewModel.LoadDataAsync();
            }
        }
    }

    private async void OnRefresh(object? sender, RoutedEventArgs e) => await ViewModel.LoadDataAsync();
    private async void OnExportExcel(object? sender, RoutedEventArgs e) => await ViewModel.ExportHoldingsAsync("excel");
    private async void OnExportXml(object? sender, RoutedEventArgs e) => await ViewModel.ExportHoldingsAsync("xml");
    private async void OnExportPdf(object? sender, RoutedEventArgs e) => await ViewModel.ExportHoldingsAsync("pdf");
    private async void OnExportBinary(object? sender, RoutedEventArgs e) => await ViewModel.ExportHoldingsAsync("binary");
}
