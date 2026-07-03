import { Routes } from '@angular/router';

export const SERVICES_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./service-list.component').then((m) => m.ServiceListComponent),
  },
  {
    path: 'confirmation/:purchaseId',
    loadComponent: () =>
      import('./service-confirmation.component').then((m) => m.ServiceConfirmationComponent),
  },
  {
    path: ':id/checkout',
    loadComponent: () =>
      import('./service-checkout-page.component').then((m) => m.ServiceCheckoutPageComponent),
  },
  {
    path: ':id',
    loadComponent: () =>
      import('./service-detail.component').then((m) => m.ServiceDetailComponent),
  },
];
