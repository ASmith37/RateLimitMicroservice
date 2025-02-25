using MessageRateLimiter.Data;
using MessageRateLimiter.Data.Models;
using MessageRateLimiter.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Microsoft.Extensions.Logging;
using Moq;

namespace MessageRateLimiter.Tests.Services;

public class CleanupServiceTests
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<CleanupService>> _loggerMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly CleanupService _service;

    public CleanupServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _loggerMock = new Mock<ILogger<CleanupService>>();
        _serviceProviderMock = new Mock<IServiceProvider>();

        // Setup service provider to return the DbContext
        var scope = new Mock<IServiceScope>();
        scope.Setup(s => s.ServiceProvider.GetService(typeof(ApplicationDbContext)))
            .Returns(_context);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(f => f.CreateScope())
            .Returns(scope.Object);

        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
            .Returns(scopeFactory.Object);
        // GetRequiredService will fall back to GetService if the service exists
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(ApplicationDbContext)))
            .Returns(_context);

        _service = new CleanupService(_loggerMock.Object, _serviceProviderMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_RemovesOldMessages()
    {
        try
        {
            // Arrange
            var oldDate = DateTime.UtcNow.AddHours(-2);
            _context.MessageLogs.Add(new MessageLogDbModel
            {
                PhoneNumber = "+1234567890",
                AccountNumber = "test-account",
                Timestamp = oldDate,
                Status = MessageStatus.Accepted
            });
            await _context.SaveChangesAsync();

            // Act
            await _service.StartAsync(CancellationToken.None);
            
            // Force cleanup to run immediately by invoking DoCleanup directly
            var cleanupMethod = typeof(CleanupService)
                .GetMethod("DoCleanup", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (cleanupMethod == null)
            {
                throw new Exception("Could not find DoCleanup method");
            }

            // Invoke cleanup
            cleanupMethod.Invoke(_service, new object?[] { null });
            await Task.Delay(100); // Short delay to allow the cleanup to finish

            // Assert
            var remainingMessages = await _context.MessageLogs.CountAsync();
            Assert.Equal(0, remainingMessages);

            // Verify logger was called with any cleanup message
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Cleanup")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }
        finally
        {
            await _service.StopAsync(CancellationToken.None);
            (_service as IDisposable)?.Dispose();
        }
    }
} 