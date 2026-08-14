import { Routes } from '@angular/router';
import { adminGuard } from './core/guards/admin.guard';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'shop', pathMatch: 'full' },
  { path: 'shop', loadComponent: () => import('./features/shop/shop.component').then(m => m.ShopComponent) },
  {
    path: 'shop/:id',
    loadComponent: () =>
      import('./features/shop/product-details/product-details.component').then(m => m.ProductDetailsComponent),
  },
  { path: 'auth/login', loadComponent: () => import('./features/auth/login/login.component').then(m => m.LoginComponent) },
  {
    path: 'auth/register',
    loadComponent: () => import('./features/auth/register/register.component').then(m => m.RegisterComponent),
  },
  {
    path: 'auth/forgot-password',
    loadComponent: () =>
      import('./features/auth/forgot-password/forgot-password.component').then(m => m.ForgotPasswordComponent),
  },
  {
    path: 'auth/reset-password',
    loadComponent: () =>
      import('./features/auth/reset-password/reset-password.component').then(m => m.ResetPasswordComponent),
  },
  {
    path: 'cart',
    loadComponent: () => import('./features/cart/cart.component').then(m => m.CartComponent),
    canActivate: [authGuard],
  },
  {
    path: 'checkout',
    loadComponent: () => import('./features/checkout/checkout.component').then(m => m.CheckoutComponent),
    canActivate: [authGuard],
  },
  {
    path: 'contact',
    loadComponent: () => import('./features/contact/contact.component').then(m => m.ContactComponent),
  },
  {
    path: 'account',
    canActivate: [authGuard],
    children: [
      { path: '', loadComponent: () => import('./features/account/profile/profile.component').then(m => m.ProfileComponent) },
      { path: 'orders', loadComponent: () => import('./features/account/orders/orders.component').then(m => m.OrdersComponent) },
      {
        path: 'orders/:id',
        loadComponent: () =>
          import('./features/account/order-detail/order-detail.component').then(m => m.OrderDetailComponent),
      },
      { path: 'addresses', loadComponent: () => import('./features/account/addresses/addresses.component').then(m => m.AddressesComponent) },
    ],
  },
  {
    path: 'admin',
    canActivate: [adminGuard],
    children: [
      { path: '', loadComponent: () => import('./features/admin/dashboard/dashboard.component').then(m => m.DashboardComponent) },
      { path: 'products', loadComponent: () => import('./features/admin/products/products.component').then(m => m.AdminProductsComponent) },
      { path: 'orders', loadComponent: () => import('./features/admin/orders/orders.component').then(m => m.AdminOrdersComponent) },
      { path: 'payments', loadComponent: () => import('./features/admin/payments/payments.component').then(m => m.AdminPaymentsComponent) },
    ],
  },
  { path: 'not-found', loadComponent: () => import('./shared/components/not-found/not-found.component').then(m => m.NotFoundComponent) },
  { path: 'server-error', loadComponent: () => import('./shared/components/server-error/server-error.component').then(m => m.ServerErrorComponent) },
  { path: '**', redirectTo: 'shop', pathMatch: 'full' },
];
