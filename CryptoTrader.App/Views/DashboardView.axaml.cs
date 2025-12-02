using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CryptoTrader.App.ViewModels;
using CryptoTrader.Shared.Models;

namespace CryptoTrader.App.Views;

public partial class DashboardView : UserControl
{
    private DashboardViewModel ViewModel => (DashboardViewModel)DataContext!;

    public DashboardView()
    {
        InitializeComponent();
        DataContext = new DashboardViewModel();
    }

    private async void OnRefresh(object? sender, RoutedEventArgs e) => await ViewModel.LoadDataAsync();
    private async void OnExport(object? sender, RoutedEventArgs e) => await ViewModel.ExportPricesToExcelAsync();

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
