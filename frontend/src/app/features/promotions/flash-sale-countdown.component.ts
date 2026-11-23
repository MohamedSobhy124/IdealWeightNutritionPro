import { isPlatformBrowser } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, Input, OnDestroy, OnInit, PLATFORM_ID, signal } from '@angular/core';
import { LocaleService } from '../../core/services/locale.service';
import { UI, UiKey } from '../../core/i18n/ui-text';

/** Live countdown for a sale end time. Announces once a minute, not every second. */
@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-flash-sale-countdown',
  standalone: true,
  host: {
    '[class.is-inverse]': 'tone === "inverse"',
  },
  template: `
    <span class="sr-only" role="timer" [attr.aria-live]="live ? 'polite' : null">{{ announcement() }}</span>
    @if (ended()) {
      <span class="ended" aria-hidden="true">{{ t('saleEnded') }}</span>
    } @else {
      <span class="countdown" [class.countdown--inverse]="tone === 'inverse'" aria-hidden="true">
        <span class="unit">{{ days() }}<small>{{ t('days') }}</small></span>
        <span class="sep" aria-hidden="true">:</span>
        <span class="unit">{{ hours() }}<small>{{ t('hours') }}</small></span>
        <span class="sep" aria-hidden="true">:</span>
        <span class="unit">{{ minutes() }}<small>{{ t('minutes') }}</small></span>
        @if (!reduceMotion) {
          <span class="sep" aria-hidden="true">:</span>
          <span class="unit">{{ seconds() }}<small>{{ t('seconds') }}</small></span>
        }
      </span>
    }
  `,
  styleUrl: './flash-sale-countdown.component.css',
})
export class FlashSaleCountdownComponent implements OnInit, OnDestroy {
  @Input({ required: true }) endDate!: string;
  /** `inverse` is for the brand-colored banner, where the type is white. */
  @Input() tone: 'default' | 'inverse' = 'default';
  /** Announce changes. Leave off when many countdowns are on one page. */
  @Input() live = false;

  private readonly platformId = inject(PLATFORM_ID);
  readonly locale = inject(LocaleService);
  readonly days = signal('00');
  readonly hours = signal('00');
  readonly minutes = signal('00');
  readonly seconds = signal('00');
  readonly ended = signal(false);
  readonly announcement = computed(() => {
    this.locale.locale();
    if (this.ended()) return this.t('saleEnded');
    return this.t('saleTimeLeft')
      .replace('{days}', String(Number(this.days())))
      .replace('{hours}', String(Number(this.hours())))
      .replace('{minutes}', String(Number(this.minutes())));
  });
  reduceMotion = false;

  private timer?: ReturnType<typeof setInterval>;
  private readonly onVisibility = () => {
    if (document.hidden) {
      this.clearTimer();
      return;
    }
    this.tick();
    this.arm();
  };

  ngOnInit(): void {
    if (isPlatformBrowser(this.platformId)) {
      this.reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
      document.addEventListener('visibilitychange', this.onVisibility);
    }
    this.tick();
    this.arm();
  }

  ngOnDestroy(): void {
    this.clearTimer();
    if (isPlatformBrowser(this.platformId)) {
      document.removeEventListener('visibilitychange', this.onVisibility);
    }
  }

  t(key: UiKey): string {
    return this.locale.pick(UI[key].en, UI[key].ar);
  }

  private arm(): void {
    this.clearTimer();
    if (this.ended()) return;
    const every = this.reduceMotion ? 60_000 : 1000;
    this.timer = setInterval(() => this.tick(), every);
  }

  private clearTimer(): void {
    if (this.timer) clearInterval(this.timer);
    this.timer = undefined;
  }

  private tick(): void {
    const remaining = new Date(this.endDate).getTime() - Date.now();
    if (remaining <= 0) {
      this.ended.set(true);
      this.days.set('00');
      this.hours.set('00');
      this.minutes.set('00');
      this.seconds.set('00');
      this.clearTimer();
      return;
    }

    const totalSeconds = Math.floor(remaining / 1000);
    const days = Math.floor(totalSeconds / 86400);
    const hours = Math.floor((totalSeconds % 86400) / 3600);
    const minutes = Math.floor((totalSeconds % 3600) / 60);
    const seconds = totalSeconds % 60;

    this.days.set(String(days).padStart(2, '0'));
    this.hours.set(String(hours).padStart(2, '0'));
    this.minutes.set(String(minutes).padStart(2, '0'));
    this.seconds.set(String(seconds).padStart(2, '0'));
  }
}
