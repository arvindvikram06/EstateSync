# EstateSync — Authentication & Token Hardening

**Date:** 2026-06-29
**Scope:** Full-stack auth/token lifecycle — frontend (Angular 21) + backend (ASP.NET Core)
**Goal:** Bring the auth flow up to industry practice: secure token storage, proper session lifecycle, rotation safety, and test coverage.

---

## At a glance

| Area | Before | After |
|---|---|---|
| Access token storage | `localStorage` (XSS-readable) | **In-memory only** (signal/field) |
| Reload persistence | Token survived in `localStorage` | **Silent refresh on bootstrap** via httpOnly cookie |
| Concurrent 401s | Each fired its own `/refresh-token` | **Single-flight** shared refresh |
| UI role checks | `currentUser().roles` from `localStorage` | **Decoded from in-memory JWT** |
| Route protection | `authGuard` only | **`roleGuard(...)`** on owner/admin routes |
| Logout | Client-side state clear only | **Server-side family revocation + cookie delete** |
| Access-token TTL | 1440 min (24h) | **15 min** |
| Refresh rotation races | Hard family revocation | **60s rotation grace window** |
| Refresh-token table | Never pruned | **Daily cleanup background job** |
| Auth test coverage | None (frontend) | **Vitest specs + backend xUnit tests** |

---

## Frontend changes

### 1. In-memory access token (XSS hardening)
**File:** `core/services/token.service.ts`

The JWT access token is no longer persisted in `localStorage`. It lives in a private in-memory field, so an injected script (XSS) can't read or exfiltrate it.

```ts
private accessToken: string | null = null;
getToken()   { return this.accessToken; }
setToken(t)  { this.accessToken = t; }
clearToken() { this.accessToken = null; }
```

- The non-sensitive user profile cache stays in `localStorage` (`es_user_profile`) for display only — never trusted for auth decisions.
- A **session hint** (`es_has_session`) is a boolean marker so bootstrap only attempts a refresh when a session previously existed (avoids a 401 round-trip for anonymous visitors).

### 2. Bootstrap silent refresh (survives hard reload)
**Files:** `core/services/auth.service.ts`, `app.config.ts`, `core/interceptors/http-context.tokens.ts`

Because the token is in-memory, a hard reload loses it. An `APP_INITIALIZER` exchanges the httpOnly refresh cookie for a fresh access token *before* the app renders.

```ts
// app.config.ts
provideAppInitializer(() => inject(AuthService).bootstrap())
```

```ts
// auth.service.ts
bootstrap(): Observable<unknown> {
  if (!this.tokenService.hasSessionHint()) return of(null);   // anonymous → no call
  return this.http.post(`${this.baseUrl}/refresh-token`, {}, {
    withCredentials: true,
    context: new HttpContext().set(SKIP_AUTH_HANDLING, true)   // bypass interceptor redirect
  }).pipe(tap(res => { if (res.success) this.persist(res.data); }),
          catchError(() => { this.tokenService.clearSessionHint(); return of(null); }));
}
```

