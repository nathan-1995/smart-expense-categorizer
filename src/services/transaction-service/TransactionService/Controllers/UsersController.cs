using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransactionService.Data;
using TransactionService.Models;

namespace TransactionService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<UsersController> _logger;

    public UsersController(AppDbContext context, ILogger<UsersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Create a new user
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        try
        {
            // Check if user already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());
            
            if (existingUser != null)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("User with this email already exists"));
            }

            var user = new User
            {
                Email = request.Email,
                PasswordHash = request.PasswordHash,
                PasswordSalt = request.PasswordSalt,
                OAuthId = request.OAuthId,
                OAuthProvider = request.OAuthProvider,
                FirstName = request.FirstName,
                LastName = request.LastName,
                IsEmailVerified = request.IsEmailVerified
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var userInfo = new UserInfo
            {
                Id = user.Id.ToString(),
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                IsEmailVerified = user.IsEmailVerified,
                Role = user.Role.ToString()
            };

            return Ok(ApiResponse<UserInfo>.SuccessResponse(userInfo, "User created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user with email {Email}", request.Email);
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Error creating user"));
        }
    }

    /// <summary>
    /// Get user by email
    /// </summary>
    [HttpGet("by-email/{email}")]
    public async Task<IActionResult> GetUserByEmail(string email)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

            if (user == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("User not found"));
            }

            var userInfo = new UserInfo
            {
                Id = user.Id.ToString(),
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                IsEmailVerified = user.IsEmailVerified,
                Role = user.Role.ToString()
            };

            return Ok(ApiResponse<UserInfo>.SuccessResponse(userInfo, "User found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by email {Email}", email);
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Error retrieving user"));
        }
    }

    /// <summary>
    /// Get user by OAuth ID
    /// </summary>
    [HttpGet("by-oauth/{provider}/{oauthId}")]
    public async Task<IActionResult> GetUserByOAuth(string provider, string oauthId)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.OAuthId == oauthId && u.OAuthProvider == provider);

            if (user == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("User not found"));
            }

            var userInfo = new UserInfo
            {
                Id = user.Id.ToString(),
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                IsEmailVerified = user.IsEmailVerified,
                Role = user.Role.ToString()
            };

            return Ok(ApiResponse<UserInfo>.SuccessResponse(userInfo, "User found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by OAuth {Provider}/{OAuthId}", provider, oauthId);
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Error retrieving user"));
        }
    }

    /// <summary>
    /// Get user credentials for password validation
    /// </summary>
    [HttpGet("{userId}/credentials")]
    public async Task<IActionResult> GetUserCredentials(Guid userId)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("User not found"));
            }

            var credentials = new UserCredentials
            {
                PasswordHash = user.PasswordHash,
                PasswordSalt = user.PasswordSalt
            };

            return Ok(ApiResponse<UserCredentials>.SuccessResponse(credentials, "Credentials retrieved"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting credentials for user {UserId}", userId);
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Error retrieving credentials"));
        }
    }

    /// <summary>
    /// Update user's last seen timestamp
    /// </summary>
    [HttpPut("{userId}/last-seen")]
    public async Task<IActionResult> UpdateLastSeen(Guid userId)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("User not found"));
            }

            user.LastSeenAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResponse(new { message = "Last seen updated successfully" }, "Last seen updated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating last seen for user {UserId}", userId);
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Error updating last seen"));
        }
    }
}

// DTOs
public class CreateUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public string? PasswordSalt { get; set; }
    public string? OAuthId { get; set; }
    public string? OAuthProvider { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool IsEmailVerified { get; set; } = false;
}

public class UserInfo
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool IsEmailVerified { get; set; }
    public string Role { get; set; } = "User";
}

public class UserCredentials
{
    public string? PasswordHash { get; set; }
    public string? PasswordSalt { get; set; }
}