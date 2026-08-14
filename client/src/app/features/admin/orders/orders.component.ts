import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButton } from '@angular/material/button';
import { MatFormField } from '@angular/material/form-field';
import { MatSelect, MatOption } from '@angular/material/select';
import { AdminOrder, OrderService } from '../../../core/services/order.service';
import { SnackbarService } from '../../../core/services/snackbar.service';

const STATUSES = ['Pending', 'PaymentReceived', 'Processing', 'Shipped', 'Delivered', 'Cancelled', 'Refunded'];

@Component({
  selector: 'app-admin-orders',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CurrencyPipe, DatePipe, MatButton, MatFormField, MatSelect, MatOption, RouterLink],
  templateUrl: './orders.component.html',
})
export class AdminOrdersComponent implements OnInit {
  private orderService = inject(OrderService);
  private snackbar = inject(SnackbarService);
  orders = signal<AdminOrder[]>([]);
  statuses = STATUSES;

  ngOnInit(): void {
    this.load();
  }

  load() {
    this.orderService.getAdminOrders().subscribe({
      next: orders => this.orders.set(orders),
      error: () => this.snackbar.error('Failed to load orders'),
    });
  }

  updateStatus(order: AdminOrder, status: string) {
    this.orderService.updateOrderStatus(order.id, status).subscribe({
      next: () => {
        this.snackbar.success('Status updated');
        this.load();
      },
      error: () => this.snackbar.error('Failed to update status'),
    });
  }
}
