export interface WishlistItem {
  id: number;
  productId: number;
  title: string;
  slug: string;
  imageUrl: string;
  price: number;
  listPrice: number;
  inStock: boolean;
  productType?: string;
}

export interface WishlistResponse {
  items: WishlistItem[];
  count: number;
}

export interface WishlistToggleResponse {
  isInWishlist: boolean;
  wishlistCount: number;
  message: string;
}
