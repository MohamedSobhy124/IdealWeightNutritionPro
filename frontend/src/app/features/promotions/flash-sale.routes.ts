import { Routes } from '@angular/router';

export const FLASH_SALE_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./flash-sale-list.component').then((m) => m.FlashSaleListComponent),
  },
  {
    path: ':id',
    loadComponent: () =>
      import('./flash-sale-detail.component').then((m) => m.FlashSaleDetailComponent),
  },
];
