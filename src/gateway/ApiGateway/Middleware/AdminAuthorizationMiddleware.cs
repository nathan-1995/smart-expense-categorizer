using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace ApiGateway.Middleware;

public class AdminAuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AdminAuthorizationMiddleware> _logger;
    private readonly string _jwtSecret;

    public AdminAuthorizationMiddleware(RequestDelegate next, ILogger<AdminAuthorizationMiddleware> logger, IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _jwtSecret = configuration["Authentication:Jwt:SecretKey"] ?? "YourDefaultSecretKey";
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only apply to admin routes
        if (!context.Request.Path.StartsWithSegments("/api/admin"))
        {
            await _next(context);
            return;
        }

        try
        {
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (authHeader == null || !authHeader.StartsWith("Bearer "))
            {
                await WriteUnauthorizedResponse(context, "Missing or invalid authorization header");
                return;
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();
            var principal = ValidateJwtToken(token);
            
            if (principal == null)
            {
                await WriteUnauthorizedResponse(context, "Invalid token");
                return;
            }

            // Check if user has admin role
            var roleClaim = principal.FindFirst("role")?.Value ?? 
                           principal.FindFirst(ClaimTypes.Role)?.Value ?? 
                           principal.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value;
            
            if (roleClaim != "Admin")
            {
                await WriteForbiddenResponse(context, "Admin access required");
                return;
            }

            // Add user info to context
            context.Items["UserId"] = principal.FindFirst("nameid")?.Value;
            context.Items["UserRole"] = roleClaim;

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in admin authorization middleware");
            await WriteUnauthorizedResponse(context, "Authorization failed");
        }
    }

    private ClaimsPrincipal? ValidateJwtToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSecret);
            
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = "SmartExpenseCategorizerGateway",
                ValidateAudience = true,
                ValidAudience = "SmartExpenseCategorizerApi",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken validatedToken);
            return principal;
        }
        catch
        {
            return null;
        }
    }

    private async Task WriteUnauthorizedResponse(HttpContext context, string message)
    {
        context.Response.StatusCode = 401;
        context.Response.ContentType = "application/json";
        
        var response = new
        {
            success = false,
            message,
            timestamp = DateTime.UtcNow
        };
        
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
    }

    private async Task WriteForbiddenResponse(HttpContext context, string message)
    {
        context.Response.StatusCode = 403;
        context.Response.ContentType = "application/json";
        
        var response = new
        {
            success = false,
            message,
            timestamp = DateTime.UtcNow
        };
        
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
    }
}