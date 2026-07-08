# Full-Stack Audit & Refactor Report

**Date:** 2026-07-06
**Scope:** `frontend/estatesync-ui` (Angular 21) + `backend/EstateSync.Api` (ASP.NET Core)
**Result:** Frontend `ng build` ✅ clean · Backend `dotnet build` ✅ 0 warnings, 0 errors · Backend tests: 34/36 pass (2 failures pre-existing, unrelated — see [Notes](#notes--caveats))

---

## Table of Contents

1. [Critical correctness bug (backend)](#1-critical-correctness-bug-backend)
2. [Frontend — API call fixes](#2-frontend--api-call-fixes)
3. [Frontend — TypeScript/component fixes](#3-frontend--typescriptcomponent-fixes)
4. [Frontend — CSS consolidation](#4-frontend--css-consolidation)
5. [Backend — query/database fixes](#5-backend--querydatabase-fixes)
6. [Backend — architecture & duplication fixes](#6-backend--architecture--duplication-fixes)
7. [Intentionally not changed](#7-intentionally-not-changed)
8. [Notes & caveats](#notes--caveats)

---

## 1. Critical correctness bug (backend)

### 1.1 `AsNoTracking()` + mutation = silently lost writes ⚠️ THE most important fix

**Files:** `Repositories/RentalApplicationRepository.cs`, `Interfaces/Repositories/IRentalApplicationRepository.cs`, `Services/Leases/LeaseService.cs`

**Before:**
```csharp
// Repository
public async Task<IEnumerable<RentalApplication>> GetByPropertyIdAsync(Guid propertyId)
{
    return await _dbSet
        .AsNoTracking()          // ← entities are NOT tracked by EF
        .Where(ra => ra.PropertyId == propertyId)
        .ToListAsync();
}

// LeaseService.RejectOtherApplicationsOnLeaseActivationAsync
var applications = await _applicationRepo.GetByPropertyIdAsync(propertyId);
foreach (var app in applications)
{
    app.Status = ApplicationStatus.Rejected;   // ← mutation on UNTRACKED entity
    app.ReviewRemarks = "Property is reserved";
    // ...
}
// SaveChangesAsync later → EF sees no changes → NOTHING is persisted
```

**After:**
```csharp
// Tracked on purpose: LeaseService mutates these entities (rejecting other
// applications on lease activation) and persists via SaveChangesAsync.
public async Task<IEnumerable<RentalApplication>> GetByPropertyIdForUpdateAsync(Guid propertyId)
{
    return await _dbSet
        .Where(ra => ra.PropertyId == propertyId)
        .ToListAsync();
}
```

**Why it matters:** When an admin approved a lease, the "reject all other pending applications for this property" step **never actually saved to the database**. Other tenants' applications stayed `Pending`/`Approved` forever (though they did receive the rejection *notification* — the notification insert was tracked, the status change was not). The rename to `...ForUpdateAsync` also documents the tracking contract so the bug can't silently come back.

---

## 2. Frontend — API call fixes

### 2.1 Admin dashboard stats fetched twice

**Files:** `features/admin/admin-dashboard.component.ts/.html`, `features/admin/tabs/admin-overview-tab/admin-overview-tab.component.ts`

**Before:** the parent dashboard called `getDashboardStats()` for the nav badge, and the overview tab called `getDashboardStats()` **again** for its charts → two identical HTTP requests on every dashboard load, plus a re-fetch on every return to the Overview tab (tabs are destroyed/recreated on switch).

**After:** the parent fetches once into signals and passes them down:
```typescript
// parent
readonly stats        = signal<AdminDashboardStats | null>(null);
readonly statsLoading = signal(true);

// overview tab — no HTTP call at all anymore
readonly stats   = input.required<AdminDashboardStats | null>();
readonly loading = input(false);
```
```html
<app-admin-overview-tab [stats]="stats()" [loading]="statsLoading()" ... />
```

**Benefits:** one request instead of two; tab switches no longer re-hit the (heaviest) stats endpoint; the overview tab became a pure presentational component (easier to test).

### 2.2 Tenant bills fetched twice — same pattern

**Files:** `features/dashboard/dashboard.component.ts/.html`, `features/dashboard/tabs/bills-tab/bills-tab.component.ts`

Same parent-fetch/share fix as 2.1. Bonus simplification: the pending-bills nav badge is now a `computed()` over the shared list instead of a manually synced signal + `(pendingCountChange)` output — one source of truth, less wiring:

```typescript
private readonly pendingBillCount = computed(
  () => this.bills().filter(b => b.status === 'Pending').length
);
```

### 2.3 Owner earnings tab: 3 uncoordinated requests → 1 `forkJoin`

**File:** `features/owner/tabs/owner-earnings-tab/owner-earnings-tab.component.ts`

**Before:** `ngOnInit` called `loadSummary()`, `loadSettlements()`, `loadBalance()` — three independent subscriptions, three separate loading states drifting out of sync.

**After:**
```typescript
forkJoin({
  summary:     this.paymentSvc.getOwnerEarningsSummary(),
  settlements: this.paymentSvc.getOwnerSettlements(undefined, 1, this.limit()),
  balance:     this.paymentSvc.getOwnerAvailableBalance(),
}).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(...)
```
`loadSettlements()`/`loadBalance()` are kept for later pagination/refresh-only reloads.

**Benefits:** requests fire in parallel with a single completion point; the tab renders once when everything is ready instead of popping in piecemeal.

### 2.4 SignalR notification refetch — throttled + reconnect gap fixed

**Files:** `core/services/notification.service.ts`, `core/services/signalr.service.ts`

**Before:** `fetchAll()` fired on every `connect()`. Worse, there was **no `onreconnected` handler at all** — notifications received while the connection was down were silently lost until a full page reload.

**After:**
```typescript
// NotificationService
fetchAllIfStale(maxAgeMs = 30_000): void {
  if (Date.now() - this.lastFetchedAt < maxAgeMs) return;
  this.fetchAll().subscribe();
}
clear(): void {
  this.notifications.set([]);
  this.lastFetchedAt = 0; // next login must fetch fresh data immediately
}

// SignalRService
this.connection.onreconnected(() =>
  this.ngZone.run(() => this.notificationService.fetchAllIfStale()));
```

**Benefits:** flaky networks can't hammer the API (30 s staleness guard), missed notifications are now recovered after a reconnect (a correctness improvement, not just perf), and the logout reset prevents a stale timestamp from skipping the next user's first fetch.

### 2.5 Admin complaints filters — debounced + race-proof

**File:** `features/admin/tabs/admin-complaints-tab/admin-complaints-tab.component.ts`

**Before:** each filter control fired `load()` immediately on change → rapid changes = multiple parallel requests; a slow older response could arrive *after* a newer one and overwrite fresher results (classic out-of-order race).

**After:** one merged, debounced trigger feeding a `switchMap` pipeline:
```typescript
this.reload$
  .pipe(
    switchMap(() => this.adminService.getAdminComplaints(...)
      .pipe(catchError(() => { this.loading.set(false); return EMPTY; }))),
    takeUntilDestroyed(this.destroyRef),
  )
  .subscribe(res => { ... });

merge(this.statusFilter.valueChanges, this.priorityFilter.valueChanges)
  .pipe(debounceTime(300), takeUntilDestroyed(this.destroyRef))
  .subscribe(() => { this.page.set(1); this.load(); });
```

**Benefits:** rapid filter changes → one request; `switchMap` cancels in-flight requests so stale data can never win; `catchError` *inside* the `switchMap` keeps one failed request from killing the whole stream.

### 2.6 Duplicate complaint service methods collapsed

**File:** `core/services/complaint.service.ts` (+ 4 callers)

**Before:** `getMyComplaints()` and `getOwnerComplaints()` — two methods, identical bodies, same URL. **After:** one `getComplaints()` with a doc comment explaining the backend scopes results by the caller's role.

### 2.7 Redundant KYC list fetch on property detail

**File:** `features/properties/property-detail/property-detail.component.ts`

**Before:** if the JWT claim said "not KYC verified", the page fetched the user's **entire KYC list** just to double-check. **After:** `readonly kycApproved = this.authService.isKycVerified;` — the auth signal is the source of truth (it's kept current by `syncProfile()` on verification notifications via SignalR). One fewer request per property view for unverified users, and the CTA is now *reactive* — it flips to "Apply" live if verification is approved while the page is open.

---

## 3. Frontend — TypeScript/component fixes

### 3.1 Unmanaged `.subscribe()` calls

**Fixed (added `takeUntilDestroyed(this.destroyRef)`):**
- `admin-create-verifier-dialog.component.ts` — create-verifier submit
- `admin-update-complaint-status-dialog.component.ts` — status update submit
- `admin-review-dialog.component.ts` — the property `forkJoin` load **and** the review submit

**Deliberately left fire-and-forget (with explanatory comments added):**
- `notification-detail-dialog` `markOneRead()` calls — the dialog closes immediately; cancelling the subscription on destroy would **abort the mark-read HTTP request**, a behavior regression. One-shot `HttpClient` observables complete on their own — no leak.
- `navbar` `markAllRead()` — the navbar lives for the app's lifetime.

**Benefit:** dialog subscriptions can no longer fire callbacks (`dialogRef.close()`, signal writes) after the dialog is destroyed.

### 3.2 Duplicated helpers removed

**File:** `core/utils/format.utils.ts` now exports `getInitials()` alongside the existing `formatCurrency`/`formatDate`.

| Duplicate removed | Where | Now |
|---|---|---|
| Inline `formatCurrency` | `admin-overview-tab.component.ts` | `readonly formatCurrency = formatCurrency;` (shared util) |
| Inline `formatRent` | `property-card.component.ts` | `readonly formatRent = formatCurrency;` |
| Inline `initials()` | `owner-applications-tab.component.ts` | `readonly initials = getInitials;` |
| Inline initials logic | `navbar.component.ts` | `computed(() => getInitials(...))` |

**Extra perf win:** both inline currency helpers created a **new `Intl.NumberFormat` instance on every call** (and they were called from templates, i.e. every change-detection cycle per item). The shared util uses one module-level cached formatter.

### 3.3 Per-item method calls in `@for` loops → memoized `computed()`

**Files:** `owner-leases-tab.component.ts/.html`, `owner-complaints-tab.component.ts/.html`

**Before:** templates called `canOwnerSign(lease)`, `canVoid(lease)`, `nextStatus(c.status)`, `nextLabel(c.status)` inside loops — re-evaluated for *every row on every change-detection cycle*.

**After:** flags are precomputed once per data change into the list signal:
```typescript
readonly leaseRows = computed(() => this.leases().map(l => ({
  ...l,
  canOwnerSign: l.status === 'PendingApproval' || (l.status === 'PartialSigned' && !l.ownerSignedAt),
  canVoid: l.status === 'PendingApproval' || l.status === 'PartialSigned',
})));
```
Templates read `lease.canOwnerSign` / `c.nextLabel` — plain property access, no function calls.

**Benefit:** work happens once per data change instead of rows × change-detection cycles. Follows the pattern `lease-detail.component.ts` already used.

### 3.4 `any` types eliminated (3)

| File | Before | After |
|---|---|---|
| `create-lease-dialog.component.ts` | `const fmtDate = (d: any) => ...` | `(d: string \| Date): string` |
| `admin-review-dialog.component.ts` | `safeUrl: any` in dialog data | `safeUrl: SafeResourceUrl` (`@angular/platform-browser`) |
| `apply-dialog.component.ts` | `v.expectedMoveInDate as any` | `as string \| Date` + typed ternary (datepicker sets a `Date` at runtime even though the control is typed `string` — now documented) |

---

## 4. Frontend — CSS consolidation

### 4.1 Expanded design tokens (`src/styles.css`)

```css
--es-radius-md: 12px;  --es-radius-pill: 40px;
--es-border-light: #EBEBEB;  --es-text-muted: #AAAAAA;
```

### 4.2 `.filter-pill` — 6 copies → 1 global definition

The identical outline-style pill (border, radius 20px, active tint) was copy-pasted in **applications-tab, owner-applications-tab, owner-complaints-tab, owner-properties-tab** — all four copies deleted; one definition now lives in `styles.css` (with a `:hover` state the copies lacked).
The **owner-earnings tab** and **public properties page** use a *filled* active pill — those are intentional design variants and keep their scoped CSS (component-scoped styles override the global base).

### 4.3 Metric/stat cards — shared base for row-layout KPI tiles

`.stat-card` (dashboard overview + owner overview — byte-identical copies) and `.summary-card` (bills tab, near-identical) now share one global block: card frame, hover shadow, icon box, value, label. Component files keep only true overrides (e.g. bills' purple icon tint, its 18px/800 value). The admin `.kpi-card`/`.fin-card` use a **column** layout with different type sizes — grouping them would have changed their look, so they stay component-local (documented in the global block's comment).

### 4.4 `.admin-table` — structural base global, themes local

Structure (wrap card, collapse, cell padding, row hover) moved to `styles.css`. The KYC tab keeps only its purple-theme overrides (sticky header, `#F5F3FF` head, uppercase purple headers, striped rows); the financial tab keeps two lines. The admin-complaints tab reuses the KYC stylesheet, so it inherits the change for free. Also fixed the one hardcoded `#6750A4` → `var(--mat-sys-primary)`.

**Net effect:** ~230 lines of duplicated CSS deleted; new components get correct cards/pills/tables by just using the class names; changing the design system now means editing one file.

### 4.5 Deferred (documented, not in this pass)
- Full sweep of ~490 inline `style="..."` attributes → utility classes (worst offenders identified: owner tabs ~110, dashboard tabs ~75).
- Global hex→token sweep beyond the files touched (≈149 instances total).
- The `!important` on destructive buttons (`.void-btn`, `.reject-btn`) was **kept** — it legitimately fights Angular Material's own elevated-specificity button styles; removing it would regress the red styling.

---

## 5. Backend — query/database fixes

### 5.1 Admin dashboard stats: ~21 sequential queries → ~11

**Files:** `Services/Admin/AdminDashboardService.cs`, `Repositories/StatisticsRepository.cs`, `IStatisticsRepository.cs`

**Before:** 21 sequential `await`s — including **3 separate role-count queries** and **6 standalone COUNT queries whose answers were already inside the breakdown dictionaries fetched in the same method**.

**After:**
1. One grouped query replaces three role counts:
```csharp
public async Task<Dictionary<string, int>> GetUserCountsByRoleAsync(...)
    => await _db.UserRoles
        .Where(ur => ur.Role != null && ur.User!.DeletedAt == null)
        .GroupBy(ur => ur.Role!.Name)
        .Select(g => new { Role = g.Key, Count = g.Count() })
        .ToDictionaryAsync(x => x.Role, x => x.Count, ct);
```
2. Five counts are now **derived** from breakdowns already fetched:
```csharp
PendingKycCount           = kycBreakdown.GetValueOrDefault("Pending"),
PendingPropertyApprovalCount = propertyBreakdown.GetValueOrDefault("PendingApproval"),
PendingLeaseApprovalCount = leaseBreakdown.GetValueOrDefault("PendingAdminApproval"),
OpenComplaintsCount       = complaintBreakdown.GetValueOrDefault("Open"),
ActiveLeasesCount         = leaseBreakdown.GetValueOrDefault("Active"),
```
Six now-unused repository methods were removed (ISP rule — no dead interface members).

**Why not `Task.WhenAll`?** A scoped `DbContext` is not thread-safe — parallel awaits on one context throw. Reducing the query count is the safe, equally effective fix.

**Verified live:** restarted the API and confirmed `GET /admin/dashboard/stats` returns internally consistent data (e.g. `openComplaintsCount: 3` == `complaintStatusBreakdown.Open: 3`).

### 5.2 Owner settlement totals: in-memory aggregation → SQL

**File:** `Repositories/OwnerSettlementRepository.cs` (`GetOwnerTotalsAsync`)

**Before:** loaded **every settlement row** for the owner into memory, then computed sums/count/last-payout with LINQ-to-Objects. Memory and transfer costs grow linearly with history size.

**After:** one grouped SQL aggregate (paid sum, pending sum, count) + one `SELECT ... ORDER BY ... LIMIT 1` projection for the last payout. Constant-size result regardless of row count.

### 5.3 Property create: redundant re-fetch removed

**File:** `Services/Properties/PropertyService.cs` (`CreateAsync`)

**Before:** after `SaveChangesAsync`, re-fetched the just-created property via `GetWithDetailsByIdAsync` (full Includes of images/amenities/owner) just to map the response.
**After:** maps from the already-tracked entity — a fresh Draft has no images/amenities, so only the Owner navigation needs populating (single lightweight `GetByIdAsync`).

### 5.4 Lease conflict checks: 2 queries → 1

**Files:** `ILeaseRepository.cs`, `LeaseRepository.cs`, `LeaseService.cs`, `RentalApplicationService.cs`

**Before:** `GetActiveLeaseByPropertyIdAsync` + `GetActiveLeaseByTenantIdAsync` — two round-trips, duplicated in **both** lease creation and rental application validation.

**After:** a single combined query used at both call sites:
```csharp
public async Task<(Lease? PropertyLease, Lease? TenantLease)> GetActiveLeaseConflictsAsync(Guid propertyId, Guid tenantId)
{
    var leases = await _dbSet.AsNoTracking()
        .Where(l => l.Status == LeaseStatus.Active && (l.PropertyId == propertyId || l.TenantId == tenantId))
        .ToListAsync();
    return (leases.FirstOrDefault(l => l.PropertyId == propertyId),
            leases.FirstOrDefault(l => l.TenantId == tenantId));
}
```
The now-unused `GetActiveLeaseByTenantIdAsync` was removed (it also carried an unnecessary `Include(Property)`).

### 5.5 Financial summary: paged query for a count → `CountAsync`

**File:** `Services/Admin/AdminFinancialService.cs`

**Before:** `GetPagedAsync(null, null, null, null, 1, 1, ct)` — ran a filtered+ordered+paged query with a `User` Include **just to read `.Total`**.
**After:** a dedicated `CountAsync()` on the settlement repository — `SELECT COUNT(*)`.

### 5.6 Unbounded admin complaint list → bounded

**Files:** `Services/Complaints/ComplaintService.cs`, `ComplaintRepository.cs`, `IComplaintRepository.cs`

**Before:** the Admin branch of `GetMyComplaintsAsync` called `GetAllAsync()` — every complaint in the database with images + lease + property Includes.
**After:** it reuses the existing paged admin query capped at the most recent 200 (`GetPagedForAdminAsync(null, null, 1, 200, ct)`); the admin UI already uses the properly paginated `/admin` endpoint, so this defensive path can never load the full table. The unbounded `GetAllAsync()` was deleted from the repo + interface. Tenant/owner branches were left alone — they're naturally bounded per user, and changing the response shape would have broken 4 frontend call sites.

---

## 6. Backend — architecture & duplication fixes

### 6.1 Seven missing FluentValidation validators added

Project rule: every request DTO gets a validator (auto-registered via `AddValidatorsFromAssemblyContaining`). Added in `Validators/`:

| Validator | Key rules |
|---|---|
| `AdminLeaseApprovalRequestValidator` | remarks ≤1000 chars; **remarks required when rejecting** |
| `CreateStaffRequestValidator` | name/email/phone required, valid email, password ≥8 |
| `SubmitKycRequestValidator` | Aadhaar `^\d{12}$`, PAN `^[A-Z]{5}[0-9]{4}[A-Z]$` |
| `SubmitTenantFinancialProfileRequestValidator` | enum valid, employer ≤250, income > 0 |
| `UpdateComplaintStatusRequestValidator` | status enum valid, remarks ≤1000 |
| `VoidLeaseRequestValidator` | reason ≤1000 |
| `UpdateLeaseRequestValidator` | end > start, rent > 0, deposit ≥ 0, notice 0–365 days, penalty 0–100 %, agreement ≤20000 |

**Benefit:** malformed requests are rejected with clean 400s *before* reaching services — previously `UpdateLeaseRequest` would happily accept `EndDate < StartDate` or a negative rent.

### 6.2 Duplicated PDF validation → `FileValidation.EnsurePdf()`

**New:** `Common/FileValidation.cs`. The identical 7-line null/empty/PDF check was copy-pasted in `KycService` and `TenantFinancialProfileService` — both now call the one helper. (`PropertyDocumentService` has genuinely different rules — PDF+images, 10 MB cap — so it was correctly left alone.)

### 6.3 TokenService no longer swallows exceptions silently

**File:** `Services/Tokens/TokenService.cs`

**Before:** `catch { return null; }` — an expired signature, wrong issuer, or malformed token all looked identical to "no token", with zero trace. Debugging auth issues meant guesswork.
**After:** `ILogger<TokenService>` injected; both failure paths log a warning with the exception before returning null. Behavior for callers is unchanged.

### 6.4 Role magic strings → `Roles` constants

**New:** `Common/Roles.cs` (`Roles.Admin/Owner/Tenant/Verifier`). Replaced ~20 string literals across 9 services/repositories (role checks, `GetByNameAsync`, `GetUsersByRoleAsync`, role-count lookups). A typo like `"Verifer"` is now a compile error instead of a silent always-false check.

### 6.5 KYC/Financial service dedup — shared `DocumentUploadService`

**New:** `Services/Documents/DocumentUploadService.cs` + `IDocumentUploadService` (registered scoped in `Program.cs`).

**Before:** `KycService.UploadDocumentAsync` and `TenantFinancialProfileService.UploadDocumentAsync` each contained the same ~45-line block: delete old file → save new file → update Document fields, or create Document + add to repo.

**After:** both reduce to:
```csharp
var doc = await _documentUpload.SaveOrReplaceAsync(
    existingLink?.Document, file, documentType, userId, $"users/{userId}/kyc", ct);

if (existingLink == null)
    kyc.KycDocuments.Add(new KycDocument { ... });   // only the join-entity wiring stays local
```
The helper deliberately does **not** call `SaveChangesAsync` — the owning service commits, preserving the single-save-per-operation rule.

**Why not a generic `VerificationService<T>` base class?** The draft/submit/review flows have real domain differences (Aadhaar/PAN uniqueness checks vs. income fields and employment-type-dependent document rules, different eligibility preconditions, different notification copy). Forcing them into one abstraction would trade honest 20% duplication for a leaky generic — the extracted upload block was the genuinely identical part.

---

## 7. Intentionally not changed

| Item | Reason |
|---|---|
| Admin review dialogs fetching item detail on open (N+1-ish) | The list DTOs genuinely lack document URLs; fetching detail on demand is correct |
| Client-side search in admin KYC/financial tabs | Needs a backend search parameter first — backend change tracked as follow-up |
| Pagination caching in property listings | Low value for the current data size |
| Splitting `AdminController` (7 services) into domain controllers | Bigger routing change; deferred |
| Extracting `LeaseSigningService` / `LeaseBillGenerationService` from the 596-line `LeaseService` | Deferred — highest-risk refactor, needs its own pass with e2e signing tests |
| Admin notification fan-out (`foreach` admin → `SendAsync`) | Fine at current admin counts; batch insert is a scale-time optimization |
| `!important` on destructive buttons | Justified against Material's specificity |
| Tenant/owner complaint list response shape | Changing to `PagedResponse` would break 4 frontend call sites; bounded the risky Admin path instead |

---

## Notes & caveats

- **Pre-existing test failures (not caused by this work):** `LocalFileStorageServiceTests.SaveFileAsync_...` and `DeleteFileAsync_...` fail with `NullReferenceException` inside `LocalFileStorageService` (line 36 — `WebRootPath` is null in the test host). File-storage code was not touched in this refactor.
- **Runtime verification done:** backend restarted with the new build; verified `GET /properties` (200), admin login, and `GET /admin/dashboard/stats` (values internally consistent with the new derived-count logic); `GET /admin/financial/summary` returns correct totals via the new `CountAsync`. Owner/tenant flows couldn't be exercised because the dev database's owner/tenant accounts don't use the seed credentials (reseeding was intentionally not run).
- **Behavioral deltas to be aware of (all intended):**
  - Admin/tenant dashboards no longer refetch stats/bills on tab switch — data is as fresh as the last dashboard load (a full page reload or re-navigation refetches).
  - The filter pills in four tabs now share one visual definition (active tint is `color-mix` 8% primary instead of hardcoded `#EDE9F8` — visually near-identical).
  - `GET /complaints` as an **Admin** now returns the most recent 200 instead of the entire table.
  - Stat/summary cards use the standardized radius/label styles from the global block (12px radius, 22px value) — bill summary keeps its 18px/800 value via local override.
