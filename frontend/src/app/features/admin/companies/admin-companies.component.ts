import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ADMIN_UI, AdminUiKey } from '../../../core/i18n/admin-ui-text';
import { AdminCompany, UpsertAdminCompanyRequest } from '../../../core/models/admin-company.models';
import { AdminCompanyService } from '../../../core/services/admin-company.service';
import { LocaleService } from '../../../core/services/locale.service';
import {
  UiCardComponent,
  UiCellDirective,
  UiDataTableComponent,
  UiFormFieldComponent,
  UiPageHeaderComponent,
  type UiTableColumn,
} from '../../../shared/ui';

@Component({
  standalone: true,
  imports: [
    FormsModule,
    UiPageHeaderComponent,
    UiCardComponent,
    UiFormFieldComponent,
    UiDataTableComponent,
    UiCellDirective,
  ],
  templateUrl: './admin-companies.component.html',
  styleUrl: './admin-companies.component.css',
})
export class AdminCompaniesComponent implements OnInit {
  private readonly companiesApi = inject(AdminCompanyService);
  readonly locale = inject(LocaleService);

  readonly items = signal<AdminCompany[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly message = signal<string | null>(null);
  readonly busyId = signal<number | null>(null);
  readonly showForm = signal(false);
  readonly editingId = signal<number | null>(null);

  name = '';
  streetAddress = '';
  city = '';
  state = '';
  postalCode = '';
  phoneNumber = '';

  t(key: AdminUiKey): string {
    return this.locale.pick(ADMIN_UI[key].en, ADMIN_UI[key].ar);
  }

  get cols(): UiTableColumn[] {
    return [
      { key: 'name', header: this.t('companyName'), sortable: true },
      { key: 'city', header: this.t('city'), hideOnMobile: true },
      { key: 'phoneNumber', header: this.t('phone') },
      { key: 'actions', header: this.t('actions'), noExport: true, hideOnMobile: true },
    ];
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.companiesApi.list().subscribe({
      next: (items) => {
        this.items.set(items);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(this.t('loadCompaniesError'));
        this.loading.set(false);
      },
    });
  }

  openCreate(): void {
    this.editingId.set(null);
    this.name = '';
    this.streetAddress = '';
    this.city = '';
    this.state = '';
    this.postalCode = '';
    this.phoneNumber = '';
    this.showForm.set(true);
    this.message.set(null);
  }

  openEdit(item: AdminCompany): void {
    this.editingId.set(item.id);
    this.name = item.name;
    this.streetAddress = item.streetAddress;
    this.city = item.city;
    this.state = item.state;
    this.postalCode = item.postalCode;
    this.phoneNumber = item.phoneNumber;
    this.showForm.set(true);
    this.message.set(null);
  }

  cancelForm(): void {
    this.showForm.set(false);
    this.editingId.set(null);
  }

  buildRequest(): UpsertAdminCompanyRequest {
    return {
      name: this.name.trim(),
      streetAddress: this.streetAddress.trim(),
      city: this.city.trim(),
      state: this.state.trim(),
      postalCode: this.postalCode.trim(),
      phoneNumber: this.phoneNumber.trim(),
    };
  }

  submit(): void {
    if (!this.name.trim()) {
      this.message.set(this.t('companyRequiredFields'));
      return;
    }

    const editId = this.editingId();
    this.busyId.set(editId ?? -1);
    this.message.set(null);

    const op =
      editId != null
        ? this.companiesApi.update(editId, this.buildRequest())
        : this.companiesApi.create(this.buildRequest());

    op.subscribe({
      next: () => {
        this.showForm.set(false);
        this.editingId.set(null);
        this.busyId.set(null);
        this.message.set(this.t('saved'));
        this.load();
      },
      error: (err) => {
        this.message.set(err.error?.errors?.[0] ?? this.t('saveFailed'));
        this.busyId.set(null);
      },
    });
  }

  remove(item: AdminCompany): void {
    if (!confirm(this.t('confirmDeleteCompany'))) return;
    this.busyId.set(item.id);
    this.message.set(null);
    this.companiesApi.delete(item.id).subscribe({
      next: () => {
        this.message.set(this.t('updated'));
        this.busyId.set(null);
        this.load();
      },
      error: (err) => {
        this.message.set(err.error?.errors?.[0] ?? this.t('actionFailed'));
        this.busyId.set(null);
      },
    });
  }
}
