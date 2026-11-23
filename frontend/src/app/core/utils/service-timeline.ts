import { UiTimelineItem } from '../../shared/ui/ui-timeline.component';
import { UiKey } from '../i18n/ui-text';
import { statusLabel } from './status-label';

const PAID = new Set(['Paid', 'Approved']);

type TranslateFn = (key: UiKey) => string;

export interface ServiceTimelineSource {
  status?: string | null;
  paymentStatus?: string | null;
  purchaseDate?: string | null;
}

/** Placed, then paid or cancelled. Uses only the status the purchase already has. */
export function buildServicePurchaseTimeline(purchase: ServiceTimelineSource, t: TranslateFn): UiTimelineItem[] {
  const paid = PAID.has(purchase.paymentStatus ?? '');
  const payment: UiTimelineItem = purchase.status === 'Cancelled'
    ? { title: t('statusCancelled'), active: true, subtitle: statusLabel(purchase.paymentStatus, t) }
    : {
        title: t('purchaseStepPaid'),
        completed: paid,
        active: !paid,
        subtitle: statusLabel(purchase.paymentStatus, t),
      };

  return [
    { title: t('purchaseStepPlaced'), completed: true, date: purchase.purchaseDate },
    payment,
  ];
}
