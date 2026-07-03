export interface AdminServicePurchaseListItem {
  id: number;
  serviceSubscriptionId: number;
  serviceTitle: string;
  customerName: string;
  email?: string | null;
  phone?: string | null;
  totalAmount: number;
  amountPaid: number;
  discountAmount: number;
  vatAmount: number;
  paymentStatus: string;
  serviceStatus: string;
  purchaseDate: string;
}

export interface AdminServicePurchaseDetail extends AdminServicePurchaseListItem {
  serviceOfferId?: number | null;
  paymentIntentId?: string | null;
  sessionId?: string | null;
  offerSummary?: string | null;
  isGuest: boolean;
}

export interface AdminServicePurchaseListResponse {
  items: AdminServicePurchaseListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface AdminServicePurchaseQuery {
  paymentStatus?: string;
  serviceStatus?: string;
  dateFrom?: string;
  dateTo?: string;
  search?: string;
  page?: number;
  pageSize?: number;
}

export interface AdminServicePurchaseStatistics {
  all: number;
  pending: number;
  approved: number;
  rejected: number;
}

export interface UpdateAdminServicePurchaseRequest {
  paymentStatus?: string;
  serviceStatus?: string;
  amountPaid?: number;
}

export interface AdminServicePurchaseActionResponse {
  purchaseId: number;
  paymentStatus: string;
  serviceStatus: string;
  amountPaid: number;
  message: string;
}
