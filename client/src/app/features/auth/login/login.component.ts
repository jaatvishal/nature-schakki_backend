import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatFormField, MatLabel } from '@angular/material/form-field';
import { MatIcon } from '@angular/material/icon';
import { MatInput } from '@angular/material/input';
import { AuthService } from '../../../core/services/auth.service';
import { CartService } from '../../../core/services/cart.service';
import { SnackbarService } from '../../../core/services/snackbar.service';

@Component({
  selector: 'app-login',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, MatFormField, MatLabel, MatInput, MatButton, MatIconButton, MatIcon, RouterLink],
  templateUrl: './login.component.html',
})
export class LoginComponent {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private cartService = inject(CartService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private snackbar = inject(SnackbarService);

  hidePassword = signal(true);
  loginForm = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });

  onSubmit() {
    if (this.loginForm.invalid) return;
    this.authService.login(this.loginForm.getRawValue() as { email: string; password: string }).subscribe({
      next: () => {
        this.cartService.mergeGuestCartOnLogin().subscribe({
          next: () => {
            this.snackbar.success('Welcome back!');
            const returnUrl = this.route.snapshot.queryParams['returnUrl'];
            this.router.navigateByUrl(returnUrl || this.authService.getPostLoginRoute());
          },
        });
      },
      error: (err: HttpErrorResponse) => {
        const msg = typeof err.error === 'string' ? err.error : err.error?.message || 'Invalid email or password';
        this.snackbar.error(msg);
      },
    });
  }
}
