import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButton } from '@angular/material/button';
import { MatFormField, MatLabel } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { RouterLink } from '@angular/router';
import { AdminService } from '../../../core/services/admin.service';
import { SnackbarService } from '../../../core/services/snackbar.service';
import { AdminInventory } from '../../../shared/models/admin';

@Component({
  selector: 'app-admin-inventory',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, ReactiveFormsModule, MatButton, MatFormField, MatLabel, MatInput, RouterLink],
  template: `
    <div class="max-w-7xl mx-auto py-8 px-4">
      <div class="flex justify-between mb-5"><div><h1 class="text-3xl font-bold">Inventory</h1><p class="text-gray-500">View and adjust current product stock.</p></div><a mat-stroked-button routerLink="/admin">Dashboard</a></div>
      <div class="flex flex-wrap gap-3 mb-4 p-3 bg-white border rounded-xl"><input [(ngModel)]="search" (keyup.enter)="load()" placeholder="Search product" class="border rounded-lg px-3 py-2 flex-1">
        <button mat-flat-button (click)="load()">Search</button></div>
      <table class="w-full text-sm bg-white border rounded-xl"><thead><tr class="border-b bg-gray-50"><th class="p-3 text-left">Product</th><th>Current Stock</th><th>Available Stock</th><th></th></tr></thead>
        <tbody>@for (i of inventory(); track i.productId) {
          <tr class="border-b" [class.bg-red-50]="i.availableQuantity <= 0"><td class="p-3"><strong>{{ i.productName }}</strong></td>
            <td class="text-center">{{ i.quantityOnHand }}</td><td class="text-center">{{ i.availableQuantity }}</td>
            <td><button mat-stroked-button (click)="select(i)">Adjust Stock</button></td></tr>
        }</tbody></table>

      @if (selected(); as item) {
        <form [formGroup]="form" (ngSubmit)="adjust()" class="border rounded p-4 mt-6 grid md:grid-cols-3 gap-3">
          <div><strong>Adjust {{ item.productName }}</strong><p class="text-sm text-gray-500">Use a negative number to remove stock.</p></div>
          <mat-form-field><mat-label>Quantity change</mat-label><input matInput type="number" formControlName="quantityChange"></mat-form-field>
          <mat-form-field><mat-label>Reason</mat-label><input matInput formControlName="reason"></mat-form-field>
          <button mat-flat-button type="submit" [disabled]="form.invalid">Apply Adjustment</button>
        </form>
      }

    </div>
  `,
})
export class AdminInventoryComponent implements OnInit {
  private admin = inject(AdminService);
  private snackbar = inject(SnackbarService);
  private fb = inject(FormBuilder);
  inventory = signal<AdminInventory[]>([]);
  selected = signal<AdminInventory | null>(null);
  search = '';
  form = this.fb.group({ quantityChange: [0, Validators.required], reason: ['', Validators.required] });

  ngOnInit() { this.load(); }
  load() { this.admin.getInventory(this.search, false).subscribe(x => this.inventory.set(x.items)); }
  select(item: AdminInventory) { this.selected.set(item); }
  adjust() {
    const item = this.selected();
    if (!item || this.form.invalid) return;
    const value = this.form.getRawValue();
    this.admin.adjustInventory(item, value.quantityChange!, value.reason!).subscribe({
      next: () => { this.snackbar.success('Inventory updated'); this.form.reset({ quantityChange: 0, reason: '' }); this.selected.set(null); this.load(); },
      error: () => this.snackbar.error('Unable to update inventory'),
    });
  }
}
