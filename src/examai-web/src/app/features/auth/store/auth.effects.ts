import { Injectable, inject } from '@angular/core'; // <-- הוספנו את inject
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { AuthService } from '../../../core/auth/services/auth.service';
import { TokenStorageService } from '../../../core/auth/services/token-storage.service';
import * as AuthActions from './auth.actions';
import { catchError, map, mergeMap, tap } from 'rxjs/operators';
import { of } from 'rxjs';
import { Router } from '@angular/router';

@Injectable()
export class AuthEffects {

  // 1. גלגל ההצלה: מרימים את ההזרקות ישירות לשדות הפרויקט!
  private actions$ = inject(Actions);
  private authService = inject(AuthService);
  private tokenStorage = inject(TokenStorageService);
  private router = inject(Router);

  register$ = createEffect(() => this.actions$.pipe(
    ofType(AuthActions.registerStart),
    mergeMap(action => this.authService.register(action.payload).pipe(
      map(() => AuthActions.registerSuccess()),
      catchError(err => of(AuthActions.registerFailure({ error: err.error?.error || 'שגיאת רישום לא ידועה' })))
    ))
  ));

  registerSuccess$ = createEffect(() => this.actions$.pipe(
    ofType(AuthActions.registerSuccess),
    tap(() => this.router.navigate(['/auth/verify-email-sent']))
  ), { dispatch: false });

  loginSuccess$ = createEffect(() => this.actions$.pipe(
    ofType(AuthActions.loginSuccess),
    tap(action => this.tokenStorage.setTokens(action.accessToken, action.refreshToken))
  ), { dispatch: false });

  logout$ = createEffect(() => this.actions$.pipe(
    ofType(AuthActions.logout),
    tap(() => {
      this.tokenStorage.clearTokens();
      this.router.navigate(['/auth/login']);
    })
  ), { dispatch: false });

  // 2. הבנאי נשאר ריק ונקי, אין יותר בעיות של סדר אתחול
  constructor() {}
}