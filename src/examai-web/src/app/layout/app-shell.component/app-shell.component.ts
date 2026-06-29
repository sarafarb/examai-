import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { SidebarComponent } from '../sidebar/sidebar.component';
import { NavbarComponent } from '../navbar/navbar.component';
import { BreadcrumbComponent } from '../breadcrumb/breadcrumb.component';

@Component({
  selector: 'app-app-shell',
  standalone: true,
  imports: [CommonModule, RouterModule, SidebarComponent, NavbarComponent, BreadcrumbComponent],
  templateUrl: './app-shell.component.html',
})
export class AppShellComponent {
  userRole: 'Teacher' | 'Admin' = 'Teacher';
  userName = 'שרה';
  isFreePlan = true;

  constructor(private router: Router) {}

  handleLogout(): void {
    this.router.navigate(['/auth/login']);
  }
}