using Microsoft.EntityFrameworkCore;
using ExamAI.Admin.API.Models;

namespace ExamAI.Admin.API.Data
{
    public class AdminDbContext : DbContext
    {
        public AdminDbContext(DbContextOptions<AdminDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<SystemConfig> SystemConfigs { get; set; }
}
    }
    