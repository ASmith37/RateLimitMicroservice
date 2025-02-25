using MessageRateLimiter.Models;

namespace MessageRateLimiter.Services;

public interface IRateLimitService
{
    Task<MessageSendabilityResponse> CheckMessageSendability(string accountNumber, string phoneNumber);
    Task LogMessageAttempt(string accountNumber, string phoneNumber, MessageStatus status);
    Task<bool> CheckMessageLimit(string phoneNumber, string accountNumber);
    Task<MessageLogResponse> GetMessages(string account, DateTime? startTime = null, DateTime? endTime = null);
    Task<Dictionary<string, MessageStats>> GetMessagesByAccount(string? account = null, DateTime? startTime = null, DateTime? endTime = null);
    Task<Dictionary<string, MessageStats>> GetMessagesByPhoneNumber(string? phoneNumber = null, DateTime? startTime = null, DateTime? endTime = null);
} 