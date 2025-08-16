namespace ApiGateway.Services;

public interface IPasswordService
{
    /// <summary>
    /// Generates a salt and hashes a password
    /// </summary>
    /// <param name="password">The plain text password</param>
    /// <returns>A tuple containing (hashedPassword, salt)</returns>
    (string hashedPassword, string salt) HashPassword(string password);
    
    /// <summary>
    /// Verifies a password against a hash and salt
    /// </summary>
    /// <param name="password">The plain text password to verify</param>
    /// <param name="hashedPassword">The stored hash</param>
    /// <param name="salt">The stored salt</param>
    /// <returns>True if password matches, false otherwise</returns>
    bool VerifyPassword(string password, string hashedPassword, string salt);
    
    /// <summary>
    /// Validates password strength
    /// </summary>
    /// <param name="password">The password to validate</param>
    /// <returns>True if password meets requirements, false otherwise</returns>
    bool IsValidPassword(string password);
}