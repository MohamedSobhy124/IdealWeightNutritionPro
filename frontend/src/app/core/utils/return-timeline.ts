import { UiTimelineItem } from '../../shared/ui/ui-timeline.component';
import { UiKey } from '../i18n/ui-text';

const STATUS_RANK: Record<string, number> = {
  Pending: 0,
  Approved: 1,
  Processing: 2,
  Completed: 3,
};

type TranslateFn = (key: UiKey) => string;

/** Maps a return status onto the review → received → completed path. */
export function buildReturnTimeline(status: string | null | undefined, requestDate: string | null | undefined, t: TranslateFn): UiTimelineItem[] {
  const current = status ?? 'Pending';

  if (current === 'Rejected' || current === 'Cancelled') {
    return [
      { title: t('returnStepSubmitted'), completed: true, date: requestDate },
      { title: t(current === 'Rejected' ? 'returnStepRejected' : 'returnStepCancelled'), active: true },
    ];
  }

  const rank = STATUS_RANK[current] ?? 0;
  const steps: { key: UiKey; minRank: number }[] = [
    { key: 'returnStepSubmitted', minRank: 0 },
    { key: 'returnStepApproved', minRank: 1 },
    { key: 'returnStepReceived', minRank: 2 },
    { key: 'returnStepCompleted', minRank: 3 },
  ];

  return steps.map((step, index) => ({
    title: t(step.key),
    date: index === 0 ? requestDate : null,
    completed: rank > step.minRank || (rank === step.minRank && rank === 3),
    active: rank === step.minRank && rank < 3,
  }));
}
