import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButton } from '@angular/material/button';
import { MatFormField, MatLabel } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { AdminService } from '../../../core/services/order.service';
import { ShopService } from '../../../core/services/shop.service';
import { SnackbarService } from '../../../core/services/snackbar.service';
import { Pagination } from '../../../shared/models/pagination';
import { Product } from '../../../shared/models/product';
import { ShopParams } from '../../../shared/models/shopparams';

@Component({
  selector: 'app-admin-products',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CurrencyPipe, ReactiveFormsModule, MatButton, MatFormField, MatLabel, MatInput, RouterLink],
  templateUrl: './products.component.html',
})
export class AdminProductsComponent implements OnInit {
  private shopService = inject(ShopService);
  private adminService = inject(AdminService);
  private fb = inject(FormBuilder);
  private snackbar = inject(SnackbarService);

  products = signal<Pagination<Product> | null>(null);
  showForm = signal(false);
  shopParams = new ShopParams();

  productForm = this.fb.group({
    name: ['', Validators.required],
    description: ['', Validators.required],
    price: [0, [Validators.required, Validators.min(0.01)]],
    pictureUrl: ['/images/placeholder.png', Validators.required],
    type: ['', Validators.required],
    brand: ['', Validators.required],
    quantityInStock: [0, [Validators.required, Validators.min(0)]],
  });

  ngOnInit(): void {
    this.loadProducts();
  }

  loadProducts() {
    this.shopService.getProducts(this.shopParams).subscribe({ next: res => this.products.set(res) });
  }

  addProduct() {
    if (this.productForm.invalid) return;
    this.adminService.createProduct(this.productForm.getRawValue() as Partial<Product>).subscribe({
      next: () => {
        this.snackbar.success('Product added');
        this.productForm.reset({ pictureUrl: '/images/placeholder.png', price: 0, quantityInStock: 0 });
        this.showForm.set(false);
        this.loadProducts();
      },
      error: () => this.snackbar.error('Failed to add product'),
    });
  }
}
