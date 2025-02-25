using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace MessageRateLimiter.Data.Models
{
    [Index(nameof(Timestamp), nameof(AccountNumber), nameof(PhoneNumber))]
    public class MessageLogDbModel
    {
        [Key]
        public int Id { get; set; }
        
        public required DateTime Timestamp { get; set; }
        
        [Required]
        public required string AccountNumber { get; set; }
        
        [Required]
        public required string PhoneNumber { get; set; }

        public MessageStatus Status { get; set; }
    }
} 