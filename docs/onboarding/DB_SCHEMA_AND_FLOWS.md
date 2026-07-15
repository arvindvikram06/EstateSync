# EstateSync — Database Schema & User Flows

This document explains the database schema and every major user flow, with diagrams. Diagrams are written in Mermaid — GitHub and the VS Code markdown preview render them automatically.

---

# Part 1 — Database Schema

## Entity overview

All entities live in `backend/EstateSync.Api/Models/Entities/`, configured in `Data/Configurations/`. Primary keys are Guids. Most tables also carry audit columns (CreatedAt/CreatedById, UpdatedAt/UpdatedById, soft-delete DeletedAt/DeletedById) written automatically by the DbContext interceptor, which also records every change into AuditLog.

| Group | Tables |
|---|---|
| Identity & access | User, Role, UserRole, RefreshToken |
| Verification | KycVerification, KycDocument, TenantFinancialProfile, TenantFinancialDocument, Document |
| Owner & payments infra | OwnerProfile, StripeConnectedAccount, StripeCustomerProfile, OwnerSettlement |
| Property | Property, PropertyImage, PropertyDocument, SavedProperty |
| Renting | RentalApplication, Lease, LeaseDocument, RentIncreaseProposal |
| Money | LeaseBill, Payment |
| Support & system | Complaint, ComplaintImage, Notification, AuditLog |

## Identity, roles and verification

```mermaid
erDiagram
    USER ||--o{ USER_ROLE : has
    ROLE ||--o{ USER_ROLE : "assigned via"
    USER ||--o{ REFRESH_TOKEN : owns
    USER ||--o{ KYC_VERIFICATION : submits
    KYC_VERIFICATION ||--o{ KYC_DOCUMENT : contains
    KYC_DOCUMENT }o--|| DOCUMENT : references
    USER ||--o{ TENANT_FINANCIAL_PROFILE : submits
    TENANT_FINANCIAL_PROFILE ||--o{ TENANT_FINANCIAL_DOCUMENT : contains
    TENANT_FINANCIAL_DOCUMENT }o--|| DOCUMENT : references
    USER ||--o| OWNER_PROFILE : "may have"
    USER ||--o| STRIPE_CONNECTED_ACCOUNT : "owner payout account"
    USER ||--o| STRIPE_CUSTOMER_PROFILE : "tenant billing profile"
```

Key points:

- **User** is the single account table for all four roles (Admin, Owner, Tenant, Verifier). Roles are many-to-many through **UserRole**. A user can hold several roles — a tenant who becomes an owner keeps both.
- User carries the verification flags that gate the flows: `IsEmailVerified` (login is blocked until true), `IsKycVerified` (needed to apply for property or become owner), `IsOwnerVerified` (Stripe onboarding complete, needed to list property), `IsFinanciallyVerified` + `VerifiedMonthlyIncome` (needed for income-restricted properties).
- **RefreshToken** stores rotating refresh tokens with a `TokenFamily` so a stolen/reused token revokes the whole family.
- **Document** is the shared file table (type, URL, mime, size, uploader, review status). KYC, financial profiles, properties and leases all link to it through small join tables (KycDocument, TenantFinancialDocument, PropertyDocument, LeaseDocument) rather than duplicating file columns.
- **KycVerification** and **TenantFinancialProfile** both follow the same review pattern: status Draft → Pending → Approved/Rejected, with ReviewedBy, ReviewRemarks, ReviewedAt.

## Property, applications and leases

```mermaid
erDiagram
    USER ||--o{ PROPERTY : owns
    PROPERTY ||--o{ PROPERTY_IMAGE : has
    PROPERTY ||--o{ PROPERTY_DOCUMENT : has
    PROPERTY ||--o{ SAVED_PROPERTY : "saved by users"
    PROPERTY ||--o{ RENTAL_APPLICATION : receives
    USER ||--o{ RENTAL_APPLICATION : "tenant applies"
    RENTAL_APPLICATION ||--o{ LEASE : "can spawn"
    PROPERTY ||--o{ LEASE : "leased via"
    USER ||--o{ LEASE : "tenant signs"
    LEASE ||--o{ LEASE_DOCUMENT : "agreement PDFs"
    LEASE ||--o{ RENT_INCREASE_PROPOSAL : has
    LEASE ||--o{ LEASE_BILL : generates
    LEASE_BILL ||--o{ PAYMENT : "settled by"
    LEASE ||--o{ COMPLAINT : "raised against"
    COMPLAINT ||--o{ COMPLAINT_IMAGE : has
```

