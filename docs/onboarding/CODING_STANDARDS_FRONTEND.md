# EstateSync — Frontend Coding Standards

Angular 21 SPA at `frontend/estatesync-ui/`. Standalone components, signals, Angular Material v21 plus Tailwind CSS v3. No other UI libraries — do not install Bootstrap, PrimeNG, etc.

## 1. Components

- Standalone components only — no NgModules.
- ChangeDetectionStrategy.OnPush on every component, always.
- Use the inject() function for dependencies — no constructor injection.
- Signals first for local state: signal, computed, and toSignal for observables. Don't use BehaviorSubject for UI state.
- NgRx Signal Store only for complex multi-step feature state (the KYC and Financial wizards are the examples). Simple pages just use signals and services.
- Never subscribe manually where toSignal works. When you must subscribe, clean it up with takeUntilDestroyed(destroyRef).
- Feature routes are lazy-loaded with loadComponent / loadChildren under app/features.
- Guards and interceptors are functional only — no class-based versions. Role-restricted routes use roleGuard with the allowed role names (there is no separate authGuard).

## 2. Types and API calls

- TypeScript models must mirror the backend DTO field names exactly.
- Every HTTP call is typed with the ApiResponse or PagedResponse wrapper and the base URL comes from environment.apiUrl.
- Frontend enums must match the backend enum numeric values exactly. Backend enums are append-only, so never reorder frontend enums either.
- Strict TypeScript — no "any". Use unknown and narrow if needed.
- Remember backend routes use PascalCase controller names (for example /api/v1/RentalApplications).

## 3. Auth and storage rules (security-critical)

- The access token lives in memory only (TokenService). The refresh token is an httpOnly cookie set by the server.
- The only allowed localStorage key is es_has_session — a boolean hint that tells bootstrap to attempt a silent refresh. Never store tokens, user profiles, or any PII in localStorage or sessionStorage.
- Roles are decoded from the JWT with jwt-decode — never trust roles from storage or from a response body.

## 4. Styling (the most-enforced rule set)

Plain-language version with token and shared-class tables: [STYLING_CHEATSHEET.md](STYLING_CHEATSHEET.md).

- Hex color codes may appear in exactly two files: styles.css (the --es-* tokens) and material-theme.scss (the --mat-sys-* tokens). Everywhere else, use the CSS variables or the token-based Tailwind classes. This is machine-checked: "npm run lint:css" fails the build on raw colors.
- Inline style attributes are banned in templates. For conditional colors use class toggles with token classes, never style bindings with hex.
- Decision ladder — stop at the first step that fits:
  1. Interactive widget (button, form field, dialog, menu) → Angular Material component.
  2. Layout, spacing, grid, one-off sizing → Tailwind utility classes in the template.
  3. Any color, or a pattern used three or more times → a shared class or token in styles.css. Reuse the existing vocabulary first: es-card, es-badge--*, es-text-sm/xs/strong, es-info-banner--*, stat-card, stat-grid, filter-pill, admin-table.
  4. Structure unique to one component → that component's own .css file, with colors via tokens.
  5. Never inline styles, never hex outside the two token files.
- Because Tailwind preflight is disabled, borders need border-solid explicitly, and a single-side border needs border-0 plus the side you want (for example border-0 border-b border-solid).
- The one exception to the hex rule: color values consumed by JavaScript (chart libraries, canvas gradients) are data, not styling.

## 5. Forms

- Reactive Forms with nonNullable true; use getRawValue() to read values including disabled controls.
- Validators mirror the backend DTO rules exactly (required, min lengths, email, etc.).
- Every mat-form-field uses appearance "outline" with a matPrefix icon.
- Show validation errors only after the field is touched.

## 6. UI patterns

- Every async action shows a loading state — use LoadingButtonComponent for buttons, PageSpinnerComponent for full pages.
- Every list handles the empty case with EmptyStateComponent, and errors with AlertBannerComponent or a snackbar via ErrorHandlerService.
- Status badges use the es-badge--* classes through StatusBadgeComponent — never custom inline badge styles.
- Page-level call-to-action buttons are pill-shaped (fully rounded, 48px tall). Use mat-flat-button for primary, mat-stroked-button for secondary; never mat-raised-button.
- Cards are white with a light border, 16px radius, soft shadow — use the es-card class.
- Multi-step wizards use programmatic navigation only — users can go back but never click ahead to skip steps (do not use mat-stepper headers).


