using System.Net.Http.Json;
using System.Text.Json;
using CryptoTrader.Shared.Models;
using CryptoExchange.Server.Data;

namespace CryptoExchange.Server.Services;

/// <summary>
/// Service for fetching cryptocurrency data from external APIs
/// Includes rate limiting to respect API quotas
/// </summary>
public class CryptoApiService
{
    private readonly HttpClient _httpClient;
    private readonly DatabaseContext _db;
    private readonly string _baseUrl;
    private readonly string? _apiKey;
    
    // Rate limiting: CoinGecko free tier allows ~10-50 calls/minute
    private readonly SemaphoreSlim _rateLimitSemaphore = new(1, 1);
    private DateTime _lastApiCall = DateTime.MinValue;
    private readonly TimeSpan _minTimeBetweenCalls = TimeSpan.FromMilliseconds(1200); // ~50 calls/minute max
    private int _callCount = 0;
    private DateTime _callCountResetTime = DateTime.UtcNow;
    private const int MaxCallsPerMinute = 30; // Demo API has lower limits

    public CryptoApiService(DatabaseContext db, string baseUrl = "https://api.coingecko.com/api/v3/", string? apiKey = null)
    {
        _db = db;
        // Ensure base URL ends with / for proper relative path resolution
        _baseUrl = baseUrl.EndsWith("/") ? baseUrl : baseUrl + "/";
        _apiKey = apiKey;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_baseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "CryptoTrader/1.0");
        
        // Add Demo API key header if provided
        if (!string.IsNullOrEmpty(_apiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("x-cg-demo-api-key", _apiKey);
            Console.WriteLine($"[CryptoAPI] Using CoinGecko Demo API key: {_apiKey[..Math.Min(10, _apiKey.Length)]}...");
        }
        else
        {
            Console.WriteLine("[CryptoAPI] WARNING: No CoinGecko API key configured. API calls may fail.");
            Console.WriteLine("[CryptoAPI] Get a free Demo API key at: https://www.coingecko.com/en/api/pricing");
        }
    }

    /// <summary>
    /// Applies rate limiting before making API calls to respect CoinGecko limits
    /// </summary>
    private async Task ApplyRateLimitAsync()
    {
        await _rateLimitSemaphore.WaitAsync();
        try
        {
            // Reset counter every minute
            if ((DateTime.UtcNow - _callCountResetTime).TotalMinutes >= 1)
            {
                _callCount = 0;
                _callCountResetTime = DateTime.UtcNow;
            }

            // If we've exceeded max calls per minute, wait until reset
            if (_callCount >= MaxCallsPerMinute)
            {
                var waitTime = TimeSpan.FromMinutes(1) - (DateTime.UtcNow - _callCountResetTime);
                if (waitTime > TimeSpan.Zero)
                {
                    Console.WriteLine($"[RateLimit] Max calls reached. Waiting {waitTime.TotalSeconds:N1}s...");
                    await Task.Delay(waitTime);
                    _callCount = 0;
                    _callCountResetTime = DateTime.UtcNow;
                }
            }

            // Enforce minimum time between calls
            var timeSinceLastCall = DateTime.UtcNow - _lastApiCall;
            if (timeSinceLastCall < _minTimeBetweenCalls)
            {
                var delay = _minTimeBetweenCalls - timeSinceLastCall;
                Console.WriteLine($"[RateLimit] Throttling for {delay.TotalMilliseconds:N0}ms");
                await Task.Delay(delay);
            }

            _lastApiCall = DateTime.UtcNow;
            _callCount++;
        }
        finally
        {
            _rateLimitSemaphore.Release();
        }
    }

    /// <summary>
    /// Gets rate limiting statistics
    /// </summary>
    public (int CallsThisMinute, int MaxCallsPerMinute, TimeSpan TimeUntilReset) GetRateLimitStats()
    {
        var timeUntilReset = TimeSpan.FromMinutes(1) - (DateTime.UtcNow - _callCountResetTime);
        if (timeUntilReset < TimeSpan.Zero) timeUntilReset = TimeSpan.Zero;
        return (_callCount, MaxCallsPerMinute, timeUntilReset);
    }

