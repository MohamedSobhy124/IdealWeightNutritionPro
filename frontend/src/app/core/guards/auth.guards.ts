import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { UserProfile } from '../models/auth.models';

function staffRoles(profile: UserProfile | null): boolean {
  return !!profile && (profile.roles.includes('Admin') || profile.roles.includes('Employee'));
}

function isAdmin(profile: UserProfile | null): boolean {
  return !!profile?.roles.includes('Admin');
}

function isEmployee(profile: UserProfile | null): boolean {
  return !!profile?.roles.includes('Employee');
}

function ensureProfile(auth: AuthService) {
  const profile = auth.profile();
  if (profile) {
    return of(profile);
  }
  return auth.loadProfile().pipe(
    catchError(() => of(null as UserProfile | null)),
  );
}

export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (auth.isAuthenticated()) return true;
  return router.createUrlTree(['/auth/login']);
};

export const anonymousGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (!auth.isAuthenticated()) return true;
  return router.createUrlTree(['/account']);
};

export const staffGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (!auth.isAuthenticated()) {
    return router.createUrlTree(['/auth/login']);
  }

  const profile = auth.profile();
  if (staffRoles(profile)) return true;
  if (profile) {
    return router.createUrlTree(['/account']);
  }

  return ensureProfile(auth).pipe(
    map((loaded) => (staffRoles(loaded) ? true : router.createUrlTree(['/account']))),
  );
};

/** @deprecated Use staffGuard */
export const adminGuard = staffGuard;

export const adminOnlyGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (!auth.isAuthenticated()) {
    return router.createUrlTree(['/auth/login']);
  }

  const profile = auth.profile();
  if (isAdmin(profile)) return true;
  if (isEmployee(profile)) {
    return router.createUrlTree(['/admin/orders']);
  }
  if (profile) {
    return router.createUrlTree(['/account']);
  }

  return ensureProfile(auth).pipe(
    map((loaded) => {
      if (isAdmin(loaded)) return true;
      if (isEmployee(loaded)) return router.createUrlTree(['/admin/orders']);
      return router.createUrlTree(['/account']);
    }),
  );
};
