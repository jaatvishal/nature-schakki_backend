import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { Address } from '../../shared/models/address';
import { User } from '../../shared/models/user';

@Injectable({
  providedIn: 'root',
})
export class AccountService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl + '/v1/account';

  getProfile() {
    return this.http.get<User>(`${this.baseUrl}/profile`);
  }

  updateProfile(profile: Partial<User>) {
    return this.http.put<User>(`${this.baseUrl}/profile`, profile);
  }

  getAddresses() {
    return this.http.get<Address[]>(`${this.baseUrl}/addresses`);
  }

  getAddress(id: number) {
    return this.http.get<Address>(`${this.baseUrl}/addresses/${id}`);
  }

  createAddress(address: Address) {
    return this.http.post<Address>(`${this.baseUrl}/addresses`, address);
  }

  updateAddress(id: number, address: Address) {
    return this.http.put<Address>(`${this.baseUrl}/addresses/${id}`, address);
  }

  deleteAddress(id: number) {
    return this.http.delete(`${this.baseUrl}/addresses/${id}`);
  }
}
