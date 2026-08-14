import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButton } from '@angular/material/button';
import { AdminPayment, OrderService } from '../../../core/services/order.service';

@Component({
  selector: 'app-admin-payments',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CurrencyPipe, DatePipe, MatButton, RouterLink],
  template: `
    <div class="max-w-6xl mx-auto py-8 px-4">
      <div class="flex justify-between mb-6">
        <h1 class="text-2xl font-bold">Payments</h1>
        <a mat-stroked-button routerLink="/admin">Dashboard</a>
      </div>
      <table class="w-full text-sm border-collapse">
        <thead><tr class="border-b bg-gray-50">
          <th class="p-3 text-left">ID</th><th class="p-3 text-left">Order</th><th class="p-3 text-left">Customer</th>
          <th class="p-3 text-left">Amount</th><th class="p-3 text-left">Status</th><th class="p-3 text-left">Date</th>
        </tr></thead>
        <tbody>
          @for (p of payments(); track p.id) {
            <tr class="border-b">
              <td class="p-3">{{ p.id }}</td><td class="p-3">#{{ p.orderId }}</td><td class="p-3">{{ p.buyerEmail }}</td>
              <td class="p-3">{{ p.amount | currency: 'INR' }}</td>
              <td class="p-3"><span class="px-2 py-1 rounded text-xs"
                [class]="p.status === 'Succeeded' ? 'bg-green-100' : p.status === 'Failed' ? 'bg-red-100' : 'bg-yellow-100'">{{ p.status }}</span></td>
              <td class="p-3">{{ p.createdAt | date: 'short' }}</td>
            </tr>
          } @empty {
            <tr><td colspan="6" class="p-8 text-center text-gray-500">No payments recorded</td></tr>
          }
        </tbody>
      </table>
    </div>
  `,
})
export class AdminPaymentsComponent implements OnInit {
  private orderService = inject(OrderService);
  payments = signal<AdminPayment[]>([]);

  ngOnInit() {
    this.orderService.getAdminPayments().subscribe(p => this.payments.set(p));
  }
}
