public enum MessageStatus
{
    Accepted,
    Rejected
}

public class MessageLog
{
    public int Id { get; set; }
    public required string AccountNumber { get; set; }
    public required string PhoneNumber { get; set; }
    public required DateTime Timestamp { get; set; }
    public required MessageStatus Status { get; set; }
} 