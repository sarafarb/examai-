import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './sidebar.component.html',
})
export class SidebarComponent {
  @Input() role: 'Teacher' | 'Admin' = 'Teacher';
  isCollapsed = false;

  teacherLinks = [
    { path: '/dashboard', icon: '📊', label: 'דשבורד' },
    { path: '/my-exams', icon: '📝', label: 'מבחנים שלי' },
    { path: '/analytics', icon: '📈', label: 'ניתוח כיתה' },
    { path: '/billing', icon: '💳', label: 'חיוב' }
  ];

  adminLinks = [
    { path: '/admin/users', icon: '👥', label: 'ניהול משתמשים' },
    { path: '/admin/subscriptions', icon: '💎', label: 'מנויים' },
    { path: '/admin/settings', icon: '⚙️', label: 'הגדרות מערכת' },
    { path: '/admin/logs', icon: '📜', label: 'לוגים' }
  ];
}