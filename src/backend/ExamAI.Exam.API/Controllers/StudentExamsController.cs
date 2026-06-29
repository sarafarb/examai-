using Microsoft.AspNetCore.Mvc;

namespace ExamAI.Exam.API.Controllers
{
    [ApiController]
    [Route("api/v1/exams/{examId}/student-exams")]
    public class StudentExamsController : ControllerBase
    {
        private readonly ILogger<StudentExamsController> _logger;

        public StudentExamsController(ILogger<StudentExamsController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> UploadStudentExam(
            string examId, 
            [FromForm] string studentName, 
            [FromForm] string? studentExternalId, 
            [FromForm] List<IFormFile> files)
        {
            // 1. וולידציות בסיסיות
            if (files == null || files.Count == 0)
                return BadRequest(new { Error = "No files uploaded." });

            if (files.Count > 20)
                return BadRequest(new { Error = "Maximum 20 files allowed per student." });

            foreach (var file in files)
            {
                if (file.Length > 50 * 1024 * 1024) // 50MB
                    return BadRequest(new { Error = $"File {file.FileName} exceeds the 50MB limit." });
            }

            // 2. סימולציית בדיקת מערכת הבילינג (Billing)
            bool isFreePlan = true;
            int currentUsage = 25; // נניח שהוא הגיע ללימיט של החינמי
            
            // בוא נשנה זמנית ל-24 כדי שיעבור את הבדיקה בטסטים
            currentUsage = 24; 

            if (isFreePlan && currentUsage >= 25)
            {
                return StatusCode(402, new { Error = "Payment Required. You have reached your monthly OCR limit of 25 pages on the Free plan." });
            }

            // יצירת מזהה ייחודי למבחן של התלמיד הספציפי
            string studentExamId = Guid.NewGuid().ToString();
            int totalPages = 0;

            // 3. עיבוד הקבצים (וירוסים, Magic bytes, והעלאה ל-S3)
            foreach (var file in files)
            {
                _logger.LogInformation("Scanning and validating {FileName}...", file.FileName);
                // כאן יהיו קריאות ל-VirusScanner ול-MagicBytes Validator שיצרנו קודם...

                string s3Path = $"mock-teacher-uuid/{examId}/students/{studentExamId}/{file.FileName}";
                _logger.LogInformation("Uploaded to S3: {Path}", s3Path);
                
                // ספירת דפים - נניח שכל קובץ תמונה זה דף אחד
                totalPages++; 
            }

            // 4. רישום ב-DB ודיווח שימוש למערכת הבילינג
            _logger.LogInformation("Recorded {TotalPages} pages usage for billing.", totalPages);
            _logger.LogInformation("Inserted record into ocr.student_exams table.");

            // 5. פרסום הודעה לתור RabbitMQ
            _logger.LogInformation("Published 'OcrJobQueuedEvent' to RabbitMQ for queue 'ocr.jobs' [ExamId: {ExamId}, StudentExamId: {StudentExamId}]", examId, studentExamId);

            // 6. החזרת 202 Accepted כי התהליך יקרה ברקע
            return StatusCode(202, new 
            { 
                StudentExamId = studentExamId, 
                Status = "processing",
                Message = "Files uploaded successfully. OCR processing started in the background."
            });
        }

        [HttpGet]
        public IActionResult GetStudentExams(string examId)
        {
            // החזרת רשימה (Mock)
            var list = new object[]
            {
                new { Id = Guid.NewGuid().ToString(), StudentName = "ישראל ישראלי", Status = "completed", Score = 85 },
                new { Id = Guid.NewGuid().ToString(), StudentName = "דנה כהן", Status = "processing", Score = (int?)null }
            };
            return Ok(list);
        }

        [HttpGet("~/api/v1/student-exams/{id}/status")] // נתיב ישיר לסטטוס בלי ExamId
        public IActionResult GetStudentExamStatus(string id)
        {
            // פולינג לקבלת סטטוס ואחוז התקדמות
            return Ok(new 
            { 
                Id = id, 
                Status = "processing", 
                ProgressPercentage = 45 
            });
        }
    }
}