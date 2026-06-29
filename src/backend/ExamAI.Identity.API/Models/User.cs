using System.ComponentModel.DataAnnotations.Schema;

namespace ExamAI.Identity.API.Models;

[Table("users", Schema = "identity")]
public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string? PasswordHash { get; set; } 
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool EmailVerified { get; set; } = false;
    public bool IsDeleted { get; set; } = false;
    public bool IsSuspended { get; set; } = false;
    public string? GoogleId { get; set; } 
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // --- שדות חדשים שנוספו עבור משימת GDPR (T-064) ---
    public string? Status { get; set; }
    public DateTime? DeletionScheduledAt { get; set; }
    public string? DeletionReason { get; set; }
    public bool MarketingConsent { get; set; }
}

// --- מחלקה חדשה שנוספה עבור תיעוד פעולות (Audit Logs) ---
[Table("audit_logs", Schema = "identity")]
public class AuditLog
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}