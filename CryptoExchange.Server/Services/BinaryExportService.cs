using CryptoTrader.Shared.Models;
using CryptoExchange.Server.Data;

namespace CryptoExchange.Server.Services;

/// <summary>
/// Service for exporting portfolio and transaction data in binary format
/// Binary format provides compact storage and fast read/write operations
/// </summary>
public class BinaryExportService
{
    private const byte FILE_VERSION = 1;
    private const string HOLDINGS_MAGIC = "CTHD"; // CryptoTrader Holdings Data
    private const string TRANSACTIONS_MAGIC = "CTTX"; // CryptoTrader Transactions
    private const string PORTFOLIO_MAGIC = "CTPF"; // CryptoTrader Portfolio
    private const string PRICES_MAGIC = "CTPR"; // CryptoTrader Prices

    private readonly DatabaseContext _db;
    private readonly CryptoApiService _cryptoService;

    public BinaryExportService(DatabaseContext db, CryptoApiService cryptoService)
    {
        _db = db;
        _cryptoService = cryptoService;
    }

    /// <summary>
    /// Exports cryptocurrency prices to binary format
    /// Format: [MAGIC:4][VERSION:1][TIMESTAMP:8][COUNT:4][PRICES...]
    /// </summary>
    public async Task<byte[]> ExportPricesToBinaryAsync()
    {
        var prices = await _cryptoService.GetCachedPricesAsync();
        
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        // Write header
        writer.Write(PRICES_MAGIC.ToCharArray());
        writer.Write(FILE_VERSION);
        writer.Write(DateTime.UtcNow.ToBinary());
        writer.Write(prices.Count);

        // Write each price
        foreach (var crypto in prices.OrderBy(p => p.MarketCapRank))
        {
            WriteString(writer, crypto.CoinId);
            WriteString(writer, crypto.Name);
            WriteString(writer, crypto.Symbol);
            writer.Write(crypto.MarketCapRank);
            writer.Write((double)crypto.CurrentPrice);
            writer.Write((double)crypto.PriceChangePercentage24h);
            writer.Write((double)crypto.MarketCap);
            writer.Write((double)crypto.TotalVolume);
            writer.Write((double)crypto.CirculatingSupply);
            writer.Write(crypto.LastUpdated.ToBinary());
        }

        return stream.ToArray();
    }

    /// <summary>
    /// Exports user holdings to binary format
    /// Format: [MAGIC:4][VERSION:1][TIMESTAMP:8][COUNT:4][HOLDINGS...]
    /// Each holding: [COINID_LEN:1][COINID:n][SYMBOL_LEN:1][SYMBOL:n][AMOUNT:8][PURCHASE_PRICE:8][PURCHASE_DATE:8]
    /// </summary>
    public async Task<byte[]> ExportHoldingsToBinaryAsync(int userId)
    {
        var holdings = (await _db.GetUserHoldingsAsync(userId)).ToList();
        
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        // Write header
        writer.Write(HOLDINGS_MAGIC.ToCharArray());
        writer.Write(FILE_VERSION);
        writer.Write(DateTime.UtcNow.ToBinary());
        writer.Write(holdings.Count);

        // Write each holding
        foreach (var holding in holdings)
        {
            WriteString(writer, holding.CoinId);
            WriteString(writer, holding.Symbol);
            writer.Write((double)holding.Amount);
            writer.Write((double)holding.PurchasePrice);
            writer.Write(holding.PurchaseDate.ToBinary());
        }

        return stream.ToArray();
    }

