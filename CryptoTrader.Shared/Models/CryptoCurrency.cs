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

