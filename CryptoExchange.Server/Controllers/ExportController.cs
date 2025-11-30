using Microsoft.AspNetCore.Mvc;
using ClosedXML.Excel;
using CryptoTrader.Shared.Models;
using CryptoExchange.Server.Data;
using CryptoExchange.Server.Services;

namespace CryptoExchange.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExportController : ControllerBase
{
    private readonly DatabaseContext _db;
    private readonly AuthService _authService;
    private readonly CryptoApiService _cryptoService;

    public ExportController(DatabaseContext db, AuthService authService, CryptoApiService cryptoService)
    {
        _db = db;
        _authService = authService;
        _cryptoService = cryptoService;
    }

    private async Task<(UserSession? session, bool isAdmin)> GetSessionAndRoleAsync()
    {
        var token = Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrEmpty(token)) return (null, false);

        if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            token = token[7..];
        }

        var result = await _authService.ValidateTokenAsync(token);
        if (!result.Success || result.Session == null) return (null, false);

        var user = await _db.GetUserByIdAsync(result.Session.UserId);
        return (result.Session, user?.Role == UserRole.Admin);
    }

    /// <summary>
    /// Export cryptocurrency prices to Excel (available to all authenticated users)
    /// </summary>
    [HttpGet("prices/excel")]
    public async Task<IActionResult> ExportPricesToExcel()
    {
        var (session, _) = await GetSessionAndRoleAsync();
        if (session == null)
        {
            return Unauthorized(new { message = "Authentication required" });
        }

        var prices = await _cryptoService.GetCachedPricesAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Crypto Prices");

        // Header row with styling
        var headers = new[] { "Rank", "Name", "Symbol", "Price (USD)", "24h Change %", "Market Cap", "Volume 24h", "Circulating Supply", "Last Updated" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.DarkBlue;
            cell.Style.Font.FontColor = XLColor.White;
        }

        // Data rows
        int row = 2;
        foreach (var crypto in prices.OrderBy(p => p.MarketCapRank))
        {
            worksheet.Cell(row, 1).Value = crypto.MarketCapRank;
            worksheet.Cell(row, 2).Value = crypto.Name;
            worksheet.Cell(row, 3).Value = crypto.Symbol.ToUpper();
            worksheet.Cell(row, 4).Value = (double)crypto.CurrentPrice;
            worksheet.Cell(row, 4).Style.NumberFormat.Format = "$#,##0.00";
            worksheet.Cell(row, 5).Value = (double)crypto.PriceChangePercentage24h;
            worksheet.Cell(row, 5).Style.NumberFormat.Format = "0.00%";
            worksheet.Cell(row, 5).Style.Font.FontColor = crypto.PriceChangePercentage24h >= 0 ? XLColor.Green : XLColor.Red;
            worksheet.Cell(row, 6).Value = (double)crypto.MarketCap;
            worksheet.Cell(row, 6).Style.NumberFormat.Format = "$#,##0";
            worksheet.Cell(row, 7).Value = (double)crypto.TotalVolume;
            worksheet.Cell(row, 7).Style.NumberFormat.Format = "$#,##0";
            worksheet.Cell(row, 8).Value = (double)crypto.CirculatingSupply;
            worksheet.Cell(row, 8).Style.NumberFormat.Format = "#,##0";
            worksheet.Cell(row, 9).Value = crypto.LastUpdated.ToString("yyyy-MM-dd HH:mm:ss");
            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var fileName = $"CryptoPrices_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    /// <summary>
    /// Export user's portfolio holdings to Excel (available to all authenticated users)
    /// </summary>
    [HttpGet("holdings/excel")]
    public async Task<IActionResult> ExportHoldingsToExcel()
    {
        var (session, _) = await GetSessionAndRoleAsync();
        if (session == null)
        {
            return Unauthorized(new { message = "Authentication required" });
        }

        var holdings = (await _db.GetUserHoldingsAsync(session.UserId)).ToList();
        var coinIds = holdings.Select(h => h.CoinId).Distinct();
        var prices = await _cryptoService.GetCryptosByIdsAsync(coinIds);
        var priceDict = prices.ToDictionary(p => p.CoinId, p => p.CurrentPrice);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("My Holdings");

        // Header row
        var headers = new[] { "Coin", "Symbol", "Amount", "Purchase Price", "Current Price", "Current Value", "Profit/Loss", "P/L %", "Purchase Date" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.DarkGreen;
            cell.Style.Font.FontColor = XLColor.White;
        }

        int row = 2;
        decimal totalValue = 0, totalCost = 0;

        foreach (var holding in holdings)
        {
            var currentPrice = priceDict.GetValueOrDefault(holding.CoinId, 0);
            var currentValue = holding.GetCurrentValue(currentPrice);
            var profitLoss = holding.GetProfitLoss(currentPrice);
            var profitLossPercent = holding.GetProfitLossPercentage(currentPrice);

            worksheet.Cell(row, 1).Value = holding.CoinId;
            worksheet.Cell(row, 2).Value = holding.Symbol.ToUpper();
            worksheet.Cell(row, 3).Value = (double)holding.Amount;
            worksheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0.0000";
            worksheet.Cell(row, 4).Value = (double)holding.PurchasePrice;
            worksheet.Cell(row, 4).Style.NumberFormat.Format = "$#,##0.00";
            worksheet.Cell(row, 5).Value = (double)currentPrice;
            worksheet.Cell(row, 5).Style.NumberFormat.Format = "$#,##0.00";
            worksheet.Cell(row, 6).Value = (double)currentValue;
            worksheet.Cell(row, 6).Style.NumberFormat.Format = "$#,##0.00";
            worksheet.Cell(row, 7).Value = (double)profitLoss;
            worksheet.Cell(row, 7).Style.NumberFormat.Format = "$#,##0.00";
            worksheet.Cell(row, 7).Style.Font.FontColor = profitLoss >= 0 ? XLColor.Green : XLColor.Red;
            worksheet.Cell(row, 8).Value = (double)(profitLossPercent / 100);
            worksheet.Cell(row, 8).Style.NumberFormat.Format = "0.00%";
            worksheet.Cell(row, 8).Style.Font.FontColor = profitLossPercent >= 0 ? XLColor.Green : XLColor.Red;
            worksheet.Cell(row, 9).Value = holding.PurchaseDate.ToString("yyyy-MM-dd");

            totalValue += currentValue;
            totalCost += holding.Amount * holding.PurchasePrice;
            row++;
        }

        // Summary row
        row++;
        worksheet.Cell(row, 5).Value = "TOTAL:";
        worksheet.Cell(row, 5).Style.Font.Bold = true;
        worksheet.Cell(row, 6).Value = (double)totalValue;
        worksheet.Cell(row, 6).Style.NumberFormat.Format = "$#,##0.00";
        worksheet.Cell(row, 6).Style.Font.Bold = true;
        worksheet.Cell(row, 7).Value = (double)(totalValue - totalCost);
        worksheet.Cell(row, 7).Style.NumberFormat.Format = "$#,##0.00";
        worksheet.Cell(row, 7).Style.Font.Bold = true;
        worksheet.Cell(row, 7).Style.Font.FontColor = (totalValue - totalCost) >= 0 ? XLColor.Green : XLColor.Red;

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var fileName = $"MyHoldings_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    /// <summary>
    /// Export transaction history to Excel (available to all authenticated users)
    /// </summary>
    [HttpGet("transactions/excel")]
    public async Task<IActionResult> ExportTransactionsToExcel()
    {
        var (session, _) = await GetSessionAndRoleAsync();
        if (session == null)
        {
            return Unauthorized(new { message = "Authentication required" });
        }

        var transactions = await _db.GetUserTransactionsAsync(session.UserId, 1000);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Transactions");

        // Header row
        var headers = new[] { "Date", "Type", "Coin", "Symbol", "Amount", "Price/Unit", "Total Value", "Fee", "Notes" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.DarkOrange;
            cell.Style.Font.FontColor = XLColor.White;
        }

        int row = 2;
        foreach (var tx in transactions)
        {
            worksheet.Cell(row, 1).Value = tx.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
            worksheet.Cell(row, 2).Value = tx.Type.ToString();
            worksheet.Cell(row, 2).Style.Font.FontColor = tx.Type == TransactionType.Buy ? XLColor.Green : XLColor.Red;
            worksheet.Cell(row, 3).Value = tx.CoinId;
            worksheet.Cell(row, 4).Value = tx.Symbol.ToUpper();
            worksheet.Cell(row, 5).Value = (double)tx.Amount;
            worksheet.Cell(row, 5).Style.NumberFormat.Format = "#,##0.0000";
            worksheet.Cell(row, 6).Value = (double)tx.PricePerUnit;
            worksheet.Cell(row, 6).Style.NumberFormat.Format = "$#,##0.00";
            worksheet.Cell(row, 7).Value = (double)tx.TotalValue;
            worksheet.Cell(row, 7).Style.NumberFormat.Format = "$#,##0.00";
            worksheet.Cell(row, 8).Value = (double)tx.Fee;
            worksheet.Cell(row, 8).Style.NumberFormat.Format = "$#,##0.00";
            worksheet.Cell(row, 9).Value = tx.Notes ?? "";
            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var fileName = $"Transactions_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    /// <summary>
    /// [ADMIN ONLY] Export all users list to Excel
    /// </summary>
    [HttpGet("admin/users/excel")]
    public async Task<IActionResult> ExportAllUsersToExcel()
    {
        var (session, isAdmin) = await GetSessionAndRoleAsync();
        if (session == null)
        {
            return Unauthorized(new { message = "Authentication required" });
        }

        if (!isAdmin)
        {
            return StatusCode(403, new { message = "Admin access required" });
        }

        var users = await _db.GetAllUsersAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("All Users");

        // Header row
        var headers = new[] { "ID", "Username", "Email", "Role", "Active", "Created At", "Last Login" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.DarkRed;
            cell.Style.Font.FontColor = XLColor.White;
        }

        int row = 2;
        foreach (var user in users)
        {
            worksheet.Cell(row, 1).Value = user.Id;
            worksheet.Cell(row, 2).Value = user.Username;
            worksheet.Cell(row, 3).Value = user.Email;
            worksheet.Cell(row, 4).Value = user.Role.ToString();
            worksheet.Cell(row, 4).Style.Font.FontColor = user.Role == UserRole.Admin ? XLColor.Red : XLColor.Black;
            worksheet.Cell(row, 5).Value = user.IsActive ? "Yes" : "No";
            worksheet.Cell(row, 5).Style.Font.FontColor = user.IsActive ? XLColor.Green : XLColor.Red;
            worksheet.Cell(row, 6).Value = user.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
            worksheet.Cell(row, 7).Value = user.LastLoginAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Never";
            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var fileName = $"AllUsers_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    /// <summary>
    /// [ADMIN ONLY] Export complete system report to Excel
    /// </summary>
    [HttpGet("admin/report/excel")]
    public async Task<IActionResult> ExportSystemReportToExcel()
    {
        var (session, isAdmin) = await GetSessionAndRoleAsync();
        if (session == null)
        {
            return Unauthorized(new { message = "Authentication required" });
        }

        if (!isAdmin)
        {
            return StatusCode(403, new { message = "Admin access required" });
        }

        using var workbook = new XLWorkbook();

        // Sheet 1: Users
        var usersSheet = workbook.Worksheets.Add("Users");
        var users = await _db.GetAllUsersAsync();
        AddUsersToSheet(usersSheet, users);

        // Sheet 2: All Crypto Prices
        var pricesSheet = workbook.Worksheets.Add("Crypto Prices");
        var prices = await _cryptoService.GetCachedPricesAsync();
        AddPricesToSheet(pricesSheet, prices);

        // Sheet 3: Summary Statistics
        var statsSheet = workbook.Worksheets.Add("Statistics");
        statsSheet.Cell(1, 1).Value = "System Statistics";
        statsSheet.Cell(1, 1).Style.Font.Bold = true;
        statsSheet.Cell(1, 1).Style.Font.FontSize = 16;

        statsSheet.Cell(3, 1).Value = "Total Users:";
        statsSheet.Cell(3, 2).Value = users.Count();
        statsSheet.Cell(4, 1).Value = "Admin Users:";
        statsSheet.Cell(4, 2).Value = users.Count(u => u.Role == UserRole.Admin);
        statsSheet.Cell(5, 1).Value = "Active Users:";
        statsSheet.Cell(5, 2).Value = users.Count(u => u.IsActive);
        statsSheet.Cell(6, 1).Value = "Tracked Cryptocurrencies:";
        statsSheet.Cell(6, 2).Value = prices.Count;
        statsSheet.Cell(7, 1).Value = "Report Generated:";
        statsSheet.Cell(7, 2).Value = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");

        statsSheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var fileName = $"SystemReport_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    private void AddUsersToSheet(IXLWorksheet sheet, IEnumerable<User> users)
    {
        var headers = new[] { "ID", "Username", "Email", "Role", "Active", "Created At" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = sheet.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.DarkBlue;
            cell.Style.Font.FontColor = XLColor.White;
        }

        int row = 2;
        foreach (var user in users)
        {
            sheet.Cell(row, 1).Value = user.Id;
            sheet.Cell(row, 2).Value = user.Username;
            sheet.Cell(row, 3).Value = user.Email;
            sheet.Cell(row, 4).Value = user.Role.ToString();
            sheet.Cell(row, 5).Value = user.IsActive ? "Yes" : "No";
            sheet.Cell(row, 6).Value = user.CreatedAt.ToString("yyyy-MM-dd");
            row++;
        }
        sheet.Columns().AdjustToContents();
    }

    private void AddPricesToSheet(IXLWorksheet sheet, IEnumerable<CryptoCurrency> prices)
    {
        var headers = new[] { "Rank", "Name", "Symbol", "Price", "24h Change %", "Market Cap" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = sheet.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.DarkGreen;
            cell.Style.Font.FontColor = XLColor.White;
        }

        int row = 2;
        foreach (var crypto in prices.OrderBy(p => p.MarketCapRank))
        {
            sheet.Cell(row, 1).Value = crypto.MarketCapRank;
            sheet.Cell(row, 2).Value = crypto.Name;
            sheet.Cell(row, 3).Value = crypto.Symbol.ToUpper();
            sheet.Cell(row, 4).Value = (double)crypto.CurrentPrice;
            sheet.Cell(row, 4).Style.NumberFormat.Format = "$#,##0.00";
            sheet.Cell(row, 5).Value = (double)crypto.PriceChangePercentage24h;
            sheet.Cell(row, 5).Style.NumberFormat.Format = "0.00%";
            sheet.Cell(row, 6).Value = (double)crypto.MarketCap;
            sheet.Cell(row, 6).Style.NumberFormat.Format = "$#,##0";
            row++;
        }
        sheet.Columns().AdjustToContents();
    }
}
