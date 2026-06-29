import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

@Component({
  selector: 'app-exam-setup',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './exam-setup.component.html'
})
export class ExamSetupComponent implements OnInit {
  examId: string = '';
  currentStep: number = 1;
  
  // מצבי טעינה
  isUploadingTemplate = false;
  isUploadingAnswerKey = false;
  
  // נתונים שחולצו
  extractedQuestions: any[] = [];
  rubricItems: any[] = [];

  constructor(private route: ActivatedRoute, private router: Router) {}

  ngOnInit(): void {
    this.examId = this.route.snapshot.paramMap.get('id') || '';
  }

  // --- Step 1: Template ---
  onTemplateSelected(event: any): void {
    const file = event.target.files[0];
    if (!file) return;

    this.isUploadingTemplate = true;
    
    // סימולציית העלאה ו-Polling של סטטוס (מחכה 3 שניות)
    setTimeout(() => {
      this.extractedQuestions = [
        { number: 1, text: 'מהי בירת צרפת?', maxPoints: 10 },
        { number: 2, text: 'הסבר את חוק ניוטון השני.', maxPoints: 20 }
      ];
      this.isUploadingTemplate = false;
    }, 3000);
  }

  // --- Step 2: Answer Key ---
  onAnswerKeySelected(event: any): void {
    const file = event.target.files[0];
    if (!file) return;

    this.isUploadingAnswerKey = true;

    // סימולציית בניית רובריקה מבוססת AI (מחכה 3 שניות)
    setTimeout(() => {
      this.rubricItems = [
        { questionNumber: 1, correctAnswer: 'פריז', alternativeAnswers: 'עיר האורות', gradingNotes: 'אין להוריד ניקוד על שגיאות כתיב', points: 10 },
        { questionNumber: 2, correctAnswer: 'F=ma', alternativeAnswers: 'כוח שווה מסה כפול תאוצה', gradingNotes: 'חובה לציין את הנוסחה', points: 20 }
      ];
      this.isUploadingAnswerKey = false;
    }, 3000);
  }

  // --- Step 3: Rubric Edit ---
  addAlternative(item: any, inputElement: HTMLInputElement): void {
    if (inputElement.value.trim()) {
      // כאן אנחנו מפשטים את המערך למחרוזת מופרדת בפסיקים לצורך ה-UI, במציאות זה מערך
      item.alternativeAnswers += (item.alternativeAnswers ? ', ' : '') + inputElement.value.trim();
      inputElement.value = '';
    }
  }

  saveSetup(): void {
    // סימולציית שמירה
    alert('ההגדרות נשמרו בהצלחה! המבחן כעת מופעל (Active).');
    this.router.navigate(['/exams']);
  }

  // ניווט באשף
  nextStep(): void {
    if (this.currentStep < 3) this.currentStep++;
  }

  prevStep(): void {
    if (this.currentStep > 1) this.currentStep--;
  }
}