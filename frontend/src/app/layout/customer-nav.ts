import { UiKey } from '../core/i18n/ui-text';

export interface CustomerNavItem {
  labelKey: UiKey;
  link: string;
  exact: boolean;
}

export const CUSTOMER_PRIMARY_NAV: CustomerNavItem[] = [
  { labelKey: 'shop', link: '/shop', exact: false },
  { labelKey: 'navOffers', link: '/offers', exact: false },
  { labelKey: 'flashSales', link: '/flash-sales', exact: false },
  { labelKey: 'combos', link: '/combos', exact: false },
  { labelKey: 'services', link: '/services', exact: false },
  { labelKey: 'blog', link: '/blog', exact: false },
];

/** Routes that are bottom-nav destinations, so the mobile header hides the back button. */
const TOP_LEVEL_PATHS = new Set(['/', '/shop', '/cart', '/wishlist', '/account', '/auth/login']);

export function isTopLevelPath(url: string): boolean {
  return TOP_LEVEL_PATHS.has(url.split(/[?#]/)[0]);
}
