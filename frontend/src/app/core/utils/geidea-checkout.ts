declare global {
  interface Window {
    GeideaCheckout?: new (
      onSuccess: (data: unknown) => void,
      onError: (error: unknown) => void,
      onCancel?: (data?: unknown) => void
    ) => { startPayment: (sessionId: string) => void };
  }
}

const GEIDEA_SCRIPT = 'https://payments.geidea.ae/hpp/geideaCheckout.min.js';
/** Give up on the popup library after this long and fall back to the hosted page. */
const SCRIPT_TIMEOUT_MS = 15000;

/** Thrown when the shopper closes the Geidea popup without paying. */
export class GeideaCancelledError extends Error {
  constructor() {
    super('Payment was cancelled.');
    this.name = 'GeideaCancelledError';
  }
}

/** Thrown when the popup cannot be shown at all (script blocked, slow, or broken). */
export class GeideaUnavailableError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'GeideaUnavailableError';
  }
}

export function loadGeideaCheckout(): Promise<void> {
  if (typeof window.GeideaCheckout !== 'undefined') {
    return Promise.resolve();
  }

  return new Promise((resolve, reject) => {
    let settled = false;
    const done = (fn: () => void) => {
      if (settled) return;
      settled = true;
      window.clearTimeout(timer);
      fn();
    };
    const fail = (message: string) => done(() => reject(new GeideaUnavailableError(message)));
    const ok = () =>
      done(() =>
        typeof window.GeideaCheckout !== 'undefined'
          ? resolve()
          : reject(new GeideaUnavailableError('Geidea checkout is unavailable.')),
      );

    const timer = window.setTimeout(() => fail('Geidea payment library took too long to load.'), SCRIPT_TIMEOUT_MS);

    const existing = document.querySelector<HTMLScriptElement>(`script[src="${GEIDEA_SCRIPT}"]`);
    if (existing) {
      // A previous attempt may have failed or already finished loading; start fresh.
      existing.remove();
    }

    const script = document.createElement('script');
    script.src = GEIDEA_SCRIPT;
    script.async = true;
    script.onload = ok;
    script.onerror = () => fail('Failed to load Geidea payment library.');
    document.head.appendChild(script);
  });
}

/**
 * Opens the Geidea payment popup. Always settles: resolves on success, rejects
 * with GeideaCancelledError when closed, GeideaUnavailableError when the popup
 * cannot be shown, or a plain Error when Geidea reports a failed payment.
 */
export function startGeideaPayment(sessionId: string): Promise<void> {
  return loadGeideaCheckout().then(
    () =>
      new Promise<void>((resolve, reject) => {
        const Checkout = window.GeideaCheckout;
        if (!Checkout) {
          reject(new GeideaUnavailableError('Geidea checkout is unavailable.'));
          return;
        }

        try {
          const payment = new Checkout(
            () => resolve(),
            (error) => {
              const message =
                typeof error === 'string'
                  ? error
                  : (error as { message?: string; detailedResponseMessage?: string })?.detailedResponseMessage ??
                    (error as { message?: string })?.message ??
                    'Geidea payment failed.';
              reject(new Error(message));
            },
            () => reject(new GeideaCancelledError()),
          );
          payment.startPayment(sessionId);
        } catch (err) {
          reject(new GeideaUnavailableError((err as Error)?.message ?? 'Could not open Geidea payment.'));
        }
      }),
  );
}
