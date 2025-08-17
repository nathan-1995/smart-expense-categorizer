namespace TransactionService.Services;

public interface IEmailService
{
    Task<bool> SendVerificationEmailAsync(string email, string firstName, string verificationToken);
}