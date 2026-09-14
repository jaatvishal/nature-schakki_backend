import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatButton } from '@angular/material/button';
import { MatFormField, MatLabel } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
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
  placingOrder = signal(false);
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

  ngOnInit(): void {
    this.cartService.getCart().subscribe();
    this.orderService.getDeliveryMethods().subscribe({
      next: methods => {
        this.deliveryMethods.set(methods);
        if (methods.length)
          this.deliveryForm.patchValue({ deliveryMethodId: methods[0].id });
      },
      error: () => {
        const fallback = { id: 1, shortName: 'Standard Delivery', deliveryTimeDays: 5, price: 0 };
        this.deliveryMethods.set([fallback]);
        this.deliveryForm.patchValue({ deliveryMethodId: fallback.id });
      },
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
    if (this.addressForm.invalid || this.deliveryForm.invalid || this.placingOrder()) return;
    this.placingOrder.set(true);
    const address = this.addressForm.getRawValue();

    const request = {
      deliveryMethodId: this.deliveryForm.value.deliveryMethodId!,
      shipToAddress: {
        firstName: address.firstName!,
        lastName: address.lastName!,
        street: [address.address1, address.address2].filter(Boolean).join(', '),
        city: address.city!,
        state: address.state!,
        zipCode: address.zipCode!,
        country: address.country!,
      },
      paymentMethod: 'COD',
    };

    this.orderService.createOrder(request).subscribe({
      next: order => {
        this.snackbar.success('Order placed successfully');
        this.cartService.clearLocalCart();
        this.router.navigate(['/account/orders', order.id]);
      },
      error: () => {
        this.placingOrder.set(false);
        this.snackbar.error('Failed to place order');
      },
    });
  }
}
