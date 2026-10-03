import {
  ApplicationConfig,
  inject,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
  provideZoneChangeDetection,
} from '@angular/core';
import { provideRouter } from '@angular/router';
import 'zone.js';

import { routes } from './app.routes';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { DATE_PIPE_DEFAULT_OPTIONS } from '@angular/common';
import { MAT_DIALOG_DEFAULT_OPTIONS } from '@angular/material/dialog';
import { authInterceptor } from './core/interceptors/auth-interceptor';
import { errorInterceptor } from './core/interceptors/error-interceptor';
import { loadingInterceptor } from './core/interceptors/loading-interceptor';
import { AuthService } from './core/services/auth.service';
import { INDIA_DATE_PIPE_TIMEZONE } from './core/config/application-time';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideZoneChangeDetection(),
    provideAppInitializer(() => inject(AuthService).initialize()),
    provideHttpClient(withInterceptors([authInterceptor, errorInterceptor, loadingInterceptor])),
    {
      provide: DATE_PIPE_DEFAULT_OPTIONS,
      useValue: { timezone: INDIA_DATE_PIPE_TIMEZONE },
    },
    {
      provide: MAT_DIALOG_DEFAULT_OPTIONS,
      useValue: { autoFocus: 'dialog', restoreFocus: true },
    },
  ],
};
