import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms'; // מוסיף תמיכה ב-ngModel

@Component({
  selector: 'app-system-config',
  standalone: true,
  imports: [FormsModule], // מייבאים לכאן את הטפסים
  templateUrl: './system-config.component.html',
  styleUrls: ['./system-config.component.scss']
})
export class SystemConfigComponent {
  confidence: number = 85;
  prompt: string = "Act as a professional grader...";

  handleSave(): void {
    if (window.confirm("שים לב! שינוי זה ישפיע על כל הבדיקות החדשות במערכת. להמשיך?")) {
      alert("הגדרות נשמרו בהצלחה!");
    }
  }
}