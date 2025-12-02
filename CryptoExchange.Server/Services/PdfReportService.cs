using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using CryptoTrader.Shared.Models;
using CryptoExchange.Server.Data;

namespace CryptoExchange.Server.Services;

/// <summary>
/// Helper class for holding with calculated values (for PDF generation)
/// </summary>
public class HoldingWithValues
{
    public CryptoHolding Holding { get; set; } = null!;
    public decimal CurrentPrice { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal ProfitLoss { get; set; }
    public decimal ProfitLossPercent { get; set; }
}

/// <summary>
/// Service for generating PDF reports for portfolio and transactions
/// Uses QuestPDF for fluent PDF generation
/// </summary>
public class PdfReportService
{
    private readonly DatabaseContext _db;
    private readonly CryptoApiService _cryptoService;

    // Color palette matching the app theme
    private static readonly string PrimaryColor = "#F0B90B";     // Gold/Yellow
    private static readonly string PositiveColor = "#4ECB71";    // Green
    private static readonly string NegativeColor = "#FF6B6B";    // Red

    static PdfReportService()
    {
        // Configure QuestPDF license (Community license for open-source)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public PdfReportService(DatabaseContext db, CryptoApiService cryptoService)
    {
        _db = db;
        _cryptoService = cryptoService;
    }

    /// <summary>
    /// Generates a comprehensive portfolio report PDF
    /// </summary>
    public async Task<byte[]> GeneratePortfolioReportAsync(int userId, string username)
    {
        var holdings = (await _db.GetUserHoldingsAsync(userId)).ToList();
        var user = await _db.GetUserByIdAsync(userId);
        var balance = user?.Balance ?? 0;

        // Get current prices
        var coinIds = holdings.Select(h => h.CoinId).Distinct();
        var prices = await _cryptoService.GetCryptosByIdsAsync(coinIds);
        var priceDict = prices.ToDictionary(p => p.CoinId, p => p.CurrentPrice);

        // Calculate totals
        decimal totalValue = 0, totalCost = 0;
        var holdingsWithValues = holdings.Select(h =>
        {
            var currentPrice = priceDict.GetValueOrDefault(h.CoinId, 0);
            var currentValue = h.GetCurrentValue(currentPrice);
            var profitLoss = h.GetProfitLoss(currentPrice);
            totalValue += currentValue;
            totalCost += h.Amount * h.PurchasePrice;
            return new HoldingWithValues
            {
                Holding = h,
                CurrentPrice = currentPrice,
                CurrentValue = currentValue,
                ProfitLoss = profitLoss,
                ProfitLossPercent = h.GetProfitLossPercentage(currentPrice)
            };
        }).OrderByDescending(h => h.CurrentValue).ToList();

        var totalProfitLoss = totalValue - totalCost;
        var totalProfitLossPercent = totalCost > 0 ? (totalProfitLoss / totalCost) * 100 : 0;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => ComposeHeader(c, "Portfolio Report", username));
                
                page.Content().Column(column =>
                {
                    // Summary section
                    column.Item().PaddingVertical(10).Element(c => ComposeSummary(c, balance, totalValue, totalProfitLoss, totalProfitLossPercent));
                    
                    // Holdings table
                    column.Item().PaddingTop(20).Text("Holdings").Bold().FontSize(14);
                    column.Item().PaddingTop(10).Element(c => ComposeHoldingsTable(c, holdingsWithValues));
                });

                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    /// <summary>
    /// Generates a transaction history report PDF
    /// </summary>
    public async Task<byte[]> GenerateTransactionsReportAsync(int userId, string username, int limit = 100)
    {
        var transactions = (await _db.GetUserTransactionsAsync(userId, limit)).ToList();
        
        var totalBuyValue = transactions.Where(t => t.Type == TransactionType.Buy).Sum(t => t.TotalValue);
        var totalSellValue = transactions.Where(t => t.Type == TransactionType.Sell).Sum(t => t.TotalValue);
        var totalFees = transactions.Sum(t => t.Fee);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => ComposeHeader(c, "Transaction History", username));
                
                page.Content().Column(column =>
                {
                    // Summary
                    column.Item().PaddingVertical(10).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text($"Total Transactions: {transactions.Count}").Bold();
                            c.Item().Text($"Total Bought: ${totalBuyValue:N2}").FontColor(PositiveColor);
                            c.Item().Text($"Total Sold: ${totalSellValue:N2}").FontColor(NegativeColor);
                            c.Item().Text($"Total Fees: ${totalFees:N2}");
                        });
                    });

