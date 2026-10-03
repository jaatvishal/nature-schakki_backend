import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { SnackbarService } from '../services/snackbar.service';

export const adminGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const snackbar = inject(SnackbarService);

  if (!authService.isLoggedIn()) {
    router.navigate(['/auth/login'], { queryParams: { returnUrl: router.url } });
    return false;
  }

  if (authService.isAdmin()) {
    return true;
  }

  snackbar.error('You are not authorized to access this area');
  router.navigateByUrl('/');
  return false;
};
