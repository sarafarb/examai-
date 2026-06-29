using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using ExamAI.Analytics.API.Services;
using ExamAI.Analytics.API.Models;
using ExamAI.Analytics.API.Data;

namespace ExamAI.Analytics.API.Controllers
{
    [ApiController]
    [Route("api/v1/analytics/exams")]
    public class AnalyticsController : ControllerBase
    {
        private readonly IDistributedCache _cache;
        private readonly AnalyticsDbContext _dbContext;
        private readonly GptRecommendationsService _gptService;

        public AnalyticsController(
            IDistributedCache cache, 
            AnalyticsDbContext dbContext,
            GptRecommendationsService gptService)
        {
            _cache = cache;
            _dbContext = dbContext;
            _gptService = gptService;
        }

        // GET /api/v1/analytics/exams/{examId}
        [HttpGet("{examId}")]
        public async Task<IActionResult> GetExamAnalytics(string examId)
        {
            string cacheKey = $"analytics:exam:{examId}";
            var cachedData = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedData))
            {
                return Content(cachedData, "application/json"); // מחזיר מ-Redis
            }

            var analytics = await _dbContext.ExamAnalytics.FindAsync(examId);
            if (analytics == null) return NotFound();

            // המרה ל-JSON ושמירה ב-Redis ל-5 דקות
            var responseData = JsonSerializer.Serialize(analytics);
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            };
            await _cache.SetStringAsync(cacheKey, responseData, cacheOptions);

            return Ok(analytics);
        }

        // GET /api/v1/analytics/exams/{examId}/recommendations
        [HttpGet("{examId}/recommendations")]
        public async Task<IActionResult> GetRecommendations(string examId)
        {
            string cacheKey = $"analytics:recommendations:{examId}";
            var cachedRecommendations = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedRecommendations))
            {
                return Ok(new { recommendations = cachedRecommendations });
            }

            var analytics = await _dbContext.ExamAnalytics.FindAsync(examId);
            if (analytics == null) return NotFound("Analytics not found for this exam.");

            // פנייה ל-GPT
            var recommendations = await _gptService.GetRecommendationsAsync(analytics);

            // שמירה ב-Redis לשעה שלמה (פעולה יקרה)
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            };
            await _cache.SetStringAsync(cacheKey, recommendations, cacheOptions);

            return Ok(new { recommendations });
        }
    }
}