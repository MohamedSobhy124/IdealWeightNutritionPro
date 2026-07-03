export interface NotificationDto {
  id: number;
  title: string;
  message: string;
  type: string;
  icon: string;
  link: string;
  isRead: boolean;
  createdAt: string;
  orderId?: number;
  relatedId?: number;
}

export interface NotificationCountDto {
  count: number;
}

export interface NotificationActionResponse {
  success: boolean;
}
