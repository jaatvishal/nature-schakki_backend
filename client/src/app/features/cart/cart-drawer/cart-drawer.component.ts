import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButton } from '@angular/material/button';
import { MatDivider } from '@angular/material/divider';
import { MatIcon } from '@angular/material/icon';
import { MatSidenav } from '@angular/material/sidenav';
import { CartService } from '../../../core/services/cart.service';
import { SnackbarService } from '../../../core/services/snackbar.service';

@Component({
  selector: 'app-cart-drawer',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButton, MatIcon, CurrencyPipe, RouterLink],
  templateUrl: './cart-drawer.component.html',
  styleUrl: './cart-drawer.component.scss',
})
export class CartDrawerComponent {
  cartService = inject(CartService);
  private snackbar = inject(SnackbarService);

  opened = input(false);
  closed = output<void>();

  close() {
    this.closed.emit();
  }

  updateQuantity(productId: number, quantity: number) {
    this.cartService.updateQuantity(productId, quantity)?.subscribe({
      error: () => this.snackbar.error('Failed to update cart'),
    });
  }

  removeItem(productId: number) {
    this.cartService.removeItem(productId)?.subscribe({
      next: () => this.snackbar.success('Item removed'),
      error: () => this.snackbar.error('Failed to remove item'),
    });
  }
}
