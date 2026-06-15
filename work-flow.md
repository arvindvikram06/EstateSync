# EstateSync Backend Architecture & Workflows

Welcome to the architectural documentation for **EstateSync Web API**. This document outlines the core domain modules using visual flowcharts, culminating in a detailed, high-level map of our strictly layered, enterprise-grade application architecture.

---

## 1. Authentication & JWT Lifecycle

The Authentication flow ensures secure, stateless access using JWT and Refresh Tokens.

```mermaid
flowchart TD
    A[Client Submits Credentials] --> B{AuthService Validation}
    B -->|Invalid| C[Throw UnauthorizedException]
    B -->|Valid| D[Generate Access Token JWT]
    D --> E[Generate Refresh Token]
    E --> F[(Save Refresh Token to DB)]
    F --> G[Return Tokens to Client]
    
    G --> H{Token Expired?}
    H -->|Yes| I[Client Submits Refresh Token]
    I --> J{Is Token Revoked?}
    J -->|Yes| K[Revoke Entire Family & Throw Exception]
    J -->|No| L[Mark Old Token Revoked]
    L --> D
```

---

## 2. Identity & Owner Verification Flow

Security is paramount. Owners cannot list properties without administrative verification of their documentation.

```mermaid
flowchart TD
    A[Owner Uploads ID & Deed] --> B[Storage Service Saves to S3/Disk]
    B --> C[VerificationRequest Created]
    C --> D((Status: Pending))
    
    E[Admin Dashboard] --> F[Admin Reviews Documents]
    D -.-> F
    
    F --> G{Admin Decision}
    G -->|Approves| H[Set Request = Approved]
    H --> I[Update User: IsIdentityVerified = true]
    
    G -->|Rejects| J[Set Request = Rejected]
    J --> K[Notify Owner to Re-upload]
```

---

## 3. Property Publishing Flow

Properties go through a moderation pipeline to ensure quality and prevent spam.

```mermaid
flowchart LR
    A[Owner Creates Property] --> B((Draft))
    B --> C[Owner Uploads Images]
    C --> D[Submit for Review]
    D --> E((Pending Review))
    
    E --> F{Admin Moderation}
    F -->|Reject| G((Rejected))
    F -->|Approve| H((Approved))
    H --> I[IsAvailable = true]
    I --> J((Live on Search!))
```

---

## 4. Rental Application & Leasing Flow

The core negotiation flow connecting Tenants to Owners.

```mermaid
flowchart TD
    A[Tenant Searches Properties] --> B[Submits Application]
    B --> C((Pending))
    
    C --> D{Owner Review}
    D -->|Reject| E((Rejected))
    D -->|Accept| F((Accepted))
    
    F --> G[Property Locked: IsAvailable = false]
    G --> H[Owner Generates Lease]
    H --> I((Active Lease))
```

---

## 5. Lease Agreement & Financial Flow

Once an application is approved, the legal binding and financial tracking begin. This module requires dual-signatures (Owner and Tenant) before automatically generating the first cycle of Stripe-payable bills.

### **The Lease Flow:**
1. **Lease Generation**: The Owner creates a `Lease` setting the monthly rent, security deposit, and start/end dates. The Lease status becomes `PendingApproval`.
2. **Dual Signature**: The Tenant and the Owner both hit the `SignLease` endpoint to digitally sign the agreement and upload the signed PDF.
3. **Lease Activation**: Once *both* parties have signed, the system automatically sets the Lease to `Active` and changes the Property status to `Leased`.
4. **Auto-Rejection**: The system automatically rejects all other pending applications for this property.
5. **Bill Generation**: The system automatically generates `LeaseBill` entries (one for the Security Deposit, and one for the first Rent cycle based on frequency).
6. **Stripe Checkout**: The Tenant clicks "Pay" on the bill. The `PaymentService` generates a secure Stripe Checkout URL.
7. **Webhook Fulfillment**: Stripe sends a Webhook back to EstateSync upon successful payment, which updates the `LeaseBill` status to `Paid`.

