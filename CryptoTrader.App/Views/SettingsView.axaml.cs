using Avalonia.Controls;
using Avalonia.Interactivity;
using CryptoTrader.App.ViewModels;

namespace CryptoTrader.App.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
        DataContext = new SettingsViewModel();
    }

    private async void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
        {
            await vm.SaveSettingsAsync();
        }
    }

    private async void OnResetClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
        {
            await vm.ResetSettingsAsync();
        }
    }
}
