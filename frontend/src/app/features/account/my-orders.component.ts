import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { canRequestReturn } from '../../core/utils/return-eligibility';
import { buildOrderTimeline } from '../../core/utils/order-timeline';
import { statusLabel } from '../../core/utils/status-label';
import { LocaleService } from '../../core/services/locale.service';
import { OrderService } from '../../core/services/order.service';
import { IwnCurrencyPipe } from '../../core/pipes/iwn-currency.pipe';
import { UI, UiKey } from '../../core/i18n/ui-text';
import { OrderSummary } from '../../core/models/order.models';
import { UiBadgeComponent, UiBadgeVariant, UiEmptyStateComponent, UiPageHeaderComponent, UiSkeletonComponent, UiTimelineComponent } from '../../shared/ui';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [RouterLink, DatePipe, IwnCurrencyPipe, UiBadgeComponent, UiEmptyStateComponent, UiPageHeaderComponent, UiSkeletonComponent, UiTimelineComponent],
  templateUrl: './my-orders.component.html',
  styleUrl: './my-orders.component.css',
})
export class MyOrdersComponent implements OnInit {
  private readonly ordersApi = inject(OrderService);
  readonly locale = inject(LocaleService);

  readonly orders = signal<OrderSummary[]>([]);
  readonly loading = signal(true);
  readonly downloadingId = signal<number | null>(null);
  readonly canRequestReturn = canRequestReturn;

  t(key: UiKey): string {
    return this.locale.pick(UI[key].en, UI[key].ar);
  }

  labelStatus(status: string | null | undefined): string {
    return statusLabel(status, (key) => this.t(key));
  }

  timelineItems(order: OrderSummary) {
    return buildOrderTimeline(order, (key) => this.t(key));
  }

  statusVariant(status: string | null | undefined): UiBadgeVariant {
    const s = (status ?? '').toLowerCase();
    if (['delivered', 'paid', 'approved', 'active', 'completed'].includes(s)) return 'success';
    if (['cancelled', 'canceled', 'failed', 'rejected', 'refunded'].includes(s)) return 'danger';
    if (s === 'shipped') return 'info';
    if (s === 'processing') return 'brand';
    if (s === 'pending') return 'warning';
    return 'neutral';
  }

  ngOnInit(): void {
    this.ordersApi.listMyOrders().subscribe({
      next: (orders) => {
        this.orders.set(orders);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  downloadInvoice(orderId: number): void {
    this.downloadingId.set(orderId);
    this.ordersApi.downloadInvoice(orderId).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const anchor = document.createElement('a');
        anchor.href = url;
        anchor.download = `Invoice-${orderId}.pdf`;
        anchor.click();
        URL.revokeObjectURL(url);
        this.downloadingId.set(null);
      },
      error: () => this.downloadingId.set(null),
    });
  }
}
