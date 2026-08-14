import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatButton } from '@angular/material/button';
import { MatFormField, MatLabel } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { MatRadioButton, MatRadioGroup } from '@angular/material/radio';
import { MatStep, MatStepper } from '@angular/material/stepper';
import { CartService } from '../../core/services/cart.service';
import { OrderService } from '../../core/services/order.service';
import { SnackbarService } from '../../core/services/snackbar.service';
import { DeliveryMethod } from '../../shared/models/order';

@Component({
  selector: 'app-checkout',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    MatStepper,
    MatStep,
    MatFormField,
    MatLabel,
    MatInput,
    MatRadioGroup,
    MatRadioButton,
    MatButton,
    CurrencyPipe,
    RouterLink,
  ],
  templateUrl: './checkout.component.html',
})
export class CheckoutComponent implements OnInit {
  private fb = inject(FormBuilder);
  cartService = inject(CartService);
  private orderService = inject(OrderService);
  private router = inject(Router);
  private snackbar = inject(SnackbarService);

  step = signal(0);
  deliveryMethods = signal<DeliveryMethod[]>([]);

  addressForm = this.fb.group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    address1: ['', Validators.required],
    address2: [''],
    city: ['', Validators.required],
    state: ['', Validators.required],
    zipCode: ['', Validators.required],
    country: ['India', Validators.required],
  });

  deliveryForm = this.fb.group({
    deliveryMethodId: [null as number | null, Validators.required],
  });

  paymentForm = this.fb.group({
    paymentMethod: ['card', Validators.required],
  });

  ngOnInit(): void {
    this.cartService.getCart().subscribe();
    this.orderService.getDeliveryMethods().subscribe({
      next: methods => this.deliveryMethods.set(methods),
      error: () => this.deliveryMethods.set([
        { id: 1, shortName: 'Standard', deliveryTime: '3-5 days', price: 50 },
        { id: 2, shortName: 'Express', deliveryTime: '1-2 days', price: 100 },
      ]),
    });
  }

  selectedDelivery(): DeliveryMethod | undefined {
    const id = this.deliveryForm.value.deliveryMethodId;
    return this.deliveryMethods().find(m => m.id === id);
  }

  total(): number {
    return this.cartService.subtotal() + (this.selectedDelivery()?.price ?? 0);
  }

  placeOrder() {
    if (this.addressForm.invalid || this.deliveryForm.invalid) return;

    const request = {
      basketId: this.cartService.getBuyerId(),
      deliveryMethodId: this.deliveryForm.value.deliveryMethodId!,
      shippingAddress: this.addressForm.getRawValue() as {
        firstName: string;
        lastName: string;
        address1: string;
        address2?: string;
        city: string;
        state: string;
        zipCode: string;
        country: string;
      },
      paymentMethod: this.paymentForm.value.paymentMethod!,
    };

    this.orderService.createOrder(request).subscribe({
      next: order => {
        this.snackbar.success('Order placed successfully');
        this.cartService.deleteCart().subscribe();
        this.router.navigate(['/account/orders', order.id]);
      },
      error: () => this.snackbar.error('Failed to place order'),
    });
  }
}
