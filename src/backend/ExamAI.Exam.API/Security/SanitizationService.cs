using Ganss.Xss;

namespace ExamAI.Exam.API.Security;

public static class SanitizationService
{
    private static readonly HtmlSanitizer _sanitizer = new HtmlSanitizer();

    public static string SanitizeInput(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        
        // 1. הסרת null bytes
        input = input.Replace("\x00", ""); 
        
        // 2. ניקוי HTML מתגיות זדוניות (XSS)
        return _sanitizer.Sanitize(input);
    }
}