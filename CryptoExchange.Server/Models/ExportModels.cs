using System.Xml.Serialization;
using CryptoTrader.Shared.Models;

namespace CryptoExchange.Server.Models;

/// <summary>
/// XML export model for cryptocurrency prices
/// </summary>
[XmlRoot("CryptoPricesExport")]
public class CryptoPricesExport
{
    [XmlAttribute("exportDate")]
    public DateTime ExportDate { get; set; }
    
    [XmlAttribute("totalCount")]
    public int TotalCount { get; set; }
    
    [XmlArray("Prices")]
    [XmlArrayItem("Crypto")]
    public List<CryptoCurrency> Prices { get; set; } = new();
}

/// <summary>
/// XML export model for user holdings
/// </summary>
[XmlRoot("HoldingsExport")]
public class HoldingsExport
{
    [XmlAttribute("exportDate")]
    public DateTime ExportDate { get; set; }
    
    [XmlAttribute("userId")]
    public int UserId { get; set; }
    
    [XmlElement("TotalHoldings")]
    public int TotalHoldings { get; set; }
    
    [XmlElement("TotalValue")]
    public decimal TotalValue { get; set; }
    
    [XmlElement("TotalCost")]
    public decimal TotalCost { get; set; }
    
    [XmlElement("TotalProfitLoss")]
    public decimal TotalProfitLoss { get; set; }
    
    [XmlArray("Holdings")]
    [XmlArrayItem("Holding")]
    public List<HoldingExportItem> Holdings { get; set; } = new();
}

/// <summary>
/// Represents a single holding in an export
/// </summary>
public class HoldingExportItem
{
    [XmlElement("CoinId")]
    public string CoinId { get; set; } = "";
    
    [XmlElement("Symbol")]
    public string Symbol { get; set; } = "";
    
    [XmlElement("Amount")]
    public decimal Amount { get; set; }
    
    [XmlElement("PurchasePrice")]
    public decimal PurchasePrice { get; set; }
    
    [XmlElement("CurrentPrice")]
    public decimal CurrentPrice { get; set; }
    
    [XmlElement("CurrentValue")]
    public decimal CurrentValue { get; set; }
    
    [XmlElement("ProfitLoss")]
    public decimal ProfitLoss { get; set; }
    
    [XmlElement("ProfitLossPercentage")]
    public decimal ProfitLossPercentage { get; set; }
    
    [XmlElement("PurchaseDate")]
    public DateTime PurchaseDate { get; set; }
}

/// <summary>
/// XML export model for transactions
/// </summary>
[XmlRoot("TransactionsExport")]
public class TransactionsExport
{
    [XmlAttribute("exportDate")]
    public DateTime ExportDate { get; set; }
    
    [XmlAttribute("userId")]
    public int UserId { get; set; }
    
    [XmlElement("TotalTransactions")]
    public int TotalTransactions { get; set; }
    
    [XmlElement("TotalBuyValue")]
    public decimal TotalBuyValue { get; set; }
    
    [XmlElement("TotalSellValue")]
    public decimal TotalSellValue { get; set; }
    
    [XmlArray("Transactions")]
    [XmlArrayItem("Transaction")]
    public List<TransactionExportItem> Transactions { get; set; } = new();
}

/// <summary>
/// Represents a single transaction in an export
/// </summary>
public class TransactionExportItem
{
    [XmlAttribute("id")]
    public int Id { get; set; }
    
    [XmlElement("Type")]
    public string Type { get; set; } = "";
    
    [XmlElement("CoinId")]
    public string CoinId { get; set; } = "";
    
    [XmlElement("Symbol")]
    public string Symbol { get; set; } = "";
    
    [XmlElement("Amount")]
    public decimal Amount { get; set; }
    
    [XmlElement("PricePerUnit")]
    public decimal PricePerUnit { get; set; }
    
    [XmlElement("TotalValue")]
    public decimal TotalValue { get; set; }
    
    [XmlElement("Fee")]
    public decimal Fee { get; set; }
    
    [XmlElement("Notes")]
    public string Notes { get; set; } = "";
    
    [XmlElement("Timestamp")]
    public DateTime Timestamp { get; set; }
}
