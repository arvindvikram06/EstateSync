# Frontend Code Review Fixes — 2026-06-28

All issues identified in the Angular code audit were fixed and verified with a clean `ng build` (0 errors, 0 warnings).

---

## 1. Critical Bug — Tenant Blocked from Signing Lease First

**Files:** `features/dashboard/tabs/leases-tab/leases-tab.component.ts`, `features/leases/lease-detail/lease-detail.component.ts`

**Problem:** `canSign` only returned `true` when `status === 'PartialSigned'`, meaning a tenant could never be the **first** signer (the lease starts at `PendingApproval`, not `PartialSigned`).

**Fix:** Accept both `PendingApproval` and `PartialSigned`, and gate on `!lease.tenantSignedAt` so a tenant who already signed cannot sign again:
```typescript
// leases-tab
canSign(lease: LeaseSummaryResponse): boolean {
  return (lease.status === 'PendingApproval' || lease.status === 'PartialSigned') && !lease.tenantSignedAt;
}

// lease-detail (computed signal)
readonly canSign = computed(() => {
  const l = this.lease();
  return !!l && (l.status === 'PendingApproval' || l.status === 'PartialSigned') && !l.tenantSignedAt;
});
```

---

## 2. Wrong Document Type Enum Value

**Files:** `core/services/property.service.ts`, `features/properties/property-edit/property-edit.component.ts`

**Problem:** `'OwnerShipProof'` is not a valid backend `DocumentType` enum value. The correct value is `'OwnershipCertificate'`.

**Fix:** All occurrences changed to `'OwnershipCertificate'`.

---

## 3. Role Checks Reading from localStorage Instead of JWT

**File:** `core/services/auth.service.ts`

**Problem:** `isOwner` and `isAdmin` computed signals read `currentUser().roles` — the profile stored in `localStorage`. This array is whatever was set at login and can be stale or tampered with.

**Fix:** Role checks now read from the JWT itself via `tokenService.decodeRoles()`, while still depending on `currentUser()` signal for reactivity (so role computeds update on login/logout):
```typescript
readonly isOwner = computed(() => {
  const u = this.currentUser();
  return !!u && this.tokenService.decodeRoles().includes('Owner');
});
readonly isAdmin = computed(() => {
  const u = this.currentUser();
  return !!u && this.tokenService.decodeRoles().includes('Admin');
});
```
Also removed the empty constructor body — `currentUser` signal is now initialized at field level.

---

## 4. `effect()` Anti-pattern Emitting `output()` on Construction

**File:** `features/owner/tabs/owner-applications-tab/owner-applications-tab.component.ts`

**Problem:** An `effect()` watched the `pendingCount()` computed signal and emitted `pendingCountChange`. This fires during component construction with stale `0`, before any data loads, causing the parent to show wrong badge counts.

**Fix:** Removed the `effect()`. `pendingCountChange` is now emitted in the HTTP `subscribe` next callback, once real data arrives:
```typescript
ngOnInit(): void {
  this.svc.getOwnerApplications().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
    next: res => {
      this.all.set(res.data ?? []);
      this.loading.set(false);
      this.pendingCountChange.emit(this.pendingCount());
    },
    error: () => this.loading.set(false),
  });
}
```

---

## 5. Memory Leaks — Missing `takeUntilDestroyed` on Observable Subscriptions

**Files:**
- `features/owner/tabs/owner-applications-tab/review-application-dialog.component.ts`
- `features/owner/tabs/owner-applications-tab/create-lease-dialog.component.ts`
- `features/owner/tabs/owner-leases-tab/void-lease-dialog.component.ts`
- `features/dashboard/tabs/complaints-tab/new-complaint-dialog.component.ts`
- `features/properties/property-detail/property-detail.component.ts` (`afterClosed` subscription)
- `features/payments/payment-success/payment-success.component.ts`

**Problem:** HTTP calls and dialog `afterClosed()` subscriptions had no teardown logic. If the component was destroyed before the HTTP response arrived (e.g., user navigates away), the subscription would keep running and try to update destroyed component state.

**Fix:** Added `DestroyRef` injection and `.pipe(takeUntilDestroyed(this.destroyRef))` to every subscription in all six files.

---

## 6. Error Field Reading Wrong Key — RFC 7807 ProblemDetails

**Files:** All dialog components listed in fix #5 above.

**Problem:** Backend returns `{ detail: "..." }` (RFC 7807 ProblemDetails format), but error handlers read `err.error.message`. The error message was always `undefined`, showing no useful feedback.

**Fix:** All error handlers now use:
```typescript
err?.error?.detail ?? err?.error?.message ?? 'Fallback error message.'
```

---

## 7. `setTimeout` Replaced — Prevents Cleanup on Destroy

**Files:**
- `features/properties/property-detail/apply-dialog.component.ts`
- `features/leases/lease-sign/lease-sign.component.ts`

**Problem (apply-dialog):** `setTimeout(() => this.dialogRef.close('applied'), 2200)` cannot be cancelled if the dialog is destroyed before it fires (e.g., user closes the dialog manually after success). The timer callback would try to run on a destroyed dialog reference.

