using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OpenAI.Chat;

namespace ExamAI.Grading.Worker.Services;

// מודל הבקשה לבדיקת שאלה
public record GradingRequest(
    string QuestionText,
    string CorrectAnswer,
    string AlternativeAnswers,
    string GradingNotes,
    int MaxPoints,
    int Strictness,
    string StudentAnswer
);

// מודל התוצאה המבוקש מ-OpenAI
public class GradingResult
{
    public bool IsCorrect { get; set; }
    public decimal Score { get; set; }
    public decimal Deduction { get; set; }
    public string Explanation { get; set; } = string.Empty;
    public double Confidence { get; set; }
}

public interface IOpenAiGradingService
{
    Task<GradingResult> GradeQuestionAsync(GradingRequest request);
}

public class OpenAiGradingService : IOpenAiGradingService
{
    private readonly ChatClient _primaryClient;   // gpt-4o
    private readonly ChatClient _fallbackClient;  // gpt-4-turbo

    // T-044: הגבלת עומס של מקסימום 10 בקשות בו-זמנית ל-OpenAI
    private static readonly SemaphoreSlim _semaphore = new(10, 10);

    public OpenAiGradingService(string apiKey)
    {
        // אתחול הקליינטים הרשמיים של OpenAI
        _primaryClient = new ChatClient("gpt-4o", apiKey);
        _fallbackClient = new ChatClient("gpt-4-turbo", apiKey);
    }

    public async Task<GradingResult> GradeQuestionAsync(GradingRequest request)
    {
        await _semaphore.WaitAsync();
        try
        {
            // ניסיון ראשון עם המודל הראשי (gpt-4o) כולל לוגיקת רטריי
            return await ExecuteWithRetryAsync(_primaryClient, request);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Fallback Alert] gpt-4o failed after retries. Error: {ex.Message}. Trying gpt-4-turbo...");

            // T-044 Fallback: אם gpt-4o כשל לחלוטין, מנסים את gpt-4-turbo פעם אחת
            return await ExecuteChatAsync(_fallbackClient, request);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<GradingResult> ExecuteWithRetryAsync(ChatClient client, GradingRequest request)
    {
        int maxRetries = 3;
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await ExecuteChatAsync(client, request);
            }
            catch (Exception) when (attempt < maxRetries)
            {
                // T-044: Exponential backoff (2, 4, 8 שניות השהייה בהתאמה)
                int delaySeconds = (int)Math.Pow(2, attempt);
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
            }
        }
        throw new Exception("All retry attempts for primary model failed.");
    }

    private async Task<GradingResult> ExecuteChatAsync(ChatClient client, GradingRequest request)
    {
        // T-044: הגדרת Timeout של 30 שניות לכל פנייה
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        // בניית הפרומפט לפי התבנית הנדרשת
        string systemPrompt = "You are grading a Hebrew school exam. Respond in JSON only.";
        string userPrompt = $@"
Question: {request.QuestionText}
Correct Answer: {request.CorrectAnswer}
Alternative Accepted Answers: {request.AlternativeAnswers}
Grading Notes: {request.GradingNotes}
Max Points: {request.MaxPoints}
Strictness Level: {request.Strictness}/100 (0=semantic match OK, 100=exact wording required)

Student's Answer: {request.StudentAnswer}

Respond in JSON only matching this schema:
{{
  ""isCorrect"": boolean,
  ""score"": number,
  ""deduction"": number,
  ""explanation"": ""string in Hebrew"",
  ""confidence"": number (0-1)
}}";

      var options = new ChatCompletionOptions
{
    ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
};

        ChatCompletion completion = await client.CompleteChatAsync(
            [
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userPrompt)
            ],
            options,
            cts.Token
        );

        string jsonResponse = completion.Content[0].Text;

        // המרה לטיפוס אובייקט C# והחזרה
        return JsonSerializer.Deserialize<GradingResult>(jsonResponse, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new Exception("Failed to deserialize OpenAI grading response.");
    }
}