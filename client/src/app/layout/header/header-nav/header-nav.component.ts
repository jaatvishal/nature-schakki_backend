import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

@Component({
  selector: 'app-header-nav',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, RouterLinkActive],
  template: `
    <nav class="hidden md:flex gap-6 uppercase text-sm font-medium">
      <a routerLink="/shop" routerLinkActive="text-amber-700" class="hover:text-amber-600">Shop</a>
      <a routerLink="/contact" routerLinkActive="text-amber-700" class="hover:text-amber-600">Contact</a>
    </nav>
  `,
})
export class HeaderNavComponent {}
