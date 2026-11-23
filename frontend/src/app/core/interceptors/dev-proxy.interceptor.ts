import { isPlatformBrowser } from '@angular/common';
import { HttpInterceptorFn } from '@angular/common/http';
import { inject, PLATFORM_ID } from '@angular/core';
import { environment } from '../../../environments/environment';

const devApiOrigin = 'https://localhost:7128';

/**
 * The storefront is http://localhost:4200 and the API is https://localhost:7128.
 * A guest-cart cookie set by the API is not sent on that cross-site read, so the
 * browser talks to the API through the dev server proxy and keeps the cookie first-party.
 * Server rendering still calls the API directly.
 */
export const devProxyInterceptor: HttpInterceptorFn = (req, next) => {
  if (environment.production || !isPlatformBrowser(inject(PLATFORM_ID))) {
    return next(req);
  }
  if (!req.url.startsWith(devApiOrigin)) {
    return next(req);
  }
  return next(req.clone({ url: req.url.slice(devApiOrigin.length) }));
};
