export interface AdminServiceListItem {
  id: number;
  title: string;
  titleAr?: string | null;
  price: number;
  serviceType: string;
  imageUrl?: string | null;
  isActive: boolean;
  displayOrder: number;
  imageCount: number;
  purchaseCount: number;
}

export interface AdminServiceImage {
  id: number;
  imageUrl: string;
  displayOrder: number;
}

export interface AdminServiceDetail {
  id: number;
  title: string;
  titleAr?: string | null;
  description?: string | null;
  descriptionAr?: string | null;
  price: number;
  serviceType: string;
  offlinePaymentPercent?: number | null;
  imageUrl?: string | null;
  isActive: boolean;
  displayOrder: number;
  createdDate: string;
  updatedDate?: string | null;
  images: AdminServiceImage[];
}

export interface UpsertAdminServiceRequest {
  title: string;
  titleAr?: string | null;
  description?: string | null;
  descriptionAr?: string | null;
  price: number;
  serviceType: string;
  offlinePaymentPercent?: number | null;
  isActive: boolean;
  displayOrder: number;
}

export interface AdminImageUploadResult {
  imageUrl: string;
}
