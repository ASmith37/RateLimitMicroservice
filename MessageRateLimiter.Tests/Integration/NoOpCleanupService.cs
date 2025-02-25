using MessageRateLimiter.Data;
using MessageRateLimiter.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using Xunit;
using MessageRateLimiter.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Hosting;

namespace MessageRateLimiter.Tests.Integration;

public class NoOpCleanupService : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public class MessageControllerTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly ILogger<MessageControllerTests> _logger;
    private readonly IServiceScope _scope;
    private readonly ApplicationDbContext _context;

    public MessageControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Configure logging to minimize noise
                services.Configure<LoggerFilterOptions>(options =>
                {
                    options.AddFilter<ConsoleLoggerProvider>("MessageRateLimiter.Services.CleanupService", LogLevel.Warning);
                });

                // Remove all existing database registrations
                var descriptors = services.Where(d => 
                    d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                    d.ServiceType == typeof(DbContextOptions) ||
                    d.ServiceType == typeof(ApplicationDbContext) ||
                    d.ServiceType.Name.Contains("DbContext") ||
                    d.ServiceType.Name.Contains("SqliteConnection") ||
                    d.ServiceType.Name.Contains("DbContextOptions") ||
                    d.ServiceType.Name.Contains("DatabaseProvider")
                ).ToList();

                foreach (var descriptor in descriptors)
                {
                    services.Remove(descriptor);
                }

                // Configure test database with unique name
                var databaseName = $"TestDb-{Guid.NewGuid()}";
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase(databaseName);
                }, ServiceLifetime.Scoped);

                // Replace other services that depend on DbContext
                services.AddScoped<IRateLimitService, RateLimitService>();

                // Replace CleanupService with no-op version
                var cleanupDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(CleanupService));
                if (cleanupDescriptor != null)
                {
                    services.Remove(cleanupDescriptor);
                }
                services.AddHostedService<NoOpCleanupService>();
            });
        });

        _client = _factory.CreateClient();
        _scope = _factory.Services.CreateScope();
        _context = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        _logger = _scope.ServiceProvider.GetRequiredService<ILogger<MessageControllerTests>>();
    }

    [Fact]
    public async Task SendMessage_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var request = new MessageSendabilityRequest
        {
            PhoneNumber = "+1234567890",
            AccountNumber = "test-account"
        };

        try
        {
            // Act
            var response = await _client.PostAsJsonAsync("/api/message/check-sendability", request);
            var content = await response.Content.ReadAsStringAsync(); // For debugging
            
            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<MessageSendabilityResponse>();
            Assert.NotNull(result);
            Assert.True(result.CanSend);
        }
        catch (Exception ex)
        {
            throw new Exception($"Test failed with error: {ex.Message}");
        }
    }

    [Fact]
    public async Task SendMessage_ExceedsPhoneNumberLimit_ReturnsTooManyRequests()
    {
        // Arrange
        var request = new MessageSendabilityRequest
        {
            AccountNumber = "TEST123",
            PhoneNumber = "1234567890"
        };

        // Act - Send 10 messages (phone number limit)
        for (int i = 0; i < 10; i++)
        {
            var setupResponse = await _client.PostAsJsonAsync("/api/message/check-sendability", request);
            _logger.LogInformation($"Setup request {i} response: {setupResponse.StatusCode} - {await setupResponse.Content.ReadAsStringAsync()}");
            
            // Verify the message was logged and check the timestamp
            var messages = await _context.MessageLogs
                .Where(m => m.PhoneNumber == request.PhoneNumber)
                .OrderByDescending(m => m.Timestamp)
                .ToListAsync();
            
            _logger.LogInformation($"Message count after request {i}: {messages.Count}");
            foreach (var msg in messages)
            {
                _logger.LogInformation($"Message timestamp: {msg.Timestamp:O}, Status: {msg.Status}");
            }

            // Add a small delay to ensure messages are processed
            await Task.Delay(50);
        }

        // Send one more to exceed the limit
        var response = await _client.PostAsJsonAsync("/api/message/check-sendability", request);
        var content = await response.Content.ReadAsStringAsync();
        _logger.LogInformation($"Final response: {response.StatusCode} - {content}");
        
        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<MessageSendabilityResponse>();
        Assert.NotNull(result);
        Assert.False(result.CanSend);
    }

    [Fact]
    public async Task SendMessage_ExceedsAccountLimit_ReturnsTooManyRequests()
    {
        // Arrange
        await _context.Database.EnsureDeletedAsync();
        await _context.Database.EnsureCreatedAsync();

        var request = new MessageSendabilityRequest
        {
            AccountNumber = "TEST123",
            PhoneNumber = "1234567890"
        };

        // Act - Send 20 messages (account limit)
        for (int i = 0; i < 20; i++)
        {
            // Use a different phone number for each request to isolate account limit testing
            request.PhoneNumber = $"1234567{i:D3}";
            
            var setupResponse = await _client.PostAsJsonAsync("/api/message/check-sendability", request);
            _logger.LogInformation($"Setup request {i} response: {setupResponse.StatusCode} - {await setupResponse.Content.ReadAsStringAsync()}");
            
            // Verify the message was logged
            var messages = await _context.MessageLogs
                .Where(m => m.AccountNumber == request.AccountNumber)
                .OrderByDescending(m => m.Timestamp)
                .ToListAsync();
            
            _logger.LogInformation($"Message count after request {i}: {messages.Count}");
        }

        // Send one more to exceed the limit
        request.PhoneNumber = "9999999999"; // Use a different phone number for the final request
        var response = await _client.PostAsJsonAsync("/api/message/check-sendability", request);
        var content = await response.Content.ReadAsStringAsync();
        _logger.LogInformation($"Final response: {response.StatusCode} - {content}");
        
        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<MessageSendabilityResponse>();
        Assert.NotNull(result);
        Assert.False(result.CanSend);
    }

    public void Dispose()
    {
        _scope.Dispose();
    }
} 