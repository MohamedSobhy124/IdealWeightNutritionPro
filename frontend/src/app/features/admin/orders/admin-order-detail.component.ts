import { DatePipe } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { IwnCurrencyPipe } from '../../../core/pipes/iwn-currency.pipe';
import { ADMIN_UI, AdminUiKey } from '../../../core/i18n/admin-ui-text';
import { AdminOrderAuditLog, AdminOrderDetail } from '../../../core/models/admin-order.models';
import { AdminOrderService } from '../../../core/services/admin-order.service';
import { LocaleService } from '../../../core/services/locale.service';
import { ReturnService } from '../../../core/services/return.service';
import {
  UiBadgeComponent,
  UiBadgeVariant,
  UiCardComponent,
  UiEmptyStateComponent,
  UiFormFieldComponent,
  UiPageHeaderComponent,
  UiSkeletonComponent,
  UiTimelineComponent,
  type UiTimelineItem,
} from '../../../shared/ui';

@Component({
  standalone: true,
  imports: [
    DatePipe,
    IwnCurrencyPipe,
    FormsModule,
    UiPageHeaderComponent,
    UiCardComponent,
    UiBadgeComponent,
    UiFormFieldComponent,
    UiEmptyStateComponent,
    UiSkeletonComponent,
    UiTimelineComponent,
  ],
  templateUrl: './admin-order-detail.component.html',
  styleUrl: './admin-order-detail.component.css',
})
export class AdminOrderDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly ordersApi = inject(AdminOrderService);
  private readonly returnsApi = inject(ReturnService);
  readonly locale = inject(LocaleService);

  readonly order = signal<AdminOrderDetail | null>(null);
  readonly auditLogs = signal<AdminOrderAuditLog[]>([]);
  readonly loadingAudit = signal(false);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly busy = signal(false);
  readonly actionMessage = signal<string | null>(null);
  readonly actionError = signal(false);

  carrier = '';
  trackingNumber = '';
  refundAmount = 0;
  refundReason = '';
  forceReason = '';
  readonly lineEdits = signal<Record<number, { quantity: number; unitPrice: number }>>({});

  readonly auditTimelineItems = computed<UiTimelineItem[]>(() =>
    this.auditLogs().map((entry) => {
      const details: string[] = [];
      if (entry.actionDetails) details.push(entry.actionDetails);
      if (entry.performedByUserEmail || entry.performedByUserId) {
        details.push(
          `${this.t('auditPerformedBy')}: ${entry.performedByUserEmail ?? entry.performedByUserId}`
        );
      }
      if (entry.oldOrderStatus || entry.newOrderStatus) {
        details.push(
          `${this.t('auditStatusChange')}: ${entry.oldOrderStatus ?? '—'} → ${entry.newOrderStatus ?? '—'}`
        );
      }
      if (entry.oldPaymentStatus || entry.newPaymentStatus) {
        details.push(
          `${this.t('auditPaymentChange')}: ${entry.oldPaymentStatus ?? '—'} → ${entry.newPaymentStatus ?? '—'}`
        );
      }
      return {
        title: entry.action,
        subtitle: details.join(' · ') || null,
        date: entry.actionDate,
        completed: true,
      };
    })
  );

  t(key: AdminUiKey): string {
    return this.locale.pick(ADMIN_UI[key].en, ADMIN_UI[key].ar);
  }

  statusVariant(status: string): UiBadgeVariant {
    const s = (status || '').toLowerCase();
    if (s.includes('deliver') || s.includes('paid') || s.includes('approve') || s.includes('complete')) return 'success';
    if (s.includes('cancel') || s.includes('fail') || s.includes('reject') || s.includes('refund')) return 'danger';
    if (s.includes('ship')) return 'info';
    if (s.includes('process')) return 'brand';
    if (s.includes('pending')) return 'warning';
    return 'neutral';
  }

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!id) {
      this.error.set(this.t('invalidOrderId'));
      this.loading.set(false);
      return;
    }

    this.load(id);
  }

  canStartProcessing(o: AdminOrderDetail): boolean {
    return !['Cancelled', 'Delivered', 'Processing', 'Shipped'].includes(o.orderStatus);
  }

  canShip(o: AdminOrderDetail): boolean {
    return o.orderStatus !== 'Cancelled' && o.orderStatus !== 'Delivered' && o.orderStatus !== 'Shipped';
  }

  canDeliver(o: AdminOrderDetail): boolean {
    return o.orderStatus === 'Shipped';
  }

  canCancel(o: AdminOrderDetail): boolean {
    return (
      !['Cancelled', 'Shipped', 'Delivered'].includes(o.orderStatus) &&
      o.paymentMethod !== 'Geidea' &&
      o.paymentMethod !== 'Tamara' &&
      o.paymentMethod !== 'Tabby'
    );
  }

  canRecheckPayment(o: AdminOrderDetail): boolean {
    return o.paymentMethod === 'Geidea' || o.paymentMethod === 'Tamara' || o.paymentMethod === 'Tabby';
  }

  canForceComplete(o: AdminOrderDetail): boolean {
    return o.orderStatus !== 'Delivered';
  }

  canForceCancel(o: AdminOrderDetail): boolean {
    return o.orderStatus !== 'Cancelled';
  }

  canRefund(o: AdminOrderDetail): boolean {
    return o.paymentStatus === 'Paid' || o.paymentStatus === 'PartiallyRefunded';
  }

  refundOrder(): void {
    const o = this.order();
    if (!o || this.refundAmount <= 0) return;
    this.busy.set(true);
    this.returnsApi.refundOrder(o.id, { refundAmount: this.refundAmount, reason: this.refundReason || undefined }).subscribe({
      next: (res) => {
        this.actionMessage.set(res.message);
        this.actionError.set(false);
        this.load(o.id);
      },
      error: (err) => {
        this.actionMessage.set(err.error?.errors?.[0] ?? this.t('refundFailed'));
        this.actionError.set(true);
        this.busy.set(false);
      },
    });
  }

  runAction(action: 'processing' | 'ship' | 'deliver' | 'cancel' | 'recheckPayment' | 'forceComplete' | 'forceCancel'): void {
    const o = this.order();
    if (!o) return;

    this.busy.set(true);
    this.actionMessage.set(null);
    this.actionError.set(false);

    const done = {
      next: (res: { message: string }) => {
        this.actionMessage.set(res.message);
        this.load(o.id);
        this.busy.set(false);
      },
      error: (err: { error?: { errors?: string[] } }) => {
        this.actionMessage.set(err.error?.errors?.[0] ?? this.t('actionFailed'));
        this.actionError.set(true);
        this.busy.set(false);
      },
    };

    switch (action) {
      case 'processing':
        this.ordersApi.startProcessing(o.id).subscribe(done);
        break;
      case 'ship':
        if (!this.carrier.trim() || !this.trackingNumber.trim()) {
          this.actionMessage.set(this.t('carrierTrackingRequired'));
          this.actionError.set(true);
          this.busy.set(false);
          return;
        }
        this.ordersApi
          .shipOrder(o.id, { carrier: this.carrier.trim(), trackingNumber: this.trackingNumber.trim() })
          .subscribe(done);
        break;
      case 'deliver':
        this.ordersApi.markDelivered(o.id).subscribe(done);
        break;
      case 'cancel':
        this.ordersApi.cancelOrder(o.id).subscribe(done);
        break;
      case 'recheckPayment':
        this.ordersApi.recheckPayment(o.id).subscribe(done);
        break;
      case 'forceComplete':
        this.ordersApi.forceComplete(o.id, { reason: this.forceReason.trim() || undefined }).subscribe(done);
        break;
      case 'forceCancel':
        this.ordersApi.forceCancel(o.id, { reason: this.forceReason.trim() || undefined }).subscribe(done);
        break;
    }
  }

  lineEditFor(orderDetailId?: number): { quantity: number; unitPrice: number } | null {
    if (!orderDetailId) return null;
    return this.lineEdits()[orderDetailId] ?? null;
  }

  updateLineItem(orderDetailId?: number): void {
    const o = this.order();
    if (!o || !orderDetailId) return;
    const edit = this.lineEdits()[orderDetailId];
    if (!edit || edit.quantity <= 0 || edit.unitPrice < 0) return;

    this.busy.set(true);
    this.actionMessage.set(null);
    this.actionError.set(false);
    this.ordersApi
      .updateLineItem(o.id, { orderDetailId, quantity: edit.quantity, unitPrice: edit.unitPrice })
      .subscribe({
        next: (res) => {
          this.actionMessage.set(res.message);
          this.actionError.set(false);
          this.load(o.id);
          this.busy.set(false);
        },
        error: (err) => {
          this.actionMessage.set(err.error?.errors?.[0] ?? this.t('actionFailed'));
          this.actionError.set(true);
          this.busy.set(false);
        },
      });
  }

  private load(id: number): void {
    this.ordersApi.getOrder(id).subscribe({
      next: (order) => {
        this.order.set(order);
        this.carrier = order.carrier ?? '';
        this.trackingNumber = order.trackingNumber ?? '';
        this.refundAmount = order.orderTotal;
        this.lineEdits.set(
          order.items.reduce<Record<number, { quantity: number; unitPrice: number }>>((acc, item) => {
            if (item.orderDetailId) {
              acc[item.orderDetailId] = { quantity: item.quantity, unitPrice: item.unitPrice };
            }
            return acc;
          }, {})
        );
        this.loading.set(false);
        this.loadAuditLog(id);
      },
      error: (err) => {
        this.error.set(err.error?.errors?.[0] ?? this.t('couldNotLoadOrder'));
        this.loading.set(false);
      },
    });
  }

  private loadAuditLog(orderId: number): void {
    this.loadingAudit.set(true);
    this.ordersApi.getAuditLog(orderId).subscribe({
      next: (logs) => {
        this.auditLogs.set(logs);
        this.loadingAudit.set(false);
      },
      error: () => {
        this.auditLogs.set([]);
        this.loadingAudit.set(false);
      },
    });
  }
}
