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

    private void OnShowAddTransaction(object? sender, RoutedEventArgs e) => ViewModel.ShowAddTransactionForm = true;
    private async void OnAddTransaction(object? sender, RoutedEventArgs e) => await ViewModel.AddTransactionAsync();
    private void OnCancelAddTransaction(object? sender, RoutedEventArgs e) => ViewModel.ShowAddTransactionForm = false;
    private async void OnRefresh(object? sender, RoutedEventArgs e) => await ViewModel.LoadDataAsync();
    private async void OnExport(object? sender, RoutedEventArgs e) => await ViewModel.ExportTransactionsAsync();
}
