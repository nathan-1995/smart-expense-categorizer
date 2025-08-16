using System.Text;
using ApiGateway.Models;
using Microsoft.Extensions.Options;

namespace ApiGateway.Services;

public interface IProxyService
{
    Task<HttpResponseMessage> ForwardRequestAsync(HttpContext context, string targetService, string? targetPath = null);
}

public class ProxyService : IProxyService
{
    private readonly HttpClient _httpClient;
    private readonly ServicesConfiguration _servicesConfig;
    private readonly ILogger<ProxyService> _logger;

    public ProxyService(HttpClient httpClient, IOptions<ServicesConfiguration> servicesConfig, ILogger<ProxyService> logger)
    {
        _httpClient = httpClient;
        _servicesConfig = servicesConfig.Value;
        _logger = logger;
    }

    public async Task<HttpResponseMessage> ForwardRequestAsync(HttpContext context, string targetService, string? targetPath = null)
    {
        var serviceConfig = GetServiceConfiguration(targetService);
        var targetUrl = BuildTargetUrl(context, serviceConfig, targetPath);
        
        _logger.LogInformation("Forwarding request to: {TargetUrl}", targetUrl);

        var requestMessage = await CreateForwardedRequestAsync(context, targetUrl);
        
        try
        {
            var response = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
            _logger.LogInformation("Received response from {TargetService}: {StatusCode}", targetService, response.StatusCode);
            
            return response;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error forwarding request to {TargetService}", targetService);
            throw new HttpRequestException($"Failed to communicate with {targetService}: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout forwarding request to {TargetService}", targetService);
            throw new TaskCanceledException($"Request to {targetService} timed out", ex);
        }
    }

    private ServiceConfiguration GetServiceConfiguration(string targetService)
    {
        return targetService.ToLower() switch
        {
            "transaction" or "transactionservice" => _servicesConfig.TransactionService,
            _ => throw new ArgumentException($"Unknown target service: {targetService}")
        };
    }

    private static string BuildTargetUrl(HttpContext context, ServiceConfiguration serviceConfig, string? targetPath = null)
    {
        var path = targetPath ?? context.Request.Path.Value ?? "";
        var queryString = context.Request.QueryString.Value ?? "";
        
        // Remove /api/v1 prefix if present (gateway routes /api/v1/* to /api/*)
        if (path.StartsWith("/api/v1"))
        {
            path = path.Replace("/api/v1", "/api");
        }
        
        return $"{serviceConfig.BaseUrl}{path}{queryString}";
    }

    private static async Task<HttpRequestMessage> CreateForwardedRequestAsync(HttpContext context, string targetUrl)
    {
        var requestMessage = new HttpRequestMessage();
        var requestMethod = context.Request.Method;
        
        if (!HttpMethods.IsGet(requestMethod) && 
            !HttpMethods.IsHead(requestMethod) && 
            !HttpMethods.IsDelete(requestMethod) && 
            !HttpMethods.IsTrace(requestMethod))
        {
            var streamContent = new StreamContent(context.Request.Body);
            requestMessage.Content = streamContent;
        }

        // Copy headers (excluding some that shouldn't be forwarded)
        var excludedHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "host", "connection", "upgrade-insecure-requests", "accept-encoding"
        };

        foreach (var header in context.Request.Headers)
        {
            if (!excludedHeaders.Contains(header.Key))
            {
                if (requestMessage.Content != null && IsContentHeader(header.Key))
                {
                    requestMessage.Content.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
                else
                {
                    requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
            }
        }

        requestMessage.RequestUri = new Uri(targetUrl);
        requestMessage.Method = new HttpMethod(requestMethod);
        
        return requestMessage;
    }

    private static bool IsContentHeader(string headerName)
    {
        return headerName.ToLower() switch
        {
            "content-type" or "content-length" or "content-encoding" or "content-disposition" or "content-language" or "content-location" or "content-md5" or "content-range" => true,
            _ => false
        };
    }
}