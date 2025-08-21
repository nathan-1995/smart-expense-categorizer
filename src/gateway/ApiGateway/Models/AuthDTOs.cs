using System.ComponentModel.DataAnnotations;

namespace ApiGateway.Models;

public class RegisterRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;
    
    [Required]
    [Compare("Password")]
    public string ConfirmPassword { get; set; } = string.Empty;
    
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}

public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string Password { get; set; } = string.Empty;
}

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public UserInfo User { get; set; } = new();
    public DateTime ExpiresAt { get; set; }
}

public class UserInfo
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool IsEmailVerified { get; set; }
    public string Role { get; set; } = "User";
}

public class CreateUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public string? PasswordSalt { get; set; }
    public string? OAuthId { get; set; }
    public string? OAuthProvider { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool IsEmailVerified { get; set; } = false;
}