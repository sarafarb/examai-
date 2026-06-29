using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using MassTransit;
using ExamAI.Shared.Contracts;
using ExamAI.Grading.Worker.Repositories; // הנחה שיש פה גישה למאגר המידע

namespace ExamAI.Export.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ExportController : ControllerBase
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IGradingRepository _repository;

    public ExportController(IPublishEndpoint publishEndpoint, IGradingRepository repository)
    {
        _publishEndpoint = publishEndpoint;
        _repository = repository;
    }

    // T-052: בקשת ייצוא עבור מבחן בודד (הבקשה מכילה את הגדרות העיצוב מה-FE)
    [HttpPost("grades/{studentGradeId}")]
    public async Task<IActionResult> RequestSingleExport(Guid studentGradeId, [FromBody] ExportRequestSettings settings)
    {
        var grade = await _repository.GetGradeByStudentExamIdAsync(studentGradeId);
        if (grade == null) return NotFound("Grade record not found.");

        if (grade.Status != "approved")
        {
            return BadRequest("Cannot export reports for unapproved grades.");
        }

        Guid jobId = Guid.NewGuid();

        // שמירה לטבלת export.export_jobs (Mock)
        // await _repository.InsertExportJobAsync(jobId, studentGradeId, "queued");

        // שליחת הודעה ל-Worker ב-RabbitMQ יחד עם הגדרות העיצוב
        await _publishEndpoint.Publish(new ExportJobMessage(
            ExportJobId: jobId,
            StudentGradeId: studentGradeId,
            TeacherId: Guid.NewGuid(), // במציאות נשלף מה-JWT
            SelectedColor: settings.PenColor,
            SelectedStyle: settings.HandwritingStyle,
            IsBatch: false
        ));

        return Accepted(new { ExportJobId = jobId, Status = "queued" });
    }

    // T-052: משיכת סטטוס עבודה
    [HttpGet("jobs/{exportJobId}/status")]
    public async Task<IActionResult> GetJobStatus(Guid exportJobId)
    {
        // בפועל ישלוף מ-DB
        return Ok(new { Status = "completed", Progress = 100 });
    }

    // T-052: יצירת קישור זמני (Presigned URL)
    [HttpGet("jobs/{exportJobId}/download")]
    public async Task<IActionResult> GetDownloadUrl(Guid exportJobId)
    {
        // יצירת קישור מדומה שפג תוקף בעוד 15 דקות
        var expiresAt = DateTime.UtcNow.AddMinutes(15);
        string mockS3PresignedUrl = $"https://examai-exports.s3.amazonaws.com/exports/{exportJobId}.pdf?X-Amz-Expires=900&token=secure";

        return Ok(new { DownloadUrl = mockS3PresignedUrl, ExpiresAt = expiresAt });
    }

    // T-052: בקשת ייצוא אצווה לכל הכיתה ב-ZIP
    [HttpPost("exams/{examId}/batch")]
    public async Task<IActionResult> RequestBatchExport(Guid examId, [FromBody] ExportRequestSettings settings)
    {
        Guid jobId = Guid.NewGuid();

        await _publishEndpoint.Publish(new ExportJobMessage(
            ExportJobId: jobId,
            StudentGradeId: Guid.Empty,
            TeacherId: Guid.NewGuid(),
            SelectedColor: settings.PenColor,
            SelectedStyle: settings.HandwritingStyle,
            IsBatch: true,
            ExamId: examId
        ));

        return Accepted(new { BatchJobId = jobId, Status = "queued" });
    }
}

// קלאס עזר לקבלת הגדרות עיצוב מהמשתמש
public class ExportRequestSettings
{
    public PenColor PenColor { get; set; } = PenColor.Red;
    public HandwritingStyle HandwritingStyle { get; set; } = HandwritingStyle.ClassicTeacher;
}