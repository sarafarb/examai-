using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ExamAI.Admin.API.Data;

namespace ExamAI.Admin.API.Controllers
{
    [ApiController]
    [Route("api/v1/admin")]
    [Authorize(Policy = "CanAccessAdmin")]
    public class AdminLogsController : ControllerBase
    {
        private readonly AdminDbContext _dbContext;

        public AdminLogsController(AdminDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // GET /api/v1/admin/audit-logs
        [HttpGet("audit-logs")]
        public async Task<IActionResult> GetAuditLogs([FromQuery] string? action, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            var query = _dbContext.AuditLogs.AsQueryable();

            if (!string.IsNullOrEmpty(action))
            {
                query = query.Where(a => a.Action == action);
            }

            var logs = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new { page, pageSize, data = logs });
        }

        // GET /api/v1/admin/metrics/dashboard
        [HttpGet("metrics/dashboard")]
        public async Task<IActionResult> GetDashboardMetrics()
        {
            // שליפת נתונים בסיסיים לדשבורד
            var totalUsers = await _dbContext.Users.CountAsync();
            var activeUsers = await _dbContext.Users.Where(u => u.Status == "active").CountAsync();
            
            // במערכת אמיתית נמשוך מ-Elasticsearch או שירות חיצוני, כרגע אנחנו מרכזים נתונים
            var metrics = new
            {
                TotalUsers = totalUsers,
                ActiveUsers30Days = activeUsers,
                TotalExamsMonth = 1250, // Mock data עד לחיבור ל-Exam DB
                PagesProcessedMonth = 4500,
                RevenueMonth = 15400.50,
                ErrorRate = "1.2%",
                AverageOcrConfidence = "92.5%",
                AverageGradingConfidence = "88.3%"
            };

            return Ok(metrics);
        }
    }
}