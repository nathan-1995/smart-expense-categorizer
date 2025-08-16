using System.Diagnostics;
using ApiGateway.Models;
using Microsoft.Extensions.Options;

namespace ApiGateway.Services;

public interface IHealthCheckService
{
    Task<GatewayHealthStatus> CheckAllServicesAsync();
    Task<HealthCheckResult> CheckServiceAsync(string serviceName);
}

public class HealthCheckService : IHealthCheckService
{
    private readonly HttpClient _httpClient;
    private readonly ServicesConfiguration _servicesConfig;
    private readonly ILogger<HealthCheckService> _logger;

    public HealthCheckService(HttpClient httpClient, IOptions<ServicesConfiguration> servicesConfig, ILogger<HealthCheckService> logger)
    {
        _httpClient = httpClient;
        _servicesConfig = servicesConfig.Value;
        _logger = logger;
    }

    public async Task<GatewayHealthStatus> CheckAllServicesAsync()
    {
        var healthStatus = new GatewayHealthStatus();
        var healthTasks = new List<Task<HealthCheckResult>>();

        // Add all configured services
        healthTasks.Add(CheckServiceHealthAsync("TransactionService", _servicesConfig.TransactionService));
        
        // Wait for all health checks to complete
        var results = await Task.WhenAll(healthTasks);
        healthStatus.Services.AddRange(results);
        
        // Determine overall status
        healthStatus.OverallStatus = results.All(r => r.Status == "Healthy") ? "Healthy" : 
                                   results.Any(r => r.Status == "Unhealthy") ? "Unhealthy" : "Degraded";
        
        return healthStatus;
    }

    public async Task<HealthCheckResult> CheckServiceAsync(string serviceName)
    {
        return serviceName.ToLower() switch
        {
            "transactionservice" or "transaction" => await CheckServiceHealthAsync("TransactionService", _servicesConfig.TransactionService),
            _ => new HealthCheckResult
            {
                Service = serviceName,
                Status = "Unknown",
                Message = "Service not configured"
            }
        };
    }

    private async Task<HealthCheckResult> CheckServiceHealthAsync(string serviceName, ServiceConfiguration config)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new HealthCheckResult
        {
            Service = serviceName
        };

        try
        {
            var healthUrl = $"{config.BaseUrl}/health";
            _logger.LogDebug("Checking health of {ServiceName} at {HealthUrl}", serviceName, healthUrl);
            
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(config.Timeout));
            var response = await _httpClient.GetAsync(healthUrl, cts.Token);
            
            stopwatch.Stop();
            result.ResponseTime = stopwatch.Elapsed;
            
            if (response.IsSuccessStatusCode)
            {
                result.Status = "Healthy";
                result.Message = $"Service responded with {response.StatusCode}";
            }
            else
            {
                result.Status = "Unhealthy";
                result.Message = $"Service responded with {response.StatusCode}";
            }
            
            _logger.LogDebug("Health check for {ServiceName} completed: {Status} in {ResponseTime}ms", 
                serviceName, result.Status, result.ResponseTime.TotalMilliseconds);
        }
        catch (TaskCanceledException)
        {
            stopwatch.Stop();
            result.ResponseTime = stopwatch.Elapsed;
            result.Status = "Unhealthy";
            result.Message = "Health check timed out";
            _logger.LogWarning("Health check for {ServiceName} timed out after {Timeout}s", serviceName, config.Timeout);
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            result.ResponseTime = stopwatch.Elapsed;
            result.Status = "Unhealthy";
            result.Message = $"Connection failed: {ex.Message}";
            _logger.LogWarning(ex, "Health check for {ServiceName} failed", serviceName);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.ResponseTime = stopwatch.Elapsed;
            result.Status = "Unhealthy";
            result.Message = $"Unexpected error: {ex.Message}";
            _logger.LogError(ex, "Unexpected error during health check for {ServiceName}", serviceName);
        }

        return result;
    }
}