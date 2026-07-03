import { Routes } from '@angular/router';
import { anonymousGuard } from '../../core/guards/auth.guards';

export const AUTH_ROUTES: Routes = [
  {
    path: 'oauth-callback',
    loadComponent: () =>
      import('./oauth-callback.component').then((m) => m.OauthCallbackComponent),
  },
  {
    path: 'login',
    canActivate: [anonymousGuard],
    loadComponent: () =>
      import('./login.component').then((m) => m.LoginComponent),
  },
  {
    path: 'register',
    canActivate: [anonymousGuard],
    loadComponent: () =>
      import('./register.component').then((m) => m.RegisterComponent),
  },
  {
    path: 'forgot-password',
    canActivate: [anonymousGuard],
    loadComponent: () =>
      import('./forgot-password.component').then((m) => m.ForgotPasswordComponent),
  },
  {
    path: 'reset-password',
    canActivate: [anonymousGuard],
    loadComponent: () =>
      import('./reset-password.component').then((m) => m.ResetPasswordComponent),
  },
  { path: '', redirectTo: 'login', pathMatch: 'full' },
];
