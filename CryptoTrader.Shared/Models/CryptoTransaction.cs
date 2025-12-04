namespace CryptoTrader.Shared.Models;

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

/// <summary>
/// Types of cryptocurrency transactions
/// </summary>
public enum TransactionType
{
    Buy,
    Sell,
    Transfer,
    Deposit,
    Withdrawal
}
