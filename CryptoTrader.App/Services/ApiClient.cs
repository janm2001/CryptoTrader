using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using CryptoTrader.Shared.Models;
using CryptoTrader.Shared.Config;

namespace CryptoTrader.App.Services;

/// <summary>
/// HTTP client service for communicating with the CryptoTrader REST API
/// </summary>
public class ApiClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private string? _authToken;
    private UserSession? _currentSession;

    public UserSession? CurrentSession => _currentSession;
    public bool IsAuthenticated => _currentSession != null;
    public string? AuthToken => _authToken;

    public ApiClient()
    {
        var settings = SettingsService.Instance;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri($"http://{settings.ServerAddress}:{settings.ServerHttpPort}/api/"),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public void UpdateBaseAddress()
    {
        var settings = SettingsService.Instance;
        _httpClient.BaseAddress = new Uri($"http://{settings.ServerAddress}:{settings.ServerHttpPort}/api/");
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
            _currentSession = null;
        }
    }

    #region Authentication

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("auth/register", request);
            var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
            
            if (result?.Success == true && result.Session != null)
            {
                SetAuthToken(result.Session.Token);
                _currentSession = result.Session;
            }
            
            return result ?? new AuthResponse { Success = false, Message = "Failed to parse response" };
        }
        catch (Exception ex)
        {
            return new AuthResponse { Success = false, Message = $"Connection error: {ex.Message}" };
        }
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("auth/login", request);
            var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
            
            if (result?.Success == true && result.Session != null)
            {
                SetAuthToken(result.Session.Token);
                _currentSession = result.Session;
                
                if (request.RememberMe)
                {
                    SettingsService.Instance.SaveSession(request.Username, result.Session.Token, true);
                }
            }
            
            return result ?? new AuthResponse { Success = false, Message = "Failed to parse response" };
        }
        catch (Exception ex)
        {
            return new AuthResponse { Success = false, Message = $"Connection error: {ex.Message}" };
        }
    }

    public async Task<AuthResponse> ValidateTokenAsync(string token)
    {
        var tempToken = _authToken;
        SetAuthToken(token);
        
        try
        {
            var response = await _httpClient.PostAsync("auth/validate", null);
            var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
            
            if (result?.Success == true && result.Session != null)
            {
                _currentSession = result.Session;
            }
            else
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
        _currentSession = null;
        SettingsService.Instance.ClearSession();
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

    public async Task<List<CryptoCurrency>> GetCachedPricesAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<CryptoCurrency>>("crypto/cached")
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

    public async Task<PortfolioSummary?> GetPortfolioSummaryAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<PortfolioSummary>("portfolio/summary");
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> AddHoldingAsync(CryptoHolding holding)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("portfolio/holdings", holding);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateHoldingAsync(int id, CryptoHolding holding)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"portfolio/holdings/{id}", holding);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteHoldingAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"portfolio/holdings/{id}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
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

    public async Task<bool> AddTransactionAsync(CryptoTransaction transaction)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("portfolio/transactions", transaction);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<decimal> GetBalanceAsync()
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<BalanceResponse>("portfolio/balance");
            return result?.Balance ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    public async Task<BuyResult> BuyCryptoAsync(string coinId, string symbol, decimal amount, decimal pricePerUnit, decimal fee = 0, string? notes = null)
    {
        try
        {
            var request = new { CoinId = coinId, Symbol = symbol, Amount = amount, PricePerUnit = pricePerUnit, Fee = fee, Notes = notes };
            var response = await _httpClient.PostAsJsonAsync("portfolio/buy", request);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<BuyResult>();
                return result ?? new BuyResult { Success = true };
            }
            
            var error = await response.Content.ReadFromJsonAsync<BuyResult>();
            return error ?? new BuyResult { Success = false, Message = "Purchase failed" };
        }
        catch (Exception ex)
        {
            return new BuyResult { Success = false, Message = ex.Message };
        }
    }

    public async Task<SellResult> SellCryptoAsync(string coinId, string symbol, decimal amount, decimal pricePerUnit, decimal fee = 0, string? notes = null)
    {
        try
        {
            var request = new { CoinId = coinId, Symbol = symbol, Amount = amount, PricePerUnit = pricePerUnit, Fee = fee, Notes = notes };
            var response = await _httpClient.PostAsJsonAsync("portfolio/sell", request);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<SellResult>();
                return result ?? new SellResult { Success = true };
            }
            
            var error = await response.Content.ReadFromJsonAsync<SellResult>();
            return error ?? new SellResult { Success = false, Message = "Sale failed" };
        }
        catch (Exception ex)
        {
            return new SellResult { Success = false, Message = ex.Message };
        }
    }

    #endregion

    #region Admin

    public async Task<List<UserInfo>> GetAllUsersAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<UserInfo>>("admin/users")
                ?? new List<UserInfo>();
        }
        catch
        {
            return new List<UserInfo>();
        }
    }

    public async Task<SystemStats?> GetSystemStatsAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<SystemStats>("admin/stats");
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> ChangeUserRoleAsync(int userId, string role)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"admin/users/{userId}/role", new { role });
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ChangeUserStatusAsync(int userId, bool isActive)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"admin/users/{userId}/status", new { isActive });
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> RefreshCryptoPricesAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync("admin/crypto/refresh", null);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region Export

    public async Task<byte[]?> ExportPricesToExcelAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("export/prices/excel");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<byte[]?> ExportPricesToXmlAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("export/prices/xml");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<byte[]?> ExportPricesToBinaryAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("export/prices/binary");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<byte[]?> ExportMarketToPdfAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("export/market/pdf");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<byte[]?> ExportHoldingsToExcelAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("export/holdings/excel");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<byte[]?> ExportHoldingsToXmlAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("export/holdings/xml");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<byte[]?> ExportHoldingsToBinaryAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("export/holdings/binary");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<byte[]?> ExportPortfolioToPdfAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("export/portfolio/pdf");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<byte[]?> ExportTransactionsToExcelAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("export/transactions/excel");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<byte[]?> ExportTransactionsToXmlAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("export/transactions/xml");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<byte[]?> ExportTransactionsToBinaryAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("export/transactions/binary");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<byte[]?> ExportTransactionsToPdfAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("export/transactions/pdf");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<byte[]?> ExportAdminReportAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("export/admin/report/excel");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    #endregion

    #region Profile Picture

    public async Task<bool> UploadProfilePictureAsync(byte[] imageData, string mimeType)
    {
        try
        {
            var base64Data = Convert.ToBase64String(imageData);
            var request = new { Data = base64Data, MimeType = mimeType };
            var response = await _httpClient.PostAsJsonAsync("user/profile-picture/base64", request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<byte[]?> GetProfilePictureAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("user/profile-picture");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> DeleteProfilePictureAsync()
    {
        try
        {
            var response = await _httpClient.DeleteAsync("user/profile-picture");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> HasProfilePictureAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("user/profile-picture/exists");
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ProfilePictureStatus>();
                return result?.HasPicture ?? false;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}

public class ProfilePictureStatus
{
    public bool Success { get; set; }
    public bool HasPicture { get; set; }
    public string? MimeType { get; set; }
    public int Size { get; set; }
}

// Additional models for API responses
public class PortfolioSummary
{
    public decimal TotalValue { get; set; }
    public decimal TotalCost { get; set; }
    public decimal TotalProfitLoss { get; set; }
    public decimal TotalProfitLossPercentage { get; set; }
    public int HoldingsCount { get; set; }
    public decimal Balance { get; set; }
    public List<HoldingDetail>? Holdings { get; set; }
}

public class HoldingDetail
{
    public string CoinId { get; set; } = "";
    public string Symbol { get; set; } = "";
    public decimal Amount { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal ProfitLoss { get; set; }
    public decimal ProfitLossPercentage { get; set; }
}

public class UserInfo
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public class SystemStats
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int AdminUsers { get; set; }
    public int TotalHoldings { get; set; }
    public int TotalTransactions { get; set; }
    public int TrackedCryptos { get; set; }
}

public class BalanceResponse
{
    public decimal Balance { get; set; }
}

public class BuyResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public decimal Balance { get; set; }
}

public class SellResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public decimal Balance { get; set; }
}

public class CryptoOption
{
    public string CoinId { get; set; } = "";
    public string Symbol { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal CurrentPrice { get; set; }
    
    public override string ToString() => $"{Name} ({Symbol.ToUpper()}) - ${CurrentPrice:N2}";
}
