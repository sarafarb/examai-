import { Routes } from '@angular/router';

// 🔐 ייבויי ה-Auth הקיימים שלך
import { RegisterComponent } from './features/auth/pages/register/register.component';
import { LoginComponent } from './features/auth/pages/login/login.component';
import { VerifyEmailSentComponent } from './features/auth/pages/verify-email-sent/verify-email-sent.component';
import { VerifyEmailComponent } from './features/auth/pages/verify-email/verify-email.component';
import { ForgotPasswordComponent } from './features/auth/pages/forgot-password/forgot-password.component';
import { ResetPasswordComponent } from './features/auth/pages/reset-password/reset-password.component';

// 🖥️ ייבויי האפליקציה החדשים (תפריטים ומבחנים)
import { AppShellComponent } from './layout/app-shell.component/app-shell.component';
import { ExamListComponent } from './features/exams/pages/exam-list/exam-list.component';
import { ExamFormComponent } from './features/exams/pages/exam-form/exam-form.component';
import { ExamSetupComponent } from './features/exams/pages/exam-setup/exam-setup.component';
import { BillingComponent } from './features/billing/pages/billing/billing.component';
import { BillingHistoryComponent } from './features/billing/pages/billing-history/billing-history.component';

export const routes: Routes = [
  // 1. נתיבי ה-Auth שלך (נשארים ללא שינוי)
  { path: 'auth/register', component: RegisterComponent },
  { path: 'auth/login', component: LoginComponent },
  { path: 'auth/verify-email-sent', component: VerifyEmailSentComponent },
  { path: 'auth/verify-email', component: VerifyEmailComponent },
  { path: 'auth/forgot-password', component: ForgotPasswordComponent },
  { path: 'auth/reset-password', component: ResetPasswordComponent },
  
  // אם מגיעים לדף הבית הריק, המערכת תפנה אוטומטית למסך ה-Login
  { path: '', redirectTo: 'auth/login', pathMatch: 'full' },

  // 2. נתיבי האפליקציה המרכזיים (עטופים בתוך ה-AppShell שמציג את סרגלי הניווט)
  {
    path: '',
    component: AppShellComponent,
    children: [
      { path: 'exams', component: ExamListComponent },
      { path: 'exams/new', component: ExamFormComponent },
      { path: 'exams/:id/setup', component: ExamSetupComponent },
      { path: 'billing', component: BillingComponent },
      { path: 'billing/history', component: BillingHistoryComponent },

      // ✨ התוספת החדשה: נתיבי הציונים והייצוא (טוען אותם בצורה חכמה - Lazy Loading)
      {
        path: 'grades',
        children: [
          {
            path: 'list/:classId',
            loadComponent: () => import('./features/grading/grades-list/grades-list.component')
              .then(m => m.GradesListComponent)
          },
          {
            path: 'review/:studentGradeId',
            loadComponent: () => import('./features/grading/grade-review-page/grade-review-page.component')
              .then(m => m.GradeReviewPageComponent)
          }
        ]
      }
      
    ]
  },

  // תפיסת בטחון: כל נתיב אחר שלא קיים יחזיר את המשתמש ל-Login
  { path: '**', redirectTo: 'auth/login' }
];