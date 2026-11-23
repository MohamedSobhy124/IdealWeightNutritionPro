import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Order } from '../../core/models/order.models';
import { OrderService } from '../../core/services/order.service';
import { LocaleService } from '../../core/services/locale.service';
import { IwnCurrencyPipe } from '../../core/pipes/iwn-currency.pipe';
import { UI, UiKey } from '../../core/i18n/ui-text';
import { canRequestReturn } from '../../core/utils/return-eligibility';
import { buildOrderTimeline } from '../../core/utils/order-timeline';
import { statusLabel } from '../../core/utils/status-label';
import {
  UiBadgeComponent,
  UiCardComponent,
  UiFormFieldComponent,
  UiPageHeaderComponent,
  UiTimelineComponent,
  UiTimelineItem,
  statusVariant,
} from '../../shared/ui';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [
    RouterLink,
    ReactiveFormsModule,
    DatePipe,
    IwnCurrencyPipe,
    UiPageHeaderComponent,
    UiCardComponent,
    UiBadgeComponent,
    UiTimelineComponent,
    UiFormFieldComponent,
  ],
  templateUrl: './order-tracking.component.html',
  styleUrl: './order-tracking.component.css',
})
export class OrderTrackingComponent {
  private readonly fb = inject(FormBuilder);
  private readonly orders = inject(OrderService);
  readonly locale = inject(LocaleService);

  readonly loading = signal(false);
  readonly attempted = signal(false);
  readonly error = signal<string | null>(null);
  readonly order = signal<Order | null>(null);
  readonly downloadingInvoice = signal(false);
  readonly canRequestReturn = canRequestReturn;
  readonly statusVariant = statusVariant;

  readonly form = this.fb.nonNullable.group({
    orderId: [0, [Validators.required, Validators.min(1)]],
    email: ['', [Validators.required, Validators.email]],
  });

  t(key: UiKey): string {
    return this.locale.pick(UI[key].en, UI[key].ar);
  }

  labelStatus(status: string | null | undefined): string {
    return statusLabel(status, (key) => this.t(key));
  }

  timelineItems(order: Order): UiTimelineItem[] {
    return buildOrderTimeline(order, (key) => this.t(key));
  }

  orderIdError(): string | null {
    if (!this.attempted()) return null;
    const control = this.form.controls.orderId;
    return control.hasError('required') || control.hasError('min') ? this.t('fieldRequired') : null;
  }

  emailError(): string | null {
    if (!this.attempted()) return null;
    const control = this.form.controls.email;
    if (control.hasError('required')) return this.t('fieldRequired');
    return control.hasError('email') ? this.t('invalidEmail') : null;
  }

  submit(): void {
    if (this.form.invalid) {
      this.attempted.set(true);
      return;
    }
    this.attempted.set(false);
    this.loading.set(true);
    this.error.set(null);
    this.order.set(null);

    const { orderId, email } = this.form.getRawValue();
    this.orders.trackOrder({ orderId, email }).subscribe({
      next: (order) => {
        this.order.set(order);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(err?.error?.errors?.[0] ?? this.t('orderNotFound'));
        this.loading.set(false);
      },
    });
  }

  downloadInvoice(): void {
    const order = this.order();
    if (!order) return;
    const email = this.form.controls.email.value;
    this.downloadingInvoice.set(true);
    this.orders.downloadInvoice(order.id, email).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const anchor = document.createElement('a');
        anchor.href = url;
        anchor.download = `Invoice-${order.id}.pdf`;
        anchor.click();
        URL.revokeObjectURL(url);
        this.downloadingInvoice.set(false);
      },
      error: () => this.downloadingInvoice.set(false),
    });
  }
}
