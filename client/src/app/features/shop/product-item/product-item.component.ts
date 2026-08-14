import { Component, inject, Input } from '@angular/core';
import { Product } from '../../../shared/models/product';
import { MatCard, MatCardContent, MatCardActions } from '@angular/material/card';
import { CurrencyPipe } from '@angular/common';
import { MatButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { RouterLink } from '@angular/router';
import { CartService } from '../../../core/services/cart.service';
import { WishlistService } from '../../../core/services/wishlist.service';
import { AuthService } from '../../../core/services/auth.service';
import { SnackbarService } from '../../../core/services/snackbar.service';

@Component({
  selector: 'app-product-item',
  imports: [MatCard, MatCardContent, CurrencyPipe, MatCardActions, MatButton, MatIcon, RouterLink],
  templateUrl: './product-item.component.html',
  styleUrl: './product-item.component.scss',
})
export class ProductItemComponent {
  @Input() product?: Product;

  private cartService = inject(CartService);
  private wishlistService = inject(WishlistService);
  private authService = inject(AuthService);
  private snackbar = inject(SnackbarService);

  addToCart(event: Event) {
    event.preventDefault();
    event.stopPropagation();
    if (!this.product) return;
    this.cartService.addItem(this.product).subscribe({
      next: () => this.snackbar.success('Added to cart'),
      error: () => this.snackbar.error('Failed to add to cart'),
    });
  }

  toggleWishlist(event: Event) {
    event.preventDefault();
    event.stopPropagation();
    if (!this.product) return;
    if (!this.authService.isLoggedIn()) {
      this.snackbar.error('Please log in to add to wishlist');
      return;
    }
    this.wishlistService.toggleWishlist(this.product.id).subscribe({
      next: () => {
        const inWishlist = this.wishlistService.isInWishlist(this.product!.id);
        this.snackbar.success(inWishlist ? 'Added to wishlist' : 'Removed from wishlist');
      },
      error: () => this.snackbar.error('Failed to update wishlist'),
    });
  }

  isInWishlist(): boolean {
    return this.product ? this.wishlistService.isInWishlist(this.product.id) : false;
  }
}