                    // Transactions table
                    column.Item().PaddingTop(20).Element(c => ComposeTransactionsTable(c, transactions));
                });

                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    /// <summary>
    /// Generates a market overview report PDF
    /// </summary>
    public async Task<byte[]> GenerateMarketReportAsync(int topCount = 50)
    {
        var prices = await _cryptoService.GetCachedPricesAsync();
        var topCryptos = prices.OrderBy(p => p.MarketCapRank).Take(topCount).ToList();

        var totalMarketCap = topCryptos.Sum(p => p.MarketCap);
        var totalVolume = topCryptos.Sum(p => p.TotalVolume);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Element(c => ComposeHeader(c, "Crypto Market Report", ""));
                
                page.Content().Column(column =>
                {
                    // Market summary
                    column.Item().PaddingVertical(10).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text($"Showing Top {topCount} Cryptocurrencies").Bold();
                            c.Item().Text($"Total Market Cap: ${totalMarketCap:N0}");
                            c.Item().Text($"Total 24h Volume: ${totalVolume:N0}");
                        });
                    });

                    // Prices table
                    column.Item().PaddingTop(10).Element(c => ComposePricesTable(c, topCryptos));
                });

                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    private void ComposeHeader(IContainer container, string title, string username)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("CryptoTrader").Bold().FontSize(20).FontColor(PrimaryColor);
                column.Item().Text(title).FontSize(16);
                if (!string.IsNullOrEmpty(username))
                {
                    column.Item().Text($"User: {username}").FontSize(10).Italic();
                }
            });

            row.ConstantItem(150).AlignRight().Column(column =>
            {
                column.Item().Text($"Generated: {DateTime.UtcNow:yyyy-MM-dd}").FontSize(9);
                column.Item().Text($"Time: {DateTime.UtcNow:HH:mm:ss} UTC").FontSize(9);
            });
        });
        
        container.PaddingBottom(5).BorderBottom(1).BorderColor(PrimaryColor);
    }

    private void ComposeFooter(IContainer container)
    {
        container.AlignCenter().Text(text =>
        {
            text.Span("Page ");
            text.CurrentPageNumber();
            text.Span(" of ");
            text.TotalPages();
            text.Span(" | CryptoTrader Report").FontSize(8);
        });
    }

    private void ComposeSummary(IContainer container, decimal balance, decimal portfolioValue, decimal profitLoss, decimal profitLossPercent)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Row(row =>
        {
            row.RelativeItem().Column(c =>
            {
                c.Item().Text("Account Summary").Bold().FontSize(12);
                c.Item().PaddingTop(5).Text($"Cash Balance: ${balance:N2}");
                c.Item().Text($"Portfolio Value: ${portfolioValue:N2}");
                c.Item().Text($"Total Assets: ${(balance + portfolioValue):N2}").Bold();
            });

            row.RelativeItem().AlignRight().Column(c =>
            {
                c.Item().Text("Performance").Bold().FontSize(12);
                c.Item().PaddingTop(5).Text($"Profit/Loss: ${profitLoss:N2}")
                    .FontColor(profitLoss >= 0 ? PositiveColor : NegativeColor);
                c.Item().Text($"Return: {(profitLossPercent >= 0 ? "+" : "")}{profitLossPercent:N2}%")
                    .FontColor(profitLossPercent >= 0 ? PositiveColor : NegativeColor);
            });
        });
    }

    private void ComposeHoldingsTable(IContainer container, IEnumerable<HoldingWithValues> holdings)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2);  // Coin
                columns.RelativeColumn(1);  // Symbol
                columns.RelativeColumn(1.5f); // Amount
                columns.RelativeColumn(1.5f); // Avg Price
                columns.RelativeColumn(1.5f); // Current Price
                columns.RelativeColumn(1.5f); // Value
                columns.RelativeColumn(1.5f); // P/L
            });

            // Header
            table.Header(header =>
            {
                header.Cell().Background(Colors.Grey.Darken3).Padding(5).Text("Coin").Bold().FontColor(Colors.White);
                header.Cell().Background(Colors.Grey.Darken3).Padding(5).Text("Symbol").Bold().FontColor(Colors.White);
                header.Cell().Background(Colors.Grey.Darken3).Padding(5).AlignRight().Text("Amount").Bold().FontColor(Colors.White);
                header.Cell().Background(Colors.Grey.Darken3).Padding(5).AlignRight().Text("Avg Price").Bold().FontColor(Colors.White);
                header.Cell().Background(Colors.Grey.Darken3).Padding(5).AlignRight().Text("Current").Bold().FontColor(Colors.White);
                header.Cell().Background(Colors.Grey.Darken3).Padding(5).AlignRight().Text("Value").Bold().FontColor(Colors.White);
                header.Cell().Background(Colors.Grey.Darken3).Padding(5).AlignRight().Text("P/L %").Bold().FontColor(Colors.White);
            });

            // Data rows
            bool alternate = false;
            foreach (var h in holdings)
            {
                var bgColor = alternate ? Colors.Grey.Lighten4 : Colors.White;
                var plColor = h.ProfitLoss >= 0 ? PositiveColor : NegativeColor;

                table.Cell().Background(bgColor).Padding(5).Text(h.Holding.CoinId);
                table.Cell().Background(bgColor).Padding(5).Text(h.Holding.Symbol.ToUpper());
                table.Cell().Background(bgColor).Padding(5).AlignRight().Text($"{h.Holding.Amount:N4}");
                table.Cell().Background(bgColor).Padding(5).AlignRight().Text($"${h.Holding.PurchasePrice:N2}");
                table.Cell().Background(bgColor).Padding(5).AlignRight().Text($"${h.CurrentPrice:N2}");
                table.Cell().Background(bgColor).Padding(5).AlignRight().Text($"${h.CurrentValue:N2}");
                table.Cell().Background(bgColor).Padding(5).AlignRight().Text($"{(h.ProfitLossPercent >= 0 ? "+" : "")}{h.ProfitLossPercent:N2}%").FontColor(plColor);

                alternate = !alternate;
            }
        });
    }

    private void ComposeTransactionsTable(IContainer container, IEnumerable<CryptoTransaction> transactions)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2);    // Date
                columns.RelativeColumn(1);    // Type
                columns.RelativeColumn(1.5f); // Coin
                columns.RelativeColumn(1.5f); // Amount
                columns.RelativeColumn(1.5f); // Price
                columns.RelativeColumn(1.5f); // Total
            });

            // Header
            table.Header(header =>
            {
                header.Cell().Background(Colors.Grey.Darken3).Padding(5).Text("Date").Bold().FontColor(Colors.White);
                header.Cell().Background(Colors.Grey.Darken3).Padding(5).Text("Type").Bold().FontColor(Colors.White);
                header.Cell().Background(Colors.Grey.Darken3).Padding(5).Text("Coin").Bold().FontColor(Colors.White);
                header.Cell().Background(Colors.Grey.Darken3).Padding(5).AlignRight().Text("Amount").Bold().FontColor(Colors.White);
                header.Cell().Background(Colors.Grey.Darken3).Padding(5).AlignRight().Text("Price").Bold().FontColor(Colors.White);
                header.Cell().Background(Colors.Grey.Darken3).Padding(5).AlignRight().Text("Total").Bold().FontColor(Colors.White);
            });

            bool alternate = false;
            foreach (var tx in transactions)
            {
                var bgColor = alternate ? Colors.Grey.Lighten4 : Colors.White;
                var typeColor = tx.Type == TransactionType.Buy ? PositiveColor : NegativeColor;

                table.Cell().Background(bgColor).Padding(5).Text(tx.Timestamp.ToString("yyyy-MM-dd HH:mm"));
                table.Cell().Background(bgColor).Padding(5).Text(tx.Type.ToString()).FontColor(typeColor);
                table.Cell().Background(bgColor).Padding(5).Text(tx.Symbol.ToUpper());
                table.Cell().Background(bgColor).Padding(5).AlignRight().Text($"{tx.Amount:N4}");
                table.Cell().Background(bgColor).Padding(5).AlignRight().Text($"${tx.PricePerUnit:N2}");
                table.Cell().Background(bgColor).Padding(5).AlignRight().Text($"${tx.TotalValue:N2}");

                alternate = !alternate;
            }
        });
    }

    private void ComposePricesTable(IContainer container, IEnumerable<CryptoCurrency> cryptos)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(30);   // Rank
                columns.RelativeColumn(2);    // Name
                columns.RelativeColumn(1);    // Symbol
                columns.RelativeColumn(1.5f); // Price
                columns.RelativeColumn(1.2f); // 24h %
                columns.RelativeColumn(2);    // Market Cap
            });

            // Header
            table.Header(header =>
            {
                header.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("#").Bold().FontColor(Colors.White);
                header.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("Name").Bold().FontColor(Colors.White);
                header.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("Symbol").Bold().FontColor(Colors.White);
                header.Cell().Background(Colors.Grey.Darken3).Padding(4).AlignRight().Text("Price").Bold().FontColor(Colors.White);
                header.Cell().Background(Colors.Grey.Darken3).Padding(4).AlignRight().Text("24h %").Bold().FontColor(Colors.White);
                header.Cell().Background(Colors.Grey.Darken3).Padding(4).AlignRight().Text("Market Cap").Bold().FontColor(Colors.White);
            });

            bool alternate = false;
            foreach (var crypto in cryptos)
            {
                var bgColor = alternate ? Colors.Grey.Lighten4 : Colors.White;
                var changeColor = crypto.PriceChangePercentage24h >= 0 ? PositiveColor : NegativeColor;

                table.Cell().Background(bgColor).Padding(4).Text($"{crypto.MarketCapRank}");
                table.Cell().Background(bgColor).Padding(4).Text(crypto.Name);
                table.Cell().Background(bgColor).Padding(4).Text(crypto.Symbol.ToUpper());
                table.Cell().Background(bgColor).Padding(4).AlignRight().Text($"${crypto.CurrentPrice:N2}");
                table.Cell().Background(bgColor).Padding(4).AlignRight().Text($"{(crypto.PriceChangePercentage24h >= 0 ? "+" : "")}{crypto.PriceChangePercentage24h:N2}%").FontColor(changeColor);
                table.Cell().Background(bgColor).Padding(4).AlignRight().Text($"${crypto.MarketCap:N0}");

                alternate = !alternate;
            }
        });
    }
}
