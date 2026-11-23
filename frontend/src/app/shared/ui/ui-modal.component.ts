import { A11yModule } from '@angular/cdk/a11y';
import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import {
  ChangeDetectionStrategy,
  afterNextRender,
  Component,
  DestroyRef,
  effect,
  ElementRef,
  HostListener,
  inject,
  input,
  output,
  PLATFORM_ID,
} from '@angular/core';

/**
 * Lightweight overlay.
 * - `variant="dialog"`: centered dialog on desktop, bottom sheet on mobile.
 * - `variant="drawer"`: full-height side panel anchored to the inline end (RTL aware).
 *
 * Focus is trapped while open and restored to the trigger on close; page scroll is locked.
 */
@Component({
  selector: 'ui-modal',
  standalone: true,
  imports: [A11yModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    ngSkipHydration: 'true',
  },
  styles: `
    .clear-bottom-nav {
      padding-bottom: calc(3.5rem + 1px);
    }

    @media (min-width: 1024px) {
      .clear-bottom-nav {
        padding-bottom: 0;
      }
    }
  `,
  template: `
    @if (open()) {
      <div
        class="fixed inset-0 z-modal flex"
        [class.clear-bottom-nav]="clearBottomNav()"
        [class.items-end]="variant() === 'dialog'"
        [class.justify-center]="variant() === 'dialog'"
        [class.sm:items-center]="variant() === 'dialog'"
        [class.justify-end]="variant() === 'drawer'"
      >
        <div
          class="absolute inset-0 bg-scrim/50 backdrop-blur-[2px] animate-fade-in"
          (click)="dismissOnBackdrop() && closed.emit()"
          aria-hidden="true"
        ></div>
        <div
          role="dialog"
          aria-modal="true"
          [attr.aria-labelledby]="headingId"
          [attr.id]="panelId() || null"
          cdkTrapFocus
          [cdkTrapFocusAutoCapture]="true"
          class="relative z-10 flex w-full flex-col overflow-hidden bg-surface text-ink-900 shadow-overlay overscroll-contain"
          [class]="panelClass()"
        >
          <header class="flex shrink-0 items-start justify-between gap-3 border-b border-ink-200 px-5 py-4">
            <div class="min-w-0">
              <h2 [id]="headingId" class="text-balance text-base font-semibold text-ink-900">{{ heading() }}</h2>
              @if (description()) {
                <p class="mt-0.5 text-sm text-ink-500">{{ description() }}</p>
              }
            </div>
            <button
              type="button"
              class="ui-btn-icon -me-2 -mt-1 shrink-0"
              (click)="closed.emit()"
              [attr.aria-label]="closeLabel()"
            >
              <span class="material-icons !text-[20px]" aria-hidden="true">close</span>
            </button>
          </header>
          <div class="min-h-0 flex-1 overflow-y-auto px-5 py-4" style="overscroll-behavior: contain">
            <ng-content></ng-content>
          </div>
          <ng-content select="[modal-footer]"></ng-content>
        </div>
      </div>
    }
  `,
})
export class UiModalComponent {
  private static seq = 0;
  private static openCount = 0;

  private readonly document = inject(DOCUMENT);
  private readonly host = inject(ElementRef<HTMLElement>);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  readonly open = input(false);
  readonly heading = input('');
  readonly description = input<string | null>(null);
  readonly size = input<'sm' | 'md' | 'lg' | 'xl'>('md');
  readonly variant = input<'dialog' | 'drawer'>('dialog');
  readonly dismissOnBackdrop = input(true);
  readonly closeLabel = input('Close');
  readonly panelId = input<string | null>(null);
  readonly clearBottomNav = input(false);
  readonly closed = output<void>();

  protected readonly headingId = `ui-modal-heading-${++UiModalComponent.seq}`;
  private locked = false;

  constructor() {
    afterNextRender(() => {
      this.document.body.appendChild(this.host.nativeElement);
    });
    effect(() => this.setScrollLock(this.open()));
    inject(DestroyRef).onDestroy(() => this.setScrollLock(false));
  }

  protected panelClass(): string {
    if (this.variant() === 'drawer') {
      const width = { sm: 'sm:max-w-sm', md: 'sm:max-w-md', lg: 'sm:max-w-xl', xl: 'sm:max-w-3xl' }[this.size()];
      return `ui-drawer-panel h-full ${width}`;
    }
    const width = { sm: 'sm:max-w-md', md: 'sm:max-w-lg', lg: 'sm:max-w-2xl', xl: 'sm:max-w-4xl' }[this.size()];
    return `max-h-[92dvh] rounded-t-3xl animate-sheet-up sm:rounded-2xl sm:animate-slide-up ${width}`;
  }

  private setScrollLock(lock: boolean): void {
    if (!this.isBrowser || lock === this.locked) return;
    this.locked = lock;
    UiModalComponent.openCount += lock ? 1 : -1;
    this.document.body.style.overflow = UiModalComponent.openCount > 0 ? 'hidden' : '';
  }

  @HostListener('document:keydown.escape', ['$event'])
  onEscape(event: Event): void {
    if (!this.open()) return;
    event.preventDefault();
    this.closed.emit();
  }
}
