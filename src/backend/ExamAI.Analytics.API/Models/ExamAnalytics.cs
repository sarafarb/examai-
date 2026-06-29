using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExamAI.Analytics.API.Models
{
    // 1. נתוני המבחן
    [Table("exam_analytics", Schema = "analytics")]
    public class ExamAnalytics
    {
        [Key]
        public string ExamId { get; set; } = string.Empty;
        public double Average { get; set; }
        public double Median { get; set; }
        public double StandardDeviation { get; set; }
        public double MinScore { get; set; }
        public double MaxScore { get; set; }
        public double PassRate { get; set; } 
        
        [Column(TypeName = "jsonb")] 
        public string DistributionJson { get; set; } = "{}"; 
        
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    // 2. נתוני שאלות (היה חסר)
    [Table("question_analytics", Schema = "analytics")]
    public class QuestionAnalytics
    {
        [Key]
        public string QuestionId { get; set; } = string.Empty;
        public string ExamId { get; set; } = string.Empty;
        public int CorrectCount { get; set; }
        public int IncorrectCount { get; set; }
        public double AverageScore { get; set; }
        public double DifficultyIndex { get; set; } 
        
        [Column(TypeName = "jsonb")]
        public string CommonMistakesJson { get; set; } = "[]";
    }

    // 3. נתוני ציוני תלמיד כדי שה-Service יוכל לחשב (היה חסר)
    [Table("student_grades")] // שם טבלה גנרי להמחשה
    public class StudentGrade
    {
        [Key]
        public string Id { get; set; } = string.Empty;
        public string ExamId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public double FinalScore { get; set; }
        public string Status { get; set; } = "ai_graded"; 
    }
}