using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ExamAI.Shared.Application;
using System.ComponentModel.DataAnnotations;
using ExamAI.Identity.API.Services;
using ICurrentUserService = ExamAI.Shared.Application.ICurrentUserService; // <-- השורה שפותרת את ההתנגשות
namespace ExamAI.Identity.API.Controllers
{
    [ApiController]
    [Route("api/v1/gdpr")]
    [Authorize] // חובה משתמש מחובר
    public class GdprController : ControllerBase
    {
        private readonly IGdprService _gdprService;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<GdprController> _logger;

        public GdprController(IGdprService gdprService, ICurrentUserService currentUser, ILogger<GdprController> logger)
        {
            _gdprService = gdprService;
            _currentUser = currentUser;
            _logger = logger;
        }

        [HttpPost("export-my-data")]
        public async Task<IActionResult> ExportMyData()
        {
            var userId = _currentUser.UserId;
            if (userId == Guid.Empty) return Unauthorized();

            // שולח משימה לרקע (למשל דרך RabbitMQ) לאיסוף כל המידע ויצירת קובץ ZIP/JSON
            await _gdprService.ScheduleDataExportAsync(userId);
            
            _logger.LogInformation("Data export scheduled for user {UserId}", userId);

            // 202 Accepted כי זה קורה ברקע
            return Accepted(new { Message = "Your data export has been scheduled. You will receive an email with a download link within 48 hours." });
        }

        [HttpDelete("delete-my-account")]
        public async Task<IActionResult> DeleteMyAccount([FromBody] DeleteAccountRequest request)
        {
            var userId = _currentUser.UserId;
            if (userId == Guid.Empty) return Unauthorized();

            var success = await _gdprService.ScheduleAccountDeletionAsync(userId, request.ConfirmEmail, request.Reason);
            
            if (!success)
            {
                return BadRequest(new { Error = "Email confirmation does not match or account cannot be deleted." });
            }

            _logger.LogWarning("Account deletion scheduled for user {UserId}. 30-day grace period started.", userId);

            return Ok(new { Message = "Account deletion scheduled. You have a 30-day grace period to cancel this action." });
        }

        [HttpPut("consent")]
        public async Task<IActionResult> UpdateConsent([FromBody] UpdateConsentRequest request)
        {
            var userId = _currentUser.UserId;
            if (userId == Guid.Empty) return Unauthorized();

            await _gdprService.UpdateMarketingConsentAsync(userId, request.MarketingConsent);
            
            _logger.LogInformation("Marketing consent updated for user {UserId} to {Consent}", userId, request.MarketingConsent);

            return Ok(new { Message = "Privacy settings updated successfully." });
        }
    }

    // --- DTOs ---

    public class DeleteAccountRequest
    {
        [Required]
        [EmailAddress]
        public string ConfirmEmail { get; set; } = string.Empty;
        public string? Reason { get; set; }
    }

    public class UpdateConsentRequest
    {
        [Required]
        public bool MarketingConsent { get; set; }
    }
}