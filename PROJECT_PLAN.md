# EstateSync — Master Project Plan

---

## Business Model & Revenue Streams

EstateSync operates as a **property rental marketplace + home services platform**.

### Research Findings (Real World Comparison)

| Platform | Who they serve | Revenue model | Maintenance handling |
|----------|---------------|---------------|---------------------|
| **NestAway (India, defunct)** | Individual owners (B2C) | 8–12% of rent from owner | Platform coordinated maintenance; commission never covered costs → contributed to bankruptcy |
| **NoBroker (India)** | Individual owners + tenants | Per-service marketplace; % for NRI managed plan | Pure pay-per-service via NoBroker Home Services (Urban Company model) |
| **Urban Company (India)** | End consumers | 20–22% commission from service professional per job | Pay per booking; anti-spam built in because every request costs money |
| **Buildium / AppFolio (US)** | **Professional property management companies** (B2B) | Monthly SaaS subscription ($62–$340/month per company) | Software only — PM company uses their own vendors; Buildium dispatches nobody |

**Buildium clarification (important):** Buildium's customers are professional property management companies who manage 50–500 properties each, not individual owners. They pay a SaaS fee; Buildium just provides the software. EstateSync is B2C (individual owners) — closer to what Buildium's *customers do* (property management) than to Buildium itself. Buildium's subscription model only makes sense when one customer manages hundreds of properties.

**Key lesson from research:** No major platform uses "N free requests + pay per extra" metered billing. They either go full SaaS subscription (unlimited routing included, B2B) or full per-transaction (pay each time, B2C marketplace). EstateSync sits between — individual owners, automatic % of rent, optional pay-per-dispatch.

---

### EstateSync Revenue Model (Final)

**Two revenue streams only:**

**1. Platform fee — % of every rent payment (automatic via Stripe Connect)**

```
Tenant pays ₹30,000 rent
  → EstateSync takes 5% = ₹1,500 (application_fee_amount in Stripe)
  → Owner receives ₹28,500 automatically
```

- Applied to ALL active leases, regardless of managed mode
- Deducted at source via Stripe Connect — owner never needs to "pay" separately
- Covers: platform access, KYC, listings, lease e-sign, payment routing, complaint tracking, service request routing

**2. Service dispatch margin (pay per dispatch, owner-triggered)**

```
Tenant raises service request → Owner reviews → Owner decides:
  "I'll handle it myself" → No extra charge (platform fee already covers routing)
  "Dispatch EstateSync worker" → Owner pays ₹X per job (EstateSync's margin over vendor cost)
```

- Owner only pays for dispatching when they explicitly choose it
- EstateSync makes margin: vendor costs ₹300, owner pays ₹499, EstateSync keeps ₹199
- Typical dispatch price range: ₹199–₹799 depending on service type (matches Urban Company pricing)

**Revenue walkthrough per property per year (₹30,000/month rent):**

| Source | Calculation | Amount |
|--------|------------|--------|
| Platform fee | 5% × ₹30,000 × 12 months | ₹18,000 |
| Service dispatch margin | avg 4 dispatches/year × ₹199 margin | ₹796 |
| **Total per active lease/year** | | **~₹18,800** |

At 100 active leases → ~₹18.8 lakh/year
At 500 active leases → ~₹94 lakh/year

**Key principle:** Platform fee is the primary revenue and is unconditional — earned every rent cycle regardless of whether the owner uses dispatch or self-resolves. Service dispatch is pure upside. If owner handles everything himself, EstateSync still earned ₹18,000 for the year just from platform fee.

**Why this works:**
- Platform fee ensures baseline revenue from every active lease, regardless of service usage
- Dispatch margin is pure upside — optional for owners, but owners will use it when the job is beyond their capability
- No spam issue — dispatch costs money, so owners only trigger it for real jobs
- No quota complexity — routing is included in the 5%, dispatching is pay-as-you-go
- No active lease = no revenue = incentivizes EstateSync to help owners find tenants faster

---

### Roles on the Platform

