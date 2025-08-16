using Microsoft.EntityFrameworkCore;
using TransactionService.Models;

namespace TransactionService.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Budget> Budgets { get; set; }
    public DbSet<FileUpload> Files { get; set; }
    public DbSet<UserSettings> UserSettings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            // Only create unique index for non-null OAuthId values
            entity.HasIndex(e => e.OAuthId).IsUnique().HasFilter("OAuthId IS NOT NULL");
            entity.HasIndex(e => e.Email).IsUnique();
            
            // Remove default value SQL - we'll handle this in C# code
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime");
            
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime");
        });

        // Category configuration
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.Name }).IsUnique();
            entity.HasIndex(e => e.UserId);
            
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime");

            entity.HasOne(d => d.User)
                .WithMany(p => p.Categories)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Transaction configuration
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CategoryId);
            entity.HasIndex(e => e.Date);
            entity.HasIndex(e => e.Amount);
            entity.HasIndex(e => new { e.UserId, e.Date });

            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime");
            
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime");

            entity.HasOne(d => d.User)
                .WithMany(p => p.Transactions)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Category)
                .WithMany(p => p.Transactions)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Budget configuration
        modelBuilder.Entity<Budget>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.CategoryId }).IsUnique();
            entity.HasIndex(e => e.UserId);

            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime");
            
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime");

            entity.HasOne(d => d.User)
                .WithMany(p => p.Budgets)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Category)
                .WithMany(p => p.Budgets)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // FileUpload configuration
        modelBuilder.Entity<FileUpload>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ProcessingStatus);

            entity.Property(e => e.UploadedAt)
                .HasColumnType("datetime");

            entity.HasOne(d => d.User)
                .WithMany(p => p.Files)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // UserSettings configuration
        modelBuilder.Entity<UserSettings>(entity =>
        {
            entity.HasIndex(e => e.UserId).IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime");
            
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime");

            entity.HasOne(d => d.User)
                .WithOne(p => p.Settings)
                .HasForeignKey<UserSettings>(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Seed default categories (these will be created for each user by the application)
        // We'll handle this in the service layer rather than database seeding
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker
            .Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entityEntry in entries)
        {
            if (entityEntry.Entity is User user)
            {
                if (entityEntry.State == EntityState.Added)
                {
                    user.CreatedAt = DateTime.UtcNow;
                }
                user.UpdatedAt = DateTime.UtcNow;
            }
            else if (entityEntry.Entity is Transaction transaction)
            {
                if (entityEntry.State == EntityState.Added)
                {
                    transaction.CreatedAt = DateTime.UtcNow;
                }
                transaction.UpdatedAt = DateTime.UtcNow;
            }
            else if (entityEntry.Entity is Budget budget)
            {
                if (entityEntry.State == EntityState.Added)
                {
                    budget.CreatedAt = DateTime.UtcNow;
                }
                budget.UpdatedAt = DateTime.UtcNow;
            }
            else if (entityEntry.Entity is UserSettings settings)
            {
                if (entityEntry.State == EntityState.Added)
                {
                    settings.CreatedAt = DateTime.UtcNow;
                }
                settings.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}