import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Address } from '../../shared/models/address';
import { AuthResponse, User } from '../../shared/models/user';

@Injectable({ providedIn: 'root' })
export class AccountService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl + '/v1/account';

  getProfile() {
    return this.http.get<AuthResponse>(`${this.baseUrl}/profile`).pipe(
      map(r => ({
        id: r.userId, email: r.email,
        firstName: r.firstName || r.displayName?.split(' ')[0] || '',
        lastName: r.lastName || r.displayName?.split(' ')[1] || '',
        roles: r.roles ?? [],
      } as User))
    );
  }

  getAddresses() { return this.http.get<Address[]>(`${this.baseUrl}/addresses`); }
  createAddress(a: Address) { return this.http.post<Address>(`${this.baseUrl}/addresses`, a); }
  updateAddress(id: number, a: Address) { return this.http.put<Address>(`${this.baseUrl}/addresses/${id}`, a); }
  deleteAddress(id: number) { return this.http.delete(`${this.baseUrl}/addresses/${id}`); }
}
