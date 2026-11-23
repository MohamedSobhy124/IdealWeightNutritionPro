import { isPlatformBrowser } from '@angular/common';
import { Injectable, PLATFORM_ID, inject, signal } from '@angular/core';

export type UiToastVariant = 'success' | 'error' | 'warning' | 'info';

export interface UiToastOptions {
  /** Milliseconds before auto-dismiss; `0` keeps the toast until dismissed. */
  duration?: number;
  actionLabel?: string;
  action?: () => void;
}

export interface UiToast {
  id: number;
  message: string;
  variant: UiToastVariant;
  actionLabel?: string;
  action?: () => void;
}

const MAX_VISIBLE = 4;

/** App-wide non-blocking feedback. Rendered by `<ui-feedback-host />`. */
@Injectable({ providedIn: 'root' })
export class UiToastService {
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
  private seq = 0;
  private readonly timers = new Map<number, ReturnType<typeof setTimeout>>();

  readonly toasts = signal<readonly UiToast[]>([]);

  success(message: string, options?: UiToastOptions): number {
    return this.show(message, 'success', options);
  }

  error(message: string, options?: UiToastOptions): number {
    return this.show(message, 'error', { duration: 6000, ...options });
  }

  warning(message: string, options?: UiToastOptions): number {
    return this.show(message, 'warning', options);
  }

  info(message: string, options?: UiToastOptions): number {
    return this.show(message, 'info', options);
  }

  show(message: string, variant: UiToastVariant = 'info', options: UiToastOptions = {}): number {
    const id = ++this.seq;
    const toast: UiToast = { id, message, variant, actionLabel: options.actionLabel, action: options.action };
    const next = [...this.toasts(), toast];
    for (const dropped of next.slice(0, Math.max(0, next.length - MAX_VISIBLE))) {
      this.clearTimer(dropped.id);
    }
    this.toasts.set(next.slice(-MAX_VISIBLE));

    const duration = options.duration ?? 4000;
    if (this.isBrowser && duration > 0) {
      this.timers.set(id, setTimeout(() => this.dismiss(id), duration));
    }
    return id;
  }

  dismiss(id: number): void {
    this.clearTimer(id);
    this.toasts.update((list) => list.filter((t) => t.id !== id));
  }

  runAction(toast: UiToast): void {
    toast.action?.();
    this.dismiss(toast.id);
  }

  private clearTimer(id: number): void {
    const timer = this.timers.get(id);
    if (timer) clearTimeout(timer);
    this.timers.delete(id);
  }
}
