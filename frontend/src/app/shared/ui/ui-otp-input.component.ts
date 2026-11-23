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
  template: `
    <div class="ui-otp" role="group" [attr.aria-label]="groupLabel()">
      @for (digit of digits(); track $index) {
        <input
          #cell
          class="ui-input ui-otp__cell"
          type="text"
          inputmode="numeric"
          maxlength="1"
          [attr.autocomplete]="$index === 0 ? 'one-time-code' : 'off'"
          [attr.aria-label]="digitLabel($index)"
          [value]="digit"
          [disabled]="disabled()"
          (input)="onInput($index, $event)"
          (keydown)="onKey($index, $event)"
          (paste)="onPaste($event)"
          (blur)="onTouched()"
        />
      }
    </div>
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

  private emit(): void {
    this.onChange(this.digits().join(''));
  }

  private focus(index: number): void {
    const cell = this.cells()[index]?.nativeElement;
    cell?.focus();
    cell?.select();
  }
}
