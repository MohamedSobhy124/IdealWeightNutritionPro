import { APP_BASE_HREF } from '@angular/common';
import { CommonEngine, isMainModule } from '@angular/ssr/node';
import express from 'express';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import bootstrap from './main.server';
import { resolveLegacyRedirect } from './legacy-redirects';
import { createBackendProxy, resolveBackendUrl } from './server-proxy';

const serverDistFolder = dirname(fileURLToPath(import.meta.url));
const browserDistFolder = resolve(serverDistFolder, '../browser');
const indexHtml = join(serverDistFolder, 'index.server.html');

const app = express();
const commonEngine = new CommonEngine();
const backendUrl = resolveBackendUrl();
const backendProxy = createBackendProxy(backendUrl);

/**
 * Permanent redirects from legacy MVC storefront URLs (SEO / bookmark parity).
 */
app.use((req, res, next) => {
  const requestUrl = new URL(req.originalUrl, 'http://localhost');
  const target = resolveLegacyRedirect(requestUrl.pathname, requestUrl.searchParams);
  if (target) {
    res.redirect(301, target);
    return;
  }
  next();
});

/**
 * Forward API, SignalR, static media, SEO, and OAuth paths to the .NET backend.
 * Set BACKEND_URL when the API is not on http://localhost:5228.
 */
app.use(backendProxy);

/**
 * Serve static files from /browser
 */
app.get(
  '**',
  express.static(browserDistFolder, {
    maxAge: '1y',
    index: 'index.html'
  }),
);

/**
 * Handle all other requests by rendering the Angular application.
 */
app.get('**', (req, res, next) => {
  const { protocol, originalUrl, baseUrl, headers } = req;

  commonEngine
    .render({
      bootstrap,
      documentFilePath: indexHtml,
      url: `${protocol}://${headers.host}${originalUrl}`,
      publicPath: browserDistFolder,
      providers: [{ provide: APP_BASE_HREF, useValue: baseUrl }],
    })
    .then((html) => res.send(html))
    .catch((err) => next(err));
});

/**
 * Start the server if this module is the main entry point.
 * The server listens on the port defined by the `PORT` environment variable, or defaults to 4000.
 */
if (isMainModule(import.meta.url)) {
  const port = process.env['PORT'] || 4000;
  const server = app.listen(port, () => {
    console.log(`Node Express server listening on http://localhost:${port}`);
    console.log(`Proxying backend traffic to ${backendUrl}`);
  });
  server.on('upgrade', backendProxy.upgrade);
}

export default app;
