import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ADMIN_UI, AdminUiKey } from '../../../core/i18n/admin-ui-text';
import { AdminPromoCodeListItem } from '../../../core/models/admin-promo.models';
import { AdminServiceListItem } from '../../../core/models/admin-service.models';
import { UpsertAdminServiceOfferRequest } from '../../../core/models/admin-service-offer.models';
import { AdminPromoService } from '../../../core/services/admin-promo.service';
import { AdminServiceService } from '../../../core/services/admin-service.service';
import { AdminServiceOfferService } from '../../../core/services/admin-service-offer.service';
import { LocaleService } from '../../../core/services/locale.service';
import {
  UiCardComponent,
  UiEmptyStateComponent,
  UiFormFieldComponent,
  UiPageHeaderComponent,
  UiSkeletonComponent,
} from '../../../shared/ui';

@Component({
  standalone: true,
  imports: [
    RouterLink,
    FormsModule,
    UiPageHeaderComponent,
    UiCardComponent,
    UiFormFieldComponent,
    UiEmptyStateComponent,
    UiSkeletonComponent,
  ],
  templateUrl: './admin-service-offer-form.component.html',
  styleUrl: './admin-service-offer-form.component.css',
})
export class AdminServiceOfferFormComponent implements OnInit {
  private readonly offersApi = inject(AdminServiceOfferService);
  private readonly servicesApi = inject(AdminServiceService);
  private readonly promoApi = inject(AdminPromoService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  readonly locale = inject(LocaleService);

  readonly loading = signal(true);
  readonly submitting = signal(false);
  readonly error = signal<string | null>(null);
  readonly saveError = signal<string | null>(null);
  readonly isEdit = signal(false);
  readonly services = signal<AdminServiceListItem[]>([]);
  readonly promos = signal<AdminPromoCodeListItem[]>([]);

  offerId: number | null = null;
  serviceSubscriptionId: number | null = null;
  promoCodeId: number | null = null;
  discountType: 'Percentage' | 'FixedAmount' = 'Percentage';
  discountValue = 0;
  startDate = '';
  endDate = '';
  isActive = true;

  t(key: AdminUiKey): string {
    return this.locale.pick(ADMIN_UI[key].en, ADMIN_UI[key].ar);
  }

  serviceLabel(s: AdminServiceListItem): string {
    return this.locale.pick(s.title, s.titleAr ?? s.title);
  }

  ngOnInit(): void {
    this.servicesApi.list(true).subscribe({
      next: (items) => this.services.set(items),
      error: () => this.services.set([]),
    });

    this.promoApi.list().subscribe({
      next: (items) => this.promos.set(items.filter((p) => p.isActive)),
      error: () => this.promos.set([]),
    });

    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam && idParam !== 'new') {
      const id = Number(idParam);
      if (!id) {
        this.error.set(this.t('invalidServiceOffer'));
        this.loading.set(false);
        return;
      }
      this.isEdit.set(true);
      this.offerId = id;
      this.offersApi.get(id).subscribe({
        next: (offer) => {
          this.applyOffer(offer);
          this.loading.set(false);
        },
        error: () => {
          this.error.set(this.t('serviceOfferNotFound'));
          this.loading.set(false);
        },
      });
    } else {
      const serviceIdParam = this.route.snapshot.queryParamMap.get('serviceSubscriptionId');
      if (serviceIdParam) {
        const sid = Number(serviceIdParam);
        if (sid) this.serviceSubscriptionId = sid;
      }
      const today = new Date();
      const nextMonth = new Date(today);
      nextMonth.setMonth(nextMonth.getMonth() + 1);
      this.startDate = this.toDateInput(today.toISOString());
      this.endDate = this.toDateInput(nextMonth.toISOString());
      this.loading.set(false);
    }
  }

  private applyOffer(offer: {
    serviceSubscriptionId: number;
    promoCodeId?: number | null;
    discountType: unknown;
    discountValue: number;
    startDate: string;
    endDate: string;
    isActive: boolean;
  }): void {
    this.serviceSubscriptionId = offer.serviceSubscriptionId;
    this.promoCodeId = offer.promoCodeId ?? null;
    const dt = String(offer.discountType);
    this.discountType = dt === 'FixedAmount' || dt === '2' ? 'FixedAmount' : 'Percentage';
    this.discountValue = offer.discountValue;
    this.startDate = this.toDateInput(offer.startDate);
    this.endDate = this.toDateInput(offer.endDate);
    this.isActive = offer.isActive;
  }

  submit(): void {
    if (!this.serviceSubscriptionId || !this.startDate || !this.endDate) {
      this.saveError.set(this.t('serviceOfferRequiredFields'));
      return;
    }

    const request: UpsertAdminServiceOfferRequest = {
      serviceSubscriptionId: this.serviceSubscriptionId,
      promoCodeId: this.promoCodeId ?? undefined,
      discountType: this.discountType,
      discountValue: this.discountValue,
      startDate: new Date(this.startDate).toISOString(),
      endDate: new Date(this.endDate).toISOString(),
      isActive: this.isActive,
    };

    this.submitting.set(true);
    this.saveError.set(null);

    const op =
      this.isEdit() && this.offerId != null
        ? this.offersApi.update(this.offerId, request)
        : this.offersApi.create(request);

    op.subscribe({
      next: (offer) => {
        this.submitting.set(false);
        if (!this.isEdit()) {
          void this.router.navigate(['/admin/service-offers', offer.id]);
        }
      },
      error: (err) => {
        this.saveError.set(err?.error?.errors?.[0] ?? this.t('saveFailed'));
        this.submitting.set(false);
      },
    });
  }

  private toDateInput(iso: string): string {
    return iso.slice(0, 10);
  }
}
