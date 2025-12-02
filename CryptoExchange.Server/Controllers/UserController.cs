using Microsoft.AspNetCore.Mvc;
using CryptoExchange.Server.Data;
using CryptoExchange.Server.Services;

namespace CryptoExchange.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly DatabaseContext _db;
    private readonly AuthService _authService;

    // Allowed image types
    private static readonly HashSet<string> AllowedMimeTypes = new()
    {
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp"
    };

    // Max file size: 2MB
    private const int MaxFileSizeBytes = 2 * 1024 * 1024;

    public UserController(DatabaseContext db, AuthService authService)
    {
        _db = db;
        _authService = authService;
    }

    private async Task<(int? UserId, string? Error)> ValidateTokenAsync()
    {
        var token = Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrEmpty(token))
            return (null, "Authentication required");

        if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            token = token[7..];

        var result = await _authService.ValidateTokenAsync(token);
        if (!result.Success || result.Session == null)
            return (null, "Invalid or expired token");

        return (result.Session.UserId, null);
    }

    /// <summary>
    /// Upload a profile picture (BLOB storage)
    /// Accepts: image/jpeg, image/png, image/gif, image/webp
    /// Max size: 2MB
    /// </summary>
    [HttpPost("profile-picture")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadProfilePicture(IFormFile file)
    {
        var (userId, error) = await ValidateTokenAsync();
        if (userId == null)
            return Unauthorized(new { Success = false, Message = error });

        if (file == null || file.Length == 0)
            return BadRequest(new { Success = false, Message = "No file uploaded" });

        if (file.Length > MaxFileSizeBytes)
            return BadRequest(new { Success = false, Message = $"File too large. Maximum size is {MaxFileSizeBytes / 1024 / 1024}MB" });

        if (!AllowedMimeTypes.Contains(file.ContentType.ToLower()))
            return BadRequest(new { Success = false, Message = $"Invalid file type. Allowed: {string.Join(", ", AllowedMimeTypes)}" });

        try
        {
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            var imageData = memoryStream.ToArray();

            await _db.SaveProfilePictureAsync(userId.Value, imageData, file.ContentType);

            return Ok(new { 
                Success = true, 
                Message = "Profile picture uploaded successfully",
                Size = imageData.Length,
                MimeType = file.ContentType
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Success = false, Message = $"Failed to save image: {ex.Message}" });
        }
    }

    /// <summary>
    /// Get the current user's profile picture
    /// </summary>
    [HttpGet("profile-picture")]
    public async Task<IActionResult> GetProfilePicture()
    {
        var (userId, error) = await ValidateTokenAsync();
        if (userId == null)
            return Unauthorized(new { Success = false, Message = error });

        var (data, mimeType) = await _db.GetProfilePictureAsync(userId.Value);
        
        if (data == null || mimeType == null)
            return NotFound(new { Success = false, Message = "No profile picture found" });

        return File(data, mimeType);
    }

    /// <summary>
    /// Get any user's profile picture by ID (public endpoint for avatars)
    /// </summary>
    [HttpGet("profile-picture/{userId:int}")]
    public async Task<IActionResult> GetUserProfilePicture(int userId)
    {
        var (data, mimeType) = await _db.GetProfilePictureAsync(userId);
        
        if (data == null || mimeType == null)
            return NotFound(new { Success = false, Message = "No profile picture found" });

        return File(data, mimeType);
    }

    /// <summary>
    /// Delete the current user's profile picture
    /// </summary>
    [HttpDelete("profile-picture")]
    public async Task<IActionResult> DeleteProfilePicture()
    {
        var (userId, error) = await ValidateTokenAsync();
        if (userId == null)
            return Unauthorized(new { Success = false, Message = error });

        await _db.DeleteProfilePictureAsync(userId.Value);
        
        return Ok(new { Success = true, Message = "Profile picture deleted" });
    }

    /// <summary>
    /// Upload profile picture from base64 encoded data (alternative method)
    /// </summary>
    [HttpPost("profile-picture/base64")]
    public async Task<IActionResult> UploadProfilePictureBase64([FromBody] Base64ImageUpload upload)
    {
        var (userId, error) = await ValidateTokenAsync();
        if (userId == null)
            return Unauthorized(new { Success = false, Message = error });

        if (string.IsNullOrEmpty(upload.Data))
            return BadRequest(new { Success = false, Message = "No image data provided" });

        if (!AllowedMimeTypes.Contains(upload.MimeType?.ToLower() ?? ""))
            return BadRequest(new { Success = false, Message = $"Invalid file type. Allowed: {string.Join(", ", AllowedMimeTypes)}" });

        try
        {
            // Handle data URL format: "data:image/png;base64,..."
            var base64Data = upload.Data;
            if (base64Data.Contains(","))
            {
                base64Data = base64Data.Split(',')[1];
            }

            var imageData = Convert.FromBase64String(base64Data);

            if (imageData.Length > MaxFileSizeBytes)
                return BadRequest(new { Success = false, Message = $"File too large. Maximum size is {MaxFileSizeBytes / 1024 / 1024}MB" });

            await _db.SaveProfilePictureAsync(userId.Value, imageData, upload.MimeType!);

            return Ok(new { 
                Success = true, 
                Message = "Profile picture uploaded successfully",
                Size = imageData.Length,
                MimeType = upload.MimeType
            });
        }
        catch (FormatException)
        {
            return BadRequest(new { Success = false, Message = "Invalid base64 data" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Success = false, Message = $"Failed to save image: {ex.Message}" });
        }
    }

    /// <summary>
    /// Check if current user has a profile picture
    /// </summary>
    [HttpGet("profile-picture/exists")]
    public async Task<IActionResult> HasProfilePicture()
    {
        var (userId, error) = await ValidateTokenAsync();
        if (userId == null)
            return Unauthorized(new { Success = false, Message = error });

        var (data, mimeType) = await _db.GetProfilePictureAsync(userId.Value);
        
        return Ok(new { 
            Success = true, 
            HasPicture = data != null,
            MimeType = mimeType,
            Size = data?.Length ?? 0
        });
    }
}

/// <summary>
/// Model for base64 image upload
/// </summary>
public class Base64ImageUpload
{
    public string Data { get; set; } = "";
    public string? MimeType { get; set; }
}
