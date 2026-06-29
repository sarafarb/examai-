using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ExamAI.Notification.Worker
{
    public class NotificationBackgroundWorker : BackgroundService
    {
        private readonly EmailService _emailService;
        private readonly ILogger<NotificationBackgroundWorker> _logger;

        public NotificationBackgroundWorker(EmailService emailService, ILogger<NotificationBackgroundWorker> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Notification Worker is running and waiting for messages...");
            
            while (!stoppingToken.IsCancellationRequested)
            {
                // כאן ה-Worker יאזין בעתיד להודעות
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}