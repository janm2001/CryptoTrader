using Avalonia.Controls;
using Avalonia.Interactivity;
using CryptoTrader.App.Models;
using CryptoTrader.App.ViewModels;
using CryptoTrader.App.Services;

namespace CryptoTrader.App.Views;

public partial class AdminView : UserControl
{
    public AdminView()
    {
        InitializeComponent();
        DataContext = new AdminViewModel();

        Loaded += async (s, e) =>
        {
            if (DataContext is AdminViewModel vm)
            {
                await vm.LoadDataAsync();
            }
        };
    }

    private async void OnRefreshPricesClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is AdminViewModel vm)
        {
            await vm.RefreshPricesAsync();
        }
    }

    private async void OnRefreshClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is AdminViewModel vm)
        {
            await vm.LoadDataAsync();
        }
    }

    private async void OnToggleAdminClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is UserInfo user && DataContext is AdminViewModel vm)
        {
            await vm.ToggleUserAdminAsync(user);
        }
    }

    private async void OnDeleteUserClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is UserInfo user && DataContext is AdminViewModel vm)
        {
            await vm.DeleteUserAsync(user);
        }
    }
}
