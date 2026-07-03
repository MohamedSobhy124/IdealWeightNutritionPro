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
        <nav class="mb-2 flex items-center gap-1.5 text-xs text-ink-500" [attr.aria-label]="breadcrumbLabel()">
          @for (crumb of breadcrumbs(); track crumb.label; let last = $last) {
            @if (crumb.link) {
              <a [routerLink]="crumb.link" [queryParams]="crumb.queryParams ?? null" class="hover:text-brand-600">{{ crumb.label }}</a>
            } @else {
              <span [class.text-ink-700]="last" [class.font-medium]="last">{{ crumb.label }}</span>
            }
            @if (!last) {
              <span class="text-ink-300 rtl:rotate-180" aria-hidden="true">›</span>
            }
          }
        </nav>
      }
      <div [class]="compact() ? 'flex items-start justify-between gap-2' : 'flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between'">
        <div class="min-w-0">
          <h1 class="text-balance text-lg font-bold tracking-tight text-ink-900 sm:text-2xl">{{ title() }}</h1>
          @if (subtitle()) {
            <p class="mt-1 text-sm text-ink-500">{{ subtitle() }}</p>
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
}
