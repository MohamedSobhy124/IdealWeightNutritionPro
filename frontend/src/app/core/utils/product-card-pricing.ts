import { ProductListItem } from '../models/catalogue.models';
import { FlashSaleProductPrice } from '../models/flash-sale.models';

export interface ProductCardPricing {
  price: number;
  listPrice: number;
  isFlashSale: boolean;
}

export function resolveProductCardPricing(
  product: Pick<ProductListItem, 'id' | 'price' | 'listPrice' | 'displayVariantId'>,
  flashPrices: FlashSaleProductPrice[]
): ProductCardPricing {
  let price = product.price;
  let listPrice = product.listPrice > 0 ? product.listPrice : product.price;
  let isFlashSale = false;

  const forProduct = flashPrices.filter((f) => f.productId === product.id);

  if (product.displayVariantId) {
    const variantFlash = forProduct.find((f) => f.productVariantId === product.displayVariantId);
    if (variantFlash && variantFlash.flashSalePrice < price) {
      price = variantFlash.flashSalePrice;
      isFlashSale = true;
    }
  }

  const productFlash = forProduct.find((f) => !f.productVariantId);
  if (productFlash && productFlash.flashSalePrice < price) {
    price = productFlash.flashSalePrice;
    isFlashSale = true;
    if (listPrice <= price) {
      listPrice = product.price;
    }
  }

  return { price, listPrice, isFlashSale };
}
