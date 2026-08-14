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
  shippingAddress: Address;
  deliveryMethod: string;
  subtotal: number;
  deliveryFee: number;
  total: number;
  status: string;
  orderItems: OrderItem[];
};

export type CreateOrderRequest = {
  basketId: string;
  deliveryMethodId: number;
  shippingAddress: Address;
  paymentMethod: string;
};

export type DeliveryMethod = {
  id: number;
  shortName: string;
  deliveryTime: string;
  price: number;
};