    /// <summary>
    /// Imports holdings from binary format
    /// </summary>
    public List<CryptoHolding> ImportHoldingsFromBinary(byte[] data)
    {
        var holdings = new List<CryptoHolding>();

        using var stream = new MemoryStream(data);
        using var reader = new BinaryReader(stream);

        // Read and verify header
        var magic = new string(reader.ReadChars(4));
        if (magic != HOLDINGS_MAGIC)
            throw new InvalidDataException("Invalid holdings file format");

        var version = reader.ReadByte();
        if (version > FILE_VERSION)
            throw new InvalidDataException($"Unsupported file version: {version}");

        var timestamp = DateTime.FromBinary(reader.ReadInt64());
        var count = reader.ReadInt32();

        // Read holdings
        for (int i = 0; i < count; i++)
        {
            var holding = new CryptoHolding
            {
                CoinId = ReadString(reader),
                Symbol = ReadString(reader),
                Amount = (decimal)reader.ReadDouble(),
                PurchasePrice = (decimal)reader.ReadDouble(),
                PurchaseDate = DateTime.FromBinary(reader.ReadInt64())
            };
            holdings.Add(holding);
        }

        return holdings;
    }

    /// <summary>
    /// Exports user transactions to binary format
    /// Format: [MAGIC:4][VERSION:1][TIMESTAMP:8][COUNT:4][TRANSACTIONS...]
    /// </summary>
    public async Task<byte[]> ExportTransactionsToBinaryAsync(int userId, int limit = 1000)
    {
        var transactions = (await _db.GetUserTransactionsAsync(userId, limit)).ToList();
        
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        // Write header
        writer.Write(TRANSACTIONS_MAGIC.ToCharArray());
        writer.Write(FILE_VERSION);
        writer.Write(DateTime.UtcNow.ToBinary());
        writer.Write(transactions.Count);

        // Write each transaction
        foreach (var tx in transactions)
        {
            writer.Write(tx.Id);
            writer.Write((byte)tx.Type);
            WriteString(writer, tx.CoinId);
            WriteString(writer, tx.Symbol);
            writer.Write((double)tx.Amount);
            writer.Write((double)tx.PricePerUnit);
            writer.Write((double)tx.TotalValue);
            writer.Write((double)tx.Fee);
            WriteString(writer, tx.Notes ?? "");
            writer.Write(tx.Timestamp.ToBinary());
        }

        return stream.ToArray();
    }

    /// <summary>
    /// Imports transactions from binary format
    /// </summary>
    public List<CryptoTransaction> ImportTransactionsFromBinary(byte[] data)
    {
        var transactions = new List<CryptoTransaction>();

        using var stream = new MemoryStream(data);
        using var reader = new BinaryReader(stream);

        // Read and verify header
        var magic = new string(reader.ReadChars(4));
        if (magic != TRANSACTIONS_MAGIC)
            throw new InvalidDataException("Invalid transactions file format");

        var version = reader.ReadByte();
        if (version > FILE_VERSION)
            throw new InvalidDataException($"Unsupported file version: {version}");

        var timestamp = DateTime.FromBinary(reader.ReadInt64());
        var count = reader.ReadInt32();

        // Read transactions
        for (int i = 0; i < count; i++)
        {
            var tx = new CryptoTransaction
            {
                Id = reader.ReadInt32(),
                Type = (TransactionType)reader.ReadByte(),
                CoinId = ReadString(reader),
                Symbol = ReadString(reader),
                Amount = (decimal)reader.ReadDouble(),
                PricePerUnit = (decimal)reader.ReadDouble(),
                TotalValue = (decimal)reader.ReadDouble(),
                Fee = (decimal)reader.ReadDouble(),
                Notes = ReadString(reader),
                Timestamp = DateTime.FromBinary(reader.ReadInt64())
            };
            transactions.Add(tx);
        }

        return transactions;
    }

