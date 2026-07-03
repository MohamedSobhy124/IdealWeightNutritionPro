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
    <section class="grid grid-cols-2 gap-3 sm:grid-cols-4" [attr.aria-label]="ariaLabel()">
      @for (item of items(); track item.label) {
        <div class="ui-card ui-card-pad flex min-h-[44px] items-center gap-2 text-sm font-medium text-ink-700">
          <span class="material-icons text-brand-600">{{ item.icon }}</span>
          <span>{{ item.label }}</span>
        </div>
      }
    </section>
  `,
})
export class UiTrustStripComponent {
  readonly items = input.required<TrustStripItem[]>();
  readonly ariaLabel = input('Trust badges');
}
