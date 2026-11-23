import { IMAGE_LOADER, ImageLoaderConfig } from '@angular/common';
import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideCharts, withDefaultRegisterables } from 'ng2-charts';

import { routes } from './app.routes';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { devProxyInterceptor } from './core/interceptors/dev-proxy.interceptor';
import { errorInterceptor } from './core/interceptors/error.interceptor';
import { localeInterceptor } from './core/interceptors/locale.interceptor';
import { provideClientHydration, withEventReplay } from '@angular/platform-browser';

/** Passthrough loader so NgOptimizedImage accepts absolute CDN / legacy asset URLs as-is. */
const passthroughImageLoader = (config: ImageLoaderConfig): string => config.src;

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideAnimationsAsync(),
    provideCharts(withDefaultRegisterables()),
    provideHttpClient(
      withFetch(),
      withInterceptors([devProxyInterceptor, localeInterceptor, authInterceptor, errorInterceptor])
    ),
    provideClientHydration(withEventReplay()),
    { provide: IMAGE_LOADER, useValue: passthroughImageLoader },
  ],
};
