import { AdminOrderListItem } from './admin-order.models';

export interface AdminDashboard {
  ordersToday: number;
  revenueToday: number;
  pendingReturns: number;
  activeReturns: number;
  lowStockProducts: number;
  outOfStockProducts: number;
  activeFlashSales: number;
  activeComboOffers: number;
  publishedBlogPosts: number;
  hiddenBlogPosts: number;
  activeServices: number;
  activeCities: number;
  pendingStockNotifications: number;
  servicePurchasesTotal: number;
  servicePurchasesPending: number;
  servicePurchasesApproved: number;
  servicePurchasesRejected: number;
  ordersTotal: number;
  ordersPending: number;
  ordersApproved: number;
  ordersProcessing: number;
  ordersShipped: number;
  ordersDelivered: number;
  ordersCancelled: number;
  recentOrders: AdminOrderListItem[];
}
