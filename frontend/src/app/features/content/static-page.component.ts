import { Component, computed, effect, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { DomSanitizer } from '@angular/platform-browser';
import { map } from 'rxjs/operators';
import { CONTENT_PAGES, ContentPageSlug } from '../../core/i18n/content-pages';
import { LocaleService } from '../../core/services/locale.service';
import { SeoService } from '../../core/services/seo.service';
import { UI, UiKey } from '../../core/i18n/ui-text';
import { UiCardComponent, UiEmptyStateComponent, UiPageHeaderComponent } from '../../shared/ui';

@Component({
  standalone: true,
  imports: [RouterLink, UiCardComponent, UiPageHeaderComponent, UiEmptyStateComponent],
  templateUrl: './static-page.component.html',
  styleUrl: './static-page.component.css',
})
export class StaticPageComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly seo = inject(SeoService);
  private readonly sanitizer = inject(DomSanitizer);
  readonly locale = inject(LocaleService);

  private readonly slug = toSignal(
    this.route.paramMap.pipe(map((params) => params.get('slug') as ContentPageSlug | null)),
    { initialValue: null }
  );

  readonly page = computed(() => {
    const slug = this.slug();
    return slug && CONTENT_PAGES[slug] ? CONTENT_PAGES[slug] : null;
  });

  readonly title = computed(() => {
    const p = this.page();
    return p ? this.locale.pick(p.title.en, p.title.ar) : '';
  });

  readonly body = computed(() => {
    const p = this.page();
    return p ? this.locale.pick(p.body.en, p.body.ar) : '';
  });
  readonly safeBody = computed(() =>
    this.sanitizer.bypassSecurityTrustHtml(this.body())
  );

  constructor() {
    effect(() => {
      const p = this.page();
      const slug = this.slug();
      if (!p || !slug) return;
      this.seo.applyPage({
        title: this.locale.pick(p.title.en, p.title.ar),
        description: this.truncate(this.locale.pick(p.body.en, p.body.ar), 160),
        path: `/page/${slug}`,
      });
    });
  }

  private truncate(value: string, maxLength: number): string {
    const text = value.replace(/<[^>]+>/g, ' ').replace(/\s+/g, ' ').trim();
    if (text.length <= maxLength) return text;
    return `${text.slice(0, maxLength - 1).trim()}…`;
  }

  t(key: UiKey): string {
    return this.locale.pick(UI[key].en, UI[key].ar);
  }
}
