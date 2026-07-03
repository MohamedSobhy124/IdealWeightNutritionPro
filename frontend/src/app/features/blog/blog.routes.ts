import { Routes } from '@angular/router';
import { UiKey } from '../../core/i18n/ui-text';

export const BLOG_ROUTES: Routes = [
  {
    path: '',
    data: { seoTitle: 'blogTitle' satisfies UiKey },
    loadComponent: () =>
      import('./blog-list.component').then((m) => m.BlogListComponent),
  },
  {
    path: ':slug',
    data: { seoDynamic: true },
    loadComponent: () =>
      import('./blog-detail.component').then((m) => m.BlogDetailComponent),
  },
];