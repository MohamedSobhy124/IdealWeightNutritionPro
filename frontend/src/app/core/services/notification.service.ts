import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import {
  NotificationActionResponse,
  NotificationCountDto,
  NotificationDto,
} from '../models/notification.models';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  getUnread() {
    return this.http.get<NotificationDto[]>(`${this.baseUrl}/notifications/unread`, {
      withCredentials: true,
    });
  }

  getCount() {
    return this.http.get<NotificationCountDto>(`${this.baseUrl}/notifications/count`, {
      withCredentials: true,
    });
  }

  markRead(id: number) {
    return this.http.post<NotificationActionResponse>(
      `${this.baseUrl}/notifications/${id}/mark-read`,
      {},
      { withCredentials: true }
    );
  }

  markAllRead() {
    return this.http.post<NotificationActionResponse>(
      `${this.baseUrl}/notifications/mark-all-read`,
      {},
      { withCredentials: true }
    );
  }
}
