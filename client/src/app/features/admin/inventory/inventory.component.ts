import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButton } from '@angular/material/button';
import { MatCheckbox } from '@angular/material/checkbox';
import { MatFormField, MatLabel } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { RouterLink } from '@angular/router';
import { AdminService } from '../../../core/services/admin.service';
import { SnackbarService } from '../../../core/services/snackbar.service';
import { AdminInventory, InventoryMovement } from '../../../shared/models/admin';

@Component({
  selector: 'app-admin-inventory',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, ReactiveFormsModule, MatButton, MatCheckbox, MatFormField, MatLabel, MatInput, RouterLink, DatePipe],
  template: `
    <div class="max-w-7xl mx-auto py-8 px-4">
      <div class="flex justify-between mb-5"><h1 class="text-2xl font-bold">Inventory</h1><a mat-stroked-button routerLink="/admin">Dashboard</a></div>
      <div class="flex flex-wrap gap-3 mb-4"><input [(ngModel)]="search" (keyup.enter)="load()" placeholder="Search product or SKU" class="border rounded px-3 py-2 flex-1">
        <mat-checkbox [(ngModel)]="lowStockOnly" (change)="load()">Low stock only</mat-checkbox><button mat-flat-button (click)="load()">Search</button></div>
      <table class="w-full text-sm"><thead><tr class="border-b bg-gray-50"><th class="p-3 text-left">Product</th><th>On Hand</th><th>Reserved</th><th>Available</th><th>Reorder</th><th></th></tr></thead>
        <tbody>@for (i of inventory(); track i.productId) {
          <tr class="border-b" [class.bg-red-50]="i.isLowStock"><td class="p-3"><strong>{{ i.productName }}</strong><br><span class="text-gray-500">{{ i.sku }}</span></td>
            <td class="text-center">{{ i.quantityOnHand }}</td><td class="text-center">{{ i.reservedQuantity }}</td><td class="text-center">{{ i.availableQuantity }}</td>
            <td class="text-center">{{ i.reorderLevel }}</td><td class="space-x-2"><button mat-stroked-button (click)="select(i)">Adjust</button><button mat-stroked-button (click)="history(i)">History</button></td></tr>
        }</tbody></table>

      @if (selected(); as item) {
        <form [formGroup]="form" (ngSubmit)="adjust()" class="border rounded p-4 mt-6 grid md:grid-cols-3 gap-3">
          <div><strong>Adjust {{ item.productName }}</strong><p class="text-sm text-gray-500">Use a negative number to remove stock.</p></div>
          <mat-form-field><mat-label>Quantity change</mat-label><input matInput type="number" formControlName="quantityChange"></mat-form-field>
          <mat-form-field><mat-label>Reason</mat-label><input matInput formControlName="reason"></mat-form-field>
          <button mat-flat-button type="submit" [disabled]="form.invalid">Apply Adjustment</button>
        </form>
      }

      @if (movements().length) {
        <h2 class="text-lg font-semibold mt-7 mb-3">Inventory History</h2>
        @for (m of movements(); track m.id) {
          <div class="border-b py-2 text-sm flex justify-between"><span>{{ m.reason }} · {{ m.previousQuantity }} → {{ m.newQuantity }}</span><span>{{ m.createdAt | date:'short' }}</span></div>
        }
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
  movements = signal<InventoryMovement[]>([]);
  search = '';
  lowStockOnly = false;
  form = this.fb.group({ quantityChange: [0, Validators.required], reason: ['', Validators.required] });

  ngOnInit() { this.load(); }
  load() { this.admin.getInventory(this.search, this.lowStockOnly).subscribe(x => this.inventory.set(x.items)); }
  select(item: AdminInventory) { this.selected.set(item); this.movements.set([]); }
  history(item: AdminInventory) { this.selected.set(item); this.admin.getInventoryHistory(item.productId).subscribe(x => this.movements.set(x)); }
  adjust() {
    const item = this.selected();
    if (!item || this.form.invalid) return;
    const value = this.form.getRawValue();
    this.admin.adjustInventory(item, value.quantityChange!, value.reason!).subscribe({
      next: () => { this.snackbar.success('Inventory updated'); this.form.reset({ quantityChange: 0, reason: '' }); this.load(); this.history(item); },
      error: () => this.snackbar.error('Unable to update inventory'),
    });
  }
}
