import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { OrderService } from '../../../core/services/order.service';
import { Order } from '../../../shared/models/order';
import {
  COD_ORDER_STATUS_STEPS,
  ORDER_STATUS_STEPS,
  orderStatusLabel,
  orderStepIndex,
} from '../../../shared/constants/order-status';
import { switchMap, takeWhile, timer } from 'rxjs';

@Component({
  selector: 'app-order-detail',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CurrencyPipe, DatePipe, MatButton, MatIcon, RouterLink],
  templateUrl: './order-detail.component.html',
})
export class OrderDetailComponent implements OnInit {
  private orderService = inject(OrderService);
  private route = inject(ActivatedRoute);
  private destroyRef = inject(DestroyRef);

  order = signal<Order | null>(null);
  loading = signal(true);
  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) return;

    timer(0, 10000).pipe(
      switchMap(() => this.orderService.getOrder(+id)),
      takeWhile(order => !['Delivered', 'Cancelled', 'Failed', 'Refunded'].includes(order.status), true),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: order => { this.order.set(order); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  stepsFor(order: Order): readonly string[] {
    return order.paymentMethod.toUpperCase() === 'COD' ? COD_ORDER_STATUS_STEPS : ORDER_STATUS_STEPS;
  }

  isActive(order: Order, step: string): boolean {
    const steps = this.stepsFor(order);
    return orderStepIndex(order.status, steps) >= orderStepIndex(step, steps);
  }

  label(status: string): string {
    return orderStatusLabel(status);
  }
}
