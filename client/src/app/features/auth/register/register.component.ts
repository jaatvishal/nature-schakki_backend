import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatButton } from '@angular/material/button';
import { MatFormField, MatLabel } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { AuthService } from '../../../core/services/auth.service';
import { SnackbarService } from '../../../core/services/snackbar.service';

@Component({
  selector: 'app-register',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, MatFormField, MatLabel, MatInput, MatButton, RouterLink],
  templateUrl: './register.component.html',
})
export class RegisterComponent implements OnInit {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);
  private snackbar = inject(SnackbarService);
  awaitingVerification = signal(false);
  registeredEmail = signal('');

  registerForm = this.fb.group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
  });
  verificationForm = this.fb.group({
    otp: ['', [Validators.required, Validators.pattern(/^\d{6}$/)]],
  });

  ngOnInit() {
    if (this.authService.isLoggedIn()) this.router.navigateByUrl('/shop');
  }

  onSubmit() {
    if (this.registerForm.invalid) return;
    this.authService.register(this.registerForm.getRawValue() as {
      firstName: string; lastName: string; email: string; password: string;
    }).subscribe({
      next: result => {
        this.registeredEmail.set(result.email);
        this.awaitingVerification.set(true);
        this.snackbar.success(result.message);
      },
      error: (err: HttpErrorResponse) => {
        if (err.status === 409 && String(err.error).includes('pending')) {
          this.registeredEmail.set(this.registerForm.getRawValue().email!);
          this.awaitingVerification.set(true);
        }
        this.snackbar.error(err.error?.detail || err.error || 'Registration failed');
      },
    });
  }

  verify() {
    if (this.verificationForm.invalid) return;
    this.authService.verifyEmail(
      this.registeredEmail(),
      this.verificationForm.getRawValue().otp!
    ).subscribe({
      next: () => {
        this.snackbar.success('Email verified. Your account is active.');
        this.router.navigateByUrl(this.authService.getPostLoginRoute());
      },
      error: (err: HttpErrorResponse) =>
        this.snackbar.error(err.error?.detail || err.error || 'Verification failed'),
    });
  }

  resend() {
    this.authService.resendVerification(this.registeredEmail()).subscribe({
      next: result => this.snackbar.success(result.message),
      error: (err: HttpErrorResponse) =>
        this.snackbar.error(err.error?.detail || err.error || 'Unable to resend code'),
    });
  }
}