Key points:

- **Property** belongs to one owner and carries address, rent, deposit, bedrooms/bathrooms, amenities (string list), the approval trail (ApproveBy, ApproveRemarks) and `RequiresVerifiedIncome`.
- **RentalApplication** links a tenant to a property with a message, expected move-in date and duration. An application *can* have many lease rows historically, but a **partial unique index on Lease.ApplicationId** guarantees only one live lease (status not Expired/Terminated/Voided) exists per application at a time.
- **Lease** is the richest table: term dates, rent, deposit, payment frequency, notice period, penalty percentage, the full e-sign trail for both parties (signature text, image URL, timestamp, IP), a SHA-256 `SignatureVerificationHash`, admin approval trail, cancellation trail, `IsRenewalRequested` and `IsDepositForfeited`. Note: `LeaseStatus` is stored as a **string** in the database.
- **LeaseBill** is one charge (Deposit, Rent, Penalty, Maintenance) with amount, pending amount, due date and the active Stripe session id. **Payment** records each gateway transaction against a bill, including platform fee and raw gateway response.
- **Complaint** hangs off a lease (optionally a specific bill) with type, priority, status and images.

## Money and platform tables

- **StripeConnectedAccount** — the owner's Stripe Connect account and its capability flags (charges enabled, payouts enabled, details submitted). An owner may list property only after onboarding completes.
- **StripeCustomerProfile** — the tenant's Stripe customer id, created lazily at first checkout.
- **OwnerSettlement** — a record of each Stripe payout to an owner.
- **Notification** — per-user in-app notification (type, title, message, entity pointer, read flag). Also the trigger point for emails: email-worthy types get queued to SMTP.
- **AuditLog** — automatic before/after JSON snapshot of every entity change, written by the DbContext interceptor.

---

# Part 2 — User Flows

## Flow 0: The big picture

The platform is a pipeline of gates. Each gate must be passed before the next step unlocks:

```mermaid
flowchart LR
    A[Register +\nverify email] --> B[KYC\napproved]
    B --> C1[Tenant: apply\nfor property]
    B --> C2[Become Owner +\nStripe onboarding]
    C2 --> D[List property →\nadmin approves]
    D --> C1
    C1 --> E[Owner approves\napplication]
    E --> F[Lease created +\nboth parties e-sign]
    F --> G[Admin approves\nlease → Active]
    G --> H[Bills generated →\ntenant pays via Stripe]
    G --> I[Complaints, renewals,\ntermination, expiry]
```

## Flow 1: Registration, email verification and login

New accounts get the Tenant role by default and cannot sign in until the email is verified.

```mermaid
sequenceDiagram
    actor U as User
    participant FE as Angular app
    participant API as AuthService (API)
    participant M as Email queue

    U->>FE: fill register form
    FE->>API: POST /auth/register
    API->>API: hash password (bcrypt), assign Tenant role,\ncreate verification token (24h expiry)
    API->>M: queue verification email
    API-->>FE: 201 "check your inbox" (no tokens issued)
    U->>FE: click link in email (/auth/verify-email?token=...)
    FE->>API: POST /auth/verify-email
    API->>API: mark IsEmailVerified = true
    U->>FE: sign in
    FE->>API: POST /auth/login
    API-->>FE: JWT access token + httpOnly refresh cookie
```

Explained:

- Registration does **not** log the user in. It only sends a verification email; login before verification is rejected with a "please verify" error, and the login page offers a resend link.
- On login the API returns a short-lived JWT access token in the body and sets the refresh token as an httpOnly cookie. The Angular app keeps the access token in memory only; on page reload it silently calls refresh-token (guided by the `es_has_session` localStorage hint) to restore the session. Refresh tokens rotate on every use.

## Flow 2: KYC verification (gate for everything)

```mermaid
stateDiagram-v2
    [*] --> Draft: tenant creates draft
    Draft --> Draft: upload Aadhaar front/back,\nPAN, address proof
    Draft --> Pending: submit
    Pending --> Approved: Admin/Verifier approves
    Pending --> Rejected: Admin/Verifier rejects
    Approved --> [*]: User.IsKycVerified = true
    Rejected --> [*]: user may start again
```

