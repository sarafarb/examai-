import { Component, OnInit } from '@angular/core';
import { Router , RouterModule} from '@angular/router';
import { CommonModule } from '@angular/common';
@Component({
  selector: 'app-admin-layout',
  standalone: true, // חשוב מאוד באנגולר החדש!
  imports: [RouterModule, CommonModule], // כאן מייבאים את מה שה-HTML צריך
  templateUrl: './admin-layout.component.html',
  styleUrls: ['./admin-layout.component.scss']
})
export class AdminLayoutComponent implements OnInit {
  menu = [
    { name: 'דאשבורד', path: '/admin/dashboard' },
    { name: 'ניהול משתמשים', path: '/admin/users' },
    { name: 'הגדרות מערכת', path: '/admin/config' },
    { name: 'לוג ביקורת (Audit)', path: '/admin/audit' }
  ];

  constructor(private router: Router) {}

  ngOnInit(): void {
    const userRole = localStorage.getItem('user_role') || 'teacher';
    if (userRole !== 'admin') {
      this.router.navigate(['/dashboard']);
    }
  }

  isActive(path: string): boolean {
    return this.router.url.includes(path);
  }
}