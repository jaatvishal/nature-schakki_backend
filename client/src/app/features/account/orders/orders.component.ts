import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButton } from '@angular/material/button';
import { OrderService } from '../../../core/services/order.service';
import { Order } from '../../../shared/models/order';

@Component({
  selector: 'app-orders',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CurrencyPipe, DatePipe, MatButton, RouterLink],
  templateUrl: './orders.component.html',
})
export class OrdersComponent implements OnInit {
  private orderService = inject(OrderService);
  orders = signal<Order[]>([]);
  loading = signal(true);

  ngOnInit(): void {
    this.orderService.getOrders().subscribe({
      next: orders => {
        this.orders.set(orders);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }
}
