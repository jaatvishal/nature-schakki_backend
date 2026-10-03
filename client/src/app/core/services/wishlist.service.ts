import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Wishlist } from '../../shared/models/wishlist';

@Injectable({
  providedIn: 'root',
})
export class WishlistService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl + '/v1/wishlist';

  private wishlistSignal = signal<Wishlist | null>(null);

  wishlist = computed(() => this.wishlistSignal());
  itemCount = computed(() => this.wishlistSignal()?.items.length ?? 0);

  getWishlist() {
    return this.http.get<Wishlist>(this.baseUrl).pipe(
      tap(wishlist => this.wishlistSignal.set(wishlist))
    );
  }

  addToWishlist(productId: number) {
    return this.http.post<Wishlist>(this.baseUrl, { productId }).pipe(
      tap(wishlist => this.wishlistSignal.set(wishlist))
    );
  }

  removeFromWishlist(productId: number) {
    return this.http.delete<Wishlist>(`${this.baseUrl}/${productId}`).pipe(
      tap(wishlist => this.wishlistSignal.set(wishlist))
    );
  }

  isInWishlist(productId: number): boolean {
    return this.wishlistSignal()?.items.some(i => i.productId === productId) ?? false;
  }

  toggleWishlist(productId: number) {
    if (this.isInWishlist(productId)) {
      return this.removeFromWishlist(productId);
    }
    return this.addToWishlist(productId);
  }

  initWishlist() {
    if (!this.wishlistSignal()) {
      this.getWishlist().subscribe();
    }
  }
}
