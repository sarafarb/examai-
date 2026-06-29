import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { ExportManagerComponent } from '../../../../shared/components/export-manager/export-manager.component';

interface Annotation {
  x: number;
  y: number;
  isCorrect: boolean;
  questionId: string;
}

interface QuestionGrade {
  id: string;
  questionText: string;
  finalScore: number;
  maxPoints: number;
  explanation: string;
}

@Component({
  selector: 'app-grade-review',
  standalone: true,
  imports: [CommonModule, MatButtonModule, ExportManagerComponent],
  templateUrl: './grade-review.component.html',
  styleUrls: [] // הוסיפי נתיב ל-CSS/SCSS אם יש צורך
})
export class GradeReviewPageComponent implements OnInit {
  // נתוני המבחן והתלמיד (במציאות יגיעו מ-Service דרך ה-ActivatedRoute)
  studentGradeId: string = 'grade-123'; 
  studentName: string = 'שירה כהן';
  studentId: string = '312456789';
  examImageUrl: string = 'assets/images/sample-exam.png'; // נתיב זמני לתמונה
  totalScore: number = 85;
  gradeStatus: 'ai_graded' | 'under_review' | 'approved' = 'ai_graded';

  // שליטה בהצגת מודאל הייצוא
  showExportModal: boolean = false;

  // רשימת הסימונים על גבי תמונת המבחן
  annotations: Annotation[] = [
    { x: 150, y: 200, isCorrect: true, questionId: 'q1' },
    { x: 150, y: 450, isCorrect: false, questionId: 'q2' }
  ];

  // רשימת השאלות וההסברים של ה-AI
  questionGrades: QuestionGrade[] = [
    {
      id: 'q1',
      questionText: 'שאלה 1: מהו הסיבוכיות של מיון בועות במקרה הגרוע?',
      finalScore: 50,
      maxPoints: 50,
      explanation: 'התלמיד ענה נכון וסיפק הסבר מלא לגבי לולאות מקוננות.'
    },
    {
      id: 'q2',
      questionText: 'שאלה 2: הסבר את המושג רקורסיה ותן דוגמה.',
      finalScore: 35,
      maxPoints: 50,
      explanation: 'ההגדרה של הרקורסיה נכונה, אך דוגמת הקוד לא מתקמפלת ומכילה לולאה אינסופית.'
    }
  ];

  constructor() {}

  ngOnInit(): void {
    // כאן תוכלי למשוך את הנתונים האמיתיים מה-API בעזרת ה-ID מה-URL
  }

  // גלילה חלקה אל השאלה שנלחצה מתוך התמונה
  scrollToQuestion(questionId: string): void {
    const element = document.getElementById(`question-${questionId}`);
    if (element) {
      element.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }
  }

  // פונקציית עזר לקביעת צבע הציון הכללי
  getScoreColor(score: number): string {
    if (score >= 85) return 'text-green-600';
    if (score >= 60) return 'text-orange-500';
    return 'text-red-600';
  }

  // פתיחת מודאל לשינוי ידני של הציון
  openOverrideModal(question: QuestionGrade): void {
    console.log('פתיחת מודאל שינוי ציון עבור שאלה:', question.id);
    // לוגיקת שינוי ציון תתווסף כאן בהמשך
  }

  // אישור הציון הסופי והעברת הסטטוס ל-approved
  approveGrade(): void {
    this.gradeStatus = 'approved';
    console.log('הציון אושר בהצלחה!');
  }
}