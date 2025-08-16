using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ApiGateway.Models;
using ApiGateway.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace ApiGateway.Tests;

public class AuthControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<IPasswordService> _mockPasswordService;

    public AuthControllerTests(WebApplicationFactory<Program> factory)
    {
        _mockUserService = new Mock<IUserService>();
        _mockPasswordService = new Mock<IPasswordService>();
        
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the existing registrations
                var userServiceDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IUserService));
                if (userServiceDescriptor != null)
                    services.Remove(userServiceDescriptor);

                var passwordServiceDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IPasswordService));
                if (passwordServiceDescriptor != null)
                    services.Remove(passwordServiceDescriptor);

                // Add mocks
                services.AddSingleton(_mockUserService.Object);
                services.AddSingleton(_mockPasswordService.Object);
            });
        });
    }

    [Fact]
    public async Task Register_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "StrongP@ssw0rd!",
            ConfirmPassword = "StrongP@ssw0rd!",
            FirstName = "John",
            LastName = "Doe"
        };

        var expectedUser = new UserInfo
        {
            Id = Guid.NewGuid().ToString(),
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            IsEmailVerified = false
        };

        _mockPasswordService.Setup(x => x.IsValidPassword(request.Password))
            .Returns(true);
        
        _mockPasswordService.Setup(x => x.HashPassword(request.Password))
            .Returns(("hashedPassword", "salt"));

        _mockUserService.Setup(x => x.GetUserByEmailAsync(request.Email))
            .ReturnsAsync((UserInfo?)null);

        _mockUserService.Setup(x => x.CreateUserAsync(It.IsAny<CreateUserRequest>()))
            .ReturnsAsync(expectedUser);

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<AuthResponse>>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.NotEmpty(apiResponse.Data.Token);
        Assert.Equal(expectedUser.Email, apiResponse.Data.User.Email);
    }

    [Fact]
    public async Task Register_WeakPassword_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "weak",
            ConfirmPassword = "weak"
        };

        _mockPasswordService.Setup(x => x.IsValidPassword(request.Password))
            .Returns(false);

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.NotNull(apiResponse);
        Assert.False(apiResponse.Success);
        Assert.Contains("Password must be at least 8 characters", apiResponse.Message);
    }

    [Fact]
    public async Task Register_UserAlreadyExists_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new RegisterRequest
        {
            Email = "existing@example.com",
            Password = "StrongP@ssw0rd!",
            ConfirmPassword = "StrongP@ssw0rd!"
        };

        var existingUser = new UserInfo
        {
            Id = Guid.NewGuid().ToString(),
            Email = request.Email
        };

        _mockPasswordService.Setup(x => x.IsValidPassword(request.Password))
            .Returns(true);

        _mockUserService.Setup(x => x.GetUserByEmailAsync(request.Email))
            .ReturnsAsync(existingUser);

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.NotNull(apiResponse);
        Assert.False(apiResponse.Success);
        Assert.Contains("User with this email already exists", apiResponse.Message);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsSuccess()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "StrongP@ssw0rd!"
        };

        var expectedUser = new UserInfo
        {
            Id = Guid.NewGuid().ToString(),
            Email = request.Email,
            FirstName = "John",
            LastName = "Doe",
            IsEmailVerified = true
        };

        _mockUserService.Setup(x => x.ValidateUserCredentialsAsync(request.Email, request.Password))
            .ReturnsAsync(expectedUser);

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<AuthResponse>>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.NotEmpty(apiResponse.Data.Token);
        Assert.Equal(expectedUser.Email, apiResponse.Data.User.Email);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "WrongPassword"
        };

        _mockUserService.Setup(x => x.ValidateUserCredentialsAsync(request.Email, request.Password))
            .ReturnsAsync((UserInfo?)null);

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.NotNull(apiResponse);
        Assert.False(apiResponse.Success);
        Assert.Contains("Invalid email or password", apiResponse.Message);
    }

    [Theory]
    [InlineData("", "StrongP@ssw0rd!", "StrongP@ssw0rd!")] // Empty email
    [InlineData("invalid-email", "StrongP@ssw0rd!", "StrongP@ssw0rd!")] // Invalid email format
    [InlineData("test@example.com", "", "")] // Empty password
    [InlineData("test@example.com", "StrongP@ssw0rd!", "DifferentPassword")] // Password mismatch
    public async Task Register_InvalidInput_ReturnsBadRequest(string email, string password, string confirmPassword)
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new RegisterRequest
        {
            Email = email,
            Password = password,
            ConfirmPassword = confirmPassword
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("", "password")] // Empty email
    [InlineData("invalid-email", "password")] // Invalid email format
    [InlineData("test@example.com", "")] // Empty password
    public async Task Login_InvalidInput_ReturnsBadRequest(string email, string password)
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new LoginRequest
        {
            Email = email,
            Password = password
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}