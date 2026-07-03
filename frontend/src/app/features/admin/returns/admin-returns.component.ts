import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { IwnCurrencyPipe } from '../../../core/pipes/iwn-currency.pipe';
import { ADMIN_UI, AdminUiKey } from '../../../core/i18n/admin-ui-text';
import { ReturnListItem } from '../../../core/models/return.models';
import { LocaleService } from '../../../core/services/locale.service';
import { ReturnService } from '../../../core/services/return.service';
import {
  UiPageHeaderComponent,
  UiDataTableComponent,
  UiCellDirective,
  UiBadgeComponent,
  type UiBadgeVariant,
  type UiTableColumn,
} from '../../../shared/ui';

@Component({
  standalone: true,
  imports: [
    RouterLink,
    DatePipe,
    IwnCurrencyPipe,
    FormsModule,
    UiPageHeaderComponent,
    UiDataTableComponent,
    UiCellDirective,
    UiBadgeComponent,
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
  status = 'all';

  t(key: AdminUiKey): string {
    return this.locale.pick(ADMIN_UI[key].en, ADMIN_UI[key].ar);
  }

  get cols(): UiTableColumn[] {
    return [
      { key: 'id', header: this.t('returnId'), sortable: true },
      { key: 'orderId', header: this.t('orderNumber'), sortable: true },
      { key: 'status', header: this.t('productStatus') },
      { key: 'customerEmail', header: this.t('customer') },
      { key: 'refundAmount', header: this.t('refundCol'), align: 'end' },
      { key: 'requestDate', header: this.t('date'), sortable: true },
    ];
  }

  statusVariant(status: string): UiBadgeVariant {
    const x = (status || '').toLowerCase();
    if (
      x.includes('deliver') ||
      x.includes('paid') ||
      x.includes('approve') ||
      x.includes('active') ||
      x.includes('complete')
    )
      return 'success';
    if (
      x.includes('cancel') ||
      x.includes('fail') ||
      x.includes('reject') ||
      x.includes('refund') ||
      x.includes('deleted') ||
      x.includes('out')
    )
      return 'danger';
    if (x.includes('ship')) return 'info';
    if (x.includes('process')) return 'brand';
    if (x.includes('pending') || x.includes('low')) return 'warning';
    return 'neutral';
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.returnsApi.listAdmin(this.status).subscribe({
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
