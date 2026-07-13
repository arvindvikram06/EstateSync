# EstateSync — Lease & Property Status Flows

---

## Lease Status State Machine

```mermaid
stateDiagram-v2
    [*] --> PendingApproval : Owner creates lease
    PendingApproval --> PartialSigned : First party signs
    PendingApproval --> Voided : Owner voids
    PartialSigned --> PendingAdminApproval : Second party signs
    PartialSigned --> Voided : Owner voids
    PendingAdminApproval --> Active : Admin approves
    PendingAdminApproval --> Voided : Admin rejects
    Active --> Terminated : Force terminate / Vacate notice processed
    Active --> Expired : Lease EndDate passes (scheduler)
```

---

## Property Status Transitions

```mermaid
stateDiagram-v2
    [*] --> Active : Property listed & approved
    Active --> Leased : Admin activates lease
    Leased --> AvailableSoon : 3 months before EndDate (scheduler)\nOR no-penalty vacate submitted\nOR owner accepts penalty vacate
    AvailableSoon --> Active : EndDate passes (LeaseExpiryScheduler)\nOR VacateNoticeScheduler fires
    Leased --> Active : Force terminate
    AvailableSoon --> Active : Force terminate
    AvailableSoon --> Leased : Owner approves lease renewal (resets countdown)
```

---

## Full Lease Lifecycle — Creation to Activation

```mermaid
flowchart TD
    A([Owner creates lease]) --> B[PendingApproval\nOwner can edit terms]
    B --> C{Owner voids?}
    C -- Yes --> VOIDED1([Voided])
    C -- No --> D{First party signs}
    D --> E[PartialSigned]
    E --> F{Owner voids?}
    F -- Yes --> VOIDED2([Voided])
    F -- No --> G{Second party signs}
    G --> H[PendingAdminApproval\nAll Admins notified]
    H --> I{Admin decision}
    I -- Approve --> J[Active\nproperty → Leased\nSecurity Deposit bill\nFirst Rent bill\nOther applications → Rejected]
    I -- Reject --> VOIDED3([Voided\nTenant + Owner notified])
```

---

## Active Lease — All End Paths

```mermaid
flowchart TD
    ACTIVE([Active Lease])

    ACTIVE --> PA[Path A\nNatural Expiry]
    ACTIVE --> PB[Path B\nVacate Notice]
    ACTIVE --> PC[Path C\nForce Terminate]
    ACTIVE --> PD[Path D\nRenewal]

    PA --> PA1[PropertyStatusUpdateScheduler\nEndDate ≤ now + 3 months\nproperty → AvailableSoon]
    PA1 --> PA2[LeaseExpiryScheduler\nEndDate < now\nlease → Expired\nproperty → Active]

    PB --> PB1{Penalty?}
    PB1 -- No penalty\nfull notice given --> PB2[property → AvailableSoon immediately\nOwner notified\nNo action needed]
    PB2 --> SCHED[VacateNoticeScheduler\non RequestedExitDate]
    PB1 -- Penalty\nshort notice --> PB3{Owner decision}
    PB3 -- Rejects --> PB4([Lease continues\nTenant may resubmit])
    PB3 -- Accepts --> PB5[Penalty bill created\nproperty → AvailableSoon\nLease stays Active]
    PB5 --> SCHED
    SCHED --> TERM1([Terminated\nproperty → Active\napplication → Completed])

    PC --> PC1{Deposit forfeited?}
    PC1 -- Yes --> PC2([Terminated\nDepositForfeited notification])
    PC1 -- No --> PC3([Terminated\nDeposit return notification])

    PD --> PD1{Owner response}
    PD1 -- Approves new EndDate --> PD2([Active continues\nproperty: AvailableSoon → Leased if needed])
    PD1 -- Rejects --> PD3([Active continues to natural expiry])
```

---

## Path B Deep Dive — Vacate Notice

```mermaid
flowchart TD
    T([Tenant submits vacate notice\nwith RequestedExitDate]) --> CHK{ExitDate ≥ today + NoticePeriodDays?}

    CHK -- YES\nNo Penalty --> NP1[PenaltyApplied = false\nNotice Status = Pending]
    NP1 --> NP2[property: Leased → AvailableSoon\nOwner notified]
    NP2 --> NP3[Wait for RequestedExitDate]
    NP3 --> NP4[VacateNoticeScheduler fires\nNotice: Pending → Accepted\nLease → Terminated\nproperty → Active\napplication → Completed]

    CHK -- NO\nPenalty applies --> P1[PenaltyApplied = true\nPenaltyAmount = Deposit × PenaltyPct%\nNotice Status = Pending]
    P1 --> P2[property: UNCHANGED stays Leased\nOwner notified with penalty amount]
    P2 --> OWN{Owner decision}

    OWN -- Rejects --> REJ[Notice Status = Rejected\nTenant notified\nLease continues unchanged]
    OWN -- Accepts --> ACC[Notice Status = Accepted\nPenalty bill created\nproperty: Leased → AvailableSoon\nLease stays Active until ExitDate]
    ACC --> P3[Wait for RequestedExitDate]
    P3 --> P4[VacateNoticeScheduler fires\nLease → Terminated\nproperty → Active\napplication → Completed]
```

**What the scheduler query matches:**

| Notice Status | PenaltyApplied | ExitDate passed? | Action |
|---|---|---|---|
| `Pending` | `false` | ✅ Yes | Terminate (auto path) |
| `Accepted` | `true` or `false` | ✅ Yes | Terminate (owner accepted) |
| `Pending` | `true` | ✅ Yes | **Ignored** — owner hasn't responded |
| `Rejected` | any | any | **Ignored** — notice is dead |

---

## Background Schedulers

```mermaid
flowchart LR
    subgraph Schedulers
        S1[PropertyStatusUpdateScheduler\nEvery 24h — starts immediately]
        S2[LeaseExpiryScheduler\nEvery 24h — starts 1h after S1]
        S3[VacateNoticeScheduler\nEvery 24h — starts immediately]
        S4[LeaseBillScheduler\nEvery 24h]
        S5[PenaltyScheduler\nEvery 24h]
    end

    S1 -->|Active lease, EndDate ≤ +3mo| R1[property → AvailableSoon\nSend LeaseExpiring]
    S2 -->|Active lease, EndDate passed| R2[lease → Expired\nproperty → Active\nSend LeaseExpired]
    S3 -->|Due vacate notices| R3[lease → Terminated\nproperty → Active\napplication → Completed\nSend LeaseTerminated]
    S4 -->|Recurring billing| R4[Generate Rent bills]
    S5 -->|Overdue bills| R5[Apply late penalty bills]
```

---

## Notifications Reference

| Trigger | Recipients |
|---|---|
| Lease created | Tenant |
| Both parties signed | All Admins |
| Admin approves lease | Tenant + Owner |
| Admin rejects lease | Tenant + Owner |
| Owner voids lease | Tenant |
| Lease expiring (3 months out) | Tenant + Owner |
| Lease expired naturally | Tenant + Owner |
| Vacate notice submitted | Owner |
| Vacate notice accepted | Tenant |
| Vacate notice rejected | Tenant |
| Vacate auto-processed by scheduler | Tenant + Owner |
| Force terminated | Tenant + Owner |
| Renewal requested | Owner |
| Renewal approved | Tenant |
| Renewal rejected | Tenant |
