using System;
using System.IO;
using System.Collections.Generic;
using SkiaSharp;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ExamAI.Shared.Contracts;
using ExamAI.Grading.Worker.Repositories; // הנחה שיש מודלים משותפים פה

namespace ExamAI.Export.Worker.Services;

public class ExamPdfGeneratorService
{
    public MemoryStream GenerateStudentReport(
        StudentGradeEntity grade, 
        PenColor color, 
        HandwritingStyle style, 
        List<byte[]> originalPageImages)
    {
        var outputStream = new MemoryStream();
        
        // 1. הגדרת צבע העט של SkiaSharp
        SKColor skPenColor = color switch
        {
            PenColor.Blue => SKColors.DarkBlue,
            PenColor.Black => SKColors.Black,
            _ => SKColors.Red // ברירת מחדל אדום
        };

        // 2. טעינת פונט כתב היד (יש להוסיף קובצי ttf לתיקיית Assets)
        string fontPath = style switch
        {
            HandwritingStyle.QuickScribble => "Assets/Fonts/HebrewHandwriting-Scribble.ttf",
            HandwritingStyle.ElegantInk => "Assets/Fonts/HebrewHandwriting-Elegant.ttf",
            _ => "Assets/Fonts/HebrewHandwriting-Classic.ttf"
        };
        
        // אם הפונט לא נמצא, נשתמש בברירת מחדל כדי שלא יקרוס
        using var typeface = SKTypeface.FromFile(fontPath) ?? SKTypeface.FromFamilyName("Arial");

        var processedPages = new List<byte[]>();
        
        // 3. עיבוד כל דף במבחן וציור בכתב יד בעזרת SkiaSharp
        for (int i = 0; i < originalPageImages.Count; i++)
        {
            using var bitmap = SKBitmap.Decode(originalPageImages[i]);
            using var canvas = new SKCanvas(bitmap);

            using var paint = new SKPaint
            {
                Typeface = typeface,
                Color = skPenColor,
                TextSize = 64, // גודל גופן הסימונים
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };

            // ציור הציון הסופי הענק בכתב יד על גבי הדף הראשון של המחברת
            if (i == 0)
            {
                using var bigScorePaint = paint.Clone();
                bigScorePaint.TextSize = 120; // ענק
                // ציור הציון בפינה השמאלית העליונה של דף המבחן (קואורדינטות לדוגמה)
                canvas.DrawText($"{grade.TotalScore}", 150, 200, bigScorePaint);
                
                // ציור קו תחתי או עיגול בכתב יד מתחת לציון
                using var strokePaint = bigScorePaint.Clone();
                strokePaint.Style = SKPaintStyle.Stroke;
                strokePaint.StrokeWidth = 8;
                canvas.DrawOval(new SKRect(100, 80, 350, 250), strokePaint); // עיגול סביב הציון
            }

            // ציור סימוני השאלות בדף (וי / מינוס)
            // הערה: נדרש למפות כל שאלה לדף ולמיקום הספציפי שלה מה-OCR
            foreach (var q in grade.QuestionGrades)
            {
                // כאן יש להשתמש בקואורדינטות האמיתיות שהתקבלו מה-AI/OCR
                // כרגע אלו מיקומים מוקדדים לדוגמה
                int mockX = 1200; // שוליים שמאליים
                int mockY = 400 + (new Random().Next(-20, 20)); // הוספת רנדומליות כדי שיראה אמיתי

                if (q.IsCorrect)
                {
                    canvas.DrawText("✓", mockX, mockY, paint);
                }
                else
                {
                    // כתיבת המינוס בכתב יד
                    canvas.DrawText($"-{q.MaxPoints - q.FinalScore}", mockX, mockY, paint);
                }
            }

            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, 85);
            processedPages.Add(data.ToArray());
        }

        // 4. בניית ה-PDF באמצעות QuestPDF
        Document.Create(container =>
        {
            // עמוד 1: דף שער נקי
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontFamily("Assistant").DirectionFromRightToLeft());

                page.Content().Column(col =>
                {
                    col.Item().Text("דוח בדיקת מבחן רשמי").FontSize(28).Bold();
                    col.Item().PaddingTop(20).Text($"שם התלמיד: {grade.StudentName}").FontSize(18);
                    col.Item().Text($"מזהה בחינה: {grade.StudentExamId}").FontSize(14).FontColor(Colors.Grey.Medium);
                    col.Item().Text($"תאריך הפקה: {DateTime.Now:dd/MM/yyyy}").FontSize(14);
                });
                
                page.Footer().AlignCenter().Text("הופק אוטומטית ע\"י מערכת ExamAI").FontSize(10).FontColor(Colors.Grey.Medium);
            });

            // עמודים 2+: דפי המחברת הסרוקים עם ציורי הכתב יד של ה-SkiaSharp
            foreach (var pageBytes in processedPages)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(0, Unit.Centimetre); // פריסה מלאה על הדף
                    page.Content().Image(pageBytes);
                    page.Footer().AlignCenter().Text("הופק אוטומטית ע\"י מערכת ExamAI").FontSize(10).FontColor(Colors.Grey.Medium);
                });
            }

            // עמוד אחרון: טבלת סיכום פדגוגית
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontFamily("Assistant").DirectionFromRightToLeft());

                page.Content().Column(col =>
                {
                    col.Item().Text("פירוט נקודות והערות פדגוגיות").FontSize(20).Bold();
                    
                    col.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3); // שאלה
                            columns.RelativeColumn(1); // ניקוד
                            columns.RelativeColumn(4); // הערה
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("שאלה").Bold();
                            header.Cell().Text("ניקוד").Bold();
                            header.Cell().Text("הערת המערכת").Bold();
                        });

                        foreach (var q in grade.QuestionGrades)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(q.QuestionText);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{q.FinalScore} / {q.MaxPoints}");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(q.Explanation).FontSize(10);
                        }
                    });
                });
            });
        }).GeneratePdf(outputStream);

        outputStream.Position = 0;
        return outputStream;
    }
}