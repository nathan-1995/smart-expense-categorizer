using System.Diagnostics;
using System.Text;

namespace ApiGateway.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString("N")[..8];
        
        // Add request ID to response headers
        context.Response.Headers["X-Request-ID"] = requestId;
        
        // Log request
        await LogRequestAsync(context, requestId);
        
        // Capture response
        var originalResponseBodyStream = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            
            // Log response
            await LogResponseAsync(context, requestId, stopwatch.ElapsedMilliseconds, responseBodyStream);
            
            // Copy response back to original stream
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            await responseBodyStream.CopyToAsync(originalResponseBodyStream);
        }
    }

    private async Task LogRequestAsync(HttpContext context, string requestId)
    {
        var request = context.Request;
        var logMessage = new StringBuilder();
        
        logMessage.AppendLine($"[{requestId}] HTTP Request Information:");
        logMessage.AppendLine($"Method: {request.Method}");
        logMessage.AppendLine($"Path: {request.Path}");
        logMessage.AppendLine($"QueryString: {request.QueryString}");
        logMessage.AppendLine($"Headers: {string.Join(", ", request.Headers.Select(h => $"{h.Key}={string.Join(";", h.Value)}"))}");
        
        // Log request body for POST/PUT requests (be careful with sensitive data)
        if (request.Method == "POST" || request.Method == "PUT")
        {
            request.EnableBuffering();
            var requestBody = await new StreamReader(request.Body).ReadToEndAsync();
            request.Body.Position = 0;
            
            if (!string.IsNullOrEmpty(requestBody) && requestBody.Length < 1000) // Limit body logging
            {
                logMessage.AppendLine($"Body: {requestBody}");
            }
        }
        
        _logger.LogInformation(logMessage.ToString());
    }

    private async Task LogResponseAsync(HttpContext context, string requestId, long elapsedMilliseconds, MemoryStream responseBodyStream)
    {
        var response = context.Response;
        responseBodyStream.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();
        
        var logMessage = new StringBuilder();
        logMessage.AppendLine($"[{requestId}] HTTP Response Information:");
        logMessage.AppendLine($"StatusCode: {response.StatusCode}");
        logMessage.AppendLine($"Headers: {string.Join(", ", response.Headers.Select(h => $"{h.Key}={string.Join(";", h.Value)}"))}");
        logMessage.AppendLine($"ElapsedTime: {elapsedMilliseconds}ms");
        
        if (!string.IsNullOrEmpty(responseBody) && responseBody.Length < 1000) // Limit body logging
        {
            logMessage.AppendLine($"Body: {responseBody}");
        }
        
        if (response.StatusCode >= 400)
        {
            _logger.LogWarning(logMessage.ToString());
        }
        else
        {
            _logger.LogInformation(logMessage.ToString());
        }
        
        responseBodyStream.Seek(0, SeekOrigin.Begin);
    }
}