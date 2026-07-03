export interface AdminOrderListItem {
  id: number;
  orderDate: string;
  customerName: string;
  email?: string;
  orderStatus: string;
  paymentStatus: string;
  orderTotal: number;
  city: string;
  isGuestOrder: boolean;
}

export interface AdminOrderQuery {
  status?: string;
  paymentStatus?: string;
  paymentMethod?: string;
  dateFrom?: string;
  dateTo?: string;
  search?: string;
  page?: number;
  pageSize?: number;
}

export interface AdminOrderListResponse {
  items: AdminOrderListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface AdminOrderDetail {
  id: number;
  orderDate: string;
  shippingDate?: string;
  orderStatus: string;
  paymentStatus: string;
  paymentMethod?: string;
  orderTotal: number;
  orderSubtotal?: number;
  discountAmount?: number;
  promoCodeText?: string;
  shipping: number;
  name: string;
  email?: string;
  phoneNumber: string;
  streetAddress: string;
  city: string;
  area?: string;
  trackingNumber?: string;
  carrier?: string;
  isGuestOrder: boolean;
  items: {
    orderDetailId?: number;
    productId: number;
    title: string;
    slug: string;
    quantity: number;
    unitPrice: number;
    lineTotal: number;
  }[];
}

export interface ShipOrderRequest {
  carrier: string;
  trackingNumber: string;
}

export interface AdminOrderActionResponse {
  orderId: number;
  orderStatus: string;
  paymentStatus: string;
  message: string;
}

export interface ForceOrderActionRequest {
  reason?: string;
}

export interface UpdateOrderLineRequest {
  orderDetailId: number;
  quantity: number;
  unitPrice: number;
}

export interface AdminOrderStatistics {
  all: number;
  pending: number;
  approved: number;
  processing: number;
  shipped: number;
  delivered: number;
  cancelled: number;
}

export interface AdminOrderAuditLog {
  id: number;
  orderHeaderId: number;
  action: string;
  actionDetails?: string;
  performedByUserId?: string;
  performedByUserEmail?: string;
  oldOrderStatus?: string;
  newOrderStatus?: string;
  oldPaymentStatus?: string;
  newPaymentStatus?: string;
  actionDate: string;
  ipAddress?: string;
  userAgent?: string;
}
