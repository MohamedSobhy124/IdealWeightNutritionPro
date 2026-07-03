import { DatePipe } from '@angular/common';
import { Component, HostListener, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { UI, UiKey } from '../../core/i18n/ui-text';
import { NotificationDto } from '../../core/models/notification.models';
import { AuthService } from '../../core/services/auth.service';
import { LocaleService } from '../../core/services/locale.service';
import { NotificationHubService } from '../../core/services/notification-hub.service';
import { NotificationService } from '../../core/services/notification.service';

@Component({
  selector: 'app-notification-bell',
  standalone: true,
  imports: [DatePipe],
  templateUrl: './notification-bell.component.html',
  styleUrl: './notification-bell.component.css',
})
export class NotificationBellComponent implements OnInit, OnDestroy {
  private readonly notificationsApi = inject(NotificationService);
  private readonly notificationHub = inject(NotificationHubService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  readonly locale = inject(LocaleService);

  readonly open = signal(false);
  readonly count = signal(0);
  readonly items = signal<NotificationDto[]>([]);
  readonly loading = signal(false);
  readonly busy = signal(false);

  private pollTimer: ReturnType<typeof setInterval> | null = null;
  private hubSub: Subscription | null = null;

  t(key: UiKey): string {
    return this.locale.pick(UI[key].en, UI[key].ar);
  }

  ngOnInit(): void {
    if (!this.auth.isAuthenticated()) return;

    this.refresh();
    this.notificationHub.connect();
    this.hubSub = this.notificationHub.received$.subscribe((notification) => {
      this.count.update((c) => c + 1);
      if (this.open()) {
        this.items.update((list) => [notification, ...list]);
      }
    });
    this.pollTimer = setInterval(() => this.refresh(), 300_000);
  }

  ngOnDestroy(): void {
    this.hubSub?.unsubscribe();
    if (this.pollTimer) clearInterval(this.pollTimer);
    this.notificationHub.disconnect();
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    if (!target.closest('.notification-bell')) {
      this.open.set(false);
    }
  }

  toggle(): void {
    const next = !this.open();
    this.open.set(next);
    if (next) this.refresh();
  }

  refresh(): void {
    if (!this.auth.accessToken()) return;

    this.notificationsApi.getCount().subscribe({
      next: (res) => this.count.set(res.count),
    });

    if (this.open()) {
      this.loading.set(true);
      this.notificationsApi.getUnread().subscribe({
        next: (list) => {
          this.items.set(list);
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
    }
  }

  markAllRead(): void {
    this.busy.set(true);
    this.notificationsApi.markAllRead().subscribe({
      next: () => {
        this.count.set(0);
        this.items.set([]);
        this.busy.set(false);
      },
      error: () => this.busy.set(false),
    });
  }

  openNotification(item: NotificationDto): void {
    this.notificationsApi.markRead(item.id).subscribe({
      next: () => {
        this.items.update((list) => list.filter((n) => n.id !== item.id));
        this.count.update((c) => Math.max(0, c - 1));
      },
    });
    this.navigateLink(item.link);
    this.open.set(false);
  }

  navigateLink(link: string): void {
    if (!link) return;
    try {
      const url = new URL(link, window.location.origin);
      if (url.origin === window.location.origin) {
        const path = url.pathname + url.search + url.hash;
        this.router.navigateByUrl(path);
      } else {
        window.location.href = link;
      }
    } catch {
      if (link.startsWith('/')) {
        this.router.navigateByUrl(link);
      } else {
        window.location.href = link;
      }
    }
  }
}
