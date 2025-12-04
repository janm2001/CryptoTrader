using System;

namespace CryptoTrader.App.Models;

/// <summary>
/// Represents a price point in time for charting
/// </summary>
public class PricePoint
{
    public DateTime Time { get; set; }
    public decimal Price { get; set; }
}

/// <summary>
/// Display model for price history list items
/// </summary>
public class PriceDisplayItem
{
    public string TimeText { get; set; } = "";
    public string PriceText { get; set; } = "";
}
