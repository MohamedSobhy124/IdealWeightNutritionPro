import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { UI, UiKey } from '../../core/i18n/ui-text';
import { Order, OrderLine } from '../../core/models/order.models';
import { AuthService } from '../../core/services/auth.service';
import { LocaleService } from '../../core/services/locale.service';
import { OrderService } from '../../core/services/order.service';
import { ReturnService } from '../../core/services/return.service';
import { canRequestReturn } from '../../core/utils/return-eligibility';
import { statusLabel } from '../../core/utils/status-label';
import {
  UiBadgeComponent,
  UiCardComponent,
  UiEmptyStateComponent,
  UiFormFieldComponent,
  UiPageHeaderComponent,
  UiSheetSelectComponent,
  UiSkeletonComponent,
  UiStepperComponent,
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
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [ReactiveFormsModule, FormsModule, RouterLink, UiPageHeaderComponent, UiCardComponent, UiSkeletonComponent, UiEmptyStateComponent, UiFormFieldComponent, UiBadgeComponent, UiStepperComponent, UiSheetSelectComponent],
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
  readonly step = signal(0);
  /** Set when Continue is pressed on a step that is still incomplete. */
  readonly attempted = signal(false);

  readonly returnSteps = computed(() => {
    this.locale.locale();
    const steps: { id: 'email' | 'reason' | 'items' | 'review'; label: string }[] = [];
    if (!this.auth.isAuthenticated()) steps.push({ id: 'email', label: this.t('returnStepEmail') });
    steps.push(
      { id: 'reason', label: this.t('returnStepReason') },
      { id: 'items', label: this.t('returnStepItems') },
      { id: 'review', label: this.t('returnStepReview') },
    );
    return steps;
  });

  readonly currentKind = computed(() => this.returnSteps()[this.step()]?.id ?? 'reason');
  readonly onReview = computed(() => this.currentKind() === 'review');

  t(key: UiKey): string {
    return this.locale.pick(UI[key].en, UI[key].ar);
  }

  labelStatus(status: string | null | undefined): string {
    return statusLabel(status, (key) => this.t(key));
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

  selectedLines(): ReturnLineRow[] {
    return this.lines().filter((l) => l.quantity > 0);
  }

  conditionOptions() {
    return [
      { value: '', label: this.t('optional') },
      ...this.conditions.map((value) => ({ value, label: this.conditionLabel(value) })),
    ];
  }

  conditionLabel(value: string): string {
    const keyByValue = {
      Unopened: 'returnConditionUnopened',
      Opened: 'returnConditionOpened',
      Damaged: 'returnConditionDamaged',
      Defective: 'returnConditionDefective',
    } as const;
    const key = keyByValue[value as keyof typeof keyByValue];
    return key ? this.t(key) : value;
  }

  emailError(): string | null {
    if (!this.attempted() || this.currentKind() !== 'email') return null;
    const control = this.form.controls.email;
    if (control.hasError('required')) return this.t('fieldRequired');
    return control.hasError('email') ? this.t('invalidEmail') : null;
  }

  reasonError(): string | null {
    if (!this.attempted() || this.currentKind() !== 'reason') return null;
    const control = this.form.controls.reason;
    if (control.hasError('required')) return this.t('fieldRequired');
    return control.hasError('maxlength') ? this.t('returnReasonTooLong') : null;
  }

  itemsError(): string | null {
    if (!this.attempted() || this.currentKind() !== 'items' || this.selectedCount() > 0) return null;
    return this.t('returnSelectItems');
  }

  nextStep(): void {
    if (!this.stepValid()) {
      this.attempted.set(true);
      return;
    }
    this.attempted.set(false);
    this.step.update((value) => Math.min(value + 1, this.returnSteps().length - 1));
  }

  prevStep(): void {
    this.attempted.set(false);
    this.step.update((value) => Math.max(value - 1, 0));
  }

  private stepValid(): boolean {
    switch (this.currentKind()) {
      case 'email':
        return this.form.controls.email.valid;
      case 'reason':
        return this.form.controls.reason.valid;
      case 'items':
        return this.selectedCount() > 0;
      default:
        return true;
    }
  }

  submit(): void {
    if (!this.onReview()) {
      this.nextStep();
      return;
    }
    if (this.form.invalid || !this.order() || this.submitting()) return;
    if (!canRequestReturn(this.order()!)) {
      this.error.set(this.t('returnNotEligible'));
      return;
    }

    if (this.form.invalid) {
      const steps = this.returnSteps();
      const emailIndex = steps.findIndex((step) => step.id === 'email');
      const reasonIndex = steps.findIndex((step) => step.id === 'reason');
      if (emailIndex >= 0 && this.form.controls.email.invalid) this.step.set(emailIndex);
      else if (reasonIndex >= 0 && this.form.controls.reason.invalid) this.step.set(reasonIndex);
      this.attempted.set(true);
      return;
    }

    const selected = this.lines().filter((l) => l.quantity > 0);
    if (selected.length === 0) {
      const itemsIndex = this.returnSteps().findIndex((step) => step.id === 'items');
      if (itemsIndex >= 0) this.step.set(itemsIndex);
      this.attempted.set(true);
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
