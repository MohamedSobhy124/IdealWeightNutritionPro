import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  forwardRef,
  input,
  signal,
  viewChildren,
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

/** Six single-digit boxes that read and write one code string. */
@Component({
  selector: 'ui-otp-input',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => UiOtpInputComponent),
      multi: true,
    },
  ],
  // Codes always read left-to-right, even on Arabic pages, so the group is LTR.
  template: `
    <div class="otp" dir="ltr" role="group" [attr.aria-label]="groupLabel()" [style.--otp-count]="length()">
      @for (digit of digits(); track $index) {
        <input
          #cell
          class="otp__cell"
          [class.otp__cell--filled]="!!digit"
          type="text"
          inputmode="numeric"
          pattern="[0-9]*"
          maxlength="1"
          [attr.autocomplete]="$index === 0 ? 'one-time-code' : 'off'"
          [attr.aria-label]="digitLabel($index)"
          [value]="digit"
          [disabled]="disabled()"
          (input)="onInput($index, $event)"
          (keydown)="onKey($index, $event)"
          (paste)="onPaste($event)"
          (focus)="selectCell($event)"
          (blur)="onTouched()"
        />
      }
    </div>
  `,
  // Component styles (not the global layer) so they beat the unlayered
  // base rule `input[type="text"] { width: 100% }` in _forms.scss.
  styles: `
    :host {
      display: block;
    }

    .otp {
      display: grid;
      grid-template-columns: repeat(var(--otp-count, 6), minmax(0, 3.25rem));
      justify-content: center;
      gap: 0.5rem;
    }

    @media (min-width: 400px) {
      .otp {
        gap: 0.65rem;
      }
    }

    .otp__cell {
      width: 100%;
      min-width: 0;
      height: 3.5rem;
      margin: 0;
      padding: 0;
      border: 1.5px solid rgb(var(--rgb-ink-200));
      border-radius: 0.9rem;
      background: rgb(var(--rgb-surface-raised));
      box-shadow: 0 1px 2px rgb(var(--rgb-ink-900) / 0.04);
      color: rgb(var(--rgb-ink-900));
      font-size: 1.5rem;
      font-weight: 800;
      font-variant-numeric: tabular-nums;
      line-height: 1;
      text-align: center;
      caret-color: rgb(var(--rgb-brand-solid));
      transition: border-color 0.15s ease, box-shadow 0.15s ease, background-color 0.15s ease, transform 0.15s ease;
    }

    .otp__cell:hover:not(:disabled) {
      border-color: rgb(var(--rgb-brand-300));
    }

    .otp__cell:focus {
      outline: none;
      border-color: rgb(var(--rgb-brand-solid));
      box-shadow: 0 0 0 4px rgb(var(--rgb-brand-solid) / 0.15);
      transform: translateY(-1px);
    }

    .otp__cell--filled {
      border-color: rgb(var(--rgb-brand-400));
      background: rgb(var(--rgb-brand-50));
      color: rgb(var(--rgb-brand-800));
    }

    .otp__cell:disabled {
      opacity: 0.55;
      cursor: not-allowed;
    }

    @media (prefers-reduced-motion: reduce) {
      .otp__cell {
        transition: none;
      }

      .otp__cell:focus {
        transform: none;
      }
    }
  `,
})
export class UiOtpInputComponent implements ControlValueAccessor {
  readonly length = input(6);
  readonly groupLabel = input('Verification code');
  /** `Digit {n} of {total}` */
  readonly digitLabelPattern = input('Digit {n} of {total}');

  protected readonly digits = signal<string[]>(Array.from({ length: 6 }, () => ''));
  protected readonly disabled = signal(false);
  private readonly cells = viewChildren<ElementRef<HTMLInputElement>>('cell');

  private onChange: (value: string) => void = () => {};
  protected onTouched: () => void = () => {};

  writeValue(value: string | null): void {
    const chars = (value ?? '').replace(/\D/g, '').slice(0, this.length()).split('');
    this.digits.set(Array.from({ length: this.length() }, (_, index) => chars[index] ?? ''));
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled.set(isDisabled);
  }

  protected digitLabel(index: number): string {
    return this.digitLabelPattern().replace('{n}', String(index + 1)).replace('{total}', String(this.length()));
  }

  protected onInput(index: number, event: Event): void {
    const input = event.target as HTMLInputElement;
    const digit = input.value.replace(/\D/g, '').slice(-1);
    input.value = digit;
    const next = this.digits().slice();
    next[index] = digit;
    this.digits.set(next);
    this.emit();
    if (digit && index < this.length() - 1) this.focus(index + 1);
  }

  protected onKey(index: number, event: KeyboardEvent): void {
    if (event.key === 'Backspace' && !this.digits()[index] && index > 0) {
      event.preventDefault();
      const next = this.digits().slice();
      next[index - 1] = '';
      this.digits.set(next);
      this.emit();
      this.focus(index - 1);
    } else if (event.key === 'ArrowLeft' && index > 0) {
      event.preventDefault();
      this.focus(index - 1);
    } else if (event.key === 'ArrowRight' && index < this.length() - 1) {
      event.preventDefault();
      this.focus(index + 1);
    }
  }

  protected onPaste(event: ClipboardEvent): void {
    const chars = (event.clipboardData?.getData('text') ?? '').replace(/\D/g, '').slice(0, this.length()).split('');
    if (chars.length === 0) return;
    event.preventDefault();
    this.digits.set(Array.from({ length: this.length() }, (_, index) => chars[index] ?? ''));
    this.emit();
    this.focus(Math.min(chars.length, this.length()) - 1);
  }

  protected selectCell(event: FocusEvent): void {
    (event.target as HTMLInputElement).select();
  }

  private emit(): void {
    this.onChange(this.digits().join(''));
  }

  private focus(index: number): void {
    const cell = this.cells()[index]?.nativeElement;
    cell?.focus();
    cell?.select();
  }
}
