using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using TransactionService.Data;
using TransactionService.Models;

namespace TransactionService.Services;

public class EmailVerificationService : IEmailVerificationService
{
    private readonly AppDbContext _context;
    private readonly ILogger<EmailVerificationService> _logger;

    public EmailVerificationService(AppDbContext context, ILogger<EmailVerificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<string> GenerateVerificationTokenAsync(Guid userId)
    {
        // Generate a secure random token
        var tokenBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(tokenBytes);
        var token = Convert.ToBase64String(tokenBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('='); // URL-safe base64

        // Invalidate any existing tokens for this user
        var existingTokens = await _context.EmailVerificationTokens
            .Where(t => t.UserId == userId && !t.IsUsed)
            .ToListAsync();

        foreach (var existingToken in existingTokens)
        {
            existingToken.IsUsed = true;
            existingToken.UsedAt = DateTime.UtcNow;
        }

        // Create new token
        var verificationToken = new EmailVerificationToken
        {
            UserId = userId,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddDays(1), // 24 hours
            IsUsed = false
        };

        _context.EmailVerificationTokens.Add(verificationToken);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Generated verification token for user {UserId}", userId);
        return token;
    }

    public async Task<bool> VerifyEmailTokenAsync(string token)
    {
        var verificationToken = await _context.EmailVerificationTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == token);

        if (verificationToken == null)
        {
            _logger.LogWarning("Invalid verification token: {Token}", token);
            return false;
        }

        if (verificationToken.IsUsed)
        {
            _logger.LogWarning("Already used verification token: {Token}", token);
            return false;
        }

        if (verificationToken.ExpiresAt < DateTime.UtcNow)
        {
            _logger.LogWarning("Expired verification token: {Token}", token);
            return false;
        }

        // Mark token as used
        verificationToken.IsUsed = true;
        verificationToken.UsedAt = DateTime.UtcNow;

        // Mark user as verified
        verificationToken.User.IsEmailVerified = true;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Successfully verified email for user {UserId}", verificationToken.UserId);
        return true;
    }

    public async Task<EmailVerificationToken?> GetTokenAsync(string token)
    {
        return await _context.EmailVerificationTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == token);
    }

    public async Task CleanupExpiredTokensAsync()
    {
        var expiredTokens = await _context.EmailVerificationTokens
            .Where(t => t.ExpiresAt < DateTime.UtcNow)
            .ToListAsync();

        _context.EmailVerificationTokens.RemoveRange(expiredTokens);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Cleaned up {Count} expired verification tokens", expiredTokens.Count);
    }
}