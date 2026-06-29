using System;
using System.ComponentModel.DataAnnotations; // משפיע עכשיו על כל הקובץ מלמעלה

namespace ExamAI.Admin.API.Models
{
    public class User
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string SecurityStamp { get; set; } = string.Empty;
        public Subscription? Subscription { get; set; }
    }

    public class Subscription
    {
        public string Id { get; set; } = string.Empty;
        public string PlanName { get; set; } = string.Empty;
    }

    public class AuditLog
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class SystemConfig
    {
        [Key] // עכשיו הוא יזהה את זה מעולה!
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}