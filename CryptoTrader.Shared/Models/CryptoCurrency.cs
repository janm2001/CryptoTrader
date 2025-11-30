using System.Text.Json.Serialization;

namespace CryptoTrader.Shared.Models;

/// <summary>
/// Represents a cryptocurrency with its market data
/// </summary>
public class CryptoCurrency
{
    [JsonIgnore]
    public int Id { get; set; }
    
    [JsonPropertyName("id")]
    public string CoinId { get; set; } = string.Empty;
    
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("current_price")]
    public decimal CurrentPrice { get; set; }
    
    [JsonPropertyName("market_cap")]
    public decimal MarketCap { get; set; }
    
    [JsonPropertyName("market_cap_rank")]
    public int MarketCapRank { get; set; }
    
    [JsonPropertyName("total_volume")]
    public decimal TotalVolume { get; set; }
    
    [JsonPropertyName("high_24h")]
    public decimal High24h { get; set; }
    
    [JsonPropertyName("low_24h")]
    public decimal Low24h { get; set; }
    
    [JsonPropertyName("price_change_24h")]
    public decimal PriceChange24h { get; set; }
    
    [JsonPropertyName("price_change_percentage_24h")]
    public decimal PriceChangePercentage24h { get; set; }
    
    [JsonPropertyName("circulating_supply")]
    public decimal CirculatingSupply { get; set; }
    
    [JsonPropertyName("total_supply")]
    public decimal? TotalSupply { get; set; }
    
    [JsonPropertyName("max_supply")]
    public decimal? MaxSupply { get; set; }
    
    [JsonPropertyName("last_updated")]
    public DateTime LastUpdated { get; set; }
    
    [JsonPropertyName("image")]
    public string ImageUrl { get; set; } = string.Empty;
    
    public override string ToString()
    {
        return $"{Name} ({Symbol.ToUpper()}): ${CurrentPrice:N2} ({PriceChangePercentage24h:+0.00;-0.00}%)";
    }
}

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

/// <summary>
/// Represents a user's cryptocurrency holding
/// </summary>
public class CryptoHolding
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string CoinId { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal PurchasePrice { get; set; }
    public DateTime PurchaseDate { get; set; }
    
    public decimal GetCurrentValue(decimal currentPrice) => Amount * currentPrice;
    public decimal GetProfitLoss(decimal currentPrice) => GetCurrentValue(currentPrice) - (Amount * PurchasePrice);
    public decimal GetProfitLossPercentage(decimal currentPrice) => 
        PurchasePrice > 0 ? ((currentPrice - PurchasePrice) / PurchasePrice) * 100 : 0;
}

/// <summary>
/// Represents a trade/transaction
/// </summary>
public class CryptoTransaction
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string CoinId { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public decimal PricePerUnit { get; set; }
    public decimal TotalValue { get; set; }
    public decimal Fee { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Notes { get; set; }
}

public enum TransactionType
{
    Buy,
    Sell,
    Transfer,
    Deposit,
    Withdrawal
}
