using System;
using System.Collections.Generic;

namespace CryptoTrader.App.Models;

/// <summary>
/// Represents a summary of a user's portfolio
/// </summary>
public class PortfolioSummary
{
    public decimal TotalValue { get; set; }
    public decimal TotalCost { get; set; }
    public decimal TotalProfitLoss { get; set; }
    public decimal TotalProfitLossPercentage { get; set; }
    public int HoldingsCount { get; set; }
    public decimal Balance { get; set; }
    public List<HoldingDetail>? Holdings { get; set; }
}

/// <summary>
/// Detailed information about a holding including current value
/// </summary>
public class HoldingDetail
{
    public string CoinId { get; set; } = "";
    public string Symbol { get; set; } = "";
    public decimal Amount { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal ProfitLoss { get; set; }
    public decimal ProfitLossPercentage { get; set; }
}

/// <summary>
/// User information for admin display
/// </summary>
public class UserInfo
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

/// <summary>
/// System statistics for admin dashboard
/// </summary>
public class SystemStats
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int AdminUsers { get; set; }
    public int TotalHoldings { get; set; }
    public int TotalTransactions { get; set; }
    public int TrackedCryptos { get; set; }
}

/// <summary>
/// Response wrapper for balance queries
/// </summary>
public class BalanceResponse
{
    public decimal Balance { get; set; }
}

/// <summary>
/// Result of a buy operation
/// </summary>
public class BuyResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public decimal Balance { get; set; }
}

/// <summary>
/// Result of a sell operation
/// </summary>
public class SellResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public decimal Balance { get; set; }
}

/// <summary>
/// Cryptocurrency option for dropdowns/selection
/// </summary>
public class CryptoOption
{
    public string CoinId { get; set; } = "";
    public string Symbol { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal CurrentPrice { get; set; }
    
    public override string ToString() => $"{Name} ({Symbol.ToUpper()}) - ${CurrentPrice:N2}";
}

/// <summary>
/// Status of user's profile picture
/// </summary>
public class ProfilePictureStatus
{
    public bool Success { get; set; }
    public bool HasPicture { get; set; }
    public string? MimeType { get; set; }
    public int Size { get; set; }
}
