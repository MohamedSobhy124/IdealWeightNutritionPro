import { RenderMode, ServerRoute } from '@angular/ssr';

/**
 * Storefront pages use runtime SSR for SEO (needs API at request time).
 * Auth, checkout, cart, and admin stay client-rendered.
 */
export const serverRoutes: ServerRoute[] = [
  { path: '', renderMode: RenderMode.Server },
  { path: 'shop', renderMode: RenderMode.Server },
  { path: 'offers', renderMode: RenderMode.Server },
  { path: 'track', renderMode: RenderMode.Server },
  { path: 'blog', renderMode: RenderMode.Server },
  { path: 'blog/**', renderMode: RenderMode.Server },
  { path: 'services', renderMode: RenderMode.Server },
  { path: 'services/**', renderMode: RenderMode.Server },
  { path: 'flash-sales/**', renderMode: RenderMode.Server },
  { path: 'combos/**', renderMode: RenderMode.Server },
  { path: 'product/**', renderMode: RenderMode.Server },
  { path: 'page/**', renderMode: RenderMode.Server },
  { path: '**', renderMode: RenderMode.Client },
];
