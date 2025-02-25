public enum RateLimitType
{
    PhoneNumber,
    Account,
}

public class RateLimit
{
    public int Id { get; set; }
    public string? AccountNumber { get; set; }
    public string? PhoneNumber { get; set; }
    public int MessagesPerSecond { get; set; }
    public RateLimitType Type { get; set; }
}

public class RateLimitInfo
{
    public required RateLimitType Type { get; set; }
    public required int Limit { get; set; }
    public required int CurrentCount { get; set; }
} 