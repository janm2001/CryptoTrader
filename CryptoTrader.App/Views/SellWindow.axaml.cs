using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CryptoTrader.App.Services;
using CryptoTrader.Shared.Models;

namespace CryptoTrader.App.Views;

public class HoldingOption
{
    public CryptoHolding Holding { get; set; } = null!;
    public decimal CurrentPrice { get; set; }
    public override string ToString() => $"{Holding.Symbol.ToUpper()} - {Holding.Amount:N8} (${Holding.Amount * CurrentPrice:N2})";
}

public partial class SellWindow : Window
{
    private readonly ApiClient _api;
    private decimal _balance;
    private List<HoldingOption> _holdingOptions = new();
    private HoldingOption? _selectedHolding;
    private decimal _amount;

    public bool SaleCompleted { get; private set; }
    public decimal NewBalance { get; private set; }

    public SellWindow()
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

            // Load holdings
            var holdings = await _api.GetHoldingsAsync();
            var cryptos = await _api.GetTopCryptosAsync(50);
            var priceDict = cryptos.ToDictionary(c => c.CoinId, c => c.CurrentPrice);

            _holdingOptions = holdings.Select(h => new HoldingOption
            {
                Holding = h,
                CurrentPrice = priceDict.GetValueOrDefault(h.CoinId, h.PurchasePrice)
            }).Where(h => h.Holding.Amount > 0).ToList();

            HoldingsComboBox.ItemsSource = _holdingOptions;

            if (!_holdingOptions.Any())
            {
                StatusText.Text = "You don't have any holdings to sell.";
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error loading data: {ex.Message}";
        }
    }

    private void OnHoldingSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (HoldingsComboBox.SelectedItem is HoldingOption option)
        {
            _selectedHolding = option;
            OwnedText.Text = $"{option.Holding.Amount:N8} {option.Holding.Symbol.ToUpper()}";
            CurrentPriceText.Text = $"${option.CurrentPrice:N2}";
            HoldingPanel.IsVisible = true;
            UpdateTotalValue();
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
        UpdateTotalValue();
    }

    private void OnSellAllClick(object? sender, RoutedEventArgs e)
    {
        if (_selectedHolding != null)
        {
            AmountTextBox.Text = _selectedHolding.Holding.Amount.ToString(CultureInfo.InvariantCulture);
        }
    }

    private void UpdateTotalValue()
    {
        if (_selectedHolding == null || _amount <= 0)
        {
            SellingText.Text = "0";
            ReceiveText.Text = "$0.00";
            SellButton.IsEnabled = false;
            StatusText.Text = "";
            return;
        }

        var totalValue = _amount * _selectedHolding.CurrentPrice;
        SellingText.Text = $"{_amount:N8} {_selectedHolding.Holding.Symbol.ToUpper()}";
        ReceiveText.Text = $"${totalValue:N2}";

        if (_amount > _selectedHolding.Holding.Amount)
        {
            StatusText.Text = $"You only own {_selectedHolding.Holding.Amount:N8} {_selectedHolding.Holding.Symbol.ToUpper()}";
            StatusText.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FF6B6B"));
            SellButton.IsEnabled = false;
        }
        else if (_amount > 0)
        {
            StatusText.Text = "";
            SellButton.IsEnabled = true;
        }
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void OnSellClick(object? sender, RoutedEventArgs e)
    {
        if (_selectedHolding == null || _amount <= 0) return;

        SellButton.IsEnabled = false;
        StatusText.Text = "Processing sale...";
        StatusText.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#F0B90B"));

        var result = await _api.SellCryptoAsync(
            _selectedHolding.Holding.CoinId,
            _selectedHolding.Holding.Symbol,
            _amount,
            _selectedHolding.CurrentPrice
        );

        if (result.Success)
        {
            SaleCompleted = true;
            NewBalance = result.Balance;
            StatusText.Text = "Sale successful!";
            StatusText.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#4ECB71"));
            
            await System.Threading.Tasks.Task.Delay(1000);
            Close();
        }
        else
        {
            StatusText.Text = result.Message ?? "Sale failed";
            StatusText.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FF6B6B"));
            SellButton.IsEnabled = true;
        }
    }
}
