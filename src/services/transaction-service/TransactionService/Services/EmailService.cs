using System.Text;
using System.Text.Json;

namespace TransactionService.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly HttpClient _httpClient;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger, HttpClient httpClient)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<bool> SendVerificationEmailAsync(string email, string firstName, string verificationToken)
    {
        try
        {
            var baseUrl = _configuration["FrontendBaseUrl"] ?? "http://localhost:3000";
            var verificationUrl = $"{baseUrl}/verify-email?token={verificationToken}";
            
            // Get MailerSend configuration from environment variables (more secure)
            var apiKey = Environment.GetEnvironmentVariable("MAILERSEND_API_KEY") ?? _configuration["MailerSend:ApiKey"];
            var fromEmail = Environment.GetEnvironmentVariable("MAILERSEND_FROM_EMAIL") ?? _configuration["MailerSend:FromEmail"] ?? "noreply@smartexpense.dev";
            var fromName = _configuration["MailerSend:FromName"] ?? "Smart Expense";
            
            _logger.LogInformation("Sending verification email to {Email} via MailerSend", email);
            
            var emailData = new
            {
                from = new { email = fromEmail, name = fromName },
                to = new[] { new { email, name = firstName } },
                subject = "Verify your email address",
                html = GenerateVerificationEmailHtml(firstName, verificationUrl),
                text = GenerateVerificationEmailText(firstName, verificationUrl)
            };

            var json = JsonSerializer.Serialize(emailData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            _httpClient.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("https://api.mailersend.com/v1/email", content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("✅ Verification email sent successfully to {Email}", email);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("MailerSend API error: {StatusCode} - {Error}", response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending verification email to {Email}. Exception type: {ExceptionType}, Message: {Message}", 
                email, ex.GetType().Name, ex.Message);
            return false;
        }
    }

    // Welcome email removed to reduce email volume - only verification email needed

    private static string GenerateVerificationEmailHtml(string firstName, string verificationUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Verify Your Email</title>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; margin: 0; padding: 20px; background-color: #f9fafb; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; border-radius: 8px; box-shadow: 0 1px 3px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 40px 30px; text-align: center; border-radius: 8px 8px 0 0; }}
        .header h1 {{ color: white; margin: 0; font-size: 28px; font-weight: 600; }}
        .content {{ padding: 40px 30px; }}
        .button {{ display: inline-block; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; text-decoration: none; padding: 16px 32px; border-radius: 6px; font-weight: 600; margin: 20px 0; }}
        .footer {{ padding: 30px; text-align: center; color: #6b7280; font-size: 14px; border-top: 1px solid #e5e7eb; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Smart Expense</h1>
        </div>
        <div class=""content"">
            <h2>Hi {firstName}!</h2>
            <p>Thanks for signing up for Smart Expense! To complete your registration and start managing your expenses, please verify your email address.</p>
            <p style=""text-align: center;"">
                <a href=""{verificationUrl}"" class=""button"">Verify My Email</a>
            </p>
            <p>Or copy and paste this link in your browser:</p>
            <p style=""word-break: break-all; color: #6b7280; font-family: monospace;"">{verificationUrl}</p>
            <p><strong>This link will expire in 24 hours.</strong></p>
        </div>
        <div class=""footer"">
            <p>If you didn't create an account, you can safely ignore this email.</p>
            <p>© 2025 Smart Expense. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }

    private static string GenerateVerificationEmailText(string firstName, string verificationUrl)
    {
        return $@"Hi {firstName}!

Thanks for signing up for Smart Expense! To complete your registration and start managing your expenses, please verify your email address.

Click here to verify: {verificationUrl}

This link will expire in 24 hours.

If you didn't create an account, you can safely ignore this email.

© 2025 Smart Expense. All rights reserved.";
    }

    // Welcome email templates removed - only verification email needed
}