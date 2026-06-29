using Microsoft.EntityFrameworkCore;
using ExamAI.Identity.API.Models;

namespace ExamAI.Identity.API.Data;

public class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<EmailVerification> EmailVerifications => Set<EmailVerification>();
    public DbSet<UserSession> Sessions => Set<UserSession>();
    public DbSet<PasswordReset> PasswordResets => Set<PasswordReset>();
    
    // --- טבלה חדשה שנוספה עבור משימת GDPR (T-064) ---
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
}