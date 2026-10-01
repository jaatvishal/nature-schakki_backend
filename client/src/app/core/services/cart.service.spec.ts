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

  it('counts distinct products instead of kilograms', () => {
    const cart = {
      id: '1',
      items: [
        { productId: 1, productName: 'A', price: 110, quantity: 2, pictureUrl: '/a', brand: 'B', type: 'T' },
        { productId: 2, productName: 'B', price: 50, quantity: 3, pictureUrl: '/b', brand: 'B', type: 'T' },
      ],
    };
    service.getCart().subscribe();
    httpMock.expectOne(r => r.method === 'GET').flush(cart);
    expect(service.itemCount()).toBe(2);
    expect(service.subtotal()).toBe(110 * 2 + 50 * 3);
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
