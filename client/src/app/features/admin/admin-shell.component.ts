import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-admin-shell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="min-h-screen bg-gray-50">
      <div class="bg-gray-900 text-white border-t border-gray-800">
        <div class="max-w-7xl mx-auto px-4 py-3 flex flex-col lg:flex-row lg:items-center gap-3">
          <a routerLink="/admin" class="font-bold text-lg whitespace-nowrap">Admin Portal</a>
          <nav class="flex flex-wrap gap-1 text-sm" aria-label="Admin navigation">
            @for (item of navigation; track item.link) {
              <a [routerLink]="item.link" routerLinkActive="bg-amber-600 text-white"
                [routerLinkActiveOptions]="{ exact: item.link === '/admin' }"
                class="px-3 py-2 rounded-lg text-gray-300 hover:bg-gray-800 hover:text-white transition">
                {{ item.label }}
              </a>
            }
          </nav>
        </div>
      </div>
      <router-outlet />
    </div>
  `,
})
export class AdminShellComponent {
  navigation = [
    { label: 'Dashboard', link: '/admin' },
    { label: 'Products', link: '/admin/products' },
    { label: 'Categories', link: '/admin/categories' },
    { label: 'Orders', link: '/admin/orders' },
    { label: 'Inventory', link: '/admin/inventory' },
    { label: 'Customers', link: '/admin/users' },
    { label: 'Payments', link: '/admin/payments' },
    { label: 'Reports', link: '/admin/reports' },
    { label: 'Alerts', link: '/admin/alerts' },
  ];
}
