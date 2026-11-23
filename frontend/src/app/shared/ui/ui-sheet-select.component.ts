import { isPlatformBrowser } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  forwardRef,
  inject,
  input,
  PLATFORM_ID,
  signal,
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { UiModalComponent } from './ui-modal.component';

export interface UiSheetOption {
  value: string | number | null;
  label: string;
}

/** Native select from 1024px up. Below that, a button opens the choices in a bottom sheet. */
@Component({
  selector: 'ui-sheet-select',
  standalone: true,
  imports: [UiModalComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => UiSheetSelectComponent),
      multi: true,
    },
  ],
  host: {
    class: 'block w-full',
    ngSkipHydration: 'true',
  },
  template: `
    @if (sheet()) {
      <button
        type="button"
        class="ui-input flex min-h-11 w-full items-center justify-between gap-2 text-start"
        [disabled]="disabled()"
        [attr.aria-haspopup]="'dialog'"
        [attr.aria-expanded]="open()"
        (click)="open.set(true)"
      >
        <span class="truncate">{{ currentLabel() }}</span>
        <span class="material-icons !text-[20px] text-ink-400" aria-hidden="true">expand_more</span>
      </button>
      <ui-modal
        [open]="open()"
        [heading]="label() || placeholder()"
        [closeLabel]="closeLabel()"
        (closed)="open.set(false)"
      >
        <ul class="m-0 list-none p-0">
          @for (opt of options(); track key(opt.value) + opt.label) {
            <li>
              <button
                type="button"
                class="flex min-h-11 w-full items-center justify-between rounded-xl px-3 text-start text-sm"
                [class.bg-brand-50]="same(opt.value, value())"
                [class.font-semibold]="same(opt.value, value())"
                [attr.aria-current]="same(opt.value, value()) ? 'true' : null"
                (click)="choose(opt)"
              >
                <span class="truncate">{{ opt.label }}</span>
                @if (same(opt.value, value())) {
                  <span class="material-icons !text-[18px] text-brand-700" aria-hidden="true">check</span>
                }
              </button>
            </li>
          }
        </ul>
      </ui-modal>
    } @else {
      <select
        class="ui-select w-full"
        [disabled]="disabled()"
        [value]="key(value())"
        (change)="onNative($event)"
        (blur)="onTouched()"
      >
        @for (opt of options(); track key(opt.value) + opt.label) {
          <option [value]="key(opt.value)">{{ opt.label }}</option>
        }
      </select>
    }
  `,
})
export class UiSheetSelectComponent implements ControlValueAccessor {
  readonly label = input('');
  readonly placeholder = input('');
  readonly closeLabel = input('Close');
  readonly options = input<readonly UiSheetOption[]>([]);

  readonly sheet = signal(false);
  readonly open = signal(false);
  readonly value = signal<string | number | null>(null);
  readonly disabled = signal(false);

  readonly currentLabel = computed(() => {
    const match = this.options().find((opt) => this.same(opt.value, this.value()));
    return match?.label || this.placeholder() || this.label();
  });

  private onChange: (value: string | number | null) => void = () => {};
  protected onTouched: () => void = () => {};

  constructor() {
    if (!isPlatformBrowser(inject(PLATFORM_ID))) return;
    const query = window.matchMedia('(max-width: 1023px)');
    const apply = () => this.sheet.set(query.matches);
    apply();
    query.addEventListener('change', apply);
    inject(DestroyRef).onDestroy(() => query.removeEventListener('change', apply));
  }

  writeValue(value: string | number | null): void {
    this.value.set(value ?? null);
  }

  registerOnChange(fn: (value: string | number | null) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(disabled: boolean): void {
    this.disabled.set(disabled);
  }

  choose(option: UiSheetOption): void {
    this.value.set(option.value);
    this.onChange(option.value);
    this.onTouched();
    this.open.set(false);
  }

  onNative(event: Event): void {
    const key = (event.target as HTMLSelectElement).value;
    const option = this.options().find((opt) => this.key(opt.value) === key);
    if (option) this.choose(option);
  }

  key(value: string | number | null): string {
    return value === null || value === undefined ? '' : String(value);
  }

  same(left: string | number | null, right: string | number | null): boolean {
    return this.key(left) === this.key(right);
  }
}
