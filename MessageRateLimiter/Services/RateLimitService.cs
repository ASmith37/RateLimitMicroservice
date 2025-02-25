using MessageRateLimiter.Data;
using MessageRateLimiter.Models;
using Microsoft.EntityFrameworkCore;
using MessageRateLimiter.Data.Models;

namespace MessageRateLimiter.Services;


public class RateLimitService : IRateLimitService
{
    private const int ACCOUNT_RATE_LIMIT = 20;
    private const int PHONE_RATE_LIMIT = 10;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RateLimitService> _logger;


    public RateLimitService(
        ApplicationDbContext context,
        ILogger<RateLimitService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<MessageSendabilityResponse> CheckMessageSendability(string accountNumber, string phoneNumber)
    {
        // Check phone number rate limit
        var phoneCount = await GetMessageCountByPhoneNumber(phoneNumber);
        if (phoneCount >= PHONE_RATE_LIMIT)
        {
            _logger.LogInformation(
                "Phone number {PhoneNumber} has exceeded rate limit. Count: {Count}", 
                phoneNumber, phoneCount);

            await LogMessageAttempt(accountNumber, phoneNumber, MessageStatus.Rejected);
            return new MessageSendabilityResponse
            {
                CanSend = false,
                ExceededLimit = new MessageRateLimiter.Models.RateLimitInfo
                {
                    Type = MessageRateLimiter.Models.RateLimitType.PhoneNumber,
                    Limit = PHONE_RATE_LIMIT,
                    CurrentCount = phoneCount
                }
            };
        }

        // Check account rate limit
        var accountCount = await GetMessageCountByAccount(accountNumber);
        if (accountCount >= ACCOUNT_RATE_LIMIT)
        {
            _logger.LogInformation(
                "Account {AccountNumber} has exceeded rate limit. Count: {Count}", 
                accountNumber, accountCount);

            await LogMessageAttempt(accountNumber, phoneNumber, MessageStatus.Rejected);
            return new MessageSendabilityResponse
            {
                CanSend = false,
                ExceededLimit = new MessageRateLimiter.Models.RateLimitInfo
                {
                    Type = MessageRateLimiter.Models.RateLimitType.Account,
                    Limit = ACCOUNT_RATE_LIMIT,
                    CurrentCount = accountCount
                }
            };
        }

        // Log the attempt before checking if we can send
        await LogMessageAttempt(accountNumber, phoneNumber, MessageStatus.Accepted);

        // Check limits again after logging the attempt
        phoneCount = await GetMessageCountByPhoneNumber(phoneNumber);
        accountCount = await GetMessageCountByAccount(accountNumber);

        if (phoneCount > PHONE_RATE_LIMIT)
        {
            return new MessageSendabilityResponse
            {
                CanSend = false,
                ExceededLimit = new MessageRateLimiter.Models.RateLimitInfo
                {
                    Type = MessageRateLimiter.Models.RateLimitType.PhoneNumber,
                    Limit = PHONE_RATE_LIMIT,
                    CurrentCount = phoneCount
                }
            };
        }

        if (accountCount > ACCOUNT_RATE_LIMIT)
        {
            return new MessageSendabilityResponse
            {
                CanSend = false,
                ExceededLimit = new MessageRateLimiter.Models.RateLimitInfo
                {
                    Type = MessageRateLimiter.Models.RateLimitType.Account,
                    Limit = ACCOUNT_RATE_LIMIT,
                    CurrentCount = accountCount
                }
            };
        }

        return new MessageSendabilityResponse
        {
            CanSend = true,
            ExceededLimit = null
        };
    }

    private async Task<int> GetMessageCountByAccount(string account)
    {
        var oneSecondAgo = DateTime.UtcNow.AddSeconds(-1);
        return await _context.MessageLogs
            .CountAsync(m => m.AccountNumber == account && 
                            m.Timestamp >= oneSecondAgo && 
                            m.Status == MessageStatus.Accepted);
    }

    private async Task<int> GetMessageCountByPhoneNumber(string phoneNumber)
    {
        var oneSecondAgo = DateTime.UtcNow.AddSeconds(-1);
        return await _context.MessageLogs
            .CountAsync(m => m.PhoneNumber == phoneNumber && 
                            m.Timestamp >= oneSecondAgo && 
                            m.Status == MessageStatus.Accepted);
    }


    public async Task LogMessageAttempt(string accountNumber, string phoneNumber, MessageStatus status)
    {
        var logEntry = new MessageLogDbModel
        {
            AccountNumber = accountNumber,
            PhoneNumber = phoneNumber,
            Timestamp = DateTime.UtcNow,
            Status = status
        };
        
        _context.MessageLogs.Add(logEntry);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> CheckMessageLimit(string phoneNumber, string accountNumber)
    {
        var oneSecondAgo = DateTime.UtcNow.AddSeconds(-1);

        // Check phone number limit
        var phoneNumberCount = await _context.MessageLogs.CountAsync(m =>
            m.PhoneNumber == phoneNumber &&
            m.Status == MessageStatus.Accepted &&
            m.Timestamp >= oneSecondAgo);

        if (phoneNumberCount >= PHONE_RATE_LIMIT)
        {
            _logger.LogInformation(
                "Phone number {PhoneNumber} has exceeded rate limit. Count: {Count}", 
                phoneNumber, phoneNumberCount);
            return false;
        }

        // Check account limit
        var accountCount = await _context.MessageLogs.CountAsync(m =>
            m.AccountNumber == accountNumber &&
            m.Status == MessageStatus.Accepted &&
            m.Timestamp >= oneSecondAgo);

        if (accountCount >= ACCOUNT_RATE_LIMIT)
        {
            _logger.LogInformation(
                "Account {AccountNumber} has exceeded rate limit. Count: {Count}", 
                accountNumber, accountCount);
            return false;
        }

        return true;
    }
} 