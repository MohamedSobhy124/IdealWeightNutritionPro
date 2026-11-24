import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { UiToastService } from '../../shared/ui/ui-toast.service';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const toast = inject(UiToastService);
  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      console.error('[API]', error.status, error.message);
      if (error.status >= 500) {
        toast.error('Something went wrong on our side. Please try again. / حدث خطأ ما، يرجى المحاولة لاحقاً.');
      }
      return throwError(() => error);
    })
  );
};
