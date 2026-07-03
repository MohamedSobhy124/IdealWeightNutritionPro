declare global {
  interface Window {
    GeideaCheckout?: new (
      onSuccess: (data: unknown) => void,
      onError: (error: unknown) => void,
      onCancel?: () => void
    ) => { startPayment: (sessionId: string) => void };
  }
}

const GEIDEA_SCRIPT = 'https://payments.geidea.ae/hpp/geideaCheckout.min.js';

export function loadGeideaCheckout(): Promise<void> {
  if (typeof window.GeideaCheckout !== 'undefined') {
    return Promise.resolve();
  }

  return new Promise((resolve, reject) => {
    const existing = document.querySelector(`script[src="${GEIDEA_SCRIPT}"]`);
    if (existing) {
      existing.addEventListener('load', () => resolve());
      existing.addEventListener('error', () => reject(new Error('Failed to load Geidea.')));
      return;
    }

    const script = document.createElement('script');
    script.src = GEIDEA_SCRIPT;
    script.onload = () => resolve();
    script.onerror = () => reject(new Error('Failed to load Geidea payment library.'));
    document.head.appendChild(script);
  });
}

export function startGeideaPayment(sessionId: string): Promise<void> {
  return loadGeideaCheckout().then(
    () =>
      new Promise((resolve, reject) => {
        const Checkout = window.GeideaCheckout;
        if (!Checkout) {
          reject(new Error('Geidea checkout is unavailable.'));
          return;
        }

        const payment = new Checkout(
          () => resolve(),
          (error) => {
            const message =
              typeof error === 'string'
                ? error
                : (error as { message?: string })?.message ?? 'Geidea payment failed.';
            reject(new Error(message));
          }
        );
        payment.startPayment(sessionId);
      })
  );
}
