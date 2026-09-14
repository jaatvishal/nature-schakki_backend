import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButton } from '@angular/material/button';
import { MatCheckbox } from '@angular/material/checkbox';
import { MatFormField, MatLabel } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import { AdminService } from '../../../core/services/admin.service';
import { SnackbarService } from '../../../core/services/snackbar.service';
import { AdminCategory, AdminProduct, AdminProductInput } from '../../../shared/models/admin';

@Component({
  selector: 'app-admin-products',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CurrencyPipe, FormsModule, ReactiveFormsModule, MatButton, MatCheckbox, MatFormField, MatLabel, MatInput, RouterLink],
  templateUrl: './products.component.html',
})
export class AdminProductsComponent implements OnInit {
  private admin = inject(AdminService);
  private fb = inject(FormBuilder);
  private snackbar = inject(SnackbarService);
  products = signal<AdminProduct[]>([]);
  categories = signal<AdminCategory[]>([]);
  showForm = signal(false);
  editingId = signal<number | undefined>(undefined);
  uploading = signal(false);
  search = '';
  categoryFilter = 0;
  sort = 'name';
  statusFilter = '';
  page = signal(1);
  total = signal(0);

  productForm = this.fb.group({
    name: ['', Validators.required],
    description: ['', Validators.required],
    sku: ['', Validators.required],
    brand: ['', Validators.required],
    category: ['', Validators.required],
    price: [0, [Validators.required, Validators.min(0.01)]],
    stock: [0, [Validators.required, Validators.min(0)]],
    unit: ['kg', Validators.required],
    pictureUrl: ['', Validators.required],
    isActive: [true],
  });

  ngOnInit() {
    this.loadProducts();
    this.admin.getCategories('', 1, 100).subscribe(x => this.categories.set(x.items));
  }
  loadProducts() {
    const active = this.statusFilter === '' ? undefined : this.statusFilter === 'active';
    this.admin.getProducts(this.search, this.categoryFilter || undefined, this.page(), 20, this.sort, active)
      .subscribe(x => { this.products.set(x.items); this.total.set(x.totalCount); });
  }
  go(page: number) { this.page.set(page); this.loadProducts(); }
  edit(product: AdminProduct) {
    this.editingId.set(product.id);
    this.showForm.set(true);
    this.productForm.setValue({
      name: product.name, description: product.description, sku: product.sku, brand: product.brand,
      category: product.category, price: product.price, stock: product.stock, unit: product.unit,
      pictureUrl: product.pictureUrl, isActive: product.isActive,
    });
  }
  reset() {
    this.editingId.set(undefined);
    this.showForm.set(false);
    this.productForm.reset({ price: 0, stock: 0, unit: 'kg', pictureUrl: '', isActive: true });
  }
  save() {
    if (this.productForm.invalid) return;
    const value = this.productForm.getRawValue() as AdminProductInput;
    const request: Observable<unknown> = this.editingId()
      ? this.admin.updateProduct(this.editingId()!, value)
      : this.admin.createProduct(value);
    request.subscribe({ next: () => { this.snackbar.success('Product saved'); this.reset(); this.loadProducts(); }, error: () => this.snackbar.error('Unable to save product') });
  }
  upload(event: Event) {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    if (!['image/jpeg', 'image/png', 'image/webp'].includes(file.type) || file.size > 5 * 1024 * 1024) {
      this.snackbar.error('Choose a JPEG, PNG, or WebP image up to 5 MB');
      return;
    }
    this.uploading.set(true);
    this.admin.uploadProductImage(file).subscribe({
      next: x => { this.productForm.patchValue({ pictureUrl: x.url }); this.uploading.set(false); },
      error: () => { this.snackbar.error('Image upload failed'); this.uploading.set(false); },
    });
  }
  toggle(product: AdminProduct) {
    this.admin.setProductActive(product.id, !product.isActive).subscribe(() => this.loadProducts());
  }
  archive(product: AdminProduct) {
    this.admin.archiveProduct(product.id).subscribe(() => this.loadProducts());
  }
}
