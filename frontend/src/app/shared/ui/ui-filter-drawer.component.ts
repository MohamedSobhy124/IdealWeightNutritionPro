import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { UiModalComponent } from './ui-modal.component';

@Component({
  selector: 'ui-filter-drawer',
  standalone: true,
  imports: [UiModalComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (open()) {
      <ui-modal
        [open]="true"
        [heading]="heading()"
        [closeLabel]="closeLabel()"
        [panelId]="panelId()"
        size="md"
        (closed)="closed.emit()"
      >
        <ng-content />
        <div class="mt-4 flex items-center justify-end gap-2 border-t border-ink-200 pt-3">
          <button type="button" class="ui-btn-secondary min-h-11" (click)="closed.emit()">{{ cancelLabel() }}</button>
          <button type="button" class="ui-btn-primary min-h-11" (click)="apply.emit()">{{ applyLabel() }}</button>
        </div>
      </ui-modal>
    }
  `,
})
export class UiFilterDrawerComponent {
  readonly open = input(false);
  readonly heading = input('Filters');
  readonly cancelLabel = input('Cancel');
  readonly applyLabel = input('Apply');
  readonly closeLabel = input('Close');
  readonly panelId = input<string | null>(null);

  readonly closed = output<void>();
  readonly apply = output<void>();
}
