using ExamAI.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;
using ExamAI.Identity.API.Data;
using ExamAI.Identity.API.Models;
namespace ExamAI.Identity.API.Services
{
    public interface IGdprService
    {
        Task ScheduleDataExportAsync(Guid userId);
        Task<bool> ScheduleAccountDeletionAsync(Guid userId, string confirmEmail, string? reason);
        Task UpdateMarketingConsentAsync(Guid userId, bool consent);
    }

    public class GdprService : IGdprService
    {
        private readonly IdentityDbContext _dbContext;
        private readonly IMessagePublisher _messagePublisher; // RabbitMQ Publisher

        public GdprService(IdentityDbContext dbContext, IMessagePublisher messagePublisher)
        {
            _dbContext = dbContext;
            _messagePublisher = messagePublisher;
        }

        public async Task ScheduleDataExportAsync(Guid userId)
        {
            // Publish event to trigger the worker
            await _messagePublisher.PublishAsync("gdpr.events", "gdpr.export.requested", new 
            { 
                UserId = userId,
                RequestedAt = DateTime.UtcNow
            });
        }

        public async Task<bool> ScheduleAccountDeletionAsync(Guid userId, string confirmEmail, string? reason)
        {
            var user = await _dbContext.Users.FindAsync(userId);
            
            if (user == null || !user.Email.Equals(confirmEmail, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // עדכון סטטוס המשתמש למחיקה מתוזמנת
            user.Status = "deletion_scheduled";
            user.DeletionScheduledAt = DateTime.UtcNow.AddDays(30); // 30-day grace period
            user.DeletionReason = reason;

            await _dbContext.SaveChangesAsync();

            // שליחת אימייל למשתמש על התחלת תהליך המחיקה
            await _messagePublisher.PublishAsync("gdpr.events", "gdpr.deletion.scheduled", new 
            { 
                UserId = userId,
                Email = user.Email,
                ScheduledDate = user.DeletionScheduledAt
            });

            return true;
        }

        public async Task UpdateMarketingConsentAsync(Guid userId, bool consent)
        {
            var user = await _dbContext.Users.FindAsync(userId);
            if (user != null)
            {
                user.MarketingConsent = consent;
                
                // הוספת Audit Log (מניחים שיש Interceptor שעושה את זה אוטומטית לפי ה-Epic, אבל הנה הדרך הידנית)
                _dbContext.AuditLogs.Add(new AuditLog 
                {
                    UserId = userId,
                    Action = "UpdateConsent",
                    Details = $"Marketing consent changed to {consent}",
                    CreatedAt = DateTime.UtcNow
                });

                await _dbContext.SaveChangesAsync();
            }
        }
    }
}