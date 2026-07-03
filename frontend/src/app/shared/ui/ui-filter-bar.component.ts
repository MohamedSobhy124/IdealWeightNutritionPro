import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'ui-filter-bar',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="mb-4 flex flex-wrap items-end justify-between gap-3">
      <ng-content />
    </div>
  `,
})
export class UiFilterBarComponent {}
