import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { CartService } from '../../core/services/cart.service';
import { SnackbarService } from '../../core/services/snackbar.service';

@Component({
  selector: 'app-cart',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CurrencyPipe, MatButton, MatIcon, RouterLink],
  templateUrl: './cart.component.html',
})
export class CartComponent implements OnInit {
  cartService = inject(CartService);
  private snackbar = inject(SnackbarService);

  ngOnInit(): void {
    this.cartService.getCart().subscribe();
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

  clearCart() {
    this.cartService.deleteCart().subscribe({
      next: () => this.snackbar.success('Cart cleared'),
      error: () => this.snackbar.error('Failed to clear cart'),
    });
  }
}
