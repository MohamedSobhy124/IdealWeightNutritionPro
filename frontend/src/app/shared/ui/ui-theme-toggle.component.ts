import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { ThemeService } from '../../core/services/theme.service';

@Component({
  selector: 'ui-theme-toggle',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <button
      type="button"
      class="ui-btn-icon"
      (click)="theme.toggle()"
      [attr.aria-label]="ariaLabel()"
      [attr.aria-pressed]="theme.isDark()"
    >
      <span class="material-icons !text-[22px]">{{ theme.isDark() ? 'light_mode' : 'dark_mode' }}</span>
    </button>
  `,
})
export class UiThemeToggleComponent {
  readonly theme = inject(ThemeService);
  readonly ariaLabel = input('Toggle theme');
}
