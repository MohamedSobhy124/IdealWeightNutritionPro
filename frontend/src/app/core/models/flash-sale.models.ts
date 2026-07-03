export interface FlashSaleSummary {
  id: number;
  name: string;
  nameAr?: string;
  description?: string;
  imageUrl: string;
  startDate: string;
  endDate: string;
  productCount: number;
}

export interface FlashSaleItem {
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
  productType?: string;
}

export interface FlashSaleDetail {
  id: number;
  name: string;
  nameAr?: string;
  description?: string;
  imageUrl: string;
  startDate: string;
  endDate: string;
  items: FlashSaleItem[];
}

export interface FlashSaleProductPrice {
  productId: number;
  productVariantId?: number | null;
  flashSalePrice: number;
}
