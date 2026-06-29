using MassTransit;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using ExamAI.OCR.Worker.Services;

namespace ExamAI.OCR.Worker.Workers;

// --- Messages & Events ---
public class OcrJobMessage
{
    public Guid JobId { get; set; } // הוספנו כדי שנוכל לעדכן סטטוס משימה
    public Guid ExamId { get; set; }
    public Guid StudentExamId { get; set; }
    public Guid TeacherId { get; set; } // דרוש עבור ה-Grading Job
    public string SourcePdfPath { get; set; } = string.Empty;
}

public class GradingJobMessage
{
    public Guid StudentExamId { get; set; }
    public Guid ExamId { get; set; }
    public Guid TeacherId { get; set; }
}

public class OcrFailedEvent
{
    public Guid JobId { get; set; }
    public Guid StudentExamId { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}

// --- Interfaces ---
public interface IS3StorageService
{
    Task<Stream> GetFileStreamAsync(string s3Path);
    Task UploadImageAsync(string s3Path, byte[] imageBytes);
}

public interface IOcrRepository
{
    Task SaveOcrResultsAsync(Guid studentExamId, object pages, object answers); // object זמני עד שיהיו לכם מודלים אמיתיים
    Task UpdateStudentExamStatusAsync(Guid studentExamId, string status);
    Task UpdateJobStatusAsync(Guid jobId, string status);
}

// --- Dummy Implementations ---
public class DummyS3StorageService : IS3StorageService
{
    public Task<Stream> GetFileStreamAsync(string s3Path) => Task.FromResult<Stream>(new MemoryStream(new byte[100]));
    public Task UploadImageAsync(string s3Path, byte[] imageBytes) => Task.CompletedTask;
}

public class DummyOcrRepository : IOcrRepository
{
    public Task SaveOcrResultsAsync(Guid studentExamId, object pages, object answers) => Task.CompletedTask;
    public Task UpdateStudentExamStatusAsync(Guid studentExamId, string status) => Task.CompletedTask;
    public Task UpdateJobStatusAsync(Guid jobId, string status) => Task.CompletedTask;
}

// --- The Worker (Consumer) ---
public class OcrJobWorker : IConsumer<OcrJobMessage> 
{
    private readonly IImagePreprocessingService _preprocessingService;
    private readonly IS3StorageService _s3Service;
    private readonly IOcrRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint; // מאפשר לשלוח הודעות לתורים אחרים
    private readonly ILogger<OcrJobWorker> _logger;

    public OcrJobWorker(
        IImagePreprocessingService preprocessingService,
        IS3StorageService s3Service,
        IOcrRepository repository,
        IPublishEndpoint publishEndpoint,
        ILogger<OcrJobWorker> logger)
    {
        _preprocessingService = preprocessingService;
        _s3Service = s3Service;
        _repository = repository;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OcrJobMessage> context)
    {
        var message = context.Message;
        _logger.LogInformation("Starting OCR processing. Job: {JobId}, StudentExam: {StudentId}", message.JobId, message.StudentExamId);

        try
        {
            // 1. קריאה מ-S3 ועיבוד (כפי שהיה)
            using Stream pdfStream = await _s3Service.GetFileStreamAsync(message.SourcePdfPath);
            var processedPages = await _preprocessingService.ProcessPdfAsync(pdfStream);

            // TODO: כאן תוסיפו את קריאת ה-OCR האמיתית (למשל tesseract/AWS Textract)
            var extractedAnswers = new List<object>(); // דמי לתוצאות
            double confidenceAverage = 0.85; // דמי לביטחון הזיהוי

            // 2. שמירת תוצאות ב-DB (Pages & Answers)
            await _repository.SaveOcrResultsAsync(message.StudentExamId, processedPages, extractedAnswers);

            // 3. עדכון סטטוס מבחן בהתאם לאיכות הזיהוי
            string examStatus = confidenceAverage >= 0.7 ? "ocr_complete" : "low_confidence";
            await _repository.UpdateStudentExamStatusAsync(message.StudentExamId, examStatus);

            // 4. עדכון סטטוס המשימה למושלם
            await _repository.UpdateJobStatusAsync(message.JobId, "completed");

            // 5. העברה לתור הבדיקה האוטומטית (Grading)
            await _publishEndpoint.Publish(new GradingJobMessage
            {
                StudentExamId = message.StudentExamId,
                ExamId = message.ExamId,
                TeacherId = message.TeacherId
            });

            _logger.LogInformation("OCR completed successfully for Job: {JobId}. Published to grading queue.", message.JobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OCR Failed for Job: {JobId}", message.JobId);

            // 6. במקרה של שגיאה - עדכון סטטוס וזריקת אירוע
            await _repository.UpdateStudentExamStatusAsync(message.StudentExamId, "ocr_failed");
            await _repository.UpdateJobStatusAsync(message.JobId, "failed");
            
            await _publishEndpoint.Publish(new OcrFailedEvent
            {
                JobId = message.JobId,
                StudentExamId = message.StudentExamId,
                ErrorMessage = ex.Message
            });

            // אנחנו זורקים את השגיאה מחדש כדי ש-MassTransit ידע שההודעה נכשלה ויפעיל את מנגנון ה-Retry
            throw; 
        }
    }
}