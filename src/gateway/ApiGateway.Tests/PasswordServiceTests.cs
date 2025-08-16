using ApiGateway.Services;
using Xunit;

namespace ApiGateway.Tests;

public class PasswordServiceTests
{
    private readonly PasswordService _passwordService;

    public PasswordServiceTests()
    {
        _passwordService = new PasswordService();
    }

    [Theory]
    [InlineData("StrongP@ssw0rd!", true)]
    [InlineData("MySecure123!", true)]
    [InlineData("Complex#Pass1", true)]
    [InlineData("AnotherGood2@", true)]
    public void IsValidPassword_StrongPasswords_ReturnsTrue(string password, bool expected)
    {
        // Act
        var result = _passwordService.IsValidPassword(password);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("weak", false)] // Too short
    [InlineData("password", false)] // No uppercase, digit, or special char
    [InlineData("PASSWORD", false)] // No lowercase, digit, or special char
    [InlineData("Password", false)] // No digit or special char
    [InlineData("Password1", false)] // No special char
    [InlineData("Password@", false)] // No digit
    [InlineData("password1@", false)] // No uppercase
    [InlineData("PASSWORD1@", false)] // No lowercase
    [InlineData("", false)] // Empty
    [InlineData("   ", false)] // Whitespace only
    public void IsValidPassword_WeakPasswords_ReturnsFalse(string password, bool expected)
    {
        // Act
        var result = _passwordService.IsValidPassword(password);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void HashPassword_ValidPassword_ReturnsHashAndSalt()
    {
        // Arrange
        var password = "StrongP@ssw0rd!";

        // Act
        var (hashedPassword, salt) = _passwordService.HashPassword(password);

        // Assert
        Assert.NotNull(hashedPassword);
        Assert.NotNull(salt);
        Assert.NotEmpty(hashedPassword);
        Assert.NotEmpty(salt);
        Assert.NotEqual(password, hashedPassword);
    }

    [Fact]
    public void HashPassword_SamePassword_ReturnsDifferentHashes()
    {
        // Arrange
        var password = "StrongP@ssw0rd!";

        // Act
        var (hash1, salt1) = _passwordService.HashPassword(password);
        var (hash2, salt2) = _passwordService.HashPassword(password);

        // Assert
        Assert.NotEqual(hash1, hash2);
        Assert.NotEqual(salt1, salt2);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("weak")]
    public void HashPassword_InvalidPassword_ThrowsArgumentException(string password)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _passwordService.HashPassword(password));
    }

    [Fact]
    public void VerifyPassword_CorrectPassword_ReturnsTrue()
    {
        // Arrange
        var password = "StrongP@ssw0rd!";
        var (hashedPassword, salt) = _passwordService.HashPassword(password);

        // Act
        var result = _passwordService.VerifyPassword(password, hashedPassword, salt);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_IncorrectPassword_ReturnsFalse()
    {
        // Arrange
        var correctPassword = "StrongP@ssw0rd!";
        var incorrectPassword = "WrongPassword!";
        var (hashedPassword, salt) = _passwordService.HashPassword(correctPassword);

        // Act
        var result = _passwordService.VerifyPassword(incorrectPassword, hashedPassword, salt);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("", "hash", "salt")]
    [InlineData("password", "", "salt")]
    [InlineData("password", "hash", "")]
    [InlineData("password", null, "salt")]
    [InlineData("password", "hash", null)]
    public void VerifyPassword_InvalidInputs_ReturnsFalse(string password, string hash, string salt)
    {
        // Act
        var result = _passwordService.VerifyPassword(password, hash, salt);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyPassword_CorruptedHash_ReturnsFalse()
    {
        // Arrange
        var password = "StrongP@ssw0rd!";
        var (hashedPassword, salt) = _passwordService.HashPassword(password);
        var corruptedHash = hashedPassword.Substring(0, hashedPassword.Length - 5) + "XXXXX";

        // Act
        var result = _passwordService.VerifyPassword(password, corruptedHash, salt);

        // Assert
        Assert.False(result);
    }
}