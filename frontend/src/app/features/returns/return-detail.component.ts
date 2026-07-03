import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { IwnCurrencyPipe } from '../../core/pipes/iwn-currency.pipe';
import { UI, UiKey } from '../../core/i18n/ui-text';
import { ReturnRequest } from '../../core/models/return.models';
import { AuthService } from '../../core/services/auth.service';
import { LocaleService } from '../../core/services/locale.service';
import { ReturnService } from '../../core/services/return.service';
import {
  UiBadgeComponent,
  UiCardComponent,
  UiEmptyStateComponent,
  UiPageHeaderComponent,
  UiSkeletonComponent,
  UiTimelineComponent,
  statusVariant,
} from '../../shared/ui';

@Component({
  standalone: true,
  imports: [
    RouterLink,
    DatePipe,
    IwnCurrencyPipe,
    UiPageHeaderComponent,
    UiCardComponent,
    UiBadgeComponent,
    UiTimelineComponent,
    UiSkeletonComponent,
    UiEmptyStateComponent,
  ],
  templateUrl: './return-detail.component.html',
  styleUrl: './return-detail.component.css',
})
export class ReturnDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly returns = inject(ReturnService);
  readonly auth = inject(AuthService);
  readonly locale = inject(LocaleService);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly item = signal<ReturnRequest | null>(null);
  readonly guestEmail = signal('');
  readonly statusVariant = statusVariant;

  t(key: UiKey): string {
    return this.locale.pick(UI[key].en, UI[key].ar);
  }

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!Number.isFinite(id) || id <= 0) {
      this.error.set(this.t('invalidReturn'));
      this.loading.set(false);
      return;
    }

    const email = this.route.snapshot.queryParamMap.get('email')?.trim() ?? '';
    this.guestEmail.set(email);

    this.returns.get(id, email || undefined).subscribe({
      next: (ret) => {
        this.item.set(ret);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(this.t('returnNotFound'));
        this.loading.set(false);
      },
    });
  }
}
