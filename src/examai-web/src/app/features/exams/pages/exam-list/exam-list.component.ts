import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';

export interface Exam {
  id: string;
  title: string;
  subject: string;
  gradeLevel: string;
  createdAt: Date;
  studentCount: number;
  status: 'draft' | 'active' | 'grading' | 'completed';
}

@Component({
  selector: 'app-exam-list',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './exam-list.component.html',
})
export class ExamListComponent implements OnInit {
  exams: Exam[] = [];
  filteredExams: Exam[] = [];
  isLoading = true;
  
  // פילטרים וחיפוש
  searchTerm = '';
  statusFilter = '';

  constructor(private router: Router) {}

  ngOnInit(): void {
    this.loadExams();
  }

  loadExams(): void {
    this.isLoading = true;
    // סימולציה של קריאת API של 1.5 שניות בשביל לראות את ה-Skeleton Loading
    setTimeout(() => {
      this.exams = [
        { id: '1', title: 'מבחן אמצע במתמטיקה', subject: 'מתמטיקה', gradeLevel: 'כיתה י\'3', createdAt: new Date('2026-05-10'), studentCount: 28, status: 'active' },
        { id: '2', title: 'בוחן קריאה והבנה - אנטיגונה', subject: 'עברית', gradeLevel: 'כיתה יא\'1', createdAt: new Date('2026-06-01'), studentCount: 32, status: 'draft' },
        { id: '3', title: 'מבחן סוף שנה היסטוריה', subject: 'היסטוריה', gradeLevel: 'כיתה ט\'2', createdAt: new Date('2026-04-15'), studentCount: 24, status: 'completed' },
        { id: '4', title: 'מבחן מכניקה וניוטון', subject: 'מדעים', gradeLevel: 'כיתה יב\'4', createdAt: new Date('2026-06-12'), studentCount: 19, status: 'grading' }
      ];
      this.applyFilters();
      this.isLoading = false;
    }, 1500);
  }

  applyFilters(): void {
    this.filteredExams = this.exams.filter(exam => {
      const matchesSearch = exam.title.toLowerCase().includes(this.searchTerm.toLowerCase());
      const matchesStatus = this.statusFilter === '' || exam.status === this.statusFilter;
      return matchesSearch && matchesStatus;
    });
  }

  getStatusBadgeClass(status: string): string {
    switch (status) {
      case 'draft': return 'bg-gray-100 text-gray-700 border-gray-300';
      case 'active': return 'bg-blue-100 text-blue-700 border-blue-300';
      case 'grading': return 'bg-orange-100 text-orange-700 border-orange-300';
      case 'completed': return 'bg-green-100 text-green-700 border-green-300';
      default: return 'bg-gray-100 text-gray-700';
    }
  }

  getStatusLabel(status: string): string {
    switch (status) {
      case 'draft': return 'טיוטה';
      case 'active': return 'פעיל';
      case 'grading': return 'בבדיקה';
      case 'completed': return 'הושלם';
      default: return status;
    }
  }

  navigateToCreate(): void {
    this.router.navigate(['/exams/new']);
  }

  deleteExam(id: string, event: Event): void {
    event.stopPropagation(); // מניעת מעבר לעמוד המבחן בלחיצה על מחיקה
    if (confirm('האם אתה בטוח שברצונך למחוק מבחן זה?')) {
      this.exams = this.exams.filter(e => e.id !== id);
      this.applyFilters();
    }
  }
}