using ApiGateway.Models;

namespace ApiGateway.Services;

public interface IUserService
{
    /// <summary>
    /// Creates a new user in the transaction service
    /// </summary>
    Task<UserInfo?> CreateUserAsync(CreateUserRequest request);
    
    /// <summary>
    /// Gets a user by email from the transaction service
    /// </summary>
    Task<UserInfo?> GetUserByEmailAsync(string email);
    
    /// <summary>
    /// Gets a user by OAuth ID from the transaction service
    /// </summary>
    Task<UserInfo?> GetUserByOAuthIdAsync(string oauthId, string provider);
    
    /// <summary>
    /// Validates user credentials for password login
    /// </summary>
    Task<UserInfo?> ValidateUserCredentialsAsync(string email, string password);
}