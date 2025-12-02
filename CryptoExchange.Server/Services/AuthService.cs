using System.Security.Cryptography;
using BCrypt.Net;
using CryptoTrader.Shared.Models;
using CryptoExchange.Server.Data;

namespace CryptoExchange.Server.Services;

/// <summary>
/// Service for handling user authentication
/// </summary>
public class AuthService
{
    private readonly DatabaseContext _db;
    private readonly int _tokenExpirationHours;

    public AuthService(DatabaseContext db, int tokenExpirationHours = 24)
    {
        _db = db;
        _tokenExpirationHours = tokenExpirationHours;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        // Validate request
        if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Length < 3)
        {
            return new AuthResponse { Success = false, Message = "Username must be at least 3 characters" };
        }

        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
        {
            return new AuthResponse { Success = false, Message = "Invalid email address" };
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
        {
            return new AuthResponse { Success = false, Message = "Password must be at least 6 characters" };
        }

        if (request.Password != request.ConfirmPassword)
        {
            return new AuthResponse { Success = false, Message = "Passwords do not match" };
        }

        // Check if user already exists
        var existingUser = await _db.GetUserByUsernameAsync(request.Username);
        if (existingUser != null)
        {
            return new AuthResponse { Success = false, Message = "Username already exists" };
        }

        var existingEmail = await _db.GetUserByEmailAsync(request.Email);
        if (existingEmail != null)
        {
            return new AuthResponse { Success = false, Message = "Email already registered" };
        }

        // Create user
        var salt = GenerateSalt();
        var passwordHash = HashPassword(request.Password, salt);

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = passwordHash,
            Salt = salt,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            Role = UserRole.User
        };

        user.Id = await _db.CreateUserAsync(user);

        // Create session
        var token = await _db.CreateSessionAsync(user.Id, _tokenExpirationHours);

        return new AuthResponse
        {
            Success = true,
            Message = "Registration successful",
            Session = new UserSession
            {
                UserId = user.Id,
                Username = user.Username,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(_tokenExpirationHours),
                IsAdmin = false,
                Balance = user.Balance
            }
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return new AuthResponse { Success = false, Message = "Username and password are required" };
        }

        var user = await _db.GetUserByUsernameAsync(request.Username);
        if (user == null)
        {
            return new AuthResponse { Success = false, Message = "Invalid username or password" };
        }

        if (!user.IsActive)
        {
            return new AuthResponse { Success = false, Message = "Account is deactivated" };
        }

        // Use BCrypt.Verify to check password (password + salt against the stored hash)
        if (!VerifyPassword(request.Password, user.Salt, user.PasswordHash))
        {
            return new AuthResponse { Success = false, Message = "Invalid username or password" };
        }

        // Update last login
        await _db.UpdateUserLastLoginAsync(user.Id);

        // Create session
        var expirationHours = request.RememberMe ? _tokenExpirationHours * 7 : _tokenExpirationHours;
        var token = await _db.CreateSessionAsync(user.Id, expirationHours);

        return new AuthResponse
        {
            Success = true,
            Message = "Login successful",
            Session = new UserSession
            {
                UserId = user.Id,
                Username = user.Username,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(expirationHours),
                IsAdmin = user.Role == UserRole.Admin,
                Balance = user.Balance
            }
        };
    }

    public async Task<AuthResponse> ValidateTokenAsync(string token)
    {
        var session = await _db.ValidateSessionAsync(token);
        if (session == null)
        {
            return new AuthResponse { Success = false, Message = "Invalid or expired token" };
        }

        return new AuthResponse
        {
            Success = true,
            Message = "Token is valid",
            Session = session
        };
    }

    public async Task LogoutAsync(string token)
    {
        await _db.DeleteSessionAsync(token);
    }

    public async Task LogoutAllAsync(int userId)
    {
        await _db.DeleteUserSessionsAsync(userId);
    }

    private static string GenerateSalt()
    {
        var saltBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(saltBytes);
        return Convert.ToBase64String(saltBytes);
    }

    private static string HashPassword(string password, string salt)
    {
        return BCrypt.Net.BCrypt.HashPassword(password + salt, BCrypt.Net.BCrypt.GenerateSalt(12));
    }

    private static bool VerifyPassword(string password, string salt, string storedHash)
    {
        return BCrypt.Net.BCrypt.Verify(password + salt, storedHash);
    }
}
