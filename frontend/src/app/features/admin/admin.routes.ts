import { Routes } from '@angular/router';
import { adminOnlyGuard, staffGuard } from '../../core/guards/auth.guards';

export const ADMIN_ROUTES: Routes = [
  {
    path: '',
    canActivate: [staffGuard],
    loadComponent: () =>
      import('./admin-shell.component').then((m) => m.AdminShellComponent),
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./dashboard/admin-dashboard.component').then((m) => m.AdminDashboardComponent),
      },
      {
        path: 'orders',
        loadComponent: () =>
          import('./orders/admin-orders.component').then((m) => m.AdminOrdersComponent),
      },
      {
        path: 'orders/:id',
        loadComponent: () =>
          import('./orders/admin-order-detail.component').then((m) => m.AdminOrderDetailComponent),
      },
      {
        path: 'products',
        canActivate: [adminOnlyGuard],
        loadComponent: () =>
          import('./products/admin-products.component').then((m) => m.AdminProductsComponent),
      },
      {
        path: 'products/new',
        canActivate: [adminOnlyGuard],
        loadComponent: () =>
          import('./products/admin-product-create.component').then(
            (m) => m.AdminProductCreateComponent
          ),
      },
      {
        path: 'products/:id',
        canActivate: [adminOnlyGuard],
        loadComponent: () =>
          import('./products/admin-product-detail.component').then(
            (m) => m.AdminProductDetailComponent
          ),
      },
      {
        path: 'stock-notifications',
        loadComponent: () =>
          import('./stock-notifications/admin-stock-notifications.component').then(
            (m) => m.AdminStockNotificationsComponent
          ),
      },
      {
        path: 'returns',
        loadComponent: () =>
          import('./returns/admin-returns.component').then((m) => m.AdminReturnsComponent),
      },
      {
        path: 'returns/:id',
        loadComponent: () =>
          import('./returns/admin-return-detail.component').then((m) => m.AdminReturnDetailComponent),
      },
      {
        path: 'reviews',
        loadComponent: () =>
          import('./reviews/admin-reviews.component').then((m) => m.AdminReviewsComponent),
      },
      {
        path: 'newsletter',
        canActivate: [adminOnlyGuard],
        loadComponent: () =>
          import('./newsletter/admin-newsletter.component').then((m) => m.AdminNewsletterComponent),
      },
      {
        path: 'promo-codes',
        canActivate: [adminOnlyGuard],
        loadComponent: () =>
          import('./promo-codes/admin-promo-codes.component').then((m) => m.AdminPromoCodesComponent),
      },
      {
        path: 'promo-codes/new',
        canActivate: [adminOnlyGuard],
        loadComponent: () =>
          import('./promo-codes/admin-promo-form.component').then((m) => m.AdminPromoFormComponent),
      },
      {
        path: 'promo-codes/:id',
        canActivate: [adminOnlyGuard],
        loadComponent: () =>
          import('./promo-codes/admin-promo-form.component').then((m) => m.AdminPromoFormComponent),
      },
      {
        path: 'categories',
        canActivate: [adminOnlyGuard],
        loadComponent: () =>
          import('./catalogue/admin-categories.component').then((m) => m.AdminCategoriesComponent),
      },
      {
        path: 'brands',
        canActivate: [adminOnlyGuard],
        loadComponent: () =>
          import('./catalogue/admin-brands.component').then((m) => m.AdminBrandsComponent),
      },
      {
        path: 'flash-sales',
        canActivate: [adminOnlyGuard],
        loadComponent: () =>
          import('./flash-sales/admin-flash-sales.component').then((m) => m.AdminFlashSalesComponent),
      },
      {
        path: 'flash-sales/new',
        canActivate: [adminOnlyGuard],
        loadComponent: () =>
          import('./flash-sales/admin-flash-sale-form.component').then((m) => m.AdminFlashSaleFormComponent),
      },
      {
        path: 'flash-sales/:id',
        canActivate: [adminOnlyGuard],
        loadComponent: () =>
          import('./flash-sales/admin-flash-sale-form.component').then((m) => m.AdminFlashSaleFormComponent),
      },
      {
        path: 'combo-offers',
        canActivate: [adminOnlyGuard],
        loadComponent: () =>
          import('./combo-offers/admin-combo-offers.component').then((m) => m.AdminComboOffersComponent),
      },
      {
        path: 'combo-offers/new',
        canActivate: [adminOnlyGuard],
        loadComponent: () =>
          import('./combo-offers/admin-combo-offer-form.component').then((m) => m.AdminComboOfferFormComponent),
      },
      {
        path: 'combo-offers/:id',
        canActivate: [adminOnlyGuard],
        loadComponent: () =>
          import('./combo-offers/admin-combo-offer-form.component').then((m) => m.AdminComboOfferFormComponent),
      },
      {
        path: 'cities',
        canActivate: [adminOnlyGuard],
        loadComponent: () =>
          import('./delivery/admin-cities.component').then((m) => m.AdminCitiesComponent),
      },
      {
        path: 'cities/:id/areas',
        canActivate: [adminOnlyGuard],
        loadComponent: () =>
          import('./delivery/admin-city-areas.component').then((m) => m.AdminCityAreasComponent),
      },
      {
        path: 'blog',
        canActivate: [adminOnlyGuard],
        loadComponent: () =>
          import('./blog/admin-blog-posts.component').then((m) => m.AdminBlogPostsComponent),
      },
      {
        path: 'blog/new',
        canActivate: [adminOnlyGuard],
        loadComponent: () =>
          import('./blog/admin-blog-form.component').then((m) => m.AdminBlogFormComponent),
      },
      {
        path: 'blog/:id',
        canActivate: [adminOnlyGuard],
        loadComponent: () =>
          import('./blog/admin-blog-form.component').then((m) => m.AdminBlogFormComponent),
      },
      {
        path: 'services',
        canActivate: [adminOnlyGuard],
        loadComponent: () =>
          import('./services/admin-services.component').then((m) => m.AdminServicesComponent),
      },
      {
        path: 'services/new',
        canActivate: [adminOnlyGuard],
        loadComponent: () =>
          import('./services/admin-service-form.component').then((m) => m.AdminServiceFormComponent),
      },
      {
        path: 'services/:id',
        canActivate: [adminOnlyGuard],
        loadComponent: () =>
          import('./services/admin-service-form.component').then((m) => m.AdminServiceFormComponent),
      },
      {
        path: 'service-offers',
        canActivate: [adminOnlyGuard],
        loadComponent: () =>
          import('./service-offers/admin-service-offers.component').then((m) => m.AdminServiceOffersComponent),
      },
      {
        path: 'service-offers/new',
        canActivate: [adminOnlyGuard],
        loadComponent: () =>
          import('./service-offers/admin-service-offer-form.component').then((m) => m.AdminServiceOfferFormComponent),
      },
      {
        path: 'service-offers/:id',
        canActivate: [adminOnlyGuard],
        loadComponent: () =>
          import('./service-offers/admin-service-offer-form.component').then((m) => m.AdminServiceOfferFormComponent),
      },
      {
        path: 'service-purchases',
        canActivate: [adminOnlyGuard],
        loadComponent: () =>
          import('./service-purchases/admin-service-purchases.component').then((m) => m.AdminServicePurchasesComponent),
      },
      {
        path: 'service-purchases/:id',
        canActivate: [adminOnlyGuard],
        loadComponent: () =>
          import('./service-purchases/admin-service-purchase-detail.component').then(
            (m) => m.AdminServicePurchaseDetailComponent
          ),
      },
      {
        path: 'companies',
        canActivate: [adminOnlyGuard],
        loadComponent: () =>
          import('./companies/admin-companies.component').then((m) => m.AdminCompaniesComponent),
      },
      {
        path: 'users',
        canActivate: [adminOnlyGuard],
        loadComponent: () =>
          import('./users/admin-users.component').then((m) => m.AdminUsersComponent),
      },
      {
        path: 'video-banner',
        canActivate: [adminOnlyGuard],
        loadComponent: () =>
          import('./video-banner/admin-video-banner.component').then((m) => m.AdminVideoBannerComponent),
      },
    ],
  },
];
