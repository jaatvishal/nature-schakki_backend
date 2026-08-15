import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButton } from '@angular/material/button';
import { MatFormField, MatLabel } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { AuthService } from '../../../core/services/auth.service';
import { SnackbarService } from '../../../core/services/snackbar.service';

@Component({
  selector: 'app-forgot-password',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, MatFormField, MatLabel, MatInput, MatButton, RouterLink],
  template: `
    <div class="max-w-md mx-auto py-8">
      <h1 class="text-2xl font-bold mb-2">Forgot Password</h1>
      <p class="text-gray-600 text-sm mb-6">Enter your email and we'll send a reset link.</p>
      <form [formGroup]="form" (ngSubmit)="submit()" class="flex flex-col gap-4">
        <mat-form-field appearance="outline">
          <mat-label>Email</mat-label>
          <input matInput type="email" formControlName="email" />
        </mat-form-field>
        <button mat-flat-button color="primary" type="submit" [disabled]="form.invalid">Send Reset Link</button>
      </form>
      <p class="mt-4 text-sm"><a routerLink="/auth/login" class="text-amber-700 hover:underline">Back to Login</a></p>
    </div>
  `,
})
export class ForgotPasswordComponent {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private snackbar = inject(SnackbarService);

  form = this.fb.group({ email: ['', [Validators.required, Validators.email]] });

  submit() {
    if (this.form.invalid) return;
    this.auth.forgotPassword(this.form.value.email!).subscribe({
      next: () => this.snackbar.success('Reset link sent if email exists'),
      error: () => this.snackbar.error('Failed to send reset link'),
    });
  }
}