| Role | Who | What they do |
|------|-----|--------------|
| `Admin` | EstateSync superuser | Full access, KYC/lease approval, staff management, create ServiceManager accounts |
| `Owner` | Property owner | Lists properties, manages applications + leases, reviews service requests |
| `Tenant` | Renter | Applies, signs leases, pays rent, raises complaints/service requests |
| `Verifier` | EstateSync KYC staff | Reviews and approves/rejects KYC & financial profile submissions |
| `ServiceManager` | EstateSync ops staff | Receives dispatch requests, updates service request status as work progresses |

> **ServiceManager is software-only.** The platform does not track who physically does the work (employed staff or partner vendor — that is an operations decision). ServiceManager simply updates the status (Acknowledged → Scheduled → InProgress → Completed) as the job progresses. This keeps the platform simple and the business model flexible.

---

### Service Request Flow (Final)

```
Tenant raises service request (ServiceType + Priority + Description)
  → Owner gets notification
  → Owner reviews and decides:

  Option A — "I'll handle it"
    → Owner self-resolves, updates status to Completed
    → No extra charge. Platform fee already covered this routing.

  Option B — "Dispatch EstateSync"
    → Owner clicks "Request Dispatch"
    → Admin/Verifier gets notified, assigns vendor
    → Vendor updates status: Acknowledged → Scheduled → InProgress → Completed
    → Owner billed per-job (LeaseBill for owner, LeaseBillType.ServiceDispatch)
    → Tenant notified at each stage

Emergency requests (Priority = Emergency):
  → Bypass cooldown
  → Owner + Admin both notified simultaneously
  → SLA: 2–4 hours dispatch
```

**Service types:** `Plumbing, Electrical, Carpentry, Cleaning, PestControl, Painting, ApplianceRepair, General`
**Priority:** `Low (1 week), Medium (48–72h), High (24h), Emergency (2–4h)`

---

### What Managed Mode Actually Means Now

After research and discussion, **there is no separate "managed mode" toggle.** The model is simpler:

- All properties pay 5% platform fee (always)
- All owners can self-resolve or request dispatch (always)
- The difference is purely operational — EstateSync admin handles dispatch when owner requests it

No `IsManaged` field. No tier split. One model for everyone. Owner behavior determines how involved EstateSync is, not a toggle.

---

## Complete Feature Flows

### Flow 1 — Registration & Auth
```
POST /api/v1/auth/register → default role: Tenant
  → Hash password (bcrypt)
  → Generate JWT + refresh token (httpOnly cookie)
  → Return AuthResponse

POST /api/v1/auth/login → same shape

POST /api/v1/auth/refresh-token (cookie)
  → Rotate refresh token, issue new JWT
```

### Flow 2 — KYC (Identity Verification)
```
POST /api/v1/kyc/draft → status: Draft
PUT  /api/v1/kyc/{id}/documents → upload AadhaarFront, AadhaarBack, PanCard, AddressProof
POST /api/v1/kyc/{id}/submit → status: Pending
PUT  /api/v1/admin/kyc-requests/{id}/review [Admin/Verifier]
  → Approved: User.IsKycVerified = true
  → Rejected: user notified with reason
```
**Business rule:** KYC must be Approved before tenant can apply to any property or owner can create listings.

### Flow 3 — Owner Onboarding
```
POST /api/v1/users/me/become-owner
  → Requires IsKycVerified = true
  → Assigns "Owner" role

POST /api/v1/payments/owner-onboard
  → Creates Stripe Connect account
  → Returns onboarding URL
  → Stripe webhook: sets IsOwnerVerified = true on account.updated
```
**Revenue touch:** EstateSync can add a Stripe Connect platform fee (% of each rent payment routed through Stripe).

### Flow 4 — Property Listing
```
POST /api/v1/properties → status: Draft
POST /api/v1/properties/{id}/images
POST /api/v1/properties/{id}/amenities
POST /api/v1/properties/{id}/documents (ownership docs)
PUT  /api/v1/properties/{id}/submit → status: PendingApproval
PUT  /api/v1/admin/properties/{id}/approval [Admin]
  → Approved: status = Active → visible in public listing
  → Rejected: owner notified
```

