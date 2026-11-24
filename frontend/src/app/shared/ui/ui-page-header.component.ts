import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

export interface UiBreadcrumb {
  label: string;
  link?: string;
  queryParams?: Record<string, string | number | boolean | null | undefined>;
}

@Component({
  selector: 'ui-page-header',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div [class]="compact() ? 'mb-0' : 'mb-4 sm:mb-6'">
      @if (breadcrumbs().length) {
        <nav class="mb-1 flex flex-wrap items-center gap-1 text-xs text-ink-600" [attr.aria-label]="breadcrumbLabel()">
          @for (crumb of breadcrumbs(); track crumb.label; let last = $last) {
            @if (crumb.link) {
              <a [routerLink]="crumb.link" [queryParams]="crumb.queryParams ?? null" class="inline-flex min-h-11 items-center px-1 hover:text-brand-700">{{ crumb.label }}</a>
            } @else {
              <span class="inline-flex min-h-11 items-center px-1" [class.text-ink-900]="last" [class.font-medium]="last">{{ crumb.label }}</span>
            }
            @if (!last) {
              <span class="text-ink-300 rtl:rotate-180" aria-hidden="true">›</span>
            }
          }
        </nav>
      }
      <div [class]="compact() ? 'flex items-start justify-between gap-2' : 'flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between'">
        <div class="min-w-0">
          @if (backLink()) {
            <a
              [routerLink]="backLink()"
              [queryParams]="backQueryParams()"
              class="mb-1 inline-flex min-h-8 items-center gap-1 rounded text-[13px] font-medium text-ink-500 no-underline hover:text-ink-900"
            >
              <span class="material-icons !text-[18px] rtl:rotate-180" aria-hidden="true">arrow_back</span>
              {{ backLabel() }}
            </a>
          }
          <div class="flex flex-wrap items-center gap-x-3 gap-y-1.5">
            @if (titleTag() === 'p') {
              <p class="display-title m-0 text-balance text-xl text-ink-900 sm:text-2xl lg:text-[1.75rem]">{{ title() }}</p>
            } @else {
              <h1 class="display-title m-0 text-balance text-xl text-ink-900 sm:text-2xl lg:text-[1.75rem]">{{ title() }}</h1>
            }
            <ng-content select="[header-status]"></ng-content>
          </div>
          @if (subtitle()) {
            <p class="mt-1.5 text-sm leading-relaxed text-ink-500">{{ subtitle() }}</p>
          }
        </div>
        <div class="flex flex-wrap items-center gap-2">
          <ng-content select="[header-actions]"></ng-content>
        </div>
      </div>
    </div>
  `,
})
export class UiPageHeaderComponent {
  readonly title = input.required<string>();
  readonly subtitle = input<string | null>(null);
  readonly breadcrumbs = input<UiBreadcrumb[]>([]);
  readonly breadcrumbLabel = input('Breadcrumb');
  readonly compact = input(false);
  /** Detail/form pages: "← Products" link above the title. */
  readonly backLink = input<string | (string | number)[] | null>(null);
  readonly backLabel = input('');
  readonly backQueryParams = input<Record<string, string | number | boolean | null | undefined> | null>(null);
  /** Product pages pass `p` so the buy box keeps the only h1. */
  readonly titleTag = input<'h1' | 'p'>('h1');
}
