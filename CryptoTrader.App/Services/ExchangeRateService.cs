using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace CryptoTrader.App.Services;

/// <summary>
/// Service for fetching real-time currency exchange rates
/// Uses free exchange rate APIs to convert between USD and EUR
/// </summary>
public class ExchangeRateService
{
    private static ExchangeRateService? _instance;
    public static ExchangeRateService Instance => _instance ??= new ExchangeRateService();

    private readonly HttpClient _httpClient;
    private Dictionary<string, decimal> _rates = new();
    private DateTime _lastUpdate = DateTime.MinValue;
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(30);

    // Fallback rates if API fails
    private static readonly Dictionary<string, decimal> FallbackRates = new()
    {
        ["USD"] = 1.0m,
        ["EUR"] = 0.92m,  // Approximate EUR/USD rate
        ["GBP"] = 0.79m   // Approximate GBP/USD rate
    };

    private ExchangeRateService()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
        _rates = new Dictionary<string, decimal>(FallbackRates);
    }

    /// <summary>
    /// Fetches current exchange rates from free API
    /// Uses exchangerate-api.com free tier
    /// </summary>
    public async Task<bool> UpdateExchangeRatesAsync()
    {
        // Only update if cache expired
        if ((DateTime.UtcNow - _lastUpdate) < _cacheExpiry && _rates.Count > 0)
        {
            return true;
        }

        try
        {
            // Try primary API: exchangerate-api.com (free, no key needed for basic rates)
            var response = await _httpClient.GetAsync("https://open.er-api.com/v6/latest/USD");
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ExchangeRateResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result?.Rates != null)
                {
                    _rates.Clear();
                    _rates["USD"] = 1.0m;

                    if (result.Rates.TryGetValue("EUR", out var eurRate))
                        _rates["EUR"] = (decimal)eurRate;
                    if (result.Rates.TryGetValue("GBP", out var gbpRate))
                        _rates["GBP"] = (decimal)gbpRate;

                    _lastUpdate = DateTime.UtcNow;
                    Console.WriteLine($"[ExchangeRate] Updated rates: EUR={_rates["EUR"]:F4}, GBP={_rates.GetValueOrDefault("GBP", 0):F4}");
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ExchangeRate] Primary API failed: {ex.Message}");
        }

        // Try backup API: frankfurter.app (free, ECB rates)
        try
        {
            var response = await _httpClient.GetAsync("https://api.frankfurter.app/latest?from=USD&to=EUR,GBP");
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<FrankfurterResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result?.Rates != null)
                {
                    _rates.Clear();
                    _rates["USD"] = 1.0m;

                    if (result.Rates.TryGetValue("EUR", out var eurRate))
                        _rates["EUR"] = eurRate;
                    if (result.Rates.TryGetValue("GBP", out var gbpRate))
                        _rates["GBP"] = gbpRate;

                    _lastUpdate = DateTime.UtcNow;
                    Console.WriteLine($"[ExchangeRate] Updated rates (backup): EUR={_rates["EUR"]:F4}");
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ExchangeRate] Backup API failed: {ex.Message}");
        }

        // Use fallback rates
        Console.WriteLine("[ExchangeRate] Using fallback rates");
        _rates = new Dictionary<string, decimal>(FallbackRates);
        return false;
    }

    /// <summary>
    /// Converts an amount from USD to the target currency
    /// </summary>
    public decimal ConvertFromUsd(decimal amountInUsd, string targetCurrency)
    {
        if (targetCurrency == "USD") return amountInUsd;

        if (_rates.TryGetValue(targetCurrency, out var rate))
        {
            return amountInUsd * rate;
        }

        return amountInUsd; // Fallback to USD if unknown currency
    }

    /// <summary>
    /// Converts an amount to USD from the source currency
    /// </summary>
    public decimal ConvertToUsd(decimal amount, string sourceCurrency)
    {
        if (sourceCurrency == "USD") return amount;

        if (_rates.TryGetValue(sourceCurrency, out var rate) && rate > 0)
        {
            return amount / rate;
        }

        return amount; // Fallback to original amount if unknown currency
    }

    /// <summary>
    /// Gets the current exchange rate for a currency (relative to USD)
    /// </summary>
    public decimal GetRate(string currency)
    {
        return _rates.GetValueOrDefault(currency, 1.0m);
    }

    /// <summary>
    /// Gets all available rates
    /// </summary>
    public IReadOnlyDictionary<string, decimal> GetAllRates() => _rates;

    /// <summary>
    /// Gets the last update time
    /// </summary>
    public DateTime LastUpdate => _lastUpdate;
}

// Response models for exchange rate APIs
internal class ExchangeRateResponse
{
    public string Result { get; set; } = "";
    public string BaseCode { get; set; } = "";
    public Dictionary<string, double> Rates { get; set; } = new();
}

internal class FrankfurterResponse
{
    public decimal Amount { get; set; }
    public string Base { get; set; } = "";
    public string Date { get; set; } = "";
    public Dictionary<string, decimal> Rates { get; set; } = new();
}