### Flow 5 — Rental Application
```
POST /api/v1/RentalApplications/properties/{propertyId} [Tenant]
  → Requires IsKycVerified = true
  → Property must be Active
  → Creates RentalApplication: status = Pending

PUT /api/v1/RentalApplications/{id}/review [Owner]
  → Approved: one application approved per property
  → Rejected: tenant notified
```

### Flow 6 — Lease E-Sign Lifecycle
```
POST /api/v1/leases [Owner]
  → Requires application Approved + tenant KYC verified
  → status: PendingApproval
  → Auto-generates agreement PDF (QuestPDF)
  → Tenant notified

PUT  /api/v1/leases/{id} [Owner] — update terms (PendingApproval only)
PUT  /api/v1/leases/{id}/sign [Owner or Tenant]
  → Body: { signatureText, signatureImageBase64 }
  → After one party signs: status = PartialSigned
  → After both sign: status = PendingAdminApproval
    → SignatureVerificationHash generated (SHA256)
    → Signed PDF generated (embeds both signatures + hash)
    → Admin notified

POST /api/v1/leases/{id}/void [Owner] — only PendingApproval or PartialSigned
POST /api/v1/leases/{id}/terminate [Owner/Admin] — only Active leases ← PLANNED
  → If within lock-in period: generates early termination penalty bill
  → status = Terminated, property = Active
  → Both parties notified

PUT /api/v1/admin/leases/{id}/approval [Admin]
  → Approved: status = Active, property = Leased
    → Generates: Security Deposit bill + First Rent bill
    → Rejects all other pending applications for property
  → Rejected: status = Voided
```

**Lease Status State Machine:**
```
PendingApproval → PartialSigned → PendingAdminApproval → Active → Expired (auto)
PendingApproval or PartialSigned → Voided (owner voids)
Active → Terminated (manual, planned)
Active → Expired (LeaseExpiryScheduler, automatic)
```

**Early Termination Rules (when Active → Terminated):**
- Security deposit is **forfeited** — not returned to tenant. Owner keeps it as compensation.
- `Lease.IsDepositForfeited = true` set inside `TerminateLeaseAsync()`
- Notification to tenant: "Security deposit forfeited due to early lease termination"
- Notification to owner: "Security deposit is retained as per lease terms"
- No new payment generated — deposit `LeaseBill` already exists and is Paid; just update the flag

**Normal Expiry Rules (when Active → Expired via scheduler):**
- Platform sends notification to owner: "Please return security deposit to tenant within 7 days"
- Actual refund is an offline bank transfer — platform reminds only, does not process it
- `Lease.IsDepositForfeited` remains `false`

### Flow 7 — Payments (Rent Collection)
```
GET  /api/v1/payments/tenant/bills → all LeaseBill records for tenant
POST /api/v1/payments/checkout/{leaseBillId}
  → Creates/retrieves Stripe customer
  → Creates Stripe checkout session
  → Returns URL → redirect to Stripe

POST /api/v1/payments/webhook [Stripe]
  → checkout.session.completed: LeaseBill.PendingAmount = 0, Payment record created
    → Notifications: PaymentReceived to tenant + owner (PLANNED — not yet sending)
  → account.updated: IsOwnerVerified = true
  → payout.paid: OwnerSettlement record created
```

**Revenue touch:** Platform fee can be added to checkout session as a line item.

### Flow 8 — Complaints
```
POST /api/v1/complaints [Tenant] — requires active lease for that property
  → Notification to owner: ComplaintRaised (PLANNED)

POST /api/v1/complaints/{id}/images
PUT  /api/v1/complaints/{id}/status
  → Open → InProgress → Resolved → Closed
  → Notifications to tenant: ComplaintUpdated, ComplaintResolved (PLANNED)
```

### Flow 9 — Service Requests (PLANNED — Phase 5)
```
POST /api/v1/service-requests [Tenant]
  → Body: { serviceType, priority, description, preferredDate }
  → Requires active lease
  → Creates ServiceRequest: status = Open
  → Notifies owner

PUT /api/v1/service-requests/{id}/self-resolve [Owner]
  → Owner marks as resolved themselves
  → status = Completed, ResolvedBy = Owner
  → No extra charge

POST /api/v1/service-requests/{id}/request-dispatch [Owner]
  → Owner requests EstateSync to handle it
  → status = DispatchRequested
  → Admin notified
  → Generates LeaseBillType.ServiceDispatch bill for owner (priced by service type)

PUT /api/v1/service-requests/{id}/status [Admin, ServiceManager]
  → DispatchRequested → Acknowledged → Scheduled → InProgress → Completed
  → Tenant notified at each stage

GET /api/v1/service-requests/tenant [Tenant] — my requests
GET /api/v1/service-requests/property/{propertyId} [Owner] — per property
GET /api/v1/service-requests [Admin] — all, filterable by status
```

