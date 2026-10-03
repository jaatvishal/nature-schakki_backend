import { ChangeDetectionStrategy, Component, inject, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatBadge } from '@angular/material/badge';
import { MatButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { CartService } from '../../../core/services/cart.service';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-header-cart-summary',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatIcon, MatBadge, MatButton, RouterLink],
  template: `
    @if (auth.isLoggedIn()) {
      <div class="flex items-center gap-1">
        <button mat-icon-button type="button" (click)="openDrawer.emit()"
          [matBadge]="cartService.itemCount()" [matBadgeHidden]="cartService.itemCount() === 0"
          matBadgeSize="small" aria-label="Open cart">
          <mat-icon>shopping_cart</mat-icon>
        </button>
        <a mat-stroked-button routerLink="/cart" class="hidden sm:inline-flex">Cart ({{ cartService.itemCount() }})</a>
      </div>
    }
  `,
})
export class HeaderCartSummaryComponent {
  cartService = inject(CartService);
  auth = inject(AuthService);
  openDrawer = output<void>();
}
