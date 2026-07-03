/** Query keys forwarded from legacy URLs to modern routes. */
const PRESERVED_QUERY_KEYS = [
  'categoryId',
  'brandId',
  'search',
  'sortBy',
  'page',
  'pageSize',
  'orderId',
  'email',
  'id',
] as const;

type RedirectResolver = (match: RegExpMatchArray, query: URLSearchParams) => string;

interface LegacyRedirectRule {
  pattern: RegExp;
  resolve: RedirectResolver;
}

const RULES: LegacyRedirectRule[] = [
  {
    pattern: /^\/Customer\/Home\/Details\/([^/]+)\/?$/i,
    resolve: (m) => `/product/${encodeURIComponent(m[1]!)}`,
  },
  {
    pattern: /^\/Customer\/Home\/(?:Index)?\/?$/i,
    resolve: (_, query) =>
      query.get('categoryId') ? withQuery('/shop', query) : withQuery('/', query),
  },
  {
    pattern: /^\/Customer\/Shop\/?$/i,
    resolve: (_, query) => withQuery('/shop', query),
  },
  {
    pattern: /^\/Customer\/Home\/AboutUs\/?$/i,
    resolve: () => '/page/about',
  },
  {
    pattern: /^\/Customer\/Home\/(?:Privacy|PrivacyPolicy)\/?$/i,
    resolve: () => '/page/privacy',
  },
  {
    pattern: /^\/Customer\/Home\/Terms\/?$/i,
    resolve: () => '/page/terms',
  },
  {
    pattern: /^\/Customer\/Home\/Shipping\/?$/i,
    resolve: () => '/page/shipping',
  },
  {
    pattern: /^\/Customer\/Home\/Returns\/?$/i,
    resolve: () => '/page/return-policy',
  },
  {
    pattern: /^\/Customer\/Home\/HelpCenter\/?$/i,
    resolve: () => '/page/help',
  },
  {
    pattern: /^\/Customer\/Home\/TrackOrder\/?$/i,
    resolve: (_, query) => withQuery('/track', query, ['orderId', 'email']),
  },
  {
    pattern: /^\/Customer\/Blog\/Details\/([^/]+)\/?$/i,
    resolve: (m) => `/blog/${encodeURIComponent(m[1]!)}`,
  },
  {
    pattern: /^\/Customer\/Blog\/?$/i,
    resolve: () => '/blog',
  },
  {
    pattern: /^\/Customer\/FlashSale\/Details\/(\d+)\/?$/i,
    resolve: (m) => `/flash-sales/${m[1]}`,
  },
  {
    pattern: /^\/Customer\/FlashSale\/?$/i,
    resolve: () => '/flash-sales',
  },
  {
    pattern: /^\/Customer\/ComboOffer\/Details\/(\d+)\/?$/i,
    resolve: (m) => `/combos/${m[1]}`,
  },
  {
    pattern: /^\/Customer\/ComboOffer\/?$/i,
    resolve: () => '/combos',
  },
  {
    pattern: /^\/Customer\/Offer\/?$/i,
    resolve: () => '/offers',
  },
  {
    pattern: /^\/Customer\/Cart\/OrderConfirmation\/(\d+)\/?$/i,
    resolve: (m) => `/order/confirmation/${m[1]}`,
  },
  {
    pattern: /^\/Customer\/Cart\/Summary\/?$/i,
    resolve: () => '/checkout',
  },
  {
    pattern: /^\/Customer\/Cart\/?$/i,
    resolve: () => '/cart',
  },
  {
    pattern: /^\/Customer\/ServiceSubscription\/Details\/(\d+)\/?$/i,
    resolve: (m) => `/services/${m[1]}`,
  },
  {
    pattern: /^\/Customer\/ServiceSubscription\/?$/i,
    resolve: () => '/services',
  },
  {
    pattern: /^\/Customer\/Account\/Orders\/?$/i,
    resolve: () => '/account/orders',
  },
  {
    pattern: /^\/Customer\/Account\/?$/i,
    resolve: () => '/account',
  },
  {
    pattern: /^\/Identity\/Account\/Login\/?$/i,
    resolve: () => '/auth/login',
  },
  {
    pattern: /^\/Identity\/Account\/Register\/?$/i,
    resolve: () => '/auth/register',
  },
  {
    pattern: /^\/Identity\/Account\/ForgotPassword\/?$/i,
    resolve: () => '/auth/forgot-password',
  },
  {
    pattern: /^\/Identity\/Account\/ResetPassword\/?$/i,
    resolve: () => '/auth/reset-password',
  },
];

function withQuery(
  path: string,
  source: URLSearchParams,
  keys: readonly string[] = PRESERVED_QUERY_KEYS
): string {
  const next = new URLSearchParams();
  for (const key of keys) {
    const value = source.get(key);
    if (value) {
      next.set(key, value);
    }
  }
  const qs = next.toString();
  return qs ? `${path}?${qs}` : path;
}

/**
 * Maps a legacy ASP.NET MVC storefront path to a modern Angular route.
 * Returns null when the path is not a known legacy URL.
 */
export function resolveLegacyRedirect(pathname: string, query: URLSearchParams): string | null {
  const normalized = pathname.length > 1 ? pathname.replace(/\/+$/, '') : pathname;

  for (const rule of RULES) {
    const match = normalized.match(rule.pattern);
    if (match) {
      return rule.resolve(match, query);
    }
  }

  return null;
}
