import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError, shareReplay } from 'rxjs/operators';
import { environment } from '../../../environments/environment';

export interface CallSettings {
  enabled: boolean;
  phoneNumber: string;
}

@Injectable({ providedIn: 'root' })
export class CallService {
  private readonly http = inject(HttpClient);
  private settings$?: Observable<CallSettings | null>;

  getSettings(): Observable<CallSettings | null> {
    if (!this.settings$) {
      this.settings$ = this.http
        .get<CallSettings>(`${environment.apiBaseUrl}/content/call`)
        .pipe(
          catchError(() => of(null)),
          shareReplay(1)
        );
    }
    return this.settings$;
  }

  buildTelUrl(phoneNumber: string): string {
    const digits = phoneNumber.replace(/\D/g, '');
    return `tel:+${digits}`;
  }
}
