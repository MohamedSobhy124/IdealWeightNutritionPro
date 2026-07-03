import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import {
  UiCardComponent,
  UiEmptyStateComponent,
  UiFormFieldComponent,
  UiPageHeaderComponent,
  UiSkeletonComponent,
} from '../../../shared/ui';
import { ADMIN_UI, AdminUiKey } from '../../../core/i18n/admin-ui-text';
import {
  PromoCodeExclusions,
  PromoExcludedComboOffer,
  PromoExcludedProduct,
  PromoExcludedService,
  UpsertAdminPromoCodeRequest,
} from '../../../core/models/admin-promo.models';
import { AdminProductListItem } from '../../../core/models/admin-product.models';
import { ComboOfferSummary } from '../../../core/models/combo-offer.models';
import { ServiceSubscriptionSummary } from '../../../core/models/service.models';
import { AdminProductService } from '../../../core/services/admin-product.service';
import { AdminPromoService } from '../../../core/services/admin-promo.service';
import { ComboOfferService } from '../../../core/services/combo-offer.service';
import { LocaleService } from '../../../core/services/locale.service';
import { ServiceSubscriptionService } from '../../../core/services/service-subscription.service';

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
  templateUrl: './admin-promo-form.component.html',
  styleUrl: './admin-promo-form.component.css',
})
export class AdminPromoFormComponent implements OnInit {
  private readonly promoApi = inject(AdminPromoService);
  private readonly productsApi = inject(AdminProductService);
  private readonly combosApi = inject(ComboOfferService);
  private readonly servicesApi = inject(ServiceSubscriptionService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  readonly locale = inject(LocaleService);

  readonly loading = signal(true);
  readonly submitting = signal(false);
  readonly error = signal<string | null>(null);
  readonly saveError = signal<string | null>(null);
  readonly exclusionMessage = signal<string | null>(null);
  readonly exclusionError = signal(false);
  readonly isEdit = signal(false);

  readonly exclusions = signal<PromoCodeExclusions | null>(null);
  readonly productResults = signal<AdminProductListItem[]>([]);
  readonly comboResults = signal<ComboOfferSummary[]>([]);
  readonly serviceResults = signal<ServiceSubscriptionSummary[]>([]);
  readonly allCombos = signal<ComboOfferSummary[]>([]);
  readonly allServices = signal<ServiceSubscriptionSummary[]>([]);

  promoId: number | null = null;
  productSearch = '';
  comboSearch = '';
  serviceSearch = '';

  code = '';
  description = '';
  discountType: 'Percentage' | 'FixedAmount' = 'Percentage';
  discountValue = 0;
  minimumOrderAmount: number | null = null;
  maximumDiscountAmount: number | null = null;
  startDate = '';
  endDate = '';
  usageLimit: number | null = null;
  usageLimitPerUser: number | null = null;
  isActive = true;
  excludeDiscountedItems = false;
  excludeAllServices = true;

  t(key: AdminUiKey): string {
    return this.locale.pick(ADMIN_UI[key].en, ADMIN_UI[key].ar);
  }

  ngOnInit(): void {
    this.combosApi.listActive().subscribe({ next: (items) => this.allCombos.set(items) });
    this.servicesApi.listServices().subscribe({ next: (items) => this.allServices.set(items) });

    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam && idParam !== 'new') {
      const id = Number(idParam);
      if (!id) {
        this.error.set(this.t('invalidPromoCode'));
        this.loading.set(false);
        return;
      }
      this.isEdit.set(true);
      this.promoId = id;
      this.promoApi.get(id).subscribe({
        next: (promo) => {
          this.applyPromo(promo);
          this.loadExclusions();
          this.loading.set(false);
        },
        error: () => {
          this.error.set(this.t('promoCodeNotFound'));
          this.loading.set(false);
        },
      });
    } else {
      const today = new Date();
      const nextMonth = new Date(today);
      nextMonth.setMonth(nextMonth.getMonth() + 1);
      this.startDate = this.toDateInput(today.toISOString());
      this.endDate = this.toDateInput(nextMonth.toISOString());
      this.loading.set(false);
    }
  }

  private applyPromo(promo: {
    code: string;
    description: string;
    discountType: unknown;
    discountValue: number;
    minimumOrderAmount?: number;
    maximumDiscountAmount?: number;
    startDate: string;
    endDate: string;
    usageLimit?: number;
    usageLimitPerUser?: number;
    isActive: boolean;
    excludeDiscountedItems: boolean;
    excludeAllServices: boolean;
  }): void {
    this.code = promo.code;
    this.description = promo.description;
    const dt = String(promo.discountType);
    this.discountType = dt === 'FixedAmount' || dt === '2' ? 'FixedAmount' : 'Percentage';
    this.discountValue = promo.discountValue;
    this.minimumOrderAmount = promo.minimumOrderAmount ?? null;
    this.maximumDiscountAmount = promo.maximumDiscountAmount ?? null;
    this.startDate = this.toDateInput(promo.startDate);
    this.endDate = this.toDateInput(promo.endDate);
    this.usageLimit = promo.usageLimit ?? null;
    this.usageLimitPerUser = promo.usageLimitPerUser ?? null;
    this.isActive = promo.isActive;
    this.excludeDiscountedItems = promo.excludeDiscountedItems;
    this.excludeAllServices = promo.excludeAllServices;
  }

  loadExclusions(): void {
    if (!this.promoId) return;
    this.promoApi.getExclusions(this.promoId).subscribe({
      next: (ex) => this.exclusions.set(ex),
      error: () => this.exclusions.set({ products: [], comboOffers: [], services: [] }),
    });
  }

