import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { isPlatformBrowser } from '@angular/common';
import { PLATFORM_ID } from '@angular/core';
import { UI, UiKey } from '../../core/i18n/ui-text';
import { LocaleService } from '../../core/services/locale.service';
import { NewsletterService } from '../../core/services/newsletter.service';
import {
  clearGuestNewsletterEmail,
  readGuestNewsletterEmail,
} from '../../core/services/newsletter-storage';
import { apiErrorMessage } from '../../core/utils/api-error';
import { UiFormFieldComponent, UiPageHeaderComponent } from '../../shared/ui';

@Component({
  standalone: true,
  imports: [FormsModule, RouterLink, UiPageHeaderComponent, UiFormFieldComponent],
  templateUrl: './newsletter-unsubscribe.component.html',
  styleUrl: './newsletter-unsubscribe.component.css',
})
export class NewsletterUnsubscribeComponent implements OnInit {
  private readonly newsletterApi = inject(NewsletterService);
  private readonly route = inject(ActivatedRoute);
  private readonly platformId = inject(PLATFORM_ID);
  readonly locale = inject(LocaleService);

  readonly email = signal('');
  readonly busy = signal(false);
  readonly message = signal<string | null>(null);
  readonly error = signal(false);
  readonly done = signal(false);

  ngOnInit(): void {
    const fromQuery = this.route.snapshot.queryParamMap.get('email')?.trim();
    if (fromQuery) {
      this.email.set(fromQuery);
      return;
    }

    if (isPlatformBrowser(this.platformId)) {
      const stored = readGuestNewsletterEmail();
      if (stored) {
        this.email.set(stored);
      }
    }
  }

  t(key: UiKey): string {
    return this.locale.pick(UI[key].en, UI[key].ar);
  }

  unsubscribe(): void {
    if (!confirm(this.t('newsletterConfirmUnsubscribe'))) return;

    const value = this.email().trim();
    if (!value || !/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(value)) {
      this.message.set(this.t('newsletterInvalidEmail'));
      this.error.set(true);
      return;
    }

    this.busy.set(true);
    this.message.set(null);
    this.error.set(false);
    this.newsletterApi.unsubscribe(value).subscribe({
      next: (res) => {
        clearGuestNewsletterEmail();
        this.done.set(true);
        this.message.set(res.message || this.t('newsletterUnsubscribed'));
        this.busy.set(false);
      },
      error: (err) => {
        this.message.set(apiErrorMessage(err, this.t('newsletterUnsubscribeFailed')));
        this.error.set(true);
        this.busy.set(false);
      },
    });
  }
}
