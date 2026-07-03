import { UiKey } from './ui-text';

export const PAYMENT_METHOD_LABEL_KEYS: Record<string, UiKey> = {
  COD: 'cashOnDelivery',
  Geidea: 'payCardGeidea',
  Tamara: 'payTamara',
  Tabby: 'payTabby',
};

export const PAYMENT_UNAVAILABLE_REASON_KEYS: Record<string, UiKey> = {
  disabled: 'payReasonDisabled',
  not_configured: 'payReasonNotConfigured',
  min_amount: 'payReasonMinAmount',
  amount_not_eligible: 'payReasonAmountNotEligible',
  temporarily_unavailable: 'payReasonTemporarilyUnavailable',
};

export function paymentMethodLabelKey(methodId: string): UiKey | undefined {
  return PAYMENT_METHOD_LABEL_KEYS[methodId];
}

export function paymentUnavailableReasonKey(reasonCode: string): UiKey | undefined {
  return PAYMENT_UNAVAILABLE_REASON_KEYS[reasonCode];
}
