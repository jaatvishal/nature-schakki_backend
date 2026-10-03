import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButton } from '@angular/material/button';
import { AuthService } from '../../../core/services/auth.service';
import { AccountService } from '../../../core/services/account.service';
import { OrderService } from '../../../core/services/order.service';
import { User } from '../../../shared/models/user';

@Component({
  selector: 'app-profile',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButton, RouterLink],
  template: `
    <div class="max-w-2xl mx-auto py-8 px-4">
      <h1 class="text-2xl font-bold mb-6">My Profile</h1>
      @if (profile()) {
        <div class="border border-amber-100 rounded-2xl overflow-hidden bg-white shadow-sm">
          <div class="bg-gradient-to-r from-amber-50 to-orange-50 p-6 flex items-center gap-4">
            <div class="w-20 h-20 rounded-full bg-amber-600 text-white flex items-center justify-center text-3xl font-bold shadow-md">
              {{ profile()!.firstName.charAt(0) }}
            </div>
            <div>
              <p class="text-2xl font-semibold text-gray-900">{{ profile()!.firstName }} {{ profile()!.lastName }}</p>
              <p class="text-gray-600">Customer Account</p>
            </div>
          </div>
          <div class="grid sm:grid-cols-2 gap-5 p-6 text-sm">
            <div><span class="text-gray-500">Customer Name</span><p class="font-semibold text-gray-900 mt-1">{{ profile()!.firstName }} {{ profile()!.lastName }}</p></div>
            <div><span class="text-gray-500">Email</span><p class="font-semibold text-gray-900 mt-1">{{ profile()!.email }}</p></div>
            <div><span class="text-gray-500">Phone Number</span><p class="font-semibold text-gray-900 mt-1">{{ profile()!.phoneNumber || 'Not provided' }}</p></div>
            <div><span class="text-gray-500">Address</span><p class="font-semibold text-gray-900 mt-1">{{ address() }}</p></div>
          </div>
        </div>
      } @else {
        <p class="text-gray-500">Loading profile...</p>
      }
      <div class="flex gap-2 mt-6">
        <a mat-stroked-button routerLink="/account/orders">My Orders</a>
        <a mat-stroked-button routerLink="/shop">Continue Shopping</a>
      </div>
    </div>
  `,
})
export class ProfileComponent implements OnInit {
  private auth = inject(AuthService);
  private account = inject(AccountService);
  private orders = inject(OrderService);
  profile = signal<User | null>(null);
  address = signal('Not provided');

  ngOnInit() {
    this.account.getProfile().subscribe({
      next: p => this.profile.set(p),
      error: () => this.profile.set(this.auth.currentUser()),
    });
    this.orders.getOrders().subscribe({
      next: orders => {
        const latest = orders[0]?.shipToAddress;
        if (!latest) return;
        this.address.set([
          latest.street || latest.address1,
          latest.city,
          latest.state,
          latest.zipCode,
          latest.country,
        ].filter(Boolean).join(', '));
      },
    });
  }
}
