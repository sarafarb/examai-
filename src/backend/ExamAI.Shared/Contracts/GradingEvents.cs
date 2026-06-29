using System;

namespace ExamAI.Shared.Contracts;

// T-046: האירוע שמופץ ברשת לאחר סיום הבדיקה האוטומטית
public record GradingCompletedEvent(
    Guid StudentExamId,
    Guid ExamId,
    Guid TeacherId,
    string StudentName,
    decimal TotalScore,
    decimal Percentage,
    bool RequiresSpecialReview // True אם יותר מ-30% מהשאלות דורשות בדיקה ידנית
);