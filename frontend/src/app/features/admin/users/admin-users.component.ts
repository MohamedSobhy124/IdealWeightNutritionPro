import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ADMIN_UI, AdminUiKey } from '../../../core/i18n/admin-ui-text';
import { AdminCompany } from '../../../core/models/admin-company.models';
import { AdminUserListItem } from '../../../core/models/admin-user.models';
import { AdminCompanyService } from '../../../core/services/admin-company.service';
import { AdminUserService } from '../../../core/services/admin-user.service';
import { LocaleService } from '../../../core/services/locale.service';
import {
  UiCellDirective,
  UiDataTableComponent,
  UiFormFieldComponent,
  UiModalComponent,
  UiPageHeaderComponent,
  type UiTableColumn,
} from '../../../shared/ui';

@Component({
  standalone: true,
  imports: [
    FormsModule,
    UiPageHeaderComponent,
    UiDataTableComponent,
    UiCellDirective,
    UiModalComponent,
    UiFormFieldComponent,
  ],
  templateUrl: './admin-users.component.html',
  styleUrl: './admin-users.component.css',
})
export class AdminUsersComponent implements OnInit {
  private readonly usersApi = inject(AdminUserService);
  private readonly companiesApi = inject(AdminCompanyService);
  readonly locale = inject(LocaleService);

  readonly items = signal<AdminUserListItem[]>([]);
  readonly companies = signal<AdminCompany[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly message = signal<string | null>(null);
  readonly showForm = signal(false);
  readonly creating = signal(false);

  email = '';
  name = '';
  password = '';
  phoneNumber = '';
  role = 'Customer';
  companyId: number | null = null;

  readonly roleOptions = ['Admin', 'Employee', 'Company', 'Customer'];

  t(key: AdminUiKey): string {
    return this.locale.pick(ADMIN_UI[key].en, ADMIN_UI[key].ar);
  }

  get cols(): UiTableColumn[] {
    return [
      { key: 'name', header: this.t('customer'), sortable: true },
      { key: 'email', header: this.t('email'), hideOnMobile: true },
      { key: 'phoneNumber', header: this.t('phone'), hideOnMobile: true },
      { key: 'role', header: this.t('userRole') },
      { key: 'companyName', header: this.t('companyName'), hideOnMobile: true },
    ];
  }

  ngOnInit(): void {
    this.load();
    this.companiesApi.list().subscribe({
      next: (list) => this.companies.set(list),
    });
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.usersApi.list().subscribe({
      next: (items) => {
        this.items.set(items);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(this.t('loadUsersError'));
        this.loading.set(false);
      },
    });
  }

  openCreate(): void {
    this.email = '';
    this.name = '';
    this.password = '';
    this.phoneNumber = '';
    this.role = 'Customer';
    this.companyId = null;
    this.showForm.set(true);
    this.message.set(null);
  }

  cancelForm(): void {
    this.showForm.set(false);
  }

  isCompanyRole(): boolean {
    return this.role === 'Company';
  }

  primaryRole(user: AdminUserListItem): string {
    return user.roles[0] ?? '—';
  }

  submit(): void {
    if (!this.email.trim() || !this.name.trim() || !this.password.trim()) {
      this.message.set(this.t('userRequiredFields'));
      return;
    }
    if (this.isCompanyRole() && !this.companyId) {
      this.message.set(this.t('companyRequiredForRole'));
      return;
    }

    this.creating.set(true);
    this.message.set(null);

    this.usersApi
      .create({
        email: this.email.trim(),
        name: this.name.trim(),
        password: this.password,
        phoneNumber: this.phoneNumber.trim() || undefined,
        role: this.role,
        companyId: this.isCompanyRole() ? (this.companyId ?? undefined) : undefined,
      })
      .subscribe({
        next: (res) => {
          this.showForm.set(false);
          this.creating.set(false);
          this.message.set(res.message || this.t('userCreated'));
          this.load();
        },
        error: (err) => {
          this.message.set(err.error?.errors?.[0] ?? this.t('saveFailed'));
          this.creating.set(false);
        },
      });
  }
}
