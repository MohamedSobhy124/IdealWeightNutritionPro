export interface CreateReturnItemRequest {
  orderDetailId: number;
  quantity: number;
  itemReason?: string;
  itemCondition?: string;
}

export interface CreateReturnRequest {
  orderId: number;
  email?: string;
  reason: string;
  additionalNotes?: string;
  items: CreateReturnItemRequest[];
}

export interface ReturnItem {
  id: number;
  orderDetailId: number;
  productId: number;
  productTitle: string;
  quantity: number;
  returnPrice: number;
  itemReason?: string;
  itemCondition?: string;
}

export interface ReturnRequest {
  id: number;
  orderId: number;
  status: string;
  requestDate: string;
  reason: string;
  additionalNotes?: string;
  rejectionReason?: string;
  refundAmount?: number;
  refundStatus?: string;
  items: ReturnItem[];
}

export interface ReturnListItem {
  id: number;
  orderId: number;
  status: string;
  requestDate: string;
  customerEmail?: string;
  refundAmount?: number;
}

export interface RefundOrderRequest {
  refundAmount: number;
  reason?: string;
}

export interface RefundOrderResponse {
  orderId: number;
  orderStatus: string;
  paymentStatus: string;
  refundAmount: number;
  gatewayRefundId?: string;
  message: string;
}
