import { ChangeDetectionStrategy, Component, input } from '@angular/core';

export interface TrustStripItem {
  icon: string;
  label: string;
}

@Component({
  selector: 'ui-trust-strip',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="grid grid-cols-2 gap-3.5 sm:grid-cols-4 sm:gap-4" [attr.aria-label]="ariaLabel()">
      @for (item of items(); track item.label) {
        <div class="ui-card flex min-h-[3.75rem] items-center gap-3 border-ink-100 bg-white px-3.5 py-3.5 text-sm font-semibold text-ink-800 sm:min-h-[4.25rem] sm:gap-3.5 sm:px-4 sm:py-4">
          <span class="flex h-11 w-11 shrink-0 items-center justify-center rounded-2xl bg-brand-50 text-brand-700 ring-1 ring-brand-100/80">
            <span class="material-icons !text-[20px]">{{ item.icon }}</span>
          </span>
          <span class="leading-snug text-balance">{{ item.label }}</span>
        </div>
      }
    </section>
  `,
})
export class UiTrustStripComponent {
  readonly items = input.required<TrustStripItem[]>();
  readonly ariaLabel = input('Trust badges');
}
