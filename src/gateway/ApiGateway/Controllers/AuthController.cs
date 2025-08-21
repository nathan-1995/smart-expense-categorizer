using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ApiGateway.Models;
using ApiGateway.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly AuthenticationConfiguration _authConfig;
    private readonly ILogger<AuthController> _logger;
    private readonly IUserService _userService;
    private readonly IPasswordService _passwordService;
    private readonly IProxyService _proxyService;

    public AuthController(
        IOptions<AuthenticationConfiguration> authConfig, 
        ILogger<AuthController> logger,
        IUserService userService,
        IPasswordService passwordService,
        IProxyService proxyService)
    {
        _authConfig = authConfig.Value;
        _logger = logger;
        _userService = userService;
        _passwordService = passwordService;
        _proxyService = proxyService;
    }

    /// <summary>
    /// Initiate Google OAuth authentication
    /// </summary>
    [HttpGet("google")]
    public IActionResult GoogleAuth(string returnUrl = "/")
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(GoogleCallback)),
            Items = { { "returnUrl", returnUrl } }
        };

        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Handle Google OAuth callback
    /// </summary>
    [HttpGet("google-callback")]
    public async Task<IActionResult> GoogleCallback()
    {
        try
        {
            var authenticateResult = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
            
            if (!authenticateResult.Succeeded)
            {
                _logger.LogWarning("Google authentication failed");
                return BadRequest(ApiResponse<object>.ErrorResponse("Google authentication failed"));
            }

            var claims = authenticateResult.Principal?.Claims.ToList() ?? new List<Claim>();
            var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var name = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            var googleId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(googleId))
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("Required user information not provided by Google"));
            }

            // Create JWT token
            var token = GenerateJwtToken(googleId, email, name);
            
            var response = ApiResponse<object>.SuccessResponse(new
            {
                token,
                user = new
                {
                    id = googleId,
                    email,
                    name
                }
            }, "Authentication successful");

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Google OAuth callback");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Authentication error"));
        }
    }

    /// <summary>
    /// Generate JWT token for development/testing purposes
    /// This endpoint should be removed or secured in production
    /// </summary>
    [HttpPost("token")]
    public IActionResult GenerateToken([FromBody] TokenRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.UserId) || string.IsNullOrEmpty(request.Email))
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("UserId and Email are required"));
            }

            var token = GenerateJwtToken(request.UserId, request.Email, request.Name);
            
            var response = ApiResponse<object>.SuccessResponse(new
            {
                token,
                expiresAt = DateTime.UtcNow.AddMinutes(_authConfig.Jwt.ExpireMinutes)
            }, "Token generated successfully");

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating token");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Token generation error"));
        }
    }

    /// <summary>
    /// Validate current JWT token
    /// </summary>
    [HttpGet("validate")]
    [Authorize]
    public IActionResult ValidateToken()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        var name = User.FindFirst(ClaimTypes.Name)?.Value;

        var response = ApiResponse<object>.SuccessResponse(new
        {
            valid = true,
            user = new
            {
                id = userId,
                email,
                name
            }
        }, "Token is valid");

        return Ok(response);
    }

    /// <summary>
    /// Refresh JWT token
    /// </summary>
    [HttpPost("refresh")]
    [Authorize]
    public IActionResult RefreshToken()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var name = User.FindFirst(ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(email))
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("Invalid token claims"));
            }

            var newToken = GenerateJwtToken(userId, email, name);
            
            var response = ApiResponse<object>.SuccessResponse(new
            {
                token = newToken,
                expiresAt = DateTime.UtcNow.AddMinutes(_authConfig.Jwt.ExpireMinutes)
            }, "Token refreshed successfully");

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Token refresh error"));
        }
    }

    /// <summary>
    /// Register a new user with email and password
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            // Validate password strength
            if (!_passwordService.IsValidPassword(request.Password))
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Password must be at least 8 characters long and contain uppercase, lowercase, digit, and special character"));
            }

            // Check if user already exists
            var existingUser = await _userService.GetUserByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("User with this email already exists"));
            }

            // Hash password
            var (hashedPassword, salt) = _passwordService.HashPassword(request.Password);

            // Create user
            var createUserRequest = new CreateUserRequest
            {
                Email = request.Email,
                PasswordHash = hashedPassword,
                PasswordSalt = salt,
                FirstName = request.FirstName,
                LastName = request.LastName,
                IsEmailVerified = false
            };

            var user = await _userService.CreateUserAsync(createUserRequest);
            if (user == null)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Failed to create user"));
            }

            // Generate JWT token
            var token = GenerateJwtToken(user.Id, user.Email, $"{user.FirstName} {user.LastName}".Trim(), user.Role);
            
            // Send verification email
            try
            {
                _logger.LogInformation("Attempting to send verification email for new user {UserId}", user.Id);
                
                // Create a new HTTP context for the internal call
                var verificationRequest = new SendVerificationRequest { UserId = Guid.Parse(user.Id) };
                var json = System.Text.Json.JsonSerializer.Serialize(verificationRequest);
                
                using var httpClient = new HttpClient();
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var verificationResponse = await httpClient.PostAsync("http://localhost:5001/api/email-verification/send", content);
                
                if (!verificationResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to send verification email for user {UserId}. Status: {StatusCode}", 
                        user.Id, verificationResponse.StatusCode);
                }
                else
                {
                    _logger.LogInformation("Verification email sent successfully for user {UserId}", user.Id);
                }
            }
            catch (Exception emailEx)
            {
                _logger.LogError(emailEx, "Error sending verification email for user {UserId}", user.Id);
            }
            
            var response = ApiResponse<AuthResponse>.SuccessResponse(new AuthResponse
            {
                Token = token,
                User = user,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_authConfig.Jwt.ExpireMinutes)
            }, "Registration successful");

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for email {Email}", request.Email);
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Registration error"));
        }
    }

    /// <summary>
    /// Login with email and password
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            // Validate credentials
            _logger.LogInformation("Validating credentials for email {Email}", request.Email);
            var user = await _userService.ValidateUserCredentialsAsync(request.Email, request.Password);
            if (user == null)
            {
                _logger.LogWarning("Invalid credentials for email {Email}", request.Email);
                return BadRequest(ApiResponse<object>.ErrorResponse("Invalid email or password"));
            }

            _logger.LogInformation("User {UserId} authenticated successfully, updating last seen", user.Id);
            // Update last seen timestamp
            var lastSeenUpdated = await _userService.UpdateLastSeenAsync(user.Id);
            _logger.LogInformation("Last seen update for user {UserId}: {Success}", user.Id, lastSeenUpdated);

            // Generate JWT token
            var token = GenerateJwtToken(user.Id, user.Email, $"{user.FirstName} {user.LastName}".Trim(), user.Role);
            
            var response = ApiResponse<AuthResponse>.SuccessResponse(new AuthResponse
            {
                Token = token,
                User = user,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_authConfig.Jwt.ExpireMinutes)
            }, "Login successful");

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for email {Email}", request.Email);
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Login error"));
        }
    }

    private string GenerateJwtToken(string userId, string email, string? name = null, string role = "User")
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_authConfig.Jwt.SecretKey);
        
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Email, email),
            new("role", role), // Use custom role claim for easier access
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        if (!string.IsNullOrEmpty(name))
        {
            claims.Add(new Claim(ClaimTypes.Name, name));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_authConfig.Jwt.ExpireMinutes),
            Issuer = _authConfig.Jwt.Issuer,
            Audience = _authConfig.Jwt.Audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Send email verification
    /// </summary>
    [HttpPost("send-verification")]
    public async Task<IActionResult> SendVerificationEmail([FromBody] SendVerificationRequest request)
    {
        try
        {
            _logger.LogInformation("Sending verification email for user {UserId}", request.UserId);
            
            var response = await _proxyService.ForwardRequestAsync(HttpContext, "transaction", "/api/email-verification/send");
            var content = await response.Content.ReadAsStringAsync();
            
            return new ContentResult
            {
                Content = content,
                ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json",
                StatusCode = (int)response.StatusCode
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending verification email");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Failed to send verification email"));
        }
    }

    /// <summary>
    /// Verify email token
    /// </summary>
    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        try
        {
            _logger.LogInformation("Verifying email token");
            
            var response = await _proxyService.ForwardRequestAsync(HttpContext, "transaction", "/api/email-verification/verify");
            var content = await response.Content.ReadAsStringAsync();
            
            return new ContentResult
            {
                Content = content,
                ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json",
                StatusCode = (int)response.StatusCode
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying email");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Failed to verify email"));
        }
    }

    /// <summary>
    /// Get verification status
    /// </summary>
    [HttpGet("verification-status/{userId:guid}")]
    public async Task<IActionResult> GetVerificationStatus(Guid userId)
    {
        try
        {
            _logger.LogInformation("Getting verification status for user {UserId}", userId);
            
            var response = await _proxyService.ForwardRequestAsync(HttpContext, "transaction", $"/api/email-verification/status/{userId}");
            var content = await response.Content.ReadAsStringAsync();
            
            return new ContentResult
            {
                Content = content,
                ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json",
                StatusCode = (int)response.StatusCode
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting verification status");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Failed to get verification status"));
        }
    }

    /// <summary>
    /// Resend verification email
    /// </summary>
    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerificationEmail([FromBody] ResendVerificationRequest request)
    {
        try
        {
            _logger.LogInformation("Resending verification email for {Email}", request.Email);
            
            var response = await _proxyService.ForwardRequestAsync(HttpContext, "transaction", "/api/email-verification/resend");
            var content = await response.Content.ReadAsStringAsync();
            
            return new ContentResult
            {
                Content = content,
                ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json",
                StatusCode = (int)response.StatusCode
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resending verification email");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Failed to resend verification email"));
        }
    }
}

public class TokenRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Name { get; set; }
}

public class SendVerificationRequest
{
    public Guid UserId { get; set; }
}

public class VerifyEmailRequest
{
    public string Token { get; set; } = string.Empty;
}

public class ResendVerificationRequest
{
    public string Email { get; set; } = string.Empty;
}