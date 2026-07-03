export interface CartItem {
  lineId: string;
  productId: number;
  productVariantId?: number;
  flashSaleItemId?: number;
  comboOfferId?: number;
  title: string;
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
