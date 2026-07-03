import type { UiBadgeVariant } from './ui-badge.component';

/** Maps common order/return/payment status strings to badge variants. */
export function statusVariant(status: string): UiBadgeVariant {
  const x = (status || '').toLowerCase();
  if (
    x.includes('deliver') ||
    x.includes('paid') ||
    x.includes('approve') ||
    x.includes('active') ||
    x.includes('complete')
  )
    return 'success';
  if (
    x.includes('cancel') ||
    x.includes('fail') ||
    x.includes('reject') ||
    x.includes('refund') ||
    x.includes('deleted') ||
    x.includes('out')
  )
    return 'danger';
  if (x.includes('ship')) return 'info';
  if (x.includes('process')) return 'brand';
  if (x.includes('pending') || x.includes('low')) return 'warning';
  return 'neutral';
}
