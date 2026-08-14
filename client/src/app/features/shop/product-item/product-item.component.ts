import { Component, inject, Input } from '@angular/core';
import { Product } from '../../../shared/models/product';
import { MatCard, MatCardContent } from '@angular/material/card';
import { CurrencyPipe } from '@angular/common';
import { MatButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { RouterLink } from '@angular/router';
import { CartService } from '../../../core/services/cart.service';
import { SnackbarService } from '../../../core/services/snackbar.service';

@Component({
  selector: 'app-product-item',
  imports: [MatCard, MatCardContent, CurrencyPipe, MatButton, MatIcon, RouterLink],
  templateUrl: './product-item.component.html',
  styleUrl: './product-item.component.scss',
})
export class ProductItemComponent {
  @Input() product?: Product;
  private cartService = inject(CartService);
  private snackbar = inject(SnackbarService);

  addToCart(event: Event) {
    event.preventDefault();
    event.stopPropagation();
    if (!this.product) return;
    this.cartService.addItem(this.product).subscribe({
      next: () => this.snackbar.success('Added to cart'),
      error: () => {},
    });
  }
}
