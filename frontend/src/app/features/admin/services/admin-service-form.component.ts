import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import {
  UiCardComponent,
  UiEmptyStateComponent,
  UiFormFieldComponent,
  UiPageHeaderComponent,
  UiSkeletonComponent,
} from '../../../shared/ui';
import { ADMIN_UI, AdminUiKey } from '../../../core/i18n/admin-ui-text';
import {
  AdminServiceImage,
  UpsertAdminServiceRequest,
} from '../../../core/models/admin-service.models';
import { AdminServiceService } from '../../../core/services/admin-service.service';
import { CatalogueService } from '../../../core/services/catalogue.service';
import { LocaleService } from '../../../core/services/locale.service';

@Component({
  standalone: true,
  imports: [
    RouterLink,
    FormsModule,
    UiPageHeaderComponent,
    UiCardComponent,
    UiFormFieldComponent,
    UiEmptyStateComponent,
    UiSkeletonComponent,
  ],
  templateUrl: './admin-service-form.component.html',
  styleUrl: './admin-service-form.component.css',
})
export class AdminServiceFormComponent implements OnInit {
  private readonly servicesApi = inject(AdminServiceService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  readonly catalogue = inject(CatalogueService);
  readonly locale = inject(LocaleService);

  readonly loading = signal(true);
  readonly submitting = signal(false);
  readonly uploading = signal(false);
  readonly error = signal<string | null>(null);
  readonly saveError = signal<string | null>(null);
  readonly isEdit = signal(false);
  readonly images = signal<AdminServiceImage[]>([]);

  serviceId: number | null = null;

  title = '';
  titleAr = '';
  description = '';
  descriptionAr = '';
  price = 0;
  serviceType = 'Online';
  offlinePaymentPercent: number | null = null;
  isActive = true;
  displayOrder = 0;
  imageUrl = '';

  t(key: AdminUiKey): string {
    return this.locale.pick(ADMIN_UI[key].en, ADMIN_UI[key].ar);
  }

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam && idParam !== 'new') {
      const id = Number(idParam);
      if (!id) {
        this.error.set(this.t('invalidService'));
        this.loading.set(false);
        return;
      }
      this.isEdit.set(true);
      this.serviceId = id;
      this.servicesApi.get(id).subscribe({
        next: (service) => {
          this.applyService(service);
          this.loading.set(false);
        },
        error: () => {
          this.error.set(this.t('serviceNotFound'));
          this.loading.set(false);
        },
      });
    } else {
      this.loading.set(false);
    }
  }

  private applyService(service: {
    title: string;
    titleAr?: string | null;
    description?: string | null;
    descriptionAr?: string | null;
    price: number;
    serviceType: string;
    offlinePaymentPercent?: number | null;
    isActive: boolean;
    displayOrder: number;
    imageUrl?: string | null;
    images: AdminServiceImage[];
  }): void {
    this.title = service.title;
    this.titleAr = service.titleAr ?? '';
    this.description = service.description ?? '';
    this.descriptionAr = service.descriptionAr ?? '';
    this.price = service.price;
    this.serviceType = service.serviceType;
    this.offlinePaymentPercent = service.offlinePaymentPercent ?? null;
    this.isActive = service.isActive;
    this.displayOrder = service.displayOrder;
    this.imageUrl = service.imageUrl ?? '';
    this.images.set(service.images ?? []);
  }

  submit(): void {
    if (!this.title.trim() || this.price <= 0) {
      this.saveError.set(this.t('serviceRequiredFields'));
      return;
    }

    const request: UpsertAdminServiceRequest = {
      title: this.title.trim(),
      titleAr: this.titleAr.trim() || null,
      description: this.description.trim() || null,
      descriptionAr: this.descriptionAr.trim() || null,
      price: this.price,
      serviceType: this.serviceType,
      offlinePaymentPercent:
        this.serviceType === 'Offline' ? this.offlinePaymentPercent : null,
      isActive: this.isActive,
      displayOrder: this.displayOrder,
    };

    this.submitting.set(true);
    this.saveError.set(null);

    const op =
      this.isEdit() && this.serviceId != null
        ? this.servicesApi.update(this.serviceId, request)
        : this.servicesApi.create(request);

    op.subscribe({
      next: (service) => {
        this.submitting.set(false);
        if (!this.isEdit()) {
          void this.router.navigate(['/admin/services', service.id]);
        }
      },
      error: (err) => {
        this.saveError.set(err?.error?.errors?.[0] ?? this.t('saveFailed'));
        this.submitting.set(false);
      },
    });
  }

  onImageSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file || !this.serviceId) return;

    this.uploading.set(true);
    this.servicesApi.uploadImage(this.serviceId, file).subscribe({
      next: (image) => {
        this.images.update((list) => [...list, image]);
        if (!this.imageUrl) this.imageUrl = image.imageUrl;
        this.uploading.set(false);
      },
      error: (err) => {
        this.saveError.set(err?.error?.errors?.[0] ?? this.t('uploadFailed'));
        this.uploading.set(false);
      },
    });
    (event.target as HTMLInputElement).value = '';
  }

  removeImage(imageId: number): void {
    if (!this.serviceId || !confirm(this.t('confirmDeleteImage'))) return;
    this.servicesApi.deleteImage(this.serviceId, imageId).subscribe({
      next: () => {
        this.images.update((list) => list.filter((i) => i.id !== imageId));
      },
      error: (err) => {
        this.saveError.set(err?.error?.errors?.[0] ?? this.t('deleteFailed'));
      },
    });
  }
}