```mermaid
flowchart TD
    A[Owner Creates Lease] --> B((Pending Approval))
    
    B --> C{Both Parties Signed?}
    C -->|No| B
    C -->|Yes| D[Lease Activated]
    
    D --> E[Property Status = Leased]
    D --> F[Auto-Reject Other Applications]
    D --> G[Generate Security Deposit Bill]
    D --> H[Generate 1st Rent Bill]
    
    G --> I[Tenant Redirects to Stripe]
    H --> I
    
    I --> J{Stripe Webhook}
    J -->|Payment Succeeded| K[Update LeaseBill: Paid]
```

---

## 6. Complaints & Maintenance Ticketing

A simple state machine allowing tenants and owners to manage property issues.

```mermaid
flowchart LR
    A((Open)) -->|Owner sends Plumber| B((In Progress))
    B -->|Issue Fixed| C((Resolved))
    C -->|Tenant Approves| D((Closed))
    C -->|Tenant Dissatisfied| A
```

---

# 🏗️ High-Level System Architecture

EstateSync is built on a clean, strictly-layered **N-Tier Architecture**. The flowchart below illustrates the exact path an HTTP Request takes from the moment it hits the server until it is safely written to PostgreSQL.

```mermaid
flowchart TD
    %% Define Styles
    classDef client fill:#f9f,stroke:#333,stroke-width:2px;
    classDef middleware fill:#ffb347,stroke:#333,stroke-width:2px;
    classDef controller fill:#87ceeb,stroke:#333,stroke-width:2px;
    classDef service fill:#98fb98,stroke:#333,stroke-width:2px;
    classDef db fill:#dda0dd,stroke:#333,stroke-width:2px;

    %% Nodes
    Client((Client App / Postman)):::client
    
    subgraph Middlewares [1. ASP.NET Core Middleware Pipeline]
        JWT[JWT Authentication]:::middleware
        EH[Global Exception Handler RFC 7807]:::middleware
    end
    
    subgraph Presentation [2. Presentation Layer]
        CTRL[API Controllers]:::controller
    end
    
    subgraph Business [3. Business Logic Layer]
        DTO[DTOs / Requests]
        MAP[Mappers]
        SRV[Services]:::service
    end
    
    subgraph Data [4. Data Access Layer]
        UOW[Unit of Work Pattern]
        REP[Generic & Specific Repositories]
    end
    
    subgraph Infrastructure [5. Infrastructure Layer]
        DBX[AppDbContext]:::db
        AUDIT[Audit Interceptor]:::db
        PG[(PostgreSQL Database)]:::db
    end

    %% Flow logic
    Client -->|HTTP Request| JWT
    JWT -->|Validates Token| EH
    EH -->|Intercepts Crashes| CTRL
    
    CTRL -->|Binds Data to| DTO
    CTRL -->|Calls| SRV
    
    SRV <-->|Maps Entities| MAP
    SRV -->|Enforces Business Rules| REP
    SRV -->|Commits Transaction| UOW
    
    REP -->|Queries Data| DBX
    UOW -->|SaveChangesAsync| DBX
    
    DBX -->|Trigger OnBeforeSave| AUDIT
    AUDIT -->|Generates Timestamps & Diffs| DBX
    
    DBX -->|Executes SQL| PG
```

### Key Architectural Highlights:
1. **Middlewares**: `GlobalExceptionHandler` intercepts all crashes and returns RFC 7807 standard JSON `ProblemDetails`, hiding stack traces in production.
2. **Controllers**: Kept extremely thin. They handle HTTP routing, role authorization (`[Authorize(Roles="...")]`), and delegate all logic to Services.
3. **Unit of Work & Repositories**: Ensures database atomicity. If a service needs to insert an `Application` and update a `Property`, it happens in a single transaction.
4. **Audit Interceptor**: A low-level hook inside EF Core `AppDbContext` that transparently watches for data mutations and automatically saves snapshots to the `AuditLogs` table without the Service layer even knowing.
