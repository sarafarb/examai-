using System;

namespace ExamAI.Shared.Contracts;

// בחירת צבע העט
public enum PenColor
{
    Red,
    Blue,
    Black
}

// בחירת סגנון כתב היד
public enum HandwritingStyle
{
    ClassicTeacher,  // סגנון ברור ומסורתי
    QuickScribble,   // כתב מהיר ומרושל
    ElegantInk       // כתב דק ואלגנטי
}

// הודעת העבודה שתישלח ל-RabbitMQ
public record ExportJobMessage(
    Guid ExportJobId,
    Guid StudentGradeId,
    Guid TeacherId,
    PenColor SelectedColor,
    HandwritingStyle SelectedStyle,
    bool IsBatch,
    Guid? ExamId = null
);