import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { LocaleService } from '../../core/services/locale.service';

export type UiAlertVariant = 'info' | 'success' | 'warning' | 'danger';

const ICONS: Record<UiAlertVariant, string> = {
  info: 'info',
  success: 'check_circle',
  warning: 'warning_amber',
  danger: 'error_outline',
};

/**
 * Inline message for page-level states (load failed, read-only notice).
 * Use `UiToastService` for action results and `ui-form-field [error]` for field validation.
 *
 * ```html
 * <ui-alert variant="danger" [message]="error()" [retryable]="true" (retry)="load()" />
 * ```
 */
@Component({
  selector: 'ui-alert',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'block' },
  template: `
    <div
      class="flex items-start gap-3 rounded-xl px-4 py-3 text-sm"
      [class]="tone()"
      [attr.role]="variant() === 'danger' || variant() === 'warning' ? 'alert' : 'status'"
    >
      <span class="material-icons mt-px shrink-0 !text-[20px]" aria-hidden="true">{{ icon() || defaultIcon() }}</span>
      <div class="min-w-0 flex-1">
        @if (title()) {
          <p class="m-0 font-semibold">{{ title() }}</p>
        }
        @if (message()) {
          <p class="m-0" [class.mt-0.5]="!!title()">{{ message() }}</p>
        }
        <ng-content></ng-content>
      </div>
      @if (retryable()) {
        <button type="button" class="ui-btn-secondary ui-btn-sm shrink-0" (click)="retry.emit()">
          <span class="material-icons !text-[18px]" aria-hidden="true">refresh</span>
          {{ retryLabel() || locale.pick('Retry', 'إعادة المحاولة') }}
        </button>
      }
      @if (dismissible()) {
        <button
          type="button"
          class="ui-btn-icon -my-1 -me-2 shrink-0"
          [attr.aria-label]="locale.pick('Dismiss', 'إغلاق')"
          (click)="dismiss.emit()"
        >
          <span class="material-icons !text-[18px]" aria-hidden="true">close</span>
        </button>
      }
    </div>
  `,
})
export class UiAlertComponent {
  protected readonly locale = inject(LocaleService);

  readonly variant = input<UiAlertVariant>('info');
  readonly title = input<string | null>(null);
  readonly message = input<string | null>(null);
  readonly icon = input<string | null>(null);
  readonly retryable = input(false);
  readonly retryLabel = input<string | null>(null);
  readonly dismissible = input(false);

  readonly retry = output<void>();
  readonly dismiss = output<void>();

  protected readonly defaultIcon = computed(() => ICONS[this.variant()]);
  protected readonly tone = computed(() => {
    switch (this.variant()) {
      case 'success':
        return 'bg-success-soft text-success-fg';
      case 'warning':
        return 'bg-warning-soft text-warning-fg';
      case 'danger':
        return 'bg-danger-soft text-danger-fg';
      default:
        return 'bg-info-soft text-info-fg';
    }
  });
}
