import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ExportManagerComponent } from '../../../shared/components/export-manager/export-manager.component';

@Component({
  selector: 'app-grade-review-page',
  standalone: true,
  imports: [CommonModule, ExportManagerComponent],
  templateUrl: './grade-review-page.component.html'
})
export class GradeReviewPageComponent implements OnInit {
  studentGradeId = 'grade-12345'; // מגיע מה-Route Params במציאות
  gradeStatus = 'approved'; // מגיע מהשרת

  showExportModal = false;

  ngOnInit(): void {
    // טעינת הנתונים מהשרת
  }

  openExportModal(): void {
    this.showExportModal = true;
  }
}