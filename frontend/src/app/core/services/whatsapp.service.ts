import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError, shareReplay } from 'rxjs/operators';
import { environment } from '../../../environments/environment';

export interface WhatsAppSettings {
  enabled: boolean;
  phoneNumber: string;
  defaultMessage: string;
  defaultMessageAr: string;
}

@Injectable({ providedIn: 'root' })
export class WhatsAppService {
  private readonly http = inject(HttpClient);
  private settings$?: Observable<WhatsAppSettings | null>;

  getSettings(): Observable<WhatsAppSettings | null> {
    if (!this.settings$) {
      this.settings$ = this.http
        .get<WhatsAppSettings>(`${environment.apiBaseUrl}/content/whatsapp`)
        .pipe(
          catchError(() => of(null)),
          shareReplay(1)
        );
    }
    return this.settings$;
  }

  buildChatUrl(phoneNumber: string, message: string): string {
    const digits = phoneNumber.replace(/\D/g, '');
    return `https://wa.me/${digits}?text=${encodeURIComponent(message)}`;
  }
}
