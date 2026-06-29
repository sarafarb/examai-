import { ApplicationConfig, importProvidersFrom } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { provideStore } from '@ngrx/store';
import { provideEffects } from '@ngrx/effects';

import { routes } from './app.routes';
import { authReducer } from './features/auth/store/auth.reducer';
import { AuthEffects } from './features/auth/store/auth.effects';

import {
  SocialLoginModule,
  SocialAuthServiceConfig,
  SOCIAL_AUTH_CONFIG,
  GoogleLoginProvider
} from '@abacritt/angularx-social-login';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(withInterceptorsFromDi()),
    provideStore({ auth: authReducer }),
    provideEffects([AuthEffects]),

    // ✅ מייבא את תשתית הספרייה (Directives, Services) לתוך ה-root injector
    importProvidersFrom(SocialLoginModule),

   
      // ✅ as any פותר את הקו האדום — זה InjectionToken לגיטימי בזמן ריצה
      {
provide: SOCIAL_AUTH_CONFIG,
      useValue: {
        autoLogin: false,
        providers: [
          {
            id: GoogleLoginProvider.PROVIDER_ID,
            provider: new GoogleLoginProvider(
              '1046078199286-gsrt3u71gf0lkdaotnm0hk5sk9sabbf9.apps.googleusercontent.com',
              {
                oneTapEnabled: false, // כיבוי חלון ה-FedCM המרחף שגורם לתקיעות
                prompt: 'select_account' // מכריח את גוגל להציג חלון בחירת חשבון
              }
            )
          }
        ],
        onError: (err: any) => console.error('Google Auth Error:', err)
      } as SocialAuthServiceConfig
    }
  ]
};