import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

export interface UiStepperStep {
  label: string;
}

/** Numbered progress for a short sequence of steps. Labels are supplied by the caller. */
@Component({
  selector: 'ui-stepper',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  styles: `
    :host {
      display: block;
    }

    .checkout-stepper {
      position: relative;
      margin-bottom: 1.5rem;
      overflow: hidden;
      border-radius: 1rem;
      border: 1px solid var(--border);
      background: #fff;
      padding: 1.15rem 1rem;
      box-shadow: var(--shadow-card);
    }

    @media (min-width: 640px) {
      .checkout-stepper {
        padding: 1.25rem 1.15rem;
      }
    }

    .checkout-stepper__track {
      position: absolute;
      inset-inline-start: 2rem;
      inset-inline-end: 2rem;
      top: 1.7rem;
      display: none;
      height: 0.2rem;
      overflow: hidden;
      border-radius: 9999px;
      background: var(--ink-100, #f3f4f6);
    }

    @media (min-width: 640px) {
      .checkout-stepper__track {
        display: block;
      }
    }

    .checkout-stepper__fill {
      height: 100%;
      border-radius: 9999px;
      background: rgb(var(--rgb-brand-700));
      transition: width 300ms ease-out;
    }

    .checkout-stepper__list {
      position: relative;
      display: grid;
      gap: 0.25rem;
    }

    @media (min-width: 640px) {
      .checkout-stepper__list {
        gap: 0.5rem;
      }
    }

    .checkout-stepper__item {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 0.4rem;
      text-align: center;
    }

    @media (min-width: 640px) {
      .checkout-stepper__item {
        gap: 0.5rem;
      }
    }

    .checkout-stepper__dot {
      position: relative;
      z-index: 10;
      display: flex;
      height: 2.35rem;
      width: 2.35rem;
      align-items: center;
      justify-content: center;
      border-radius: 9999px;
      border: 2px solid var(--border);
      background: #fff;
      font-size: 0.875rem;
      font-weight: 800;
      font-variant-numeric: tabular-nums;
      color: var(--ink-400);
      transition: background 150ms ease, border-color 150ms ease, color 150ms ease, box-shadow 150ms ease;
    }

    @media (min-width: 640px) {
      .checkout-stepper__dot {
        height: 2.55rem;
        width: 2.55rem;
      }
    }

    .checkout-stepper__dot--done {
      border-color: rgb(var(--rgb-brand-solid) / 0.35);
      background: rgb(var(--rgb-brand-50));
      color: rgb(var(--rgb-brand-solid));
      box-shadow: none;
    }

    .checkout-stepper__dot--current {
      border-color: rgb(var(--rgb-brand-700));
      background: rgb(var(--rgb-brand-700));
      color: #fff;
      box-shadow: 0 0 0 4px rgb(var(--rgb-brand-700) / 0.14);
    }

    .checkout-stepper__dot--upcoming {
      border-color: var(--border);
      background: #fff;
      color: var(--ink-400);
    }

    .checkout-stepper__label {
      max-width: 4.5rem;
      font-size: 10px;
      font-weight: 600;
      line-height: 1.25;
      color: var(--ink-500);
    }

    @media (min-width: 640px) {
      .checkout-stepper__label {
        max-width: none;
        font-size: 0.75rem;
      }
    }

    .checkout-stepper__label--active {
      color: rgb(var(--rgb-brand-700));
      font-weight: 700;
    }

    :host-context(.dark) .checkout-stepper {
      background: var(--surface-raised);
      border-color: var(--border);
    }

    :host-context(.dark) .checkout-stepper__dot--upcoming {
      background: var(--surface);
    }
  `,
  template: `
    <nav class="checkout-stepper" [attr.aria-label]="ariaLabel()">
      <div class="checkout-stepper__track" aria-hidden="true">
        <div class="checkout-stepper__fill" [style.width.%]="progress()"></div>
      </div>
      <ol
        class="checkout-stepper__list"
        [style.grid-template-columns]="'repeat(' + steps().length + ', minmax(0, 1fr))'"
      >
        @for (step of steps(); track step.label; let i = $index) {
          <li class="checkout-stepper__item">
            <span
              class="checkout-stepper__dot"
              [class.checkout-stepper__dot--done]="current() > i"
              [class.checkout-stepper__dot--current]="current() === i"
              [class.checkout-stepper__dot--upcoming]="current() < i"
              [attr.aria-current]="current() === i ? 'step' : null"
            >
              @if (current() > i) {
                <span class="material-icons !text-[18px]" aria-hidden="true">check</span>
              } @else {
                {{ i + 1 }}
              }
            </span>
            <span class="checkout-stepper__label" [class.checkout-stepper__label--active]="current() >= i">
              {{ step.label }}
            </span>
          </li>
        }
      </ol>
    </nav>
  `,
})
export class UiStepperComponent {
  readonly steps = input.required<UiStepperStep[]>();
  readonly current = input(0);
  readonly ariaLabel = input<string | null>(null);

  protected readonly progress = computed(() => {
    const last = this.steps().length - 1;
    if (last <= 0) return 0;
    return Math.round((Math.min(this.current(), last) / last) * 100);
  });
}
