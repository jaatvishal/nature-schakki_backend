import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButton } from '@angular/material/button';
import { MatCard, MatCardContent } from '@angular/material/card';
import { OrderService } from '../../../core/services/order.service';
import { ShopService } from '../../../core/services/shop.service';
import { ShopParams } from '../../../shared/models/shopparams';

@Component({
  selector: 'app-dashboard',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatCard, MatCardContent, MatButton, RouterLink],
  templateUrl: './dashboard.component.html',
})
export class DashboardComponent implements OnInit {
  private shopService = inject(ShopService);
  private orderService = inject(OrderService);

  productCount = signal(0);
  orderCount = signal(0);

  ngOnInit(): void {
    const params = new ShopParams();
    params.pageSize = 1;
    this.shopService.getProducts(params).subscribe({
      next: res => this.productCount.set(res.count),
    });
    this.orderService.getAdminOrders().subscribe({
      next: orders => this.orderCount.set(orders.length),
      error: () => {},
    });
  }
}
