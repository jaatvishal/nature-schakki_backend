import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { NavigationExtras, Router } from '@angular/router';
import { throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { SnackbarService } from '../services/snackbar.service';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const snackbar = inject(SnackbarService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 400) {
        if (error.error?.errors) {
          const modelStateErrors: string[] = [];
          for (const key in error.error.errors) {
            if (error.error.errors[key]) {
              modelStateErrors.push(error.error.errors[key]);
            }
          }
          return throwError(() => modelStateErrors.flat());
        } else {
          snackbar.error(error.error?.title || error.error?.message || 'Bad request');
        }
      }

      if (error.status === 401 && !req.url.includes('/account/')) {
        snackbar.error('Session expired. Please log in again.');
      }

      if (error.status === 403) {
        snackbar.error('You are not authorized to perform this action');
      }

      if (error.status === 404 && !req.url.includes('/cart')) {
        router.navigateByUrl('/not-found');
      }

      if (error.status === 500) {
        const navigationExtras: NavigationExtras = {
          state: { error: { title: 'Server error', detail: 'Please try again later.' } },
        };
        router.navigateByUrl('/server-error', navigationExtras);
      }

      if (error.status === 0) {
        snackbar.error('Unable to reach the server. Please check your connection.');
      }

      return throwError(() => error);
    })
  );
};
