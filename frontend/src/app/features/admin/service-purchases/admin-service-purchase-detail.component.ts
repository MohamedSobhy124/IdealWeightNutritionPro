import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { IwnCurrencyPipe } from '../../../core/pipes/iwn-currency.pipe';
import { ADMIN_UI, AdminUiKey } from '../../../core/i18n/admin-ui-text';
import { UI, UiKey } from '../../../core/i18n/ui-text';
import { AdminServicePurchaseDetail } from '../../../core/models/admin-service-purchase.models';
import { AdminServicePurchaseService } from '../../../core/services/admin-service-purchase.service';
import { LocaleService } from '../../../core/services/locale.service';
import { buildServicePurchaseTimeline } from '../../../core/utils/service-timeline';
import { statusLabel } from '../../../core/utils/status-label';
import {
  UiBadgeComponent,
  UiBadgeVariant,
  UiCardComponent,
  UiEmptyStateComponent,
  UiFormFieldComponent,
  UiPageHeaderComponent,
  UiSkeletonComponent,
  UiTimelineComponent,
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
    UiEmptyStateComponent,
    UiFormFieldComponent,
    UiSkeletonComponent,
    UiTimelineComponent,
  ],
  templateUrl: './admin-service-purchase-detail.component.html',
  styleUrl: './admin-service-purchase-detail.component.css',
})
export class AdminServicePurchaseDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly purchasesApi = inject(AdminServicePurchaseService);
  readonly locale = inject(LocaleService);

  readonly purchase = signal<AdminServicePurchaseDetail | null>(null);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly saving = signal(false);
  readonly actionMessage = signal<string | null>(null);
  readonly actionError = signal(false);

  paymentStatus = '';
  serviceStatus = '';
  amountPaid = 0;

  readonly purchaseTimeline = computed(() => {
    this.locale.locale();
    const current = this.purchase();
    if (!current) return [];
    return buildServicePurchaseTimeline(
      {
        status: current.serviceStatus,
        paymentStatus: current.paymentStatus,
        purchaseDate: current.purchaseDate,
      },
      (key: UiKey) => this.locale.pick(UI[key].en, UI[key].ar),
    );
  });

  t(key: AdminUiKey): string {
    return this.locale.pick(ADMIN_UI[key].en, ADMIN_UI[key].ar);
  }

  labelStatus(status: string | null | undefined): string {
    return statusLabel(status, (key: UiKey) => this.locale.pick(UI[key].en, UI[key].ar));
  }

  statusVariant(status: string): UiBadgeVariant {
    const s = (status || '').toLowerCase();
    if (s.includes('paid') || s.includes('complete') || s.includes('active')) return 'success';
    if (s.includes('fail') || s.includes('cancel') || s.includes('reject')) return 'danger';
    if (s.includes('pending')) return 'warning';
    return 'neutral';
  }

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!id) {
      this.error.set(this.t('invalidServicePurchase'));
      this.loading.set(false);
      return;
    }

    this.purchasesApi.get(id).subscribe({
      next: (detail) => {
        this.purchase.set(detail);
        this.paymentStatus = detail.paymentStatus;
        this.serviceStatus = detail.serviceStatus;
        this.amountPaid = detail.amountPaid;
        this.loading.set(false);
      },
      error: () => {
        this.error.set(this.t('servicePurchaseNotFound'));
        this.loading.set(false);
      },
    });
  }

  save(): void {
    const p = this.purchase();
    if (!p) return;
    this.saving.set(true);
    this.actionMessage.set(null);
    this.actionError.set(false);
    this.purchasesApi
      .update(p.id, {
        paymentStatus: this.paymentStatus.trim() || undefined,
        serviceStatus: this.serviceStatus.trim() || undefined,
        amountPaid: this.amountPaid,
      })
      .subscribe({
        next: (res) => {
          this.actionMessage.set(res.message);
          this.actionError.set(false);
          this.saving.set(false);
          this.ngOnInit();
        },
        error: (err) => {
          this.actionMessage.set(err.error?.errors?.[0] ?? this.t('actionFailed'));
          this.actionError.set(true);
          this.saving.set(false);
        },
      });
  }
}
