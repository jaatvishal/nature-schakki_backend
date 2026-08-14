import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { OrderService } from '../../../core/services/order.service';
import { Order } from '../../../shared/models/order';

const STEPS = ['Pending', 'PaymentReceived', 'Processing', 'Shipped', 'Delivered'];

@Component({
  selector: 'app-order-detail',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CurrencyPipe, DatePipe, MatButton, MatIcon, RouterLink],
  templateUrl: './order-detail.component.html',
})
export class OrderDetailComponent implements OnInit {
  private orderService = inject(OrderService);
  private route = inject(ActivatedRoute);
  order = signal<Order | null>(null);
  loading = signal(true);
  steps = STEPS;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) return;
    this.orderService.getOrder(+id).subscribe({
      next: order => { this.order.set(order); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  stepIndex(status: string): number {
    const idx = this.steps.indexOf(status);
    return idx >= 0 ? idx : 0;
  }

  isActive(status: string, step: string): boolean {
    return this.stepIndex(status) >= this.stepIndex(step);
  }
}
