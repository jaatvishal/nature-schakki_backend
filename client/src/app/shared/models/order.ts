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
  shippingAddress: Omit<Address, 'address1' | 'address2'> & { street: string };
  deliveryMethod: string;
  subtotal: number;
  deliveryCost: number;
  discount: number;
  total: number;
  status: string;
  paymentMethod: string;
  paymentStatus: string;
  orderItems: OrderItem[];
};

export type CreateOrderRequest = {
  deliveryMethodId: number;
  shipToAddress: {
    firstName: string;
    lastName: string;
    street: string;
    city: string;
    state: string;
    zipCode: string;
    country: string;
  };
  paymentMethod: string;
};

export type DeliveryMethod = {
  id: number;
  shortName: string;
  deliveryTimeDays: number;
  price: number;
};
