import { Component, inject, Input, OnDestroy, OnInit, signal } from '@angular/core';
import { LocaleService } from '../../core/services/locale.service';
import { UI, UiKey } from '../../core/i18n/ui-text';

@Component({
  selector: 'app-flash-sale-countdown',
  standalone: true,
  template: `
    @if (ended()) {
      <span class="ended">{{ t('saleEnded') }}</span>
    } @else {
      <span class="countdown" [attr.aria-label]="t('saleEndsIn')">
        <span class="unit">{{ days() }}<small>{{ t('days') }}</small></span>
        <span class="sep">:</span>
        <span class="unit">{{ hours() }}<small>{{ t('hours') }}</small></span>
        <span class="sep">:</span>
        <span class="unit">{{ minutes() }}<small>{{ t('minutes') }}</small></span>
        <span class="sep">:</span>
        <span class="unit">{{ seconds() }}<small>{{ t('seconds') }}</small></span>
      </span>
    }
  `,
  styleUrl: './flash-sale-countdown.component.css',
})
export class FlashSaleCountdownComponent implements OnInit, OnDestroy {
  @Input({ required: true }) endDate!: string;

  readonly locale = inject(LocaleService);
  readonly days = signal('00');
  readonly hours = signal('00');
  readonly minutes = signal('00');
  readonly seconds = signal('00');
  readonly ended = signal(false);

  private timer?: ReturnType<typeof setInterval>;

  ngOnInit(): void {
    this.tick();
    this.timer = setInterval(() => this.tick(), 1000);
  }

  ngOnDestroy(): void {
    if (this.timer) clearInterval(this.timer);
  }

  t(key: UiKey): string {
    return this.locale.pick(UI[key].en, UI[key].ar);
  }

  private tick(): void {
    const remaining = new Date(this.endDate).getTime() - Date.now();
    if (remaining <= 0) {
      this.ended.set(true);
      this.days.set('00');
      this.hours.set('00');
      this.minutes.set('00');
      this.seconds.set('00');
      if (this.timer) clearInterval(this.timer);
      return;
    }

    const totalSeconds = Math.floor(remaining / 1000);
    const d = Math.floor(totalSeconds / 86400);
    const h = Math.floor((totalSeconds % 86400) / 3600);
    const m = Math.floor((totalSeconds % 3600) / 60);
    const s = totalSeconds % 60;

    this.days.set(String(d).padStart(2, '0'));
    this.hours.set(String(h).padStart(2, '0'));
    this.minutes.set(String(m).padStart(2, '0'));
    this.seconds.set(String(s).padStart(2, '0'));
  }
}
