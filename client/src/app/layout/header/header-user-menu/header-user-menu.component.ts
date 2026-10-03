import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { MatMenu, MatMenuItem, MatMenuTrigger } from '@angular/material/menu';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-header-user-menu',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButton, MatIconButton, MatIcon, MatMenu, MatMenuItem, MatMenuTrigger, RouterLink],
  template: `
    @if (authService.currentUser(); as user) {
      <button mat-icon-button [matMenuTriggerFor]="userMenu" class="!rounded-full !bg-amber-50" [attr.aria-label]="'Open profile menu for ' + user.firstName">
        <mat-icon>person</mat-icon>
      </button>
      <mat-menu #userMenu="matMenu">
        <a mat-menu-item routerLink="/account">Profile</a>
        <a mat-menu-item routerLink="/account/orders">My Orders</a>
        @if (authService.isAdmin()) {
          <a mat-menu-item routerLink="/admin">Admin</a>
        }
        <button mat-menu-item (click)="authService.logout()">Logout</button>
      </mat-menu>
    } @else {
      <a mat-stroked-button routerLink="/auth/login">Login</a>
      <a mat-stroked-button routerLink="/auth/register" class="hidden sm:inline-flex">Register</a>
    }
  `,
})
export class HeaderUserMenuComponent {
  authService = inject(AuthService);
}
