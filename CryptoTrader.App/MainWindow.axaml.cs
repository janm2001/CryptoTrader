using Avalonia.Controls;
using CryptoTrader.App.ViewModels;

namespace CryptoTrader.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}