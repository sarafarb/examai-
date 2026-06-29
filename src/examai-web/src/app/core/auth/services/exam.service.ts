import { Injectable } from '@angular/core';
import { HttpClient, HttpEvent, HttpEventType } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ExamService {
  // החלף ב-URL של ה-API האמיתי שלכם
  private apiUrl = 'http://localhost:5000/api/exams'; 

  constructor(private http: HttpClient) {}

  // פונקציה להעלאת קובץ בודד עם מעקב התקדמות
  uploadStudentExamFile(examId: string, studentName: string, studentId: string, file: File): Observable<HttpEvent<any>> {
    const formData = new FormData();
    formData.append('studentName', studentName);
    formData.append('studentId', studentId);
    formData.append('file', file); // קובץ בודד עבור Parallel Uploads

    return this.http.post(`${this.apiUrl}/${examId}/students/upload`, formData, {
      reportProgress: true,
      observe: 'events' // מאפשר לקבל את ה-Events של ה-Progress
    });
  }
}