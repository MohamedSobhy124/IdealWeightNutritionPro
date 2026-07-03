import { Routes } from '@angular/router';

export const COMBO_OFFER_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./combo-offer-list.component').then((m) => m.ComboOfferListComponent),
  },
  {
    path: ':id',
    loadComponent: () =>
      import('./combo-offer-detail.component').then((m) => m.ComboOfferDetailComponent),
  },
];
