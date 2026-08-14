import { Component, inject, OnInit, signal } from '@angular/core';
import { MatProgressBar } from '@angular/material/progress-bar';
import { BusyService } from '../../core/services/busy.service';
import { CartService } from '../../core/services/cart.service';
import { WishlistService } from '../../core/services/wishlist.service';
import { CartDrawerComponent } from '../../features/cart/cart-drawer/cart-drawer.component';
import { getEnabledHeaderItems, HeaderItemId } from './header.config';
import { HeaderCartSummaryComponent } from './header-cart-summary/header-cart-summary.component';
import { HeaderLogoComponent } from './header-logo/header-logo.component';
import { HeaderNavComponent } from './header-nav/header-nav.component';
import { HeaderSearchComponent } from './header-search/header-search.component';
import { HeaderUserMenuComponent } from './header-user-menu/header-user-menu.component';

@Component({
  selector: 'app-header',
  imports: [
    MatProgressBar,
    HeaderLogoComponent,
    HeaderNavComponent,
    HeaderSearchComponent,
    HeaderCartSummaryComponent,
    HeaderUserMenuComponent,
    CartDrawerComponent,
  ],
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss',
})
export class HeaderComponent implements OnInit {
  busyService = inject(BusyService);
  private cartService = inject(CartService);
  private wishlistService = inject(WishlistService);

  headerItems = getEnabledHeaderItems();
  cartDrawerOpen = signal(false);

  ngOnInit(): void {
    this.cartService.initCart();
    this.wishlistService.initWishlist();
  }

  isEnabled(id: HeaderItemId): boolean {
    return this.headerItems.some(item => item.id === id);
  }

  openCartDrawer() {
    this.cartDrawerOpen.set(true);
  }

  closeCartDrawer() {
    this.cartDrawerOpen.set(false);
  }
}