**ServiceType enum (planned):**
`Plumbing, Electrical, Carpentry, Cleaning, PestControl, Painting, ApplianceRepair, General`

**ServiceRequestStatus enum (planned):**
`Open, DispatchRequested, Acknowledged, Scheduled, InProgress, Completed, Cancelled`

**ServicePriority enum (planned):**
`Low` (1 week SLA), `Medium` (48–72h), `High` (24h), `Emergency` (2–4h)


### Flow 10 — Notifications
```
GET /api/v1/notifications [Authenticated] — returns unread notifications (PLANNED)
PUT /api/v1/notifications/read [Authenticated] — marks all read (PLANNED)
```

### Flow 11 — Background Schedulers

| Scheduler | Runs | What it does | Status |
|-----------|------|--------------|--------|
| `PropertyStatusUpdateScheduler` | Every 24h (on startup) | Active leases ending ≤3 months → property `Leased→AvailableSoon` + LeaseExpiring notification | ✅ Running |
| `LeaseExpiryScheduler` | Every 24h (1h after startup) | Expired leases → `Expired`, property → `Active` + LeaseExpired notification | ✅ Running |
| `LeaseBillScheduler` | Every 24h | Active leases → generate recurring Rent bills by PaymentFrequency | ✅ Complete, ❌ disabled |
| `PenaltyScheduler` | Every 24h (2h after startup) | Overdue Rent bills → generate Penalty bill at `PenaltyPercentage` | ❌ Not implemented |
| `RentDueReminderScheduler` | Every 24h | Bills due in 3 days → RentDue notification to tenant | ❌ Not implemented (optional) |

---

## Known Technical Debt & Bugs

| Issue | Severity | Location |
|-------|----------|----------|
| Stripe webhook no idempotency check — duplicate Payment records on retry | CRITICAL | `PaymentService.HandleWebhookAsync` |
| No unique constraint on `Payment.TransactionId` | CRITICAL | `Models/Payment.cs` |
| Secrets (Stripe key, JWT secret) in `appsettings.json` | CRITICAL | `appsettings.json` |
| No unique constraint on `StripePayoutId` in OwnerSettlement | HIGH | `Models/OwnerSettlement.cs` |
| Race condition: duplicate rental applications | HIGH | `RentalApplicationService` |
| Race condition: two active leases per property possible | HIGH | `LeaseService.AdminApproveLeaseAsync` |
| Full Aadhaar/PAN returned in KYC API response (PII) | HIGH | `DTOs/Kyc/KycVerificationResponse.cs` |
| 10 request DTOs missing FluentValidation validators | MEDIUM | `Validators/` |
| `LeaseBill` has no `Status` field — paid state inferred from `PendingAmount > 0` | MEDIUM | `Models/Entities/LeaseBill.cs` |
| Lease renewal is a boolean flag — no proper state machine | MEDIUM | `Models/Entities/Lease.cs` |
| `IPropertyService.GetReviewsAsync()` stub returns empty — ISP violation | LOW | `Services/Properties/PropertyService.cs` |

---

## Phased Implementation Roadmap

### Phase 1 — Backend Completion (CURRENT — ~1 day)

Complete all backend gaps so the API is fully functional before any frontend work.

