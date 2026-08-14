import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse, LoginRequest, RegisterRequest, User } from '../../shared/models/user';

const TOKEN_KEY = 'token';
const USER_KEY = 'user';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);
  private baseUrl = environment.apiUrl + '/v1/account';

  private userSignal = signal<User | null>(this.loadUserFromStorage());

  currentUser = computed(() => this.userSignal());
  isLoggedIn = computed(() => !!this.userSignal());
  isAdmin = computed(() => this.userSignal()?.roles.includes('Admin') ?? false);

  register(request: RegisterRequest) {
    return this.http.post<AuthResponse>(`${this.baseUrl}/register`, request).pipe(
      tap(response => this.setUser(response))
    );
  }

  login(request: LoginRequest) {
    return this.http.post<AuthResponse>(`${this.baseUrl}/login`, request).pipe(
      tap(response => this.setUser(response))
    );
  }

  logout() {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    this.userSignal.set(null);
    this.router.navigateByUrl('/');
  }

  refreshToken() {
    return this.http.post<AuthResponse>(`${this.baseUrl}/refresh-token`, {}).pipe(
      tap(response => this.setUser(response))
    );
  }

  getCurrentUser() {
    return this.http.get<User>(`${this.baseUrl}/current-user`).pipe(
      tap(user => {
        const token = this.getToken();
        this.userSignal.set({ ...user, token: token ?? undefined });
        localStorage.setItem(USER_KEY, JSON.stringify(this.userSignal()));
      })
    );
  }

  getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  private setUser(response: AuthResponse) {
    const user: User = {
      email: response.email,
      firstName: response.firstName,
      lastName: response.lastName,
      roles: response.roles,
      token: response.token,
    };
    localStorage.setItem(TOKEN_KEY, response.token);
    localStorage.setItem(USER_KEY, JSON.stringify(user));
    this.userSignal.set(user);
  }

  private loadUserFromStorage(): User | null {
    const stored = localStorage.getItem(USER_KEY);
    if (!stored) return null;
    try {
      return JSON.parse(stored) as User;
    } catch {
      return null;
    }
  }
}
