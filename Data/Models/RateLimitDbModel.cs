using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace MessageRateLimiter.Data.Models
{
    [Index(nameof(AccountNumber), nameof(PhoneNumber))]
    public class RateLimitDbModel
    {
        [Key]
        public int Id { get; set; }
        
        public string? AccountNumber { get; set; }
        
        public string? PhoneNumber { get; set; }
        
        [Required]
        public int MessagesPerSecond { get; set; }
    }
} 