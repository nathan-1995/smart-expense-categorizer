namespace ApiGateway.Models;

public class ServiceConfiguration
{
    public string BaseUrl { get; set; } = string.Empty;
    public int Timeout { get; set; } = 30;
}

public class ServicesConfiguration
{
    public ServiceConfiguration TransactionService { get; set; } = new();
}

public class JwtConfiguration
{
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public int ExpireMinutes { get; set; } = 60;
}

public class AuthenticationConfiguration
{
    public JwtConfiguration Jwt { get; set; } = new();
    public GoogleConfiguration Google { get; set; } = new();
}

public class GoogleConfiguration
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}

public class GatewayRateLimitConfiguration
{
    public bool EnableRateLimiting { get; set; } = true;
    public GeneralRateLimit General { get; set; } = new();
}

public class GeneralRateLimit
{
    public int Limit { get; set; } = 100;
    public string Period { get; set; } = "1m";
}

public class CorsConfiguration
{
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
    public string[] AllowedMethods { get; set; } = Array.Empty<string>();
    public string[] AllowedHeaders { get; set; } = Array.Empty<string>();
    public bool AllowCredentials { get; set; } = true;
}