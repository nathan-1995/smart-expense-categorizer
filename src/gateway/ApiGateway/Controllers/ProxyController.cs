using ApiGateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/v1")]
public class ProxyController : ControllerBase
{
    private readonly IProxyService _proxyService;
    private readonly ILogger<ProxyController> _logger;

    public ProxyController(IProxyService proxyService, ILogger<ProxyController> logger)
    {
        _proxyService = proxyService;
        _logger = logger;
    }

    /// <summary>
    /// Proxy requests to Transaction Service
    /// Routes /api/v1/transactions/* to Transaction Service /api/*
    /// </summary>
    [HttpGet("transactions/{**path}")]
    [HttpPost("transactions/{**path}")]
    [HttpPut("transactions/{**path}")]
    [HttpDelete("transactions/{**path}")]
    [HttpPatch("transactions/{**path}")]
    public async Task<IActionResult> ProxyToTransactionService(string? path = null)
    {
        try
        {
            _logger.LogInformation("Proxying transaction request: {Method} {Path}", Request.Method, path);
            
            var response = await _proxyService.ForwardRequestAsync(HttpContext, "transaction");
            
            // Stream the response content
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
            _logger.LogError(ex, "Error proxying request to Transaction Service");
            return StatusCode(500, new { error = "Gateway error", message = ex.Message });
        }
    }

    /// <summary>
    /// Proxy test endpoints to Transaction Service (for development)
    /// Routes /api/v1/test/* to Transaction Service /api/test/*
    /// </summary>
    [HttpGet("test/{**path}")]
    [HttpPost("test/{**path}")]
    public async Task<IActionResult> ProxyTestEndpoints(string? path = null)
    {
        try
        {
            _logger.LogInformation("Proxying test request: {Method} {Path}", Request.Method, path);
            
            var response = await _proxyService.ForwardRequestAsync(HttpContext, "transaction");
            
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
            _logger.LogError(ex, "Error proxying test request to Transaction Service");
            return StatusCode(500, new { error = "Gateway error", message = ex.Message });
        }
    }

    /// <summary>
    /// Generic fallback proxy for any unmatched routes
    /// This should be used cautiously and typically disabled in production
    /// </summary>
    [HttpGet("{**path}")]
    [HttpPost("{**path}")]
    [HttpPut("{**path}")]
    [HttpDelete("{**path}")]
    [HttpPatch("{**path}")]
    public async Task<IActionResult> ProxyGeneric(string path)
    {
        try
        {
            _logger.LogInformation("Generic proxy request: {Method} {Path}", Request.Method, path);
            
            // Determine target service based on path
            var targetService = DetermineTargetService(path);
            
            if (string.IsNullOrEmpty(targetService))
            {
                return NotFound(new { error = "Route not found", path });
            }
            
            var response = await _proxyService.ForwardRequestAsync(HttpContext, targetService);
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
            _logger.LogError(ex, "Error in generic proxy for path: {Path}", path);
            return StatusCode(500, new { error = "Gateway error", message = ex.Message });
        }
    }

    private static string? DetermineTargetService(string path)
    {
        // Route based on path prefix
        return path.ToLower() switch
        {
            var p when p.StartsWith("transactions") => "transaction",
            var p when p.StartsWith("test") => "transaction",
            var p when p.StartsWith("api/test") => "transaction",
            var p when p.StartsWith("api/transactions") => "transaction",
            // Add more routing rules here for other services
            _ => null
        };
    }
}