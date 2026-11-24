import { CdkMenu, CdkMenuItem, CdkMenuTrigger } from '@angular/cdk/menu';
import { ConnectedPosition } from '@angular/cdk/overlay';
import { ChangeDetectionStrategy, Component, inject, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LocaleService } from '../../core/services/locale.service';

export interface UiMenuItem {
  id: string;
  label: string;
  icon?: string;
  /** Red text; put destructive actions last. */
  danger?: boolean;
  disabled?: boolean;
  /** Draws a divider above this item. */
  dividerBefore?: boolean;
  /** Navigates instead of emitting `select`. */
  link?: string | (string | number)[];
  /** Hide the item without rebuilding the array. */
  hidden?: boolean;
}

/**
 * Overflow (⋮ / ⋯) menu for row and page actions. Keyboard: Enter/Space opens, arrows move, Esc closes.
 *
 * ```html
 * <ui-action-menu [items]="rowMenu(p)" (select)="onRowAction($event, p)" />
 * ```
 */
@Component({
  selector: 'ui-action-menu',
  standalone: true,
  imports: [CdkMenuTrigger, CdkMenu, CdkMenuItem, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'inline-flex' },
  template: `
    <button
      type="button"
      [class]="variant() === 'button' ? 'ui-btn-secondary ui-btn-sm !px-2' : 'ui-btn-icon'"
      [cdkMenuTriggerFor]="menu"
      [cdkMenuPosition]="positions"
      [attr.aria-label]="label() || locale.pick('More actions', 'إجراءات إضافية')"
      [disabled]="disabled()"
      (click)="$event.stopPropagation()"
    >
      <span class="material-icons !text-[20px]" aria-hidden="true">{{ icon() }}</span>
    </button>

    <ng-template #menu>
      <div
        cdkMenu
        class="min-w-[12rem] max-w-xs rounded-xl border border-ink-200 bg-surface-raised p-1.5 shadow-elevated animate-slide-up focus:outline-none"
      >
        @for (item of items(); track item.id) {
          @if (!item.hidden) {
            @if (item.dividerBefore) {
              <div class="my-1.5 border-t border-ink-100" role="separator"></div>
            }
            @if (item.link && !item.disabled) {
              <a
                cdkMenuItem
                [routerLink]="item.link"
                class="flex min-h-10 w-full items-center gap-3 rounded-lg px-3 text-sm no-underline hover:bg-ink-50 focus:bg-ink-50 focus:outline-none"
                [class.text-danger]="item.danger"
                [class.text-ink-700]="!item.danger"
              >
                @if (item.icon) {
                  <span class="material-icons !text-[18px]" [class.text-ink-400]="!item.danger" aria-hidden="true">{{ item.icon }}</span>
                }
                <span class="truncate">{{ item.label }}</span>
              </a>
            } @else {
              <button
                type="button"
                cdkMenuItem
                [cdkMenuItemDisabled]="!!item.disabled"
                (cdkMenuItemTriggered)="select.emit(item.id)"
                class="flex min-h-10 w-full cursor-pointer items-center gap-3 rounded-lg border-0 bg-transparent px-3 text-start text-sm hover:bg-ink-50 focus:bg-ink-50 focus:outline-none disabled:cursor-not-allowed disabled:opacity-45"
                [class.text-danger]="item.danger"
                [class.text-ink-700]="!item.danger"
              >
                @if (item.icon) {
                  <span class="material-icons !text-[18px]" [class.text-ink-400]="!item.danger" aria-hidden="true">{{ item.icon }}</span>
                }
                <span class="truncate">{{ item.label }}</span>
              </button>
            }
          }
        }
      </div>
    </ng-template>
  `,
})
export class UiActionMenuComponent {
  protected readonly locale = inject(LocaleService);

  readonly items = input.required<readonly UiMenuItem[]>();
  /** `more_vert` for rows, `more_horiz` for page headers. */
  readonly icon = input('more_vert');
  /** `icon` = borderless icon button (rows); `button` = bordered (page headers). */
  readonly variant = input<'icon' | 'button'>('icon');
  readonly label = input<string | null>(null);
  readonly disabled = input(false);

  readonly select = output<string>();

  protected readonly positions: ConnectedPosition[] = [
    { originX: 'end', originY: 'bottom', overlayX: 'end', overlayY: 'top', offsetY: 4 },
    { originX: 'end', originY: 'top', overlayX: 'end', overlayY: 'bottom', offsetY: -4 },
  ];
}
