import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'ui-filter-bar',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  styles: `
    :host {
      display: block;
    }

    .ui-filter-bar {
      margin-bottom: 1rem;
      display: flex;
      flex-wrap: wrap;
      align-items: flex-end;
      justify-content: space-between;
      gap: 0.75rem;
      border-radius: 1rem;
      border: 1px solid var(--border);
      background: #fff;
      padding: 0.85rem 1rem;
      box-shadow: var(--shadow-card);
    }

    :host-context(.dark) .ui-filter-bar {
      background: var(--surface-raised);
      border-color: var(--border);
    }
  `,
  template: `
    <div class="ui-filter-bar">
      <ng-content />
    </div>
  `,
})
export class UiFilterBarComponent {}
