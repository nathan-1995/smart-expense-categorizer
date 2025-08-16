using BCrypt.Net;

namespace ApiGateway.Services;

public class PasswordService : IPasswordService
{
    private const int MinPasswordLength = 8;
    private const int BcryptWorkFactor = 12; // Higher = more secure but slower

    public (string hashedPassword, string salt) HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));

        if (!IsValidPassword(password))
            throw new ArgumentException("Password does not meet security requirements", nameof(password));

        // Generate salt
        var salt = BCrypt.Net.BCrypt.GenerateSalt(BcryptWorkFactor);
        
        // Hash password with salt
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password, salt);
        
        return (hashedPassword, salt);
    }

    public bool VerifyPassword(string password, string hashedPassword, string salt)
    {
        if (string.IsNullOrWhiteSpace(password) || 
            string.IsNullOrWhiteSpace(hashedPassword) || 
            string.IsNullOrWhiteSpace(salt))
            return false;

        try
        {
            // BCrypt.Verify handles the salt internally when using the full hash
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
        catch
        {
            return false;
        }
    }

    public bool IsValidPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return false;

        // Password requirements:
        // - At least 8 characters long
        // - Contains at least one uppercase letter
        // - Contains at least one lowercase letter  
        // - Contains at least one digit
        // - Contains at least one special character
        
        if (password.Length < MinPasswordLength)
            return false;

        bool hasUpper = password.Any(char.IsUpper);
        bool hasLower = password.Any(char.IsLower);
        bool hasDigit = password.Any(char.IsDigit);
        bool hasSpecial = password.Any(c => !char.IsLetterOrDigit(c));

        return hasUpper && hasLower && hasDigit && hasSpecial;
    }
}