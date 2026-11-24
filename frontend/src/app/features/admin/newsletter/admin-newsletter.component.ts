import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { ADMIN_UI, AdminUiKey } from '../../../core/i18n/admin-ui-text';
import { NewsletterSubscription } from '../../../core/models/newsletter.models';
import { LocaleService } from '../../../core/services/locale.service';
import { UiConfirmService } from '../../../shared/ui/ui-confirm.service';
import { UiToastService } from '../../../shared/ui/ui-toast.service';
import { NewsletterService } from '../../../core/services/newsletter.service';
import {
  UiBadgeComponent,
  UiCellDirective,
  UiDataTableComponent,
  UiActionMenuComponent,
  UiAlertComponent,
  UiPageHeaderComponent,
  type UiMenuItem,
  type UiTableTab,
  type UiTableColumn,
} from '../../../shared/ui';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [
    DatePipe,
    UiPageHeaderComponent,
    UiDataTableComponent,
    UiCellDirective,
    UiBadgeComponent,
    UiActionMenuComponent,
    UiAlertComponent,
  ],
  templateUrl: './admin-newsletter.component.html',
  styleUrl: './admin-newsletter.component.css',
})
export class AdminNewsletterComponent implements OnInit {
  private readonly newsletterApi = inject(NewsletterService);
  readonly locale = inject(LocaleService);
  private readonly confirmDialog = inject(UiConfirmService);
  private readonly toast = inject(UiToastService);

  readonly items = signal<NewsletterSubscription[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly busyId = signal<number | null>(null);
  readonly exporting = signal(false);
  readonly status = signal('all');

  readonly tabs = computed<UiTableTab[]>(() => [
    { value: 'all', label: this.t('filterAll') },
    { value: 'active', label: this.t('filterActive') },
    { value: 'inactive', label: this.t('filterInactive') },
  ]);

  t(key: AdminUiKey): string {
    return this.locale.pick(ADMIN_UI[key].en, ADMIN_UI[key].ar);
  }

  get cols(): UiTableColumn[] {
    return [
      { key: 'email', header: this.t('email'), sortable: true },
      { key: 'subscribedDate', header: this.t('subscribedDate'), sortable: true },
      { key: 'source', header: this.t('source'), hideOnMobile: true },
      { key: 'status', header: this.t('productStatus') },
      { key: 'actions', header: '', noExport: true, hideable: false, width: '3.5rem', align: 'end', mobile: 'actions' },
    ];
  }

  onTab(value: string): void {
    this.status.set(value);
    this.load();
  }

  rowMenu(item: NewsletterSubscription): UiMenuItem[] {
    return [
      item.isActive
        ? { id: 'toggle', label: this.t('deactivate'), icon: 'unsubscribe' }
        : { id: 'toggle', label: this.t('activate'), icon: 'mark_email_read' },
      { id: 'delete', label: this.t('remove'), icon: 'delete_outline', danger: true, dividerBefore: true },
    ];
  }

  onRowAction(id: string, item: NewsletterSubscription): void {
    if (id === 'toggle') this.toggle(item);
    if (id === 'delete') void this.remove(item);
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.newsletterApi.listAdmin(this.status()).subscribe({
      next: (items) => {
        this.items.set(items);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(this.t('loadNewsletterError'));
        this.loading.set(false);
      },
    });
  }

  toggle(item: NewsletterSubscription): void {
    this.busyId.set(item.id);
    this.newsletterApi.toggleActive(item.id).subscribe({
      next: () => {
        this.items.update((list) =>
          list.map((n) => (n.id === item.id ? { ...n, isActive: !n.isActive } : n))
        );
        this.toast.success(this.t('updated'));
        this.busyId.set(null);
      },
      error: () => {
        this.toast.error(this.t('actionFailed'));
        this.busyId.set(null);
      },
    });
  }

  async remove(item: NewsletterSubscription): Promise<void> {
    if (!(await this.confirmDialog.danger(this.t('confirmDeleteSubscription'), item.email + '. ' + this.t('actionCannotBeUndone'), this.t('remove')))) return;
    this.busyId.set(item.id);
    this.newsletterApi.delete(item.id).subscribe({
      next: () => {
        this.items.update((list) => list.filter((n) => n.id !== item.id));
        this.toast.success(this.t('updated'));
        this.busyId.set(null);
      },
      error: () => {
        this.toast.error(this.t('actionFailed'));
        this.busyId.set(null);
      },
    });
  }

  exportCsv(): void {
    this.exporting.set(true);
    this.newsletterApi.exportActive().subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const anchor = document.createElement('a');
        anchor.href = url;
        anchor.download = `newsletter-subscribers-${new Date().toISOString().slice(0, 10)}.csv`;
        anchor.click();
        URL.revokeObjectURL(url);
        this.exporting.set(false);
      },
      error: () => {
        this.toast.error(this.t('exportFailed'));
        this.exporting.set(false);
      },
    });
  }
}
