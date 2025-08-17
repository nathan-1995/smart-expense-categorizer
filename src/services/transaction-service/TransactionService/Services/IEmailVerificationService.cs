using TransactionService.Models;

namespace TransactionService.Services;

public interface IEmailVerificationService
{
    Task<string> GenerateVerificationTokenAsync(Guid userId);
    Task<bool> VerifyEmailTokenAsync(string token);
    Task<EmailVerificationToken?> GetTokenAsync(string token);
    Task CleanupExpiredTokensAsync();
}