import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButton } from '@angular/material/button';
import { MatCard, MatCardContent } from '@angular/material/card';
import { AuthService } from '../../../core/services/auth.service';
import { AdminDashboard, OrderService } from '../../../core/services/order.service';

@Component({
  selector: 'app-dashboard',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatCard, MatCardContent, MatButton, RouterLink, CurrencyPipe, DatePipe],
  templateUrl: './dashboard.component.html',
})
export class DashboardComponent implements OnInit {
  private orderService = inject(OrderService);
  private auth = inject(AuthService);

  dashboard = signal<AdminDashboard | null>(null);
  adminName = signal('Admin');

  ngOnInit(): void {
    const user = this.auth.currentUser();
    if (user?.firstName) this.adminName.set(user.firstName);
    this.orderService.getAdminDashboard().subscribe({
      next: data => this.dashboard.set(data),
      error: () => {},
    });
  }
}
