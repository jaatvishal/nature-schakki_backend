import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, switchMap, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';
import { CartItem, ShoppingCart } from '../../shared/models/cart';
import { Product } from '../../shared/models/product';

const GUEST_ID_KEY = 'guestCartId';

@Injectable({ providedIn: 'root' })
export class CartService {
  private http = inject(HttpClient);
  private auth = inject(AuthService);
  private router = inject(Router);
  private baseUrl = environment.apiUrl + '/v1/cart';
  private cartSignal = signal<ShoppingCart | null>(null);

  cart = computed(() => this.cartSignal());
  itemCount = computed(() => this.cartSignal()?.items.reduce((s, i) => s + i.quantity, 0) ?? 0);
  subtotal = computed(() => this.cartSignal()?.items.reduce((s, i) => s + i.price * i.quantity, 0) ?? 0);

  getCartId(): string {
    const userId = this.auth.getUserId();
    if (userId) return userId.toString();
    let guestId = localStorage.getItem(GUEST_ID_KEY);
    if (!guestId) {
      guestId = crypto.randomUUID();
      localStorage.setItem(GUEST_ID_KEY, guestId);
    }
    return guestId;
  }

  getCart() {
    return this.http.get<ShoppingCart>(this.baseUrl, { params: { id: this.getCartId() } }).pipe(
      tap(c => this.cartSignal.set(c))
    );
  }

  setCart(cart: ShoppingCart) {
    return this.http.post<ShoppingCart>(this.baseUrl, cart).pipe(tap(c => this.cartSignal.set(c)));
  }

  deleteCart() {
    const id = this.getCartId();
    return this.http.delete(this.baseUrl, { params: { id } }).pipe(tap(() => this.cartSignal.set({ id, items: [] })));
  }

  clearLocalCart() {
    this.cartSignal.set({ id: this.getCartId(), items: [] });
  }

  addItem(product: Product, quantity = 1): Observable<ShoppingCart> {
    if (!this.auth.isLoggedIn()) {
      this.router.navigate(['/auth/login'], { queryParams: { returnUrl: this.router.url } });
      throw new Error('login required');
    }
    const cart = this.cartSignal() ?? { id: this.getCartId(), items: [] };
    const existing = cart.items.find(i => i.productId === product.id);
    const items: CartItem[] = existing
      ? cart.items.map(i => i.productId === product.id ? { ...i, quantity: i.quantity + quantity } : i)
      : [...cart.items, {
          productId: product.id, productName: product.name, price: product.price, quantity,
          pictureUrl: product.pictureUrl, brand: product.brand, type: product.type ?? '',
        }];
    return this.setCart({ id: this.getCartId(), items });
  }

  updateQuantity(productId: number, quantity: number) {
    const cart = this.cartSignal();
    if (!cart) return;
    const items = quantity <= 0 ? cart.items.filter(i => i.productId !== productId)
      : cart.items.map(i => i.productId === productId ? { ...i, quantity } : i);
    return this.setCart({ ...cart, id: this.getCartId(), items });
  }

  removeItem(productId: number) {
    return this.updateQuantity(productId, 0);
  }

  initCart() {
    if (this.auth.isLoggedIn()) this.getCart().subscribe();
  }

  mergeGuestCartOnLogin() {
    const guestId = localStorage.getItem(GUEST_ID_KEY);
    const userId = this.auth.getUserId()?.toString();
    if (!guestId || !userId || guestId === userId) return this.getCart();
    return this.http.get<ShoppingCart>(this.baseUrl, { params: { id: guestId } }).pipe(
      switchMap(guest => {
        if (!guest.items.length) return this.getCart();
        return this.http.get<ShoppingCart>(this.baseUrl, { params: { id: userId } }).pipe(
          switchMap(userCart => {
            const merged = [...(userCart.items ?? [])];
            guest.items.forEach(g => {
              const ex = merged.find(m => m.productId === g.productId);
              if (ex) ex.quantity += g.quantity; else merged.push(g);
            });
            return this.setCart({ id: userId, items: merged });
          })
        );
      }),
      tap(() => localStorage.removeItem(GUEST_ID_KEY))
    );
  }
}
