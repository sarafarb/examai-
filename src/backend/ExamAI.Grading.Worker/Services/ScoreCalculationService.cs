using System;
using System.Collections.Generic;
using System.Linq;

namespace ExamAI.Grading.Worker.Services;

public class TotalScoreResult
{
    public decimal TotalEarned { get; set; }
    public decimal TotalMax { get; set; }
}

public interface IScoreCalculationService
{
    decimal CalculateQuestionScore(GradingResult gradingResult, decimal maxPoints);
    TotalScoreResult CalculateTotalScore(IEnumerable<(decimal earned, decimal max)> questionGrades);
    decimal CalculatePercentage(decimal totalEarned, decimal maxPoints);
}

public class ScoreCalculationService : IScoreCalculationService
{
    public decimal CalculateQuestionScore(GradingResult gradingResult, decimal maxPoints)
    {
        // מבטיח שהציון לא יורד מ-0 ולא עולה על המקסימום לשאלה
        decimal finalScore = gradingResult.Score - gradingResult.Deduction;
        return Math.Clamp(finalScore, 0, maxPoints);
    }

    public TotalScoreResult CalculateTotalScore(IEnumerable<(decimal earned, decimal max)> questionGrades)
    {
        return new TotalScoreResult
        {
            TotalEarned = questionGrades.Sum(q => q.earned),
            TotalMax = questionGrades.Sum(q => q.max)
        };
    }

    public decimal CalculatePercentage(decimal totalEarned, decimal maxPoints)
    {
        if (maxPoints <= 0) return 0;
        
        decimal percentage = (totalEarned / maxPoints) * 100;
        return Math.Round(percentage, 2); // עיגול ל-2 ספרות אחרי הנקודה
    }
}