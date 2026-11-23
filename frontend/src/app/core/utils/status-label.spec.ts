import { buildOrderTimeline } from './order-timeline';
import { buildReturnTimeline } from './return-timeline';
import { statusLabel } from './status-label';
import { UiKey } from '../i18n/ui-text';

const t = (key: UiKey) => key;

describe('statusLabel', () => {
  it('translates a known status code', () => {
    expect(statusLabel('Pending', t)).toBe('statusPending');
  });

  it('leaves an unknown status as the server sent it', () => {
    expect(statusLabel('WarehouseHold', t)).toBe('WarehouseHold');
  });

  it('returns an empty string when the status is missing', () => {
    expect(statusLabel(null, t)).toBe('');
  });
});

describe('buildOrderTimeline', () => {
  it('stops a cancelled order after the placed step', () => {
    const items = buildOrderTimeline({ orderStatus: 'Cancelled', orderDate: '2026-01-01' }, t);
    expect(items.map((item) => item.title)).toEqual(['orderStepPlaced', 'orderStepCancelled']);
  });

  it('marks delivery complete only when the order is delivered', () => {
    const items = buildOrderTimeline({ orderStatus: 'Shipped', paymentStatus: 'Paid' }, t);
    expect(items[3].title).toBe('orderStepShipped');
    expect(items[3].active).toBe(true);
    expect(items[4].completed).toBeFalsy();
  });
});

describe('buildReturnTimeline', () => {
  it('branches when a return is rejected', () => {
    const items = buildReturnTimeline('Rejected', '2026-01-01', t);
    expect(items.map((item) => item.title)).toEqual(['returnStepSubmitted', 'returnStepRejected']);
  });
});