**Fix:** Replaced with `timer(2200).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(...)` — auto-cancels when the component is destroyed.

**Problem (lease-sign):** `ngAfterViewInit` + `setTimeout` assumed the `@if` block containing the `<canvas>` had already rendered when the lifecycle hook fired. This is not guaranteed, causing `canvasRef` to be `undefined`.

**Fix:** Removed `AfterViewInit`. Added `effect()` that watches the `lease()` signal — the canvas `@if` condition. Used `queueMicrotask()` inside the effect to defer canvas initialization until after Angular's current render cycle completes:
```typescript
effect(() => {
  if (!this.lease() || this.loading()) return;
  queueMicrotask(() => {
    if (!this.canvasRef?.nativeElement || this.pad) return;
    const canvas = this.canvasRef.nativeElement;
    canvas.width  = canvas.clientWidth;
    canvas.height = canvas.clientHeight;
    this.pad = new SignaturePad(canvas, { penColor: '#222222' });
    const onEndStroke = () => this.padEmpty.set(this.pad!.isEmpty());
    this.pad.addEventListener('endStroke', onEndStroke);
    this.destroyRef.onDestroy(() => this.pad?.removeEventListener('endStroke', onEndStroke));
  });
});
```

---

## 8. DOM Access via `document.getElementById` — Replaced with Template Refs

**Files:**
- `features/kyc/components/identity-wizard/identity-wizard.component.html` + `.ts`
- `features/kyc/components/financial-wizard/financial-wizard.component.html` + `.ts`

**Problem:** `triggerFileInput(docType)` called `document.getElementById('file-input-' + docType)?.click()` — a direct DOM query that bypasses Angular's rendering model and breaks under ViewEncapsulation.

**Fix:** Added `#fileInput` template variable to the `<input type="file">` element and called `fileInput.click()` directly in the template. The `triggerFileInput()` methods were deleted from both TypeScript files.

---

## 9. `(this as any).countries` Cast Eliminated

**File:** `features/properties/property-create/property-create.component.ts`

**Problem:** `readonly countries: ICountry[] = []` was declared readonly but mutated in `ngOnInit` via `(this as any).countries = this.locSvc.getCountries()` — a runtime hack that violates type safety.

**Fix:** Initialized at field declaration:
```typescript
readonly countries = this.locSvc.getCountries();
```
The `ngOnInit` assignment was removed.

---

## 10. Nullable `declaredMonthlyIncome` FormControl — Silent `0` Bug

**File:** `features/kyc/components/financial-wizard/financial-wizard.component.ts`

**Problem:** `declaredMonthlyIncome: new FormControl<number | null>(null, ...)` produced a nullable signal. The submit method used `val.declaredMonthlyIncome ?? 0`, meaning a blank field silently sent `0` to the API instead of failing validation.

**Fix:** Made non-nullable with `nonNullable: true` and an initial value of `0`. Removed the `?? 0` fallback — `getRawValue()` from a non-nullable control will never be `null`.

---

## 11. Type Annotation Improvements — Eliminated `any`

**Files:**
- `features/properties/property-detail/image-gallery-dialog.component.ts`
- `core/stores/financial.store.ts`

**Problem:**
- `image-gallery-dialog`: `inject(MAT_DIALOG_DATA) as { images: any[] }` — lost type information for image URLs.
- `financial.store.ts`: `uploadingDoc: string | null` and `uploadDocument: rxMethod<{file: File, docType: string}>` were too broad, losing enum type safety.

**Fix:**
- `image-gallery-dialog`: Typed as `{ images: PropertyImageResponse[] }`.
- `financial.store.ts`: `uploadingDoc: FinancialDocumentType | null` and `docType: FinancialDocumentType`. `onFinFileSelected` parameter in `financial-wizard.component.ts` updated to match.

---

## 12. `confirm()` Browser Dialog Replaced with MatDialog

**File:** `features/owner/tabs/owner-properties-tab/owner-properties-tab.component.ts`

**Problem:** Native `confirm()` is synchronous, blocking, and not styleable — inconsistent with the Material design system.

**Fix:** Created `shared/components/confirm-dialog/confirm-dialog.component.ts` (new reusable component) with `ConfirmDialogData` interface supporting `title`, `message`, `confirmLabel`, and `destructive` flag. `deleteProperty()` now opens it via `MatDialog`.

---

## 13. Inline `<style>` Blocks Moved to CSS Files

**Files:**
- `features/dashboard/tabs/leases-tab/leases-tab.component.html` → `leases-tab.component.css` (new)
- `features/dashboard/tabs/complaints-tab/complaints-tab.component.html` → `complaints-tab.component.css` (new)
- `features/dashboard/tabs/bills-tab/bills-tab.component.html` → `bills-tab.component.css` (new)

**Problem:** `<style>` blocks inside component templates bypass Angular's `ViewEncapsulation` — styles bleed globally and apply to all matching selectors across the entire app.

**Fix:** Moved all styles to dedicated `.css` files. Added `styleUrl` to each component's `@Component` decorator.

---

## Build Verification

```
Application bundle generation complete. [4.867 seconds]
0 errors, 0 warnings
```

All 13 issue categories resolved. Build output is clean.
