using System.Net;
using System.Text.Json;
using ApiGateway.Models;

namespace ApiGateway.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        var apiResponse = exception switch
        {
            HttpRequestException httpEx => new ApiResponse<object>
            {
                Success = false,
                Message = "External service error",
                Errors = new List<string> { httpEx.Message }
            },
            TaskCanceledException => new ApiResponse<object>
            {
                Success = false,
                Message = "Request timeout",
                Errors = new List<string> { "The request took too long to process" }
            },
            UnauthorizedAccessException => new ApiResponse<object>
            {
                Success = false,
                Message = "Unauthorized",
                Errors = new List<string> { "You are not authorized to access this resource" }
            },
            ArgumentException argEx => new ApiResponse<object>
            {
                Success = false,
                Message = "Invalid request",
                Errors = new List<string> { argEx.Message }
            },
            _ => new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while processing your request",
                Errors = new List<string> { "Internal server error" }
            }
        };

        response.StatusCode = exception switch
        {
            HttpRequestException => (int)HttpStatusCode.BadGateway,
            TaskCanceledException => (int)HttpStatusCode.RequestTimeout,
            UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
            ArgumentException => (int)HttpStatusCode.BadRequest,
            _ => (int)HttpStatusCode.InternalServerError
        };

        var jsonResponse = JsonSerializer.Serialize(apiResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await response.WriteAsync(jsonResponse);
    }
}