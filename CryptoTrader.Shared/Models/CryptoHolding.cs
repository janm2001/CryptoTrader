namespace CryptoTrader.Shared.Models;

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
    
    /// <summary>
    /// Calculates the current value of the holding
    /// </summary>
    public decimal GetCurrentValue(decimal currentPrice) => Amount * currentPrice;
    
    /// <summary>
    /// Calculates the profit/loss in currency
    /// </summary>
    public decimal GetProfitLoss(decimal currentPrice) => GetCurrentValue(currentPrice) - (Amount * PurchasePrice);
    
    /// <summary>
    /// Calculates the profit/loss as a percentage
    /// </summary>
    public decimal GetProfitLossPercentage(decimal currentPrice) => 
        PurchasePrice > 0 ? ((currentPrice - PurchasePrice) / PurchasePrice) * 100 : 0;
}
