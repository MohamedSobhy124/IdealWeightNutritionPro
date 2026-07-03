import { Injectable, inject } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { environment } from '../../../environments/environment';
import { NotificationDto } from '../models/notification.models';
import { AuthService } from './auth.service';

@Injectable({ providedIn: 'root' })
export class NotificationHubService {
  private readonly auth = inject(AuthService);
  private connection: signalR.HubConnection | null = null;
  private started = false;

  private readonly receivedSubject = new Subject<NotificationDto>();
  readonly received$ = this.receivedSubject.asObservable();

  connect(): void {
    const token = this.auth.accessToken();
    if (!token || this.started) return;

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(environment.signalRHubUrl, {
        accessTokenFactory: () => this.auth.accessToken() ?? '',
        withCredentials: true,
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    this.connection.on('ReceiveNotification', (notification: NotificationDto) => {
      this.receivedSubject.next(notification);
    });

    this.connection.onreconnected(() => {
      void this.connection?.invoke('JoinAdminGroup');
    });

    this.started = true;
    void this.startConnection();
  }

  disconnect(): void {
    this.started = false;
    if (!this.connection) return;

    void this.connection.stop().finally(() => {
      this.connection = null;
    });
  }

  private async startConnection(): Promise<void> {
    if (!this.connection) return;

    try {
      await this.connection.start();
      await this.connection.invoke('JoinAdminGroup');
    } catch {
      this.started = false;
      this.connection = null;
    }
  }
}
