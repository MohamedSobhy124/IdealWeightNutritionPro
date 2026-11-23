import { UiTimelineItem } from '../../shared/ui/ui-timeline.component';
import { UiKey } from '../i18n/ui-text';
import { statusLabel } from './status-label';

const STATUS_RANK: Record<string, number> = {
  Pending: 0,
  Paid: 1,
  Approved: 1,
  Processing: 2,
  Shipped: 3,
  Delivered: 4,
};

const CLOSED_STATUSES = new Set(['Refunded', 'PartiallyRefunded', 'Returned', 'ReturnApproved', 'ReturnRequested']);

export interface OrderTimelineSource {
  orderStatus?: string | null;
  paymentStatus?: string | null;
  orderDate?: string | null;
}

type TranslateFn = (key: UiKey) => string;

export function buildOrderTimeline(order: OrderTimelineSource, t: TranslateFn): UiTimelineItem[] {
  const status = order.orderStatus ?? 'Pending';
  const payment = statusLabel(order.paymentStatus, t);

  if (status === 'Cancelled') {
    return [
      { title: t('orderStepPlaced'), completed: true, subtitle: payment, date: order.orderDate },
      { title: t('orderStepCancelled'), active: true },
    ];
  }

  if (CLOSED_STATUSES.has(status)) {
    return [
      { title: t('orderStepPlaced'), completed: true, subtitle: payment, date: order.orderDate },
      { title: statusLabel(status, t), active: true },
    ];
  }

  const rank = STATUS_RANK[status] ?? 0;
  const steps: { key: UiKey; minRank: number; subtitle?: string | null }[] = [
    { key: 'orderStepPlaced', minRank: 0 },
    { key: 'orderStepPayment', minRank: 1, subtitle: payment || null },
    { key: 'orderStepProcessing', minRank: 2 },
    { key: 'orderStepShipped', minRank: 3 },
    { key: 'orderStepDelivered', minRank: 4 },
  ];

  return steps.map((step, index) => {
    const completed = rank > step.minRank || (rank === step.minRank && rank === 4);
    const active = rank === step.minRank && rank < 4;
    return {
      title: t(step.key),
      subtitle: step.subtitle ?? null,
      date: index === 0 ? order.orderDate : null,
      completed,
      active,
    };
  });
}
