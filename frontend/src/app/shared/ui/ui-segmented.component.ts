import { ChangeDetectionStrategy, Component, ElementRef, inject, input, output } from '@angular/core';

export interface UiSegmentedOption {
  value: string;
  label: string;
}

/** Single-choice filter rendered as a radio group (arrow keys move and select, RTL aware). */
@Component({
  selector: 'ui-segmented',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (label()) {
      <span class="ui-label mb-1 block" [id]="labelId">{{ label() }}</span>
    }
    <div
      class="ui-segmented"
      role="radiogroup"
      [attr.aria-labelledby]="label() ? labelId : null"
      [attr.aria-label]="label() ? null : ariaLabel()"
      (keydown)="onKeydown($event)"
    >
      @for (option of options(); track option.value) {
        <button
          type="button"
          role="radio"
          class="ui-segmented__option"
          [class.ui-segmented__option--active]="option.value === value()"
          [attr.aria-checked]="option.value === value()"
          [attr.tabindex]="option.value === focusableValue() ? 0 : -1"
          [attr.data-value]="option.value"
          (click)="select(option.value)"
        >
          {{ option.label }}
        </button>
      }
    </div>
  `,
  styles: `:host { display: block; min-width: 0; }`,
})
export class UiSegmentedComponent {
  private static nextId = 0;
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  readonly options = input.required<UiSegmentedOption[]>();
  readonly value = input<string | null>(null);
  readonly label = input<string | null>(null);
  readonly ariaLabel = input<string | null>(null);
  readonly valueChange = output<string>();

  protected readonly labelId = `ui-segmented-${UiSegmentedComponent.nextId++}`;

  protected focusableValue(): string | undefined {
    const options = this.options();
    return options.some((o) => o.value === this.value()) ? (this.value() ?? undefined) : options[0]?.value;
  }

  protected select(value: string): void {
    if (value !== this.value()) this.valueChange.emit(value);
  }

  protected onKeydown(event: KeyboardEvent): void {
    const options = this.options();
    if (!options.length) return;
    const rtl = getComputedStyle(this.host.nativeElement).direction === 'rtl';
    const current = Math.max(0, options.findIndex((o) => o.value === this.value()));

    let next: number;
    switch (event.key) {
      case 'ArrowRight':
        next = current + (rtl ? -1 : 1);
        break;
      case 'ArrowLeft':
        next = current + (rtl ? 1 : -1);
        break;
      case 'ArrowDown':
        next = current + 1;
        break;
      case 'ArrowUp':
        next = current - 1;
        break;
      case 'Home':
        next = 0;
        break;
      case 'End':
        next = options.length - 1;
        break;
      default:
        return;
    }
    event.preventDefault();
    const target = options[(next + options.length) % options.length];
    this.select(target.value);
    queueMicrotask(() =>
      this.host.nativeElement
        .querySelector<HTMLElement>(`[data-value="${CSS.escape(target.value)}"]`)
        ?.focus(),
    );
  }
}
