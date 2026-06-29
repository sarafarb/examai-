using Microsoft.EntityFrameworkCore;
using ExamAI.Analytics.API.Models;

namespace ExamAI.Analytics.API.Data
{
    public class AnalyticsDbContext : DbContext
    {
        public AnalyticsDbContext(DbContextOptions<AnalyticsDbContext> options)
            : base(options)
        {
        }

        // 📊 הטבלאות החדשות של האנליטיקה
        public DbSet<ExamAnalytics> ExamAnalytics { get; set; }
        public DbSet<QuestionAnalytics> QuestionAnalytics { get; set; }

        // 📝 טבלת ציוני הסטודנטים שממנה שולפים את הנתונים לחישוב
        public DbSet<StudentGrade> StudentGrades { get; set; }
    }
}