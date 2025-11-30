using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using CryptoTrader.Shared.Models;
using CryptoTrader.Shared.Config;

namespace CryptoTrader.App.Services;

/// <summary>
/// HTTP client service for communicating with the CryptoTrader REST API
/// </summary>
public class ApiClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly UserSettings _settings;
    private string? _authToken;

    public ApiClient(UserSettings settings)
    {
        _settings = settings;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri($"http://{settings.ServerAddress}:{settings.HttpPort}/api/"),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public void SetAuthToken(string? token)
    {
        _authToken = token;
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
        else
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }

    #region Authentication

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("auth/register", request);
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        
        if (result?.Success == true && result.Session != null)
        {
            SetAuthToken(result.Session.Token);
        }
        
        return result ?? new AuthResponse { Success = false, Message = "Failed to parse response" };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("auth/login", request);
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        
        if (result?.Success == true && result.Session != null)
        {
            SetAuthToken(result.Session.Token);
            
            if (request.RememberMe)
            {
                _settings.SavedUsername = request.Username;
                _settings.SavedToken = result.Session.Token;
                _settings.RememberMe = true;
                _settings.Save();
            }
        }
        
        return result ?? new AuthResponse { Success = false, Message = "Failed to parse response" };
    }

    public async Task<AuthResponse> ValidateTokenAsync(string token)
    {
        var tempToken = _authToken;
        SetAuthToken(token);
        
        try
        {
            var response = await _httpClient.PostAsync("auth/validate", null);
            var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
            
            if (result?.Success != true)
            {
                SetAuthToken(tempToken);
            }
            
            return result ?? new AuthResponse { Success = false, Message = "Failed to validate token" };
        }
        catch
        {
            SetAuthToken(tempToken);
            return new AuthResponse { Success = false, Message = "Connection failed" };
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            await _httpClient.PostAsync("auth/logout", null);
        }
        catch { }
        
        SetAuthToken(null);
        _settings.ClearAuthData();
    }

    #endregion

    #region Crypto Data

    public async Task<List<CryptoCurrency>> GetTopCryptosAsync(int count = 50, string currency = "usd")
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<CryptoCurrency>>($"crypto/top?count={count}&currency={currency}")
                ?? new List<CryptoCurrency>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching cryptos: {ex.Message}");
            return new List<CryptoCurrency>();
        }
    }

    public async Task<CryptoCurrency?> GetCryptoAsync(string coinId, string currency = "usd")
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<CryptoCurrency>($"crypto/{coinId}?currency={currency}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<CryptoCurrency>> SearchCryptosAsync(string query)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<CryptoCurrency>>($"crypto/search?query={Uri.EscapeDataString(query)}")
                ?? new List<CryptoCurrency>();
        }
        catch
        {
            return new List<CryptoCurrency>();
        }
    }

    public async Task<List<CryptoCurrency>> GetLatestPricesAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<CryptoCurrency>>("crypto/latest")
                ?? new List<CryptoCurrency>();
        }
        catch
        {
            return new List<CryptoCurrency>();
        }
    }

    #endregion

    #region Portfolio

    public async Task<List<CryptoHolding>> GetHoldingsAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<CryptoHolding>>("portfolio/holdings")
                ?? new List<CryptoHolding>();
        }
        catch
        {
            return new List<CryptoHolding>();
        }
    }

    public async Task<object?> GetPortfolioSummaryAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<object>("portfolio/summary");
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<CryptoTransaction>> GetTransactionsAsync(int limit = 100)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<CryptoTransaction>>($"portfolio/transactions?limit={limit}")
                ?? new List<CryptoTransaction>();
        }
        catch
        {
            return new List<CryptoTransaction>();
        }
    }

    #endregion

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
