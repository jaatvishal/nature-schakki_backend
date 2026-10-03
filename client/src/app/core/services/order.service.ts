import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { CreateOrderRequest, DeliveryMethod, Order } from '../../shared/models/order';

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
        street: request.shippingAddress.street || request.shippingAddress.address1,
        city: request.shippingAddress.city,
        state: request.shippingAddress.state,
        zipCode: request.shippingAddress.zipCode,
        country: request.shippingAddress.country,
      },
      deliveryMethodId: request.deliveryMethodId,
      paymentMethod: request.paymentMethod,
    });
  }

  getDeliveryMethods() {
    return this.http.get<DeliveryMethod[]>(`${environment.apiUrl}/v1/deliverymethods`);
  }
}
