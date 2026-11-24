import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ADMIN_UI, AdminUiKey } from '../../../core/i18n/admin-ui-text';
import { AdminBlogPostListItem } from '../../../core/models/admin-blog.models';
import { AdminBlogService } from '../../../core/services/admin-blog.service';
import { LocaleService } from '../../../core/services/locale.service';
import { UiToastService } from '../../../shared/ui/ui-toast.service';
import { UiConfirmService } from '../../../shared/ui/ui-confirm.service';
import {
  UiBadgeComponent,
  UiCellDirective,
  UiDataTableComponent,
  UiPageHeaderComponent,
  type UiTableColumn,
  UiAlertComponent,
  UiActionMenuComponent,
} from '../../../shared/ui';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [
    UiActionMenuComponent,
    UiAlertComponent,
    RouterLink,
    DatePipe,
    UiPageHeaderComponent,
    UiDataTableComponent,
    UiCellDirective,
    UiBadgeComponent,
  ],
  templateUrl: './admin-blog-posts.component.html',
  styleUrl: './admin-blog-posts.component.css',
})
export class AdminBlogPostsComponent implements OnInit {
  private readonly blogApi = inject(AdminBlogService);
  readonly locale = inject(LocaleService);
  private readonly toast = inject(UiToastService);
  private readonly confirmDialog = inject(UiConfirmService);

  readonly items = signal<AdminBlogPostListItem[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly message = signal<string | null>(null);
  readonly busyId = signal<number | null>(null);

  t(key: AdminUiKey): string {
    return this.locale.pick(ADMIN_UI[key].en, ADMIN_UI[key].ar);
  }

  get cols(): UiTableColumn[] {
    return [
      { key: 'title', header: this.t('title'), sortable: true },
      { key: 'category', header: this.t('blogCategory'), hideOnMobile: true },
      { key: 'author', header: this.t('blogAuthor'), hideOnMobile: true },
      { key: 'publishedDate', header: this.t('date') },
      { key: 'status', header: this.t('productStatus') },
      { key: 'actions', header: '', noExport: true, hideable: false, width: '3.5rem', align: 'end', mobile: 'actions' },
    ];
  }

  displayTitle(item: AdminBlogPostListItem): string {
    return this.locale.pick(item.title, item.titleAr);
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.blogApi.list().subscribe({
      next: (items) => {
        this.items.set(items);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(this.t('loadBlogPostsError'));
        this.loading.set(false);
      },
    });
  }

  async remove(item: AdminBlogPostListItem): Promise<void> {
    const confirmed = await this.confirmDialog.ask({
      title: this.t('confirmDeleteBlogPost'),
      confirmLabel: this.t('confirmAction'),
      danger: true,
      icon: 'visibility_off',
    });
    if (!confirmed) return;
    this.busyId.set(item.id);
    this.message.set(null);
    this.blogApi.delete(item.id).subscribe({
      next: () => {
        this.items.update((list) =>
          list.map((p) => (p.id === item.id ? { ...p, isDeleted: true } : p))
        );
        this.toast.success(this.t('updated'));
        this.busyId.set(null);
      },
      error: () => {
        this.message.set(this.t('actionFailed'));
        this.busyId.set(null);
      },
    });
  }

  restore(item: AdminBlogPostListItem): void {
    this.busyId.set(item.id);
    this.message.set(null);
    this.blogApi.restore(item.id).subscribe({
      next: () => {
        this.items.update((list) =>
          list.map((p) => (p.id === item.id ? { ...p, isDeleted: false } : p))
        );
        this.toast.success(this.t('updated'));
        this.busyId.set(null);
      },
      error: () => {
        this.message.set(this.t('actionFailed'));
        this.busyId.set(null);
      },
    });
  }
}
