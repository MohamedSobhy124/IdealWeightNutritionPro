import { Routes } from '@angular/router';

export const LEGACY_REDIRECT_ROUTES: Routes = [
  { path: 'Customer/Home/Details/:slug', redirectTo: 'product/:slug', pathMatch: 'full' },
  { path: 'Customer/Home/Index', redirectTo: '', pathMatch: 'full' },
  {
    path: 'Customer/Home',
    loadComponent: () =>
      import('./legacy-home-redirect.component').then((m) => m.LegacyHomeRedirectComponent),
  },
  { path: 'Customer/Shop', redirectTo: 'shop', pathMatch: 'full' },
  { path: 'Customer/Home/AboutUs', redirectTo: 'page/about', pathMatch: 'full' },
  { path: 'Customer/Home/Privacy', redirectTo: 'page/privacy', pathMatch: 'full' },
  { path: 'Customer/Home/PrivacyPolicy', redirectTo: 'page/privacy', pathMatch: 'full' },
  { path: 'Customer/Home/Terms', redirectTo: 'page/terms', pathMatch: 'full' },
  { path: 'Customer/Home/Shipping', redirectTo: 'page/shipping', pathMatch: 'full' },
  { path: 'Customer/Home/Returns', redirectTo: 'page/return-policy', pathMatch: 'full' },
  { path: 'Customer/Home/HelpCenter', redirectTo: 'page/help', pathMatch: 'full' },
  { path: 'Customer/Home/TrackOrder', redirectTo: 'track', pathMatch: 'full' },
  { path: 'Customer/Blog/Details/:slug', redirectTo: 'blog/:slug', pathMatch: 'full' },
  { path: 'Customer/Blog', redirectTo: 'blog', pathMatch: 'full' },
  { path: 'Customer/FlashSale/Details/:id', redirectTo: 'flash-sales/:id', pathMatch: 'full' },
  { path: 'Customer/FlashSale', redirectTo: 'flash-sales', pathMatch: 'full' },
  { path: 'Customer/ComboOffer/Details/:id', redirectTo: 'combos/:id', pathMatch: 'full' },
  { path: 'Customer/ComboOffer', redirectTo: 'combos', pathMatch: 'full' },
  { path: 'Customer/Offer', redirectTo: 'offers', pathMatch: 'full' },
  {
    path: 'Customer/Cart/OrderConfirmation/:id',
    redirectTo: 'order/confirmation/:id',
    pathMatch: 'full',
  },
  { path: 'Customer/Cart/Summary', redirectTo: 'checkout', pathMatch: 'full' },
  { path: 'Customer/Cart', redirectTo: 'cart', pathMatch: 'full' },
  {
    path: 'Customer/ServiceSubscription/Details/:id',
    redirectTo: 'services/:id',
    pathMatch: 'full',
  },
  { path: 'Customer/ServiceSubscription', redirectTo: 'services', pathMatch: 'full' },
  { path: 'Customer/Account/Orders', redirectTo: 'account/orders', pathMatch: 'full' },
  { path: 'Customer/Account', redirectTo: 'account', pathMatch: 'full' },
  { path: 'Identity/Account/Login', redirectTo: 'auth/login', pathMatch: 'full' },
  { path: 'Identity/Account/Register', redirectTo: 'auth/register', pathMatch: 'full' },
  {
    path: 'Identity/Account/ForgotPassword',
    redirectTo: 'auth/forgot-password',
    pathMatch: 'full',
  },
  {
    path: 'Identity/Account/ResetPassword',
    redirectTo: 'auth/reset-password',
    pathMatch: 'full',
  },
];
