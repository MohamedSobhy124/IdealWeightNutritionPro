import { isPlatformBrowser } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, input, PLATFORM_ID } from '@angular/core';
import { ChartConfiguration, ChartData, ChartType } from 'chart.js';
import { BaseChartDirective } from 'ng2-charts';

@Component({
  selector: 'ui-chart',
  standalone: true,
  imports: [BaseChartDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="relative" [style.height]="height()">
      @if (isBrowser) {
        <canvas
          baseChart
          [type]="type()"
          [data]="data()"
          [options]="mergedOptions()"
        ></canvas>
      } @else {
        <div class="ui-skeleton h-full w-full"></div>
      }
    </div>
  `,
})
export class UiChartComponent {
  private readonly platformId = inject(PLATFORM_ID);
  protected readonly isBrowser = isPlatformBrowser(this.platformId);

  readonly type = input<ChartType>('line');
  readonly data = input.required<ChartData>();
  readonly options = input<ChartConfiguration['options']>({});
  readonly height = input('300px');

  protected mergedOptions(): ChartConfiguration['options'] {
    return {
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: {
          labels: { usePointStyle: true, boxWidth: 8, font: { family: 'Inter' } },
        },
      },
      ...this.options(),
    };
  }
}
