import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButton } from '@angular/material/button';
import { AuthService } from '../../../core/services/auth.service';
import { AccountService } from '../../../core/services/account.service';
import { User } from '../../../shared/models/user';

@Component({
  selector: 'app-profile',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButton, RouterLink],
  template: `
    <div class="max-w-2xl mx-auto py-8 px-4">
      <h1 class="text-2xl font-bold mb-6">My Profile</h1>
      @if (profile()) {
        <div class="border rounded-xl p-6 space-y-4 bg-white shadow-sm">
          <div class="flex items-center gap-4">
            <div class="w-16 h-16 rounded-full bg-amber-100 flex items-center justify-center text-2xl font-bold text-amber-700">
              {{ profile()!.firstName.charAt(0) }}
            </div>
            <div>
              <p class="text-xl font-semibold">{{ profile()!.firstName }} {{ profile()!.lastName }}</p>
              <p class="text-gray-500">{{ profile()!.email }}</p>
            </div>
          </div>
          <div class="grid sm:grid-cols-2 gap-3 text-sm border-t pt-4">
            <div><span class="text-gray-500">Email</span><p class="font-medium">{{ profile()!.email }}</p></div>
            <div><span class="text-gray-500">Role</span><p class="font-medium">{{ profile()!.roles.join(', ') }}</p></div>
            <div><span class="text-gray-500">User ID</span><p class="font-medium">#{{ profile()!.id }}</p></div>
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
  profile = signal<User | null>(null);

  ngOnInit() {
    this.account.getProfile().subscribe({
      next: p => this.profile.set(p),
      error: () => this.profile.set(this.auth.currentUser()),
    });
  }
}
