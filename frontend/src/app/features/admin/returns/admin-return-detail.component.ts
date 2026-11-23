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
import {
  UiBadgeComponent,
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
  ],
  templateUrl: './admin-return-detail.component.html',
  styleUrl: './admin-return-detail.component.css',
})
export class AdminReturnDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly returnsApi = inject(ReturnService);
  readonly locale = inject(LocaleService);

  readonly item = signal<ReturnRequest | null>(null);
  readonly loading = signal(true);
  readonly busy = signal(false);
  readonly message = signal<string | null>(null);
  readonly isError = signal(false);

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

  statusVariant(status: string): UiBadgeVariant {
    const s = (status || '').toLowerCase();
    if (s.includes('complete') || s.includes('approve')) return 'success';
    if (s.includes('reject') || s.includes('cancel')) return 'danger';
    if (s.includes('process')) return 'brand';
    if (s.includes('pending')) return 'warning';
    return 'neutral';
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
      error: () => this.loading.set(false),
    });
  }

  approve(): void {
    this.run(() => this.returnsApi.approve(this.item()!.id, { adminNotes: this.adminNotes || undefined }));
  }

  reject(): void {
    if (!this.rejectionReason.trim()) {
      this.message.set(this.t('rejectionReasonRequired'));
      this.isError.set(true);
      return;
    }
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

  cancelReturn(): void {
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
    this.message.set(null);
    action().subscribe({
      next: () => {
        this.returnsApi.getAdmin(current.id).subscribe({
          next: (item) => {
            this.item.set(item);
            this.busy.set(false);
            this.message.set(this.t('updated'));
            this.isError.set(false);
          },
        });
      },
      error: (err) => {
        this.message.set(err.error?.errors?.[0] ?? this.t('actionFailed'));
        this.isError.set(true);
        this.busy.set(false);
      },
    });
  }
}
