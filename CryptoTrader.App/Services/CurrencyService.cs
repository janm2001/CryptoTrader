using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CryptoTrader.App.Services;

/// <summary>
/// Service for handling currency formatting and conversion based on user settings
/// </summary>
public class CurrencyService : INotifyPropertyChanged
{
    private static CurrencyService? _instance;
    public static CurrencyService Instance => _instance ??= new CurrencyService();

    private readonly ExchangeRateService _exchangeRates;
    private string _currentCurrency = "USD";
    
    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<string>? CurrencyChanged;

    private CurrencyService()
    {
        _exchangeRates = ExchangeRateService.Instance;
        // Load from settings
        _currentCurrency = SettingsService.Instance.DisplayCurrency;
        
        // Initialize exchange rates in background
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await _exchangeRates.UpdateExchangeRatesAsync();
    }

    public string CurrentCurrency
    {
        get => _currentCurrency;
        set
        {
            if (_currentCurrency != value)
            {
                _currentCurrency = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrencySymbol));
                OnPropertyChanged(nameof(ExchangeRate));
                CurrencyChanged?.Invoke(this, value);
            }
        }
    }

    public string CurrencySymbol => _currentCurrency switch
    {
        "EUR" => "€",
        "GBP" => "£",
        "USD" => "$",
        _ => "$"
    };

    /// <summary>
    /// Gets the current exchange rate (relative to USD)
    /// </summary>
    public decimal ExchangeRate => _exchangeRates.GetRate(_currentCurrency);

    /// <summary>
    /// Updates exchange rates from API
    /// </summary>
    public async Task RefreshExchangeRatesAsync()
    {
        await _exchangeRates.UpdateExchangeRatesAsync();
        OnPropertyChanged(nameof(ExchangeRate));
        CurrencyChanged?.Invoke(this, _currentCurrency);
    }

    /// <summary>
    /// Converts a value from USD to the current display currency
    /// </summary>
    public decimal Convert(decimal valueInUsd)
    {
        return _exchangeRates.ConvertFromUsd(valueInUsd, _currentCurrency);
    }

    /// <summary>
    /// Formats a decimal value (assumed in USD) with the current currency symbol
    /// Automatically converts to the selected currency
    /// </summary>
    public string Format(decimal valueInUsd)
    {
        var converted = Convert(valueInUsd);
        return $"{CurrencySymbol}{converted:N2}";
    }

    /// <summary>
    /// Formats a decimal value with sign (+ or -) and currency symbol
    /// </summary>
    public string FormatWithSign(decimal valueInUsd)
    {
        var converted = Convert(valueInUsd);
        var sign = converted >= 0 ? "+" : "";
        return $"{sign}{CurrencySymbol}{converted:N2}";
    }

    /// <summary>
    /// Formats a decimal value for display (positive values show +)
    /// </summary>
    public string FormatProfitLoss(decimal valueInUsd)
    {
        var converted = Convert(valueInUsd);
        if (converted >= 0)
            return $"+{CurrencySymbol}{converted:N2}";
        else
            return $"-{CurrencySymbol}{Math.Abs(converted):N2}";
    }

    /// <summary>
    /// Gets the CultureInfo for the current currency (for StringFormat in XAML)
    /// </summary>
    public CultureInfo GetCultureInfo()
    {
        return _currentCurrency switch
        {
            "EUR" => new CultureInfo("de-DE"),  // Euro format
            "GBP" => new CultureInfo("en-GB"),  // British Pound format
            "USD" => new CultureInfo("en-US"),  // US Dollar format
            _ => CultureInfo.InvariantCulture
        };
    }

    /// <summary>
    /// Gets the exchange rate info for display
    /// </summary>
    public string GetExchangeRateInfo()
    {
        if (_currentCurrency == "USD")
            return "Base currency (USD)";
        
        var rate = _exchangeRates.GetRate(_currentCurrency);
        var lastUpdate = _exchangeRates.LastUpdate;
        var updateInfo = lastUpdate > DateTime.MinValue 
            ? $"Updated: {lastUpdate:HH:mm}" 
            : "Using fallback rates";
        
        return $"1 USD = {rate:F4} {_currentCurrency} ({updateInfo})";
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
