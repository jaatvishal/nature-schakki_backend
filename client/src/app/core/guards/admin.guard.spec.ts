import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { vi } from 'vitest';
import { SnackbarService } from '../services/snackbar.service';
import { AuthService } from '../services/auth.service';
import { adminGuard } from './admin.guard';

describe('adminGuard', () => {
  it('allows authenticated admins', () => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: { isLoggedIn: () => true, isAdmin: () => true } },
        { provide: SnackbarService, useValue: { error: vi.fn() } },
      ],
    });
    expect(TestBed.runInInjectionContext(() => adminGuard({} as never, {} as never))).toBe(true);
  });

  it('blocks a customer from direct admin URLs', () => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: { isLoggedIn: () => true, isAdmin: () => false } },
        { provide: SnackbarService, useValue: { error: vi.fn() } },
      ],
    });
    vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    expect(TestBed.runInInjectionContext(() => adminGuard({} as never, {} as never))).toBe(false);
  });
});
