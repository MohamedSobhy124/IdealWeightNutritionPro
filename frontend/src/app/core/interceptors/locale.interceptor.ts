import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { LocaleService } from '../services/locale.service';

export const localeInterceptor: HttpInterceptorFn = (req, next) => {
  const locale = inject(LocaleService);
  return next(
    req.clone({
      setHeaders: {
        'Accept-Language': locale.locale(),
      },
    })
  );
};
