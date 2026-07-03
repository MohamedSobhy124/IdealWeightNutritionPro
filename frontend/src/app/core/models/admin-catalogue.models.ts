export interface AdminCategory {
  id: number;
  name: string;
  nameAr: string;
  description: string;
  descriptionAr: string;
  imageUrl?: string;
  isDeleted: boolean;
}

export interface UpsertAdminCategoryRequest {
  name: string;
  nameAr: string;
  description?: string;
  descriptionAr?: string;
  imageUrl?: string;
}

export interface AdminBrand {
  id: number;
  name: string;
  nameAr: string;
  description?: string;
  descriptionAr?: string;
  imageUrl?: string;
  isDeleted: boolean;
}

export interface UpsertAdminBrandRequest {
  name: string;
  nameAr: string;
  description?: string;
  descriptionAr?: string;
  imageUrl?: string;
}
