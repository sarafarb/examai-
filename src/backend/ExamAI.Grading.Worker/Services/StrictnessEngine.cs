using System;

namespace ExamAI.Grading.Worker.Services;

public class StrictnessResult
{
    public GradingResult AdjustedResult { get; set; } = new();
    public bool NeedsReview { get; set; }
}

public interface IStrictnessEngine
{
    StrictnessResult ApplyStrictness(GradingResult llmResult, int strictness, string studentAnswer, string correctAnswer);
}

public class StrictnessEngine : IStrictnessEngine
{
    private readonly IStringSimilarityService _similarityService;

    public StrictnessEngine(IStringSimilarityService similarityService)
    {
        _similarityService = similarityService;
    }

    public StrictnessResult ApplyStrictness(GradingResult llmResult, int strictness, string studentAnswer, string correctAnswer)
    {
        var result = new StrictnessResult 
        { 
            AdjustedResult = llmResult,
            NeedsReview = false
        };

        // Strictness 0-30: Semantic (מקבלים את הפסיקה של ה-LLM לחלוטין, כולל Partial Credit)
        if (strictness <= 30)
        {
            return result; 
        }
        
        // Strictness 31-70: Balanced (ה-LLM קובע)
        if (strictness <= 70)
        {
            return result;
        }

        // Strictness 71-100: Exact string match required
        double similarity = _similarityService.ComputeSimilarity(studentAnswer, correctAnswer);
        
        // דורשים התאמה מחמירה. 100 קשיחות = 1.0 התאמה (100%), 80 קשיחות = 0.8 התאמה (80%).
        double requiredSimilarityThreshold = strictness / 100.0; 
        
        bool isStringSimilar = similarity >= requiredSimilarityThreshold;

        if (!isStringSimilar && llmResult.IsCorrect)
        {
            // ה-LLM אמר נכון, אבל מנוע המחרוזות קבע שהתשובה רחוקה מדי בניסוח -> דורסים את ה-LLM ומבקשים בדיקה אנושית
            result.AdjustedResult.IsCorrect = false;
            result.AdjustedResult.Score = 0;
            result.AdjustedResult.Explanation = $"נפסל עקב אי-התאמה בניסוח (הוגדרה קשיחות של {strictness}). " + llmResult.Explanation;
            result.NeedsReview = true;
        }
        else if (isStringSimilar && !llmResult.IsCorrect)
        {
            // המחרוזת כמעט זהה, אבל ה-LLM משום מה פסל -> מרמים דגל לבדיקה אנושית
            result.NeedsReview = true;
        }

        return result;
    }
}