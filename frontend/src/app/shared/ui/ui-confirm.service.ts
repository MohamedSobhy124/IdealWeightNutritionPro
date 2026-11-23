import { Injectable, signal } from '@angular/core';

export interface UiConfirmOptions {
  title: string;
  message?: string;
  confirmLabel?: string;
  cancelLabel?: string;
  /** Styles the confirm button as destructive. */
  danger?: boolean;
  /** When set, the user must type this exact text before confirming. */
  requireText?: string;
  icon?: string;
}

export interface UiConfirmRequest extends UiConfirmOptions {
  resolve: (confirmed: boolean) => void;
}

/**
 * Promise-based replacement for `window.confirm`. Rendered by `<ui-feedback-host />`.
 *
 * ```ts
 * if (!(await this.confirm.ask({ title: 'Delete brand?', danger: true }))) return;
 * ```
 */
@Injectable({ providedIn: 'root' })
export class UiConfirmService {
  readonly current = signal<UiConfirmRequest | null>(null);

  ask(options: UiConfirmOptions): Promise<boolean> {
    this.current()?.resolve(false);
    return new Promise<boolean>((resolve) => {
      this.current.set({ ...options, resolve });
    });
  }

  /** Shorthand for a destructive confirmation. */
  danger(title: string, message?: string, confirmLabel?: string): Promise<boolean> {
    return this.ask({ title, message, confirmLabel, danger: true, icon: 'delete_outline' });
  }

  settle(confirmed: boolean): void {
    const request = this.current();
    if (!request) return;
    this.current.set(null);
    request.resolve(confirmed);
  }
}
