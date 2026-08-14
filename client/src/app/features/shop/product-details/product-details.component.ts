import { CurrencyPipe } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { MatButton } from '@angular/material/button';
import { MatDivider } from '@angular/material/divider';
import { MatFormField, MatLabel } from '@angular/material/form-field';
import { MatIcon } from '@angular/material/icon';
import { MatInput } from '@angular/material/input';
import { CartService } from '../../../core/services/cart.service';
import { AuthService } from '../../../core/services/auth.service';
import { ShopService } from '../../../core/services/shop.service';
import { SnackbarService } from '../../../core/services/snackbar.service';
import { WishlistService } from '../../../core/services/wishlist.service';
import { Product } from '../../../shared/models/product';

@Component({
  selector: 'app-product-details',
  imports: [CurrencyPipe, FormsModule, MatButton, MatIcon, MatFormField, MatInput, MatLabel, MatDivider],
  templateUrl: './product-details.component.html',
  styleUrl: './product-details.component.scss',
})
export class ProductDetailsComponent implements OnInit {
  private shopService = inject(ShopService);
  private activatedRoute = inject(ActivatedRoute);
  private cartService = inject(CartService);
  private wishlistService = inject(WishlistService);
  private authService = inject(AuthService);
  private snackbar = inject(SnackbarService);

  product?: Product;
  quantity = 1;

  ngOnInit(): void {
    this.loadProduct();
  }

  loadProduct() {
    const id = this.activatedRoute.snapshot.paramMap.get('id');
    if (!id) return;
    this.shopService.getProduct(+id).subscribe({
      next: product => (this.product = product),
      error: error => console.error(error),
    });
  }

  addToCart() {
    if (!this.product) return;
    this.cartService.addItem(this.product, this.quantity).subscribe({
      next: () => this.snackbar.success('Added to cart'),
      error: () => this.snackbar.error('Failed to add to cart'),
    });
  }

  toggleWishlist() {
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
