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

    public async Task<MessageLogResponse> GetMessages(string account, DateTime? startTime = null, DateTime? endTime = null)
    {
        var timeRange = startTime.HasValue
            ? (Start: startTime.Value.ToUniversalTime(), End: endTime!.Value.ToUniversalTime())
            : (Start: DateTime.UtcNow.AddSeconds(-1), End: DateTime.UtcNow);

        var messages = await _context.MessageLogs
            .Where(m => m.AccountNumber == account && 
                        m.Timestamp >= timeRange.Start && 
                        m.Timestamp <= timeRange.End)
            .ToListAsync();

        return new MessageLogResponse
        {
            AccountNumber = account,
            StartTime = timeRange.Start,
            EndTime = timeRange.End,
            AcceptedMessages = messages.Where(m => m.Status == MessageStatus.Accepted)
                .Select(m => new MessageLog
                {
                    Id = m.Id,
                    AccountNumber = m.AccountNumber,
                    PhoneNumber = m.PhoneNumber,
                    Timestamp = m.Timestamp,
                    Status = m.Status
                }).ToList(),
            RejectedMessages = messages.Where(m => m.Status == MessageStatus.Rejected)
                .Select(m => new MessageLog
                {
                    Id = m.Id,
                    AccountNumber = m.AccountNumber,
                    PhoneNumber = m.PhoneNumber,
                    Timestamp = m.Timestamp,
                    Status = m.Status
                }).ToList()
        };
    }

    public async Task<Dictionary<string, MessageStats>> GetMessagesByAccount(
        string? account = null, 
        DateTime? startTime = null, 
        DateTime? endTime = null)
    {
        var timeRange = startTime.HasValue
            ? (Start: startTime.Value.ToUniversalTime(), End: endTime!.Value.ToUniversalTime())
            : (Start: DateTime.UtcNow.AddSeconds(-1), End: DateTime.UtcNow);

        var query = _context.MessageLogs
            .Where(m => m.Timestamp >= timeRange.Start && m.Timestamp <= timeRange.End);

        if (account != null)
        {
            query = query.Where(m => m.AccountNumber == account);
        }

        var stats = await query
            .GroupBy(m => m.AccountNumber)
            .Select(g => new
            {
                AccountNumber = g.Key,
                Stats = new MessageStats
                {
                    AcceptedCount = g.Count(m => m.Status == MessageStatus.Accepted),
                    RejectedCount = g.Count(m => m.Status == MessageStatus.Rejected)
                }
            })
            .ToDictionaryAsync(x => x.AccountNumber, x => x.Stats);

        return stats;
    }

    public async Task<Dictionary<string, MessageStats>> GetMessagesByPhoneNumber(
        string? phoneNumber = null, 
        DateTime? startTime = null, 
        DateTime? endTime = null)
    {
        var timeRange = startTime.HasValue
            ? (Start: startTime.Value.ToUniversalTime(), End: endTime!.Value.ToUniversalTime())
            : (Start: DateTime.UtcNow.AddSeconds(-1), End: DateTime.UtcNow);

        var query = _context.MessageLogs
            .Where(m => m.Timestamp >= timeRange.Start && m.Timestamp <= timeRange.End);

        if (phoneNumber != null)
        {
            query = query.Where(m => m.PhoneNumber == phoneNumber);
        }

        var stats = await query
            .GroupBy(m => m.PhoneNumber)
            .Select(g => new
            {
                PhoneNumber = g.Key,
                Stats = new MessageStats
                {
                    AcceptedCount = g.Count(m => m.Status == MessageStatus.Accepted),
                    RejectedCount = g.Count(m => m.Status == MessageStatus.Rejected)
                }
            })
            .ToDictionaryAsync(x => x.PhoneNumber, x => x.Stats);

        return stats;
    }
} 