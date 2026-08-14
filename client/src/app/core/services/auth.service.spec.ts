import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { AuthService } from './auth.service';
import { environment } from '../../../environments/environment';

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
      email: 'test@test.com',
      firstName: 'Test',
      lastName: 'User',
      roles: ['Customer'],
      token: 'jwt-token',
    };

    service.login({ email: 'test@test.com', password: 'Pass@123' }).subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/v1/account/login`);
    expect(req.request.method).toBe('POST');
    req.flush(response);

    expect(service.isLoggedIn()).toBe(true);
    expect(service.getToken()).toBe('jwt-token');
  });

  it('should logout and clear storage', () => {
    localStorage.setItem('token', 'jwt-token');
    localStorage.setItem('user', JSON.stringify({ email: 'a@b.com', roles: [] }));

    service.logout();

    expect(service.isLoggedIn()).toBe(false);
    expect(localStorage.getItem('token')).toBeNull();
  });
});
