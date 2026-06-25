import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { MainLayoutComponent } from './shared/components/layout/main-layout.component';
import { AuthLayoutComponent } from './shared/components/layout/auth-layout.component';

export const routes: Routes = [
  // ── Auth pages — no navbar ──────────────────────────────────────────────
  {
    path: 'auth',
    component: AuthLayoutComponent,
    children: [
      { path: '', loadChildren: () => import('./features/auth/auth.routes').then(m => m.authRoutes) }
    ]
  },

  // ── All other pages — main layout with navbar ───────────────────────────
  {
    path: '',
    component: MainLayoutComponent,
    children: [
      {
        path: '',
        loadComponent: () => import('./features/properties/properties.component').then(m => m.PropertiesComponent)
      },
      {
        path: 'dashboard',
        canActivate: [authGuard],
        loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent)
      },
      {
        path: 'kyc',
        canActivate: [authGuard],
        loadComponent: () => import('./features/kyc/kyc-wizard.component').then(m => m.KycWizardComponent)
      },
      {
        path: 'owner/dashboard',
        canActivate: [authGuard],
        loadComponent: () => import('./features/owner/owner-dashboard.component').then(m => m.OwnerDashboardComponent)
      },
      {
        path: 'properties/:id',
        loadComponent: () => import('./features/properties/property-detail/property-detail.component').then(m => m.PropertyDetailComponent)
      },
      {
        path: 'leases/:id/sign',
        canActivate: [authGuard],
        loadComponent: () => import('./features/leases/lease-sign/lease-sign.component').then(m => m.LeaseSignComponent)
      },
      {
        path: 'leases/:id',
        canActivate: [authGuard],
        loadComponent: () => import('./features/leases/lease-detail/lease-detail.component').then(m => m.LeaseDetailComponent)
      },
      {
        path: 'payments/success',
        loadComponent: () => import('./features/payments/payment-success/payment-success.component').then(m => m.PaymentSuccessComponent)
      },
      {
        path: 'payments/cancel',
        loadComponent: () => import('./features/payments/payment-cancel/payment-cancel.component').then(m => m.PaymentCancelComponent)
      },
      {
        path: '**',
        redirectTo: ''
      }
    ]
  }
];