**Tasks:**
1. Uncomment `LeaseBillScheduler` in `Program.cs` line 188 (1 line)
2. Add `GetOverdueBillsWithLeaseAsync()` to `ILeaseBillRepository` + `LeaseBillRepository` (with `Include(Lease).ThenInclude(LeaseBills, Tenant)`)
3. Create `Services/Background/PenaltyScheduler.cs` — daily, generates `LeaseBillType.Penalty` bills for overdue rent bills using `Lease.PenaltyPercentage`
4. Add `IsDepositForfeited bool` to `Lease` entity + EF migration
5. Implement `TerminateLeaseAsync()` in `ILeaseService` + `LeaseService` — sets `Terminated`, resets property to `Active`, sets `IsDepositForfeited = true`, sends deposit-forfeited notifications
6. Add `POST /api/v1/leases/{id}/terminate` in `LeasesController` [Owner, Admin]
7. Create `DTOs/Leases/TerminateLeaseRequest.cs` + `Validators/TerminateLeaseRequestValidator.cs`
8. Add deposit-return reminder notification in `LeaseExpiryScheduler` — notify owner to return deposit on normal expiry
9. Inject `INotificationRepository` in `ComplaintService` — send `ComplaintRaised` to owner on create, `ComplaintUpdated` + `ComplaintResolved` to tenant on status change
10. Inject `INotificationRepository` in `PaymentService` — send `PaymentReceived` to tenant + owner in webhook
11. Create `Controllers/NotificationsController.cs` — `GET /api/v1/notifications` + `PUT /api/v1/notifications/read`
12. Create `DTOs/Notifications/NotificationResponse.cs`

---

### Phase 2 — Tenant Dashboard (Full Content) (~3–4 days)

Replace all placeholder tabs with real API-connected content.

| Tab | Content | API |
|-----|---------|-----|
| Overview | KYC status, active lease summary, pending bills count, open complaints count | Multiple |
| Applications | My applications with status badges, property name, applied date | `GET /api/v1/RentalApplications/tenant` |
| Leases | Current + past leases, active lease shows rent due date, link to sign page | `GET /api/v1/leases/tenant` |
| Bills | All LeaseBills (Paid/Pending/Overdue), Pay button for pending | `GET /api/v1/payments/tenant/bills` |
| Complaints | My complaints list, file new complaint button, status badges | `GET /api/v1/complaints` |
| Service Requests | Placeholder — Phase 7 | — |

**New pages:**
- Lease Detail (`/leases/:id`) — terms, signature status, download PDF
- Lease Sign (`/leases/:id/sign`) — signature pad canvas, submit base64 PNG
- Payment redirect (`/payments/success`, `/payments/cancel`)

---

### Phase 3 — Owner Dashboard (~3–4 days)

| Section | Content | API |
|---------|---------|-----|
| My Properties | Grid with status badges, quick actions | `GET /api/v1/properties/me` |
| Create/Edit Property | Multi-step form: info → images → amenities → documents → submit | POST/PUT/image/amenity/doc endpoints |
| Rental Applications | Per-property list, Approve/Reject buttons | `GET /api/v1/RentalApplications/owner` |
| My Leases | Active, pending, terminated leases with tenant names | `GET /api/v1/leases/owner` |
| Complaints | Complaints filed against my properties, update status | `GET /api/v1/complaints` |
| Service Requests | Placeholder — Phase 7 | — |

**New pages:**
- Property Create/Edit wizard (`/owner/properties/new`, `/owner/properties/:id/edit`)
- Property Management detail (`/owner/properties/:id`) — images, amenities, documents tabs

---

### Phase 4 — Public Property Detail Page (~1–2 days)

```
/properties/:id
- Image gallery (carousel)
- Title, location, type, bedrooms, rent + frequency
- Amenities chips
- Owner display name only
- "Apply to Rent" CTA (login required)
```

---

### Phase 5 — Admin Panel (~3–4 days)

```
/admin (lazy-loaded, Admin role guard)
- KYC Review Queue — Pending submissions, Approve/Reject with reason
- Financial Profile Review Queue — same pattern
- Property Approval Queue — pending properties
- Lease Approval Queue — PendingAdminApproval leases, view signed PDF
- Staff Management — create Verifier + ServiceManager accounts, deactivate
- Audit Logs — filterable by entityType, entityId, action
```

---

### Phase 6 — Notifications UI (~1–2 days)

- Bell icon in navbar with unread count badge
- Dropdown: last 5 unread with type icon + message + timestamp
- "Mark all read" button
- `/notifications` full page with paginated list

---

