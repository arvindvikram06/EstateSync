# SignalR + Angular Signals — What We Built and Why

A complete, honest guide to the live-refresh architecture in EstateSync.

---

## The Problem We Were Solving

SignalR was already set up. The hub connected, the bell badge incremented, notifications appeared in the panel. But **page data never refreshed**. If a tenant requested a site visit, the owner had to manually switch tabs or reload to see it. KYC approval updated the user's verification state but only because it was wired separately. Everything else went stale.

The instinct is to think "just push data over the socket." That's actually the wrong instinct, and understanding why matters.

---

## The Core Pattern: Event-Driven Invalidation

We did NOT push data over SignalR. We push a **tiny signal** — "something in this domain changed" — and the client refetches over REST.

```
Backend → SignalR → frontend gets notification
                 ↓
         DataRefreshService.trigger('visits')
                 ↓
         visits-tab hears it → calls load(silent=true)
                 ↓
         GET /api/v1/site-visits → fresh data
```

This is the industry standard pattern for SaaS dashboards (Notion, Linear, Jira all do variants of this). The alternative — pushing full data payloads over the socket — creates a parallel data path that diverges from your REST API, duplicates serialization logic, and makes the socket a critical dependency for correctness, not just real-time UX.

---

## Architecture Overview

```
SignalR Hub (backend)
    │
    ▼
SignalRService (frontend, root)
    │   1. addNotification() → updates bell/panel
    │   2. refreshDomainsFor(type, entityType) → ['visits'] etc.
    │   3. dataRefresh.trigger('visits')
    ▼
DataRefreshService (root, RxJS Subject bus)
    │   domain-keyed, debounced 400ms
    ▼
Tab component subscribed to 'visits'
    │   load(silent=true) → skips skeleton flash
    ▼
HTTP GET → fresh data rendered
```

---

## The Files

### `data-refresh.service.ts`

The central bus. A single `Subject<RefreshDomain>` that any service can trigger and any component can listen to.

```ts
export type RefreshDomain =
  | 'visits' | 'applications' | 'leases' | 'complaints'
  | 'bills' | 'properties' | 'admin-properties' | 'admin-leases'
  | 'profile';
```

**`REFRESH_GROUPS`** — a declarative lookup table mapping every `NotificationType` to the domains it invalidates:

```ts
const REFRESH_GROUPS: Partial<Record<NotificationType, RefreshDomain[]>> = {
  [NotificationType.SiteVisitRequested]: ['visits'],
  [NotificationType.LeasePendingAdminApproval]: ['leases', 'admin-leases'],
  [NotificationType.VerificationApproved]: ['profile'],
  // ... all types
};
```

**`refreshDomainsFor(type, entityType)`** — exported pure function. Looks up the table, falls back to `['properties', 'admin-properties']` when `entityType === 'Property'` (backend sends `General` + entityType for property approval events), returns `[]` for anything unknown.

**`debounceTime(400)`** on every `on()` subscription — if 3 notifications arrive in 100ms, components get exactly one refetch.

---

### `signalr.service.ts`

After the refactor, this file does exactly one thing: manage the hub connection lifecycle and translate incoming notifications.

```ts
private readonly onNotification = (notification: NotificationResponse): void => {
  this.ngZone.run(() => {
    this.notificationService.addNotification(notification);
    for (const domain of refreshDomainsFor(notification.type, notification.entityType)) {
      this.dataRefresh.trigger(domain);
    }
  });
};
```

That's the entire business logic. Before the refactor it had a 55-line `domainsFor()` switch, a special `if` block for KYC that called `authService` and `kycStore` directly, and a dynamic `await import('rxjs')` for `firstValueFrom`. All gone.

**Why `NgZone.run()`?** SignalR callbacks fire outside Angular's zone. Without this, OnPush components would not detect signal/store changes triggered by SignalR events.

