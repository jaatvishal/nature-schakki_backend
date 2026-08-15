import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButton } from '@angular/material/button';
import { MatFormField, MatLabel } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { AuthService } from '../../../core/services/auth.service';
import { SnackbarService } from '../../../core/services/snackbar.service';

@Component({
  selector: 'app-reset-password',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, MatFormField, MatLabel, MatInput, MatButton, RouterLink],
  template: `
    <div class="max-w-md mx-auto py-8">
      <h1 class="text-2xl font-bold mb-6">Reset Password</h1>
      <form [formGroup]="form" (ngSubmit)="submit()" class="flex flex-col gap-4">
        <mat-form-field appearance="outline">
          <mat-label>New Password</mat-label>
          <input matInput type="password" formControlName="password" />
        </mat-form-field>
        <button mat-flat-button color="primary" type="submit" [disabled]="form.invalid">Reset Password</button>
      </form>
      <p class="mt-4 text-sm"><a routerLink="/auth/login" class="text-amber-700 hover:underline">Back to Login</a></p>
    </div>
  `,
})
export class ResetPasswordComponent implements OnInit {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private snackbar = inject(SnackbarService);
  private email = '';
  private token = '';

  form = this.fb.group({ password: ['', [Validators.required, Validators.minLength(6)]] });

  ngOnInit() {
    this.email = this.route.snapshot.queryParams['email'] ?? '';
    this.token = this.route.snapshot.queryParams['token'] ?? '';
  }

  submit() {
    if (this.form.invalid || !this.email || !this.token) return;
    this.auth.resetPassword(this.email, this.token, this.form.value.password!).subscribe({
      next: () => {
        this.snackbar.success('Password reset successful');
        this.router.navigateByUrl('/auth/login');
      },
      error: () => this.snackbar.error('Failed to reset password'),
    });
  }
}
