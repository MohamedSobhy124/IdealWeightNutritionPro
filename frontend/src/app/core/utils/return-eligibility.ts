import { Order } from '../models/order.models';

const RETURNABLE_STATUSES = new Set(['shipped', 'delivered']);

/** Mirrors backend eligibility for showing the return request entry point. */
export function canRequestReturn(order: Pick<Order, 'orderStatus'>): boolean {
  return RETURNABLE_STATUSES.has(order.orderStatus?.trim().toLowerCase() ?? '');
}
