import { ChangeDetectionStrategy, Component, computed, inject, linkedSignal } from '@angular/core';
import { UI, UiKey } from '../../core/i18n/ui-text';
import { LocaleService } from '../../core/services/locale.service';
import { UiConfirmService } from './ui-confirm.service';
import { UiModalComponent } from './ui-modal.component';
import { UiToastService, UiToastVariant } from './ui-toast.service';

const TOAST_ICON: Record<UiToastVariant, string> = {
  success: 'check_circle',
  error: 'error',
  warning: 'warning',
  info: 'info',
};

const TOAST_ICON_CLASS: Record<UiToastVariant, string> = {
  success: 'text-success-fg',
  error: 'text-danger-fg',
  warning: 'text-warning-fg',
  info: 'text-info-fg',
};

/** Renders app-wide confirm dialogs and toasts. Mount once in the root component. */
@Component({
  selector: 'ui-feedback-host',
  standalone: true,
  imports: [UiModalComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (request(); as req) {
      <ui-modal
        [open]="true"
        size="sm"
        [heading]="req.title"
        [closeLabel]="label('Close', 'إغلاق')"
        [dismissOnBackdrop]="!req.requireText"
        (closed)="confirm.settle(false)"
      >
        <div class="flex items-start gap-3">
          @if (req.icon || req.danger) {
            <span
              class="flex h-10 w-10 shrink-0 items-center justify-center rounded-full"
              [class.bg-danger-soft]="req.danger"
              [class.text-danger-fg]="req.danger"
              [class.bg-brand-50]="!req.danger"
              [class.text-brand-700]="!req.danger"
              aria-hidden="true"
            >
              <span class="material-icons !text-[22px]">{{ req.icon || 'warning_amber' }}</span>
            </span>
          }
          @if (req.message) {
            <p class="m-0 pt-2 text-sm leading-relaxed text-ink-600">{{ req.message }}</p>
          }
        </div>
        @if (req.requireText) {
          <label class="mt-4 block">
            <span class="ui-label">
              {{ t('typeToConfirmBefore') }}
              <strong class="font-mono text-ink-900">{{ req.requireText }}</strong>
              {{ t('typeToConfirmAfter') }}
            </span>
            <input
              class="ui-input"
              autocomplete="off"
              autocapitalize="off"
              spellcheck="false"
              autofocus
              [attr.aria-invalid]="confirmWordError() ? true : null"
              [value]="typed()"
              (input)="typed.set($any($event.target).value)"
              (blur)="confirmBlurred.set(true)"
            />
            @if (confirmWordError(); as mismatch) {
              <p class="ui-error" role="alert">{{ mismatch }}</p>
            }
          </label>
        }
        <div modal-footer class="flex flex-col-reverse gap-2 border-t border-ink-200 px-5 py-4 sm:flex-row sm:justify-end">
          <button type="button" class="ui-btn-secondary" (click)="confirm.settle(false)">
            {{ req.cancelLabel || label('Cancel', 'إلغاء') }}
          </button>
          <button
            type="button"
            [class]="req.danger ? 'ui-btn-danger' : 'ui-btn-primary'"
            [disabled]="!canConfirm()"
            (click)="confirm.settle(true)"
          >
            {{ req.confirmLabel || (req.danger ? label('Delete', 'حذف') : label('Confirm', 'تأكيد')) }}
          </button>
        </div>
      </ui-modal>
    }

    <div
      class="pointer-events-none fixed inset-x-0 bottom-0 z-toast flex flex-col items-center gap-2 p-4 pb-[max(1rem,env(safe-area-inset-bottom))] max-lg:bottom-16 sm:items-end"
      role="region"
      [attr.aria-label]="label('Notifications', 'الإشعارات')"
    >
      <div class="flex w-full max-w-sm flex-col gap-2" aria-live="polite" aria-atomic="false">
        @for (toast of toasts.toasts(); track toast.id) {
          <div class="ui-toast" [attr.role]="toast.variant === 'error' ? 'alert' : 'status'">
            <span class="material-icons mt-px !text-[20px]" [class]="iconClass[toast.variant]" aria-hidden="true">
              {{ icon[toast.variant] }}
            </span>
            <p class="m-0 min-w-0 flex-1 pt-0.5 leading-snug">{{ toast.message }}</p>
            @if (toast.actionLabel) {
              <button type="button" class="ui-btn-ghost ui-btn-sm -my-1 text-brand-700" (click)="toasts.runAction(toast)">
                {{ toast.actionLabel }}
              </button>
            }
            <button
              type="button"
              class="ui-btn-icon -my-1.5 -me-2 min-h-9 min-w-9"
              (click)="toasts.dismiss(toast.id)"
              [attr.aria-label]="label('Dismiss', 'إغلاق')"
            >
              <span class="material-icons !text-[18px]" aria-hidden="true">close</span>
            </button>
          </div>
        }
      </div>
    </div>
  `,
})
export class UiFeedbackHostComponent {
  protected readonly confirm = inject(UiConfirmService);
  protected readonly toasts = inject(UiToastService);
  private readonly locale = inject(LocaleService);

  protected readonly icon = TOAST_ICON;
  protected readonly iconClass = TOAST_ICON_CLASS;
  protected readonly request = this.confirm.current;
  protected readonly typed = linkedSignal({ source: this.request, computation: () => '' });
  protected readonly confirmBlurred = linkedSignal({ source: this.request, computation: () => false });

  protected readonly canConfirm = computed(() => {
    const req = this.request();
    return !req?.requireText || this.typed().trim() === req.requireText;
  });

  protected confirmWordError(): string | null {
    const req = this.request();
    if (!req?.requireText || !this.confirmBlurred() || !this.typed().trim() || this.canConfirm()) return null;
    return this.t('confirmWordMismatch');
  }

  protected t(key: UiKey): string {
    return this.locale.pick(UI[key].en, UI[key].ar);
  }

  protected label(en: string, ar: string): string {
    return this.locale.pick(en, ar);
  }
}
