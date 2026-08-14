import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { CreateOrderRequest, DeliveryMethod, Order } from '../../shared/models/order';
import { Product } from '../../shared/models/product';

@Injectable({ providedIn: 'root' })
export class OrderService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl + '/v1/orders';

  getOrders() {
    return this.http.get<Order[]>(this.baseUrl);
  }

  getOrder(id: number) {
    return this.http.get<Order>(`${this.baseUrl}/${id}`);
  }

  createOrder(request: CreateOrderRequest) {
    return this.http.post<Order>(this.baseUrl, {
      shipToAddress: {
        firstName: request.shippingAddress.firstName,
        lastName: request.shippingAddress.lastName,
        street: request.shippingAddress.address1,
        city: request.shippingAddress.city,
        state: request.shippingAddress.state,
        zipCode: request.shippingAddress.zipCode,
        country: request.shippingAddress.country,
      },
      deliveryMethodId: request.deliveryMethodId,
    });
  }

  getDeliveryMethods() {
    return this.http.get<DeliveryMethod[]>(`${environment.apiUrl}/v1/deliverymethods`);
  }

  getAdminOrders() {
    return this.http.get<AdminOrder[]>(`${environment.apiUrl}/v1/admin/orders`);
  }

  updateOrderStatus(id: number, status: string) {
    return this.http.put(`${environment.apiUrl}/v1/admin/orders/${id}/status`, { status });
  }

  getAdminPayments() {
    return this.http.get<AdminPayment[]>(`${environment.apiUrl}/v1/admin/payments`);
  }
}

export type AdminOrder = Order & { paymentStatus?: string };
export type AdminPayment = {
  id: number;
  orderId: number;
  buyerEmail: string;
  amount: number;
  status: string;
  paymentIntentId: string;
  createdAt: string;
};

@Injectable({ providedIn: 'root' })
export class AdminService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl + '/v1/admin';

  createProduct(product: Partial<Product>) {
    return this.http.post<Product>(`${this.baseUrl}/products`, product);
  }
}
