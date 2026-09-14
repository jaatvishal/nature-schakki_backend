import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CartItem, ShoppingCart } from '../../shared/models/cart';
import { Product } from '../../shared/models/product';
import { AuthService } from './auth.service';

const BUYER_ID_KEY = 'buyerId';

@Injectable({
  providedIn: 'root',
})
export class CartService {
  private http = inject(HttpClient);
  private authService = inject(AuthService);
  private baseUrl = environment.apiUrl + '/cart';

  private cartSignal = signal<ShoppingCart | null>(null);

  cart = computed(() => this.cartSignal());
  itemCount = computed(() =>
    this.cartSignal()?.items.reduce((sum, item) => sum + item.quantity, 0) ?? 0
  );
  subtotal = computed(() =>
    this.cartSignal()?.items.reduce((sum, item) => sum + item.price * item.quantity, 0) ?? 0
  );

  getBuyerId(): string {
    const userId = this.authService.getUserId();
    if (userId) return userId.toString();

    let buyerId = localStorage.getItem(BUYER_ID_KEY);
    if (!buyerId) {
      buyerId = crypto.randomUUID();
      localStorage.setItem(BUYER_ID_KEY, buyerId);
    }
    return buyerId;
  }

  getCart() {
    const id = this.getBuyerId();
    return this.http.get<ShoppingCart>(this.baseUrl, { params: { id } }).pipe(
      tap(cart => this.cartSignal.set(cart))
    );
  }

  setCart(cart: ShoppingCart) {
    return this.http.post<ShoppingCart>(this.baseUrl, cart).pipe(
      tap(updated => this.cartSignal.set(updated))
    );
  }

  deleteCart() {
    const id = this.getBuyerId();
    return this.http.delete(this.baseUrl, { params: { id } }).pipe(
      tap(() => this.cartSignal.set({ id, items: [] }))
    );
  }

  clearLocalCart() {
    this.cartSignal.set({ id: this.getBuyerId(), items: [] });
  }

  addItem(product: Product, quantity = 1) {
    const cart = this.cartSignal() ?? { id: this.getBuyerId(), items: [] };
    const existing = cart.items.find(i => i.productId === product.id);

    const items: CartItem[] = existing
      ? cart.items.map(i =>
          i.productId === product.id ? { ...i, quantity: i.quantity + quantity } : i
        )
      : [
          ...cart.items,
          {
            productId: product.id,
            productName: product.name,
            price: product.price,
            quantity,
            pictureUrl: product.pictureUrl,
            brand: product.brand,
            type: product.type ?? '',
          },
        ];

    return this.setCart({ id: cart.id, items });
  }

  updateQuantity(productId: number, quantity: number) {
    const cart = this.cartSignal();
    if (!cart) return;

    const items =
      quantity <= 0
        ? cart.items.filter(i => i.productId !== productId)
        : cart.items.map(i => (i.productId === productId ? { ...i, quantity } : i));

    return this.setCart({ ...cart, items });
  }

  removeItem(productId: number) {
    return this.updateQuantity(productId, 0);
  }

  initCart() {
  if (!this.cartSignal()) {
    this.getCart().subscribe();
  }
  }
}
