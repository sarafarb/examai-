import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ExportManagerComponent } from '../../../shared/components/export-manager/export-manager.component';

@Component({
  selector: 'app-grades-list',
  standalone: true,
  imports: [CommonModule, ExportManagerComponent],
  templateUrl: './grades-list.component.html'
})
export class GradesListComponent {
  classId = 'class-999'; // ה-ID של הכיתה הנוכחית
  showBatchExportModal = false;

  openBatchExportModal(): void {
    this.showBatchExportModal = true;
  }
}