using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExamAI.Grading.Worker.Repositories;
using ExamAI.Grading.Worker.Services;
using ExamAI.Shared.Services; // הוספנו את ה-namespace של ה-Audit

namespace ExamAI.Grading.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class GradesController : ControllerBase
{
    private readonly IGradingRepository _repository;
    private readonly IScoreCalculationService _scoreService;
    private readonly IAuditService _auditService; // הזרקת ה-Audit Service (T-048)

    public GradesController(
        IGradingRepository repository, 
        IScoreCalculationService scoreService, 
        IAuditService auditService)
    {
        _repository = repository;
        _scoreService = scoreService;
        _auditService = auditService;
    }

    // --- משימה T-046: שליפת ציון של תלמיד בודד ---
    [HttpGet("{studentExamId}")]
    public async Task<IActionResult> GetStudentGrade(Guid studentExamId)
    {
        var grade = await _repository.GetGradeByStudentExamIdAsync(studentExamId);
        if (grade == null) return NotFound("Student grade not found.");
        
        var response = new {
            GradeData = grade,
            PresignedExamImageUrls = new List<string> { $"https://storage.examai.com/exams/{grade.ExamId}/page1.jpg?token=mock_presigned" }
        };

        return Ok(response);
    }

    // --- משימה T-046: שליפת כל הציונים של מבחן עם סינון סטטוס ---
    [HttpGet("exams/{examId}")]
    public async Task<IActionResult> GetExamGrades(Guid examId, [FromQuery] string? status)
    {
        var grades = await _repository.GetGradesByExamIdAsync(examId, status);
        return Ok(grades);
    }

    // --- משימה T-047: דריסת ציון של שאלה על ידי המורה (Override) ---
    [HttpPut("questions/{questionGradeId}/override")]
    public async Task<IActionResult> OverrideQuestionGrade(Guid questionGradeId, [FromBody] OverrideRequest request)
    {
        var questionGrade = await _repository.GetQuestionGradeByIdAsync(questionGradeId);
        if (questionGrade == null) return NotFound("Question grade not found.");

        if (request.NewScore < 0 || request.NewScore > questionGrade.MaxPoints)
        {
            return BadRequest($"Invalid score. Must be between 0 and {questionGrade.MaxPoints}.");
        }

        Guid currentTeacherId = GetCurrentTeacherId();
        var studentGrade = await _repository.GetGradeByStudentExamIdAsync(questionGrade.StudentGradeId);
        if (studentGrade != null)
        {
            bool isOwner = await _repository.IsTeacherOwnerOfExamAsync(currentTeacherId, studentGrade.ExamId);
            if (!isOwner) return Forbid("You do not have permission to override this exam.");
        }

        var teacherOverride = new TeacherOverrideEntity
        {
            QuestionGradeId = questionGradeId,
            OldScore = questionGrade.FinalScore,
            NewScore = request.NewScore,
            Comment = request.Comment
        };
        await _repository.SaveTeacherOverrideAsync(teacherOverride);

        decimal oldScore = questionGrade.FinalScore;
        await _repository.UpdateQuestionScoreAsync(questionGradeId, request.NewScore);

        if (studentGrade != null)
        {
            var updatedQuestionGrades = (await _repository.GetGradeByStudentExamIdAsync(questionGrade.StudentGradeId))?.QuestionGrades;
            if (updatedQuestionGrades != null)
            {
                var pairs = new List<(decimal earned, decimal max)>();
                foreach (var q in updatedQuestionGrades) pairs.Add((q.FinalScore, q.MaxPoints));

                var totalResult = _scoreService.CalculateTotalScore(pairs);
                studentGrade.TotalScore = totalResult.TotalEarned;
                studentGrade.Percentage = _scoreService.CalculatePercentage(totalResult.TotalEarned, totalResult.TotalMax);
                studentGrade.Status = "under_review";

                await _repository.SaveStudentGradeAsync(studentGrade);
            }
        }

        // שימוש ב-Audit Service המרכזי במקום שמירה ישירה
        _auditService.LogNonBlocking(
            userId: currentTeacherId,
            action: "GRADE_OVERRIDDEN",
            resourceType: "QuestionGrade",
            resourceId: questionGradeId,
            oldValue: oldScore.ToString(),
            newValue: request.NewScore.ToString()
        );

        return Ok(new { Message = "Grade overridden successfully.", NewFinalScore = request.NewScore });
    }

    // --- משימה T-048: אישור ציון סופי על ידי המורה (Approve) ---
    [HttpPost("{studentGradeId}/approve")]
    public async Task<IActionResult> ApproveGrade(Guid studentGradeId)
    {
        var studentGrade = await _repository.GetGradeByStudentExamIdAsync(studentGradeId);
        if (studentGrade == null) return NotFound("Grade not found.");

        if (studentGrade.Status == "approved")
        {
            return BadRequest("This grade is already approved.");
        }

        Guid currentTeacherId = GetCurrentTeacherId();
        bool isOwner = await _repository.IsTeacherOwnerOfExamAsync(currentTeacherId, studentGrade.ExamId);
        if (!isOwner) return Forbid("Not authorized to approve this exam.");

        studentGrade.Status = "approved";
        
        await _repository.SaveStudentGradeAsync(studentGrade);
        await _repository.UpdateStudentExamStatusAsync(studentGradeId, "approved");

        _auditService.LogNonBlocking(
            userId: currentTeacherId,
            action: "GRADE_APPROVED",
            resourceType: "StudentGrade",
            resourceId: studentGradeId,
            oldValue: "under_review",
            newValue: "approved"
        );

        return Ok(new { Message = "Grade approved successfully." });
    }

    // --- משימה T-047: הוספת הערת מורה כללית למבחן ---
    [HttpPost("{studentGradeId}/comments")]
    public async Task<IActionResult> AddTeacherComment(Guid studentGradeId, [FromBody] CommentRequest request)
    {
        await _repository.SaveCommentAsync(studentGradeId, request.Comment);

        _auditService.LogNonBlocking(
            userId: GetCurrentTeacherId(),
            action: "COMMENT_ADDED",
            resourceType: "StudentGrade",
            resourceId: studentGradeId,
            oldValue: string.Empty,
            newValue: request.Comment
        );

        return Ok("Comment added successfully.");
    }

    // --- משימה T-047: שליפת היסטוריית שינויים מלאה (Audit Trail) ---
    [HttpGet("{studentGradeId}/history")]
    public async Task<IActionResult> GetGradeHistory(Guid studentGradeId)
    {
        var history = await _repository.GetHistoryAsync(studentGradeId);
        return Ok(history);
    }

    private Guid GetCurrentTeacherId()
    {
        return Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.NewGuid().ToString());
    }
}

public record OverrideRequest(decimal NewScore, bool NewIsCorrect, string Comment);
public record CommentRequest(string Comment);