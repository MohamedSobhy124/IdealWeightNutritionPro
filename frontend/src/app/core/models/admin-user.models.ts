export interface AdminUserListItem {
  id: string;
  email: string;
  name: string;
  phoneNumber?: string;
  companyId?: number;
  companyName?: string;
  roles: string[];
}

export interface CreateAdminUserRequest {
  email: string;
  name: string;
  password: string;
  phoneNumber?: string;
  role: string;
  companyId?: number;
}

export interface CreateAdminUserResponse {
  userId: string;
  message: string;
}
