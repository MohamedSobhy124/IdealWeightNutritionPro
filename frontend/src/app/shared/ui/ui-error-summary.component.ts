import {
  afterNextRender,
  ChangeDetectionStrategy,
  Component,
  effect,
  ElementRef,
  inject,
  Injector,
  input,
} from '@angular/core';
import { LocaleService } from '../../core/services/locale.service';

export interface UiFormError {
  /** `id` of the input to focus when the link is clicked. */
  fieldId?: string;
  message: string;
}

/**
 * Lists form problems at the top of a form after a failed save, with links that jump to each field.
 * Hidden when `errors` is empty. Moves focus to itself when new errors appear.
 *
 * ```html
 * <ui-error-summary [errors]="formErrors()" />
 * ```
 */
@Component({
  selector: 'ui-error-summary',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'block empty:hidden' },
  template: `
    @if (errors().length) {
      <div class="flex gap-3 rounded-xl bg-danger-soft px-4 py-3.5 text-sm text-danger-fg" role="alert" tabindex="-1">
        <span class="material-icons mt-px shrink-0 !text-[20px]" aria-hidden="true">error_outline</span>
        <div class="min-w-0">
          <p class="m-0 font-semibold">{{ heading() }}</p>
          <ul class="m-0 mt-1.5 list-disc space-y-1 ps-5">
            @for (err of errors(); track $index) {
              <li>
                @if (err.fieldId) {
                  <a [attr.href]="'#' + err.fieldId" class="underline underline-offset-2" (click)="jump($event, err.fieldId)">{{ err.message }}</a>
                } @else {
                  {{ err.message }}
                }
              </li>
            }
          </ul>
        </div>
      </div>
    }
  `,
})
export class UiErrorSummaryComponent {
  private readonly locale = inject(LocaleService);
  private readonly host = inject(ElementRef<HTMLElement>);
  private readonly injector = inject(Injector);

  readonly errors = input<readonly UiFormError[]>([]);
  readonly title = input<string | null>(null);

  constructor() {
    effect(() => {
      if (!this.errors().length) return;
      afterNextRender(
        () => {
          const box = this.host.nativeElement.querySelector('[role="alert"]') as HTMLElement | null;
          box?.scrollIntoView({ block: 'center', behavior: 'smooth' });
          box?.focus({ preventScroll: true });
        },
        { injector: this.injector },
      );
    });
  }

  protected heading(): string {
    const custom = this.title();
    if (custom) return custom;
    const n = this.errors().length;
    return n === 1
      ? this.locale.pick('Fix 1 problem before saving', 'يرجى إصلاح مشكلة واحدة قبل الحفظ')
      : this.locale.pick(`Fix ${n} problems before saving`, `يرجى إصلاح ${n} مشكلات قبل الحفظ`);
  }

  protected jump(event: Event, fieldId: string): void {
    event.preventDefault();
    const field = document.getElementById(fieldId);
    if (!field) return;
    field.scrollIntoView({ block: 'center', behavior: 'smooth' });
    field.focus({ preventScroll: true });
  }
}
