using Avalonia.Controls;
using Avalonia.Interactivity;
using CryptoTrader.App.ViewModels;

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
}
