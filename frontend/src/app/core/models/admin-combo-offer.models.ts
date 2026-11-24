export interface AdminComboOfferListItem {
  id: number;
  name: string;
  nameAr?: string;
  description?: string;
  imageUrl: string;
  comboPrice: number;
  originalPrice: number;
  savingsPercent: number;
  startDate: string;
  endDate: string;
  productCount: number;
  inStock: boolean;
  isActive: boolean;
}

export interface AdminComboOfferItem {
  id: number;
  productId: number;
  productVariantId?: number;
  productTitle: string;
  quantity: number;
  isRequired: boolean;
  displayOrder: number;
}

export interface AdminComboOfferImage {
  id: number;
  imageUrl: string;
  displayOrder: number;
  imageInfo?: string;
}

export interface UpdateAdminComboOfferItemRequest {
  quantity: number;
  displayOrder: number;
  isRequired: boolean;
}

export interface AdminComboOfferDetail {
  id: number;
  name: string;
  nameAr?: string;
  description?: string;
  descriptionAr?: string;
  imageUrl: string;
  comboPrice: number;
  originalPrice: number;
  savingsPercent: number;
  startDate: string;
  endDate: string;
  minimumQuantity: number;
  maximumQuantity?: number;
  displayOrder: number;
  allowFreeDelivery?: boolean;
  freeDeliveryMinimumAmount?: number;
  inStock: boolean;
  isActive: boolean;
  items: AdminComboOfferItem[];
  images: AdminComboOfferImage[];
}

export interface UpsertAdminComboOfferRequest {
  name: string;
  nameAr: string;
  description?: string;
  descriptionAr?: string;
  imageUrl: string;
  comboPrice: number;
  startDate: string;
  endDate: string;
  minimumQuantity?: number;
  maximumQuantity?: number;
  displayOrder?: number;
  allowFreeDelivery?: boolean;
  freeDeliveryMinimumAmount?: number;
  notifySubscribers?: boolean;
  isActive: boolean;
}

export interface AddAdminComboOfferItemRequest {
  productId: number;
  productVariantId?: number;
  quantity: number;
  isRequired: boolean;
}
