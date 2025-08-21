using System.Text;
using System.Text.Json;
using ApiGateway.Models;

namespace ApiGateway.Services;

public class UserService : IUserService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UserService> _logger;
    private readonly IPasswordService _passwordService;
    private readonly string _transactionServiceUrl;

    public UserService(
        HttpClient httpClient, 
        ILogger<UserService> logger, 
        IPasswordService passwordService,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _passwordService = passwordService;
        _transactionServiceUrl = configuration.GetValue<string>("Services:TransactionService:BaseUrl") 
            ?? "http://localhost:5001";
    }

    public async Task<UserInfo?> CreateUserAsync(CreateUserRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_transactionServiceUrl}/api/users", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to create user. Status: {Status}, Content: {Content}", 
                    response.StatusCode, errorContent);
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<UserInfo>>(responseContent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            return apiResponse?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user with email {Email}", request.Email);
            return null;
        }
    }

    public async Task<UserInfo?> GetUserByEmailAsync(string email)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_transactionServiceUrl}/api/users/by-email/{email}");
            
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;
                    
                _logger.LogError("Failed to get user by email. Status: {Status}", response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<UserInfo>>(content, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            return apiResponse?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by email {Email}", email);
            return null;
        }
    }

    public async Task<UserInfo?> GetUserByOAuthIdAsync(string oauthId, string provider)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_transactionServiceUrl}/api/users/by-oauth/{provider}/{oauthId}");
            
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;
                    
                _logger.LogError("Failed to get user by OAuth ID. Status: {Status}", response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<UserInfo>>(content, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            return apiResponse?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by OAuth ID {OAuthId} for provider {Provider}", oauthId, provider);
            return null;
        }
    }

    public async Task<UserInfo?> ValidateUserCredentialsAsync(string email, string password)
    {
        try
        {
            var user = await GetUserByEmailAsync(email);
            if (user == null)
                return null;

            // For OAuth users, password validation doesn't apply
            if (string.IsNullOrEmpty(user.Id))
                return null;

            // Get user with password details for validation
            var response = await _httpClient.GetAsync($"{_transactionServiceUrl}/api/users/{user.Id}/credentials");
            
            if (!response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync();
            var credentialsResponse = JsonSerializer.Deserialize<ApiResponse<UserCredentials>>(content, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var credentials = credentialsResponse?.Data;
            if (credentials?.PasswordHash == null || credentials?.PasswordSalt == null)
                return null;

            // Verify password
            if (_passwordService.VerifyPassword(password, credentials.PasswordHash, credentials.PasswordSalt))
                return user;

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating credentials for email {Email}", email);
            return null;
        }
    }

    public async Task<bool> UpdateLastSeenAsync(string userId)
    {
        try
        {
            var url = $"{_transactionServiceUrl}/api/users/{userId}/last-seen";
            _logger.LogInformation("Updating last seen for user {UserId} at URL: {Url}", userId, url);
            
            var response = await _httpClient.PutAsync(url, null);
            _logger.LogInformation("Last seen update response for user {UserId}: {StatusCode}", userId, response.StatusCode);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to update last seen for user {UserId}. Status: {Status}, Content: {Content}", 
                    userId, response.StatusCode, errorContent);
            }
            
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating last seen for user {UserId}", userId);
            return false;
        }
    }
}

public class UserCredentials
{
    public string? PasswordHash { get; set; }
    public string? PasswordSalt { get; set; }
}