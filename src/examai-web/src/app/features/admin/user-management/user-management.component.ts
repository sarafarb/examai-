import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
@Component({
  selector: 'app-user-management',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './user-management.component.html',
  styleUrls: ['./user-management.component.scss']
})
export class UserManagementComponent {
  users = [
    { id: '1', name: 'משה כהן', email: 'moshe@school.com', role: 'teacher', status: 'active', plan: 'pro' },
    { id: '2', name: 'דנה לוי', email: 'dana@admin.com', role: 'admin', status: 'active', plan: 'enterprise' }
  ];

  handleDelete(id: string): void {
    if (window.confirm('פעולה הרסנית: האם אתה בטוח שברצונך למחוק משתמש זה?')) {
      this.users = this.users.filter(u => u.id !== id);
    }
  }
}