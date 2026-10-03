import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButton } from '@angular/material/button';
import { MatCheckbox } from '@angular/material/checkbox';
import { MatFormField, MatLabel } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { RouterLink } from '@angular/router';
import { AdminService } from '../../../core/services/admin.service';
import { SnackbarService } from '../../../core/services/snackbar.service';
import { AdminCategory } from '../../../shared/models/admin';

@Component({
  selector: 'app-admin-categories',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, MatButton, MatCheckbox, MatFormField, MatLabel, MatInput, RouterLink],
  template: `
    <div class="max-w-5xl mx-auto py-8 px-4">
      <div class="flex justify-between mb-5"><h1 class="text-2xl font-bold">Categories</h1><a mat-stroked-button routerLink="/admin">Dashboard</a></div>
      <form [formGroup]="form" (ngSubmit)="save()" class="grid md:grid-cols-3 gap-3 border rounded p-4 mb-6">
        <mat-form-field><mat-label>Name</mat-label><input matInput formControlName="name"></mat-form-field>
        <mat-form-field><mat-label>Description</mat-label><input matInput formControlName="description"></mat-form-field>
        <div class="flex gap-2 items-center"><mat-checkbox formControlName="isActive">Active</mat-checkbox><button mat-flat-button type="submit" [disabled]="form.invalid">Save</button>
          @if (editingId()) { <button mat-stroked-button type="button" (click)="reset()">Cancel</button> }</div>
      </form>
      <table class="w-full text-sm"><thead><tr class="border-b bg-gray-50"><th class="p-3 text-left">Name</th><th>Products</th><th>Status</th><th></th></tr></thead>
        <tbody>@for (c of categories(); track c.id) {
          <tr class="border-b"><td class="p-3"><strong>{{ c.name }}</strong><br><span class="text-gray-500">{{ c.description }}</span></td>
            <td class="text-center">{{ c.productCount }}</td><td class="text-center">{{ c.isActive ? 'Active' : 'Inactive' }}</td>
            <td><button mat-stroked-button (click)="edit(c)">Edit</button></td></tr>
        }</tbody></table>
    </div>
  `,
})
export class AdminCategoriesComponent implements OnInit {
  private admin = inject(AdminService);
  private snackbar = inject(SnackbarService);
  private fb = inject(FormBuilder);
  categories = signal<AdminCategory[]>([]);
  editingId = signal<number | undefined>(undefined);
  form = this.fb.group({ name: ['', Validators.required], description: [''], isActive: [true] });

  ngOnInit() { this.load(); }
  load() { this.admin.getCategories().subscribe(x => this.categories.set(x.items)); }
  edit(c: AdminCategory) { this.editingId.set(c.id); this.form.setValue({ name: c.name, description: c.description ?? '', isActive: c.isActive }); }
  reset() { this.editingId.set(undefined); this.form.reset({ name: '', description: '', isActive: true }); }
  save() {
    if (this.form.invalid) return;
    const value = this.form.getRawValue();
    const category = { name: value.name!, description: value.description ?? '', isActive: value.isActive ?? true };
    const observer = { next: () => { this.snackbar.success('Category saved'); this.reset(); this.load(); }, error: () => this.snackbar.error('Unable to save category') };
    const id = this.editingId();
    if (id) this.admin.saveCategory(category, id).subscribe(observer);
    else this.admin.saveCategory(category).subscribe(observer);
  }
}
