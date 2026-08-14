import { ApplicationConfig, provideBrowserGlobalErrorListeners, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import 'zone.js';

import { routes } from './app.routes';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { Dialog } from '@angular/cdk/dialog';
import { MAT_DIALOG_DEFAULT_OPTIONS } from '@angular/material/dialog';
import { errorInterceptor } from './core/interceptors/error-interceptor';
import { loadingInterceptor } from './core/interceptors/loading-interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideZoneChangeDetection(),
    provideHttpClient(withInterceptors([errorInterceptor,loadingInterceptor])), 
    {
      provide: MAT_DIALOG_DEFAULT_OPTIONS,
      useValue: {autoFocus: 'dialog',restoreFocus:true}
    }
  ]
};
