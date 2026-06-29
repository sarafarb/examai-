import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { Router } from '@angular/router';
import { ExportManagerComponent } from '../../../../shared/components/export-manager/export-manager.component';

interface StudentExamRow {
  studentExamId: string;
  studentName: string;
  score: number;
  status: 'ai_graded' | 'under_review' | 'approved';
}

@Component({
  selector: 'app-grades-list',
  standalone: true,
  imports: [
    CommonModule, 
    MatFormFieldModule, 
    MatSelectModule, 
    MatTableModule, 
    MatButtonModule, 
    MatIconModule,
    ExportManagerComponent
  ],
  templateUrl: './grades-list.component.html',
  styleUrls: [] // הוסיפי נתיב ל-CSS/SCSS אם יש צורך
})
export class GradesListComponent implements OnInit {
  classId: string = 'class-computer-science-101'; // מזהה הכיתה הנוכחית
  averageScore: number = 78.4;
  showBatchModal: boolean = false;

  // העמודות שיוצגו בטבלה של Angular Material
  displayedColumns: string[] = ['studentName', 'score', 'status', 'actions'];
  
  // נתוני טבלה לדוגמה
  allData: StudentExamRow[] = [
    { studentExamId: 'exam-1', studentName: 'ישראל ישראלי', score: 92, status: 'approved' },
    { studentExamId: 'exam-2', studentName: 'מיכל לוי', score: 55, status: 'ai_graded' },
    { studentExamId: 'exam-3', studentName: 'דוד כהן', score: 72, status: 'under_review' },
    { studentExamId: 'exam-4', studentName: 'דנה אברהם', score: 88, status: 'approved' }
  ];

  // מקור המידע שמוזן לתוך ה-טבלה ב-HTML
  dataSource: StudentExamRow[] = [];

  constructor(private router: Router) {}

  ngOnInit(): void {
    // אתחול הטבלה עם כל המידע המקורי
    this.dataSource = [...this.allData];
  }

  // סינון הטבלה לפי בחירת הסטטוס מה-Dropdown
  applyFilter(status: string): void {
    if (status === 'all') {
      this.dataSource = [...this.allData];
    } else {
      this.dataSource = this.allData.filter(item => item.status === status);
    }
  }

  // תרגום הסטטוסים מאנגלית לעברית יפה ב-UI
  getStatusLabel(status: string): string {
    const labels: { [key: string]: string } = {
      'ai_graded': 'נבדק ע"י AI',
      'under_review': 'בבדיקה',
      'approved': 'אושר'
    };
    return labels[status] || status;
  }

  // ניווט לעמוד הסקירה של תלמיד ספציפי בלחיצה על כפתור ה-👁️
  goToReview(studentExamId: string): void {
    this.router.navigate(['/grades/review', studentExamId]);
  }

  // מופעל בלחיצה על כפתור ייצוא כל הציון ל-ZIP
  bulkExportPdf(): void {
    this.showBatchModal = true;
  }
}