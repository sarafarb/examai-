namespace ExamAI.Grading.Worker.Services;

public class BoundingBox
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
}

public class GradingAnnotation
{
    public string Symbol { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public BoundingBox Box { get; set; } = new();
}

public static class GradingAnnotationBuilder
{
    public static GradingAnnotation Build(decimal earnedScore, decimal maxScore, decimal deduction, BoundingBox box)
    {
        var annotation = new GradingAnnotation
        {
            Box = box
        };

        if (earnedScore == maxScore && deduction == 0)
        {
            annotation.Symbol = "✓";
            annotation.Label = "מלא";
            annotation.Color = "green";
        }
        else if (earnedScore == 0)
        {
            annotation.Symbol = "✗";
            annotation.Label = "0 נקודות";
            annotation.Color = "red";
        }
        else // Partial Credit
        {
            annotation.Symbol = $"-{deduction}";
            annotation.Label = $"{earnedScore} נקודות";
            annotation.Color = "orange"; // או red לפי העדפתכם
        }

        return annotation;
    }
}