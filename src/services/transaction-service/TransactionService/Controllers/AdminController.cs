using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransactionService.Data;
using TransactionService.Models;

namespace TransactionService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<AdminController> _logger;

    public AdminController(AppDbContext context, ILogger<AdminController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("users")]
    public async Task<ActionResult<object>> GetUsers()
    {
        try
        {
            var users = await _context.Users
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.FirstName,
                    u.LastName,
                    u.IsEmailVerified,
                    u.Role,
                    u.LastSeenAt,
                    u.CreatedAt,
                    TransactionCount = u.Transactions.Count(),
                    CategoryCount = u.Categories.Count()
                })
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            return Ok(new
            {
                success = true,
                data = users,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users list");
            return StatusCode(500, new
            {
                success = false,
                message = "Internal server error",
                timestamp = DateTime.UtcNow
            });
        }
    }

    [HttpGet("users/{userId}")]
    public async Task<ActionResult<object>> GetUserDetails(Guid userId)
    {
        try
        {
            var user = await _context.Users
                .Include(u => u.Transactions)
                .Include(u => u.Categories)
                .Include(u => u.Budgets)
                .Include(u => u.Files)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "User not found",
                    timestamp = DateTime.UtcNow
                });
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    user.Id,
                    user.Email,
                    user.FirstName,
                    user.LastName,
                    user.IsEmailVerified,
                    user.Role,
                    user.LastSeenAt,
                    user.CreatedAt,
                    user.UpdatedAt,
                    Statistics = new
                    {
                        TransactionCount = user.Transactions.Count,
                        CategoryCount = user.Categories.Count,
                        BudgetCount = user.Budgets.Count,
                        FileUploadCount = user.Files.Count,
                        TotalSpent = user.Transactions.Sum(t => t.Amount),
                        LastTransactionDate = user.Transactions.Any() ? user.Transactions.Max(t => t.Date) : (DateTime?)null
                    }
                },
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user details for {UserId}", userId);
            return StatusCode(500, new
            {
                success = false,
                message = "Internal server error",
                timestamp = DateTime.UtcNow
            });
        }
    }

    [HttpDelete("users/{userId}")]
    public async Task<ActionResult<object>> DeleteUser(Guid userId)
    {
        try
        {
            var user = await _context.Users
                .Include(u => u.Transactions)
                .Include(u => u.Categories)
                .Include(u => u.Budgets)
                .Include(u => u.Files)
                .Include(u => u.EmailVerificationTokens)
                .Include(u => u.Settings)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "User not found",
                    timestamp = DateTime.UtcNow
                });
            }

            // Don't allow deleting admin users
            if (user.Role == UserRole.Admin)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Cannot delete admin users",
                    timestamp = DateTime.UtcNow
                });
            }

            _logger.LogWarning("Admin deleting user {UserId} ({Email})", userId, user.Email);

            // Remove all user data
            _context.EmailVerificationTokens.RemoveRange(user.EmailVerificationTokens);
            _context.Transactions.RemoveRange(user.Transactions);
            _context.Budgets.RemoveRange(user.Budgets);
            _context.Categories.RemoveRange(user.Categories);
            _context.Files.RemoveRange(user.Files);
            
            if (user.Settings != null)
            {
                _context.UserSettings.Remove(user.Settings);
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "User and all associated data deleted successfully",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", userId);
            return StatusCode(500, new
            {
                success = false,
                message = "Internal server error",
                timestamp = DateTime.UtcNow
            });
        }
    }

    [HttpPut("users/{userId}/role")]
    public async Task<ActionResult<object>> UpdateUserRole(Guid userId, [FromBody] UpdateUserRoleRequest request)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            
            if (user == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "User not found",
                    timestamp = DateTime.UtcNow
                });
            }

            user.Role = request.Role;
            user.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} role updated to {Role}", userId, request.Role);

            return Ok(new
            {
                success = true,
                message = "User role updated successfully",
                data = new { user.Id, user.Email, user.Role },
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user role for {UserId}", userId);
            return StatusCode(500, new
            {
                success = false,
                message = "Internal server error",
                timestamp = DateTime.UtcNow
            });
        }
    }

    [HttpGet("stats")]
    public async Task<ActionResult<object>> GetSystemStats()
    {
        try
        {
            var stats = new
            {
                TotalUsers = await _context.Users.CountAsync(),
                AdminUsers = await _context.Users.CountAsync(u => u.Role == UserRole.Admin),
                VerifiedUsers = await _context.Users.CountAsync(u => u.IsEmailVerified),
                TotalTransactions = await _context.Transactions.CountAsync(),
                TotalCategories = await _context.Categories.CountAsync(),
                TotalBudgets = await _context.Budgets.CountAsync(),
                RecentRegistrations = await _context.Users
                    .Where(u => u.CreatedAt >= DateTime.UtcNow.AddDays(-7))
                    .CountAsync(),
                ActiveUsers = await _context.Users
                    .Where(u => u.LastSeenAt >= DateTime.UtcNow.AddDays(-30))
                    .CountAsync()
            };

            return Ok(new
            {
                success = true,
                data = stats,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving system stats");
            return StatusCode(500, new
            {
                success = false,
                message = "Internal server error",
                timestamp = DateTime.UtcNow
            });
        }
    }
}

public class UpdateUserRoleRequest
{
    public UserRole Role { get; set; }
}