using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace CryptoTrader.App.Services;

/// <summary>
/// Service for handling currency formatting based on user settings
/// </summary>
public class CurrencyService : INotifyPropertyChanged
{
    private static CurrencyService? _instance;
    public static CurrencyService Instance => _instance ??= new CurrencyService();

    private string _currentCurrency = "USD";
    
    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<string>? CurrencyChanged;

    private CurrencyService()
    {
        // Load from settings
        _currentCurrency = SettingsService.Instance.DisplayCurrency;
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
    /// Formats a decimal value with the current currency symbol
    /// </summary>
    public string Format(decimal value)
    {
        return $"{CurrencySymbol}{value:N2}";
    }

    /// <summary>
    /// Formats a decimal value with sign (+ or -) and currency symbol
    /// </summary>
    public string FormatWithSign(decimal value)
    {
        var sign = value >= 0 ? "+" : "";
        return $"{sign}{CurrencySymbol}{value:N2}";
    }

    /// <summary>
    /// Formats a decimal value for display (positive values show +)
    /// </summary>
    public string FormatProfitLoss(decimal value)
    {
        if (value >= 0)
            return $"+{CurrencySymbol}{value:N2}";
        else
            return $"-{CurrencySymbol}{Math.Abs(value):N2}";
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

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
