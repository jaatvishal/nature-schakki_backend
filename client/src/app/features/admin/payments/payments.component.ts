import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButton } from '@angular/material/button';
import { RouterLink } from '@angular/router';
import { AdminService } from '../../../core/services/admin.service';
import { AdminPayment } from '../../../shared/models/admin';

@Component({
  selector: 'app-admin-payments',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CurrencyPipe, DatePipe, FormsModule, MatButton, RouterLink],
  template: `
    <div class="max-w-7xl mx-auto py-8 px-4">
      <div class="flex justify-between mb-5"><h1 class="text-2xl font-bold">Payment History</h1><a mat-stroked-button routerLink="/admin">Dashboard</a></div>
      <div class="flex gap-2 mb-4"><input [(ngModel)]="search" (keyup.enter)="load()" placeholder="Order or customer" class="border rounded px-3 py-2 flex-1">
        <select [(ngModel)]="status" (change)="load()" class="border rounded px-3"><option value="">All statuses</option><option>Pending</option><option>Collected</option><option>Failed</option></select><button mat-flat-button (click)="load()">Search</button></div>
      <table class="w-full text-sm"><thead><tr class="border-b bg-gray-50"><th class="p-3 text-left">Order</th><th>Customer</th><th>Method</th><th>Status</th><th>Amount</th><th>Reference</th><th>Date</th></tr></thead>
        <tbody>@for (p of payments(); track p.orderId) {
          <tr class="border-b"><td class="p-3"><a [routerLink]="['/admin/orders', p.orderId]" class="text-blue-700">#{{ p.orderId }}</a></td><td>{{ p.customer }}</td>
            <td class="text-center">{{ p.method }}</td><td class="text-center">{{ p.status }}</td><td class="text-center">{{ p.amount | currency:'INR' }}</td>
            <td class="text-center">{{ p.reference || '—' }}</td><td class="text-center">{{ p.date | date:'short' }}</td></tr>
        } @empty { <tr><td colspan="7" class="p-8 text-center text-gray-500">No transactions found.</td></tr> }</tbody></table>
    </div>
  `,
})
export class AdminPaymentsComponent implements OnInit {
  private admin = inject(AdminService);
  payments = signal<AdminPayment[]>([]);
  search = '';
  status = '';
  ngOnInit() { this.load(); }
  load() { this.admin.getPayments(this.search, this.status).subscribe(x => this.payments.set(x.items)); }
}
