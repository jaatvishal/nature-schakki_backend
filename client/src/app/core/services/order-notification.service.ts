import { Injectable, inject } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';
import { SnackbarService } from './snackbar.service';

export type OrderStatusEvent = {
  orderId: number;
  status: string;
  displayStatus: string;
  message: string;
};

@Injectable({ providedIn: 'root' })
export class OrderNotificationService {
  private auth = inject(AuthService);
  private snackbar = inject(SnackbarService);
  private hub?: signalR.HubConnection;
  private listeners = new Set<(event: OrderStatusEvent) => void>();

  initForUser(userId: number): void {
    const token = this.auth.getToken();
    if (!token) return;

    this.hub?.stop();
    const baseUrl = environment.apiUrl.replace(/\/api$/, '');
    this.hub = new signalR.HubConnectionBuilder()
      .withUrl(`${baseUrl}/hubs/order`, { accessTokenFactory: () => token })
      .withAutomaticReconnect()
      .build();

    this.hub.on('OrderStatusChanged', (event: OrderStatusEvent) => {
      this.snackbar.success(event.message || `Order #${event.orderId}: ${event.displayStatus}`);
      this.listeners.forEach(fn => fn(event));
    });

    this.hub
      .start()
      .then(() => this.hub?.invoke('JoinUserGroup', userId))
      .catch(() => {});
  }

  joinOrderGroup(orderId: number): void {
    this.hub?.invoke('JoinOrderGroup', orderId).catch(() => {});
  }

  onStatusChanged(fn: (event: OrderStatusEvent) => void): () => void {
    this.listeners.add(fn);
    return () => this.listeners.delete(fn);
  }

  disconnect(): void {
    this.hub?.stop();
    this.hub = undefined;
  }
}
