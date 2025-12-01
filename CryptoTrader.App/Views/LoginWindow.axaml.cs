using Avalonia.Controls;
using Avalonia.Interactivity;
using CryptoTrader.App.ViewModels;
using CryptoTrader.App.Services;

namespace CryptoTrader.App.Views;

public partial class LoginWindow : Window
{
    private LoginViewModel ViewModel => (LoginViewModel)DataContext!;

    public LoginWindow()
    {
        InitializeComponent();
        DataContext = new LoginViewModel();
        
        // Try auto-login when window loads
        Loaded += async (s, e) =>
        {
            if (await ViewModel.TryAutoLoginAsync())
            {
                OpenMainWindow();
            }
        };
    }

    private async void OnLogin(object? sender, RoutedEventArgs e)
    {
        if (await ViewModel.LoginAsync())
        {
            OpenMainWindow();
        }
    }

    private async void OnRegister(object? sender, RoutedEventArgs e)
    {
        if (await ViewModel.RegisterAsync())
        {
            OpenMainWindow();
        }
    }

    private void ShowLogin(object? sender, RoutedEventArgs e) => ViewModel.ShowRegisterForm = false;
    private void ShowRegister(object? sender, RoutedEventArgs e) => ViewModel.ShowRegisterForm = true;

    private void OpenMainWindow()
    {
        var session = ViewModel.LoggedInSession;
        if (session != null)
        {
            // Store auth token in NavigationService for shared access
            var nav = NavigationService.Instance;
            nav.SetUserInfo(session.Username, session.IsAdmin, session.Token);
            
            var mainWindow = new MainAppWindow(session.Username);
            mainWindow.Show();
            Close();
        }
    }
}
