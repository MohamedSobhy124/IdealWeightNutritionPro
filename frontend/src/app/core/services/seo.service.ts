import { DOCUMENT } from '@angular/common';
import { inject, Injectable } from '@angular/core';
import { UI, UiKey } from '../i18n/ui-text';
import { CatalogueService } from './catalogue.service';
import { LocaleService } from './locale.service';
import { environment } from '../../../environments/environment';

export interface SeoPage {
  title: string;
  titleAr?: string | null;
  description?: string | null;
  descriptionAr?: string | null;
  imageUrl?: string | null;
  path?: string;
  type?: 'website' | 'product' | 'article';
}

@Injectable({ providedIn: 'root' })
export class SeoService {
  private readonly document = inject(DOCUMENT);
  private readonly locale = inject(LocaleService);
  private readonly catalogue = inject(CatalogueService);

  private lastRouteKeys: { title: UiKey; description?: UiKey } | null = null;
  private lastDynamic: SeoPage | null = null;

  applyRoute(titleKey: UiKey, descriptionKey?: UiKey): void {
    this.lastRouteKeys = { title: titleKey, description: descriptionKey };
    this.lastDynamic = null;
    this.render({
      title: this.locale.pick(UI[titleKey].en, UI[titleKey].ar),
      description: descriptionKey
        ? this.locale.pick(UI[descriptionKey].en, UI[descriptionKey].ar)
        : this.defaultDescription(),
    });
  }

  applyPage(page: SeoPage): void {
    this.lastDynamic = page;
    this.lastRouteKeys = null;
    this.render({
      title: this.locale.pick(page.title, page.titleAr),
      description: page.description || page.descriptionAr
        ? this.locale.pick(page.description ?? '', page.descriptionAr)
        : this.defaultDescription(),
      imageUrl: page.imageUrl,
      path: page.path,
      type: page.type,
    });
  }

  refresh(): void {
    if (this.lastDynamic) {
      this.applyPage(this.lastDynamic);
      return;
    }
    if (this.lastRouteKeys) {
      this.applyRoute(this.lastRouteKeys.title, this.lastRouteKeys.description);
      return;
    }
    this.reset();
  }

  reset(): void {
    this.lastDynamic = null;
    this.lastRouteKeys = null;
    this.render({
      title: this.siteName(),
      description: this.defaultDescription(),
    });
  }

  setStructuredData(id: string, data: unknown): void {
    let script = this.document.querySelector(`script[data-seo-id="${id}"]`) as HTMLScriptElement | null;
    if (!script) {
      script = this.document.createElement('script');
      script.type = 'application/ld+json';
      script.setAttribute('data-seo-id', id);
      this.document.head.appendChild(script);
    }
    script.text = JSON.stringify(data);
  }

  clearStructuredData(id: string): void {
    this.document.querySelector(`script[data-seo-id="${id}"]`)?.remove();
  }

  private render(options: {
    title: string;
    description?: string;
    imageUrl?: string | null;
    path?: string;
    type?: 'website' | 'product' | 'article';
  }): void {
    const siteName = this.siteName();
    const pageTitle = options.title.trim();
    const fullTitle =
      pageTitle.toLowerCase() === siteName.toLowerCase()
        ? siteName
        : `${pageTitle} | ${siteName}`;

    this.document.title = fullTitle;
    this.setMeta('description', options.description ?? this.defaultDescription());
    this.setMeta('og:title', fullTitle, 'property');
    this.setMeta('og:description', options.description ?? this.defaultDescription(), 'property');
    this.setMeta('og:site_name', siteName, 'property');
    this.setMeta('og:type', options.type ?? 'website', 'property');
    this.setMeta('twitter:card', 'summary_large_image');
    this.setMeta('twitter:title', fullTitle);
    this.setMeta('twitter:description', options.description ?? this.defaultDescription());

    const canonical = this.absoluteUrl(options.path);
    this.setLink('canonical', canonical);

    const image = this.absoluteImageUrl(options.imageUrl);
    if (image) {
      this.setMeta('og:image', image, 'property');
      this.setMeta('twitter:image', image);
    } else {
      this.removeMeta('og:image', 'property');
      this.removeMeta('twitter:image');
    }

    this.setMeta('og:url', canonical, 'property');
  }

  private siteName(): string {
    return this.locale.pick(
      environment.siteName,
      environment.siteNameAr ?? environment.siteName
    );
  }

  private defaultDescription(): string {
    return this.locale.pick(
      environment.siteDescription,
      environment.siteDescriptionAr ?? environment.siteDescription
    );
  }

  private absoluteUrl(path?: string): string {
    const base = environment.siteUrl?.replace(/\/$/, '') || this.windowOrigin();
    const routePath = path ?? this.currentPath();
    return `${base}${routePath.startsWith('/') ? routePath : `/${routePath}`}`;
  }

  private absoluteImageUrl(imageUrl?: string | null): string | undefined {
    if (!imageUrl) return undefined;
    const resolved = this.catalogue.resolveImageUrl(imageUrl);
    if (resolved.startsWith('http://') || resolved.startsWith('https://')) {
      return resolved;
    }
    const base = environment.legacyAssetsBaseUrl || environment.siteUrl || this.windowOrigin();
    return `${base.replace(/\/$/, '')}${resolved.startsWith('/') ? resolved : `/${resolved}`}`;
  }

  private currentPath(): string {
    if (typeof window === 'undefined') return '/';
    return `${window.location.pathname}${window.location.search}`;
  }

  private windowOrigin(): string {
    if (typeof window === 'undefined') return 'https://idealweightnutrition.ae';
    return window.location.origin;
  }

  private setMeta(name: string, content: string, attr: 'name' | 'property' = 'name'): void {
    const selector =
      attr === 'name' ? `meta[name="${name}"]` : `meta[property="${name}"]`;
    let element = this.document.querySelector(selector) as HTMLMetaElement | null;
    if (!element) {
      element = this.document.createElement('meta');
      element.setAttribute(attr, name);
      this.document.head.appendChild(element);
    }
    element.content = content;
  }

  private removeMeta(name: string, attr: 'name' | 'property' = 'name'): void {
    const selector =
      attr === 'name' ? `meta[name="${name}"]` : `meta[property="${name}"]`;
    this.document.querySelector(selector)?.remove();
  }

  private setLink(rel: string, href: string): void {
    let element = this.document.querySelector(`link[rel="${rel}"]`) as HTMLLinkElement | null;
    if (!element) {
      element = this.document.createElement('link');
      element.rel = rel;
      this.document.head.appendChild(element);
    }
    element.href = href;
  }
}
