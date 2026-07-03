import { Component, inject, OnInit, signal } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs/operators';
import { CallService } from '../../core/services/call.service';
import { LocaleService } from '../../core/services/locale.service';
import { WhatsAppService } from '../../core/services/whatsapp.service';
import { UI, UiKey } from '../../core/i18n/ui-text';

@Component({
  selector: 'app-contact-floating',
  standalone: true,
  templateUrl: './contact-floating.component.html',
  styleUrl: './contact-floating.component.css',
})
export class ContactFloatingComponent implements OnInit {
  private readonly whatsapp = inject(WhatsAppService);
  private readonly call = inject(CallService);
  private readonly router = inject(Router);
  readonly locale = inject(LocaleService);

  readonly whatsappEnabled = signal(false);
  readonly whatsappUrl = signal<string | null>(null);
  readonly callEnabled = signal(false);
  readonly callUrl = signal<string | null>(null);
  readonly isShopRoute = signal(false);

  ngOnInit(): void {
    this.isShopRoute.set(this.router.url.startsWith('/shop'));
    this.router.events.pipe(filter((event) => event instanceof NavigationEnd)).subscribe(() => {
      this.isShopRoute.set(this.router.url.startsWith('/shop'));
    });

    this.whatsapp.getSettings().subscribe((settings) => {
      if (!settings?.enabled || !settings.phoneNumber) return;
      const message = this.locale.pick(settings.defaultMessage, settings.defaultMessageAr);
      this.whatsappUrl.set(this.whatsapp.buildChatUrl(settings.phoneNumber, message));
      this.whatsappEnabled.set(true);
    });

    this.call.getSettings().subscribe((settings) => {
      if (!settings?.enabled || !settings.phoneNumber) return;
      this.callUrl.set(this.call.buildTelUrl(settings.phoneNumber));
      this.callEnabled.set(true);
    });
  }

  t(key: UiKey): string {
    return this.locale.pick(UI[key].en, UI[key].ar);
  }
}
