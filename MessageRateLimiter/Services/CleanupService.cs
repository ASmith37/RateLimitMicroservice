using MessageRateLimiter.Data;
using Microsoft.EntityFrameworkCore;

namespace MessageRateLimiter.Services
{
    public class CleanupService : IHostedService, IDisposable
    {
        private readonly ILogger<CleanupService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private Timer? _timer;

        public CleanupService(
            ILogger<CleanupService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Cleanup Service is starting.");

            _timer = new Timer(DoCleanup, null, TimeSpan.Zero, 
                TimeSpan.FromHours(1));

            return Task.CompletedTask;
        }

        private async void DoCleanup(object? state)
        {
            _logger.LogInformation("Executing cleanup task");

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();

                var cutoffTime = DateTime.UtcNow.AddHours(-1);
                
                // Get old messages
                var oldMessages = await dbContext.MessageLogs
                    .Where(m => m.Timestamp < cutoffTime)
                    .ToListAsync();

                // Remove them
                dbContext.MessageLogs.RemoveRange(oldMessages);
                await dbContext.SaveChangesAsync();

                _logger.LogInformation(
                    "Cleanup completed. Deleted {DeletedCount} old records", 
                    oldMessages.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during cleanup");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Cleanup Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
} 