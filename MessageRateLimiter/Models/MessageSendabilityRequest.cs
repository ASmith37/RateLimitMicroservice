namespace MessageRateLimiter.Models;

public class MessageSendabilityRequest
{
    public string AccountNumber { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
} 