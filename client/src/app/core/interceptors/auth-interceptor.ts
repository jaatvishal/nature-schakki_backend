import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { BehaviorSubject, catchError, filter, switchMap, take, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

const REFRESH_KEY = 'refreshToken';
const PUBLIC_URLS = ['/product', '/account/login', '/account/register', '/account/forgot-password', '/account/reset-password', '/deliverymethods'];

let isRefreshing = false;
const refreshTokenSubject = new BehaviorSubject<string | null>(null);

const isPublic = (url: string) => PUBLIC_URLS.some(p => url.includes(p));

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const token = authService.getToken();

  let authReq = req;
  if (token && !isPublic(req.url)) {
    authReq = req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
  }

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status !== 401 || isPublic(req.url) || req.url.includes('/account/refresh')) {
        return throwError(() => error);
      }
      if (!localStorage.getItem(REFRESH_KEY)) return throwError(() => error);
      return handle401(authService, authReq, next);
    })
  );
};

function handle401(authService: AuthService, req: Parameters<HttpInterceptorFn>[0], next: Parameters<HttpInterceptorFn>[1]) {
  if (!isRefreshing) {
    isRefreshing = true;
    refreshTokenSubject.next(null);
    return authService.refreshToken().pipe(
      switchMap(response => {
        isRefreshing = false;
        refreshTokenSubject.next(response.token);
        return next(req.clone({ setHeaders: { Authorization: `Bearer ${response.token}` } }));
      }),
      catchError(err => {
        isRefreshing = false;
        if (authService.isLoggedIn()) authService.logout();
        return throwError(() => err);
      })
    );
  }
  return refreshTokenSubject.pipe(
    filter(t => t !== null),
    take(1),
    switchMap(t => next(req.clone({ setHeaders: { Authorization: `Bearer ${t}` } })))
  );
}
