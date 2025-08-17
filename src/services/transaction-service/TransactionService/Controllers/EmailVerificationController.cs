using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransactionService.Data;
using TransactionService.Services;

namespace TransactionService.Controllers;

[ApiController]
[Route("api/email-verification")]
public class EmailVerificationController : ControllerBase
{
    private readonly IEmailVerificationService _emailVerificationService;
    private readonly IEmailService _emailService;
    private readonly AppDbContext _context;
    private readonly ILogger<EmailVerificationController> _logger;

    public EmailVerificationController(
        IEmailVerificationService emailVerificationService,
        IEmailService emailService,
        AppDbContext context,
        ILogger<EmailVerificationController> logger)
    {
        _emailVerificationService = emailVerificationService;
        _emailService = emailService;
        _context = context;
        _logger = logger;
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendVerificationEmail([FromBody] SendVerificationEmailRequest request)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.UserId);

            if (user == null)
            {
                return BadRequest(new { success = false, message = "User not found" });
            }

            if (user.IsEmailVerified)
            {
                return BadRequest(new { success = false, message = "Email is already verified" });
            }

            // Generate verification token
            var token = await _emailVerificationService.GenerateVerificationTokenAsync(user.Id);

            // Send verification email
            var emailSent = await _emailService.SendVerificationEmailAsync(
                user.Email, 
                user.FirstName ?? "User", 
                token);

            if (!emailSent)
            {
                return StatusCode(500, new { success = false, message = "Failed to send verification email" });
            }

            return Ok(new { 
                success = true, 
                message = "Verification email sent successfully",
                data = new { email = user.Email }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending verification email for user {UserId}", request.UserId);
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpPost("verify")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Token))
            {
                return BadRequest(new { success = false, message = "Token is required" });
            }

            var success = await _emailVerificationService.VerifyEmailTokenAsync(request.Token);

            if (!success)
            {
                return BadRequest(new { 
                    success = false, 
                    message = "Invalid, expired, or already used verification token" 
                });
            }

            // Get the verified user for response data
            var verificationToken = await _emailVerificationService.GetTokenAsync(request.Token);

            return Ok(new { 
                success = true, 
                message = "Email verified successfully",
                data = new { 
                    userId = verificationToken?.UserId,
                    email = verificationToken?.User?.Email
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying email with token {Token}", request.Token);
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpGet("status/{userId:guid}")]
    public async Task<IActionResult> GetVerificationStatus(Guid userId)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound(new { success = false, message = "User not found" });
            }

            return Ok(new {
                success = true,
                data = new {
                    userId = user.Id,
                    email = user.Email,
                    isEmailVerified = user.IsEmailVerified
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting verification status for user {UserId}", userId);
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpPost("resend")]
    public async Task<IActionResult> ResendVerificationEmail([FromBody] ResendVerificationEmailRequest request)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
            {
                // Don't reveal if email exists for security
                return Ok(new { 
                    success = true, 
                    message = "If the email exists, a verification email has been sent" 
                });
            }

            if (user.IsEmailVerified)
            {
                return BadRequest(new { success = false, message = "Email is already verified" });
            }

            // Generate new verification token
            var token = await _emailVerificationService.GenerateVerificationTokenAsync(user.Id);

            // Send verification email
            var emailSent = await _emailService.SendVerificationEmailAsync(
                user.Email, 
                user.FirstName ?? "User", 
                token);

            return Ok(new { 
                success = true, 
                message = "If the email exists, a verification email has been sent"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resending verification email for {Email}", request.Email);
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }
}

public record SendVerificationEmailRequest(Guid UserId);
public record VerifyEmailRequest(string Token);
public record ResendVerificationEmailRequest(string Email);