using System.Text;
using ApiGateway.Middleware;
using ApiGateway.Models;
using ApiGateway.Services;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure URLs
builder.WebHost.UseUrls("http://localhost:5000");

// Add Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Configuration binding
builder.Services.Configure<ServicesConfiguration>(
    builder.Configuration.GetSection("Services"));
builder.Services.Configure<AuthenticationConfiguration>(
    builder.Configuration.GetSection("Authentication"));
builder.Services.Configure<GatewayRateLimitConfiguration>(
    builder.Configuration.GetSection("RateLimit"));
builder.Services.Configure<CorsConfiguration>(
    builder.Configuration.GetSection("Cors"));

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Smart Expense Categorizer API Gateway", Version = "v1" });
    
    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement()
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

// Configure CORS
var corsConfig = builder.Configuration.GetSection("Cors").Get<CorsConfiguration>();
if (corsConfig != null)
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowConfiguredOrigins", policy =>
        {
            policy.WithOrigins(corsConfig.AllowedOrigins)
                  .WithMethods(corsConfig.AllowedMethods)
                  .WithHeaders(corsConfig.AllowedHeaders);
                  
            if (corsConfig.AllowCredentials)
            {
                policy.AllowCredentials();
            }
        });
    });
}

// Configure Authentication
var authConfig = builder.Configuration.GetSection("Authentication").Get<AuthenticationConfiguration>();
if (authConfig != null)
{
    var key = Encoding.UTF8.GetBytes(authConfig.Jwt.SecretKey);
    
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false; // Set to true in production
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = authConfig.Jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = authConfig.Jwt.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
        
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Log.Warning("JWT Authentication failed: {Exception}", context.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Log.Debug("JWT Token validated for user: {UserId}", 
                    context.Principal?.Identity?.Name);
                return Task.CompletedTask;
            }
        };
    });
    
    // Add Google OAuth (if configured)
    if (!string.IsNullOrEmpty(authConfig.Google.ClientId))
    {
        builder.Services.AddAuthentication()
            .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
            {
                options.ClientId = authConfig.Google.ClientId;
                options.ClientSecret = authConfig.Google.ClientSecret;
            });
    }
}

// Configure Rate Limiting
var rateLimitConfig = builder.Configuration.GetSection("RateLimit").Get<GatewayRateLimitConfiguration>();
if (rateLimitConfig?.EnableRateLimiting == true)
{
    builder.Services.AddMemoryCache();
    builder.Services.Configure<IpRateLimitOptions>(options =>
    {
        options.EnableEndpointRateLimiting = true;
        options.StackBlockedRequests = false;
        options.HttpStatusCode = 429;
        options.RealIpHeader = "X-Real-IP";
        options.ClientIdHeader = "X-ClientId";
        options.GeneralRules = new List<RateLimitRule>
        {
            new()
            {
                Endpoint = "*",
                Period = rateLimitConfig.General.Period,
                Limit = rateLimitConfig.General.Limit
            }
        };
    });
    
    builder.Services.AddInMemoryRateLimiting();
    builder.Services.AddSingleton<IRateLimitConfiguration, AspNetCoreRateLimit.RateLimitConfiguration>();
}

// Add HttpClient with Polly for resilience
builder.Services.AddHttpClient<IProxyService, ProxyService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "SmartExpenseGateway/1.0");
});

builder.Services.AddHttpClient<IHealthCheckService, HealthCheckService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.Add("User-Agent", "SmartExpenseGateway-HealthCheck/1.0");
});

builder.Services.AddHttpClient<IUserService, UserService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "SmartExpenseGateway-UserService/1.0");
});

// Register custom services
builder.Services.AddScoped<IProxyService, ProxyService>();
builder.Services.AddScoped<IHealthCheckService, HealthCheckService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();

// Add health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Smart Expense Categorizer API Gateway v1");
        c.RoutePrefix = "swagger";
    });
}

// Add custom middleware pipeline (before request logging to avoid interfering with static files)
app.UseMiddleware<ErrorHandlingMiddleware>();

// Add Serilog request logging (after Swagger to avoid logging static files)
app.UseSerilogRequestLogging(opts => 
{
    opts.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
});

// Add custom request logging middleware (skip for Swagger static files)
app.UseWhen(context => !context.Request.Path.StartsWithSegments("/swagger"), 
    appBuilder => appBuilder.UseMiddleware<RequestLoggingMiddleware>());

// Add CORS
if (corsConfig != null)
{
    app.UseCors("AllowConfiguredOrigins");
}

// Add Rate Limiting
if (rateLimitConfig?.EnableRateLimiting == true)
{
    app.UseIpRateLimiting();
}

// Add Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Add Admin Authorization Middleware
app.UseMiddleware<AdminAuthorizationMiddleware>();

// Map controllers
app.MapControllers();

// Health check endpoint
app.MapHealthChecks("/health");

// Root endpoint
app.MapGet("/", () => new
{
    service = "Smart Expense Categorizer API Gateway",
    version = "1.0.0",
    status = "Running",
    timestamp = DateTime.UtcNow,
    endpoints = new[]
    {
        "/health - Gateway health check",
        "/api/health - Detailed health checks",
        "/api/health/services - All services health",
        "/api/auth/google - Google OAuth login",
        "/api/auth/token - Generate JWT token (dev only)",
        "/api/v1/transactions/* - Transaction service proxy",
        "/api/v1/test/* - Test endpoints proxy",
        "/swagger - API documentation"
    }
});

Log.Information("API Gateway starting up on http://localhost:5000");
app.Run();

// Make Program class public for testing
public partial class Program { }