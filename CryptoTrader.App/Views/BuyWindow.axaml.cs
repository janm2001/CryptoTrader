using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CryptoTrader.App.Services;
using CryptoTrader.Shared.Models;

namespace CryptoTrader.App.Views;

public partial class BuyWindow : Window
{
    private readonly ApiClient _api;
    private decimal _balance;
    private List<CryptoCurrency> _cryptos = new();
    private CryptoCurrency? _selectedCrypto;
    private decimal _amount;

    public bool PurchaseCompleted { get; private set; }
    public decimal NewBalance { get; private set; }

    public BuyWindow()
    {
        InitializeComponent();
        _api = new ApiClient();
        
        // Use shared auth token
        var token = NavigationService.Instance.AuthToken;
        if (!string.IsNullOrEmpty(token))
        {
            _api.SetAuthToken(token);
        }
        
        Loaded += async (s, e) => await LoadDataAsync();
    }

    public void SetAuthToken(string token)
    {
        _api.SetAuthToken(token);
    }

    private async System.Threading.Tasks.Task LoadDataAsync()
    {
        try
        {
            // Load balance
            _balance = await _api.GetBalanceAsync();
            BalanceText.Text = $"${_balance:N2}";

            // Load top cryptos
            _cryptos = await _api.GetTopCryptosAsync(20);
            
            var options = _cryptos.Select(c => new CryptoOption
            {
                CoinId = c.CoinId,
                Symbol = c.Symbol,
                Name = c.Name,
                CurrentPrice = c.CurrentPrice
            }).ToList();

            CryptoComboBox.ItemsSource = options;
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error loading data: {ex.Message}";
        }
    }

    private void OnCryptoSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (CryptoComboBox.SelectedItem is CryptoOption option)
        {
            _selectedCrypto = _cryptos.FirstOrDefault(c => c.CoinId == option.CoinId);
            if (_selectedCrypto != null)
            {
                CurrentPriceText.Text = $"${_selectedCrypto.CurrentPrice:N2}";
                PricePanel.IsVisible = true;
                UpdateTotalCost();
            }
        }
    }

    private void OnAmountChanged(object? sender, TextChangedEventArgs e)
    {
        if (decimal.TryParse(AmountTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
        {
            _amount = amount;
        }
        else
        {
            _amount = 0;
        }
        UpdateTotalCost();
    }

    private void UpdateTotalCost()
    {
        if (_selectedCrypto == null || _amount <= 0)
        {
            TotalCostText.Text = "$0.00";
            ReceiveText.Text = "0";
            BuyButton.IsEnabled = false;
            StatusText.Text = "";
            return;
        }

        var totalCost = _amount * _selectedCrypto.CurrentPrice;
        TotalCostText.Text = $"${totalCost:N2}";
        ReceiveText.Text = $"{_amount:N8} {_selectedCrypto.Symbol.ToUpper()}";

        if (totalCost > _balance)
        {
            StatusText.Text = "Insufficient balance!";
            StatusText.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FF6B6B"));
            BuyButton.IsEnabled = false;
        }
        else if (_amount > 0)
        {
            StatusText.Text = "";
            BuyButton.IsEnabled = true;
        }
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void OnBuyClick(object? sender, RoutedEventArgs e)
    {
        if (_selectedCrypto == null || _amount <= 0) return;

        BuyButton.IsEnabled = false;
        StatusText.Text = "Processing purchase...";
        StatusText.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#F0B90B"));

        var result = await _api.BuyCryptoAsync(
            _selectedCrypto.CoinId,
            _selectedCrypto.Symbol,
            _amount,
            _selectedCrypto.CurrentPrice
        );

        if (result.Success)
        {
            PurchaseCompleted = true;
            NewBalance = result.Balance;
            StatusText.Text = "Purchase successful!";
            StatusText.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#4ECB71"));
            
            await System.Threading.Tasks.Task.Delay(1000);
            Close();
        }
        else
        {
            StatusText.Text = result.Message ?? "Purchase failed";
            StatusText.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FF6B6B"));
            BuyButton.IsEnabled = true;
        }
    }
}
