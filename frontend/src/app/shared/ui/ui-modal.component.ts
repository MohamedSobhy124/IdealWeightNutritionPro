import {
  ChangeDetectionStrategy,
  Component,
  effect,
  ElementRef,
  HostListener,
  input,
  output,
  viewChild,
} from '@angular/core';

/**
 * Lightweight overlay: centered dialog on desktop, bottom sheet on mobile.
 */
@Component({
  selector: 'ui-modal',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    ngSkipHydration: 'true',
  },
  template: `
    @if (open()) {
      <div class="fixed inset-0 z-[100] flex items-end justify-center sm:items-center">
        <div
          class="absolute inset-0 bg-ink-900/40 backdrop-blur-sm animate-fade-in"
          (click)="dismissOnBackdrop() && closed.emit()"
          aria-hidden="true"
        ></div>
        <div
          #dialogPanel
          role="dialog"
          aria-modal="true"
          [attr.aria-labelledby]="headingId"
          [attr.id]="panelId() || null"
          class="relative z-10 flex max-h-[92vh] w-full flex-col overflow-hidden bg-white shadow-overlay
                 animate-sheet-up rounded-t-3xl overscroll-contain
                 sm:w-full sm:animate-slide-up sm:rounded-2xl"
          [class.sm:max-w-md]="size() === 'sm'"
          [class.sm:max-w-lg]="size() === 'md'"
          [class.sm:max-w-2xl]="size() === 'lg'"
          [class.sm:max-w-4xl]="size() === 'xl'"
          style="overscroll-behavior: contain"
        >
          <header class="flex items-center justify-between gap-3 border-b border-ink-200 px-5 py-4">
            <h2 [id]="headingId" class="text-balance text-base font-semibold text-ink-900">{{ heading() }}</h2>
            <button
              #closeButton
              type="button"
              class="ui-btn-icon"
              (click)="closed.emit()"
              [attr.aria-label]="closeLabel()"
            >
              <span class="material-icons !text-[20px]" aria-hidden="true">close</span>
            </button>
          </header>
          <div class="min-h-0 flex-1 overflow-y-auto px-5 py-4" style="overscroll-behavior: contain">
            <ng-content></ng-content>
          </div>
        </div>
      </div>
    }
  `,
})
export class UiModalComponent {
  private static seq = 0;

  readonly open = input(false);
  readonly heading = input('');
  readonly size = input<'sm' | 'md' | 'lg' | 'xl'>('md');
  readonly dismissOnBackdrop = input(true);
  readonly closeLabel = input('Close');
  readonly panelId = input<string | null>(null);
  readonly closed = output<void>();

  protected readonly headingId = `ui-modal-heading-${++UiModalComponent.seq}`;

  private readonly closeButton = viewChild<ElementRef<HTMLButtonElement>>('closeButton');

  constructor() {
    effect(() => {
      if (!this.open()) return;
      queueMicrotask(() => this.closeButton()?.nativeElement.focus());
    });
  }

  @HostListener('document:keydown.escape', ['$event'])
  onEscape(event: Event): void {
    if (!this.open()) return;
    event.preventDefault();
    this.closed.emit();
  }
}
