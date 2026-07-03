import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { LocaleService } from '../../core/services/locale.service';
import { OrderService } from '../../core/services/order.service';
import { ServiceSubscriptionService } from '../../core/services/service-subscription.service';
import { IwnCurrencyPipe } from '../../core/pipes/iwn-currency.pipe';
import { UI, UiKey } from '../../core/i18n/ui-text';
import { OrderSummary } from '../../core/models/order.models';
import { ServicePurchaseSummary } from '../../core/models/service-purchase.models';
import { UiBadgeComponent, UiBadgeVariant, UiCardComponent, UiPageHeaderComponent, UiSkeletonComponent } from '../../shared/ui';

@Component({
  standalone: true,
  imports: [RouterLink, DatePipe, FormsModule, IwnCurrencyPipe, UiCardComponent, UiBadgeComponent, UiPageHeaderComponent, UiSkeletonComponent],
  templateUrl: './account-dashboard.component.html',
  styleUrl: './account-dashboard.component.css',
})
export class AccountDashboardComponent implements OnInit {
  readonly auth = inject(AuthService);
  readonly locale = inject(LocaleService);
  private readonly ordersApi = inject(OrderService);
  private readonly servicesApi = inject(ServiceSubscriptionService);

  readonly orders = signal<OrderSummary[]>([]);
  readonly ordersLoading = signal(true);
  readonly subscriptions = signal<ServicePurchaseSummary[]>([]);
  readonly subscriptionsLoading = signal(true);
  readonly actionMessage = signal<string | null>(null);
  readonly actionError = signal(false);
  readonly busy = signal(false);
  currentPassword = '';
  newPassword = '';

  t(key: UiKey): string {
    return this.locale.pick(UI[key].en, UI[key].ar);
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

  displaySubscriptionTitle(sub: ServicePurchaseSummary): string {
    return this.locale.pick(sub.serviceTitle, sub.serviceTitleAr);
  }

  ngOnInit(): void {
    if (this.auth.isAuthenticated() && !this.auth.profile()) {
      this.auth.loadProfile().subscribe();
    }

    this.ordersApi.listMyOrders().subscribe({
      next: (orders) => {
        this.orders.set(orders.slice(0, 5));
        this.ordersLoading.set(false);
      },
      error: () => this.ordersLoading.set(false),
    });

    this.servicesApi.listMyPurchases().subscribe({
      next: (subs) => {
        this.subscriptions.set(subs.slice(0, 5));
        this.subscriptionsLoading.set(false);
      },
      error: () => this.subscriptionsLoading.set(false),
    });
  }

  changePassword(): void {
    if (!this.currentPassword || !this.newPassword) return;
    this.busy.set(true);
    this.auth.changePassword({ currentPassword: this.currentPassword, newPassword: this.newPassword }).subscribe({
      next: (res) => {
        this.actionMessage.set(res.message);
        this.actionError.set(false);
        this.currentPassword = '';
        this.newPassword = '';
        this.busy.set(false);
      },
      error: (err) => {
        this.actionMessage.set(err.error?.errors?.[0] ?? 'Action failed.');
        this.actionError.set(true);
        this.busy.set(false);
      },
    });
  }

  exportPersonalData(): void {
    this.busy.set(true);
    this.auth.exportPersonalData().subscribe({
      next: (res) => {
        const blob = new Blob([res.json], { type: 'application/json' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = 'personal-data.json';
        a.click();
        URL.revokeObjectURL(url);
        this.actionMessage.set('Personal data exported.');
        this.actionError.set(false);
        this.busy.set(false);
      },
      error: () => {
        this.actionMessage.set('Export failed.');
        this.actionError.set(true);
        this.busy.set(false);
      },
    });
  }

  deleteAccount(): void {
    if (!confirm('Delete account and personal data? This cannot be undone.')) return;
    this.busy.set(true);
    this.auth.deletePersonalData().subscribe({
      next: () => {
        this.auth.logout();
      },
      error: () => {
        this.actionMessage.set('Delete failed.');
        this.actionError.set(true);
        this.busy.set(false);
      },
    });
  }
}
