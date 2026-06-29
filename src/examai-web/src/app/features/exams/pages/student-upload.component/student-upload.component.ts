import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { HttpEventType, HttpEvent } from '@angular/common/http';
import { ExamService } from '../../../../core/auth/services/exam.service'; // ודא נתיב נכון לשירות

@Component({
  selector: 'app-student-upload',
  templateUrl: './student-upload.component.html',
  styleUrls: ['./student-upload.component.scss']
})
export class StudentUploadComponent implements OnInit {
  uploadForm: FormGroup;
  examId: string = '';
  selectedFiles: File[] = [];
  
  // ניהול אחוזי ההעלאה לכל קובץ בנפרד (מפתח: שם הקובץ, ערך: אחוזים)
  uploadProgress: { [key: string]: number } = {};
  
  freePagesLeft: number = 15; // דמי - יש לשלוף מיוזר פרופיל/פלן
  totalPagesToUpload: number = 0;
  isDragging: boolean = false;
  isUploading: boolean = false;

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private examService: ExamService
  ) {
    this.uploadForm = this.fb.group({
      studentName: ['', Validators.required],
      studentId: ['']
    });
  }

  ngOnInit(): void {
    this.examId = this.route.snapshot.paramMap.get('id') || '';
  }

  onDragOver(event: DragEvent) {
    event.preventDefault();
    this.isDragging = true;
  }

  onDragLeave(event: DragEvent) {
    event.preventDefault();
    this.isDragging = false;
  }

  onDrop(event: DragEvent) {
    event.preventDefault();
    this.isDragging = false;
    if (event.dataTransfer?.files) {
      this.addFiles(Array.from(event.dataTransfer.files));
    }
  }

  onFileSelected(event: any) {
    if (event.target.files) {
      this.addFiles(Array.from(event.target.files));
    }
  }

  private addFiles(files: File[]) {
    const validFiles = files.filter(f => f.type === 'application/pdf' || f.name.endsWith('.zip'));
    this.selectedFiles.push(...validFiles);
    
    // מאתחל את ה-Progress של הקבצים החדשים ל-0
    validFiles.forEach(f => this.uploadProgress[f.name] = 0);
    this.calculateTotalPages();
  }

  removeFile(index: number) {
    const file = this.selectedFiles[index];
    delete this.uploadProgress[file.name];
    this.selectedFiles.splice(index, 1);
    this.calculateTotalPages();
  }

  private calculateTotalPages() {
    // זמני: מחשב לפי 3 עמודים לקובץ עד שיוטמע ה-pdf.js
    this.totalPagesToUpload = this.selectedFiles.length * 3;
  }

  // --- קריאת ה-POST האמיתית במקביל ---
  onSubmit() {
    if (this.uploadForm.invalid || this.selectedFiles.length === 0) return;

    this.isUploading = true;
    const studentName = this.uploadForm.get('studentName')?.value;
    const studentId = this.uploadForm.get('studentId')?.value;

    // יצירת מערך של פרומיסים/אובזרבבלס כדי לעקוב מתי כולם מסיימים
    const uploadPromises = this.selectedFiles.map(file => {
      return new Promise<void>((resolve, reject) => {
        this.examService.uploadStudentExamFile(this.examId, studentName, studentId, file).subscribe({
          next: (event: HttpEvent<any>) => {
            if (event.type === HttpEventType.UploadProgress && event.total) {
              // עדכון האחוזים בזמן אמת לקובץ הספציפי
              this.uploadProgress[file.name] = Math.round((100 * event.loaded) / event.total);
            } else if (event.type === HttpEventType.Response) {
              this.uploadProgress[file.name] = 100; // סיים בהצלחה
              resolve();
            }
          },
          error: (err) => {
            console.error(`שגיאה בהעלאת הקובץ ${file.name}`, err);
            reject(err);
          }
        });
      });
    });

    // מחכים שכל ההעלאות המקביליות יסתיימו
    Promise.all(uploadPromises)
      .then(() => {
        this.isUploading = false;
        // ניווט חזרה לרשימת המבחנים של הקורס/מבחן הנוכחי
        this.router.navigate([`/exams/${this.examId}/students`]);
      })
      .catch(() => {
        this.isUploading = false;
        alert('חלק מהקבצים נכשלו בהעלאה. אנא נסה שנית.');
      });
  }
}