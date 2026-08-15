import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { OrderService } from '../../../core/services/order.service';
import { OrderNotificationService } from '../../../core/services/order-notification.service';
import { Order } from '../../../shared/models/order';
import { ORDER_STATUS_STEPS, orderStatusLabel, orderStepIndex } from '../../../shared/constants/order-status';

@Component({
  selector: 'app-order-detail',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CurrencyPipe, DatePipe, MatButton, MatIcon, RouterLink],
  templateUrl: './order-detail.component.html',
})
export class OrderDetailComponent implements OnInit, OnDestroy {
  private orderService = inject(OrderService);
  private route = inject(ActivatedRoute);
  private orderNotifications = inject(OrderNotificationService);
  private unsubscribe?: () => void;

  order = signal<Order | null>(null);
  loading = signal(true);
  steps = ORDER_STATUS_STEPS;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) return;

    const orderId = +id;
    this.orderNotifications.joinOrderGroup(orderId);
    this.unsubscribe = this.orderNotifications.onStatusChanged(event => {
      if (event.orderId === orderId) {
        this.order.update(o => (o ? { ...o, status: event.status } : o));
      }
    });

    this.orderService.getOrder(orderId).subscribe({
      next: order => { this.order.set(order); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  ngOnDestroy(): void {
    this.unsubscribe?.();
  }

  stepIndex(status: string): number {
    return orderStepIndex(status);
  }

  isActive(status: string, step: string): boolean {
    return this.stepIndex(status) >= this.stepIndex(step);
  }

  label(status: string): string {
    return orderStatusLabel(status);
  }
}
