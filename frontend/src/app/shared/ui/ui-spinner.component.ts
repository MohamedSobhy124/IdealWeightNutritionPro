import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

@Component({
  selector: 'ui-spinner',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span
      class="inline-block animate-spin rounded-full border-current border-t-transparent align-[-0.125em]"
      [class]="sizeClass()"
      role="status"
      [attr.aria-label]="label()"
    ></span>
  `,
})
export class UiSpinnerComponent {
  readonly size = input<'sm' | 'md' | 'lg'>('md');
  readonly label = input('Loading');

  protected readonly sizeClass = computed(() => {
    switch (this.size()) {
      case 'sm':
        return 'h-4 w-4 border-2';
      case 'lg':
        return 'h-8 w-8 border-[3px]';
      default:
        return 'h-6 w-6 border-2';
    }
  });
}
