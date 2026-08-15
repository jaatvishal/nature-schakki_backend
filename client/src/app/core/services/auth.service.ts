import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse, LoginRequest, RegisterRequest, User } from '../../shared/models/user';

const TOKEN_KEY = 'token';
const REFRESH_KEY = 'refreshToken';
const USER_KEY = 'user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);
  private baseUrl = environment.apiUrl + '/v1/account';
  private userSignal = signal<User | null>(this.loadUserFromStorage());

  currentUser = computed(() => this.userSignal());
  isLoggedIn = computed(() => !!this.getToken() && !!this.userSignal());
  isAdmin = computed(() => this.userSignal()?.roles?.includes('Admin') ?? false);

  getUserId(): number | null {
    return this.userSignal()?.id ?? null;
  }

  register(request: RegisterRequest) {
    return this.http.post<AuthResponse>(`${this.baseUrl}/register`, {
      email: request.email, password: request.password,
      firstName: request.firstName, lastName: request.lastName,
    }).pipe(tap(r => this.setUser(r)));
  }

  login(request: LoginRequest) {
    return this.http.post<AuthResponse>(`${this.baseUrl}/login`, request).pipe(tap(r => this.setUser(r)));
  }

  logout() {
    if (this.getToken()) this.http.post(`${this.baseUrl}/logout`, {}).subscribe({ error: () => {} });
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(REFRESH_KEY);
    localStorage.removeItem(USER_KEY);
    this.userSignal.set(null);
    this.router.navigateByUrl('/auth/login');
  }

  forgotPassword(email: string) {
    return this.http.post<{ message: string }>(`${this.baseUrl}/forgot-password`, { email });
  }

  resetPassword(email: string, token: string, newPassword: string) {
    return this.http.post<{ message: string }>(`${this.baseUrl}/reset-password`, { email, token, newPassword });
  }

  refreshToken() {
    const refreshToken = localStorage.getItem(REFRESH_KEY);
    return this.http.post<AuthResponse>(`${this.baseUrl}/refresh-token`, { refreshToken }).pipe(tap(r => this.setUser(r)));
  }

  getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  getPostLoginRoute(): string {
    return this.isAdmin() ? '/admin' : '/shop';
  }

  private setUser(response: AuthResponse) {
    const parts = (response.displayName || '').split(' ', 2);
    const user: User = {
      id: response.userId,
      email: response.email,
      firstName: response.firstName || parts[0] || response.email,
      lastName: response.lastName || (parts[1] ?? ''),
      roles: response.roles ?? [],
      token: response.token,
    };
    if (!response.token) return;
    localStorage.setItem(TOKEN_KEY, response.token);
    if (response.refreshToken) localStorage.setItem(REFRESH_KEY, response.refreshToken);
    localStorage.setItem(USER_KEY, JSON.stringify(user));
    this.userSignal.set(user);
  }

  private loadUserFromStorage(): User | null {
    try {
      const stored = localStorage.getItem(USER_KEY);
      return stored && this.getToken() ? (JSON.parse(stored) as User) : null;
    } catch { return null; }
  }
}
