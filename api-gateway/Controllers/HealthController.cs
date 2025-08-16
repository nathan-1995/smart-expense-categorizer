using ApiGateway.Models;
using ApiGateway.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    private readonly IHealthCheckService _healthCheckService;
    private readonly ILogger<HealthController> _logger;

    public HealthController(IHealthCheckService healthCheckService, ILogger<HealthController> logger)
    {
        _healthCheckService = healthCheckService;
        _logger = logger;
    }

    /// <summary>
    /// Get the health status of the API Gateway
    /// </summary>
    [HttpGet]
    public IActionResult GetGatewayHealth()
    {
        var response = ApiResponse<object>.SuccessResponse(new
        {
            service = "API Gateway",
            status = "Healthy",
            timestamp = DateTime.UtcNow,
            version = "1.0.0"
        });

        return Ok(response);
    }

    /// <summary>
    /// Get the aggregated health status of all services
    /// </summary>
    [HttpGet("services")]
    public async Task<IActionResult> GetServicesHealth()
    {
        try
        {
            _logger.LogInformation("Checking health of all services");
            var healthStatus = await _healthCheckService.CheckAllServicesAsync();
            
            var response = ApiResponse<GatewayHealthStatus>.SuccessResponse(healthStatus);
            
            // Return appropriate status code based on overall health
            return healthStatus.OverallStatus switch
            {
                "Healthy" => Ok(response),
                "Degraded" => StatusCode(207, response), // Multi-Status
                _ => StatusCode(503, response) // Service Unavailable
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking services health");
            var response = ApiResponse<object>.ErrorResponse("Error checking services health");
            return StatusCode(500, response);
        }
    }

    /// <summary>
    /// Get the health status of a specific service
    /// </summary>
    [HttpGet("services/{serviceName}")]
    public async Task<IActionResult> GetServiceHealth(string serviceName)
    {
        try
        {
            _logger.LogInformation("Checking health of service: {ServiceName}", serviceName);
            var healthResult = await _healthCheckService.CheckServiceAsync(serviceName);
            
            var response = ApiResponse<HealthCheckResult>.SuccessResponse(healthResult);
            
            return healthResult.Status switch
            {
                "Healthy" => Ok(response),
                "Unknown" => NotFound(ApiResponse<object>.ErrorResponse($"Service '{serviceName}' not found")),
                _ => StatusCode(503, response)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking health of service: {ServiceName}", serviceName);
            var response = ApiResponse<object>.ErrorResponse($"Error checking health of service '{serviceName}'");
            return StatusCode(500, response);
        }
    }
}