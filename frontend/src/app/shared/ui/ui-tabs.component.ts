import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { UiTabItem } from './ui-tabs.types';

export type { UiTabItem } from './ui-tabs.types';

@Component({
  selector: 'ui-tabs',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="flex flex-wrap gap-2"
      role="tablist"
      [attr.aria-label]="ariaLabel()"
      (keydown)="onListKeydown($event)"
    >
      @for (tab of tabs(); track tab.id; let i = $index) {
        <button
          type="button"
          role="tab"
          class="rounded-full border px-4 py-2 text-sm font-medium transition-colors min-h-[44px]"
          [class.border-brand-600]="activeId() === tab.id"
          [class.bg-brand-600]="activeId() === tab.id"
          [class.text-white]="activeId() === tab.id"
          [class.border-ink-200]="activeId() !== tab.id"
          [class.bg-surface]="activeId() !== tab.id"
          [class.text-ink-700]="activeId() !== tab.id"
          [id]="tabButtonId(tab)"
          [attr.aria-selected]="activeId() === tab.id"
          [attr.aria-controls]="tabPanelId(tab)"
          [attr.tabindex]="activeId() === tab.id ? 0 : -1"
          (click)="selectTab(tab.id)"
        >
          {{ tab.label }}
        </button>
      }
    </div>
  `,
  styles: `:host { display: block; } .bg-surface { background: var(--surface); }`,
})
export class UiTabsComponent {
  readonly tabs = input.required<UiTabItem[]>();
  readonly activeId = input.required<string>();
  readonly ariaLabel = input('Tabs');
  readonly tabIdPrefix = input('tab');
  readonly panelIdPrefix = input('tab-panel');
  readonly tabChange = output<string>();

  protected tabButtonId(tab: UiTabItem): string {
    return `${this.tabIdPrefix()}-${tab.id}`;
  }

  protected tabPanelId(tab: UiTabItem): string {
    return `${this.panelIdPrefix()}-${tab.id}`;
  }

  protected selectTab(id: string): void {
    this.tabChange.emit(id);
  }

  protected onListKeydown(event: KeyboardEvent): void {
    const tabs = this.tabs();
    if (tabs.length === 0) return;

    const currentIndex = tabs.findIndex((tab) => tab.id === this.activeId());
    if (currentIndex < 0) return;

    let nextIndex: number | null = null;
    switch (event.key) {
      case 'ArrowRight':
      case 'ArrowDown':
        nextIndex = (currentIndex + 1) % tabs.length;
        break;
      case 'ArrowLeft':
      case 'ArrowUp':
        nextIndex = (currentIndex - 1 + tabs.length) % tabs.length;
        break;
      case 'Home':
        nextIndex = 0;
        break;
      case 'End':
        nextIndex = tabs.length - 1;
        break;
      default:
        return;
    }

    event.preventDefault();
    const next = tabs[nextIndex];
    this.tabChange.emit(next.id);
    queueMicrotask(() => document.getElementById(this.tabButtonId(next))?.focus());
  }
}
