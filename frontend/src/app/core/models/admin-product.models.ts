export interface AdminProductListItem {
  id: number;
  title: string;
  slug: string;
  imageUrl: string;
  price: number;
  listPrice: number;
  stockQuantity: number;
  productType: string;
  categoryName?: string;
  brandName?: string;
  isDeleted: boolean;
  inStock: boolean;
  isNew: boolean;
  isTrending: boolean;
}

export type AdminProductFilter =
  | 'active'
  | 'all'
  | 'deleted'
  | 'lowstock'
  | 'outofstock'
  | 'instock'
  | 'new'
  | 'trending';

export interface AdminProductListResponse {
  items: AdminProductListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface AdminProductOptionValue {
  id: number;
  value: string;
  valueAr: string;
  displayOrder: number;
}

export interface AdminProductOption {
  id: number;
  name: string;
  nameAr: string;
  displayOrder: number;
  values: AdminProductOptionValue[];
}

export interface AdminProductVariant {
  id: number;
  sku?: string;
  variantName?: string;
  imageUrl?: string;
  price: number;
  listPrice?: number;
  price50?: number;
  price100?: number;
  stockQuantity: number;
  minimumStockAlert: number;
  expiryDate?: string;
  isDeleted: boolean;
}

export interface AdminProductImage {
  id: number;
  imageUrl: string;
  displayOrder: number;
  imageInfo?: string;
}

export interface AdminProductDetail {
  id: number;
  title: string;
  slug: string;
  imageUrl: string;
  price: number;
  listPrice: number;
  storeCost?: number | null;
  stockQuantity: number;
  minimumStockAlert: number;
  isNew: boolean;
  isTrending: boolean;
  allowFreeDelivery: boolean;
  freeDeliveryMinimumAmount: number;
  productType: string;
  categoryName?: string;
  brandName?: string;
  isDeleted: boolean;
  variants: AdminProductVariant[];
  images: AdminProductImage[];
  options: AdminProductOption[];
}

export interface AddAdminProductOptionRequest {
  name: string;
  nameAr: string;
  displayOrder: number;
}

export interface AddAdminProductOptionValueRequest {
  value: string;
  valueAr: string;
  displayOrder: number;
}

export interface UpdateAdminProductVariantDetailRequest {
  price: number;
  listPrice?: number;
  price50?: number;
  price100?: number;
  stockQuantity: number;
  minimumStockAlert: number;
  sku?: string;
  expiryDate?: string;
}

export interface GenerateVariantsResponse {
  created: number;
  skipped: number;
  message: string;
}

export interface RegenerateProductSlugsResponse {
  success: boolean;
  message: string;
  updatedCount: number;
  totalProducts: number;
}

export interface SetProductTypeRequest {
  productType: string;
}

export interface UpdateAdminProductVariantRequest {
  id: number;
  price: number;
  listPrice?: number;
  stockQuantity: number;
}

export interface UpdateAdminProductRequest {
  title: string;
  price: number;
  listPrice: number;
  storeCost?: number | null;
  stockQuantity: number;
  minimumStockAlert: number;
  isNew: boolean;
  isTrending: boolean;
  allowFreeDelivery: boolean;
  freeDeliveryMinimumAmount: number;
  isDeleted: boolean;
  variants?: UpdateAdminProductVariantRequest[];
}

export interface CreateAdminProductRequest {
  title: string;
  titleAr?: string;
  description?: string;
  descriptionAr?: string;
  slug?: string;
  categoryId: number;
  brandId?: number;
  price: number;
  listPrice: number;
  storeCost?: number | null;
  stockQuantity: number;
  minimumStockAlert?: number;
  isNew?: boolean;
  isTrending?: boolean;
  allowFreeDelivery?: boolean;
  freeDeliveryMinimumAmount?: number;
  expiryDate?: string;
}
