import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Order } from '../../core/models/order.models';
import { OrderService } from '../../core/services/order.service';
import { LocaleService } from '../../core/services/locale.service';
import { IwnCurrencyPipe } from '../../core/pipes/iwn-currency.pipe';
import { UI, UiKey } from '../../core/i18n/ui-text';
import { canRequestReturn } from '../../core/utils/return-eligibility';
import { buildOrderTimeline } from '../../core/utils/order-timeline';
import { statusLabel } from '../../core/utils/status-label';

import { UiBadgeComponent, UiCardComponent, UiEmptyStateComponent, UiPageHeaderComponent, UiSkeletonComponent, UiTimelineComponent, statusVariant } from '../../shared/ui';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [RouterLink, DatePipe, IwnCurrencyPipe, UiPageHeaderComponent, UiCardComponent, UiSkeletonComponent, UiEmptyStateComponent, UiBadgeComponent, UiTimelineComponent],
  templateUrl: './order-confirmation.component.html',
  styleUrl: './order-confirmation.component.css',
})
export class OrderConfirmationComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly orders = inject(OrderService);
  readonly locale = inject(LocaleService);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly order = signal<Order | null>(null);
  readonly guestEmail = signal<string | undefined>(undefined);
  readonly downloadingInvoice = signal(false);
  readonly accountCreated = signal(false);
  readonly accountLinked = signal(false);
  readonly canRequestReturn = canRequestReturn;
  readonly statusVariant = statusVariant;

  t(key: UiKey): string {
    return this.locale.pick(UI[key].en, UI[key].ar);
  }

  labelStatus(status: string | null | undefined): string {
    return statusLabel(status, (key) => this.t(key));
  }

  timelineItems(order: Order) {
    return buildOrderTimeline(order, (key) => this.t(key));
  }

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!Number.isFinite(id)) {
      this.error.set(this.t('invalidOrder'));
      this.loading.set(false);
      return;
    }

    const nav = history.state as {
      order?: { orderId: number; orderTotal: number; orderStatus: string; paymentStatus: string };
      email?: string;
      accountCreated?: boolean;
      accountLinked?: boolean;
    };
    const email = nav?.email;
    this.guestEmail.set(email);
    this.accountCreated.set(!!nav?.accountCreated);
    this.accountLinked.set(!!nav?.accountLinked);

    this.orders.getOrder(id, email).subscribe({
      next: (order) => {
        this.order.set(order);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(this.t('couldNotLoadOrder'));
        this.loading.set(false);
      },
    });
  }

  downloadInvoice(): void {
    const order = this.order();
    if (!order) return;
    this.downloadingInvoice.set(true);
    this.orders.downloadInvoice(order.id, this.guestEmail()).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const anchor = document.createElement('a');
        anchor.href = url;
        anchor.download = `Invoice-${order.id}.pdf`;
        anchor.click();
        URL.revokeObjectURL(url);
        this.downloadingInvoice.set(false);
      },
      error: () => {
        this.downloadingInvoice.set(false);
      },
    });
  }
}
