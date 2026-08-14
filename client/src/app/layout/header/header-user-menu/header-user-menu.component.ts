import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { MatButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { MatMenu, MatMenuItem, MatMenuTrigger } from '@angular/material/menu';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-header-user-menu',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButton, MatIcon, MatMenu, MatMenuItem, MatMenuTrigger, RouterLink],
  template: `
    @if (authService.isLoggedIn()) {
      <button mat-stroked-button [matMenuTriggerFor]="userMenu">
        <mat-icon>person</mat-icon>
        <span class="hidden sm:inline ml-1">{{ authService.currentUser()?.firstName }}</span>
      </button>
      <mat-menu #userMenu="matMenu">
        <a mat-menu-item routerLink="/account">Profile</a>
        <a mat-menu-item routerLink="/account/orders">Orders</a>
        <a mat-menu-item routerLink="/account/addresses">Addresses</a>
        @if (authService.isAdmin()) {
          <a mat-menu-item routerLink="/admin">Admin</a>
        }
        <button mat-menu-item (click)="logout()">Logout</button>
      </mat-menu>
    } @else {
      <a mat-stroked-button routerLink="/auth/login">Login</a>
      <a mat-stroked-button routerLink="/auth/register" class="hidden sm:inline-flex">Register</a>
    }
  `,
})
export class HeaderUserMenuComponent {
  authService = inject(AuthService);
  private router = inject(Router);

  logout() {
    this.authService.logout();
    this.router.navigateByUrl('/');
  }
}
