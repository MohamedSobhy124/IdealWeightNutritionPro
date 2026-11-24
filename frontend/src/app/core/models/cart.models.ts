export interface CartItem {
  lineId: string;
  productId: number;
  productVariantId?: number;
  flashSaleItemId?: number;
  comboOfferId?: number;
  title: string;
  titleAr?: string | null;
  slug: string;
  imageUrl: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
  inStock: boolean;
  maxQuantity: number;
}

export interface CartPromo {
  id: number;
  code: string;
  description: string;
  discountAmount: number;
}

export interface CartResponse {
  items: CartItem[];
  itemCount: number;
  subtotal: number;
  discount: number;
  total: number;
  appliedPromo?: CartPromo;
  /** Free-delivery progress (does not depend on the delivery city). */
  freeDelivery?: CartFreeDelivery | null;
}

export interface CartFreeDelivery {
  /** Every item allows free delivery, so a threshold applies. */
  available: boolean;
  /** Subtotal (before promo discount) needed for free delivery. */
  threshold?: number | null;
  qualifies: boolean;
  amountRemaining: number;
}

export interface AddCartItemRequest {
  productId: number;
  quantity: number;
  productVariantId?: number;
  flashSaleItemId?: number;
  comboOfferId?: number;
}

export interface ApplyPromoRequest {
  code: string;
}
