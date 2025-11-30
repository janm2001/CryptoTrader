using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

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
