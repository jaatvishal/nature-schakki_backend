import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButton } from '@angular/material/button';
import { MatFormField, MatLabel } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { AccountService } from '../../../core/services/account.service';
import { AuthService } from '../../../core/services/auth.service';
import { SnackbarService } from '../../../core/services/snackbar.service';

@Component({
  selector: 'app-profile',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, MatFormField, MatLabel, MatInput, MatButton, RouterLink],
  templateUrl: './profile.component.html',
})
export class ProfileComponent implements OnInit {
  private fb = inject(FormBuilder);
  authService = inject(AuthService);
  private accountService = inject(AccountService);
  private snackbar = inject(SnackbarService);

  profileForm = this.fb.group({
    firstName: [''],
    lastName: [''],
    email: [{ value: '', disabled: true }],
  });

  ngOnInit(): void {
    const user = this.authService.currentUser();
    if (user) {
      this.profileForm.patchValue(user);
    }
    this.accountService.getProfile().subscribe({
      next: profile => this.profileForm.patchValue(profile),
      error: () => {},
    });
  }

  onSubmit() {
    const { firstName, lastName } = this.profileForm.getRawValue();
    this.accountService.updateProfile({ firstName: firstName ?? undefined, lastName: lastName ?? undefined }).subscribe({
      next: () => this.snackbar.success('Profile updated'),
      error: () => this.snackbar.error('Failed to update profile'),
    });
  }
}
