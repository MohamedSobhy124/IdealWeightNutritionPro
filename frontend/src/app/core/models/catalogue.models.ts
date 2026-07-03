export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface ProductListItem {
  id: number;
  slug: string;
  title: string;
  titleAr: string;
  price: number;
  listPrice: number;
  inStock: boolean;
  imageUrl: string;
  categoryId?: number;
  categoryName?: string;
  brandId?: number;
  brandName?: string;
  isNew: boolean;
  isTrending: boolean;
  displayVariantId?: number | null;
  productType?: string;
}

export interface ProductOptionValue {
  id: number;
  value: string;
  valueAr: string;
}

export interface ProductOption {
  id: number;
  name: string;
  nameAr: string;
  values: ProductOptionValue[];
}

export interface ProductVariant {
  id: number;
  sku?: string;
  price: number;
  listPrice?: number;
  stockQuantity: number;
  imageUrl?: string;
  optionValueIds: number[];
}

export interface ProductDetail {
  id: number;
  slug: string;
  title: string;
  titleAr: string;
  description: string;
  descriptionAr: string;
  suggestedUse?: string;
  suggestedUseAr?: string;
  healthNotes?: string;
  healthNotesAr?: string;
  specification?: string;
  specificationAr?: string;
  price: number;
  listPrice: number;
  inStock: boolean;
  stockQuantity: number;
  productType: string;
  imageUrls: string[];
  category?: Category;
  brand?: Brand;
  isNew: boolean;
  isTrending: boolean;
  options: ProductOption[];
  variants: ProductVariant[];
}

export interface Category {
  id: number;
  name: string;
  nameAr: string;
  imageUrl?: string;
}

export interface Brand {
  id: number;
  name: string;
  nameAr: string;
  imageUrl?: string;
}

export interface ProductQuery {
  search?: string;
  categoryId?: number;
  brandId?: number;
  availability?: string;
  sortBy?: string;
  page?: number;
  pageSize?: number;
}

export interface CategoryProductSection {
  category: Category;
  products: ProductListItem[];
}
