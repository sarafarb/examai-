using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using ExamAI.Admin.API.Data;
using ExamAI.Admin.API.Models;

namespace ExamAI.Admin.API.Controllers
{
    [ApiController]
    [Route("api/v1/admin/config")]
    [Authorize(Policy = "CanAccessAdmin")]
    public class AdminConfigController : ControllerBase
    {
        private readonly AdminDbContext _dbContext;
        private readonly IDistributedCache _cache;

        public AdminConfigController(AdminDbContext dbContext, IDistributedCache cache)
        {
            _dbContext = dbContext;
            _cache = cache;
        }

        // GET /api/v1/admin/config
        [HttpGet]
        public async Task<IActionResult> GetAllConfigs()
        {
            var configs = await _dbContext.SystemConfigs
                .Where(c => !c.Key.Contains("secret")) // הסתרת סודות
                .ToListAsync();
                
            return Ok(configs);
        }

        // PUT /api/v1/admin/config/{key}
        [HttpPut("{key}")]
        public async Task<IActionResult> UpdateConfig(string key, [FromBody] UpdateConfigDto request)
        {
            var config = await _dbContext.SystemConfigs.FirstOrDefaultAsync(c => c.Key == key);
            
            if (config == null)
            {
                config = new SystemConfig { Key = key };
                _dbContext.SystemConfigs.Add(config);
            }

            config.Value = request.Value;
            config.UpdatedAt = DateTime.UtcNow;
            
            // שמירת לוג אודות השינוי (Audit Log)
            _dbContext.AuditLogs.Add(new AuditLog 
            { 
                UserId = "ADMIN_USER", // בפועל יימשך מה-Token
                Action = "UPDATE_CONFIG", 
                Details = $"Updated config key: {key}" 
            });

            await _dbContext.SaveChangesAsync();

            // מחיקת ה-Cache כדי שה-Workers ימשכו את הפרומפט החדש
            await _cache.RemoveAsync($"config_{key}");

            return Ok(config);
        }
    }

    public record UpdateConfigDto(string Value);
}