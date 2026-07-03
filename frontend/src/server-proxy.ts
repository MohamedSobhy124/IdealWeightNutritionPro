import {
  createProxyMiddleware,
  type Filter,
  type RequestHandler,
} from 'http-proxy-middleware';

/** Paths forwarded to the .NET API (mirrors proxy.conf.json). */
const PROXY_PREFIXES = [
  '/api',
  '/hubs',
  '/videos',
  '/images',
  '/sitemap.xml',
  '/robots.txt',
  '/signin-google',
] as const;

export function shouldProxyPath(pathname: string): boolean {
  return PROXY_PREFIXES.some(
    (prefix) => pathname === prefix || pathname.startsWith(`${prefix}/`)
  );
}

const pathFilter: Filter = (pathname) => shouldProxyPath(pathname);

export function createBackendProxy(target: string): RequestHandler {
  return createProxyMiddleware({
    target,
    changeOrigin: true,
    secure: false,
    ws: true,
    pathFilter,
  });
}

export function resolveBackendUrl(): string {
  return process.env['BACKEND_URL']?.trim() || 'http://localhost:5228';
}
