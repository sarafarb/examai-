import { Component, Input, Output, EventEmitter, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, takeUntil, switchMap } from 'rxjs';
import { ExportService } from '../../../core/services/export.service';
import { ExportSettings } from '../../../core/models/export.models';

@Component({
  selector: 'app-export-manager',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './export-manager.component.html',
  styleUrls: ['./export-manager.component.scss']
})
export class ExportManagerComponent implements OnDestroy {
  @Input() mode: 'SINGLE' | 'BATCH' = 'SINGLE';
  @Input() targetId!: string; // gradeId or classId
  @Output() close = new EventEmitter<void>();

  // הגדרות המודאל
  penColor: 'Red' | 'Blue' | 'Black' = 'Blue';
  handwritingStyle: 'Classic' | 'Quick Scribble' | 'Elegant' = 'Classic';

  // ניהול מצב
  state: 'SETTINGS' | 'PROCESSING' | 'ERROR' = 'SETTINGS';
  progress = 0;
  errorMessage = '';
  
  private destroy$ = new Subject<void>();

  constructor(private exportService: ExportService) {}

  startExport(): void {
    this.state = 'PROCESSING';
    this.errorMessage = '';
    this.progress = 0;

    const settings: ExportSettings = {
      penColor: this.penColor,
      handwritingStyle: this.handwritingStyle
    };

    const request$ = this.mode === 'SINGLE' 
      ? this.exportService.startSingleExport(this.targetId, settings)
      : this.exportService.startBatchExport(this.targetId, settings);

    request$.pipe(
      switchMap(res => this.exportService.pollExportStatus(res.exportJobId)),
      takeUntil(this.destroy$)
    ).subscribe({
      next: (status) => {
        if (status.status === 'PROCESSING') {
          this.progress = status.progressPercentage || 0;
        } else if (status.status === 'COMPLETED') {
          if (status.downloadUrl) {
            this.exportService.downloadFile(status.downloadUrl);
          }
          this.closeModal(); // סגירה אוטומטית אחרי הורדה
        } else if (status.status === 'FAILED') {
          this.state = 'ERROR';
          this.errorMessage = status.errorMessage || 'אירעה שגיאה בלתי צפויה במהלך יצירת ה-PDF.';
        }
      },
      error: (err) => {
        this.state = 'ERROR';
        this.errorMessage = 'שגיאת תקשורת מול השרת. אנא בדוק את החיבור.';
      }
    });
  }

  resetFlow(): void {
    this.state = 'SETTINGS';
    this.errorMessage = '';
    this.progress = 0;
  }

  closeModal(): void {
    this.destroy$.next();
    this.close.emit();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}