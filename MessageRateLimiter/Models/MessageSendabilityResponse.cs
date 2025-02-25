namespace MessageRateLimiter.Models;

public class MessageSendabilityResponse
{
    public bool CanSend { get; set; }
    public RateLimitInfo? ExceededLimit { get; set; }
}

public class RateLimitInfo
{
    public RateLimitType Type { get; set; }
    public int Limit { get; set; }
    public int CurrentCount { get; set; }
}

public enum RateLimitType
{
    PhoneNumber,
    Account
} 