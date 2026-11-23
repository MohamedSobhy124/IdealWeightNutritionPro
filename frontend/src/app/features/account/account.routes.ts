import { Routes } from '@angular/router';
import { authGuard } from '../../core/guards/auth.guards';

export const ACCOUNT_ROUTES: Routes = [
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./account-shell.component').then((m) => m.AccountShellComponent),
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./account-dashboard.component').then((m) => m.AccountDashboardComponent),
      },
      {
        path: 'orders',
        loadComponent: () =>
          import('./my-orders.component').then((m) => m.MyOrdersComponent),
      },
      {
        path: 'subscriptions',
        loadComponent: () =>
          import('./my-subscriptions.component').then((m) => m.MySubscriptionsComponent),
      },
      {
        path: 'returns',
        loadComponent: () =>
          import('../returns/my-returns.component').then((m) => m.MyReturnsComponent),
      },
    ],
  },
];
