import { ChangeDetectionStrategy, Component, computed, inject, OnInit, PLATFORM_ID, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { RouterLink } from '@angular/router';

import { CatalogueService } from '../../core/services/catalogue.service';
import { LocaleService } from '../../core/services/locale.service';
import { ServiceSubscriptionService } from '../../core/services/service-subscription.service';
import { UI, UiKey } from '../../core/i18n/ui-text';
import { ServiceSubscriptionSummary } from '../../core/models/service.models';
import { serviceTypeLabel } from '../../core/utils/status-label';
import { IwnCurrencyPipe } from '../../core/pipes/iwn-currency.pipe';
import { UiEmptyStateComponent, UiSkeletonComponent } from '../../shared/ui';

/** Page-local copy (not shared, so kept out of ui-text.ts). */
const COPY = {
  eyebrow: { en: 'Expert-led wellness', ar: 'عافية بإشراف المختصين' },
  statServices: { en: 'Services', ar: 'خدمات' },
  statOnOffer: { en: 'On offer', ar: 'عليها عروض' },
  statBestSaving: { en: 'Best saving', ar: 'أعلى توفير' },
  howTitle: { en: 'How it works', ar: 'كيف تعمل الخدمة' },
  step1Title: { en: 'Consultation', ar: 'الاستشارة' },
  step1Text: { en: 'We get to know your goals, habits and health history.', ar: 'نتعرف على أهدافك وعاداتك وتاريخك الصحي.' },
  step2Title: { en: 'Your plan', ar: 'خطتك' },
  step2Text: { en: 'A personalised nutrition and lifestyle plan built for you.', ar: 'خطة تغذية ونمط حياة مصممة خصيصًا لك.' },
  step3Title: { en: 'Follow-up', ar: 'المتابعة' },
  step3Text: { en: 'Regular check-ins to track progress and adjust the plan.', ar: 'متابعة دورية لقياس التقدم وتعديل الخطة.' },
  save: { en: 'Save', ar: 'وفر' },
} as const;

type CopyKey = keyof typeof COPY;

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [RouterLink, IwnCurrencyPipe, UiSkeletonComponent, UiEmptyStateComponent],
  templateUrl: './service-list.component.html',
  styleUrl: './service-list.component.css',
})
export class ServiceListComponent implements OnInit {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly servicesApi = inject(ServiceSubscriptionService);
  readonly catalogue = inject(CatalogueService);
  readonly locale = inject(LocaleService);

  readonly services = signal<ServiceSubscriptionSummary[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  readonly steps = [
    { icon: 'support_agent', title: 'step1Title', text: 'step1Text' },
    { icon: 'assignment', title: 'step2Title', text: 'step2Text' },
    { icon: 'insights', title: 'step3Title', text: 'step3Text' },
  ] as const;

  /** View-only stats derived from the loaded list. */
  readonly onOfferCount = computed(() => this.services().filter((s) => this.onOffer(s)).length);
  readonly bestSaving = computed(() =>
    this.services().reduce((max, s) => Math.max(max, this.savingPercent(s)), 0),
  );

  t(key: UiKey): string {
    return this.locale.pick(UI[key].en, UI[key].ar);
  }

  c(key: CopyKey): string {
    return this.locale.pick(COPY[key].en, COPY[key].ar);
  }

  displayTitle(service: ServiceSubscriptionSummary): string {
    return this.locale.pick(service.title, service.titleAr);
  }

  displayDescription(service: ServiceSubscriptionSummary): string {
    return this.locale.pick(service.description ?? '', service.descriptionAr);
  }

  typeLabel(type: string | null | undefined): string {
    return serviceTypeLabel(type, (key) => this.t(key));
  }

  typeIcon(type: string | null | undefined): string {
    return type === 'Offline' ? 'storefront' : 'videocam';
  }

  onOffer(service: ServiceSubscriptionSummary): boolean {
    return service.salePrice != null && service.hasActiveOffer;
  }

  savingPercent(service: ServiceSubscriptionSummary): number {
    if (!this.onOffer(service) || service.price <= 0) return 0;
    const pct = Math.round((1 - (service.salePrice as number) / service.price) * 100);
    return pct > 0 ? pct : 0;
  }

  ngOnInit(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    this.servicesApi.listServices().subscribe({
      next: (services) => {
        this.services.set(services);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(this.t('servicesLoadError'));
        this.loading.set(false);
      },
    });
  }
}
