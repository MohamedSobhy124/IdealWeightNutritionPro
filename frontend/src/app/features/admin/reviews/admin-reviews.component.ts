import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal, viewChild } from '@angular/core';
import { RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { ADMIN_UI, AdminUiKey } from '../../../core/i18n/admin-ui-text';
import { AdminReviewListItem } from '../../../core/models/review.models';
import { LocaleService } from '../../../core/services/locale.service';
import { ReviewService } from '../../../core/services/review.service';
import { UiConfirmService } from '../../../shared/ui/ui-confirm.service';
import { UiToastService } from '../../../shared/ui/ui-toast.service';
import {
  UiActionMenuComponent,
  UiAlertComponent,
  UiBadgeComponent,
  UiCellDirective,
  UiDataTableComponent,
  UiPageHeaderComponent,
  type UiMenuItem,
  type UiTableColumn,
  type UiTableTab,
} from '../../../shared/ui';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [
    RouterLink,
    DatePipe,
    UiPageHeaderComponent,
    UiDataTableComponent,
    UiCellDirective,
    UiBadgeComponent,
    UiActionMenuComponent,
    UiAlertComponent,
  ],
  templateUrl: './admin-reviews.component.html',
  styleUrl: './admin-reviews.component.css',
})
export class AdminReviewsComponent implements OnInit {
  private readonly reviewsApi = inject(ReviewService);
  readonly locale = inject(LocaleService);
  private readonly confirmDialog = inject(UiConfirmService);
  private readonly toast = inject(UiToastService);

  private readonly table = viewChild(UiDataTableComponent);

  readonly items = signal<AdminReviewListItem[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly busyId = signal<number | null>(null);
  readonly bulkBusy = signal(false);
  readonly selected = signal<AdminReviewListItem[]>([]);
  readonly status = signal('pending');

  readonly stars = [1, 2, 3, 4, 5];

  readonly tabs = computed<UiTableTab[]>(() => {
    this.locale.isArabic();
    return [
      { value: 'pending', label: this.t('filterPending') },
      { value: 'approved', label: this.t('filterApproved') },
      { value: 'all', label: this.t('filterAll') },
    ];
  });

  readonly selectedPending = computed(() => this.selected().filter((r) => !r.isApproved));

  t(key: AdminUiKey): string {
    return this.locale.pick(ADMIN_UI[key].en, ADMIN_UI[key].ar);
  }

  get cols(): UiTableColumn[] {
    return [
      { key: 'comment', header: this.t('reviewComment'), mobile: 'primary', hideable: false },
      { key: 'product', header: this.t('reviewProduct'), mobile: 'secondary' },
      { key: 'rating', header: this.t('reviewRating') },
      { key: 'status', header: this.t('reviewStatus') },
      { key: 'createdAt', header: this.t('date'), hideOnMobile: true, sortable: true },
      { key: 'actions', header: '', noExport: true, hideable: false, align: 'end', mobile: 'actions' },
    ];
  }

  rowMenu(item: AdminReviewListItem): UiMenuItem[] {
    return [
      item.isApproved
        ? { id: 'toggle', label: this.t('reviewPending'), icon: 'visibility_off' }
        : { id: 'toggle', label: this.t('approve'), icon: 'check' },
      { id: 'delete', label: this.t('deleteReview'), icon: 'delete_outline', danger: true, dividerBefore: true },
    ];
  }

  onRowAction(id: string, item: AdminReviewListItem): void {
    if (id === 'toggle') this.toggleApproval(item);
    if (id === 'delete') void this.deleteReview(item);
  }

  ngOnInit(): void {
    this.load();
  }

  onTab(value: string): void {
    this.status.set(value);
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.reviewsApi.listAdmin(this.status()).subscribe({
      next: (items) => {
        this.items.set(items);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(this.t('loadReviewsError'));
        this.loading.set(false);
      },
    });
  }

  toggleApproval(item: AdminReviewListItem): void {
    this.busyId.set(item.id);
    this.reviewsApi.toggleApproval(item.id).subscribe({
      next: () => {
        this.applyToggle([item.id]);
        this.toast.success(this.t('updated'));
        this.busyId.set(null);
      },
      error: () => {
        this.toast.error(this.t('actionFailed'));
        this.busyId.set(null);
      },
    });
  }

  /** Approves the selected pending reviews (the API toggles, so approved ones are skipped). */
  approveSelected(): void {
    const targets = this.selectedPending();
    if (!targets.length) return;
    this.bulkBusy.set(true);
    forkJoin(
      targets.map((r) =>
        this.reviewsApi.toggleApproval(r.id).pipe(
          map(() => r.id),
          catchError(() => of(null)),
        ),
      ),
    ).subscribe((ids) => {
      const ok = ids.filter((x): x is number => x !== null);
      this.applyToggle(ok);
      if (ok.length) this.toast.success(this.t('updated'));
      if (ok.length < ids.length) this.toast.error(this.t('bulkPartialFailed'));
      this.bulkBusy.set(false);
      this.table()?.clearSelection();
    });
  }

  private applyToggle(ids: number[]): void {
    const set = new Set(ids);
    const tab = this.status();
    this.items.update((list) =>
      list
        .map((r) => (set.has(r.id) ? { ...r, isApproved: !r.isApproved } : r))
        // Drop rows that no longer match the current tab.
        .filter((r) => tab === 'all' || (tab === 'approved' ? r.isApproved : !r.isApproved)),
    );
  }

  async deleteReview(item: AdminReviewListItem): Promise<void> {
    const confirmed = await this.confirmDialog.danger(
      this.t('confirmDeleteReview'),
      `${item.userName}: “${(item.comment ?? '').slice(0, 80)}”. ${this.t('actionCannotBeUndone')}`,
      this.t('deleteReview'),
    );
    if (!confirmed) return;
    this.busyId.set(item.id);
    this.reviewsApi.deleteReview(item.id).subscribe({
      next: () => {
        this.items.update((list) => list.filter((r) => r.id !== item.id));
        this.toast.success(this.t('updated'));
        this.busyId.set(null);
      },
      error: () => {
        this.toast.error(this.t('actionFailed'));
        this.busyId.set(null);
      },
    });
  }
}
