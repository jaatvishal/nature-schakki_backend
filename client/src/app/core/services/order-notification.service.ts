import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { interval, Subscription, switchMap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';
import { SnackbarService } from './snackbar.service';

export type OrderStatusEvent = { orderId: number; status: string; displayStatus: string; message: string };

type AppNotification = { id: number; title: string; message: string; link?: string };

@Injectable({ providedIn: 'root' })
export class OrderNotificationService {
  private http = inject(HttpClient);
  private auth = inject(AuthService);
  private snackbar = inject(SnackbarService);
  private listeners = new Set<(event: OrderStatusEvent) => void>();
  private seenIds = new Set<number>();
  private sub?: Subscription;
  private initialized = false;

  initForUser(_userId: number): void {
    if (!this.auth.getToken()) return;
    this.disconnect();
    this.seenIds.clear();
    this.initialized = false;
    this.sub = interval(15000)
      .pipe(switchMap(() => this.http.get<AppNotification[]>(`${environment.apiUrl}/v1/notifications`)))
      .subscribe(list => this.handle(list));
  }

  joinOrderGroup(_orderId: number): void {}

  onStatusChanged(fn: (event: OrderStatusEvent) => void): () => void {
    this.listeners.add(fn);
    return () => this.listeners.delete(fn);
  }

  disconnect(): void {
    this.sub?.unsubscribe();
    this.sub = undefined;
  }

  private handle(list: AppNotification[]): void {
    for (const n of list) {
      if (this.seenIds.has(n.id)) continue;
      this.seenIds.add(n.id);
      if (!this.initialized) continue;
      this.snackbar.success(n.message);
      const orderId = +(n.link?.match(/orders\/(\d+)/)?.[1] ?? 0);
      if (orderId) this.listeners.forEach(fn => fn({ orderId, status: '', displayStatus: n.title, message: n.message }));
    }
    this.initialized = true;
  }
}
