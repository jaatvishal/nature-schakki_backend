import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { MatButton } from '@angular/material/button';
import { OrderService } from '../../../core/services/order.service';
import { Order } from '../../../shared/models/order';
import { switchMap, timer } from 'rxjs';

@Component({
  selector: 'app-orders',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CurrencyPipe, DatePipe, MatButton, RouterLink],
  templateUrl: './orders.component.html',
})
export class OrdersComponent implements OnInit {
  private orderService = inject(OrderService);
  private destroyRef = inject(DestroyRef);
  orders = signal<Order[]>([]);
  loading = signal(true);

  ngOnInit(): void {
    timer(0, 15000).pipe(
      switchMap(() => this.orderService.getOrders()),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: orders => {
        this.orders.set(orders);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }
}
