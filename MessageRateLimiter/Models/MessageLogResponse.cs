namespace MessageRateLimiter.Models
{
    public class MessageLogResponse
    {
        public required string AccountNumber { get; set; }
        public required DateTime StartTime { get; set; }
        public required DateTime EndTime { get; set; }
        public required List<MessageLog> AcceptedMessages { get; set; }
        public required List<MessageLog> RejectedMessages { get; set; }
    }
} 