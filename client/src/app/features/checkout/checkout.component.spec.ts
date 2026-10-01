import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { AuthService } from '../../core/services/auth.service';
import { CartService } from '../../core/services/cart.service';
import { OrderService } from '../../core/services/order.service';
import { SnackbarService } from '../../core/services/snackbar.service';
import { CheckoutComponent } from './checkout.component';

describe('CheckoutComponent', () => {
  const order = {
    id: 42,
    buyerEmail: 'customer@test.com',
    orderDate: new Date().toISOString(),
    shipToAddress: {
      firstName: 'Test', lastName: 'Customer', street: '1 Test Street',
      city: 'City', state: 'State', zipCode: '123456', country: 'India',
    },
    subtotal: 100,
    deliveryCost: 0,
    discount: 0,
    total: 100,
    status: 'Processing',
    orderItems: [],
    paymentMethod: 'COD',
    paymentStatus: 'Pending',
  };

  beforeEach(() => {
    sessionStorage.clear();
  });

  it('finalizes forms and prevents a duplicate place-order request', async () => {
    const createOrder = vi.fn().mockReturnValue(of(order));
    await TestBed.configureTestingModule({
      imports: [CheckoutComponent],
      providers: [
        provideRouter([]),
        {
          provide: AuthService,
          useValue: {
            currentUser: () => ({ id: 7, firstName: 'Test', lastName: 'Customer' }),
            getUserId: () => 7,
          },
        },
        {
          provide: CartService,
          useValue: {
            cart: signal({ id: '7', items: [{ productId: 1, quantity: 1 }] }),
            subtotal: signal(100),
            getCart: () => of({ id: '7', items: [] }),
            clearLocalCart: vi.fn(),
          },
        },
        {
          provide: OrderService,
          useValue: {
            getDeliveryMethods: () => of([{ id: 1, shortName: 'Standard', deliveryTimeDays: 5, price: 0 }]),
            createOrder,
            getOrder: () => of(order),
          },
        },
        { provide: SnackbarService, useValue: { success: vi.fn(), error: vi.fn() } },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(CheckoutComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;
    component.addressForm.setValue({
      firstName: 'Test', lastName: 'Customer', phone: '9876543210',
      address1: '1 Test Street', landmark: '', city: 'City', state: 'State',
      zipCode: '123456', country: 'India',
    });
    component.deliveryForm.setValue({ deliveryMethodId: 1 });

    component.placeOrder();
    component.placeOrder();

    expect(createOrder).toHaveBeenCalledTimes(1);
    expect(component.placedOrder()?.id).toBe(42);
    expect(component.addressForm.disabled).toBe(true);
    expect(component.deliveryForm.disabled).toBe(true);
    expect(component.paymentForm.disabled).toBe(true);
    expect(sessionStorage.getItem('completedCheckoutOrder:7')).toBe('42');
  });
});
