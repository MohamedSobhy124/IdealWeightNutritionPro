import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { IwnCurrencyPipe } from '../../../core/pipes/iwn-currency.pipe';
import { ADMIN_UI, AdminUiKey } from '../../../core/i18n/admin-ui-text';
import { UI, UiKey } from '../../../core/i18n/ui-text';
import { AdminOrderAuditLog, AdminOrderDetail, UpdateOrderDetailsRequest } from '../../../core/models/admin-order.models';
import { AdminOrderService } from '../../../core/services/admin-order.service';
import { LocaleService } from '../../../core/services/locale.service';
import { ReturnService } from '../../../core/services/return.service';
import { buildOrderTimeline } from '../../../core/utils/order-timeline';
import { statusLabel } from '../../../core/utils/status-label';
import { UiConfirmService } from '../../../shared/ui/ui-confirm.service';
import { UiToastService } from '../../../shared/ui/ui-toast.service';
import {
  statusVariant,
  UiActionMenuComponent,
  UiAlertComponent,
  UiBadgeComponent,
  UiBadgeVariant,
  UiCardComponent,
  UiEmptyStateComponent,
  UiFormFieldComponent,
  UiPageHeaderComponent,
  UiSkeletonComponent,
  UiTimelineComponent,
  type UiMenuItem,
  type UiTimelineItem,
} from '../../../shared/ui';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
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
    UiActionMenuComponent,
    UiAlertComponent,
    RouterLink,
  ],
  templateUrl: './admin-order-detail.component.html',
  styleUrl: './admin-order-detail.component.css',
})
export class AdminOrderDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly ordersApi = inject(AdminOrderService);
  private readonly returnsApi = inject(ReturnService);
  readonly locale = inject(LocaleService);
  private readonly toast = inject(UiToastService);
  private readonly confirmDialog = inject(UiConfirmService);

  readonly order = signal<AdminOrderDetail | null>(null);
  readonly auditLogs = signal<AdminOrderAuditLog[]>([]);
  readonly loadingAudit = signal(false);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly busy = signal(false);
  readonly shipOpen = signal(false);
  readonly editingLines = signal(false);
  readonly activityTab = signal<'timeline' | 'audit'>('timeline');

  carrier = '';
  trackingNumber = '';
  refundAmount = 0;
  refundReason = '';
  forceReason = '';

  readonly editingDetails = signal(false);
  readonly downloadingInvoice = signal(false);
  detailsForm: UpdateOrderDetailsRequest = { name: '', phoneNumber: '', streetAddress: '', city: '' };
  readonly lineEdits = signal<Record<number, { quantity: number; unitPrice: number }>>({});

  readonly orderTimeline = computed(() => {
    this.locale.locale();
    const current = this.order();
    if (!current) return [];
    return buildOrderTimeline(current, (key: UiKey) => this.locale.pick(UI[key].en, UI[key].ar));
  });

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
          `${this.t('auditStatusChange')}: ${this.labelStatus(entry.oldOrderStatus) || '—'} → ${this.labelStatus(entry.newOrderStatus) || '—'}`
        );
      }
      if (entry.oldPaymentStatus || entry.newPaymentStatus) {
        details.push(
          `${this.t('auditPaymentChange')}: ${this.labelStatus(entry.oldPaymentStatus) || '—'} → ${this.labelStatus(entry.newPaymentStatus) || '—'}`
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

  labelStatus(status: string | null | undefined): string {
    return statusLabel(status, (key: UiKey) => this.locale.pick(UI[key].en, UI[key].ar));
  }

  readonly statusVariant: (status: string) => UiBadgeVariant = statusVariant;

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
    return !['Cancelled', 'Delivered', 'Processing', 'Shipped', 'OutForDelivery'].includes(o.orderStatus);
  }

  canShip(o: AdminOrderDetail): boolean {
    return !['Cancelled', 'Delivered', 'Shipped', 'OutForDelivery'].includes(o.orderStatus);
  }

  canDeliver(o: AdminOrderDetail): boolean {
    return o.orderStatus === 'Shipped' || o.orderStatus === 'OutForDelivery';
  }

  /** Online-payment orders are cancelled at the gateway by the API, so they are allowed too. */
  canCancel(o: AdminOrderDetail): boolean {
    return !['Cancelled', 'Shipped', 'OutForDelivery', 'Delivered'].includes(o.orderStatus);
  }

  canEditDetails(o: AdminOrderDetail): boolean {
    return o.orderStatus !== 'Cancelled';
  }

  startEditDetails(): void {
    const o = this.order();
    if (!o) return;
    this.detailsForm = {
      name: o.name,
      phoneNumber: o.phoneNumber,
      streetAddress: o.streetAddress,
      city: o.city,
      area: o.area ?? '',
      carrier: o.carrier ?? '',
      trackingNumber: o.trackingNumber ?? '',
    };
    this.editingDetails.set(true);
  }

  cancelEditDetails(): void {
    this.editingDetails.set(false);
  }

  saveDetails(): void {
    const o = this.order();
    if (!o) return;

    const f = this.detailsForm;
    if (!f.name?.trim() || !f.phoneNumber?.trim() || !f.streetAddress?.trim() || !f.city?.trim()) {
      this.toast.error(this.t('orderDetailsRequired'));
      return;
    }

    this.busy.set(true);
    this.ordersApi.updateDetails(o.id, f).subscribe({
      next: (res) => {
        this.toast.success(res.message);
        this.editingDetails.set(false);
        this.busy.set(false);
        this.load(o.id);
      },
      error: (err) => {
        this.toast.error(err.error?.errors?.[0] ?? this.t('actionFailed'));
        this.busy.set(false);
      },
    });
  }

  downloadInvoice(): void {
    const o = this.order();
    if (!o || this.downloadingInvoice()) return;
    this.downloadingInvoice.set(true);
    this.ordersApi.downloadInvoice(o.id).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const anchor = document.createElement('a');
        anchor.href = url;
        anchor.download = `Invoice-${o.id}.pdf`;
        anchor.click();
        URL.revokeObjectURL(url);
        this.downloadingInvoice.set(false);
      },
      error: () => {
        this.toast.error(this.t('actionFailed'));
        this.downloadingInvoice.set(false);
      },
    });
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

  async refundOrder(): Promise<void> {
    const o = this.order();
    if (!o || this.refundAmount <= 0) return;
    const ok = await this.confirmDialog.ask({
      title: this.t('confirmRefund'),
      message: `#${o.id} · ${this.refundAmount.toFixed(2)} AED`,
      confirmLabel: this.t('processRefund'),
      danger: true,
      icon: 'currency_exchange',
    });
    if (!ok) return;
    this.busy.set(true);
    this.returnsApi.refundOrder(o.id, { refundAmount: this.refundAmount, reason: this.refundReason || undefined }).subscribe({
      next: (res) => {
        this.toast.success(res.message);
        this.busy.set(false);
        this.load(o.id);
      },
      error: (err) => {
        this.toast.error(err.error?.errors?.[0] ?? this.t('refundFailed'));
        this.busy.set(false);
      },
    });
  }

  async runAction(action: 'processing' | 'ship' | 'deliver' | 'cancel' | 'recheckPayment' | 'forceComplete' | 'forceCancel'): Promise<void> {
    const o = this.order();
    if (!o) return;
    if (!(await this.confirmIfNeeded(action, o))) return;

    this.busy.set(true);

    const done = {
      next: (res: { message: string }) => {
        this.toast.success(res.message);
        this.shipOpen.set(false);
        this.load(o.id);
        this.busy.set(false);
      },
      error: (err: { error?: { errors?: string[] } }) => {
        this.toast.error(err.error?.errors?.[0] ?? this.t('actionFailed'));
        this.busy.set(false);
      },
    };

    switch (action) {
      case 'processing':
        this.ordersApi.startProcessing(o.id).subscribe(done);
        break;
      case 'ship':
        if (!this.carrier.trim() || !this.trackingNumber.trim()) {
          this.toast.error(this.t('carrierTrackingRequired'));
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

  /** Asks before actions that are hard to undo or that notify the customer. */
  private confirmIfNeeded(action: string, o: AdminOrderDetail): Promise<boolean> {
    switch (action) {
      case 'cancel':
        return this.confirmDialog.ask({
          title: this.t('confirmCancelOrder'),
          message: `#${o.id} · ${o.name}. ${this.t('confirmCancelOrderMsg')}`,
          confirmLabel: this.t('cancelOrder'),
          cancelLabel: this.t('keepOrder'),
          danger: true,
          icon: 'cancel',
        });
      case 'deliver':
        return this.confirmDialog.ask({
          title: this.t('confirmMarkDelivered'),
          message: `#${o.id} · ${o.name}`,
          confirmLabel: this.t('markDelivered'),
          icon: 'task_alt',
        });
      case 'forceComplete':
        return this.confirmDialog.ask({
          title: this.t('confirmForceComplete'),
          message: this.t('forceCompleteHint'),
          confirmLabel: this.t('forceComplete'),
          danger: true,
          icon: 'warning_amber',
        });
      case 'forceCancel':
        return this.confirmDialog.ask({
          title: this.t('confirmForceCancel'),
          message: this.t('forceCancelHint'),
          confirmLabel: this.t('forceCancel'),
          cancelLabel: this.t('keepOrder'),
          danger: true,
          icon: 'warning_amber',
        });
      default:
        return Promise.resolve(true);
    }
  }

  // ---- Header: next step + overflow menu --------------------------------
  /** The one action the order normally needs next, shown as the header's primary button. */
  nextStep(o: AdminOrderDetail): { action: 'processing' | 'ship' | 'deliver'; label: string; icon: string } | null {
    if (this.canStartProcessing(o)) return { action: 'processing', label: this.t('startProcessing'), icon: 'inventory' };
    if (this.canShip(o)) return { action: 'ship', label: this.t('markShipped'), icon: 'local_shipping' };
    if (this.canDeliver(o)) return { action: 'deliver', label: this.t('markDelivered'), icon: 'task_alt' };
    return null;
  }

  runNextStep(o: AdminOrderDetail): void {
    const step = this.nextStep(o);
    if (!step) return;
    if (step.action === 'ship') this.shipOpen.set(true);
    else void this.runAction(step.action);
  }

  headerMenu(o: AdminOrderDetail): UiMenuItem[] {
    const next = this.nextStep(o)?.action;
    return [
      { id: 'ship', label: this.t('markShipped'), icon: 'local_shipping', hidden: !this.canShip(o) || next === 'ship' },
      { id: 'recheck', label: this.t('recheckPayment'), icon: 'refresh', hidden: !this.canRecheckPayment(o) },
      { id: 'edit', label: this.t('orderEditDetails'), icon: 'edit_location_alt', hidden: !this.canEditDetails(o) },
      { id: 'cancel', label: this.t('cancelOrder'), icon: 'cancel', danger: true, dividerBefore: true, hidden: !this.canCancel(o) },
    ];
  }

  onHeaderAction(id: string): void {
    if (id === 'ship') this.shipOpen.set(true);
    if (id === 'recheck') void this.runAction('recheckPayment');
    if (id === 'edit') this.startEditDetails();
    if (id === 'cancel') void this.runAction('cancel');
  }

  /** Progress bar: 0 placed, 1 processing, 2 shipped, 3 delivered. */
  progressIndex(o: AdminOrderDetail): number {
    switch (o.orderStatus) {
      case 'Delivered':
        return 3;
      case 'Shipped':
      case 'OutForDelivery':
        return 2;
      case 'Processing':
      case 'PreparingShipping':
      case 'Preparing Shiping':
        return 1;
      default:
        return 0;
    }
  }

  readonly steps = computed(() => {
    this.locale.isArabic();
    return [this.t('stepPlaced'), this.t('stepProcessing'), this.t('stepShipped'), this.t('stepDelivered')];
  });

  copy(text: string | null | undefined): void {
    if (!text || typeof navigator === 'undefined' || !navigator.clipboard) return;
    void navigator.clipboard.writeText(text).then(() => this.toast.success(this.t('copied')));
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
    this.ordersApi
      .updateLineItem(o.id, { orderDetailId, quantity: edit.quantity, unitPrice: edit.unitPrice })
      .subscribe({
        next: (res) => {
          this.toast.success(res.message);
          this.load(o.id);
          this.busy.set(false);
        },
        error: (err) => {
          this.toast.error(err.error?.errors?.[0] ?? this.t('actionFailed'));
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