  searchProducts(): void {
    const term = this.productSearch.trim();
    if (!term) {
      this.productResults.set([]);
      return;
    }
    this.productsApi.list(1, 20, term).subscribe({
      next: (res) => {
        const excluded = new Set((this.exclusions()?.products ?? []).map((p) => p.productId));
        this.productResults.set(res.items.filter((p) => !excluded.has(p.id)));
      },
    });
  }

  filterCombos(): void {
    const term = this.comboSearch.trim().toLowerCase();
    const excluded = new Set((this.exclusions()?.comboOffers ?? []).map((c) => c.comboOfferId));
    this.comboResults.set(
      this.allCombos().filter((c) => {
        if (excluded.has(c.id)) return false;
        if (!term) return true;
        return c.name.toLowerCase().includes(term) || (c.nameAr ?? '').toLowerCase().includes(term);
      })
    );
  }

  filterServices(): void {
    const term = this.serviceSearch.trim().toLowerCase();
    const excluded = new Set((this.exclusions()?.services ?? []).map((s) => s.serviceSubscriptionId));
    this.serviceResults.set(
      this.allServices().filter((s) => {
        if (excluded.has(s.id)) return false;
        if (!term) return true;
        return s.title.toLowerCase().includes(term) || (s.titleAr ?? '').toLowerCase().includes(term);
      })
    );
  }

  productLabel(p: AdminProductListItem | PromoExcludedProduct): string {
    const titleAr = 'titleAr' in p ? p.titleAr : undefined;
    return this.locale.pick(p.title, titleAr ?? p.title);
  }

  comboLabel(c: ComboOfferSummary | PromoExcludedComboOffer): string {
    return this.locale.pick(c.name, c.nameAr ?? c.name);
  }

  serviceLabel(s: ServiceSubscriptionSummary | PromoExcludedService): string {
    return this.locale.pick(s.title, s.titleAr ?? s.title);
  }

  addProduct(productId: number): void {
    if (!this.promoId) return;
    this.promoApi.addExcludedProduct(this.promoId, productId).subscribe({
      next: () => {
        this.exclusionMessage.set(this.t('promoExclusionAdded'));
        this.exclusionError.set(false);
        this.productSearch = '';
        this.productResults.set([]);
        this.loadExclusions();
      },
      error: (err) => {
        this.exclusionMessage.set(err?.error?.errors?.[0] ?? this.t('actionFailed'));
        this.exclusionError.set(true);
      },
    });
  }

  removeProduct(exclusionId: number): void {
    this.promoApi.removeExcludedProduct(exclusionId).subscribe({
      next: () => this.loadExclusions(),
      error: () => {
        this.exclusionMessage.set(this.t('actionFailed'));
        this.exclusionError.set(true);
      },
    });
  }

  addCombo(comboOfferId: number): void {
    if (!this.promoId) return;
    this.promoApi.addExcludedCombo(this.promoId, comboOfferId).subscribe({
      next: () => {
        this.exclusionMessage.set(this.t('promoExclusionAdded'));
        this.exclusionError.set(false);
        this.loadExclusions();
        this.filterCombos();
      },
      error: (err) => {
        this.exclusionMessage.set(err?.error?.errors?.[0] ?? this.t('actionFailed'));
        this.exclusionError.set(true);
      },
    });
  }

  removeCombo(exclusionId: number): void {
    this.promoApi.removeExcludedCombo(exclusionId).subscribe({
      next: () => {
        this.loadExclusions();
        this.filterCombos();
      },
      error: () => {
        this.exclusionMessage.set(this.t('actionFailed'));
        this.exclusionError.set(true);
      },
    });
  }

  addService(serviceSubscriptionId: number): void {
    if (!this.promoId) return;
    this.promoApi.addExcludedService(this.promoId, serviceSubscriptionId).subscribe({
      next: () => {
        this.exclusionMessage.set(this.t('promoExclusionAdded'));
        this.exclusionError.set(false);
        this.loadExclusions();
        this.filterServices();
      },
      error: (err) => {
        this.exclusionMessage.set(err?.error?.errors?.[0] ?? this.t('actionFailed'));
        this.exclusionError.set(true);
      },
    });
  }

  removeService(exclusionId: number): void {
    this.promoApi.removeExcludedService(exclusionId).subscribe({
      next: () => {
        this.loadExclusions();
        this.filterServices();
      },
      error: () => {
        this.exclusionMessage.set(this.t('actionFailed'));
        this.exclusionError.set(true);
      },
    });
  }

  submit(): void {
    if (!this.code.trim() || !this.description.trim() || !this.startDate || !this.endDate) {
      this.saveError.set(this.t('promoRequiredFields'));
      return;
    }

    const request: UpsertAdminPromoCodeRequest = {
      code: this.code.trim().toUpperCase(),
      description: this.description.trim(),
      discountType: this.discountType,
      discountValue: this.discountValue,
      minimumOrderAmount: this.minimumOrderAmount ?? undefined,
      maximumDiscountAmount: this.maximumDiscountAmount ?? undefined,
      startDate: new Date(this.startDate).toISOString(),
      endDate: new Date(this.endDate).toISOString(),
      usageLimit: this.usageLimit ?? undefined,
      usageLimitPerUser: this.usageLimitPerUser ?? undefined,
      isActive: this.isActive,
      excludeDiscountedItems: this.excludeDiscountedItems,
      excludeAllServices: this.excludeAllServices,
    };

    this.submitting.set(true);
    this.saveError.set(null);

    const op =
      this.isEdit() && this.promoId != null
        ? this.promoApi.update(this.promoId, request)
        : this.promoApi.create(request);

    op.subscribe({
      next: (promo) => {
        this.submitting.set(false);
        if (!this.isEdit()) {
          void this.router.navigate(['/admin/promo-codes', promo.id]);
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
