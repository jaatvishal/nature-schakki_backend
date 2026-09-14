import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { MatButton } from '@angular/material/button';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AdminService } from '../../../core/services/admin.service';
import { AdminOrder } from '../../../shared/models/admin';

@Component({
  selector: 'app-admin-order-detail',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CurrencyPipe, DatePipe, MatButton, RouterLink],
  template: `
    <div class="max-w-5xl mx-auto py-8 px-4">
      <a mat-stroked-button routerLink="/admin/orders">← Orders</a>
      @if (order(); as o) {
        <div class="flex justify-between mt-5 mb-6"><div><h1 class="text-2xl font-bold">Order #{{ o.id }}</h1><p>{{ o.customer }} · {{ o.orderDate | date:'medium' }}</p></div>
          <div class="text-right"><strong>{{ o.total | currency:'INR' }}</strong><p>{{ o.status }}</p></div></div>
        <div class="grid md:grid-cols-2 gap-6">
          <section class="border rounded p-4"><h2 class="font-semibold mb-3">Order Items</h2>@for (i of o.orderItems; track i.productId) {
            <div class="flex justify-between border-b py-2"><span>{{ i.productName }} × {{ i.quantity }}</span><strong>{{ i.price * i.quantity | currency:'INR' }}</strong></div>
          }</section>
          <section class="border rounded p-4"><h2 class="font-semibold mb-3">Payment & Totals</h2>
            <p>Method: {{ o.paymentMethod }}</p><p>Status: {{ o.paymentStatus }}</p><p>Subtotal: {{ o.subtotal | currency:'INR' }}</p>
            <p>Delivery: {{ o.deliveryCost | currency:'INR' }}</p><p>Discount: {{ o.discount | currency:'INR' }}</p></section>
        </div>
        <section class="mt-6 border rounded p-4"><h2 class="font-semibold mb-3">Status History</h2>
          @for (h of o.timeline; track h.changedAt) { <div class="border-b py-2 text-sm">{{ h.fromStatus }} → <strong>{{ h.toStatus }}</strong> by user #{{ h.changedByUserId }} · {{ h.changedAt | date:'medium' }}</div>
          } @empty { <p class="text-gray-500">No administrative status changes recorded yet.</p> }
        </section>
      }
    </div>
  `,
})
export class AdminOrderDetailComponent implements OnInit {
  private admin = inject(AdminService);
  private route = inject(ActivatedRoute);
  order = signal<AdminOrder | null>(null);
  ngOnInit() {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (id) this.admin.getOrder(id).subscribe(x => this.order.set(x));
  }
}
