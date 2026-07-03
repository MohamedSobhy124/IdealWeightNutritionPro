export interface AdminFlashSaleListItem {
  id: number;
  name: string;
  nameAr?: string;
  description?: string;
  imageUrl: string;
  startDate: string;
  endDate: string;
  productCount: number;
  isActive: boolean;
}

export interface AdminFlashSaleItem {
  id: number;
  productId: number;
  productVariantId?: number;
  title: string;
  slug: string;
  imageUrl: string;
  normalPrice: number;
  flashSalePrice: number;
  availableQuantity: number;
  discountPercent: number;
}

export interface AdminFlashSaleDetail {
  id: number;
  name: string;
  nameAr?: string;
  description?: string;
  descriptionAr?: string;
  imageUrl: string;
  startDate: string;
  endDate: string;
  isActive: boolean;
  items: AdminFlashSaleItem[];
}

export interface UpsertAdminFlashSaleRequest {
  name: string;
  nameAr: string;
  description?: string;
  descriptionAr?: string;
  imageUrl: string;
  startDate: string;
  endDate: string;
  isActive: boolean;
  notifySubscribers?: boolean;
}

export interface AddAdminFlashSaleItemRequest {
  productId: number;
  productVariantId?: number;
  flashSalePrice: number;
  flashSaleQuantity: number;
}
