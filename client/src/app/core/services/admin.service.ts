import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AdminAlert,
  AdminAudit,
  AdminCategory,
  AdminDashboard,
  AdminInventory,
  AdminOrder,
  AdminPayment,
  AdminProduct,
  AdminProductInput,
  AdminReport,
  AdminUser,
  InventoryMovement,
  PagedResult,
} from '../../shared/models/admin';

@Injectable({ providedIn: 'root' })
export class AdminService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/v1/admin`;

  getDashboard() {
    return this.http.get<AdminDashboard>(`${this.baseUrl}/dashboard`);
  }

  getUsers(search = '', page = 1, pageSize = 20, sort = 'newest', active?: boolean) {
    return this.http.get<PagedResult<AdminUser>>(`${this.baseUrl}/users`, {
      params: this.params({ search, page, pageSize, sort, active }),
    });
  }

  setUserActive(id: number, isActive: boolean) {
    return this.http.put<void>(`${this.baseUrl}/users/${id}/status`, { isActive });
  }

  getUserOrders(id: number) {
    return this.http.get<AdminOrder[]>(`${this.baseUrl}/users/${id}/orders`);
  }

  getProducts(search = '', categoryId?: number, page = 1, pageSize = 20, sort = 'name', active?: boolean) {
    return this.http.get<PagedResult<AdminProduct>>(`${this.baseUrl}/products`, {
      params: this.params({ search, categoryId, page, pageSize, sort, active }),
    });
  }

  createProduct(product: AdminProductInput) {
    return this.http.post<AdminProduct>(`${this.baseUrl}/products`, product);
  }

  updateProduct(id: number, product: AdminProductInput) {
    return this.http.put<void>(`${this.baseUrl}/products/${id}`, product);
  }

  setProductActive(id: number, active: boolean) {
    return this.http.put<void>(`${this.baseUrl}/products/${id}/active`, active);
  }

  archiveProduct(id: number) {
    return this.http.delete<void>(`${this.baseUrl}/products/${id}`);
  }

  uploadProductImage(file: File) {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<{ url: string }>(`${this.baseUrl}/products/images`, form);
  }

  getCategories(search = '', page = 1, pageSize = 20) {
    return this.http.get<PagedResult<AdminCategory>>(`${this.baseUrl}/categories`, {
      params: { search, page, pageSize },
    });
  }

  saveCategory(
    category: Pick<AdminCategory, 'name' | 'description' | 'isActive'>,
    id?: number
  ): Observable<unknown> {
    return id
      ? this.http.put<void>(`${this.baseUrl}/categories/${id}`, category)
      : this.http.post<AdminCategory>(`${this.baseUrl}/categories`, category);
  }

  getOrders(filters: {
    search?: string;
    status?: string;
    from?: string;
    to?: string;
    sort?: string;
    page?: number;
    pageSize?: number;
  } = {}) {
    return this.http.get<PagedResult<AdminOrder>>(`${this.baseUrl}/orders`, {
      params: this.params(filters),
    });
  }

  getOrder(id: number) {
    return this.http.get<AdminOrder>(`${this.baseUrl}/orders/${id}`);
  }

  updateOrderStatus(id: number, status: string) {
    return this.http.put<void>(`${this.baseUrl}/orders/${id}/status`, { status });
  }

  getInventory(search = '', lowStockOnly = false, page = 1, pageSize = 20) {
    return this.http.get<PagedResult<AdminInventory>>(`${this.baseUrl}/inventory`, {
      params: { search, lowStockOnly, page, pageSize },
    });
  }

  adjustInventory(product: AdminInventory, quantityChange: number, reason: string) {
    return this.http.put<void>(`${this.baseUrl}/inventory/${product.productId}`, {
      quantityChange,
      reason,
      expectedQuantity: product.quantityOnHand,
    });
  }

  getInventoryHistory(productId: number) {
    return this.http.get<InventoryMovement[]>(`${this.baseUrl}/inventory/${productId}/history`);
  }

  getPayments(search = '', status = '', page = 1, pageSize = 20) {
    return this.http.get<PagedResult<AdminPayment>>(`${this.baseUrl}/payments`, {
      params: { search, status, page, pageSize },
    });
  }

  getReport(from?: string, to?: string) {
    return this.http.get<AdminReport>(`${this.baseUrl}/reports`, {
      params: this.params({ from, to }),
    });
  }

  getAudit(search = '', page = 1, pageSize = 20) {
    return this.http.get<PagedResult<AdminAudit>>(`${this.baseUrl}/audit`, {
      params: { search, page, pageSize },
    });
  }

  getAlerts() {
    return this.http.get<AdminAlert[]>(`${this.baseUrl}/alerts`);
  }

  private params(values: Record<string, string | number | boolean | undefined>) {
    let params = new HttpParams();
    for (const [key, value] of Object.entries(values)) {
      if (value !== undefined && value !== '') params = params.set(key, value);
    }
    return params;
  }
}
