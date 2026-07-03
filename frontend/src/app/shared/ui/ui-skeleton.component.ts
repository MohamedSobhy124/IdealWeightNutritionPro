import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'ui-skeleton',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<span
    class="ui-skeleton block"
    [class.rounded-full]="circle()"
    [style.width]="width()"
    [style.height]="height()"
  ></span>`,
})
export class UiSkeletonComponent {
  readonly width = input('100%');
  readonly height = input('1rem');
  readonly circle = input(false);
}
