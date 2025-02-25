using MessageRateLimiter.Models;

namespace MessageRateLimiter.Services;

public interface IRateLimitService
{
    Task<MessageSendabilityResponse> CheckMessageSendability(string accountNumber, string phoneNumber);
    Task LogMessageAttempt(string accountNumber, string phoneNumber, MessageStatus status);
    Task<bool> CheckMessageLimit(string phoneNumber, string accountNumber);
} 