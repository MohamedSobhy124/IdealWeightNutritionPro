import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { IwnCurrencyPipe } from '../../../core/pipes/iwn-currency.pipe';
import { ADMIN_UI, AdminUiKey } from '../../../core/i18n/admin-ui-text';
import { UI, UiKey } from '../../../core/i18n/ui-text';
import { ReturnRequest } from '../../../core/models/return.models';
import { LocaleService } from '../../../core/services/locale.service';
import { ReturnService } from '../../../core/services/return.service';
import { buildReturnTimeline } from '../../../core/utils/return-timeline';
import { statusLabel } from '../../../core/utils/status-label';
import { UiConfirmService } from '../../../shared/ui/ui-confirm.service';
import { UiToastService } from '../../../shared/ui/ui-toast.service';
import {
  statusVariant,
  UiBadgeComponent,
  UiEmptyStateComponent,
  UiBadgeVariant,
  UiCardComponent,
  UiFormFieldComponent,
  UiPageHeaderComponent,
  UiSkeletonComponent,
  UiTimelineComponent,
} from '../../../shared/ui';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [
    DatePipe,
    IwnCurrencyPipe,
    FormsModule,
    RouterLink,
    UiPageHeaderComponent,
    UiCardComponent,
    UiBadgeComponent,
    UiFormFieldComponent,
    UiSkeletonComponent,
    UiTimelineComponent,
    UiEmptyStateComponent,
  ],
  templateUrl: './admin-return-detail.component.html',
  styleUrl: './admin-return-detail.component.css',
})
export class AdminReturnDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly returnsApi = inject(ReturnService);
  readonly locale = inject(LocaleService);
  private readonly toast = inject(UiToastService);
  private readonly confirmDialog = inject(UiConfirmService);

  readonly item = signal<ReturnRequest | null>(null);
  readonly loading = signal(true);
  readonly busy = signal(false);
  readonly loadFailed = signal(false);
  readonly rejectOpen = signal(false);

  rejectionReason = '';
  adminNotes = '';
  refundTransactionId = '';

  readonly returnTimeline = computed(() => {
    this.locale.locale();
    const current = this.item();
    if (!current) return [];
    return buildReturnTimeline(current.status, current.requestDate, (key: UiKey) => this.locale.pick(UI[key].en, UI[key].ar));
  });

  t(key: AdminUiKey): string {
    return this.locale.pick(ADMIN_UI[key].en, ADMIN_UI[key].ar);
  }

  labelStatus(status: string | null | undefined): string {
    return statusLabel(status, (key: UiKey) => this.locale.pick(UI[key].en, UI[key].ar));
  }

  readonly statusVariant: (status: string) => UiBadgeVariant = statusVariant;

  isOpen(r: ReturnRequest): boolean {
    return r.status !== 'Completed' && r.status !== 'Rejected' && r.status !== 'Cancelled';
  }

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!id) {
      this.loading.set(false);
      return;
    }
    this.returnsApi.getAdmin(id).subscribe({
      next: (item) => {
        this.item.set(item);
        this.loading.set(false);
      },
      error: () => {
        this.loadFailed.set(true);
        this.loading.set(false);
      },
    });
  }

  approve(): void {
    this.run(() => this.returnsApi.approve(this.item()!.id, { adminNotes: this.adminNotes || undefined }));
  }

  async reject(): Promise<void> {
    if (!this.rejectionReason.trim()) {
      this.toast.error(this.t('rejectionReasonRequired'));
      return;
    }
    if (!(await this.confirmDialog.danger(this.t('reject') + ' #' + this.item()!.id + '?', this.rejectionReason.trim(), this.t('reject')))) return;
    this.run(() =>
      this.returnsApi.reject(this.item()!.id, {
        rejectionReason: this.rejectionReason.trim(),
        adminNotes: this.adminNotes || undefined,
      })
    );
  }

  receive(): void {
    this.run(() => this.returnsApi.markReceived(this.item()!.id));
  }

  complete(): void {
    this.run(() =>
      this.returnsApi.complete(this.item()!.id, {
        refundTransactionId: this.refundTransactionId || undefined,
      })
    );
  }

  async cancelReturn(): Promise<void> {
    if (!(await this.confirmDialog.danger(this.t('cancel') + ' #' + this.item()!.id + '?', this.t('actionCannotBeUndone'), this.t('cancel')))) return;
    this.run(() =>
      this.returnsApi.cancel(this.item()!.id, {
        reason: this.rejectionReason.trim() || undefined,
        adminNotes: this.adminNotes || undefined,
      })
    );
  }

  private run(action: () => ReturnType<ReturnService['approve']>): void {
    const current = this.item();
    if (!current) return;
    this.busy.set(true);
    action().subscribe({
      next: () => {
        this.returnsApi.getAdmin(current.id).subscribe({
          next: (item) => {
            this.item.set(item);
            this.busy.set(false);
            this.rejectOpen.set(false);
            this.toast.success(this.t('updated'));
          },
        });
      },
      error: (err) => {
        this.toast.error(err.error?.errors?.[0] ?? this.t('actionFailed'));
        this.busy.set(false);
      },
    });
  }
}
