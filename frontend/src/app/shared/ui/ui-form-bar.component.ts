import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  HostListener,
  inject,
  input,
  output,
} from '@angular/core';
import { LocaleService } from '../../core/services/locale.service';
import { UiSpinnerComponent } from './ui-spinner.component';

/**
 * Sticky save bar for admin forms: unsaved indicator, Cancel, Save, and Ctrl/⌘+S.
 *
 * Inside a `<form (ngSubmit)>` the Save button submits the form, and Ctrl+S calls `requestSubmit()`.
 * Outside a form, both emit `save`.
 *
 * ```html
 * <form class="admin-form" (ngSubmit)="save()">
 *   …
 *   <ui-form-bar [dirty]="hasUnsavedChanges()" [saving]="saving()" (cancel)="back()" />
 * </form>
 * ```
 */
@Component({
  selector: 'ui-form-bar',
  standalone: true,
  imports: [UiSpinnerComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'admin-form-bar items-center' },
  template: `
    @if (dirty() && !saving()) {
      <span class="admin-form-bar__dirty" role="status">{{ unsavedLabel() || locale.pick('Unsaved changes', 'تغييرات غير محفوظة') }}</span>
    }
    <span class="flex-1"></span>
    <ng-content></ng-content>
    <kbd class="hidden text-[11px] font-semibold text-ink-400 lg:inline" aria-hidden="true">{{ shortcutHint }}</kbd>
    @if (showCancel()) {
      <button type="button" class="ui-btn-secondary" [disabled]="saving()" (click)="cancel.emit()">
        {{ cancelLabel() || locale.pick('Cancel', 'إلغاء') }}
      </button>
    }
    <button
      [attr.type]="inForm ? 'submit' : 'button'"
      class="ui-btn-primary"
      [disabled]="saving() || disabled()"
      (click)="onSaveClick()"
    >
      @if (saving()) {
        <ui-spinner size="sm" />
        {{ savingLabel() || locale.pick('Saving…', 'جاري الحفظ…') }}
      } @else {
        <span class="material-icons !text-[18px]" aria-hidden="true">check</span>
        {{ saveLabel() || locale.pick('Save changes', 'حفظ التغييرات') }}
      }
    </button>
  `,
})
export class UiFormBarComponent {
  protected readonly locale = inject(LocaleService);
  private readonly host = inject(ElementRef<HTMLElement>);

  readonly dirty = input(false);
  readonly saving = input(false);
  readonly disabled = input(false);
  readonly showCancel = input(true);
  readonly saveLabel = input<string | null>(null);
  readonly savingLabel = input<string | null>(null);
  readonly cancelLabel = input<string | null>(null);
  readonly unsavedLabel = input<string | null>(null);

  readonly save = output<void>();
  readonly cancel = output<void>();

  protected readonly shortcutHint =
    typeof navigator !== 'undefined' && /Mac|iPhone|iPad/.test(navigator.platform) ? '⌘S' : 'Ctrl S';

  protected get inForm(): boolean {
    return !!this.form();
  }

  private form(): HTMLFormElement | null {
    return (this.host.nativeElement as HTMLElement).closest('form');
  }

  /** Outside a form the button is `type="button"`, so emit. Inside a form, let the native submit run. */
  protected onSaveClick(): void {
    if (!this.inForm) this.save.emit();
  }

  @HostListener('document:keydown', ['$event'])
  protected onKey(event: KeyboardEvent): void {
    if (!(event.ctrlKey || event.metaKey) || event.key.toLowerCase() !== 's') return;
    event.preventDefault();
    if (this.saving() || this.disabled()) return;
    const form = this.form();
    if (form) form.requestSubmit();
    else this.save.emit();
  }
}
