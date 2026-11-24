import { isPlatformBrowser } from '@angular/common';
import {
  afterNextRender,
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  effect,
  inject,
  Injector,
  input,
  PLATFORM_ID,
  signal,
} from '@angular/core';
import { LocaleService } from '../../core/services/locale.service';

export interface UiFormSection {
  /** `id` of the section element. Give that element `data-form-section` for scroll offset. */
  id: string;
  label: string;
  /** Number of errors in this section; shows the item in red. */
  errors?: number;
}

/**
 * Sticky jump list for long forms (≥1280px). Highlights the section in view.
 *
 * ```html
 * <div class="admin-form-with-nav">
 *   <ui-form-section-nav [sections]="sections" />
 *   <form>…<ui-card id="pricing" data-form-section>…</ui-card>…</form>
 * </div>
 * ```
 */
@Component({
  selector: 'ui-form-section-nav',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'hidden xl:block' },
  template: `
    <nav class="sticky top-24 flex flex-col text-sm" [attr.aria-label]="label() || locale.pick('Form sections', 'أقسام النموذج')">
      @for (s of sections(); track s.id) {
        <a
          [attr.href]="'#' + s.id"
          class="border-s-2 px-3 py-1.5 no-underline transition-colors hover:text-ink-900"
          [class.border-ink-200]="active() !== s.id"
          [class.text-ink-500]="active() !== s.id && !s.errors"
          [class.border-brand-600]="active() === s.id"
          [class.text-brand-700]="active() === s.id && !s.errors"
          [class.font-semibold]="active() === s.id"
          [class.text-danger]="!!s.errors"
          [attr.aria-current]="active() === s.id ? 'true' : null"
          (click)="go($event, s.id)"
        >
          {{ s.label }}
          @if (s.errors) {
            <span class="ms-1 text-xs">· {{ s.errors }}</span>
          }
        </a>
      }
    </nav>
  `,
})
export class UiFormSectionNavComponent {
  protected readonly locale = inject(LocaleService);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
  private readonly injector = inject(Injector);

  readonly sections = input.required<readonly UiFormSection[]>();
  readonly label = input<string | null>(null);

  protected readonly active = signal<string | null>(null);
  private observer: IntersectionObserver | null = null;

  constructor() {
    inject(DestroyRef).onDestroy(() => this.observer?.disconnect());
    effect(() => {
      const ids = this.sections().map((s) => s.id);
      if (!this.isBrowser) return;
      afterNextRender(() => this.observe(ids), { injector: this.injector });
    });
  }

  private observe(ids: string[]): void {
    this.observer?.disconnect();
    if (!('IntersectionObserver' in window)) return;
    const visible = new Map<string, number>();
    this.observer = new IntersectionObserver(
      (entries) => {
        for (const e of entries) {
          if (e.isIntersecting) visible.set(e.target.id, e.boundingClientRect.top);
          else visible.delete(e.target.id);
        }
        // The topmost visible section wins; keep the last one when none are visible.
        const top = ids.find((id) => visible.has(id));
        if (top) this.active.set(top);
      },
      { rootMargin: '-88px 0px -55% 0px' },
    );
    for (const id of ids) {
      const el = document.getElementById(id);
      if (el) this.observer.observe(el);
    }
    if (!this.active() && ids.length) this.active.set(ids[0]);
  }

  protected go(event: Event, id: string): void {
    event.preventDefault();
    const el = document.getElementById(id);
    if (!el) return;
    el.scrollIntoView({ behavior: 'smooth', block: 'start' });
    this.active.set(id);
  }
}
