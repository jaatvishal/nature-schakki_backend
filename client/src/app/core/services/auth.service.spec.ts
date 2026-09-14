import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { AuthService } from './auth.service';
import { environment } from '../../../environments/environment';
import { Router } from '@angular/router';
import { vi } from 'vitest';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('should login and store user', () => {
    const response = {
      userId: 1,
      email: 'test@test.com',
      displayName: 'Test User',
      firstName: 'Test',
      lastName: 'User',
      roles: ['Customer'],
      token: 'jwt-token',
      refreshToken: 'refresh-token',
    };

    service.login({ email: 'test@test.com', password: 'Pass@123' }).subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/v1/account/login`);
    expect(req.request.method).toBe('POST');
    req.flush(response);

    expect(service.isLoggedIn()).toBe(true);
    expect(service.getToken()).toBe('jwt-token');
  });

  it('should logout and clear storage', () => {
    vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    localStorage.setItem('token', 'jwt-token');
    localStorage.setItem('user', JSON.stringify({ email: 'a@b.com', roles: [] }));

    service.logout();
    httpMock.expectOne(`${environment.apiUrl}/v1/account/logout`).flush({});

    expect(service.isLoggedIn()).toBe(false);
    expect(localStorage.getItem('token')).toBeNull();
  });

  it('restores and validates authentication after refresh', async () => {
    localStorage.setItem('token', 'persisted-token');
    localStorage.setItem('refreshToken', 'persisted-refresh-token');

    const initialization = service.initialize();
    const req = httpMock.expectOne(`${environment.apiUrl}/v1/account/current-user`);
    expect(req.request.headers.get('Authorization')).toBeNull();
    req.flush({
      userId: 7,
      email: 'refresh@test.com',
      displayName: 'Refresh User',
      firstName: 'Refresh',
      lastName: 'User',
      roles: ['Customer'],
      token: '',
      refreshToken: '',
    });
    await initialization;

    expect(service.isLoggedIn()).toBe(true);
    expect(service.currentUser()?.id).toBe(7);
    expect(service.getToken()).toBe('persisted-token');
  });
});
