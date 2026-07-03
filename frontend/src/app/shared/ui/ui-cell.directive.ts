import { Directive, inject, input, TemplateRef } from '@angular/core';

/**
 * Marks an <ng-template> as the custom cell renderer for a DataTable column.
 * Usage: <ng-template uiCell="status" let-row>...</ng-template>
 * The template context exposes `$implicit` (the row) and `row`.
 */
@Directive({
  selector: '[uiCell]',
  standalone: true,
})
export class UiCellDirective<T = unknown> {
  readonly uiCell = input.required<string>();
  readonly template = inject(TemplateRef<{ $implicit: T; row: T }>);

  static ngTemplateContextGuard<T>(
    _dir: UiCellDirective<T>,
    _ctx: unknown
  ): _ctx is { $implicit: T; row: T } {
    return true;
  }
}
