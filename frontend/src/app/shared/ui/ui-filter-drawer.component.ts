import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { UiModalComponent } from './ui-modal.component';

@Component({
  selector: 'ui-filter-drawer',
  standalone: true,
  imports: [UiModalComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styles: `
    .ui-filter-drawer__actions {
      display: flex;
      align-items: center;
      justify-content: flex-end;
      gap: 0.55rem;
      border-top: 1px solid var(--border);
      background: var(--surface);
      padding: 0.85rem 1.25rem calc(0.85rem + env(safe-area-inset-bottom));
    }

    .ui-filter-drawer__apply {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      min-height: 3rem;
      padding: 0.55rem 1.05rem;
      border-radius: var(--radius-full);
      border: 1px solid rgb(var(--rgb-brand-700));
      background: rgb(var(--rgb-brand-700));
      color: #fff;
      font-size: 0.875rem;
      font-weight: 700;
      box-shadow: 0 6px 16px rgb(var(--rgb-brand-700) / 0.24);
    }

    .ui-filter-drawer__apply:hover {
      background: rgb(var(--rgb-brand-solid-hover));
      color: #fff;
    }
  `,
  template: `
    @if (open()) {
      <ui-modal
        [open]="true"
        [heading]="heading()"
        [closeLabel]="closeLabel()"
        [panelId]="panelId()"
        [clearBottomNav]="true"
        size="md"
        (closed)="closed.emit()"
      >
        <ng-content />
        <div class="ui-filter-drawer__actions" modal-footer>
          <button type="button" class="ui-btn-secondary min-h-12" (click)="closed.emit()">{{ cancelLabel() }}</button>
          <button type="button" class="ui-filter-drawer__apply min-h-12" (click)="apply.emit()">{{ applyLabel() }}</button>
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
