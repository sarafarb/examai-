using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using MassTransit;
using ExamAI.Shared.Contracts;
using ExamAI.Export.Worker.Services;
using ExamAI.Grading.Worker.Repositories;

namespace ExamAI.Export.Worker.Consumers;

public class ExportJobConsumer : IConsumer<ExportJobMessage>
{
    private readonly ExamPdfGeneratorService _pdfGeneratorService;
    private readonly IGradingRepository _repository;

    public ExportJobConsumer(ExamPdfGeneratorService pdfGeneratorService, IGradingRepository repository)
    {
        _pdfGeneratorService = pdfGeneratorService;
        _repository = repository;
    }

    public async Task Consume(ConsumeContext<ExportJobMessage> context)
    {
        var message = context.Message;
        Console.WriteLine($"[Export Worker] Processing export job {message.ExportJobId} for StudentGrade {message.StudentGradeId}...");

        try
        {
            // 1. עדכון סטטוס משימה ל- "processing"
            // await _repository.UpdateExportJobStatusAsync(message.ExportJobId, "processing");

            if (message.IsBatch)
            {
                // --- טיפול בייצוא כיתתי (Batch ZIP) ---
                Console.WriteLine($"[Export Worker] Generating Batch ZIP for Exam {message.ExamId}...");
                
                // סימולציית יצירת ZIP מהציונים המאושרים של המבחן
                await Task.Delay(3000); // סימולציית ריצה
                
                Console.WriteLine($"[Export Worker] Batch ZIP uploaded to S3: exports/batches/{message.ExportJobId}.zip");
            }
            else
            {
                // --- טיפול בסטודנט בודד ---
                // שליפת נתוני הציון מהמאגר
                var grade = await _repository.GetGradeByStudentExamIdAsync(message.StudentGradeId);
                if (grade == null) throw new Exception("Grade record not found.");

                // סימולציה לשליפת קובצי ה-Bytes של תמונות המחברת המקוריות (למשל מתוך S3)
                var mockOriginalImages = new List<byte[]> { new byte[100], new byte[100] }; 

                // הפעלת שירות ה-PDF שבונה את האנוטציות בכתב יד (העט והפונט שנבחרו ב-FE)
                using MemoryStream pdfStream = _pdfGeneratorService.GenerateStudentReport(
                    grade, 
                    message.SelectedColor, 
                    message.SelectedStyle, 
                    mockOriginalImages
                );

                // 2. העלאת ה-PDF המוכן ל-S3 (כאן יוזרק קליינט של AWS S3 במציאות)
                string s3Key = $"{message.TeacherId}/exports/{message.ExportJobId}.pdf";
                Console.WriteLine($"[Export Worker] Uploading PDF to S3 key: {s3Key} (Size: {pdfStream.Length} bytes)");
                await Task.Delay(1500); // סימולציית העלאה
            }

            // 3. עדכון סטטוס משימה ל- "completed" ב-DB
            // await _repository.UpdateExportJobStatusAsync(message.ExportJobId, "completed");
            Console.WriteLine($"[Export Worker] Job {message.ExportJobId} successfully completed! Teacher notified.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Export Worker] Critical error in job {message.ExportJobId}: {ex.Message}");
            // await _repository.UpdateExportJobStatusAsync(message.ExportJobId, "failed");
            throw;
        }
    }
}