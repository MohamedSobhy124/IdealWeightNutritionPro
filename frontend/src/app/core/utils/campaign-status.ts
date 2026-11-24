import type { UiBadgeVariant } from '../../shared/ui/ui-badge.component';

/** Where a dated, switchable campaign (promo code, flash sale, combo offer) stands right now. */
export type CampaignState = 'live' | 'scheduled' | 'ended' | 'off';

export interface CampaignLike {
  isActive: boolean;
  startDate?: string | null;
  endDate?: string | null;
}

export function campaignState(item: CampaignLike, now: number = Date.now()): CampaignState {
  if (!item.isActive) return 'off';
  const start = item.startDate ? Date.parse(item.startDate) : NaN;
  const end = item.endDate ? Date.parse(item.endDate) : NaN;
  if (!Number.isNaN(end) && end < now) return 'ended';
  if (!Number.isNaN(start) && start > now) return 'scheduled';
  return 'live';
}

export const CAMPAIGN_VARIANT: Record<CampaignState, UiBadgeVariant> = {
  live: 'success',
  scheduled: 'info',
  ended: 'neutral',
  off: 'neutral',
};

/** Tab order for campaign lists; `all` shows everything. */
export const CAMPAIGN_TABS = ['live', 'scheduled', 'ended', 'off', 'all'] as const;
export type CampaignTab = (typeof CAMPAIGN_TABS)[number];
