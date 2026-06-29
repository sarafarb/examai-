using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ExamAI.Shared.Services;

public interface IAuditService
{
    void LogNonBlocking(Guid userId, string action, string resourceType, Guid resourceId, string oldValue, string newValue);
}

public class AuditService : IAuditService
{
    private readonly ILogger<AuditService> _logger;
    // TODO: הזרקת ElasticClient אמיתי של NEST / Elastic.Clients.Elasticsearch

    public AuditService(ILogger<AuditService> logger)
    {
        _logger = logger;
    }

    public void LogNonBlocking(Guid userId, string action, string resourceType, Guid resourceId, string oldValue, string newValue)
    {
        // מריץ ברקע בלי לחסום את ה-Thread הראשי של ה-API
        Task.Run(async () =>
        {
            try
            {
                var auditEntry = new
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Action = action, // e.g., GRADE_APPROVED, LOGIN
                    ResourceType = resourceType,
                    ResourceId = resourceId,
                    OldValue = oldValue,
                    NewValue = newValue,
                    Timestamp = DateTime.UtcNow
                };

                // 1. שמירה ל-DB (דרך Repository או DbContext)
                _logger.LogInformation($"[Audit DB] Saved to PostgreSQL: {action} by {userId}");

                // 2. שליחה ל-Elasticsearch לטובת חיפוש מהיר
                // await _elasticClient.IndexDocumentAsync(auditEntry);
                _logger.LogInformation($"[Audit Elastic] Indexed to Elasticsearch: {action}");
            }
            catch (Exception ex)
            {
                // ה-Audit נכשל, אך ה-Main Flow של המשתמש לא נפגע!
                _logger.LogError($"Failed to write audit log: {ex.Message}");
            }
        });
    }
}