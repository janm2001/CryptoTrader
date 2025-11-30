using Microsoft.AspNetCore.Mvc;
using CryptoTrader.Shared.Models;
using CryptoExchange.Server.Data;
using CryptoExchange.Server.Services;

namespace CryptoExchange.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PortfolioController : ControllerBase
{
    private readonly DatabaseContext _db;
    private readonly AuthService _authService;
    private readonly CryptoApiService _cryptoService;

    public PortfolioController(DatabaseContext db, AuthService authService, CryptoApiService cryptoService)
    {
        _db = db;
        _authService = authService;
        _cryptoService = cryptoService;
    }

    private async Task<UserSession?> GetSessionAsync()
    {
        var token = Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrEmpty(token)) return null;

        if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            token = token[7..];
        }

        var result = await _authService.ValidateTokenAsync(token);
        return result.Success ? result.Session : null;
    }

    /// <summary>
    /// Gets user's crypto holdings
    /// </summary>
    [HttpGet("holdings")]
    public async Task<ActionResult<List<CryptoHolding>>> GetHoldings()
    {
        var session = await GetSessionAsync();
        if (session == null)
        {
            return Unauthorized(new { message = "Invalid or missing token" });
        }

        var holdings = await _db.GetUserHoldingsAsync(session.UserId);
        return Ok(holdings);
    }

    /// <summary>
    /// Gets user's holdings with current values
    /// </summary>
    [HttpGet("holdings/detailed")]
    public async Task<ActionResult> GetDetailedHoldings()
    {
        var session = await GetSessionAsync();
        if (session == null)
        {
            return Unauthorized(new { message = "Invalid or missing token" });
        }

        var holdings = await _db.GetUserHoldingsAsync(session.UserId);
        var holdingsList = holdings.ToList();
        
        if (holdingsList.Count == 0)
        {
            return Ok(new List<object>());
        }

        var coinIds = holdingsList.Select(h => h.CoinId).Distinct();
        var prices = await _cryptoService.GetCryptosByIdsAsync(coinIds);
        var priceDict = prices.ToDictionary(p => p.CoinId, p => p.CurrentPrice);

        var result = holdingsList.Select(h =>
        {
            var currentPrice = priceDict.GetValueOrDefault(h.CoinId, 0);
            return new
            {
                h.Id,
                h.CoinId,
                h.Symbol,
                h.Amount,
                h.PurchasePrice,
                h.PurchaseDate,
                CurrentPrice = currentPrice,
                CurrentValue = h.GetCurrentValue(currentPrice),
                ProfitLoss = h.GetProfitLoss(currentPrice),
                ProfitLossPercentage = h.GetProfitLossPercentage(currentPrice)
            };
        });

        return Ok(result);
    }

    /// <summary>
    /// Adds a new holding
    /// </summary>
    [HttpPost("holdings")]
    public async Task<ActionResult<CryptoHolding>> AddHolding([FromBody] AddHoldingRequest request)
    {
        var session = await GetSessionAsync();
        if (session == null)
        {
            return Unauthorized(new { message = "Invalid or missing token" });
        }

        var holding = new CryptoHolding
        {
            UserId = session.UserId,
            CoinId = request.CoinId,
            Symbol = request.Symbol,
            Amount = request.Amount,
            PurchasePrice = request.PurchasePrice,
            PurchaseDate = request.PurchaseDate ?? DateTime.UtcNow
        };

        holding.Id = await _db.AddHoldingAsync(holding);
        return CreatedAtAction(nameof(GetHoldings), new { id = holding.Id }, holding);
    }

    /// <summary>
    /// Updates a holding
    /// </summary>
    [HttpPut("holdings/{id}")]
    public async Task<ActionResult> UpdateHolding(int id, [FromBody] UpdateHoldingRequest request)
    {
        var session = await GetSessionAsync();
        if (session == null)
        {
            return Unauthorized(new { message = "Invalid or missing token" });
        }

        var holdings = await _db.GetUserHoldingsAsync(session.UserId);
        var holding = holdings.FirstOrDefault(h => h.Id == id);
        
        if (holding == null)
        {
            return NotFound(new { message = "Holding not found" });
        }

        holding.Amount = request.Amount ?? holding.Amount;
        holding.PurchasePrice = request.PurchasePrice ?? holding.PurchasePrice;

        await _db.UpdateHoldingAsync(holding);
        return Ok(holding);
    }

    /// <summary>
    /// Deletes a holding
    /// </summary>
    [HttpDelete("holdings/{id}")]
    public async Task<ActionResult> DeleteHolding(int id)
    {
        var session = await GetSessionAsync();
        if (session == null)
        {
            return Unauthorized(new { message = "Invalid or missing token" });
        }

        await _db.DeleteHoldingAsync(id, session.UserId);
        return NoContent();
    }

    /// <summary>
    /// Gets user's transaction history
    /// </summary>
    [HttpGet("transactions")]
    public async Task<ActionResult<List<CryptoTransaction>>> GetTransactions([FromQuery] int limit = 100)
    {
        var session = await GetSessionAsync();
        if (session == null)
        {
            return Unauthorized(new { message = "Invalid or missing token" });
        }

        var transactions = await _db.GetUserTransactionsAsync(session.UserId, limit);
        return Ok(transactions);
    }

    /// <summary>
    /// Records a new transaction
    /// </summary>
    [HttpPost("transactions")]
    public async Task<ActionResult<CryptoTransaction>> AddTransaction([FromBody] AddTransactionRequest request)
    {
        var session = await GetSessionAsync();
        if (session == null)
        {
            return Unauthorized(new { message = "Invalid or missing token" });
        }

        var transaction = new CryptoTransaction
        {
            UserId = session.UserId,
            CoinId = request.CoinId,
            Symbol = request.Symbol,
            Type = request.Type,
            Amount = request.Amount,
            PricePerUnit = request.PricePerUnit,
            TotalValue = request.Amount * request.PricePerUnit,
            Fee = request.Fee,
            Timestamp = request.Timestamp ?? DateTime.UtcNow,
            Notes = request.Notes
        };

        transaction.Id = await _db.AddTransactionAsync(transaction);
        return CreatedAtAction(nameof(GetTransactions), new { id = transaction.Id }, transaction);
    }

    /// <summary>
    /// Gets portfolio summary
    /// </summary>
    [HttpGet("summary")]
    public async Task<ActionResult> GetPortfolioSummary()
    {
        var session = await GetSessionAsync();
        if (session == null)
        {
            return Unauthorized(new { message = "Invalid or missing token" });
        }

        var holdings = (await _db.GetUserHoldingsAsync(session.UserId)).ToList();
        
        if (holdings.Count == 0)
        {
            return Ok(new
            {
                TotalValue = 0m,
                TotalCost = 0m,
                TotalProfitLoss = 0m,
                TotalProfitLossPercentage = 0m,
                HoldingsCount = 0
            });
        }

        var coinIds = holdings.Select(h => h.CoinId).Distinct();
        var prices = await _cryptoService.GetCryptosByIdsAsync(coinIds);
        var priceDict = prices.ToDictionary(p => p.CoinId, p => p.CurrentPrice);

        var totalValue = holdings.Sum(h => h.GetCurrentValue(priceDict.GetValueOrDefault(h.CoinId, 0)));
        var totalCost = holdings.Sum(h => h.Amount * h.PurchasePrice);
        var totalProfitLoss = totalValue - totalCost;
        var totalProfitLossPercentage = totalCost > 0 ? (totalProfitLoss / totalCost) * 100 : 0;

        return Ok(new
        {
            TotalValue = totalValue,
            TotalCost = totalCost,
            TotalProfitLoss = totalProfitLoss,
            TotalProfitLossPercentage = totalProfitLossPercentage,
            HoldingsCount = holdings.Count
        });
    }
}

public class AddHoldingRequest
{
    public string CoinId { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal PurchasePrice { get; set; }
    public DateTime? PurchaseDate { get; set; }
}

public class UpdateHoldingRequest
{
    public decimal? Amount { get; set; }
    public decimal? PurchasePrice { get; set; }
}

public class AddTransactionRequest
{
    public string CoinId { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public decimal PricePerUnit { get; set; }
    public decimal Fee { get; set; }
    public DateTime? Timestamp { get; set; }
    public string? Notes { get; set; }
}
