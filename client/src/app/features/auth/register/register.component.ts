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

  registerForm = this.fb.group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
  });

  ngOnInit() {
    if (this.authService.isLoggedIn()) this.router.navigateByUrl('/shop');
  }

  onSubmit() {
    if (this.registerForm.invalid) return;
    this.authService.register(this.registerForm.getRawValue() as {
      firstName: string; lastName: string; email: string; password: string;
    }).subscribe({
      next: () => {
        this.snackbar.success('Account created');
        this.router.navigateByUrl(this.authService.getPostLoginRoute());
      },
      error: () => this.snackbar.error('Registration failed'),
    });
  }
}
