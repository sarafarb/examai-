import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';

@Component({
  selector: 'app-exam-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './exam-form.component.html'
})
export class ExamFormComponent implements OnInit {
  examForm!: FormGroup;
  isSubmitting = false;
  subjects = ['מתמטיקה', 'עברית', 'היסטוריה', 'מדעים', 'אנגלית', 'אחר'];

  constructor(
    private fb: FormBuilder,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.initForm();
  }

  initForm(): void {
    this.examForm = this.fb.group({
      title: ['', [Validators.required, Validators.minLength(3)]],
      subject: ['', [Validators.required]],
      gradeLevel: ['', [Validators.required]],
      maxScore: [100, [Validators.required, Validators.min(1), Validators.max(1000)]],
      strictness: [50, [Validators.required, Validators.min(0), Validators.max(100)]]
    });
  }

  // חילוץ מהיר עבור שגיאות וולידציה ב-HTML
  get f() { return this.examForm.controls; }

  // חישוב דינמי של טקסט התצוגה המקדימה עבור ה-Slider
  get strictnessPreviewText(): string {
    const value = this.examForm.get('strictness')?.value ?? 50;
    if (value <= 25) {
      return "🤖 חומרת בדיקה קלה (סמנטית): תשובות כמו 'שלוש' יתקבלו במקום '3'. שגיאות כתיב קלות לא יורידו ניקוד במידה והכוונה נכונה.";
    } else if (value <= 75) {
      return "⚖️ חומרת בדיקה מאוזנת: ה-AI יבדוק דיוק עובדתי ורעיוני, אך יתעלם מנוסח מילולי שונה במעט כל עוד הרעיון קיים במלואו.";
    } else {
      return "🔍 חומרת בדיקה קשוחה (מילולית מדויקת): נדרשת התאמה גבוהה מאוד למחוון. שגיאות מושגיות או חוסר במילות מפתח מדויקות יגררו הורדת ציון.";
    }
  }

  onSubmit(): void {
    if (this.examForm.invalid) {
      this.examForm.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    
    // סימולציית שליחה ל-POST /api/v1/exams
    setTimeout(() => {
      const mockCreatedId = 'exam-uuid-12345';
      this.isSubmitting = false;
      
      // ניווט מידי לעמוד ה-Setup כפי שנדרש
      this.router.navigate([`/exams/${mockCreatedId}/setup`]);
    }, 1000);
  }
}