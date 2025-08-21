using Microsoft.AspNetCore.Mvc;
using ApiGateway.Services;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AdminController : ControllerBase
{
    private readonly IProxyService _proxyService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(IProxyService proxyService, ILogger<AdminController> logger)
    {
        _proxyService = proxyService;
        _logger = logger;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var response = await _proxyService.ForwardRequestAsync(HttpContext, "transaction", "/api/admin/users");
        var content = await response.Content.ReadAsStringAsync();
        return new ContentResult
        {
            Content = content,
            ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json",
            StatusCode = (int)response.StatusCode
        };
    }

    [HttpGet("users/{userId}")]
    public async Task<IActionResult> GetUserDetails(Guid userId)
    {
        var response = await _proxyService.ForwardRequestAsync(HttpContext, "transaction", $"/api/admin/users/{userId}");
        var content = await response.Content.ReadAsStringAsync();
        return new ContentResult
        {
            Content = content,
            ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json",
            StatusCode = (int)response.StatusCode
        };
    }

    [HttpDelete("users/{userId}")]
    public async Task<IActionResult> DeleteUser(Guid userId)
    {
        var response = await _proxyService.ForwardRequestAsync(HttpContext, "transaction", $"/api/admin/users/{userId}");
        var content = await response.Content.ReadAsStringAsync();
        return new ContentResult
        {
            Content = content,
            ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json",
            StatusCode = (int)response.StatusCode
        };
    }

    [HttpPut("users/{userId}/role")]
    public async Task<IActionResult> UpdateUserRole(Guid userId)
    {
        var response = await _proxyService.ForwardRequestAsync(HttpContext, "transaction", $"/api/admin/users/{userId}/role");
        var content = await response.Content.ReadAsStringAsync();
        return new ContentResult
        {
            Content = content,
            ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json",
            StatusCode = (int)response.StatusCode
        };
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetSystemStats()
    {
        var response = await _proxyService.ForwardRequestAsync(HttpContext, "transaction", "/api/admin/stats");
        var content = await response.Content.ReadAsStringAsync();
        return new ContentResult
        {
            Content = content,
            ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json",
            StatusCode = (int)response.StatusCode
        };
    }
}