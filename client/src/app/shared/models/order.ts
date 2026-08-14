import { Address } from './address';

export type OrderItem = {
  productId: number;
  productName: string;
  pictureUrl: string;
  price: number;
  quantity: number;
};

export type Order = {
  id: number;
  buyerEmail: string;
  orderDate: string;
  shipToAddress: Address;
  deliveryMethod?: { shortName: string };
  subtotal: number;
  deliveryCost: number;
  total: number;
  status: string;
  orderItems: OrderItem[];
  paymentStatus?: string;
};

export type CreateOrderRequest = {
  deliveryMethodId: number;
  shippingAddress: Address;
};

export type DeliveryMethod = {
  id: number;
  shortName: string;
  description?: string;
  deliveryTime?: string;
  deliveryTimeDays?: number;
  price: number;
};
