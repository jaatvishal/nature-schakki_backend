import { Component, effect, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { HeaderComponent } from './layout/header/header.component';
import { FooterComponent } from './layout/footer/footer.component';
import { AuthService } from './core/services/auth.service';
import { OrderNotificationService } from './core/services/order-notification.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, HeaderComponent, FooterComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css',
})
export class AppComponent {
  title = 'NaturesChakki';
  private auth = inject(AuthService);
  private orderNotifications = inject(OrderNotificationService);

  constructor() {
    effect(() => {
      const user = this.auth.currentUser();
      if (user?.id) this.orderNotifications.initForUser(user.id);
      else this.orderNotifications.disconnect();
    });
  }
}
