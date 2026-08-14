import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatButton } from '@angular/material/button';
import { MatFormField, MatLabel } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { MatRadioButton, MatRadioGroup } from '@angular/material/radio';
import { MatStep, MatStepper } from '@angular/material/stepper';
import { AuthService } from '../../core/services/auth.service';
import { CartService } from '../../core/services/cart.service';
import { OrderService } from '../../core/services/order.service';
import { SnackbarService } from '../../core/services/snackbar.service';
import { DeliveryMethod } from '../../shared/models/order';

@Component({
  selector: 'app-checkout',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, MatStepper, MatStep, MatFormField, MatLabel, MatInput,
    MatRadioGroup, MatRadioButton, MatButton, CurrencyPipe, RouterLink],
  templateUrl: './checkout.component.html',
})
export class CheckoutComponent implements OnInit {
  private fb = inject(FormBuilder);
  cartService = inject(CartService);
  private orderService = inject(OrderService);
  private authService = inject(AuthService);
  private router = inject(Router);
  private snackbar = inject(SnackbarService);

  placing = signal(false);
  deliveryMethods = signal<DeliveryMethod[]>([]);

  addressForm = this.fb.group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    address1: ['', Validators.required],
    city: ['', Validators.required],
    state: ['', Validators.required],
    zipCode: ['', Validators.required],
    country: ['India', Validators.required],
  });

  deliveryForm = this.fb.group({ deliveryMethodId: [null as number | null, Validators.required] });
  paymentForm = this.fb.group({ paymentMethod: ['cod', Validators.required] });

  ngOnInit(): void {
    const user = this.authService.currentUser();
    if (user) {
      this.addressForm.patchValue({ firstName: user.firstName, lastName: user.lastName });
    }
    this.cartService.getCart().subscribe({
      error: () => this.router.navigateByUrl('/shop'),
    });
    this.orderService.getDeliveryMethods().subscribe({
      next: m => this.deliveryMethods.set(m),
      error: () => this.deliveryMethods.set([
        { id: 1, shortName: 'Standard', deliveryTimeDays: 5, price: 50 },
        { id: 2, shortName: 'Express', deliveryTimeDays: 2, price: 100 },
      ]),
    });
  }

  selectedDelivery(): DeliveryMethod | undefined {
    return this.deliveryMethods().find(m => m.id === this.deliveryForm.value.deliveryMethodId);
  }

  total(): number {
    return this.cartService.subtotal() + (this.selectedDelivery()?.price ?? 0);
  }

  placeOrder() {
    if (this.addressForm.invalid || this.deliveryForm.invalid || this.placing()) return;
    this.placing.set(true);
    const addr = this.addressForm.getRawValue();
    this.orderService.createOrder({
      deliveryMethodId: this.deliveryForm.value.deliveryMethodId!,
      paymentMethod: this.paymentForm.value.paymentMethod!,
      shippingAddress: {
        firstName: addr.firstName!, lastName: addr.lastName!, street: addr.address1!,
        city: addr.city!, state: addr.state!, zipCode: addr.zipCode!, country: addr.country!,
      },
    }).subscribe({
      next: order => {
        this.snackbar.success('Order placed successfully!');
        this.cartService.deleteCart().subscribe();
        this.router.navigate(['/account/orders', order.id]);
      },
      error: () => {
        this.snackbar.error('Failed to place order. Check cart and try again.');
        this.placing.set(false);
      },
    });
  }
}
