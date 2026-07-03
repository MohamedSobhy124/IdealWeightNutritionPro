import { DecimalPipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { ADMIN_UI, AdminUiKey } from '../../../core/i18n/admin-ui-text';
import { VideoBanner, VideoBannerService } from '../../../core/services/video-banner.service';
import { LocaleService } from '../../../core/services/locale.service';
import {
  UiCardComponent,
  UiPageHeaderComponent,
  UiSkeletonComponent,
} from '../../../shared/ui';

@Component({
  standalone: true,
  imports: [DecimalPipe, UiPageHeaderComponent, UiCardComponent, UiSkeletonComponent],
  templateUrl: './admin-video-banner.component.html',
  styleUrl: './admin-video-banner.component.css',
})
export class AdminVideoBannerComponent implements OnInit {
  private readonly api = inject(VideoBannerService);
  readonly locale = inject(LocaleService);

  readonly banner = signal<VideoBanner | null>(null);
  readonly loading = signal(true);
  readonly message = signal<string | null>(null);
  readonly error = signal(false);
  readonly busy = signal(false);

  t(key: AdminUiKey): string {
    return this.locale.pick(ADMIN_UI[key].en, ADMIN_UI[key].ar);
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.api.getAdminBanner().subscribe({
      next: (banner) => {
        this.banner.set(banner);
        this.loading.set(false);
      },
      error: () => {
        this.message.set(this.t('loadOrdersError'));
        this.error.set(true);
        this.loading.set(false);
      },
    });
  }

  onVideoSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    this.busy.set(true);
    this.api.uploadVideo(file).subscribe({
      next: (banner) => {
        this.banner.set(banner);
        this.message.set(this.t('videoUploaded'));
        this.error.set(false);
        this.busy.set(false);
        input.value = '';
      },
      error: () => {
        this.message.set(this.t('bannerUploadFailed'));
        this.error.set(true);
        this.busy.set(false);
      },
    });
  }

  onPosterSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    this.busy.set(true);
    this.api.uploadPoster(file).subscribe({
      next: (banner) => {
        this.banner.set(banner);
        this.message.set(this.t('posterUploaded'));
        this.error.set(false);
        this.busy.set(false);
        input.value = '';
      },
      error: () => {
        this.message.set(this.t('bannerUploadFailed'));
        this.error.set(true);
        this.busy.set(false);
      },
    });
  }

  removeVideo(): void {
    this.busy.set(true);
    this.api.deleteVideo().subscribe({
      next: (banner) => {
        this.banner.set(banner);
        this.message.set(this.t('videoDeleted'));
        this.error.set(false);
        this.busy.set(false);
      },
      error: () => {
        this.message.set(this.t('bannerUploadFailed'));
        this.error.set(true);
        this.busy.set(false);
      },
    });
  }

  removePoster(): void {
    this.busy.set(true);
    this.api.deletePoster().subscribe({
      next: (banner) => {
        this.banner.set(banner);
        this.message.set(this.t('posterDeleted'));
        this.error.set(false);
        this.busy.set(false);
      },
      error: () => {
        this.message.set(this.t('bannerUploadFailed'));
        this.error.set(true);
        this.busy.set(false);
      },
    });
  }
}
