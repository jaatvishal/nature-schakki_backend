import { CurrencyPipe } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { MatButton } from '@angular/material/button';
import { MatChip } from '@angular/material/chips';
import { MatFormField, MatLabel } from '@angular/material/form-field';
import { MatIcon } from '@angular/material/icon';
import { MatInput } from '@angular/material/input';
import { CartService } from '../../../core/services/cart.service';
import { ShopService } from '../../../core/services/shop.service';
import { SnackbarService } from '../../../core/services/snackbar.service';
import { Product } from '../../../shared/models/product';

@Component({
  selector: 'app-product-details',
  imports: [CurrencyPipe, FormsModule, MatButton, MatIcon, MatFormField, MatInput, MatLabel, MatChip],
  templateUrl: './product-details.component.html',
  styleUrl: './product-details.component.scss',
})
export class ProductDetailsComponent implements OnInit {
  private shopService = inject(ShopService);
  private route = inject(ActivatedRoute);
  private cartService = inject(CartService);
  private snackbar = inject(SnackbarService);

  product?: Product;
  quantity = 1;

  get totalPrice(): number {
    return (this.product?.price ?? 0) * this.quantity;
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) return;
    this.shopService.getProduct(+id).subscribe({
      next: p => (this.product = p),
      error: () => this.snackbar.error('Product not found'),
    });
  }

  addToCart() {
    if (!this.product) return;
    this.cartService.addItem(this.product, this.quantity).subscribe({
      next: () => this.snackbar.success('Added to cart'),
      error: () => {},
    });
  }
}
