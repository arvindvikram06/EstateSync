# EstateSync — Bugs & Errors Fixed

**Date:** 2026-06-29
**Scope:** Lease flow + auth/token hardening sessions

Each entry: symptom → root cause → fix → verification.

---

## 🔴 1. Owner dashboard would not route for an owner user

**Symptom:** An owner saw the "Owner dashboard" link in the navbar, but clicking it (or the sidebar tab) redirected back to home — never reaching `/owner/dashboard`.

**Root cause:** ASP.NET's `JwtSecurityTokenHandler` serializes `ClaimTypes.Role` to the **short `"role"`** key in the JWT. `TokenService.decodeRoles()` only looked for the **long Microsoft URI**, so it returned `[]`.
- The navbar `isOwner()` reads `currentUser().roles`, which fell back to the backend response body (had `Owner`) → link **showed**.
- `roleGuard('Owner','Admin')` calls `decodeRoles()` **directly** → `[]` → **redirect**.
Result: link visible, route blocked.

**Fix:** `core/services/token.service.ts` — read the short key with the long URI as fallback:
```ts
const claim = payload.role
  ?? payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
```
Added a regression test for the short `role` claim.

**Verified:** owner reaches `/owner/dashboard`; tenant is redirected; 23/23 frontend tests pass.

---

## 🔴 2. Logged out after ~5–6 hard refreshes

**Symptom:** After moving the access token in-memory, repeatedly hard-refreshing a protected page logged the user out after several reloads.

**Root cause:** The SPA now calls `/refresh-token` on every page load. The backend rotates the refresh token and **revokes the entire token family** the instant an already-revoked token is presented (reuse detection). Quick/overlapping reloads send a just-rotated cookie before the new one is committed → reuse detected → family nuked → logout.

**Fix:** `Services/Auth/AuthService.cs` — added a **60-second rotation grace window**. A revoked token presented within the window, when a live successor exists, returns the successor instead of revoking the family. Genuine replay later still trips reuse detection.

**Verified:** rapid reloads stay logged in; `AuthServiceTests` covers both the grace path and the still-revoke path (existing reuse test uses `-5min`, outside the window).

---

## 🟠 3. Redundant "Owner Dashboard" tab in the tenant sidebar

**Symptom:** The tenant dashboard sidebar showed an "Owner Dashboard" item the user never requested, duplicating the navbar link.

**Root cause:** A leftover `items.push(...)` in `features/dashboard/dashboard.component.ts` from earlier dashboard/lease work (not the security session).

**Fix:** Removed the sidebar item and the now-unused `AuthService` injection/import. Owner Dashboard remains reachable via the navbar link.

---

## 🔴 4. Lease "Sign and Submit" button unclickable

**Symptom:** On the lease sign page, the button stayed disabled even when both the name field and signature were filled.

**Root cause:** `canSubmit = computed(() => this.form.valid && !this.padEmpty())`. `form.valid` is **not a signal**, so the computed only re-evaluated when `padEmpty` changed. Drawing the signature first, then typing the name, left `canSubmit` stuck at `false`.

**Fix:** `features/leases/lease-sign/lease-sign.component.ts` — bridged form validity into a signal via `form.statusChanges`, and used `setTimeout(…, 0)` (macrotask) instead of `queueMicrotask` for SignaturePad init so Angular CD + `@ViewChild` are ready.
```ts
this.form.statusChanges.pipe(takeUntilDestroyed(this.destroyRef))
  .subscribe(s => this.formValid.set(s === 'VALID'));
readonly canSubmit = computed(() => this.formValid() && !this.padEmpty());
```

---

## 🔴 5. Duplicate lease creation for the same application

**Symptom:** An owner could create a second lease for a rental application while an existing lease was mid-signing or awaiting admin approval.

**Root cause:** `LeaseService.CreateLeaseAsync` only blocked status `PendingApproval`. `PartialSigned` and `PendingAdminApproval` fell through.

**Fix:** `Services/Leases/LeaseService.cs` — block on all non-terminal statuses (allow only when the latest lease is `Voided`, `Expired`, or `Terminated`), with a descriptive `ConflictException` naming the current state.

---

## 🟠 6. Backend test project never compiled (broken ProjectReference)

**Symptom:** `dotnet test` failed with 494 "type/namespace not found" errors across the suite.

**Root cause:** `EstateSync.Tests.csproj` referenced `..\..\src\EstateSync.Api\EstateSync.Api.csproj`, but the API had moved to `backend/EstateSync.Api/`. The reference resolved to nothing, so the test assembly had no API types. (Pre-existing; not introduced by these changes.)

**Fix:** Corrected the reference to `..\EstateSync.Api\EstateSync.Api.csproj`. Error count dropped 494 → ~290 (remaining were stale test files, see #7).

---

## 🟡 7. Stale test files from the in-progress entity refactor (quarantined)

**Symptom:** After the reference fix, 290 errors remained across 11 test files (e.g. references to the deleted `Location` entity, old constructors, renamed enums, new required members).

**Root cause:** An unfinished `Location` removal / entity refactor left those test files out of sync with the API. Unrelated to the auth work.

**Action (temporary):** Added a clearly-marked, reversible `<!-- TEMP-QUARANTINE -->` block in the test csproj excluding the 11 stale files, so the suite compiles and runs. The block lists every file to restore.

**Status:** With the quarantine, `dotnet test` runs **34 passed, 2 failed** — the 2 failures are `LocalFileStorageServiceTests` (filesystem I/O, environment-specific path), also pre-existing.

**Follow-up needed:** after the entity refactor lands, remove the quarantine block and reconcile the 11 test files against the new models.

---

## Frontend hardening fixes (from the code review pass)

| # | Issue | Fix |
|---|---|---|
| 🟠 8 | Access token in `localStorage` (XSS exfiltration) | Moved in-memory + bootstrap silent refresh |
| 🟠 9 | UI roles trusted from `localStorage` | Decode roles from the in-memory JWT |
| 🔴 10 | Concurrent 401s fired multiple refreshes (token-rotation storm) | Single-flight shared refresh in the error interceptor |
| 🟠 11 | Notifications not cleared on logout (cross-user leak) | `notificationService.clear()` in logout |
| 🟠 12 | Privileged routes only had `authGuard` | Added `roleGuard(...)` to owner/admin routes |
| 🟠 13 | No production `environment` replacement → raw error logging in prod | Added `environment.production.ts` + `fileReplacements` |
| 🟠 14 | SignalR gave up reconnecting after ~30s; stale token on reconnect | Capped infinite backoff + token re-read on reconnect |
| 🟡 15 | `totalPages` getter recomputed each CD cycle | Converted to `computed()` signal |
| 🟡 16 | Images missing lazy loading | `loading="lazy"` + `decoding="async"` on all `<img>` |

---

## Test status snapshot (2026-06-29)
- **Backend:** `AuthServiceTests` 10/10 pass; full suite 34 pass / 2 pre-existing env failures (with stale tests quarantined).
- **Frontend:** 23/23 Vitest tests pass; production build clean (only pre-existing budget/leaflet warnings).
