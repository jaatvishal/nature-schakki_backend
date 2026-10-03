import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButton } from '@angular/material/button';
import { MatFormField } from '@angular/material/form-field';
import { MatSelect, MatOption } from '@angular/material/select';
import { AdminService } from '../../../core/services/admin.service';
import { AdminOrder } from '../../../shared/models/admin';
import { SnackbarService } from '../../../core/services/snackbar.service';
import { ADMIN_ORDER_STATUSES, orderStatusLabel } from '../../../shared/constants/order-status';

@Component({
  selector: 'app-admin-orders',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CurrencyPipe, DatePipe, FormsModule, MatButton, MatFormField, MatSelect, MatOption, RouterLink],
  templateUrl: './orders.component.html',
})
export class AdminOrdersComponent implements OnInit {
  private admin = inject(AdminService);
  private snackbar = inject(SnackbarService);
  orders = signal<AdminOrder[]>([]);
  statuses = ADMIN_ORDER_STATUSES;
  search = '';
  status = '';
  sort = 'newest';
  from = '';
  to = '';
  page = signal(1);
  total = signal(0);

  ngOnInit(): void {
    this.load();
  }

  load() {
    this.admin.getOrders({
      search: this.search, status: this.status, sort: this.sort,
      from: this.from, to: this.to, page: this.page(),
    }).subscribe({
      next: result => { this.orders.set(result.items); this.total.set(result.totalCount); },
      error: () => this.snackbar.error('Failed to load orders'),
    });
  }
  go(page: number) { this.page.set(page); this.load(); }

  updateStatus(order: AdminOrder, status: string) {
    this.admin.updateOrderStatus(order.id, status).subscribe({
      next: () => {
        this.snackbar.success('Status updated');
        this.load();
      },
      error: () => this.snackbar.error('Failed to update status'),
    });
  }

  label(status: string): string {
    return orderStatusLabel(status);
  }

  availableStatuses(order: AdminOrder): readonly string[] {
    const transitions: Record<string, readonly string[]> = {
      Pending: ['PaymentReceived', 'Processing', 'Failed', 'Cancelled'],
      PaymentReceived: ['Processing', 'Refunded', 'Cancelled'],
      Processing: ['Packed', 'Cancelled'],
      Packed: ['Shipped'],
      Shipped: ['OutForDelivery'],
      OutForDelivery: ['Delivered'],
    };
    return [order.status, ...(transitions[order.status] ?? [])];
  }
}
