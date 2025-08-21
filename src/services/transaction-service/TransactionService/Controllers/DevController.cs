using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransactionService.Data;
using TransactionService.Models;

namespace TransactionService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DevController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<DevController> _logger;

    public DevController(AppDbContext context, ILogger<DevController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpPost("promote-admin")]
    public async Task<ActionResult<object>> PromoteToAdmin([FromBody] PromoteAdminRequest request)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            
            if (user == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "User not found",
                    timestamp = DateTime.UtcNow
                });
            }

            user.Role = UserRole.Admin;
            user.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();

            _logger.LogInformation("User {Email} promoted to Admin", request.Email);

            return Ok(new
            {
                success = true,
                message = "User promoted to Admin successfully",
                data = new { user.Id, user.Email, user.Role },
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error promoting user {Email} to admin", request.Email);
            return StatusCode(500, new
            {
                success = false,
                message = "Internal server error",
                timestamp = DateTime.UtcNow
            });
        }
    }
}

public class PromoteAdminRequest
{
    public string Email { get; set; } = string.Empty;
}