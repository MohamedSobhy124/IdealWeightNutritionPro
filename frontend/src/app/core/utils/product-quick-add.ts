import { Router } from '@angular/router';
import { ProductListItem } from '../models/catalogue.models';
import { AddCartItemRequest } from '../models/cart.models';
import { CartService } from '../services/cart.service';

export type ProductQuickAddSource = Pick<
  ProductListItem,
  'id' | 'slug' | 'inStock' | 'productType' | 'displayVariantId'
>;

export function isVariableProduct(productType?: string | null): boolean {
  return (productType ?? '').toLowerCase() === 'variable';
}

export function productQuickAddButtonLabel(
  product: Pick<ProductQuickAddSource, 'inStock' | 'productType'>,
  labels: { addToCart: string; chooseVariant: string },
): string {
  if (product.inStock && isVariableProduct(product.productType)) {
    return labels.chooseVariant;
  }

  return labels.addToCart;
}

/** True when quick-add should open PDP (out of stock or variable without a pinned variant). */
export function shouldNavigateToProductForQuickAdd(
  product: Pick<ProductQuickAddSource, 'slug' | 'inStock' | 'productType'>,
  options?: { pinnedVariantId?: number | null },
): boolean {
  if (!product.inStock) {
    return true;
  }

  const pinnedVariantId = options?.pinnedVariantId;
  if (pinnedVariantId != null && pinnedVariantId > 0) {
    return false;
  }

  return isVariableProduct(product.productType);
}

export function quickAddProduct(
  router: Router,
  cart: CartService,
  product: ProductQuickAddSource,
  options?: {
    pinnedVariantId?: number | null;
    flashSaleItemId?: number;
    quantity?: number;
    onAdded?: () => void;
    onFailed?: () => void;
  },
): void {
  if (shouldNavigateToProductForQuickAdd(product, { pinnedVariantId: options?.pinnedVariantId })) {
    void router.navigate(['/product', product.slug]);
    return;
  }

  const request: AddCartItemRequest = {
    productId: product.id,
    quantity: options?.quantity ?? 1,
    productVariantId: options?.pinnedVariantId ?? product.displayVariantId ?? undefined,
    flashSaleItemId: options?.flashSaleItemId,
  };

  cart.addItem(request).subscribe({
    next: () => options?.onAdded?.(),
    error: () => options?.onFailed?.(),
  });
}
