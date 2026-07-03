import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guards';
import { UiKey } from './core/i18n/ui-text';
import { LEGACY_REDIRECT_ROUTES } from './legacy/legacy-redirect.routes';
export const routes: Routes = [
  {
    path: '',
    data: { seoHome: true },
    loadComponent: () =>
      import('./features/home/home-page.component').then((m) => m.HomePageComponent),
  },
  {
    path: 'shop',
    data: { seoTitle: 'shop' satisfies UiKey },
    loadComponent: () =>
      import('./features/catalogue/shop-page.component').then((m) => m.ShopPageComponent),
  },
  {
    path: 'product/:slug',
    data: { seoDynamic: true },
    loadComponent: () =>
      import('./features/catalogue/product-detail.component').then(
        (m) => m.ProductDetailComponent
      ),
  },
  {
    path: 'cart',
    data: { seoTitle: 'cart' satisfies UiKey },
    loadComponent: () =>
      import('./features/cart/cart-page.component').then((m) => m.CartPageComponent),
  },
  {
    path: 'checkout',
    data: { seoTitle: 'checkout' satisfies UiKey },
    loadComponent: () =>
      import('./features/checkout/checkout-page.component').then((m) => m.CheckoutPageComponent),
  },  {
    path: 'auth',
    loadChildren: () => import('./features/auth/auth.routes').then((m) => m.AUTH_ROUTES),
  },
  {
    path: 'account',
    loadChildren: () => import('./features/account/account.routes').then((m) => m.ACCOUNT_ROUTES),
  },
  {
    path: 'admin',
    loadChildren: () => import('./features/admin/admin.routes').then((m) => m.ADMIN_ROUTES),
  },
  {
    path: 'blog',
    loadChildren: () => import('./features/blog/blog.routes').then((m) => m.BLOG_ROUTES),
  },
  {
    path: 'services',
    loadChildren: () =>
      import('./features/services/services.routes').then((m) => m.SERVICES_ROUTES),
  },
  {
    path: 'flash-sales',
    loadChildren: () =>
      import('./features/promotions/flash-sale.routes').then((m) => m.FLASH_SALE_ROUTES),
  },
  {
    path: 'combos',
    loadChildren: () =>
      import('./features/promotions/combo-offer.routes').then((m) => m.COMBO_OFFER_ROUTES),
  },
  {
    path: 'offers',
    data: { seoTitle: 'offersTitle' satisfies UiKey },
    loadComponent: () =>
      import('./features/promotions/offers-page.component').then((m) => m.OffersPageComponent),
  },
  {
    path: 'wishlist',
    canActivate: [authGuard],
    data: { seoTitle: 'wishlistTitle' satisfies UiKey },
    loadComponent: () =>
      import('./features/wishlist/wishlist-page.component').then((m) => m.WishlistPageComponent),
  },
  {
    path: 'track',
    data: { seoTitle: 'trackOrderTitle' satisfies UiKey },
    loadComponent: () =>
      import('./features/orders/order-tracking.component').then((m) => m.OrderTrackingComponent),
  },  {
    path: 'order/confirmation/:id',
    loadComponent: () =>
      import('./features/orders/order-confirmation.component').then(
        (m) => m.OrderConfirmationComponent
      ),
  },
  {
    path: 'about',
    redirectTo: 'page/about',
    pathMatch: 'full',
  },
  {
    path: 'privacy',
    redirectTo: 'page/privacy',
    pathMatch: 'full',
  },
  {
    path: 'terms',
    redirectTo: 'page/terms',
    pathMatch: 'full',
  },
  {
    path: 'shipping',
    redirectTo: 'page/shipping',
    pathMatch: 'full',
  },
  {
    path: 'return-policy',
    redirectTo: 'page/return-policy',
    pathMatch: 'full',
  },
  {
    path: 'help',
    redirectTo: 'page/help',
    pathMatch: 'full',
  },
  {
    path: 'page',
    data: { seoDynamic: true },
    loadChildren: () => import('./features/content/content.routes').then((m) => m.CONTENT_ROUTES),
  },
  {
    path: 'orders/:orderId/return',
    loadComponent: () =>
      import('./features/returns/request-return.component').then((m) => m.RequestReturnComponent),
  },
  {
    path: 'returns/:id',
    loadComponent: () =>
      import('./features/returns/return-detail.component').then((m) => m.ReturnDetailComponent),
  },
  {
    path: 'newsletter/unsubscribe',
    data: {
      seoTitle: 'newsletterUnsubscribePageTitle' satisfies UiKey,
      seoDescription: 'newsletterUnsubscribePageLead' satisfies UiKey,
    },
    loadComponent: () =>
      import('./features/newsletter/newsletter-unsubscribe.component').then(
        (m) => m.NewsletterUnsubscribeComponent
      ),
  },
  ...LEGACY_REDIRECT_ROUTES,
  {
    path: '**',
    redirectTo: '',
  },
];
