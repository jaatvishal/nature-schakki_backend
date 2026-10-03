import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { AuthService } from '../../core/services/auth.service';
import { ShopService } from '../../core/services/shop.service';
import { SnackbarService } from '../../core/services/snackbar.service';
import { WishlistService } from '../../core/services/wishlist.service';
import { Product } from '../../shared/models/product';

@Component({
  selector: 'app-wishlist',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CurrencyPipe, MatButton, MatIcon, RouterLink],
  templateUrl: './wishlist.component.html',
})
export class WishlistComponent implements OnInit {
  wishlistService = inject(WishlistService);
  private shopService = inject(ShopService);
  private authService = inject(AuthService);
  private snackbar = inject(SnackbarService);

  products: Product[] = [];

  ngOnInit(): void {
    if (!this.authService.isLoggedIn()) {
      this.snackbar.error('Please log in to view your wishlist');
      return;
    }
    this.wishlistService.getWishlist().subscribe({
      next: wishlist => {
        const ids = wishlist.items.map(i => i.productId);
        if (ids.length === 0) return;
        ids.forEach(id => {
          this.shopService.getProduct(id).subscribe({
            next: product => this.products.push(product),
          });
        });
      },
    });
  }

  removeFromWishlist(productId: number) {
    this.wishlistService.removeFromWishlist(productId).subscribe({
      next: () => {
        this.products = this.products.filter(p => p.id !== productId);
        this.snackbar.success('Removed from wishlist');
      },
      error: () => this.snackbar.error('Failed to remove from wishlist'),
    });
  }
}
