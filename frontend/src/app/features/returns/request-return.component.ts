import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { UI, UiKey } from '../../core/i18n/ui-text';
import { Order, OrderLine } from '../../core/models/order.models';
import { AuthService } from '../../core/services/auth.service';
import { LocaleService } from '../../core/services/locale.service';
import { OrderService } from '../../core/services/order.service';
import { ReturnService } from '../../core/services/return.service';
import { canRequestReturn } from '../../core/utils/return-eligibility';
import {
  UiBadgeComponent,
  UiCardComponent,
  UiEmptyStateComponent,
  UiFormFieldComponent,
  UiPageHeaderComponent,
  UiSkeletonComponent,
  statusVariant,
} from '../../shared/ui';

interface ReturnLineRow {
  orderDetailId: number;
  title: string;
  maxQuantity: number;
  quantity: number;
  itemReason: string;
  itemCondition: string;
}

@Component({
  standalone: true,
  imports: [ReactiveFormsModule, FormsModule, RouterLink, UiPageHeaderComponent, UiCardComponent, UiSkeletonComponent, UiEmptyStateComponent, UiFormFieldComponent, UiBadgeComponent],
  templateUrl: './request-return.component.html',
  styleUrl: './request-return.component.css',
})
export class RequestReturnComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly orders = inject(OrderService);
  private readonly returns = inject(ReturnService);
  readonly auth = inject(AuthService);
  readonly locale = inject(LocaleService);

  readonly loading = signal(true);
  readonly submitting = signal(false);
  readonly error = signal<string | null>(null);
  readonly order = signal<Order | null>(null);
  readonly lines = signal<ReturnLineRow[]>([]);
  readonly guestEmail = signal('');

  readonly form = this.fb.nonNullable.group({
    reason: ['', [Validators.required, Validators.maxLength(500)]],
    additionalNotes: ['', Validators.maxLength(2000)],
    email: ['', [Validators.email]],
  });

  readonly conditions = ['Unopened', 'Opened', 'Damaged', 'Defective'] as const;
  readonly canRequestReturn = canRequestReturn;
  readonly statusVariant = statusVariant;

  t(key: UiKey): string {
    return this.locale.pick(UI[key].en, UI[key].ar);
  }

  ngOnInit(): void {
    const orderId = Number(this.route.snapshot.paramMap.get('orderId'));
    if (!Number.isFinite(orderId) || orderId <= 0) {
      this.error.set(this.t('invalidOrder'));
      this.loading.set(false);
      return;
    }

    const emailFromQuery = this.route.snapshot.queryParamMap.get('email')?.trim() ?? '';
    if (emailFromQuery) {
      this.guestEmail.set(emailFromQuery);
      this.form.controls.email.setValue(emailFromQuery);
    }

    if (!this.auth.isAuthenticated()) {
      this.form.controls.email.addValidators(Validators.required);
    }

    const email = this.auth.isAuthenticated() ? undefined : emailFromQuery || undefined;
    this.orders.getOrder(orderId, email).subscribe({
      next: (order) => {
        this.order.set(order);
        if (!this.auth.isAuthenticated() && order.email) {
          this.guestEmail.set(order.email);
          this.form.controls.email.setValue(order.email);
        }
        this.initLines(order.items);
        if (!canRequestReturn(order)) {
          this.error.set(this.t('returnNotEligible'));
        }
        this.loading.set(false);
      },
      error: () => {
        this.error.set(this.t('couldNotLoadOrder'));
        this.loading.set(false);
      },
    });
  }

  private initLines(items: OrderLine[]): void {
    const rows: ReturnLineRow[] = [];
    for (const item of items) {
      if (!item.orderDetailId) continue;
      rows.push({
        orderDetailId: item.orderDetailId,
        title: item.title,
        maxQuantity: item.quantity,
        quantity: 0,
        itemReason: '',
        itemCondition: '',
      });
    }
    this.lines.set(rows);
  }

  selectedCount(): number {
    return this.lines().filter((l) => l.quantity > 0).length;
  }

  submit(): void {
    if (this.form.invalid || !this.order() || this.submitting()) return;
    if (!canRequestReturn(this.order()!)) {
      this.error.set(this.t('returnNotEligible'));
      return;
    }

    const selected = this.lines().filter((l) => l.quantity > 0);
    if (selected.length === 0) {
      this.error.set(this.t('returnSelectItems'));
      return;
    }

    this.submitting.set(true);
    this.error.set(null);

    const { reason, additionalNotes, email } = this.form.getRawValue();
    const orderId = this.order()!.id;

    this.returns
      .create({
        orderId,
        reason: reason.trim(),
        additionalNotes: additionalNotes.trim() || undefined,
        email: this.auth.isAuthenticated() ? undefined : email.trim(),
        items: selected.map((l) => ({
          orderDetailId: l.orderDetailId,
          quantity: l.quantity,
          itemReason: l.itemReason.trim() || undefined,
          itemCondition: l.itemCondition || undefined,
        })),
      })
      .subscribe({
        next: (created) => {
          const guestEmail = this.auth.isAuthenticated() ? undefined : email.trim();
          void this.router.navigate(['/returns', created.id], {
            queryParams: guestEmail ? { email: guestEmail } : {},
          });
        },
        error: (err) => {
          this.error.set(err?.error?.errors?.[0] ?? this.t('returnSubmitFailed'));
          this.submitting.set(false);
        },
      });
  }
}
