import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { CartService } from './cart.service';
import { environment } from '../../../environments/environment';
import { provideRouter } from '@angular/router';

describe('CartService', () => {
  let service: CartService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    service = TestBed.inject(CartService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('should create buyer id on init', () => {
    expect(service.getCartId()).toBeTruthy();
    expect(localStorage.getItem('guestCartId')).toBeTruthy();
  });

  it('should fetch cart', () => {
    const cart = { id: 'buyer-1', items: [] };

    service.getCart().subscribe(result => {
      expect(result.items.length).toBe(0);
    });

    const req = httpMock.expectOne(
      r => r.url.startsWith(`${environment.apiUrl}/v1/cart`) && r.method === 'GET'
    );
    req.flush(cart);
  });
});
