using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExamAI.Grading.Worker.Services;

namespace ExamAI.Grading.Worker.Repositories;

// הגדרת ישויות ה-DB לפי הדרישות בטיקטים
public class StudentGradeEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StudentExamId { get; set; }
    public Guid ExamId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public decimal TotalScore { get; set; }
    public decimal Percentage { get; set; }
    public string Status { get; set; } = "ai_graded"; // ai_graded, under_review, approved
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<QuestionGradeEntity> QuestionGrades { get; set; } = [];
}

public class QuestionGradeEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StudentGradeId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public decimal MaxPoints { get; set; }
    public decimal AiScore { get; set; }        // שמירת הציון המקורי של ה-AI
    public decimal FinalScore { get; set; }     // הציון המשוקלל/הסופי (יכול להשתנות בדריסה)
    public bool IsCorrect { get; set; }
    public string Explanation { get; set; } = string.Empty;
    public bool NeedsReview { get; set; }
    public string AnnotationJson { get; set; } = string.Empty; // שמירת ה-Bounding Box והסימון כ-JSON
}

public class TeacherOverrideEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid QuestionGradeId { get; set; }
    public decimal OldScore { get; set; }
    public decimal NewScore { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class AuditLogEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EntityName { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string OldValue { get; set; } = string.Empty;
    public string NewValue { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public interface IGradingRepository
{
    Task SaveStudentGradeAsync(StudentGradeEntity grade);
    Task UpdateStudentExamStatusAsync(Guid studentExamId, string status);
    Task<StudentGradeEntity?> GetGradeByStudentExamIdAsync(Guid studentExamId);
    Task<List<StudentGradeEntity>> GetGradesByExamIdAsync(Guid examId, string? statusFilter);
    Task<QuestionGradeEntity?> GetQuestionGradeByIdAsync(Guid questionGradeId);
    Task SaveTeacherOverrideAsync(TeacherOverrideEntity teacherOverride);
    Task UpdateQuestionScoreAsync(Guid questionGradeId, decimal newScore);
    Task SaveAuditLogAsync(AuditLogEntity log);
    Task SaveCommentAsync(Guid studentGradeId, string comment);
    Task<List<object>> GetHistoryAsync(Guid studentGradeId);
    Task<bool> IsTeacherOwnerOfExamAsync(Guid teacherId, Guid examId);
}