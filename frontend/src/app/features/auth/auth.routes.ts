import { Routes } from '@angular/router';
import { anonymousGuard } from '../../core/guards/auth.guards';
import { UiKey } from '../../core/i18n/ui-text';

export const AUTH_ROUTES: Routes = [
  {
    path: 'oauth-callback',
    loadComponent: () =>
      import('./oauth-callback.component').then((m) => m.OauthCallbackComponent),
  },
  {
    path: 'login',
    canActivate: [anonymousGuard],
    data: { seoTitle: 'login' satisfies UiKey },
    loadComponent: () =>
      import('./login.component').then((m) => m.LoginComponent),
  },
  {
    path: 'register',
    canActivate: [anonymousGuard],
    data: { seoTitle: 'registerTitle' satisfies UiKey },
    loadComponent: () =>
      import('./register.component').then((m) => m.RegisterComponent),
  },
  {
    path: 'forgot-password',
    canActivate: [anonymousGuard],
    data: { seoTitle: 'forgotPassword' satisfies UiKey },
    loadComponent: () =>
      import('./forgot-password.component').then((m) => m.ForgotPasswordComponent),
  },
  {
    path: 'reset-password',
    canActivate: [anonymousGuard],
    data: { seoTitle: 'resetPasswordTitle' satisfies UiKey },
    loadComponent: () =>
      import('./reset-password.component').then((m) => m.ResetPasswordComponent),
  },
  { path: '', redirectTo: 'login', pathMatch: 'full' },
];
