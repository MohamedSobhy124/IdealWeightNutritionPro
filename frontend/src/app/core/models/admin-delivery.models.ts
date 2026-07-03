export interface AdminCity {
  id: number;
  name: string;
  nameAr?: string;
  emirate: string;
  emirateAr?: string;
  deliveryCharge: number;
  isActive: boolean;
  displayOrder: number;
  remoteAreaCount?: number;
}

export interface UpsertAdminCityRequest {
  name: string;
  nameAr?: string;
  emirate: string;
  emirateAr?: string;
  deliveryCharge: number;
  isActive: boolean;
  displayOrder: number;
}

export interface AdminRemoteArea {
  id: number;
  cityId: number;
  name: string;
  nameAr?: string;
  deliveryCharge: number;
  isActive: boolean;
  displayOrder: number;
}

export interface UpsertAdminRemoteAreaRequest {
  name: string;
  nameAr?: string;
  deliveryCharge: number;
  isActive: boolean;
  displayOrder: number;
}
