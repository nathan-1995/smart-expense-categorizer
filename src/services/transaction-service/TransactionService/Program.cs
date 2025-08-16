using Microsoft.EntityFrameworkCore;
using Serilog;
using TransactionService.Data;

var builder = WebApplication.CreateBuilder(args);

// Configure URLs
builder.WebHost.UseUrls("http://localhost:5001");

// Add Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Transaction Service API", Version = "v1" });
});

// Database configuration with environment variable support
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Override with environment variables if they exist
var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "ExpenseTracker";
var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "root";
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "rootpassword";

// Build connection string from environment variables if available
if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DB_USER")))
{
    connectionString = $"Server={dbHost};Database={dbName};Uid={dbUser};Pwd={dbPassword};";
    Log.Information("Using environment variable connection string");
}
else
{
    Log.Information("Using appsettings.json connection string");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// CORS configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Transaction Service API v1");
    });
}

app.UseSerilogRequestLogging();

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

// Health check endpoint
app.MapHealthChecks("/health");

// Auto-migrate database on startup (for development only)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    try
    {
        Log.Information("Checking database connection...");
        var canConnect = await dbContext.Database.CanConnectAsync();
        Log.Information("Database connection: {CanConnect}", canConnect);
        
        Log.Information("Checking for pending migrations...");
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
        Log.Information("Pending migrations: {PendingMigrations}", string.Join(", ", pendingMigrations));
        
        if (pendingMigrations.Any())
        {
            Log.Information("Applying migrations...");
            await dbContext.Database.MigrateAsync();
            Log.Information("Database migration completed successfully");
        }
        else
        {
            Log.Information("No pending migrations found");
        }
        
        // Check if tables exist
        var tablesExist = await dbContext.Database.ExecuteSqlRawAsync("SHOW TABLES LIKE 'Users'");
        Log.Information("Users table check result: {TablesExist}", tablesExist);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Database migration failed");
        throw;
    }
}

Log.Information("Transaction Service starting up...");
app.Run();