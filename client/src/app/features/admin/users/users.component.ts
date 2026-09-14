import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButton } from '@angular/material/button';
import { RouterLink } from '@angular/router';
import { AdminService } from '../../../core/services/admin.service';
import { SnackbarService } from '../../../core/services/snackbar.service';
import { AdminOrder, AdminUser } from '../../../shared/models/admin';

@Component({
  selector: 'app-admin-users',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, MatButton, RouterLink, CurrencyPipe, DatePipe],
  template: `
    <div class="max-w-7xl mx-auto py-8 px-4">
      <div class="flex justify-between items-center mb-5"><h1 class="text-2xl font-bold">Customers</h1><a mat-stroked-button routerLink="/admin">Dashboard</a></div>
      <div class="flex gap-2 mb-4">
        <input [(ngModel)]="search" (keyup.enter)="load()" placeholder="Search name or email" class="border rounded px-3 py-2 flex-1" />
        <select [(ngModel)]="statusFilter" (change)="load()" class="border rounded px-3"><option value="">All statuses</option><option value="active">Active</option><option value="inactive">Inactive</option></select>
        <select [(ngModel)]="sort" (change)="load()" class="border rounded px-3"><option value="newest">Newest</option><option value="oldest">Oldest</option><option value="name">Name</option><option value="email">Email</option></select>
        <button mat-flat-button (click)="load()">Search</button>
      </div>
      <div class="overflow-x-auto"><table class="w-full text-sm">
        <thead><tr class="border-b bg-gray-50"><th class="p-3 text-left">Customer</th><th>Verified</th><th>Status</th><th>Registered</th><th>Last Login</th><th>Orders</th><th>Spent</th><th></th></tr></thead>
        <tbody>
          @for (u of users(); track u.id) {
            <tr class="border-b">
              <td class="p-3"><strong>{{ u.name }}</strong><br><span class="text-gray-500">{{ u.email }}</span></td>
              <td class="text-center">{{ u.emailVerified ? 'Yes' : 'No' }}</td>
              <td class="text-center">{{ u.isActive ? 'Active' : 'Inactive' }}</td>
              <td class="text-center">{{ u.createdAt | date:'shortDate' }}</td>
              <td class="text-center">{{ u.lastLoginAt ? (u.lastLoginAt | date:'short') : 'Never' }}</td>
              <td class="text-center">{{ u.totalOrders }}</td><td class="text-center">{{ u.totalSpent | currency:'INR' }}</td>
              <td class="space-x-2"><button mat-stroked-button (click)="ordersFor(u)">Orders</button><button mat-stroked-button (click)="toggle(u)">{{ u.isActive ? 'Deactivate' : 'Activate' }}</button></td>
            </tr>
          } @empty { <tr><td colspan="8" class="p-8 text-center text-gray-500">No customers found.</td></tr> }
        </tbody>
      </table></div>
      <div class="flex justify-end gap-2 mt-4"><button mat-stroked-button [disabled]="page() === 1" (click)="go(page() - 1)">Previous</button>
        <span class="py-2">Page {{ page() }}</span><button mat-stroked-button [disabled]="page() * 20 >= total()" (click)="go(page() + 1)">Next</button></div>
      @if (selectedUser()) {
        <h2 class="text-lg font-semibold mt-7">Order history · {{ selectedUser()!.name }}</h2>
        @for (o of userOrders(); track o.id) {
          <div class="flex justify-between border-b py-2"><a [routerLink]="['/admin/orders', o.id]" class="text-blue-700">Order #{{ o.id }}</a><span>{{ o.status }} · {{ o.total | currency:'INR' }}</span></div>
        } @empty { <p class="text-gray-500 mt-2">No orders.</p> }
      }
    </div>
  `,
})
export class AdminUsersComponent implements OnInit {
  private admin = inject(AdminService);
  private snackbar = inject(SnackbarService);
  users = signal<AdminUser[]>([]);
  selectedUser = signal<AdminUser | null>(null);
  userOrders = signal<AdminOrder[]>([]);
  search = '';
  sort = 'newest';
  statusFilter = '';
  page = signal(1);
  total = signal(0);

  ngOnInit() { this.load(); }
  load() {
    const active = this.statusFilter === '' ? undefined : this.statusFilter === 'active';
    this.admin.getUsers(this.search, this.page(), 20, this.sort, active).subscribe({
      next: x => { this.users.set(x.items); this.total.set(x.totalCount); },
      error: () => this.snackbar.error('Unable to load users'),
    });
  }
  go(page: number) { this.page.set(page); this.load(); }
  toggle(user: AdminUser) {
    this.admin.setUserActive(user.id, !user.isActive).subscribe({
      next: () => { this.snackbar.success('Account status updated'); this.load(); },
      error: () => this.snackbar.error('Unable to update account'),
    });
  }
  ordersFor(user: AdminUser) {
    this.selectedUser.set(user);
    this.admin.getUserOrders(user.id).subscribe(x => this.userOrders.set(x));
  }
}
