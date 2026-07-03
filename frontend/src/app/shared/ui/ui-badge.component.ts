import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

export type UiBadgeVariant =
  | 'neutral'
  | 'brand'
  | 'success'
  | 'warning'
  | 'danger'
  | 'info';

@Component({
  selector: 'ui-badge',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span [class]="cssClass()">
      @if (dot()) {
        <span class="h-1.5 w-1.5 rounded-full bg-current opacity-70" aria-hidden="true"></span>
      }
      <ng-content></ng-content>
    </span>
  `,
})
export class UiBadgeComponent {
  readonly variant = input<UiBadgeVariant>('neutral');
  readonly dot = input(false);

  protected readonly cssClass = computed(() => `ui-badge-${this.variant()}`);
}