### Phase 7 — Service Request Module (~4–5 days) ← LAST

Only after Phases 1–6 are complete and stable.

**Backend:**
- New `ServiceRequest` entity + EF migration
- Enums: `ServiceType` (`Plumbing, Electrical, Carpentry, Cleaning, PestControl, Painting, ApplianceRepair, General`), `ServiceRequestStatus` (`Open, DispatchRequested, Acknowledged, Scheduled, InProgress, Completed, Cancelled`), `ServicePriority` (`Low, Medium, High, Emergency`)
- `ServiceRequest` fields: `ServiceRequestId`, `LeaseId`, `TenantId`, `ServiceType`, `Priority`, `Description`, `Status`, `PreferredDate`, `ResolvedByOwner bool`, `Notes`, `CreatedAt`, `UpdatedAt`
- `IServiceRequestService` + `ServiceRequestService` + `IServiceRequestRepository` + `ServiceRequestRepository`
- `ServiceRequestsController` at `/api/v1/service-requests`
- Add `LeaseBillType.ServiceDispatch` to enum — billed to owner when EstateSync dispatches
- Add `ServiceRequestRaised`, `ServiceRequestUpdated`, `ServiceRequestCompleted` to `NotificationType` enum
- Add `ServiceManager` to role seeding

**Frontend:**
- Tenant: Service Requests tab — list + "New Request" form
- Owner: Service Requests section — list, "Resolve myself" button, "Request EstateSync Dispatch" button
- ServiceManager dashboard: assigned dispatch requests, status update controls
- Admin: all service requests, assign to ServiceManager

---

## Current Implementation Status

### Backend
| Area | Status |
|------|--------|
| Auth (register, login, refresh) | ✅ Complete |
| KYC flow | ✅ Complete |
| Financial Profile flow | ✅ Complete |
| Owner onboarding (Stripe Connect) | ✅ Complete |
| Property CRUD + approval | ✅ Complete |
| Property images/amenities/documents | ✅ Complete |
| Rental Applications | ✅ Complete |
| Lease lifecycle (create, sign, void, admin approval) | ✅ Complete |
| Lease termination (mid-tenure) | ❌ Missing |
| Payment (Stripe Checkout + webhook) | ✅ Complete (has idempotency bug) |
| Complaints CRUD | ✅ Complete (no notifications) |
| LeaseBillScheduler | ✅ Complete, ❌ disabled |
| PenaltyScheduler | ❌ Missing |
| Complaint notifications | ❌ Missing |
| Payment notifications | ❌ Missing |
| Notifications endpoint | ❌ Missing |
| Service Requests | ❌ Not started |
| Admin actions | ✅ Complete |
| Audit Logs | ✅ Complete |

### Frontend
| Area | Status |
|------|--------|
| Auth pages (login, register) | ✅ Complete |
| Navbar with search | ✅ Complete |
| Property listing page (filters, pagination) | ✅ Complete |
| Property detail page | ❌ Not started |
| KYC wizard | ✅ Complete |
| Tenant dashboard shell (6 tabs) | ✅ Complete (placeholder content) |
| Tenant dashboard — real content (all tabs) | ❌ Not started |
| Lease sign page | ❌ Not started |
| Payment redirect pages | ❌ Not started |
| Owner dashboard | ❌ Not started |
| Admin panel | ❌ Not started |
| Notifications UI | ❌ Not started |
| Service Requests UI | ❌ Not started |

---

## Tech Stack Reference

**Backend:** ASP.NET Core 8, EF Core, PostgreSQL, Stripe.NET, QuestPDF, BCrypt, FluentValidation, JWT
**Frontend:** Angular 18+, standalone components, signals, NgRx Signal Store, `signature_pad`, `jwtDecode`
**Base URL:** `http://localhost:5118` (backend) / `http://localhost:4200` (frontend)
**Static files:** `http://localhost:5000/uploads/...`

**Test seeds (after POST /api/v1/system/reset-and-seed):**
| Role | Email | Password |
|------|-------|----------|
| Admin | admin@estatesync.com | Password123! |
| Owner | owner@estatesync.com | Password123! |
| Tenant | tenant@estatesync.com | Password123! |
