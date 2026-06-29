import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, timer, switchMap, takeWhile } from 'rxjs';
import { ExportSettings, ExportJobResponse, ExportJobStatus } from '../models/export.models';

@Injectable({
  providedIn: 'root'
})
export class ExportService {
  constructor(private http: HttpClient) {}

  // התחלת ייצוא עבור מבחן בודד
  startSingleExport(gradeId: string, settings: ExportSettings): Observable<ExportJobResponse> {
    return this.http.post<ExportJobResponse>(`/api/v1/export/grades/${gradeId}`, settings);
  }

  // התחלת ייצוא קבוצתי (Batch) עבור כיתה שלמה
  startBatchExport(classId: string, settings: ExportSettings): Observable<ExportJobResponse> {
    return this.http.post<ExportJobResponse>(`/api/v1/export/batch/${classId}`, settings);
  }

  // דגימת סטטוס כל 2 שניות (Polling)
  pollExportStatus(jobId: string): Observable<ExportJobStatus> {
    return timer(0, 2000).pipe(
      switchMap(() => this.http.get<ExportJobStatus>(`/api/v1/export/status/${jobId}`)),
      // ממשיך לדגום כל עוד הסטטוס לא הסתיים (בהצלחה או בשגיאה)
      takeWhile(res => res.status !== 'COMPLETED' && res.status !== 'FAILED', true)
    );
  }

  // הורדת הקובץ בדפדפן
  downloadFile(url: string): void {
    window.open(url, '_blank');
  }
}