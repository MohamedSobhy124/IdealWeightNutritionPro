import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const token = auth.accessToken();

  const isAuthRoute =
    req.url.includes('/auth/login') ||
    req.url.includes('/auth/register') ||
    req.url.includes('/auth/refresh');

  const authedReq =
    token && !isAuthRoute
      ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
      : req;

  return next(authedReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status !== 401 || isAuthRoute || !auth.refreshToken()) {
        return throwError(() => error);
      }

      return auth.refresh().pipe(
        switchMap(() => {
          const retry = req.clone({
            setHeaders: { Authorization: `Bearer ${auth.accessToken()}` },
          });
          return next(retry);
        }),
        catchError((refreshError) => {
          auth.clear();
          return throwError(() => refreshError);
        })
      );
    })
  );
};
