import { UiTimelineItem } from '../../shared/ui/ui-timeline.component';
import { Order } from '../models/order.models';
import { UiKey } from '../i18n/ui-text';

const STATUS_RANK: Record<string, number> = {
  Pending: 0,
  Paid: 1,
  Approved: 1,
  Processing: 2,
  Shipped: 3,
  Delivered: 4,
};

type TranslateFn = (key: UiKey) => string;

export function buildOrderTimeline(order: Order, t: TranslateFn): UiTimelineItem[] {
  const status = order.orderStatus ?? 'Pending';

  if (status === 'Cancelled') {
    return [
      { title: t('orderStepPlaced'), completed: true, subtitle: order.paymentStatus },
      { title: t('orderStepCancelled'), active: true, subtitle: status },
    ];
  }

  const rank = STATUS_RANK[status] ?? 0;
  const steps: { key: UiKey; minRank: number; subtitle?: string }[] = [
    { key: 'orderStepPlaced', minRank: 0 },
    { key: 'orderStepPayment', minRank: 1, subtitle: order.paymentStatus },
    { key: 'orderStepProcessing', minRank: 2 },
    { key: 'orderStepShipped', minRank: 3 },
    { key: 'orderStepDelivered', minRank: 4 },
  ];

  return steps.map((step) => {
    const completed = rank > step.minRank || (rank === step.minRank && rank === 4);
    const active = rank === step.minRank && rank < 4;
    return {
      title: t(step.key),
      subtitle: step.subtitle ?? null,
      completed,
      active,
    };
  });
}