Explained: the user creates a KYC draft, uploads the four document types, then submits. An Admin or Verifier reviews it in the staff portal. Approval flips `User.IsKycVerified`, which unlocks both applying for properties (tenant side) and becoming an owner. The frontend drives this with a four-step wizard where steps can never be skipped forward.

## Flow 3: Tenant financial profile (optional gate)

Same state machine as KYC (Draft → Pending → Approved/Rejected), but with employment details and income documents (salary slips, bank statements). Approval sets `IsFinanciallyVerified` and stores `VerifiedMonthlyIncome`. It is only required to apply for properties flagged `RequiresVerifiedIncome`.

## Flow 4: Becoming an owner and Stripe onboarding

```mermaid
flowchart TD
    A[KYC-verified user clicks\n'Become owner'] --> B[Owner role assigned]
    B --> C[POST /payments/owner-onboard\ncreates Stripe Connect account]
    C --> D[User redirected to Stripe\nhosted onboarding]
    D --> E[Stripe webhook: account updated]
    E --> F{charges + payouts\nenabled?}
    F -- yes --> G[IsOwnerVerified = true\ncan now list property]
    F -- no --> D
```

Explained: becoming an owner requires KYC first (ForbiddenException otherwise). Listing property additionally requires completed Stripe onboarding, because rent payments flow to the owner's connected account.

## Flow 5: Property listing and approval

```mermaid
stateDiagram-v2
    [*] --> Draft: owner creates listing
    Draft --> Draft: add images, amenities,\nownership documents
    Draft --> PendingApproval: owner submits
    PendingApproval --> Active: admin approves
    PendingApproval --> Rejected: admin rejects (with remarks)
    Active --> Leased: lease activated
    Leased --> AvailableSoon: lease ends within 3 months\n(background scheduler)
    AvailableSoon --> Active: lease expired\n(background scheduler)
    Leased --> Active: lease terminated/expired
```

Explained: a listing starts as Draft where the owner attaches photos, amenities and ownership documents. Submitting moves it to PendingApproval; only an admin can make it Active (publicly searchable). Once a lease on it activates, the property becomes Leased. Two background schedulers manage the tail end: one flips Leased → AvailableSoon three months before the lease ends (and notifies both parties), the other flips it back to Active when the lease actually expires.

## Flow 6: Rental application

```mermaid
sequenceDiagram
    actor T as Tenant (KYC verified)
    actor O as Owner
    participant API as API

    T->>API: POST /RentalApplications/properties/{propertyId}\n(message, move-in date, duration)
    API->>API: checks: tenant KYC verified, property Active,\nincome verified if property requires it
    API->>O: notification "application received"
    O->>API: PUT /RentalApplications/{id}/review (approve/reject)
    API->>T: notification + email (approved/rejected)
```

Explained: only one application per property can be Approved. When a lease is later admin-approved, all other pending/approved applications for that property are automatically rejected.

## Flow 7: Lease lifecycle (the core of the platform)

### State machine

```mermaid
stateDiagram-v2
    [*] --> PendingApproval: owner creates lease\nfrom approved application
    PendingApproval --> PartialSigned: first party signs
    PartialSigned --> PendingAdminApproval: second party signs
    PendingApproval --> Voided: owner voids
    PartialSigned --> Voided: owner voids
    PendingAdminApproval --> Active: admin approves
    PendingAdminApproval --> Voided: admin rejects
    Active --> Expired: end date passes\n(background scheduler)
    Active --> Terminated: terminated early\n(owner decides deposit forfeiture)
    Active --> Active: renewal approved\n(new end date)
```

### End-to-end sequence

```mermaid
sequenceDiagram
    actor O as Owner
    actor T as Tenant
    actor A as Admin
    participant API as API

    O->>API: POST /leases (terms, dates, rent, deposit)
    API->>API: generate agreement PDF (QuestPDF)
    API->>T: notification "lease created — please sign"
    T->>API: PUT /leases/{id}/sign (name + signature PNG)
    Note over API: status = PartialSigned\nsignature image, timestamp, IP stored
    O->>API: PUT /leases/{id}/sign
    Note over API: both signed → status = PendingAdminApproval\nSHA-256 verification hash computed\nsigned PDF generated with both signatures
    API->>A: notification "lease pending approval"
    A->>API: PUT /admin/leases/{id}/approval (approve)
    Note over API: status = Active, property = Leased\nother applications rejected\nSecurity-deposit bill + first rent bill created
    API->>T: notification + email "lease approved"
    API->>O: notification + email "lease approved"
```

