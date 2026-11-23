import { ChangeDetectionStrategy, Component, ElementRef, inject, input, signal } from '@angular/core';

/** Wraps a projected password input and toggles it between hidden and visible. */
@Component({
  selector: 'ui-password-field',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="ui-password">
      <ng-content />
      <button
        type="button"
        class="ui-password__toggle"
        (click)="toggle()"
        [attr.aria-pressed]="visible()"
        [attr.aria-label]="visible() ? hideLabel() : showLabel()"
      >
        <span class="material-icons text-[20px]" aria-hidden="true">{{ visible() ? 'visibility_off' : 'visibility' }}</span>
      </button>
    </div>
  `,
})
export class UiPasswordFieldComponent {
  private readonly host = inject(ElementRef<HTMLElement>);

  readonly showLabel = input('Show password');
  readonly hideLabel = input('Hide password');
  protected readonly visible = signal(false);

  toggle(): void {
    this.visible.update((value) => !value);
    const input = this.host.nativeElement.querySelector('input');
    if (input) input.type = this.visible() ? 'text' : 'password';
  }
}
