using Docnet.Core;
using Docnet.Core.Models;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Tesseract;

namespace ExamAI.OCR.Worker.Services;

public interface IImagePreprocessingService
{
    Task<List<PreprocessedPageResult>> ProcessPdfAsync(Stream pdfStream);
}

public class ImagePreprocessingService : IImagePreprocessingService
{
    private readonly TesseractEngine _tesseractOsdEngine;

    public ImagePreprocessingService()
    {
        // אתחול מנוע Tesseract במצב OSD (זיהוי כיוון בלבד)
        _tesseractOsdEngine = new TesseractEngine(@"./tessdata", "osd", EngineMode.Default);
    }

    public async Task<List<PreprocessedPageResult>> ProcessPdfAsync(Stream pdfStream)
    {
        var results = new List<PreprocessedPageResult>();

        // המרת ה-Stream למערך בתים (byte array) שנתמך ב-Docnet
        using var ms = new MemoryStream();
        await pdfStream.CopyToAsync(ms);
        byte[] pdfBytes = ms.ToArray();

        // שימוש ב-GetDocReader עם מידות דף דינמיות
        using var docReader = DocLib.Instance.GetDocReader(pdfBytes, new PageDimensions());
        int pageCount = docReader.GetPageCount();

        for (int i = 0; i < pageCount; i++)
        {
            using var pageReader = docReader.GetPageReader(i);
            
            // קבלת ה-Bytes של התמונה בצורה נקייה
            var rawBytes = pageReader.GetImage(); 
            var width = pageReader.GetPageWidth();
            var height = pageReader.GetPageHeight();

            // טעינה ל-OpenCV בעזרת FromPixelData
            using var bgraMat = Mat.FromPixelData(height, width, MatType.CV_8UC4, rawBytes);
            using var grayMat = new Mat();
            
            // המרה לשחור-לבן לצורך עיבוד תמונה אופטימלי
            Cv2.CvtColor(bgraMat, grayMat, ColorConversionCodes.BGRA2GRAY);

            var processedResult = ProcessSingleImage(grayMat);
            results.Add(processedResult);
        }

        return results;
    }

    private PreprocessedPageResult ProcessSingleImage(Mat srcMat)
    {
        var result = new PreprocessedPageResult();
        using var processedMat = new Mat();

        // 1. OSD: בדיקת כיוון ותיקון
        result.OrientationCorrected = CorrectOrientation(srcMat, processedMat);
        var workingMat = result.OrientationCorrected ? processedMat : srcMat;

        // 2. Deskew (יישור זווית)
        result.DeskewApplied = DeskewImage(workingMat, workingMat);

        // 3. Noise Removal & Contrast
        ApplyFilters(workingMat);

        // המרה חזרה ל-PNG
        result.ImageBytes = workingMat.ToBytes(".png");
        return result;
    }

    private bool DeskewImage(Mat src, Mat dst)
    {
        using var edges = new Mat();
        Cv2.Canny(src, edges, 50, 200, 3);
        
        var lines = Cv2.HoughLinesP(edges, 1, Math.PI / 180, 100, 100, 10);
        if (lines.Length == 0) { if (src != dst) src.CopyTo(dst); return false; }

        double angle = 0;
        foreach (var line in lines)
        {
            angle += Math.Atan2(line.P2.Y - line.P1.Y, line.P2.X - line.P1.X);
        }
        angle /= lines.Length;
        angle = angle * (180 / Math.PI);

        if (Math.Abs(angle) > 0.5 && Math.Abs(angle) < 45)
        {
            var center = new Point2f(src.Cols / 2f, src.Rows / 2f);
            using var rotationMatrix = Cv2.GetRotationMatrix2D(center, angle, 1.0);
            Cv2.WarpAffine(src, dst, rotationMatrix, src.Size(), InterpolationFlags.Cubic, BorderTypes.Replicate);
            return true;
        }

        if (src != dst) src.CopyTo(dst);
        return false;
    }

    private bool CorrectOrientation(Mat src, Mat dst)
    {
        try
        {
            using var pix = Pix.LoadFromMemory(src.ToBytes(".png"));
            using var page = _tesseractOsdEngine.Process(pix, PageSegMode.OsdOnly);
            
            // פתרון בטוח: קריאת נתוני ה-OSD כטקסט ישיר למניעת שגיאות קומפילציה
            string osdText = page.GetText();
            
            if (!string.IsNullOrEmpty(osdText))
            {
                // אם הדף מסובב ב-90 מעלות
                if (osdText.Contains("degrees: 90") || osdText.Contains("Orientation: 1"))
                {
                    Cv2.Rotate(src, dst, RotateFlags.Rotate90Clockwise);
                    return true;
                }
                // אם הדף הפוך לחלוטין (180 מעלות)
                if (osdText.Contains("degrees: 180") || osdText.Contains("Orientation: 2"))
                {
                    Cv2.Rotate(src, dst, RotateFlags.Rotate180);
                    return true;
                }
                // אם הדף מסובב ב-270 מעלות
                if (osdText.Contains("degrees: 270") || osdText.Contains("Orientation: 3"))
                {
                    Cv2.Rotate(src, dst, RotateFlags.Rotate90Counterclockwise);
                    return true;
                }
            }
        }
        catch
        {
            // פולבק בטיחותי למקרה של כשל במנוע ה-OSD
        }

        if (src != dst) src.CopyTo(dst);
        return false;
    }

    private void ApplyFilters(Mat mat)
    {
        Cv2.EqualizeHist(mat, mat);
        using var median = new Mat();
        using var bilateral = new Mat();
        Cv2.MedianBlur(mat, median, 3);
        Cv2.BilateralFilter(median, bilateral, 9, 75, 75);
        bilateral.CopyTo(mat);
    }
}

public class PreprocessedPageResult
{
    public byte[] ImageBytes { get; set; } = Array.Empty<byte>();
    public bool DeskewApplied { get; set; }
    public bool OrientationCorrected { get; set; }
}