Explained, step by step:

1. **Creation** — the owner creates a lease from an approved application. The tenant must be KYC verified. The system generates the agreement PDF immediately. The partial unique index guarantees a second live lease can't be created for the same application (a race returns a conflict error).
2. **Editing** — the owner can update terms only while the status is PendingApproval (before anyone signed); the PDF regenerates.
3. **Signing** — either party signs first. The frontend uses a signature pad canvas; the PNG, typed name, timestamp and IP address are stored per party. After the second signature the system computes a SHA-256 hash over lease id, both party ids and both timestamps, embeds both signatures and the hash into a final signed PDF, and notifies all admins.
4. **Admin decision** — approval activates the lease, marks the property Leased, rejects competing applications, and generates the first two bills (security deposit and first rent). Rejection voids the lease.
5. **Renewal** — the tenant requests renewal on an active lease; the owner either approves (choosing the new end date, which must be later than the current one) or declines. Approval also restores the property from AvailableSoon back to Leased.
6. **Termination** — an active lease can be terminated manually; the owner decides whether the security deposit is forfeited (`IsDepositForfeited`), and both parties are notified accordingly.
7. **Expiry** — a daily background scheduler expires leases past their end date and frees the property.

## Flow 8: Payments (Stripe)

```mermaid
sequenceDiagram
    actor T as Tenant
    participant FE as Angular app
    participant API as API
    participant S as Stripe

    T->>FE: open Bills, click Pay on a bill
    FE->>API: POST /payments/checkout/{leaseBillId}
    API->>S: create/reuse Stripe customer,\ncreate checkout session
    API-->>FE: checkout URL
    FE->>S: redirect tenant to Stripe-hosted page
    T->>S: pays with card
    S->>API: webhook (signature-validated)
    API->>API: bill marked Paid, Payment record created
    API->>T: notification + email "payment received"
    S-->>FE: redirect to /payments/success
```

Explained: the API never touches card data — Stripe hosts the payment page. The source of truth for payment success is the **webhook**, not the redirect: the success page is cosmetic, while the webhook (validated by Stripe's signature) updates the bill and writes the Payment row. Owner payouts arrive via Stripe Connect and are recorded as OwnerSettlement rows. Rent-due reminders and penalty bills are produced by background schedulers.

## Flow 9: Complaints

```mermaid
stateDiagram-v2
    [*] --> Open: tenant raises complaint\n(active lease required)
    Open --> InProgress: owner starts work
    InProgress --> Resolved: owner resolves (remarks)
    Resolved --> Closed: closed
    Open --> Closed: closed directly
```

Explained: a tenant with an active lease raises a complaint with a type (maintenance, billing, noise, …), a priority, a description, and optional photos. The property owner sees complaints for their properties, moves them through the statuses and can attach resolution remarks. Both dashboards support filtering, and raising a complaint notifies (and emails) the owner.

## Flow 10: Background jobs (no user involved)

All are hosted BackgroundServices in `Services/Background/` (plus the email dispatcher in `Services/Email/`):

- **PropertyStatusUpdateScheduler** — leases ending within 3 months: property Leased → AvailableSoon, "lease expiring" notifications (fires once per lease).
- **LeaseExpiryScheduler** — leases past end date: lease → Expired, property → Active, "lease expired" notifications. Starts 1 hour after the other scheduler to avoid lock contention.
- **LeaseBillScheduler** — generates the recurring rent bills for active leases according to their payment frequency, with rent-due notifications.
- **PenaltyScheduler** — creates penalty bills for overdue rent, using the lease's penalty percentage.
- **RefreshTokenCleanupScheduler** — prunes expired and long-revoked refresh tokens so the table doesn't grow unbounded (keeps recently revoked ones for the rotation grace window).
- **EmailDispatcher** — continuously drains the in-memory email queue and delivers over SMTP with retries; mail failures never affect business operations.
