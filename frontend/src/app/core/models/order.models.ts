export interface OrderSummary {
  id: number;
  orderDate: string;
  orderStatus: string;
  paymentStatus: string;
  orderTotal: number;
}

export interface OrderLine {
  orderDetailId?: number;
  productId: number;
  title: string;
  slug: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export interface Order {
  id: number;
  orderDate: string;
  orderStatus: string;
  paymentStatus: string;
  paymentMethod?: string;
  orderTotal: number;
  orderSubtotal?: number;
  shipping: number;
  name: string;
  email?: string;
  phoneNumber: string;
  streetAddress: string;
  city: string;
  area?: string;
  items: OrderLine[];
}

export interface TrackOrderRequest {
  orderId: number;
  email: string;
}
