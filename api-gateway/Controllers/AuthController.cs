using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ApiGateway.Models;
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

    public AuthController(IOptions<AuthenticationConfiguration> authConfig, ILogger<AuthController> logger)
    {
        _authConfig = authConfig.Value;
        _logger = logger;
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

    private string GenerateJwtToken(string userId, string email, string? name = null)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_authConfig.Jwt.SecretKey);
        
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Email, email),
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
}

public class TokenRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Name { get; set; }
}