    /// <summary>
    /// Exports complete portfolio (balance + holdings + summary) to binary format
    /// </summary>
    public async Task<byte[]> ExportPortfolioToBinaryAsync(int userId)
    {
        var holdings = (await _db.GetUserHoldingsAsync(userId)).ToList();
        var user = await _db.GetUserByIdAsync(userId);
        var balance = user?.Balance ?? 0;

        // Get current prices for holdings
        var coinIds = holdings.Select(h => h.CoinId).Distinct();
        var prices = await _cryptoService.GetCryptosByIdsAsync(coinIds);
        var priceDict = prices.ToDictionary(p => p.CoinId, p => p.CurrentPrice);

        // Calculate totals
        decimal totalValue = 0, totalCost = 0;
        foreach (var holding in holdings)
        {
            var currentPrice = priceDict.GetValueOrDefault(holding.CoinId, 0);
            totalValue += holding.GetCurrentValue(currentPrice);
            totalCost += holding.Amount * holding.PurchasePrice;
        }

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        // Write header
        writer.Write(PORTFOLIO_MAGIC.ToCharArray());
        writer.Write(FILE_VERSION);
        writer.Write(DateTime.UtcNow.ToBinary());
        writer.Write(userId);

        // Write summary
        writer.Write((double)balance);
        writer.Write((double)totalValue);
        writer.Write((double)totalCost);
        writer.Write((double)(totalValue - totalCost)); // Profit/Loss
        writer.Write(holdings.Count);

        // Write each holding with current values
        foreach (var holding in holdings)
        {
            var currentPrice = priceDict.GetValueOrDefault(holding.CoinId, 0);
            var currentValue = holding.GetCurrentValue(currentPrice);
            var profitLoss = holding.GetProfitLoss(currentPrice);

            WriteString(writer, holding.CoinId);
            WriteString(writer, holding.Symbol);
            writer.Write((double)holding.Amount);
            writer.Write((double)holding.PurchasePrice);
            writer.Write((double)currentPrice);
            writer.Write((double)currentValue);
            writer.Write((double)profitLoss);
            writer.Write(holding.PurchaseDate.ToBinary());
        }

        return stream.ToArray();
    }

    /// <summary>
    /// Reads portfolio summary from binary data
    /// </summary>
    public PortfolioSummary ImportPortfolioSummaryFromBinary(byte[] data)
    {
        using var stream = new MemoryStream(data);
        using var reader = new BinaryReader(stream);

        // Read and verify header
        var magic = new string(reader.ReadChars(4));
        if (magic != PORTFOLIO_MAGIC)
            throw new InvalidDataException("Invalid portfolio file format");

        var version = reader.ReadByte();
        if (version > FILE_VERSION)
            throw new InvalidDataException($"Unsupported file version: {version}");

        var timestamp = DateTime.FromBinary(reader.ReadInt64());
        var userId = reader.ReadInt32();

        // Read summary
        var balance = (decimal)reader.ReadDouble();
        var totalValue = (decimal)reader.ReadDouble();
        var totalCost = (decimal)reader.ReadDouble();
        var profitLoss = (decimal)reader.ReadDouble();
        var holdingsCount = reader.ReadInt32();

        return new PortfolioSummary
        {
            Balance = balance,
            TotalValue = totalValue,
            TotalCost = totalCost,
            TotalProfitLoss = profitLoss,
            TotalProfitLossPercentage = totalCost > 0 ? (profitLoss / totalCost) * 100 : 0,
            HoldingsCount = holdingsCount
        };
    }

    /// <summary>
    /// Helper to write length-prefixed string
    /// </summary>
    private static void WriteString(BinaryWriter writer, string value)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(value);
        writer.Write((ushort)bytes.Length);
        writer.Write(bytes);
    }

    /// <summary>
    /// Helper to read length-prefixed string
    /// </summary>
    private static string ReadString(BinaryReader reader)
    {
        var length = reader.ReadUInt16();
        var bytes = reader.ReadBytes(length);
        return System.Text.Encoding.UTF8.GetString(bytes);
    }
}

/// <summary>
/// Portfolio summary for binary import
/// </summary>
public class PortfolioSummary
{
    public decimal Balance { get; set; }
    public decimal TotalValue { get; set; }
    public decimal TotalCost { get; set; }
    public decimal TotalProfitLoss { get; set; }
    public decimal TotalProfitLossPercentage { get; set; }
    public int HoldingsCount { get; set; }
}
