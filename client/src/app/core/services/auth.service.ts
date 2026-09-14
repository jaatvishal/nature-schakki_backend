import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, firstValueFrom, of, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AuthResponse,
  LoginRequest,
  RegisterRequest,
  RegisterResult,
  User,
} from '../../shared/models/user';

const TOKEN_KEY = 'token';
const REFRESH_TOKEN_KEY = 'refreshToken';
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
  isLoggedIn = computed(() => !!this.userSignal() && !!this.getToken());
  isAdmin = computed(() => this.userSignal()?.roles?.includes('Admin') ?? false);

  register(request: RegisterRequest) {
    return this.http.post<RegisterResult>(`${this.baseUrl}/register`, request);
  }

  verifyEmail(email: string, otp: string) {
    return this.http.post<AuthResponse>(`${this.baseUrl}/verify-email`, { email, otp }).pipe(
      tap(response => this.setUser(response))
    );
  }

  resendVerification(email: string) {
    return this.http.post<RegisterResult>(`${this.baseUrl}/resend-verification`, { email });
  }

  login(request: LoginRequest) {
    return this.http.post<AuthResponse>(`${this.baseUrl}/login`, request).pipe(
      tap(response => this.setUser(response))
    );
  }

  logout() {
    if (this.getToken()) {
      this.http.post(`${this.baseUrl}/logout`, {}).subscribe({ error: () => {} });
    }
    this.clearSession();
    this.router.navigateByUrl('/');
  }

  private clearSession() {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    this.userSignal.set(null);
  }

  refreshToken() {
    const refreshToken = localStorage.getItem(REFRESH_TOKEN_KEY);
    return this.http.post<AuthResponse>(`${this.baseUrl}/refresh`, { refreshToken }).pipe(
      tap(response => this.setUser(response))
    );
  }

  getCurrentUser() {
    return this.http.get<AuthResponse>(`${this.baseUrl}/current`).pipe(
      tap(response => this.setUser(response))
    );
  }

  async initialize(): Promise<void> {
    if (!this.getToken()) {
      this.clearSession();
      return;
    }

    await firstValueFrom(
      this.getCurrentUser().pipe(
        catchError(() => {
          this.clearSession();
          return of(null);
        })
      )
    );
  }

  getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  hasRefreshToken(): boolean {
    return !!localStorage.getItem(REFRESH_TOKEN_KEY);
  }

  getUserId(): number | null {
    return this.userSignal()?.id ?? null;
  }

  private setUser(response: AuthResponse) {
    const user: User = {
      id: response.userId,
      email: response.email,
      displayName: response.displayName,
      firstName: response.firstName,
      lastName: response.lastName,
      roles: response.roles ?? [],
      token: response.token,
    };
    localStorage.setItem(TOKEN_KEY, response.token);
    if (response.refreshToken)
      localStorage.setItem(REFRESH_TOKEN_KEY, response.refreshToken);
    localStorage.setItem(USER_KEY, JSON.stringify(user));
    this.userSignal.set(user);
  }

  private loadUserFromStorage(): User | null {
    if (!this.getToken()) return null;
    const stored = localStorage.getItem(USER_KEY);
    if (!stored) return null;
    try {
      return JSON.parse(stored) as User;
    } catch {
      return null;
    }
  }
}
