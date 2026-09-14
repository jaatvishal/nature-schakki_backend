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
  discount: number;
  total: number;
  status: string;
  orderItems: OrderItem[];
  paymentMethod: string;
  paymentStatus: string;
};

export type CreateOrderRequest = {
  deliveryMethodId: number;
  shippingAddress: Address;
  paymentMethod: string;
};

export type DeliveryMethod = {
  id: number;
  shortName: string;
  description?: string;
  deliveryTime?: string;
  deliveryTimeDays?: number;
  price: number;
};
