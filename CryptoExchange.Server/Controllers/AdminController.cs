using Microsoft.AspNetCore.Mvc;
using CryptoTrader.Shared.Models;
using CryptoExchange.Server.Data;
using CryptoExchange.Server.Services;

namespace CryptoExchange.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly DatabaseContext _db;
    private readonly AuthService _authService;

    public AdminController(DatabaseContext db, AuthService authService)
    {
        _db = db;
        _authService = authService;
    }

    private async Task<(UserSession? session, bool isAdmin)> ValidateAdminAsync()
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
    /// [ADMIN ONLY] Get all users in the system
    /// </summary>
    [HttpGet("users")]
    public async Task<ActionResult> GetAllUsers()
    {
        var (session, isAdmin) = await ValidateAdminAsync();
        if (session == null)
            return Unauthorized(new { message = "Authentication required" });
        if (!isAdmin)
            return StatusCode(403, new { message = "Admin access required" });

        var users = await _db.GetAllUsersAsync();
        var result = users.Select(u => new
        {
            u.Id,
            u.Username,
            u.Email,
            Role = u.Role.ToString(),
            u.IsActive,
            u.CreatedAt,
            u.LastLoginAt
        });

        return Ok(result);
    }

    /// <summary>
    /// [ADMIN ONLY] Get specific user details
    /// </summary>
    [HttpGet("users/{userId}")]
    public async Task<ActionResult> GetUser(int userId)
    {
        var (session, isAdmin) = await ValidateAdminAsync();
        if (session == null)
            return Unauthorized(new { message = "Authentication required" });
        if (!isAdmin)
            return StatusCode(403, new { message = "Admin access required" });

        var user = await _db.GetUserByIdAsync(userId);
        if (user == null)
            return NotFound(new { message = "User not found" });

        var holdings = await _db.GetUserHoldingsAsync(userId);
        var transactions = await _db.GetUserTransactionsAsync(userId, 50);

        return Ok(new
        {
            User = new
            {
                user.Id,
                user.Username,
                user.Email,
                Role = user.Role.ToString(),
                user.IsActive,
                user.CreatedAt,
                user.LastLoginAt
            },
            HoldingsCount = holdings.Count(),
            TransactionsCount = transactions.Count(),
            Holdings = holdings,
            RecentTransactions = transactions
        });
    }

    /// <summary>
    /// [ADMIN ONLY] Update user role
    /// </summary>
    [HttpPut("users/{userId}/role")]
    public async Task<ActionResult> UpdateUserRole(int userId, [FromBody] UpdateRoleRequest request)
    {
        var (session, isAdmin) = await ValidateAdminAsync();
        if (session == null)
            return Unauthorized(new { message = "Authentication required" });
        if (!isAdmin)
            return StatusCode(403, new { message = "Admin access required" });

        // Prevent admin from demoting themselves
        if (userId == session.UserId && request.Role != UserRole.Admin)
            return BadRequest(new { message = "Cannot change your own admin role" });

        var user = await _db.GetUserByIdAsync(userId);
        if (user == null)
            return NotFound(new { message = "User not found" });

        await _db.UpdateUserRoleAsync(userId, request.Role);
        return Ok(new { message = $"User role updated to {request.Role}" });
    }

    /// <summary>
    /// [ADMIN ONLY] Activate or deactivate a user
    /// </summary>
    [HttpPut("users/{userId}/status")]
    public async Task<ActionResult> UpdateUserStatus(int userId, [FromBody] UpdateStatusRequest request)
    {
        var (session, isAdmin) = await ValidateAdminAsync();
        if (session == null)
            return Unauthorized(new { message = "Authentication required" });
        if (!isAdmin)
            return StatusCode(403, new { message = "Admin access required" });

        // Prevent admin from deactivating themselves
        if (userId == session.UserId && !request.IsActive)
            return BadRequest(new { message = "Cannot deactivate your own account" });

        var user = await _db.GetUserByIdAsync(userId);
        if (user == null)
            return NotFound(new { message = "User not found" });

        await _db.SetUserActiveStatusAsync(userId, request.IsActive);
        
        // If deactivating, also invalidate all sessions
        if (!request.IsActive)
        {
            await _db.DeleteUserSessionsAsync(userId);
        }

        return Ok(new { message = $"User {(request.IsActive ? "activated" : "deactivated")}" });
    }

    /// <summary>
    /// [ADMIN ONLY] Get system statistics
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult> GetSystemStats()
    {
        var (session, isAdmin) = await ValidateAdminAsync();
        if (session == null)
            return Unauthorized(new { message = "Authentication required" });
        if (!isAdmin)
            return StatusCode(403, new { message = "Admin access required" });

        var users = (await _db.GetAllUsersAsync()).ToList();
        var prices = (await _db.GetAllPricesAsync()).ToList();

        return Ok(new
        {
            Users = new
            {
                Total = users.Count,
                Admins = users.Count(u => u.Role == UserRole.Admin),
                Active = users.Count(u => u.IsActive),
                Inactive = users.Count(u => !u.IsActive)
            },
            Crypto = new
            {
                TrackedCoins = prices.Count,
                LastUpdate = prices.FirstOrDefault()?.LastUpdated
            },
            Server = new
            {
                Uptime = DateTime.UtcNow.ToString("O"),
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
            }
        });
    }

    /// <summary>
    /// [ADMIN ONLY] Force refresh crypto prices
    /// </summary>
    [HttpPost("crypto/refresh")]
    public async Task<ActionResult> ForceRefreshPrices([FromServices] CryptoApiService cryptoApi)
    {
        var (session, isAdmin) = await ValidateAdminAsync();
        if (session == null)
            return Unauthorized(new { message = "Authentication required" });
        if (!isAdmin)
            return StatusCode(403, new { message = "Admin access required" });

        var prices = await cryptoApi.GetTopCryptosAsync(100);
        return Ok(new { message = $"Refreshed {prices.Count} crypto prices" });
    }
}

public class UpdateRoleRequest
{
    public UserRole Role { get; set; }
}

public class UpdateStatusRequest
{
    public bool IsActive { get; set; }
}
