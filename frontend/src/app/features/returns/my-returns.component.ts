import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { IwnCurrencyPipe } from '../../core/pipes/iwn-currency.pipe';
import { UI, UiKey } from '../../core/i18n/ui-text';
import { ReturnListItem } from '../../core/models/return.models';
import { LocaleService } from '../../core/services/locale.service';
import { ReturnService } from '../../core/services/return.service';
import {
  UiBadgeComponent,
  UiEmptyStateComponent,
  UiPageHeaderComponent,
  UiSkeletonComponent,
  statusVariant,
} from '../../shared/ui';

@Component({
  standalone: true,
  imports: [
    RouterLink,
    DatePipe,
    IwnCurrencyPipe,
    UiPageHeaderComponent,
    UiSkeletonComponent,
    UiEmptyStateComponent,
    UiBadgeComponent,
  ],
  templateUrl: './my-returns.component.html',
  styleUrl: './my-returns.component.css',
})
export class MyReturnsComponent implements OnInit {
  private readonly returns = inject(ReturnService);
  readonly locale = inject(LocaleService);

  readonly loading = signal(true);
  readonly items = signal<ReturnListItem[]>([]);
  readonly statusVariant = statusVariant;

  t(key: UiKey): string {
    return this.locale.pick(UI[key].en, UI[key].ar);
  }

  ngOnInit(): void {
    this.returns.listMine().subscribe({
      next: (list) => {
        this.items.set(list);
        this.loading.set(false);
      },
      error: () => {
        this.items.set([]);
        this.loading.set(false);
      },
    });
  }
}
