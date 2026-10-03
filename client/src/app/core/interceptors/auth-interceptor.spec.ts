import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AuthService } from '../services/auth.service';
import { authInterceptor } from './auth-interceptor';

describe('authInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        {
          provide: AuthService,
          useValue: {
            getToken: () => 'admin-token',
            hasRefreshToken: () => false,
            isLoggedIn: () => true,
          },
        },
      ],
    });
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('attaches the JWT to Admin product and image upload requests', () => {
    http.get('/api/v1/admin/products').subscribe();
    const products = httpMock.expectOne('/api/v1/admin/products');
    expect(products.request.headers.get('Authorization')).toBe('Bearer admin-token');
    products.flush({ items: [], totalCount: 0 });

    http.post('/api/v1/admin/products/images', new FormData()).subscribe();
    const upload = httpMock.expectOne('/api/v1/admin/products/images');
    expect(upload.request.headers.get('Authorization')).toBe('Bearer admin-token');
    upload.flush({ url: '/uploads/product.png' });
  });

  it('does not attach the JWT to login', () => {
    http.post('/api/v1/account/login', {}).subscribe();
    const login = httpMock.expectOne('/api/v1/account/login');
    expect(login.request.headers.has('Authorization')).toBe(false);
    login.flush({});
  });
});