    /// <summary>
    /// Fetches top cryptocurrencies by market cap
    /// </summary>
    public async Task<List<CryptoCurrency>> GetTopCryptosAsync(int count = 100, string currency = "usd")
    {
        try
        {
            // Apply rate limiting before API call
            await ApplyRateLimitAsync();
            
            // Note: URL must NOT start with / when using BaseAddress, otherwise it ignores the base path
            var url = $"coins/markets?vs_currency={currency}&order=market_cap_desc&per_page={count}&page=1&sparkline=false";
            Console.WriteLine($"[CryptoAPI] Fetching prices from: {_httpClient.BaseAddress}{url}");
            
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[CryptoAPI] API Error: {response.StatusCode} - {errorContent}");
                
                // Return cached data on API error
                var cached = await _db.GetAllPricesAsync();
                Console.WriteLine($"[CryptoAPI] Returning {cached.Count()} cached prices due to API error");
                return cached.Take(count).ToList();
            }

            var cryptos = await response.Content.ReadFromJsonAsync<List<CryptoCurrency>>();
            
            if (cryptos != null && cryptos.Count > 0)
            {
                Console.WriteLine($"[CryptoAPI] Successfully fetched {cryptos.Count} cryptocurrencies");
                
                // Save to database
                foreach (var crypto in cryptos)
                {
                    await _db.UpsertPriceAsync(crypto);
                }
                
                Console.WriteLine($"[CryptoAPI] Updated database with {cryptos.Count} prices");
            }
            else
            {
                Console.WriteLine("[CryptoAPI] Warning: API returned empty or null response");
            }

            return cryptos ?? new List<CryptoCurrency>();
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"[CryptoAPI] HTTP Error: {ex.Message}");
            
            // Return cached data from database
            var cached = await _db.GetAllPricesAsync();
            Console.WriteLine($"[CryptoAPI] Returning {cached.Count()} cached prices");
            return cached.Take(count).ToList();
        }
        catch (TaskCanceledException ex)
        {
            Console.WriteLine($"[CryptoAPI] Request timeout: {ex.Message}");
            
            var cached = await _db.GetAllPricesAsync();
            return cached.Take(count).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CryptoAPI] Unexpected error: {ex.GetType().Name} - {ex.Message}");
            
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
            // Apply rate limiting before API call
            await ApplyRateLimitAsync();
            
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
            // Apply rate limiting before API call
            await ApplyRateLimitAsync();
            
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
        // Handle empty query
        if (string.IsNullOrWhiteSpace(query))
        {
            Console.WriteLine("[Search] Empty query, returning empty list");
            return new List<CryptoCurrency>();
        }

        try
        {
            // Apply rate limiting before API call
            await ApplyRateLimitAsync();
            
            var url = $"/search?query={Uri.EscapeDataString(query)}";
            Console.WriteLine($"[Search] Calling API: {url}");
            
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[Search] Response: {json.Substring(0, Math.Min(500, json.Length))}...");
            
            var searchResult = JsonSerializer.Deserialize<JsonElement>(json);

            var coinIds = new List<string>();
            if (searchResult.TryGetProperty("coins", out var coins))
            {
                foreach (var coin in coins.EnumerateArray().Take(10))
                {
                    if (coin.TryGetProperty("id", out var id))
                    {
                        var coinId = id.GetString();
                        if (!string.IsNullOrEmpty(coinId))
                        {
                            coinIds.Add(coinId);
                            Console.WriteLine($"[Search] Found coin ID: {coinId}");
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("[Search] No 'coins' property found in response");
            }

            if (coinIds.Count > 0)
            {
                Console.WriteLine($"[Search] Fetching details for {coinIds.Count} coins");
                return await GetCryptosByIdsAsync(coinIds);
            }

            Console.WriteLine("[Search] No coin IDs found, returning empty list");
            return new List<CryptoCurrency>();
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"[Search] HTTP Error: {ex.Message}");
            
            // Try to search in cached data instead
            var cached = await _db.GetAllPricesAsync();
            var queryLower = query.ToLowerInvariant();
            return cached
                .Where(c => c.Name.ToLowerInvariant().Contains(queryLower) || 
                           c.Symbol.ToLowerInvariant().Contains(queryLower) ||
                           c.CoinId.ToLowerInvariant().Contains(queryLower))
                .Take(10)
                .ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Search] Error: {ex.Message}");
            Console.WriteLine($"[Search] Stack trace: {ex.StackTrace}");
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
            // Apply rate limiting before API call
            await ApplyRateLimitAsync();
            
            // Note: URL must NOT start with / when using BaseAddress
            var response = await _httpClient.GetAsync("simple/supported_vs_currencies");
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
