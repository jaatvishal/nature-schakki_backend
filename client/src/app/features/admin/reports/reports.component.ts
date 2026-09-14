import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButton } from '@angular/material/button';
import { RouterLink } from '@angular/router';
import { AdminService } from '../../../core/services/admin.service';
import { AdminReport } from '../../../shared/models/admin';

@Component({
  selector: 'app-admin-reports',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, MatButton, RouterLink, CurrencyPipe],
  template: `
    <div class="max-w-6xl mx-auto py-8 px-4">
      <div class="flex justify-between mb-5"><h1 class="text-2xl font-bold">Reports</h1><a mat-stroked-button routerLink="/admin">Dashboard</a></div>
      <div class="flex gap-3 items-end mb-6"><label class="text-sm">From<input type="date" [(ngModel)]="from" class="block border rounded p-2"></label>
        <label class="text-sm">To<input type="date" [(ngModel)]="to" class="block border rounded p-2"></label><button mat-flat-button (click)="load()">Apply</button></div>
      @if (report(); as r) {
        <div class="grid grid-cols-2 md:grid-cols-4 gap-3 mb-7">
          <div class="border rounded p-4"><strong class="text-2xl">{{ r.orderCount }}</strong><p class="text-gray-500">Orders</p></div>
          <div class="border rounded p-4"><strong class="text-2xl">{{ r.revenue | currency:'INR' }}</strong><p class="text-gray-500">Revenue</p></div>
          <div class="border rounded p-4"><strong class="text-2xl">{{ r.codOrders }}</strong><p class="text-gray-500">COD Orders</p></div>
          <div class="border rounded p-4"><strong class="text-2xl">{{ r.cancelledOrders }}</strong><p class="text-gray-500">Cancelled</p></div>
        </div>
        <div class="grid md:grid-cols-2 gap-6">
          <section><h2 class="font-semibold mb-3">Daily Sales</h2>@for (p of r.salesTrend; track p.label) {
            <div class="flex justify-between border-b py-2 text-sm"><span>{{ p.label }} · {{ p.count }} orders</span><strong>{{ p.value | currency:'INR' }}</strong></div>
          } @empty { <p class="text-gray-500">No completed sales.</p> }</section>
          <section><h2 class="font-semibold mb-3">Best Sellers</h2>@for (p of r.topProducts; track p.productId) {
            <div class="flex justify-between border-b py-2 text-sm"><span>{{ p.name }}</span><strong>{{ p.quantity }} · {{ p.revenue | currency:'INR' }}</strong></div>
          } @empty { <p class="text-gray-500">No sales data.</p> }</section>
        </div>
      }
    </div>
  `,
})
export class AdminReportsComponent implements OnInit {
  private admin = inject(AdminService);
  report = signal<AdminReport | null>(null);
  from = '';
  to = '';
  ngOnInit() { this.load(); }
  load() { this.admin.getReport(this.from || undefined, this.to || undefined).subscribe(x => this.report.set(x)); }
}
