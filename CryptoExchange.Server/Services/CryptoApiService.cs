using System.Net.Http.Json;
using System.Text.Json;
using CryptoTrader.Shared.Models;
using CryptoExchange.Server.Data;

namespace CryptoExchange.Server.Services;

/// <summary>
/// Service for fetching cryptocurrency data from external APIs
/// </summary>
public class CryptoApiService
{
    private readonly HttpClient _httpClient;
    private readonly DatabaseContext _db;
    private readonly string _baseUrl;

    public CryptoApiService(DatabaseContext db, string baseUrl = "https://api.coingecko.com/api/v3")
    {
        _db = db;
        _baseUrl = baseUrl;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_baseUrl)
        };
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "CryptoTrader/1.0");
    }

    /// <summary>
    /// Fetches top cryptocurrencies by market cap
    /// </summary>
    public async Task<List<CryptoCurrency>> GetTopCryptosAsync(int count = 100, string currency = "usd")
    {
        try
        {
            var url = $"/coins/markets?vs_currency={currency}&order=market_cap_desc&per_page={count}&page=1&sparkline=false";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var cryptos = await response.Content.ReadFromJsonAsync<List<CryptoCurrency>>();
            
            if (cryptos != null)
            {
                // Save to database
                foreach (var crypto in cryptos)
                {
                    await _db.UpsertPriceAsync(crypto);
                }
            }

            return cryptos ?? new List<CryptoCurrency>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching crypto data: {ex.Message}");
            
            // Return cached data from database
            var cached = await _db.GetAllPricesAsync();
            return cached.Take(count).ToList();
        }
    }

    /// <summary>
    /// Fetches specific cryptocurrencies by their IDs
    /// </summary>
    public async Task<List<CryptoCurrency>> GetCryptosByIdsAsync(IEnumerable<string> coinIds, string currency = "usd")
    {
        try
        {
            var ids = string.Join(",", coinIds);
            var url = $"/coins/markets?vs_currency={currency}&ids={ids}&order=market_cap_desc&sparkline=false";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var cryptos = await response.Content.ReadFromJsonAsync<List<CryptoCurrency>>();
            
            if (cryptos != null)
            {
                foreach (var crypto in cryptos)
                {
                    await _db.UpsertPriceAsync(crypto);
                }
            }

            return cryptos ?? new List<CryptoCurrency>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching crypto data: {ex.Message}");
            
            // Return cached data
            var result = new List<CryptoCurrency>();
            foreach (var id in coinIds)
            {
                var cached = await _db.GetPriceAsync(id);
                if (cached != null)
                    result.Add(cached);
            }
            return result;
        }
    }

    /// <summary>
    /// Fetches detailed information about a specific cryptocurrency
    /// </summary>
    public async Task<CryptoCurrency?> GetCryptoDetailsAsync(string coinId, string currency = "usd")
    {
        try
        {
            var cryptos = await GetCryptosByIdsAsync(new[] { coinId }, currency);
            return cryptos.FirstOrDefault();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching crypto details: {ex.Message}");
            return await _db.GetPriceAsync(coinId);
        }
    }

    /// <summary>
    /// Gets simple price for multiple coins (lighter endpoint)
    /// </summary>
    public async Task<Dictionary<string, decimal>> GetSimplePricesAsync(IEnumerable<string> coinIds, string currency = "usd")
    {
        try
        {
            var ids = string.Join(",", coinIds);
            var url = $"/simple/price?ids={ids}&vs_currencies={currency}";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var prices = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, decimal>>>(json);

            var result = new Dictionary<string, decimal>();
            if (prices != null)
            {
                foreach (var kvp in prices)
                {
                    if (kvp.Value.TryGetValue(currency, out var price))
                    {
                        result[kvp.Key] = price;
                    }
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching simple prices: {ex.Message}");
            return new Dictionary<string, decimal>();
        }
    }

    /// <summary>
    /// Searches for cryptocurrencies by name or symbol
    /// </summary>
    public async Task<List<CryptoCurrency>> SearchCryptosAsync(string query)
    {
        try
        {
            var url = $"/search?query={Uri.EscapeDataString(query)}";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var searchResult = JsonSerializer.Deserialize<JsonElement>(json);

            var coinIds = new List<string>();
            if (searchResult.TryGetProperty("coins", out var coins))
            {
                foreach (var coin in coins.EnumerateArray().Take(10))
                {
                    if (coin.TryGetProperty("id", out var id))
                    {
                        coinIds.Add(id.GetString() ?? "");
                    }
                }
            }

            if (coinIds.Count > 0)
            {
                return await GetCryptosByIdsAsync(coinIds);
            }

            return new List<CryptoCurrency>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error searching cryptos: {ex.Message}");
            return new List<CryptoCurrency>();
        }
    }

    /// <summary>
    /// Gets list of supported currencies
    /// </summary>
    public async Task<List<string>> GetSupportedCurrenciesAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/simple/supported_vs_currencies");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<string>>() ?? new List<string>();
        }
        catch
        {
            return new List<string> { "usd", "eur", "gbp", "btc", "eth" };
        }
    }

    /// <summary>
    /// Get cached prices from database
    /// </summary>
    public async Task<List<CryptoCurrency>> GetCachedPricesAsync()
    {
        var prices = await _db.GetAllPricesAsync();
        return prices.ToList();
    }
}
