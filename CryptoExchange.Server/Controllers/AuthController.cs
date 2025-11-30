using Microsoft.AspNetCore.Mvc;
using CryptoTrader.Shared.Models;
using CryptoExchange.Server.Data;
using CryptoExchange.Server.Services;

namespace CryptoExchange.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        if (!result.Success)
        {
            return Unauthorized(result);
        }
        return Ok(result);
    }

    [HttpPost("validate")]
    public async Task<ActionResult<AuthResponse>> ValidateToken([FromHeader(Name = "Authorization")] string? token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return Unauthorized(new AuthResponse { Success = false, Message = "No token provided" });
        }

        // Remove "Bearer " prefix if present
        if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            token = token[7..];
        }

        var result = await _authService.ValidateTokenAsync(token);
        if (!result.Success)
        {
            return Unauthorized(result);
        }
        return Ok(result);
    }

    [HttpPost("logout")]
    public async Task<ActionResult> Logout([FromHeader(Name = "Authorization")] string? token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return BadRequest("No token provided");
        }

        if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            token = token[7..];
        }

        await _authService.LogoutAsync(token);
        return Ok(new { message = "Logged out successfully" });
    }
}
