import { Routes } from '@angular/router';

export const CONTENT_ROUTES: Routes = [
  {
    path: ':slug',
    loadComponent: () =>
      import('./static-page.component').then((m) => m.StaticPageComponent),
  },
];
