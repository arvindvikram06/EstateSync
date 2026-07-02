import { Routes } from '@angular/router';
import { roleGuard } from './core/guards/role.guard';
import { staffRedirectGuard } from './core/guards/staff-redirect.guard';
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

  // ── Staff portal login — no navbar ──────────────────────────────────────
  {
    path: 'staff',
    component: AuthLayoutComponent,
    children: [
      {
        path: 'login',
        loadComponent: () => import('./features/verifier/staff-login/staff-login.component').then(m => m.StaffLoginComponent)
      },
      { path: '', redirectTo: 'login', pathMatch: 'full' }
    ]
  },

  // ── Staff dashboards — top-level, no public navbar wrapper ──────────────
  // DashboardLayoutComponent manages full-page layout with [topOffset]="0"
  {
    path: 'admin/dashboard',
    canActivate: [roleGuard('Admin')],
    loadComponent: () => import('./features/admin/admin-dashboard.component').then(m => m.AdminDashboardComponent)
  },
  {
    path: 'verifier/dashboard',
    canActivate: [roleGuard('Admin', 'Verifier')],
    loadComponent: () => import('./features/verifier/verifier-dashboard.component').then(m => m.VerifierDashboardComponent)
  },

  // ── Public + Tenant/Owner zone — main layout with navbar ────────────────
  {
    path: '',
    component: MainLayoutComponent,
    canActivate: [staffRedirectGuard],
    children: [
      {
        path: '',
        loadComponent: () => import('./features/properties/properties.component').then(m => m.PropertiesComponent)
      },
      {
        path: 'dashboard',
        canActivate: [roleGuard('Tenant', 'Owner')],
        loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent)
      },
      {
        path: 'kyc',
        canActivate: [roleGuard('Tenant', 'Owner')],
        loadComponent: () => import('./features/kyc/kyc-wizard.component').then(m => m.KycWizardComponent)
      },
      {
        path: 'owner/dashboard',
        canActivate: [roleGuard('Owner')],
        loadComponent: () => import('./features/owner/owner-dashboard.component').then(m => m.OwnerDashboardComponent)
      },
      {
        path: 'properties/create',
        canActivate: [roleGuard('Owner')],
        loadComponent: () => import('./features/properties/property-create/property-create.component').then(m => m.PropertyCreateComponent)
      },
      {
        path: 'properties/:id/edit',
        canActivate: [roleGuard('Owner')],
        loadComponent: () => import('./features/properties/property-edit/property-edit.component').then(m => m.PropertyEditComponent)
      },
      {
        path: 'properties/:id',
        loadComponent: () => import('./features/properties/property-detail/property-detail.component').then(m => m.PropertyDetailComponent)
      },
      {
        path: 'leases/:id/sign',
        canActivate: [roleGuard('Tenant', 'Owner')],
        loadComponent: () => import('./features/leases/lease-sign/lease-sign.component').then(m => m.LeaseSignComponent)
      },
      {
        path: 'leases/:id',
        canActivate: [roleGuard('Tenant', 'Owner')],
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
        path: 'unauthorized',
        loadComponent: () => import('./features/unauthorized/unauthorized.component').then(m => m.UnauthorizedComponent)
      },
      {
        path: '**',
        redirectTo: ''
      }
    ]
  }
];
