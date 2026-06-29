using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ExamAI.Admin.API.Data;
using ExamAI.Admin.API.Models;
using System.Linq;
using System.Threading.Tasks;

namespace ExamAI.Admin.API.Controllers
{
    [ApiController]
    [Route("api/v1/admin/users")]
    [Authorize(Policy = "CanAccessAdmin")]
    public class AdminUsersController : ControllerBase
    {
        private readonly AdminDbContext _dbContext;

        public AdminUsersController(AdminDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // GET /api/v1/admin/users
        [HttpGet]
        public async Task<IActionResult> GetUsers([FromQuery] string? search, [FromQuery] string? role, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var query = _dbContext.Users.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u => u.Email.Contains(search) || u.Name.Contains(search));
            }

            if (!string.IsNullOrEmpty(role))
            {
                query = query.Where(u => u.Role == role);
            }

            var totalCount = await query.CountAsync();
            var users = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new { u.Id, u.Name, u.Email, u.Role, u.Status })
                .ToListAsync();

            return Ok(new { totalCount, page, pageSize, data = users });
        }

        // GET /api/v1/admin/users/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(string id)
        {
            var user = await _dbContext.Users
                .Include(u => u.Subscription)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return NotFound();

            var auditLogs = await _dbContext.AuditLogs
                .Where(a => a.UserId == id)
                .OrderByDescending(a => a.CreatedAt)
                .Take(10)
                .ToListAsync();

            return Ok(new { user, recentLogs = auditLogs });
        }

        // PUT /api/v1/admin/users/{id}/status
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateUserStatus(string id, [FromBody] UpdateStatusDto request)
        {
            var user = await _dbContext.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.Status = request.Status;

            if (request.Status == "suspended")
            {
                // כאן נוסיף לוגיקה לביטול סשנים (למשל, עדכון token_version או מחיקת Refresh Tokens מה-DB)
                user.SecurityStamp = Guid.NewGuid().ToString(); // Revokes JWTs if using Identity
            }

            _dbContext.AuditLogs.Add(new AuditLog { UserId = id, Action = $"Status changed to {request.Status}", Reason = request.Reason });
            await _dbContext.SaveChangesAsync();

            return Ok();
        }

        // PUT /api/v1/admin/users/{id}/role
        [HttpPut("{id}/role")]
        public async Task<IActionResult> UpdateUserRole(string id, [FromBody] UpdateRoleDto request)
        {
            var user = await _dbContext.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.Role = request.Role;
            await _dbContext.SaveChangesAsync();

            return Ok();
        }

        // DELETE /api/v1/admin/users/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> HardDeleteUser(string id)
        {
            var user = await _dbContext.Users.FindAsync(id);
            if (user == null) return NotFound();

            // GDPR Compliance: מחיקה מוחלטת של המשתמש
            _dbContext.Users.Remove(user);

            // אנונימיזציה של הלוגים (שמירת הפעולות בלי המזהה האישי)
            var userLogs = await _dbContext.AuditLogs.Where(a => a.UserId == id).ToListAsync();
            foreach(var log in userLogs)
            {
                log.UserId = "DELETED_USER";
                log.Details = "Anonymized for GDPR";
            }

            await _dbContext.SaveChangesAsync();
            return NoContent();
        }
    }

    public record UpdateStatusDto(string Status, string Reason);
    public record UpdateRoleDto(string Role);
}