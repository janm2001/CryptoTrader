using Avalonia.Controls;
using Avalonia.Interactivity;
using CryptoTrader.App.ViewModels;

namespace CryptoTrader.App.Views;

public partial class TransactionsView : UserControl
{
    private TransactionsViewModel ViewModel => (TransactionsViewModel)DataContext!;

    public TransactionsView()
    {
        InitializeComponent();
        DataContext = new TransactionsViewModel();
    }

    private async void OnRefresh(object? sender, RoutedEventArgs e) => await ViewModel.LoadDataAsync();
    private async void OnExportExcel(object? sender, RoutedEventArgs e) => await ViewModel.ExportTransactionsAsync("excel");
    private async void OnExportXml(object? sender, RoutedEventArgs e) => await ViewModel.ExportTransactionsAsync("xml");
    private async void OnExportPdf(object? sender, RoutedEventArgs e) => await ViewModel.ExportTransactionsAsync("pdf");
    private async void OnExportBinary(object? sender, RoutedEventArgs e) => await ViewModel.ExportTransactionsAsync("binary");
}
