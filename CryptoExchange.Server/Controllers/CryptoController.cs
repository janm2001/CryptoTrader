using Microsoft.AspNetCore.Mvc;
using CryptoTrader.Shared.Models;
using CryptoExchange.Server.Services;
using CryptoExchange.Server.Data;

namespace CryptoExchange.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CryptoController : ControllerBase
{
    private readonly CryptoApiService _cryptoService;
    private readonly PriceUpdateService _priceUpdateService;
    private readonly DatabaseContext _db;

    public CryptoController(CryptoApiService cryptoService, PriceUpdateService priceUpdateService, DatabaseContext db)
    {
        _cryptoService = cryptoService;
        _priceUpdateService = priceUpdateService;
        _db = db;
    }

    /// <summary>
    /// Gets top cryptocurrencies by market cap
    /// </summary>
    [HttpGet("top")]
    public async Task<ActionResult<List<CryptoCurrency>>> GetTopCryptos(
        [FromQuery] int count = 50,
        [FromQuery] string currency = "usd")
    {
        var cryptos = await _cryptoService.GetTopCryptosAsync(count, currency);
        return Ok(cryptos);
    }

    /// <summary>
    /// Gets cryptocurrency by ID
    /// </summary>
    [HttpGet("{coinId}")]
    public async Task<ActionResult<CryptoCurrency>> GetCrypto(string coinId, [FromQuery] string currency = "usd")
    {
        var crypto = await _cryptoService.GetCryptoDetailsAsync(coinId, currency);
        if (crypto == null)
        {
            return NotFound(new { message = $"Cryptocurrency '{coinId}' not found" });
        }
        return Ok(crypto);
    }

    /// <summary>
    /// Gets multiple cryptocurrencies by IDs
    /// </summary>
    [HttpPost("batch")]
    public async Task<ActionResult<List<CryptoCurrency>>> GetCryptosByIds(
        [FromBody] List<string> coinIds,
        [FromQuery] string currency = "usd")
    {
        if (coinIds == null || coinIds.Count == 0)
        {
            return BadRequest(new { message = "No coin IDs provided" });
        }

        var cryptos = await _cryptoService.GetCryptosByIdsAsync(coinIds, currency);
        return Ok(cryptos);
    }

    /// <summary>
    /// Gets simple prices for multiple coins
    /// </summary>
    [HttpGet("prices")]
    public async Task<ActionResult<Dictionary<string, decimal>>> GetSimplePrices(
        [FromQuery] string ids,
        [FromQuery] string currency = "usd")
    {
        if (string.IsNullOrEmpty(ids))
        {
            return BadRequest(new { message = "No coin IDs provided" });
        }

        var coinIds = ids.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var prices = await _cryptoService.GetSimplePricesAsync(coinIds, currency);
        return Ok(prices);
    }

    /// <summary>
    /// Search for cryptocurrencies
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<List<CryptoCurrency>>> SearchCryptos([FromQuery] string query)
    {
        if (string.IsNullOrEmpty(query))
        {
            return BadRequest(new { message = "Search query is required" });
        }

        var results = await _cryptoService.SearchCryptosAsync(query);
        return Ok(results);
    }

    /// <summary>
    /// Gets cached prices (no external API call)
    /// </summary>
    [HttpGet("cached")]
    public async Task<ActionResult<List<CryptoCurrency>>> GetCachedPrices()
    {
        var prices = await _cryptoService.GetCachedPricesAsync();
        return Ok(prices);
    }

    /// <summary>
    /// Gets last fetched prices from the price update service
    /// </summary>
    [HttpGet("latest")]
    public ActionResult<List<CryptoCurrency>> GetLatestPrices()
    {
        var prices = _priceUpdateService.GetLastPrices();
        return Ok(prices);
    }

    /// <summary>
    /// Gets supported fiat currencies
    /// </summary>
    [HttpGet("currencies")]
    public async Task<ActionResult<List<string>>> GetSupportedCurrencies()
    {
        var currencies = await _cryptoService.GetSupportedCurrenciesAsync();
        return Ok(currencies);
    }

    /// <summary>
    /// Gets price history for a specific cryptocurrency
    /// </summary>
    [HttpGet("{coinId}/history")]
    public async Task<ActionResult<IEnumerable<CryptoPriceHistory>>> GetPriceHistory(
        string coinId,
        [FromQuery] int days = 30)
    {
        var history = await _db.GetPriceHistoryAsync(coinId, days);
        return Ok(history);
    }

    /// <summary>
    /// Gets the latest price history entries for a cryptocurrency
    /// </summary>
    [HttpGet("{coinId}/history/latest")]
    public async Task<ActionResult<IEnumerable<CryptoPriceHistory>>> GetLatestPriceHistory(
        string coinId,
        [FromQuery] int limit = 100)
    {
        var history = await _db.GetLatestPriceHistoryAsync(coinId, limit);
        return Ok(history);
    }
}
