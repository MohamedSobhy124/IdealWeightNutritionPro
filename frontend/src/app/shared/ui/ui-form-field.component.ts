import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  effect,
  ElementRef,
  inject,
  input,
} from '@angular/core';

/**
 * Consistent field wrapper: label, projected control (native input/select styled
 * with `.ui-input`), optional helper text and inline error message.
 * Auto-wires `id`, `for`, `aria-describedby`, and `aria-invalid` on the first control.
 */
@Component({
  selector: 'ui-form-field',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="w-full">
      @if (label()) {
        <label class="ui-label" [attr.for]="for() || generatedId">
          {{ label() }}
          @if (required()) {
            <span class="text-danger" aria-hidden="true">*</span>
          }
        </label>
      }
      <ng-content></ng-content>
      @if (error()) {
        <p class="ui-error" [id]="errorDescId">{{ error() }}</p>
      } @else if (hint()) {
        <p class="ui-help" [id]="hintDescId">{{ hint() }}</p>
      }
    </div>
  `,
})
export class UiFormFieldComponent implements AfterViewInit {
  private static seq = 0;
  private readonly host = inject(ElementRef<HTMLElement>);

  readonly label = input<string | null>(null);
  readonly for = input<string | null>(null);
  readonly hint = input<string | null>(null);
  readonly error = input<string | null>(null);
  readonly required = input(false);

  protected readonly generatedId = `ui-field-${++UiFormFieldComponent.seq}`;
  protected readonly errorDescId = `${this.generatedId}-error`;
  protected readonly hintDescId = `${this.generatedId}-hint`;

  constructor() {
    effect(() => {
      this.error();
      this.hint();
      queueMicrotask(() => this.wireControl());
    });
  }

  ngAfterViewInit(): void {
    this.wireControl();
  }

  private wireControl(): void {
    const root = this.host.nativeElement;
    const control = root.querySelector('input, select, textarea') as HTMLElement | null;
    if (!control) return;

    const id = this.for() || control.id || this.generatedId;
    control.id = id;

    const labelEl = root.querySelector('label');
    if (labelEl) labelEl.setAttribute('for', id);

    const describedBy: string[] = [];
    if (this.error()) {
      control.setAttribute('aria-invalid', 'true');
      describedBy.push(this.errorDescId);
    } else {
      control.removeAttribute('aria-invalid');
      if (this.hint()) describedBy.push(this.hintDescId);
    }

    if (describedBy.length) {
      control.setAttribute('aria-describedby', describedBy.join(' '));
    } else {
      control.removeAttribute('aria-describedby');
    }
  }
}
