using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransactionService.Data;
using TransactionService.Models;

namespace TransactionService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<TestController> _logger;

    public TestController(AppDbContext context, ILogger<TestController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("health")]
    public async Task<IActionResult> Health()
    {
        try
        {
            // Test database connection
            var canConnect = await _context.Database.CanConnectAsync();
            
            return Ok(new { 
                status = "healthy", 
                database = canConnect ? "connected" : "disconnected",
                timestamp = DateTime.UtcNow 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return StatusCode(500, new { status = "unhealthy", error = ex.Message });
        }
    }

    [HttpPost("create-test-user")]
    public async Task<IActionResult> CreateTestUser()
    {
        try
        {
            // First, test if we can connect to the database
            var canConnect = await _context.Database.CanConnectAsync();
            if (!canConnect)
            {
                return StatusCode(500, new { error = "Cannot connect to database" });
            }

            var testUser = new User
            {
                OAuthId = "test-oauth-" + Guid.NewGuid(),
                Email = $"test-{DateTime.UtcNow.Ticks}@example.com",
                FirstName = "Test",
                LastName = "User"
            };

            _context.Users.Add(testUser);
            await _context.SaveChangesAsync();

            // Create a default category for this user
            var defaultCategory = new Category
            {
                UserId = testUser.Id,
                Name = "General",
                Color = "#3B82F6",
                Icon = "folder",
                IsDefault = true
            };

            _context.Categories.Add(defaultCategory);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created test user with ID: {UserId}", testUser.Id);

            return Ok(new { 
                user = new { 
                    testUser.Id, 
                    testUser.OAuthId, 
                    testUser.Email, 
                    testUser.FirstName, 
                    testUser.LastName, 
                    testUser.CreatedAt 
                }, 
                category = new { 
                    defaultCategory.Id, 
                    defaultCategory.Name, 
                    defaultCategory.Color, 
                    defaultCategory.Icon 
                },
                message = "Test user created successfully" 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create test user. Full error: {Error}", ex.ToString());
            
            return StatusCode(500, new { 
                error = ex.Message,
                innerError = ex.InnerException?.Message,
                stackTrace = ex.StackTrace,
                fullError = ex.ToString()
            });
        }
    }

    [HttpPost("create-test-transaction")]
    public async Task<IActionResult> CreateTestTransaction()
    {
        try
        {
            // Get the first user and category
            var user = await _context.Users.FirstOrDefaultAsync();
            var category = await _context.Categories.FirstOrDefaultAsync();

            if (user == null)
            {
                return BadRequest("No users found. Create a test user first.");
            }

            var testTransaction = new Transaction
            {
                UserId = user.Id,
                CategoryId = category?.Id,
                Amount = 25.50m,
                Description = "Test Coffee Purchase",
                Date = DateTime.Today,
                MerchantName = "Starbucks",
                Source = TransactionSource.Manual
            };

            _context.Transactions.Add(testTransaction);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created test transaction with ID: {TransactionId}", testTransaction.Id);

            return Ok(new { 
                transaction = new {
                    testTransaction.Id,
                    testTransaction.Amount,
                    testTransaction.Description,
                    testTransaction.Date,
                    testTransaction.MerchantName,
                    testTransaction.Source,
                    CategoryName = category?.Name
                },
                message = "Test transaction created successfully" 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create test transaction");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        try
        {
            var users = await _context.Users
                .Select(u => new {
                    u.Id,
                    u.OAuthId,
                    u.Email,
                    u.FirstName,
                    u.LastName,
                    u.CreatedAt,
                    Categories = u.Categories.Select(c => new {
                        c.Id,
                        c.Name,
                        c.Color,
                        c.Icon
                    }).ToList(),
                    Transactions = u.Transactions.Select(t => new {
                        t.Id,
                        t.Amount,
                        t.Description,
                        t.Date,
                        t.MerchantName,
                        CategoryName = t.Category != null ? t.Category.Name : null
                    }).ToList()
                })
                .ToListAsync();

            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get users");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpDelete("cleanup")]
    public async Task<IActionResult> Cleanup()
    {
        try
        {
            // Delete all test data
            _context.Transactions.RemoveRange(_context.Transactions);
            _context.Categories.RemoveRange(_context.Categories);
            _context.Users.RemoveRange(_context.Users);
            
            await _context.SaveChangesAsync();

            return Ok(new { message = "All test data cleaned up" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup test data");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}