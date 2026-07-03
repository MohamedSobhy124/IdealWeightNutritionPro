export interface AdminCompany {
  id: number;
  name: string;
  streetAddress: string;
  city: string;
  state: string;
  postalCode: string;
  phoneNumber: string;
}

export interface UpsertAdminCompanyRequest {
  name: string;
  streetAddress?: string;
  city?: string;
  state?: string;
  postalCode?: string;
  phoneNumber?: string;
}
