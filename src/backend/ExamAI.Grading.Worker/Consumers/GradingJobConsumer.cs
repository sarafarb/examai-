using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using MassTransit;
using ExamAI.Grading.Worker.Services;
using ExamAI.Grading.Worker.Repositories;

namespace ExamAI.Grading.Worker.Consumers;
public class GradingJobMessage
{
    public Guid StudentExamId { get; set; }
    public Guid ExamId { get; set; }
    public Guid TeacherId { get; set; }
}
public record GradingCompletedEvent(
    Guid StudentExamId,
    Guid ExamId,
    Guid TeacherId,
    string StudentName,
    decimal TotalScore,
    decimal Percentage,
    bool RequiresSpecialReview
);
public class GradingJobConsumer : IConsumer<GradingJobMessage>
{
    private readonly IOpenAiGradingService _gradingService;
    private readonly IStrictnessEngine _strictnessEngine;
    private readonly IScoreCalculationService _scoreService;
    private readonly IGradingRepository _repository;

    public GradingJobConsumer(
        IOpenAiGradingService gradingService,
        IStrictnessEngine strictnessEngine,
        IScoreCalculationService scoreService,
        IGradingRepository repository)
    {
        _gradingService = gradingService;
        _strictnessEngine = strictnessEngine;
        _scoreService = scoreService;
        _repository = repository;
    }

    public async Task Consume(ConsumeContext<GradingJobMessage> context)
    {
        var message = context.Message;
        
        // סימולציה של שליפת שאלות ותשובות תלמיד מה-DB (T-042)
        var mockQuestions = new List<GradingRequest>
        {
            new("מהי בירת ישראל?", "ירושלים", "", "", 10, 80, "ירשלים"),
            new("כמה זה 5x5?", "25", "עשרים וחמש", "", 10, 50, "30"),
            new("מי כתב את התקווה?", "נפתלי הרץ אימבר", "", "", 10, 90, "לא יודע")
        };

        var studentGrade = new StudentGradeEntity
        {
            StudentExamId = message.StudentExamId,
            ExamId = message.ExamId,
            StudentName = "ישראל ישראלי"
        };

        int reviewCount = 0;
        var scorePairs = new List<(decimal earned, decimal max)>();

        foreach (var req in mockQuestions)
        {
            // 1. בדיקת ה-AI
            GradingResult aiResult = await _gradingService.GradeQuestionAsync(req);
            
            // 2. הפעלת מנוע קשיחות
            StrictnessResult strictnessResult = _strictnessEngine.ApplyStrictness(aiResult, req.Strictness, req.StudentAnswer, req.CorrectAnswer);
            
            // 3. חישוב ציון לשאלה
            decimal finalQuestionScore = _scoreService.CalculateQuestionScore(strictnessResult.AdjustedResult, req.MaxPoints);
            scorePairs.Add((finalQuestionScore, req.MaxPoints));

            if (strictnessResult.NeedsReview) reviewCount++;

            // 4. בניית אנוטציה גרפית
            var annotation = GradingAnnotationBuilder.Build(finalQuestionScore, req.MaxPoints, strictnessResult.AdjustedResult.Deduction, new BoundingBox { X = 50, Y = 100, Width = 200, Height = 40 });

            studentGrade.QuestionGrades.Add(new QuestionGradeEntity
            {
                QuestionText = req.QuestionText,
                MaxPoints = req.MaxPoints,
                AiScore = aiResult.Score,
                FinalScore = finalQuestionScore,
                IsCorrect = strictnessResult.AdjustedResult.IsCorrect,
                Explanation = strictnessResult.AdjustedResult.Explanation,
                NeedsReview = strictnessResult.NeedsReview,
                AnnotationJson = JsonSerializer.Serialize(annotation)
            });
        }

        // 5. חישוב סופי משוקלל (ScoreCalculationService)
        var totalScoreResult = _scoreService.CalculateTotalScore(scorePairs);
        studentGrade.TotalScore = totalScoreResult.TotalEarned;
        studentGrade.Percentage = _scoreService.CalculatePercentage(totalScoreResult.TotalEarned, totalScoreResult.TotalMax);

        // T-046: בדיקה אם מעל 30% מהשאלה דורשות אישור אנושי
        bool requiresSpecialReview = ((double)reviewCount / mockQuestions.Count) > 0.3;

        // 6. שמירה ל-Database ועדכון סטטוס ה-OCR ל-Graded
        await _repository.SaveStudentGradeAsync(studentGrade);
        await _repository.UpdateStudentExamStatusAsync(message.StudentExamId, "graded");

        // 7. הפצת אירוע סיום ברשת (שיוביל לשליחת המייל למורה)
        await context.Publish(new GradingCompletedEvent(
            StudentExamId: studentGrade.StudentExamId,
            ExamId: studentGrade.ExamId,
            TeacherId: message.TeacherId,
            StudentName: studentGrade.StudentName,
            TotalScore: studentGrade.TotalScore,
            Percentage: studentGrade.Percentage,
            RequiresSpecialReview: requiresSpecialReview
        ));
    }
}