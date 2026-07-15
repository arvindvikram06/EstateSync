# EstateSync — Frontend Engineering Guide

**Stack:** Angular 21 · Angular Material v21 · Tailwind CSS v3 · NgRx Signal Store · SignalR  
**Project root:** `frontend/estatesync-ui/`  
**Dev server:** `http://localhost:4200`  
**API target:** `http://localhost:5000` (set in `environment.ts`)

> When in doubt, follow **LeaseDetailComponent** or **OwnerApplicationsTabComponent** — they are the most complete reference implementations.

---

## Table of Contents

1. [Architecture Rules](#1-architecture-rules)
2. [Project Layout](#2-project-layout)
3. [Auth System](#3-auth-system)
4. [Routing & Guards](#4-routing--guards)
5. [State Management](#5-state-management)
6. [HTTP & API Calls](#6-http--api-calls)
7. [TypeScript Models & Enums](#7-typescript-models--enums)
8. [UI Design System](#8-ui-design-system)
9. [Shared Components Reference](#9-shared-components-reference)
10. [Forms](#10-forms)
11. [Real-Time Notifications (SignalR)](#11-real-time-notifications-signalr)
12. [Adding a New Feature — Checklist](#12-adding-a-new-feature--checklist)
13. [Known Patterns & Gotchas](#13-known-patterns--gotchas)
14. [Running the Project](#14-running-the-project)

---

## 1. Architecture Rules

### Components

- **Standalone only** — no `NgModule`. Every component declares `standalone: true`.
- **`ChangeDetectionStrategy.OnPush` on every component** — no exceptions.
- **`inject()` function** for all dependencies — never constructor injection.
- **Lazy-loaded** feature routes via `loadComponent()` / `loadChildren()` under `app/features/`.

### Reactivity

- **Signals-first for local UI state:** `signal()`, `computed()`, `toSignal()`.
- **Do not use** `BehaviorSubject` for UI state.
- **`NgRx Signal Store`** only for complex multi-step flows (e.g., `KycStore`). Simple pages use signals + service calls.
- **Subscriptions:** prefer `toSignal()` where possible. When you must subscribe manually, clean up with `takeUntilDestroyed(destroyRef)`.

### Critical: Two Reactivity Systems — Never Mix Them

Angular has two completely separate reactivity systems. `computed()` only tracks signal reads — it cannot track RxJS emissions from `FormControl.valueChanges`.

```typescript
// WRONG — computed() never re-evaluates when the form changes:
readonly selectedDate = computed(() => this.form.controls.exitDate.value);

// CORRECT — bridges RxJS FormControl to the signal world:
readonly selectedDate = toSignal(
  this.form.controls.exitDate.valueChanges,
  { initialValue: null as Date | null }
);
```

Use `toSignal()` any time you need a computed signal that depends on a reactive form value.

### Guards & Interceptors

- **Functional only** — no class-based `CanActivate` or `HttpInterceptor`.
- Role-restricted routes use `roleGuard('Owner', 'Admin')` — this is the auth guard; there is no separate `authGuard`.
- `staffRedirectGuard` — redirects Admin/Verifier users away from public routes to their dashboards.

---

## 2. Project Layout

```
frontend/estatesync-ui/src/app/
│
├── core/
│   ├── guards/
│   │   ├── role.guard.ts           # roleGuard(roles...) — functional CanActivate
│   │   ├── staff-redirect.guard.ts # Redirects staff away from public pages
│   │   └── role-home.util.ts       # Maps role → home route
│   │
│   ├── interceptors/
│   │   ├── auth.interceptor.ts     # Adds Authorization: Bearer header
│   │   ├── error.interceptor.ts    # 401→refresh, 403→/unauthorized, others→snackbar
│   │   └── http-context.tokens.ts  # SKIP_AUTH_HANDLING context token
│   │
│   ├── models/
│   │   ├── api-response.model.ts   # ApiResponse<T>, PagedResponse<T>
│   │   ├── auth.model.ts           # AuthResponse, LoginRequest, RegisterRequest, UserProfileResponse
│   │   ├── enums.ts                # All frontend enums (must mirror backend exactly)
│   │   ├── property.model.ts
│   │   ├── kyc.model.ts
│   │   ├── lease.model.ts
│   │   ├── lease-bill.model.ts
│   │   ├── rental-application.model.ts
│   │   ├── complaint.model.ts
│   │   ├── notification.model.ts
│   │   └── admin.model.ts
│   │
│   ├── services/
│   │   ├── auth/
│   │   │   ├── auth.service.ts     # currentUser signal, login, logout, bootstrap, syncProfile
│   │   │   └── token.service.ts    # localStorage token, decodeUser(), isAuthenticated()
│   │   ├── admin/
│   │   │   ├── admin.service.ts
│   │   │   └── user.service.ts     # getProfile() — KYC/owner state
│   │   ├── leasing/
│   │   │   ├── lease.service.ts
│   │   │   └── rental-application.service.ts
│   │   ├── property/
│   │   │   └── property.service.ts
│   │   ├── verification/
│   │   │   └── kyc.service.ts
│   │   ├── complaint/
│   │   │   └── complaint.service.ts
│   │   ├── payment/
│   │   │   └── payment.service.ts
│   │   └── shared/
│   │       ├── error-handler.service.ts  # Shows snackbars for API errors
│   │       ├── notification.service.ts   # In-app notification state
│   │       ├── signalr.service.ts        # SignalR hub connection
│   │       ├── geocoding.service.ts
│   │       └── location.service.ts
│   │
│   ├── stores/
│   │   └── kyc.store.ts            # NgRx Signal Store for KYC wizard multi-step state
│   │
│   └── utils/
│       └── format.utils.ts         # formatDate(), formatCurrency()
│
├── features/
│   ├── auth/                       # login, register, verify-email
│   ├── dashboard/                  # Tenant dashboard (tabs: overview, leases, applications, bills, complaints, verifications)
│   ├── owner/                      # Owner dashboard (tabs: overview, properties, applications, leases, complaints, earnings)
│   ├── admin/                      # Admin dashboard (tabs: overview, kyc, properties, leases, complaints, verifiers, audit-logs, financial)
│   ├── verifier/                   # Verifier dashboard
│   ├── kyc/                        # KYC wizard (identity only — financial profile removed)
│   ├── leases/
│   │   ├── lease-detail/           # Full lease detail page with bills, payments, vacate notice, e-sign status
│   │   └── lease-sign/             # Signature pad page
│   ├── properties/                 # Public listing, detail, create wizard, edit
│   ├── payments/                   # payment-success, payment-cancel redirect pages
│   ├── profile/                    # User profile page
│   └── unauthorized/
│
├── shared/
│   ├── components/
│   │   ├── alert-banner/           # Error/warning/success/info banners
│   │   ├── complaint-detail-dialog/
│   │   ├── confirm-dialog/         # Generic yes/no confirmation dialog
│   │   ├── dashboard-layout/       # Sidebar shell for all dashboards
│   │   ├── empty-state/            # Icon + title + subtitle for empty lists
│   │   ├── layout/
│   │   │   ├── auth-layout.component.ts    # Clean layout, no navbar
│   │   │   └── main-layout.component.ts    # Public layout with Navbar
│   │   ├── loading-button/         # mat-button + spinner overlay
│   │   ├── navbar/                 # Top navigation bar
│   │   ├── notification-panel/     # Bell icon + notification list overlay
│   │   ├── page-spinner/           # Full-page loading overlay
│   │   ├── skeleton-loader/        # Animated skeleton for loading states
│   │   └── status-badge/           # Pill badge using .es-badge--* classes
│   └── validators/
│       └── password-match.validator.ts
│
└── layout/                         # (deprecated path — layouts moved to shared/components/layout/)
```

---

## 3. Auth System

### Token Storage (Security-Critical)

| Storage | What lives there |
|---|---|
| `localStorage` key `es_access_token` | JWT access token — survives page reload |
| `httpOnly` cookie | Refresh token — set by server, never accessible from JS |

**Nothing else is ever stored in localStorage or sessionStorage.** No user objects, no roles, no PII.

### TokenService (`core/services/auth/token.service.ts`)

```typescript
// Read the access token
tokenService.getToken(): string | null

// Decode ALL user info directly from the JWT — no separate storage needed
tokenService.decodeUser(): { userId, email, fullName, roles } | null

// Check expiry
tokenService.isAuthenticated(): boolean

// Role check
tokenService.hasRole('Owner'): boolean
```

Roles and user identity are always decoded from the JWT using `jwtDecode`. Never trust roles from a response body or from any storage other than the JWT itself.

JWT claims (set by backend):
- `sub` → `userId`
- `email` → `email`
- `name` → `fullName`
- `http://schemas.microsoft.com/ws/2008/06/identity/claims/role` or `role` → `roles[]`

### AuthService (`core/services/auth/auth.service.ts`)

```typescript
// Signals (read these in templates and computed)
authService.currentUser()           // AuthResponse | null
authService.isLoggedIn()            // computed
authService.isOwner()               // computed
authService.isAdmin()               // computed
authService.isKycVerified()         // from UserProfileResponse
authService.isOwnerVerified()       // from UserProfileResponse

// Methods
authService.login(request)
authService.logout()
authService.syncProfile()           // Re-fetches KYC/owner verification state — call after approval events
authService.refreshToken()          // Used by error interceptor on 401
```

### Bootstrap (app startup)

`provideAppInitializer(() => inject(AuthService).bootstrap())` in `app.config.ts` runs before first render.

**Fast path (valid token in localStorage):** restores `currentUser` signal instantly from the JWT — zero network calls.

**Fallback (expired/missing token):** hits `/auth/refresh-token` via the httpOnly cookie. If that also fails, clears state and the user stays logged out.

### Interceptors

**`auth.interceptor.ts`** — reads the token from `TokenService.getToken()` and injects `Authorization: Bearer <token>` header on every request.

**`error.interceptor.ts`:**
- `401` → calls `authService.refreshToken()`, replays the original request. On repeat 401 → clears state, navigates to `/auth/login`.
- `403` → navigates to `/unauthorized`.
- Other errors → shows snackbar via `ErrorHandlerService`.
- Use `SKIP_AUTH_HANDLING` context token on auth/refresh requests to prevent infinite loops:

```typescript
import { SKIP_AUTH_HANDLING } from '@core/interceptors/http-context.tokens';
this.http.post(url, body, {
  context: new HttpContext().set(SKIP_AUTH_HANDLING, true)
});
```

---

## 4. Routing & Guards

### Route structure

```
/                           → PropertiesComponent (public, MainLayout)
/auth/login                 → LoginComponent (AuthLayout)
/auth/register              → RegisterComponent (AuthLayout)
/auth/verify-email          → VerifyEmailComponent (AuthLayout)
/staff/login                → StaffLoginComponent (AuthLayout)

/dashboard                  → DashboardComponent [Tenant, Owner] (MainLayout)
/owner/dashboard            → OwnerDashboardComponent [Owner] (MainLayout)
/profile                    → ProfileComponent [any auth] (MainLayout)
/kyc                        → KycWizardComponent [Tenant, Owner] (MainLayout)

/properties/create          → PropertyCreateComponent [Owner] (standalone, no navbar)
/properties/:id             → PropertyDetailComponent (public, MainLayout)
/properties/:id/edit        → PropertyEditComponent [Owner] (MainLayout)

/leases/:id                 → LeaseDetailComponent [Tenant, Owner, Admin] (MainLayout)
/leases/:id/sign            → LeaseSignComponent [Tenant, Owner] (MainLayout)

/admin/dashboard            → AdminDashboardComponent [Admin] (standalone, no navbar)
/admin/leases/:id           → LeaseDetailComponent [Admin] (standalone, no navbar)
/verifier/dashboard         → VerifierDashboardComponent [Admin, Verifier] (standalone)

/payments/success           → PaymentSuccessComponent (public, MainLayout)
/payments/cancel            → PaymentCancelComponent (public, MainLayout)
/unauthorized               → UnauthorizedComponent (MainLayout)
```

### roleGuard

```typescript
// Allows any of the listed roles. Redirects to /auth/login if not logged in,
// /unauthorized if logged in but wrong role.
canActivate: [roleGuard('Owner')]
canActivate: [roleGuard('Admin', 'Verifier')]
```

### staffRedirectGuard

Applied to the public `MainLayout` route. If the logged-in user is Admin or Verifier, redirects them to their dashboard so they never see the public property listing.

### DashboardLayout

All staff dashboards use `DashboardLayoutComponent` with a custom sidebar — **not** `mat-sidenav`. The layout exposes a `[topOffset]` input:
- `0` for standalone dashboards (no public navbar above)
- `64` for dashboards nested under MainLayout

---

## 5. State Management

### Local state (most components)

```typescript
readonly items   = signal<Item[]>([]);
readonly loading = signal(true);
readonly error   = signal<string | null>(null);

ngOnInit(): void {
  this.svc.getItems().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
    next: res => { this.items.set(res.data); this.loading.set(false); },
    error: ()  => { this.loading.set(false); },
  });
}
```

### Derived state

```typescript
readonly isEmpty = computed(() => !this.loading() && this.items().length === 0);
readonly hasError = computed(() => this.error() !== null);
```

### Form value as signal (required pattern for datepickers and controlled inputs)

```typescript
// FormControl values are RxJS — bridge to signals with toSignal()
readonly selectedDate = toSignal(
  this.form.controls.exitDate.valueChanges,
  { initialValue: null as Date | null }
);
```

### NgRx Signal Store (KycStore)

Used only for the KYC multi-step wizard. If you need to share state across multiple components in a complex flow (more than 3 steps with shared data), follow the `kyc.store.ts` pattern. For everything else, use plain signals.

---

## 6. HTTP & API Calls

### Base URL

Always use `environment.apiUrl`:
```typescript
private readonly baseUrl = `${environment.apiUrl}/api/v1/feature`;
```

### Typed requests

Every HTTP call is typed against `ApiResponse<T>` or `PagedResponse<T>`:

```typescript
getItems(): Observable<PagedResponse<ItemResponse>> {
  return this.http.get<PagedResponse<ItemResponse>>(`${this.baseUrl}`);
}

createItem(req: CreateItemRequest): Observable<ApiResponse<ItemResponse>> {
  return this.http.post<ApiResponse<ItemResponse>>(`${this.baseUrl}`, req);
}
```

### Backend route casing

The backend uses PascalCase controller names. Use these exact paths:
- `/api/v1/RentalApplications` (not `/rental-applications`)
- All others follow standard kebab/lowercase routing

### File uploads

```typescript
const formData = new FormData();
formData.append('file', file);
formData.append('documentType', DocumentType.AadhaarFront.toString());
this.http.put<ApiResponse<DocumentResponse>>(url, formData);
// Do NOT set Content-Type manually — let the browser set multipart boundary
```

### Stripe redirects

After receiving a checkout URL from the API, redirect with `window.location.href = url`.

---

## 7. TypeScript Models & Enums

### Response wrappers (`core/models/api-response.model.ts`)

```typescript
interface ApiResponse<T>   { success: boolean; message: string; data: T; }
interface PagedResponse<T> { success: boolean; data: T[]; page: number; limit: number; total: number; }
```

### Enum rules

Enums live in `core/models/enums.ts` and **must mirror backend integer values exactly**. Backend enums are append-only — never reorder frontend enums either.

```typescript
export enum LeaseStatus {
  PendingApproval = 0, Active = 1, Expired = 2, Terminated = 3,
  PendingAdminApproval = 4, PartialSigned = 5, Voided = 6
}
export enum PropertyStatus {
  Draft = 0, PendingApproval = 1, Active = 2, Rejected = 3, Rented = 4, Leased = 5, AvailableSoon = 6
}
export enum ApplicationStatus { Pending = 0, Approved = 1, Rejected = 2, Withdrawn = 3 }
export enum ApprovalStatus    { Draft = 0, Pending = 1, Approved = 2, Rejected = 3 }
export enum PaymentFrequency  { Monthly = 0, Quarterly = 1, HalfYearly = 2, Yearly = 3 }
export enum PaymentStatus     { Pending = 0, Paid = 1, Failed = 2 }
export enum LeaseBillType     { Rent = 0, Deposit = 1, Penalty = 2, Maintenance = 3, Other = 4 }
// PropertyType uses string values: 'IndependentHouse' | 'Villa'
```

### Model field names

TypeScript models must use **exactly the same field names** as the backend DTOs. Do not camelCase backend PascalCase fields — `HttpClient` deserializes JSON correctly without mapping.

---

## 8. UI Design System

### Stack & Responsibility Split

| Tool | Job | Never use it for |
|---|---|---|
| **Angular Material v21** | Interactive widgets: buttons, inputs, dialogs, menus, chips, spinners, datepicker, paginator | Layout or positioning |
| **Tailwind CSS v3** (`preflight: false`) | Layout, spacing, grid, flexbox, responsive | Colors (use tokens) or interactive components |
| **`styles.css` + `material-theme.scss`** | All color tokens and shared CSS classes | — |

Do not install other UI libraries (Bootstrap, PrimeNG, NG-Zorro, etc.).

### Color Tokens — The Only Rule That Matters

**Hex color codes may appear in exactly two files:**  
`styles.css` (defines `--es-*` tokens) and `material-theme.scss` (defines `--mat-sys-*` tokens).

Everywhere else: use `var(--es-*)`, `var(--mat-sys-*)`, or Tailwind token classes. `npm run lint:css` (stylelint) fails the build on any raw hex in other files.

**Exception:** color values consumed by JavaScript (ApexCharts `[colors]`, canvas gradients) are data, not styling — hex allowed there.

**Inline `style="..."` attributes are banned in templates.** For conditional colors, use `[class]` toggles:

```html
<!-- WRONG -->
<span [style.color]="isOverdue ? '#DC2626' : '#222'">...</span>

<!-- CORRECT -->
<span [class]="isOverdue ? 'es-text-danger' : ''">...</span>
```

### Key Tokens

| Token | Value | Use |
|---|---|---|
| `--mat-sys-primary` / `--es-brand` | `#FF385C` | Brand coral — buttons, active states, accents |
| `--es-bg` | `#F7F7F7` | Page background |
| `--es-text` | `#222` | Primary text |
| `--es-text-secondary` | `#717171` | Muted/secondary text |
| `--es-border` | `#DDD` | Input borders, stronger dividers |
| `--es-border-light` | `#EBEBEB` | Card borders, light dividers |
| `--es-success` / `--es-danger` / `--es-warning` | green / red / amber | Status text & icons |

### Decision Ladder (stop at first match)

1. **Interactive widget** (button, input, dialog, menu, spinner) → Angular Material component.
2. **Layout / spacing / grid / one-off sizing** → Tailwind utility in the template.
3. **Any color, or a pattern used 3+ times** → check the shared-class vocabulary below first, then add to `styles.css` if not there.
4. **Structure unique to one component** → that component's `.css` file, colors via `var(--es-*)`.
5. **Never** `style="..."`, **never** hex outside the two token files.

### Shared-Class Vocabulary (use before writing new CSS)

| Class | Use when |
|---|---|
| `.es-card` | White card — border, 16px radius, soft shadow |
| `.es-badge--approved/pending/rejected/partial/draft` | Status pill colors |
| `.es-text-sm` / `.es-text-xs` / `.es-text-strong` | Small / muted / bold text |
| `.es-text-success` / `.es-text-danger` / `.es-text-warn-deep` | Colored status text (use as `[class]` toggles) |
| `.es-info-banner--success/error/warning/info` | Full-width message banners |
| `.kyc-banner--*` | Verification status banners on dashboards |
| `.stat-grid` + `.stat-card` + `.stat-icon--*` | Dashboard KPI tiles |
| `.filter-pill` / `.option-pill` | Pill filter/selector buttons |
| `.admin-table` | Data tables. **Always** wrap in `.admin-table-wrap` or `overflow-x-auto` — has `min-width: 560px`. |
| `.table-scroll` | Scrollable table body inside a card |
| `.dash-shell`, `.dash-sidebar`, `.dash-main` | Dashboard shell layout |
| `.dash-nav-item`, `.dash-nav-item--active` | Sidebar navigation items |

### Buttons

- **Primary CTA:** `mat-flat-button color="primary"` — pill shape: `class="!rounded-full !h-12 !font-semibold"`
- **Secondary / Back:** `mat-stroked-button`
- **Destructive:** `style` class with token `var(--es-danger)` border — never `mat-raised-button`
- **Never** `mat-raised-button` for new features (shadow too heavy for the design)

### Form Fields

```html
<mat-form-field appearance="outline" class="w-full">
  <mat-label>Label</mat-label>
  <mat-icon matPrefix>icon_name</mat-icon>
  <input matInput formControlName="field" />
  @if (form.controls.field.invalid && form.controls.field.touched) {
    <mat-error>Validation message</mat-error>
  }
</mat-form-field>
```

- Always `appearance="outline"`.
- Always `matPrefix` icon.
- Show errors only when `touched`.

### Status Badges

Use `<app-status-badge [status]="lease.status" />` — never write custom badge styles. The component maps status strings to `.es-badge--*` classes.

### Datepicker (Standalone Components)

```typescript
// In @Component decorator:
providers: [provideNativeDateAdapter()]  // NOT MatNativeDateModule in imports

// In imports array:
imports: [MatDatepickerModule, ...]
```

`MatNativeDateModule` in `imports` does not reliably register the date adapter in standalone components on Angular 17+. Always use `provideNativeDateAdapter()` in `providers`.

### KYC / Multi-Step Wizards

- **Do not use `mat-stepper`** — its headers allow users to skip steps by clicking.
- Use the custom `.kyc-steps` indicator with programmatic navigation only.
- Users may go back, never skip forward.

---

## 9. Shared Components Reference

### `LoadingButtonComponent`

```html
<app-loading-button
  [loading]="submitting()"
  [disabled]="form.invalid"
  label="Submit"
  (clicked)="submit()"
/>
```

Use for every button that triggers an async action. Never show a raw spinner next to a button.

### `EmptyStateComponent`

```html
@if (items().length === 0 && !loading()) {
  <app-empty-state icon="inbox" title="No items yet" subtitle="They'll appear here." />
}
```

### `AlertBannerComponent`

```html
@if (error()) {
  <app-alert-banner type="error" [message]="error()!" />
}
```

Types: `error` | `warning` | `success` | `info`.

### `PageSpinnerComponent`

```html
@if (loading()) {
  <app-page-spinner />
}
```

Full-page overlay. Use on initial data load, not on subsequent actions.

### `SkeletonLoaderComponent`

```html
@if (loading()) {
  <app-skeleton-loader [rows]="4" />
}
```

Use instead of `PageSpinner` when you know the approximate shape of the content.

### `ConfirmDialogComponent`

```typescript
this.dialog.open(ConfirmDialogComponent, {
  data: { title: 'Delete item?', message: 'This cannot be undone.', confirmLabel: 'Delete' }
}).afterClosed().subscribe(confirmed => { if (confirmed) this.delete(); });
```

### `DashboardLayoutComponent`

```html
<app-dashboard-layout [navItems]="navItems" [topOffset]="0">
  <router-outlet />
</app-dashboard-layout>
```

`topOffset="0"` for standalone staff dashboards. `topOffset="64"` if the main 64px navbar is above.

---

## 10. Forms

```typescript
readonly form = this.fb.nonNullable.group({
  email:    ['', [Validators.required, Validators.email]],
  password: ['', [Validators.required, Validators.minLength(8)]],
});

submit(): void {
  if (this.form.invalid) { this.form.markAllAsTouched(); return; }
  const value = this.form.getRawValue(); // getRawValue() includes disabled controls
  // ...
}
```

- Always `fb.nonNullable.group(...)` — prevents null surprises.
- Use `getRawValue()` not `this.form.value` — includes disabled controls.
- Mirror validator rules to the backend DTO constraints exactly.
- Validators only check format/shape. Business-rule checks (e.g., date must be in the future by N days) belong in the submit handler or the service.

---

## 11. Real-Time Notifications (SignalR)

`SignalRService` (`core/services/shared/signalr.service.ts`) manages the hub connection. It is started after login in `AuthService`.

The backend sends notifications to a user-specific group. The service exposes an observable that the `NotificationPanelComponent` subscribes to. When a notification arrives, the panel updates without a page refresh.

**You do not need to touch SignalR** when adding new features. Just ensure the backend sends a `Notification` row via `INotificationSender` — the SignalR push is automatic.

---

## 12. Adding a New Feature — Checklist

```
Model
  [ ] Add interface/type in core/models/{feature}.model.ts
  [ ] Add any new enum values to enums.ts (append only, mirror backend integers)

Service
  [ ] Create core/services/{domain}/{feature}.service.ts
  [ ] All methods return Observable<ApiResponse<T>> or Observable<PagedResponse<T>>
  [ ] Use environment.apiUrl as base URL
  [ ] Do not hardcode any URL strings in components

Component
  [ ] standalone: true
  [ ] changeDetection: ChangeDetectionStrategy.OnPush
  [ ] inject() for all dependencies
  [ ] Signals for all local state
  [ ] toSignal() to bridge any FormControl.valueChanges to computed signals
  [ ] takeUntilDestroyed(destroyRef) on all manual subscriptions
  [ ] Loading state on every async action (LoadingButtonComponent or PageSpinnerComponent)
  [ ] Empty state (EmptyStateComponent) for empty lists
  [ ] Error state (AlertBannerComponent or snackbar) for failures

Route
  [ ] Lazy-loaded via loadComponent()
  [ ] roleGuard() with correct roles
  [ ] Placed inside correct layout (MainLayout vs standalone)

Styling
  [ ] No hex colors outside styles.css / material-theme.scss
  [ ] No inline style="" in template
  [ ] Forms: appearance="outline" + matPrefix icon
  [ ] Buttons: mat-flat-button (primary) or mat-stroked-button (secondary), pill-shaped for CTAs
  [ ] Cards: .es-card class or equivalent token-based styling
  [ ] Status values: <app-status-badge [status]="x" />
  [ ] npm run lint:css passes
```

---

## 13. Known Patterns & Gotchas

### Route casing

The backend `RentalApplicationsController` registers as `/api/v1/RentalApplications` (PascalCase). All other controllers use lowercase. Always check the backend route before wiring a service call.

### `mat-datepicker` in standalone components

Requires `provideNativeDateAdapter()` in the component's `providers` array — not `MatNativeDateModule` in `imports`. See §8 Datepicker section.

### `computed()` + reactive forms

`computed()` cannot track `FormControl.value` because that is RxJS, not a signal. Bridge with `toSignal(control.valueChanges, { initialValue: ... })`. See §1 and §5.

### Tailwind `border` side-specificity

Because Tailwind's preflight is disabled, borders need `border-solid` explicitly. A single-side border requires:
```html
<div class="border-0 border-b border-solid border-es-border-light">
```

### `afterClosed()` memory leak

Dialog `afterClosed()` completes automatically after one emission — but only when the dialog closes. If the component is destroyed before the dialog closes, pipe through `takeUntilDestroyed`:

```typescript
this.dialog.open(...).afterClosed()
  .pipe(takeUntilDestroyed(this.destroyRef))
  .subscribe(result => { ... });
```

### `admin-table` overflow

`.admin-table` has `min-width: 560px`. Always wrap it:
```html
<div class="admin-table-wrap overflow-x-auto">
  <table class="admin-table">...</table>
</div>
```

### KYC-only verification wizard

`TenantFinancialProfile` has been **removed** from both backend and frontend. The KYC wizard handles identity verification only (AadhaarFront, AadhaarBack, PanCard, AddressProof). Do not re-add financial profile flows.

### Removed modules (do not reference)

- `financial-wizard.component` — removed
- `financial-profile.service.ts` — removed
- `financial.store.ts` — removed
- `FinancialProfileModel` — removed
- `es_has_session` localStorage key — removed (replaced by `es_access_token`)
- `RentIncreaseProposal` — removed from backend and frontend

---

## 14. Running the Project

```bash
# Install dependencies (first time or after pulling)
cd frontend/estatesync-ui
npm install

# Dev server (with hot reload)
ng serve
# → http://localhost:4200

# Type check only (no emit)
npx tsc --noEmit

# CSS lint (fails on raw hex colors outside token files)
npm run lint:css

# Run unit tests
ng test

# Production build
ng build --configuration production
```

### Environment config

`src/environments/environment.ts` — `apiUrl` defaults to `http://localhost:5000`. Do not commit a different API URL.

### Test accounts (after backend seed)

| Role | Email | Password |
|---|---|---|
| Admin | admin@estatesync.com | Password123! |
| Owner | owner@estatesync.com | Password123! |
| Tenant | tenant@estatesync.com | Password123! |
