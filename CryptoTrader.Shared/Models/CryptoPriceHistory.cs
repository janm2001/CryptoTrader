namespace CryptoTrader.Shared.Models;

/// <summary>
/// Represents historical price data for a cryptocurrency
/// </summary>
public class CryptoPriceHistory
{
    public int Id { get; set; }
    public string CoinId { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal MarketCap { get; set; }
    public decimal Volume { get; set; }
    public DateTime Timestamp { get; set; }
}
