using System;
using System.Linq;
using System.Text.Json;
using ExamAI.Analytics.API.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ExamAI.Analytics.API.Models;

namespace ExamAI.Analytics.API.Services
{
    public class AnalyticsCalculationService
    {
        private readonly AnalyticsDbContext _dbContext;

        public AnalyticsCalculationService(AnalyticsDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task CalculateExamAnalyticsAsync(string examId)
        {
            // 1. שליפת כל הציונים המאושרים של המבחן (הנחה שיש לך טבלת StudentGrades)
            var grades = await _dbContext.StudentGrades
                .Where(g => g.ExamId == examId && g.Status == "approved")
                .Select(g => g.FinalScore)
                .ToListAsync();

            if (!grades.Any()) return;

            // 2. חישובים סטטיסטיים
            int count = grades.Count;
            double average = grades.Average();
            double min = grades.Min();
            double max = grades.Max();
            double passRate = (double)grades.Count(g => g >= 60) / count * 100;

            // חציון (Median)
            var sortedGrades = grades.OrderBy(n => n).ToList();
            double median = count % 2 == 0 
                ? (sortedGrades[count / 2 - 1] + sortedGrades[count / 2]) / 2.0 
                : sortedGrades[count / 2];

            // סטיית תקן (Standard Deviation)
            double sumOfSquares = grades.Select(g => Math.Pow(g - average, 2)).Sum();
            double stdDev = Math.Sqrt(sumOfSquares / count);

            // 3. התפלגות ציונים (Distribution) ב-10 קבוצות
            var distribution = new Dictionary<string, int>
            {
                {"0-10", 0}, {"11-20", 0}, {"21-30", 0}, {"31-40", 0}, {"41-50", 0},
                {"51-60", 0}, {"61-70", 0}, {"71-80", 0}, {"81-90", 0}, {"91-100", 0}
            };

            foreach (var grade in grades)
            {
                string bucket = grade switch
                {
                    <= 10 => "0-10",
                    <= 20 => "11-20",
                    <= 30 => "21-30",
                    <= 40 => "31-40",
                    <= 50 => "41-50",
                    <= 60 => "51-60",
                    <= 70 => "61-70",
                    <= 80 => "71-80",
                    <= 90 => "81-90",
                    _ => "91-100"
                };
                distribution[bucket]++;
            }

            // 4. Upsert (עדכון או יצירה)
            var analytics = await _dbContext.ExamAnalytics.FirstOrDefaultAsync(e => e.ExamId == examId) 
                            ?? new ExamAnalytics { ExamId = examId };

            analytics.Average = Math.Round(average, 2);
            analytics.Median = median;
            analytics.StandardDeviation = Math.Round(stdDev, 2);
            analytics.MinScore = min;
            analytics.MaxScore = max;
            analytics.PassRate = Math.Round(passRate, 2);
            analytics.DistributionJson = JsonSerializer.Serialize(distribution);
            analytics.LastUpdated = DateTime.UtcNow;

            if (analytics.ExamId != null && _dbContext.Entry(analytics).State == EntityState.Detached)
            {
                _dbContext.ExamAnalytics.Add(analytics);
            }

            await _dbContext.SaveChangesAsync();
        }
    }
}