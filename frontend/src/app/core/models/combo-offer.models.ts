export interface ComboOfferSummary {
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
}

export interface ComboOfferLineItem {
  productId: number;
  productVariantId?: number;
  title: string;
  slug: string;
  quantity: number;
  isRequired: boolean;
  inStock: boolean;
}

export interface ComboOfferDetail {
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
  maxQuantity: number;
  inStock: boolean;
  items: ComboOfferLineItem[];
}