**Connection lifecycle** — driven by a signal `effect()` watching `authService.isLoggedIn()`. Connect on login, disconnect on logout. Guard `if (this.connection) return` prevents duplicate connections. On reconnect, `fetchAllIfStale()` recovers missed notifications (only fetches if last fetch was >30s ago, so a flaky reconnect loop won't hammer the API).

---

### `DataRefreshService` — the bus

```ts
trigger(domain: RefreshDomain): void {
  this.refreshSubject.next(domain);
}

on(domain: RefreshDomain): Observable<void> {
  return this.refreshSubject.pipe(
    filter(d => d === domain),
    debounceTime(400),
    map(() => undefined)
  );
}
```

Simple. No state. No HTTP. Just RxJS plumbing.

---

### `auth.service.ts` — profile domain subscriber

```ts
constructor() {
  this.dataRefresh.on('profile')
    .pipe(switchMap(() => this.syncProfile()))
    .subscribe();
}
```

`switchMap` cancels a previous in-flight `syncProfile()` call if another `'profile'` event fires before the first completes. This is the correct operator here — we only care about the latest verification state, not every intermediate result.

---

### `kyc.store.ts` — profile domain subscriber

```ts
withHooks((store, dataRefresh = inject(DataRefreshService)) => ({
  onInit() {
    dataRefresh.on('profile').subscribe(() => store.loadExisting());
  }
}))
```

`withHooks` is the NgRx Signal Store way to run init logic with injection. `loadExisting` is an `rxMethod<void>` — it uses `switchMap` internally, so concurrent calls are handled safely.

Both `auth.service` and `kyc.store` react to the same `'profile'` trigger independently. Clean separation — each service owns its own response.

---

### Tab components — silent reload pattern

Every tab that needs live data follows this pattern:

```ts
private load(silent = false): void {
  if (!silent) this.loading.set(true);
  this.someService.getData().pipe(
    takeUntilDestroyed(this.destroyRef),
    finalize(() => this.loading.set(false))
  ).subscribe(res => this.data.set(res.data));
}

ngOnInit(): void {
  this.load();
  this.dataRefresh.on('visits')
    .pipe(takeUntilDestroyed(this.destroyRef))
    .subscribe(() => this.load(true));
}
```

`silent = true` skips `loading.set(true)` so the skeleton loader doesn't flash on auto-refresh. The user sees data update in place, not a blank screen followed by data.

`@if`-mounted tabs (Angular's default for `mat-tab-group` with lazy content) have no subscription when they're not visible — zero wasted HTTP calls for tabs the user hasn't opened.

---

### Manual refresh buttons

Some events have no SignalR notification:

| Tab | Why no auto-refresh |
|-----|---------------------|
| Bills | Hangfire generates bills at midnight; Stripe webhooks have variable delay |
| Owner earnings | Stripe settlement timing; no notification type exists |
| Admin KYC | Backend sends no notification when a tenant submits KYC |
| Admin financial profiles | Same — no notification on submission |
| Verifier overview | Same |

For these, a small `mat-icon-button` with a `refresh` icon calls `load()` (non-silent, shows the spinner). Bills tab uses Angular's `output()` signal to tell its parent `DashboardComponent` to reload, because the bills count also drives the sidebar badge.

---

## Pros

**1. One data path.** Data always comes from REST. SignalR is only for "something changed" signals. Your REST API is the source of truth — not a parallel socket feed. This means caching, error handling, pagination, and auth all work exactly the same whether the refresh was triggered by the user or by SignalR.

**2. Tabs only fetch when mounted.** `@if`-gated tabs + `takeUntilDestroyed()` means a user sitting on the Visits tab doesn't trigger refetches in the Leases or Complaints tab when notifications arrive for those domains.

**3. Debounce protects against bursts.** If an admin approves a lease and the backend sends 3 notifications in quick succession (LeaseApprovedByAdmin, PaymentGenerated, TenantNotified), the tab gets exactly one refetch 400ms after the last one.

**4. Uniform pattern.** Every notification — including KYC, which previously was a special case — goes through the same bus. Adding a new domain or new notification type is one line in `REFRESH_GROUPS`. No touching SignalRService.

**5. Testable.** `refreshDomainsFor()` is a pure function. `DataRefreshService` is a thin class. Both are unit-testable without Angular TestBed. The spec file tests the bus in isolation with `fakeAsync`.

**6. SignalRService is small and clear.** After the refactor: connect, push notification, trigger domains. 85 lines. Injects 5 things. No business logic.

---

## Cons (honestly)

**1. Data can still be stale between notification and refetch.** The SignalR event arrives → `debounceTime(400)` → HTTP request → response. That's 400ms + network round-trip. For fast interactions (owner clicks Approve and immediately looks at the tenant's tab) the data might not be there yet. This is inherent to the event-driven invalidation pattern, not a bug in the implementation.

**2. No offline recovery for tab data.** `onreconnected` calls `fetchAllIfStale()` to recover missed *notifications*. But if the user was offline while on the Visits tab and 3 visits were requested, the tab won't know to reload — it'll wait for the next notification. Fix: call `load(true)` on all mounted tabs on reconnect. We didn't implement this.

**3. Debounce means you miss the "which record changed" signal.** We throw away the notification payload after using the type to determine the domain. The tab always refetches the whole list. If you had 500 site visits, you'd re-fetch all 500 instead of patching the one that changed. For most endpoints this is fine (paginated, small datasets), but it's worth knowing.

**4. `withHooks` in `KycStore` is a leaky pattern.** Ideally the store shouldn't know about `DataRefreshService` — that's an infrastructure concern. A cleaner approach: a higher-level "profile effect" outside the store. But that would require a separate Angular effect or service, which adds complexity for marginal gain. The current approach works and is readable.

**5. Root services subscribing forever without cleanup.** `auth.service.ts` and the `withHooks` in `kyc.store.ts` subscribe to `dataRefresh.on('profile')` without `takeUntilDestroyed`. This is intentional — root services live for the app lifetime — but it means if you ever move to a non-root scope, you'd have a subscription leak.

**6. Manual refresh buttons are a UX cop-out for missing notifications.** The "no notification exists for KYC submissions" problem is a backend gap, not a frontend feature. The right fix is to add admin notifications on KYC/financial-profile submission. The manual button is a pragmatic workaround, not a solution.

**7. `switchMap` in `auth.service` can cancel in-flight profile fetches.** If two `'profile'` events fire within the debounce window (400ms), only the second `syncProfile()` call runs. The first is cancelled. In practice this is fine — we only care about the latest state — but it's worth knowing the operator behaviour.

---

## What We Did NOT Do (and why)

**Polling** — rejected. Hammers the API every N seconds regardless of whether anything changed. Wasteful at scale.

**Pushing full data payloads over SignalR** — rejected. Creates a parallel data path separate from REST. Doubles the serialization surface. Makes SignalR a correctness dependency, not just a UX enhancement. If SignalR drops, your REST API still works; if you push data over SignalR, a drop means stale UI forever.

**Granular patch updates** — "only update the one record that changed." Attractive in theory. In practice: you'd need to handle add/remove/update cases in every component, keep local collections in sync, and deal with race conditions between the patched record and any in-flight list fetch. The refetch-the-list approach is simpler and correct. Premature optimisation.

---

## The "same pattern" principle

Before the refactor, the code had two styles for handling post-notification refreshes:

- KYC: direct service calls from `SignalRService` (`authService.syncProfile()`, `kycStore.loadExisting()`)  
- Everything else: `dataRefresh.trigger()` → tab subscribes

Now it's one style: `dataRefresh.trigger()` → interested parties subscribe. `SignalRService` doesn't know about `AuthService`'s profile, `KycStore`'s state, or any tab component. It just fires domain signals. Each subscriber decides what to do with it.

This is the Open/Closed Principle applied to a notification system: adding a new reaction to a domain (say, updating a badge count when `'bills'` fires) means adding a subscription in the right place — not modifying `SignalRService`.
