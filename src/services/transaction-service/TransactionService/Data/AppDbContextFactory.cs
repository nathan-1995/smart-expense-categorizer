using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TransactionService.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        
        // Get connection string from environment variables for design-time operations
        var server = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
        var database = Environment.GetEnvironmentVariable("DB_NAME") ?? "ExpenseTracker";
        var username = Environment.GetEnvironmentVariable("DB_USER") ?? "root";
        var password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "rootpassword";
        
        var connectionString = $"Server={server};Database={database};Uid={username};Pwd={password};AllowUserVariables=true;UseAffectedRows=false;";
        
        optionsBuilder.UseMySql(
            connectionString,
            new MySqlServerVersion(new Version(8, 0, 21)));

        return new AppDbContext(optionsBuilder.Options);
    }
}