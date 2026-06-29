import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { Store } from '@ngrx/store';
import { SocialAuthService, GoogleSigninButtonModule } from '@abacritt/angularx-social-login';
import { AuthService } from '../../../../core/auth/services/auth.service';
import { loginSuccess } from '../../store/auth.actions';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, GoogleSigninButtonModule],
  templateUrl: './login.component.html',
  styleUrls: []
})
export class LoginComponent implements OnInit {
  loginForm!: FormGroup;
  loading = false;
  errorMessage: string | null = null;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private socialAuthService: SocialAuthService,
    private store: Store,
    private router: Router
  ) {}

  ngOnInit(): void {
      console.log('ORIGIN =', window.location.origin);

    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required]],
      rememberMe: [false]
    });

    // האזנה להתחברות דרך גוגל
    this.socialAuthService.authState.subscribe({
      next: (user) => {
        // התיקון כאן: אנחנו מוודאים שגם ה-user וגם ה-idToken קיימים!
        if (user && user.idToken) {
          this.loading = true;
          this.authService.googleLogin(user.idToken).subscribe({
            next: (res) => this.handleSuccess(res),
            error: (err) => this.handleError(err)
          });
        }
      }
    });
  }
  onSubmit(): void {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.loading = true;
    this.errorMessage = null;

    this.authService.login(this.loginForm.value).subscribe({
      next: (res) => this.handleSuccess(res),
      error: (err) => this.handleError(err)
    });
  }

  private handleSuccess(res: any): void {
    this.loading = false;
    this.store.dispatch(loginSuccess({
      user: res.user,
      accessToken: res.accessToken,
      refreshToken: res.refreshToken
    }));
    this.router.navigate(['/dashboard']);
  }

  private handleError(err: any): void {
    this.loading = false;
    if (err.status === 401) {
      this.errorMessage = 'אימייל או סיסמה שגויים.';
    } else if (err.status === 403) {
      this.router.navigate(['/auth/verify-email-sent']);
    } else if (err.status === 429) {
      this.errorMessage = 'החשבון ננעל ל-15 דקות עקב יותר מדי ניסיונות כושלים.';
    } else {
      this.errorMessage = 'התרחשה שגיאה בהתחברות, אנא נסה שוב מאוחר יותר.';
    }
  }
}