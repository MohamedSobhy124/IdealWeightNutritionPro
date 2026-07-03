export interface NewsletterSubscription {
  id: number;
  email: string;
  subscribedDate: string;
  isActive: boolean;
  unsubscribedDate?: string | null;
  source?: string | null;
}

export interface NewsletterSubscribeResponse {
  message: string;
  isReactivated: boolean;
}

export interface NewsletterStatusResponse {
  isSubscribed: boolean;
}

export interface NewsletterUnsubscribeResponse {
  message: string;
}

export interface NewsletterUnsubscribeRequest {
  email?: string;
}
