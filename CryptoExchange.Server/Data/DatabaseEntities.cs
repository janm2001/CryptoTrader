using Dapper;
using System.Data;

namespace CryptoExchange.Server.Data;

/// <summary>
/// Database entity for Holdings table - maps directly to SQLite columns
/// </summary>
public class HoldingEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string CoinId { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public double Amount { get; set; }
    public double PurchasePrice { get; set; }
    public string PurchaseDate { get; set; } = string.Empty;

    /// <summary>
    /// Converts to the shared model
    /// </summary>
    public CryptoTrader.Shared.Models.CryptoHolding ToModel()
    {
        return new CryptoTrader.Shared.Models.CryptoHolding
        {
            Id = Id,
            UserId = UserId,
            CoinId = CoinId,
            Symbol = Symbol,
            Amount = (decimal)Amount,
            PurchasePrice = (decimal)PurchasePrice,
            PurchaseDate = DateTime.TryParse(PurchaseDate, out var date) ? date : DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates from the shared model
    /// </summary>
    public static HoldingEntity FromModel(CryptoTrader.Shared.Models.CryptoHolding model)
    {
        return new HoldingEntity
        {
            Id = model.Id,
            UserId = model.UserId,
            CoinId = model.CoinId,
            Symbol = model.Symbol,
            Amount = (double)model.Amount,
            PurchasePrice = (double)model.PurchasePrice,
            PurchaseDate = model.PurchaseDate.ToString("O")
        };
    }
}

/// <summary>
/// Database entity for Transactions table - maps directly to SQLite columns
/// </summary>
public class TransactionEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string CoinId { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public int Type { get; set; }
    public double Amount { get; set; }
    public double PricePerUnit { get; set; }
    public double TotalValue { get; set; }
    public double Fee { get; set; }
    public string Timestamp { get; set; } = string.Empty;
    public string? Notes { get; set; }

    /// <summary>
    /// Converts to the shared model
    /// </summary>
    public CryptoTrader.Shared.Models.CryptoTransaction ToModel()
    {
        return new CryptoTrader.Shared.Models.CryptoTransaction
        {
            Id = Id,
            UserId = UserId,
            CoinId = CoinId,
            Symbol = Symbol,
            Type = (CryptoTrader.Shared.Models.TransactionType)Type,
            Amount = (decimal)Amount,
            PricePerUnit = (decimal)PricePerUnit,
            TotalValue = (decimal)TotalValue,
            Fee = (decimal)Fee,
            Timestamp = DateTime.TryParse(Timestamp, out var date) ? date : DateTime.UtcNow,
            Notes = Notes
        };
    }

    /// <summary>
    /// Creates from the shared model
    /// </summary>
    public static TransactionEntity FromModel(CryptoTrader.Shared.Models.CryptoTransaction model)
    {
        return new TransactionEntity
        {
            Id = model.Id,
            UserId = model.UserId,
            CoinId = model.CoinId,
            Symbol = model.Symbol,
            Type = (int)model.Type,
            Amount = (double)model.Amount,
            PricePerUnit = (double)model.PricePerUnit,
            TotalValue = (double)model.TotalValue,
            Fee = (double)model.Fee,
            Timestamp = model.Timestamp.ToString("O"),
            Notes = model.Notes
        };
    }
}

/// <summary>
/// Database entity for PriceHistory table
/// </summary>
public class PriceHistoryEntity
{
    public int Id { get; set; }
    public string CoinId { get; set; } = string.Empty;
    public double Price { get; set; }
    public double MarketCap { get; set; }
    public double Volume { get; set; }
    public string Timestamp { get; set; } = string.Empty;

    /// <summary>
    /// Converts to the shared model
    /// </summary>
    public CryptoTrader.Shared.Models.CryptoPriceHistory ToModel()
    {
        return new CryptoTrader.Shared.Models.CryptoPriceHistory
        {
            Id = Id,
            CoinId = CoinId,
            Price = (decimal)Price,
            MarketCap = (decimal)MarketCap,
            Volume = (decimal)Volume,
            Timestamp = DateTime.TryParse(Timestamp, out var date) ? date : DateTime.UtcNow
        };
    }
}
