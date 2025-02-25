using MessageRateLimiter.Data;
using MessageRateLimiter.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Microsoft.Extensions.Logging;
using Moq;

namespace MessageRateLimiter.Tests.Services;

public class RateLimitServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly RateLimitService _service;
    private readonly Mock<ILogger<RateLimitService>> _loggerMock;

    public RateLimitServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _loggerMock = new Mock<ILogger<RateLimitService>>();
        _service = new RateLimitService(_context, _loggerMock.Object);
    }

    [Fact]
    public async Task IsAllowed_UnderLimit_ReturnsTrue()
    {
        // Arrange
        var accountNumber = "TEST123";
        var phoneNumber = "1234567890";

        // Act
        var result = await _service.CheckMessageSendability(accountNumber, phoneNumber);

        // Assert
        Assert.True(result.CanSend);
    }

    [Fact]
    public async Task IsAllowed_OverPhoneLimit_ReturnsFalse()
    {
        // Arrange
        var accountNumber = "TEST123";
        var phoneNumber = "1234567890";

        // Send 10 messages (phone limit)
        for (int i = 0; i < 10; i++)
        {
            await _service.CheckMessageSendability(accountNumber, phoneNumber);
        }

        // Act
        var result = await _service.CheckMessageSendability(accountNumber, phoneNumber);

        // Assert
        Assert.False(result.CanSend);
    }

    [Fact]
    public async Task IsAllowed_OverAccountLimit_ReturnsFalse()
    {
        // Arrange
        var accountNumber = "TEST123";
        var phoneNumber = "1234567890";

        // Send 20 messages (account limit)
        for (int i = 0; i < 20; i++)
        {
            await _service.CheckMessageSendability(accountNumber, phoneNumber);
        }

        // Act
        var result = await _service.CheckMessageSendability(accountNumber, phoneNumber);

        // Assert
        Assert.False(result.CanSend);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
} 