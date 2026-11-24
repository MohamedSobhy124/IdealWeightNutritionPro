import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { IwnCurrencyPipe } from '../../../core/pipes/iwn-currency.pipe';
import { ADMIN_UI, AdminUiKey } from '../../../core/i18n/admin-ui-text';
import { UI, UiKey } from '../../../core/i18n/ui-text';
import { statusLabel } from '../../../core/utils/status-label';
import { ReturnListItem } from '../../../core/models/return.models';
import { LocaleService } from '../../../core/services/locale.service';
import { ReturnService } from '../../../core/services/return.service';
import {
  statusVariant,
  UiAlertComponent,
  UiPageHeaderComponent,
  UiDataTableComponent,
  UiCellDirective,
  UiBadgeComponent,
  type UiBadgeVariant,
  type UiTableColumn,
  type UiTableTab,
} from '../../../shared/ui';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [
    RouterLink,
    DatePipe,
    IwnCurrencyPipe,
    UiPageHeaderComponent,
    UiDataTableComponent,
    UiCellDirective,
    UiBadgeComponent,
    UiAlertComponent,
  ],
  templateUrl: './admin-returns.component.html',
  styleUrl: './admin-returns.component.css',
})
export class AdminReturnsComponent implements OnInit {
  private readonly returnsApi = inject(ReturnService);
  readonly locale = inject(LocaleService);

  readonly items = signal<ReturnListItem[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly status = signal('all');

  readonly tabs = computed<UiTableTab[]>(() => [
    { value: 'all', label: this.t('filterAll') },
    { value: 'Pending', label: this.t('filterPending') },
    { value: 'Approved', label: this.t('filterApproved') },
    { value: 'Processing', label: this.t('filterProcessing') },
    { value: 'Completed', label: this.t('filterCompleted') },
    { value: 'Rejected', label: this.t('filterRejected') },
  ]);

  t(key: AdminUiKey): string {
    return this.locale.pick(ADMIN_UI[key].en, ADMIN_UI[key].ar);
  }

  get cols(): UiTableColumn[] {
    return [
      { key: 'id', header: this.t('returnId'), sortable: true, mobile: 'primary', hideable: false },
      { key: 'orderId', header: this.t('orderNumber'), sortable: true },
      { key: 'status', header: this.t('productStatus') },
      { key: 'customerEmail', header: this.t('customer'), mobile: 'secondary' },
      { key: 'refundAmount', header: this.t('refundCol'), align: 'end' },
      { key: 'requestDate', header: this.t('date'), sortable: true },
    ];
  }

  readonly statusVariant: (status: string) => UiBadgeVariant = statusVariant;

  statusText(status: string): string {
    return statusLabel(status, (key: UiKey) => this.locale.pick(UI[key].en, UI[key].ar));
  }

  onTab(value: string): void {
    this.status.set(value);
    this.load();
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.returnsApi.listAdmin(this.status()).subscribe({
      next: (items) => {
        this.items.set(items);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(this.t('loadReturnsError'));
        this.loading.set(false);
      },
    });
  }
}
