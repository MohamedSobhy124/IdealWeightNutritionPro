import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ADMIN_UI, AdminUiKey } from '../../../core/i18n/admin-ui-text';
import { AdminReviewListItem } from '../../../core/models/review.models';
import { LocaleService } from '../../../core/services/locale.service';
import { ReviewService } from '../../../core/services/review.service';
import {
  UiBadgeComponent,
  UiCellDirective,
  UiDataTableComponent,
  UiFilterBarComponent,
  UiPageHeaderComponent,
  type UiTableColumn,
} from '../../../shared/ui';

@Component({
  standalone: true,
  imports: [
    RouterLink,
    DatePipe,
    FormsModule,
    UiPageHeaderComponent,
    UiDataTableComponent,
    UiCellDirective,
    UiBadgeComponent,
    UiFilterBarComponent,
  ],
  templateUrl: './admin-reviews.component.html',
  styleUrl: './admin-reviews.component.css',
})
export class AdminReviewsComponent implements OnInit {
  private readonly reviewsApi = inject(ReviewService);
  readonly locale = inject(LocaleService);

  readonly items = signal<AdminReviewListItem[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly message = signal<string | null>(null);
  readonly busyId = signal<number | null>(null);
  status = 'all';

  t(key: AdminUiKey): string {
    return this.locale.pick(ADMIN_UI[key].en, ADMIN_UI[key].ar);
  }

  get cols(): UiTableColumn[] {
    return [
      { key: 'product', header: this.t('reviewProduct') },
      { key: 'userName', header: this.t('reviewUser') },
      { key: 'rating', header: this.t('reviewRating'), align: 'center' },
      { key: 'comment', header: this.t('reviewComment'), hideOnMobile: true },
      { key: 'status', header: this.t('reviewStatus') },
      { key: 'createdAt', header: this.t('date'), hideOnMobile: true },
      { key: 'actions', header: this.t('actions'), noExport: true, hideOnMobile: true },
    ];
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.reviewsApi.listAdmin(this.status).subscribe({
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
    this.message.set(null);
    this.reviewsApi.toggleApproval(item.id).subscribe({
      next: () => {
        this.items.update((list) =>
          list.map((r) => (r.id === item.id ? { ...r, isApproved: !r.isApproved } : r))
        );
        this.message.set(this.t('updated'));
        this.busyId.set(null);
      },
      error: () => {
        this.message.set(this.t('actionFailed'));
        this.busyId.set(null);
      },
    });
  }

  deleteReview(item: AdminReviewListItem): void {
    if (!confirm('Delete this review?')) return;
    this.busyId.set(item.id);
    this.message.set(null);
    this.reviewsApi.deleteReview(item.id).subscribe({
      next: () => {
        this.items.update((list) => list.filter((r) => r.id !== item.id));
        this.message.set(this.t('updated'));
        this.busyId.set(null);
      },
      error: () => {
        this.message.set(this.t('actionFailed'));
        this.busyId.set(null);
      },
    });
  }
}
