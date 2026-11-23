import { UiKey } from '../i18n/ui-text';

/** Known API status codes. Anything else is shown as the server sent it. */
const STATUS_KEYS: Record<string, UiKey> = {
  Pending: 'statusPending',
  Paid: 'statusPaid',
  Approved: 'statusApproved',
  Processing: 'statusProcessing',
  Shipped: 'statusShipped',
  Delivered: 'statusDelivered',
  Cancelled: 'statusCancelled',
  Refunded: 'statusRefunded',
  PartiallyRefunded: 'statusPartiallyRefunded',
  Returned: 'statusReturned',
  ReturnApproved: 'statusReturnApproved',
  ReturnRequested: 'statusReturnRequested',
  Rejected: 'statusRejected',
  Completed: 'statusCompleted',
  Processed: 'statusProcessed',
  Failed: 'statusFailed',
  ApprovedForDelayedPayment: 'statusDelayedPayment',
  Active: 'statusActive',
};

export function serviceTypeLabel(type: string | null | undefined, t: (key: UiKey) => string): string {
  if (type === 'Online') return t('serviceTypeOnline');
  if (type === 'Offline') return t('serviceTypeOffline');
  return type ?? '';
}

export function statusLabel(status: string | null | undefined, t: (key: UiKey) => string): string {
  if (!status) return '';
  const key = STATUS_KEYS[status];
  return key ? t(key) : status;
}
