using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using CryptoTrader.App.Services;

namespace CryptoTrader.App.ViewModels;

/// <summary>
/// Converter for price change colors (green for positive, red for negative)
/// </summary>
public class PriceChangeColorConverter : IMultiValueConverter
{
    public static readonly PriceChangeColorConverter Instance = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count > 0 && values[0] is decimal change)
        {
            return change >= 0 
                ? new SolidColorBrush(Color.Parse("#00d26a")) 
                : new SolidColorBrush(Color.Parse("#ff4757"));
        }
        return new SolidColorBrush(Colors.Gray);
    }
}

/// <summary>
/// Converter for boolean to color (connected = green, disconnected = red)
/// </summary>
public class BoolToColorConverter : IValueConverter
{
    public static readonly BoolToColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isTrue = value is bool b && b;
        var trueColor = "#00b894";  // Green
        var falseColor = "#ff6b6b"; // Red

        // Allow custom colors via parameter (format: "trueColor|falseColor")
        if (parameter is string paramStr && paramStr.Contains('|'))
        {
            var colors = paramStr.Split('|');
            if (colors.Length == 2)
            {
                trueColor = colors[0];
                falseColor = colors[1];
            }
        }

        var colorStr = isTrue ? trueColor : falseColor;
        
        if (targetType == typeof(IBrush) || targetType == typeof(ISolidColorBrush))
        {
            return new SolidColorBrush(Color.Parse(colorStr));
        }
        
        return Color.Parse(colorStr);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converter for currency formatting based on user settings
/// </summary>
public class CurrencyConverter : IValueConverter
{
    public static readonly CurrencyConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is decimal decimalValue)
        {
            return CurrencyService.Instance.Format(decimalValue);
        }
        if (value is double doubleValue)
        {
            return CurrencyService.Instance.Format((decimal)doubleValue);
        }
        return value?.ToString() ?? "";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
