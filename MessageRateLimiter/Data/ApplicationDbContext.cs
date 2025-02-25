using Microsoft.EntityFrameworkCore;
using MessageRateLimiter.Data.Models;

namespace MessageRateLimiter.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<MessageLogDbModel> MessageLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Additional configurations can be added here if needed
            modelBuilder.Entity<MessageLogDbModel>()
                .HasIndex(m => new { m.Timestamp, m.AccountNumber, m.PhoneNumber });

            // Add a new index specifically for cleanup operations
            modelBuilder.Entity<MessageLogDbModel>()
                .HasIndex(m => m.Timestamp)
                .HasDatabaseName("IX_MessageLogs_Timestamp_Cleanup");
        }
    }
}