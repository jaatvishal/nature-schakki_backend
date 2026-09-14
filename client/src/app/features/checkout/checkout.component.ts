import { CurrencyPipe, DatePipe, UpperCasePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatButton } from '@angular/material/button';
import { MatFormField, MatLabel, MatError } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { MatRadioButton, MatRadioGroup } from '@angular/material/radio';
import { MatStep, MatStepper, MatStepperNext, MatStepperPrevious } from '@angular/material/stepper';
import { AuthService } from '../../core/services/auth.service';
import { CartService } from '../../core/services/cart.service';
import { OrderService } from '../../core/services/order.service';
import { SnackbarService } from '../../core/services/snackbar.service';
import { DeliveryMethod, Order } from '../../shared/models/order';
import { ORDER_STATUS_STEPS, orderStatusLabel } from '../../shared/constants/order-status';

@Component({
  selector: 'app-checkout',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, MatStepper, MatStep, MatStepperNext, MatStepperPrevious,
    MatFormField, MatLabel, MatError, MatInput, MatRadioGroup, MatRadioButton, MatButton, CurrencyPipe, RouterLink, DatePipe, UpperCasePipe],
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
  placedOrder = signal<Order | null>(null);
  deliveryMethods = signal<DeliveryMethod[]>([]);
  statusSteps = ORDER_STATUS_STEPS;

  addressForm = this.fb.group({
    firstName: ['', [Validators.required, Validators.minLength(2)]],
    lastName: ['', [Validators.required, Validators.minLength(2)]],
    phone: ['', [Validators.required, Validators.pattern(/^[6-9]\d{9}$/)]],
    address1: ['', [Validators.required, Validators.minLength(5)]],
    landmark: [''],
    city: ['', Validators.required],
    state: ['', Validators.required],
    zipCode: ['', [Validators.required, Validators.pattern(/^\d{6}$/)]],
    country: ['India', Validators.required],
  });

  deliveryForm = this.fb.group({ deliveryMethodId: [null as number | null, Validators.required] });
  paymentForm = this.fb.group({ paymentMethod: ['COD', Validators.required] });

  ngOnInit(): void {
    const user = this.authService.currentUser();
    if (user) this.addressForm.patchValue({ firstName: user.firstName, lastName: user.lastName });
    this.cartService.getCart().subscribe({ error: () => this.router.navigateByUrl('/shop') });
    this.orderService.getDeliveryMethods().subscribe({
      next: methods => {
        this.deliveryMethods.set(methods);
        if (methods.length) this.deliveryForm.patchValue({ deliveryMethodId: methods[0].id });
      },
      error: () => {
        const fallback = { id: 1, shortName: 'Standard Delivery', deliveryTimeDays: 5, price: 0 };
        this.deliveryMethods.set([fallback]);
        this.deliveryForm.patchValue({ deliveryMethodId: fallback.id });
      },
    });
  }

  selectedDelivery() {
    return this.deliveryMethods().find(m => m.id === this.deliveryForm.value.deliveryMethodId);
  }

  total() {
    return this.cartService.subtotal() + (this.selectedDelivery()?.price ?? 0);
  }

  placeOrder() {
    if (this.addressForm.invalid || this.deliveryForm.invalid || this.placing()) return;
    this.placing.set(true);
    const a = this.addressForm.getRawValue();
    const street = a.landmark ? `${a.address1}, Near ${a.landmark}` : a.address1!;
    this.orderService.createOrder({
      deliveryMethodId: this.deliveryForm.value.deliveryMethodId!,
      paymentMethod: this.paymentForm.value.paymentMethod!,
      shippingAddress: {
        firstName: a.firstName!, lastName: a.lastName!, street,
        city: a.city!, state: a.state!, zipCode: a.zipCode!, country: a.country!,
      },
    }).subscribe({
      next: order => {
        this.placedOrder.set(order);
        this.cartService.clearLocalCart();
        this.snackbar.success('Order placed!');
        this.placing.set(false);
      },
      error: () => { this.snackbar.error('Failed to place order'); this.placing.set(false); },
    });
  }

  isStatusActive(status: string, step: string) {
    const steps = this.statusSteps as readonly string[];
    return steps.indexOf(status) >= steps.indexOf(step);
  }

  label(status: string) {
    return orderStatusLabel(status);
  }
}
