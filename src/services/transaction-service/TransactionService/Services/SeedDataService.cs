using Microsoft.EntityFrameworkCore;
using TransactionService.Data;
using TransactionService.Models;
using BCrypt.Net;

namespace TransactionService.Services;

public class SeedDataService
{
    private readonly AppDbContext _context;
    private readonly ILogger<SeedDataService> _logger;

    public SeedDataService(AppDbContext context, ILogger<SeedDataService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedUsersAsync()
    {
        try
        {
            // Check if any users exist
            var existingUserCount = await _context.Users.CountAsync();
            if (existingUserCount > 0)
            {
                _logger.LogInformation("Users already exist, skipping seed data");
                return;
            }

            _logger.LogInformation("No users found, creating seed data...");

            // Create seed users with proper password hashing
            var seedUsers = new List<User>
            {
                // Admin user
                new User
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                    Email = "admin@smartexpense.com",
                    FirstName = "System",
                    LastName = "Administrator",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPass123!", BCrypt.Net.BCrypt.GenerateSalt(12)),
                    PasswordSalt = "seed_admin_salt",
                    IsEmailVerified = true,
                    Role = UserRole.Admin,
                    CreatedAt = DateTime.UtcNow.AddDays(-30),
                    UpdatedAt = DateTime.UtcNow,
                    LastSeenAt = DateTime.UtcNow
                },
                // Alice - Verified customer
                new User
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Email = "alice.johnson@gmail.com",
                    FirstName = "Alice",
                    LastName = "Johnson",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPass123!", BCrypt.Net.BCrypt.GenerateSalt(12)),
                    PasswordSalt = "seed_alice_salt",
                    IsEmailVerified = true,
                    Role = UserRole.User,
                    CreatedAt = DateTime.UtcNow.AddDays(-15),
                    UpdatedAt = DateTime.UtcNow,
                    LastSeenAt = DateTime.UtcNow.AddDays(-2)
                },
                // Bob - Unverified customer
                new User
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Email = "bob.wilson@yahoo.com",
                    FirstName = "Bob",
                    LastName = "Wilson",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPass123!", BCrypt.Net.BCrypt.GenerateSalt(12)),
                    PasswordSalt = "seed_bob_salt",
                    IsEmailVerified = false,
                    Role = UserRole.User,
                    CreatedAt = DateTime.UtcNow.AddDays(-10),
                    UpdatedAt = DateTime.UtcNow,
                    LastSeenAt = DateTime.UtcNow.AddDays(-1)
                },
                // Carol - Verified customer
                new User
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    Email = "carol.davis@hotmail.com",
                    FirstName = "Carol",
                    LastName = "Davis",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPass123!", BCrypt.Net.BCrypt.GenerateSalt(12)),
                    PasswordSalt = "seed_carol_salt",
                    IsEmailVerified = true,
                    Role = UserRole.User,
                    CreatedAt = DateTime.UtcNow.AddDays(-7),
                    UpdatedAt = DateTime.UtcNow,
                    LastSeenAt = DateTime.UtcNow.AddHours(-5)
                }
            };

            _context.Users.AddRange(seedUsers);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully created {Count} seed users", seedUsers.Count);

            // Create some categories for Alice and Carol
            var seedCategories = new List<Category>
            {
                new Category
                {
                    Id = Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111"),
                    UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"), // Alice
                    Name = "Food & Dining",
                    Color = "#FF6B6B",
                    CreatedAt = DateTime.UtcNow
                },
                new Category
                {
                    Id = Guid.Parse("aaaaaaaa-2222-2222-2222-222222222222"),
                    UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"), // Alice
                    Name = "Transportation",
                    Color = "#4ECDC4",
                    CreatedAt = DateTime.UtcNow
                },
                new Category
                {
                    Id = Guid.Parse("cccccccc-1111-1111-1111-111111111111"),
                    UserId = Guid.Parse("33333333-3333-3333-3333-333333333333"), // Carol
                    Name = "Healthcare",
                    Color = "#F8B500",
                    CreatedAt = DateTime.UtcNow
                }
            };

            _context.Categories.AddRange(seedCategories);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully created {Count} seed categories", seedCategories.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating seed data");
            throw;
        }
    }
}