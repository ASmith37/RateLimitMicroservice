namespace MessageRateLimiter.Models;

public class MessageSendabilityRequest
{
    public string? AccountNumber { get; set; }
    public string? PhoneNumber { get; set; }
}

public class MessageSendabilityResponse
{
    public bool CanSend { get; set; }
    public string? ExceededLimit { get; set; }
} 