A new `SKIP_AUTH_HANDLING` HttpContext token tells the error interceptor not to run its 401-handling for the bootstrap call (so a missing/expired session doesn't trigger a navigation mid-startup).

### 3. Single-flight refresh
**File:** `core/interceptors/error.interceptor.ts`

When several requests 401 at once, they now share **one** `/refresh-token` call instead of each firing its own (which would rotate the refresh token multiple times and invalidate the session).

```ts
let refresh$: Observable<string> | null = null;
// ...
if (!refresh$) {
  refresh$ = authService.refreshToken().pipe(
    map(res => res.data.accessToken),
    finalize(() => { refresh$ = null; }),
    shareReplay({ bufferSize: 1, refCount: false })
  );
}
return refresh$.pipe(switchMap(token => next(req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }))), ...);
```

### 4. Roles decoded from the JWT (not localStorage)
**Files:** `core/services/token.service.ts`, `core/services/auth.service.ts`

`decodeRoles()` reads roles straight from the in-memory JWT, so a user can't grant themselves a role by editing `localStorage`. It reads the short `"role"` claim the JWT actually carries, with the long Microsoft URI as a fallback:

```ts
const claim = payload.role
  ?? payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
```

`persist()` overwrites the response body's roles with the JWT-decoded roles, so `currentUser().roles` is always authoritative.

### 5. Role-based route guard
**Files:** `core/guards/role.guard.ts`, `app.routes.ts`

```ts
export function roleGuard(...roles: string[]): CanActivateFn {
  return () => {
    const ts = inject(TokenService), router = inject(Router);
    if (!ts.isAuthenticated()) return router.createUrlTree(['/auth/login']);
    return roles.some(r => ts.decodeRoles().includes(r)) ? true : router.createUrlTree(['/']);
  };
}
```

Applied: `owner/dashboard` → `roleGuard('Owner', 'Admin')`, `admin/dashboard` → `roleGuard('Admin')`.

### 6. Logout calls the server + clears cross-user state
**Files:** `core/services/auth.service.ts`, `core/services/notification.service.ts`

`logout()` now `POST`s `/api/v1/auth/logout` (with credentials + `SKIP_AUTH_HANDLING`), then clears all client state in `finalize` so teardown always completes — even if the network call fails. It also clears in-memory notifications (`notificationService.clear()`) to prevent the next user on a shared machine from seeing the previous user's list.

### 7. SignalR token + reconnect resilience
**File:** `core/services/signalr.service.ts`

- `accessTokenFactory` re-reads the token on every (re)connect so a refreshed token is used.
- Custom `withAutomaticReconnect` policy retries forever with capped exponential backoff (was: default policy giving up after ~30s).
- The `ReceiveNotification` handler is detached and `stop()` awaited on disconnect.

---

## Backend changes

### 8. Server-side logout + token revocation
**Files:** `Interfaces/Services/Auth/IAuthService.cs`, `Services/Auth/AuthService.cs`, `Controllers/AuthController.cs`

```csharp
public async Task LogoutAsync(string? refreshToken) {
  if (string.IsNullOrWhiteSpace(refreshToken)) return;        // idempotent
  var entity = await _refreshTokenRepository.GetByTokenAsync(refreshToken);
  if (entity == null) return;
  await RevokeTokenFamilyAsync(entity.TokenFamily);            // reuse existing helper
  await _unitOfWork.SaveChangesAsync();
}
```

`POST /api/v1/auth/logout` reads the cookie, revokes the family, and deletes the cookie with attributes matching how it was set.

### 9. Refresh-token rotation grace window
**File:** `Services/Auth/AuthService.cs`

Rotation revokes the presented token and issues a new one; presenting an already-revoked token nukes the whole family (reuse detection). Because the SPA now refreshes on every load, quick/overlapping reloads can present a just-rotated token. A **60-second grace window** treats that as a benign duplicate and returns the family's current successor instead of revoking.

```csharp
private static readonly TimeSpan RefreshRotationGrace = TimeSpan.FromSeconds(60);
// if revoked within the grace window AND a live successor exists → return successor, no revoke
// otherwise → genuine reuse → revoke family
```

### 10. Shorter access-token TTL
**File:** `appsettings.json` — `Jwt:ExpiryMinutes` `1440` → `15`.
Safe because of the single-flight refresh + bootstrap refresh + grace window.

### 11. Refresh-token cleanup background job
**Files:** `Interfaces/Repositories/IRefreshTokenRepository.cs`, `Repositories/RefreshTokenRepository.cs`, `Services/Background/RefreshTokenCleanupScheduler.cs`, `Program.cs`

`DeleteExpiredAndStaleRevokedAsync(now, revokedRetention)` removes expired rows and rows revoked longer than the retention (2 days; keeps recent ones so the grace window still works). A `BackgroundService` (mirroring `LeaseExpiryScheduler`) runs it every 24h.

---

## Tests

### Backend (`AuthServiceTests.cs`) — 10/10 pass
- `RefreshTokenAsync_ShouldReturnSuccessor_WhenRevokedWithinGraceWindow`
- `LogoutAsync_ShouldRevokeFamily_WhenTokenIsValid`
- `LogoutAsync_ShouldBeNoOp_WhenTokenIsNullOrUnknown`
- (existing reuse-detection test still green — it uses `RevokedAt = -5min`, outside the grace window)

### Frontend (Vitest) — 23/23 pass
- `token.service.spec.ts` — in-memory token, `isAuthenticated`, `decodeRoles` (short `role` + long URI), session hint
- `role.guard.spec.ts` — allow/redirect-home/redirect-login paths
- `auth.service.spec.ts` — bootstrap with/without session hint, logout posts + clears
- `error.interceptor.spec.ts` — two concurrent 401s trigger exactly one refresh, both retry with the new bearer

---

## Files touched

**Frontend (new):** `core/interceptors/http-context.tokens.ts`, `core/guards/role.guard.ts`, `core/services/token.service.spec.ts`, `core/services/auth.service.spec.ts`, `core/guards/role.guard.spec.ts`, `core/interceptors/error.interceptor.spec.ts`
**Frontend (modified):** `core/services/token.service.ts`, `core/services/auth.service.ts`, `core/services/notification.service.ts`, `core/services/signalr.service.ts`, `core/interceptors/error.interceptor.ts`, `app.config.ts`, `app.routes.ts`, `features/dashboard/dashboard.component.ts`
**Backend (new):** `Services/Background/RefreshTokenCleanupScheduler.cs`
**Backend (modified):** `Controllers/AuthController.cs`, `Services/Auth/AuthService.cs`, `Interfaces/Services/Auth/IAuthService.cs`, `Interfaces/Repositories/IRefreshTokenRepository.cs`, `Repositories/RefreshTokenRepository.cs`, `Program.cs`, `appsettings.json`, `EstateSync.Tests/Services/AuthServiceTests.cs`, `EstateSync.Tests/EstateSync.Tests.csproj`

---

## Manual verification checklist
1. Log in → hard-refresh a protected page repeatedly → stays logged in (silent refresh + grace window).
2. Owner logs in → `/owner/dashboard` routes correctly; a Tenant navigating there is redirected.
3. Logout → `refreshToken` cookie deleted; a subsequent `/refresh-token` returns 401 (session truly dead).
4. Idle past 15 min → next request silently refreshes once (one `/refresh-token` in Network tab).
