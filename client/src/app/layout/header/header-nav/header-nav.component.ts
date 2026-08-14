import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

@Component({
  selector: 'app-header-nav',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, RouterLinkActive],
  template: `
    <nav class="hidden md:flex gap-4 uppercase text-lg">
      <a routerLink="/" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }">Home</a>
      <a routerLink="/shop" routerLinkActive="active">Shop</a>
      <a routerLink="/wishlist" routerLinkActive="active">Wishlist</a>
      <a routerLink="/contact" routerLinkActive="active">Contact</a>
    </nav>
  `,
})
export class HeaderNavComponent {}
