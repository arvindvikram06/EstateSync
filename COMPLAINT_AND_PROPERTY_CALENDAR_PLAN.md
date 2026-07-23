# EstateSync Complaint Lifecycle and Property Operations Calendar Implementation Plan

## Objective

Improve two existing EstateSync modules without introducing a large new domain:

1. Turn complaints into a controlled, auditable resolution workflow where an owner cannot permanently close a tenant's complaint.
2. Add a property operations calendar that combines visits, bills, lease dates, vacate dates, and complaint follow-ups.

This plan is designed as a focused MVP that can be implemented quickly and extended later.

---

## Feature 1: Enhanced Complaint Lifecycle

### Current problems

The current complaint statuses are:

```text
Open
InProgress
Resolved
Closed
```

The current implementation has these limitations:

- Owner, tenant, and admin use the same generic status endpoint.
- An owner can select statuses without a strict role-based transition table.
- A tenant can close a complaint but cannot reject an owner's resolution and reopen it.
- `ReviewRemarks` stores only the latest text and overwrites previous context.
- There is no immutable status history.
- There are no `ResolvedAt`, `ClosedAt`, or follow-up timestamps.
- The UI cannot show who changed a complaint or why.

### Required business behavior

An owner does not close a complaint. The owner marks the work as resolved and provides resolution notes.

The tenant then chooses one of two actions:

```text
Confirm resolution → complaint becomes Closed
Problem still exists → complaint becomes Reopened
```

An admin can intervene and force-close a complaint with a mandatory reason.

### Status model

Replace the enum with:

```csharp
public enum ComplaintStatus
{
    Open,
    InProgress,
    Resolved,
    Reopened,
    Closed,
    Cancelled
}
```

`Resolved` means the owner claims the issue has been fixed and is waiting for tenant confirmation.

`Closed` means the tenant accepted the resolution or an admin closed the case.

`Cancelled` means the tenant withdrew a complaint before resolution. It is different from a successfully completed complaint.

### State-transition rules

| Current status | Action | New status | Allowed actor |
|---|---|---|---|
| `Open` | Start work | `InProgress` | Owner, Admin |
| `Open` | Resolve directly | `Resolved` | Owner, Admin |
| `Open` | Cancel | `Cancelled` | Tenant |
| `InProgress` | Mark resolved | `Resolved` | Owner, Admin |
| `Resolved` | Confirm resolution | `Closed` | Tenant |
| `Resolved` | Reopen | `Reopened` | Tenant |
| `Reopened` | Start work | `InProgress` | Owner, Admin |
| `Reopened` | Mark resolved | `Resolved` | Owner, Admin |
| Any nonterminal status | Force close | `Closed` | Admin |

Terminal statuses:

```text
Closed
Cancelled
```

No normal transition is allowed out of a terminal status.

### Complaint entity changes

Add:

```csharp
public DateTime? WorkStartedAt { get; set; }
public DateTime? FollowUpAt { get; set; }
public DateTime? ResolvedAt { get; set; }
public DateTime? ClosedAt { get; set; }
public DateTime? ReopenedAt { get; set; }
public int ReopenCount { get; set; }
public string? ResolutionNotes { get; set; }
```

Keep `ReviewRemarks` temporarily for backward compatibility, but stop using it as the only audit record.

Validation:

- `ResolutionNotes`: required when resolving, maximum 2,000 characters.
- Reopen reason: required, maximum 1,000 characters.
- Admin force-close reason: required, maximum 1,000 characters.
- `FollowUpAt`: optional and must be in the future when assigned.

### Complaint history entity

Create:

```csharp
public class ComplaintHistory
{
    public Guid ComplaintHistoryId { get; set; }
    public Guid ComplaintId { get; set; }
    public Complaint Complaint { get; set; } = null!;
    public Guid ActorUserId { get; set; }
    public User ActorUser { get; set; } = null!;
    public ComplaintStatus? FromStatus { get; set; }
    public ComplaintStatus ToStatus { get; set; }
    public ComplaintAction Action { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

Action enum:

```csharp
public enum ComplaintAction
{
    Created,
    WorkStarted,
    FollowUpScheduled,
    ResolutionProposed,
    ResolutionConfirmed,
    Reopened,
    Cancelled,
    ForceClosed,
    CommentAdded
}
```

Indexes:

```text
ComplaintHistory(ComplaintId, CreatedAt)
Complaint(Status, FollowUpAt)
```

History records are append-only. Do not expose update or delete endpoints for them.

### API changes

Replace normal use of the generic status endpoint with explicit action endpoints:

```text
PUT  /api/v1/complaints/{id}/start
PUT  /api/v1/complaints/{id}/follow-up
PUT  /api/v1/complaints/{id}/resolve
PUT  /api/v1/complaints/{id}/confirm-resolution
PUT  /api/v1/complaints/{id}/reopen
PUT  /api/v1/complaints/{id}/cancel
PUT  /api/v1/complaints/{id}/force-close
POST /api/v1/complaints/{id}/comments
GET  /api/v1/complaints/{id}/history
```

DTOs:

```csharp
public sealed record ScheduleComplaintFollowUpRequest(DateTime FollowUpAt, string? Comment);
public sealed record ResolveComplaintRequest(string ResolutionNotes);
public sealed record ReopenComplaintRequest(string Reason);
public sealed record ForceCloseComplaintRequest(string Reason);
public sealed record AddComplaintCommentRequest(string Comment);
```

The old endpoint:

```text
PUT /api/v1/complaints/{id}/status
```

should either be removed or retained temporarily for compatibility but routed through the same state-machine validation. It must not allow arbitrary status assignment.

### Authorization rules

Tenant:

- View complaints raised for their leases.
- Create and upload initial evidence.
- Add comments.
- Cancel an `Open` complaint.
- Confirm a `Resolved` complaint.
- Reopen a `Resolved` complaint.

Owner:

- View complaints for their properties.
- Add comments.
- Start work.
- Schedule follow-up.
- Mark a complaint resolved.
- Cannot close, cancel, or force-close.

Admin:

- View all complaints.
- Perform owner actions.
- Force-close with a mandatory reason.

Every service method must derive the actor from the authenticated user. Never accept an actor or owner ID from the request body.

### Notifications

Add notification types at the end of the existing enum:

```text
ComplaintWorkStarted
ComplaintFollowUpScheduled
ComplaintResolutionProposed
ComplaintReopened
ComplaintClosed
ComplaintCancelled
ComplaintCommentAdded
```

Notification targets:

| Event | Recipient |
|---|---|
| Complaint created | Owner |
| Work started | Tenant |
| Follow-up scheduled | Tenant |
| Resolution proposed | Tenant |
| Tenant reopens | Owner and Admin |
| Tenant confirms | Owner |
| Tenant cancels | Owner |
| Admin force-closes | Tenant and Owner |
| Comment added | Other participant |

Use SignalR through the existing notification system.

### Frontend changes

#### Tenant complaint view

For a `Resolved` complaint, show:

```text
Owner marked this complaint as resolved
Resolution notes: ...

[Confirm Fixed] [Problem Still Exists]
```

`Problem Still Exists` opens a required-reason dialog and calls the reopen endpoint.

Show:

- Status badge.
- Priority.
- Property and lease.
- Follow-up date.
- Resolution notes.
- Image evidence.
- Timeline/history.
- Add-comment field.

#### Owner complaint view

Actions depend on status:

```text
Open       → Start Work, Mark Resolved
InProgress → Schedule Follow-up, Mark Resolved
Reopened   → Start Work, Mark Resolved
Resolved   → Waiting for Tenant Confirmation
Closed     → Read-only
Cancelled  → Read-only
```

The owner must enter resolution notes before marking a complaint resolved.

#### Admin complaint view

Add:

- Full history timeline.
- Reopen count.
- Force-close dialog requiring a reason.
- Filter for `Reopened`.
- Filter for overdue follow-ups.

### Complaint lifecycle acceptance tests

1. Tenant creates a complaint and owner receives a notification.
2. Owner changes `Open` to `InProgress`.
3. Owner cannot set `Closed`.
4. Owner resolves with mandatory resolution notes.
5. Tenant confirms and status becomes `Closed`.
6. Tenant reopens a resolved complaint with a mandatory reason.
7. Reopening increments `ReopenCount`.
8. Owner receives a reopen notification.
9. Tenant cannot reopen `Closed` or `Cancelled`.
10. Unauthorized users cannot read or update the complaint.
11. Each action creates exactly one history entry.
12. Concurrent invalid transitions return `409 Conflict`.

---

## Feature 2: Property Operations Calendar

### Purpose

The owner currently visits several separate tabs to understand what is happening. The calendar provides one operational view across:

```text
Site visits
Lease bills
Lease start and expiry
Vacate notices
Complaint follow-ups
```

The first version is an aggregation of existing data. Do not duplicate these events into a new calendar-events table.

### Calendar-first delivery order

Build this feature before the complaint lifecycle enhancement:

```text
Owner publishes one-hour visit slots
→ tenant sees only available slots
→ tenant requests a slot
→ owner confirms
→ both parties can reschedule through available slots
→ visits and property events appear in the operations calendar
```

The calendar has two kinds of information:

1. **Stored visit availability** created by an owner.
2. **Derived operational events** read from visits, bills, leases, vacate notices, and later complaint follow-ups.

Do not create duplicate database rows for bills or lease dates.

### Visit-duration decision

Every site visit occupies exactly:

```text
60 minutes
```

Examples:

```text
09:00 → 10:00
10:00 → 11:00
14:00 → 15:00
```

The duration is controlled by the backend and is not supplied by the tenant.

Two visits for the same property may not overlap. A pending or approved visit reserves its complete one-hour period.

### Owner-managed availability

The owner must publish availability before tenants can request visits.

Create:

```csharp
public class PropertyVisitSlot
{
    public Guid PropertyVisitSlotId { get; set; }
    public Guid PropertyId { get; set; }
    public Property Property { get; set; } = null!;
    public DateTime SlotDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public int DurationMinutes { get; set; } = 60;
    public VisitSlotStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

Slot statuses:

```csharp
public enum VisitSlotStatus
{
    Available,
    Reserved,
    Booked,
    Blocked
}
```

Meaning:

- `Available`: visible to tenants and can be selected.
- `Reserved`: a tenant request or reschedule proposal is awaiting confirmation.
- `Booked`: the visit is approved.
- `Blocked`: the owner temporarily removed the slot from tenant selection.

Database constraints:

```text
Unique(PropertyId, SlotDate, StartTime)
DurationMinutes = 60
StartTime must be aligned to a full hour
```

Owner slot actions:

```text
POST   /api/v1/properties/{propertyId}/visit-slots
POST   /api/v1/properties/{propertyId}/visit-slots/bulk
GET    /api/v1/properties/{propertyId}/visit-slots/manage
PUT    /api/v1/properties/{propertyId}/visit-slots/{slotId}/block
PUT    /api/v1/properties/{propertyId}/visit-slots/{slotId}/unblock
DELETE /api/v1/properties/{propertyId}/visit-slots/{slotId}
```

Bulk creation example:

```json
{
  "date": "2026-07-28",
  "startTimes": ["09:00", "10:00", "14:00", "15:00"]
}
```

Rules:

- Only the property owner can manage slots.
- Slots must be in the future.
- A booked or reserved slot cannot be deleted or blocked.
- Duplicate slots return `409 Conflict`.
- Tenants never see `Blocked`, `Reserved`, or `Booked` slots as available.

### Tenant availability endpoint

```text
GET /api/v1/properties/{propertyId}/visit-slots/available
```

Query:

```text
from=2026-07-28
to=2026-08-04
```

Response:

```json
[
  {
    "slotId": "guid",
    "date": "2026-07-28",
    "startTime": "09:00",
    "endTime": "10:00",
    "durationMinutes": 60
  },
  {
    "slotId": "guid",
    "date": "2026-07-28",
    "startTime": "14:00",
    "endTime": "15:00",
    "durationMinutes": 60
  }
]
```

The frontend must not render a hard-coded time list. It loads slots from this endpoint after the tenant selects a date.

The backend revalidates slot availability during booking. Frontend availability alone is not sufficient because two tenants can click the same slot simultaneously.

### Site visit model changes

Add:

```csharp
public Guid PropertyVisitSlotId { get; set; }
public PropertyVisitSlot PropertyVisitSlot { get; set; } = null!;
public Guid? RescheduledByUserId { get; set; }
public DateTime? RescheduledAt { get; set; }
public string? RescheduleReason { get; set; }
```

Use explicit pending states:

```csharp
public enum SiteVisitStatus
{
    PendingOwnerApproval,
    PendingTenantApproval,
    Approved,
    Declined,
    Cancelled,
    Completed
}
```

Existing `Pending` rows should migrate to `PendingOwnerApproval`.

### Booking and slot state changes

Tenant booking:

```text
Available slot
→ create SiteVisit(PendingOwnerApproval)
→ slot becomes Reserved
```

Owner approval:

```text
PendingOwnerApproval
→ SiteVisit becomes Approved
→ slot becomes Booked
```

Owner decline:

```text
PendingOwnerApproval
→ SiteVisit becomes Declined
→ slot becomes Available
```

Tenant cancellation:

```text
Pending/Approved visit
→ SiteVisit becomes Cancelled
→ slot becomes Available
```

Completion:

```text
Approved visit
→ SiteVisit becomes Completed
→ slot remains Booked as historical evidence
```

All visit and slot changes must be saved in one database transaction.

### Calendar event contract

```csharp
public sealed record PropertyCalendarEventResponse
{
    public required string EventId { get; init; }
    public required CalendarEventType EventType { get; init; }
    public Guid PropertyId { get; init; }
    public required string PropertyTitle { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public DateTime StartAt { get; init; }
    public DateTime? EndAt { get; init; }
    public required string Status { get; init; }
    public required string Route { get; init; }
    public bool RequiresAction { get; init; }
}
```

Event types:

```csharp
public enum CalendarEventType
{
    SiteVisit,
    RentDue,
    BillOverdue,
    LeaseStart,
    LeaseExpiry,
    VacateDate,
    ComplaintFollowUp
}
```

### Calendar sources

| Source | Date | Condition |
|---|---|---|
| Site visit | `VisitDate + VisitTime` | Pending or Approved |
| Rent/deposit bill | `DueDate` | Pending amount greater than zero |
| Overdue bill | `DueDate` | Due date passed and pending amount greater than zero |
| Lease start | `StartDate` | Relevant lease |
| Lease expiry | `EndDate` | Active or future lease |
| Vacate notice | Effective vacate date | Pending or accepted notice |
| Complaint | `FollowUpAt` | Nonterminal complaint |

`RequiresAction` examples:

- Pending site visit awaiting owner approval.
- Overdue bill.
- Complaint follow-up due today or overdue.
- Renewal or vacate action awaiting owner response.

### API

Owner endpoint:

```text
GET /api/v1/property-calendar/owner
```

Query:

```text
from=2026-07-01
to=2026-07-31
propertyId=<optional-guid>
eventTypes=SiteVisit,RentDue,ComplaintFollowUp
```

Tenant endpoint, if time remains:

```text
GET /api/v1/property-calendar/tenant
```

Tenant events must only include:

- Their site visits.
- Their bills.
- Their lease dates.
- Their vacate dates.
- Their complaint follow-ups.

Validation:

- `from` and `to` are required.
- `to` must be on or after `from`.
- Maximum range is 93 days.
- An owner may only request properties they own.
- Return dates consistently as UTC or with an explicit application timezone policy.

### Repository design

Create focused projection queries rather than loading complete entities:

```csharp
Task<IReadOnlyList<CalendarVisitProjection>> GetOwnerVisitEventsAsync(...);
Task<IReadOnlyList<CalendarBillProjection>> GetOwnerBillEventsAsync(...);
Task<IReadOnlyList<CalendarLeaseProjection>> GetOwnerLeaseEventsAsync(...);
Task<IReadOnlyList<CalendarVacateProjection>> GetOwnerVacateEventsAsync(...);
Task<IReadOnlyList<CalendarComplaintProjection>> GetOwnerComplaintEventsAsync(...);
```

The calendar service:

1. Runs independent queries.
2. Maps them to the shared event contract.
3. Sorts by `StartAt`.
4. Returns a single list.

Avoid N+1 property queries. Every projection should include the property title.

### Rescheduling for owner and tenant

Both the owner and tenant can propose a new available slot:

```text
PUT /api/v1/site-visits/{id}/reschedule
```

Request:

```csharp
public sealed record RescheduleSiteVisitRequest(
    Guid NewSlotId,
    string Reason);
```

Rules:

- Visit must not be `Completed`, `Declined`, or `Cancelled`.
- New slot must belong to the same property.
- New slot must currently be `Available`.
- Reason is required and has a maximum length of 500 characters.
- Release the previous slot.
- Reserve the new slot.
- Tenant proposal changes status to `PendingOwnerApproval`.
- Owner proposal changes status to `PendingTenantApproval`.
- Notify the other party.

Confirmation:

```text
PUT /api/v1/site-visits/{id}/confirm-reschedule
```

- Owner confirms `PendingOwnerApproval`.
- Tenant confirms `PendingTenantApproval`.
- Confirmation changes the visit to `Approved`.
- Confirmation changes the new slot from `Reserved` to `Booked`.

Rejection:

```text
PUT /api/v1/site-visits/{id}/reject-reschedule
```

For the MVP, rejecting a reschedule cancels the proposal, releases the proposed slot, and returns the visit to its previous approved date/time. Therefore, retain:

```csharp
public Guid? PreviousPropertyVisitSlotId { get; set; }
```

until the proposal is confirmed or rejected.

The rescheduling transaction must be concurrency-safe. Only one request can reserve an available slot. A database unique constraint and transaction are the final protection.

Cancellation continues through the existing status flow, but enforce that only the tenant can cancel their request and neither party can cancel a completed visit.

### Visit frontend changes

#### Tenant property page

Replace hard-coded times with:

```text
Select date
→ load available owner-published slots
→ select one-hour slot
→ request visit
```

Show:

```text
09:00 AM – 10:00 AM
10:00 AM – 11:00 AM
```

If no slots exist:

```text
No visit times are available for this date. Try another date.
```

#### Tenant visits tab

For pending or approved visits:

```text
[Reschedule] [Cancel]
```

If the owner proposes a reschedule:

```text
Owner proposed: 28 July, 2:00 PM – 3:00 PM
[Accept New Time] [Reject]
```

#### Owner visits tab

For tenant requests:

```text
[Approve] [Propose Another Time] [Decline]
```

For approved visits:

```text
[Reschedule] [Mark Complete]
```

#### Owner calendar

Add an availability-management mode:

```text
View events | Manage visit slots
```

The owner can select a date and publish or block one-hour slots.

### Bill behavior

Bills are read-only calendar events in this feature.

Do not allow editing bill amounts or due dates from the calendar. Clicking an event navigates to the existing bill or lease view.

Overdue display:

```text
Due today     → warning
Past due      → error
Paid          → omitted by default
```

### Frontend

Add a `Calendar` tab to the owner dashboard.

For the MVP, use an agenda grouped by day rather than adding a large calendar dependency:

```text
Thursday, 23 July

10:00  Site Visit       Green View Apartment
       Pending approval                         [Review]

       Rent Due         Lake Residency
       ₹30,000                                 [View Bill]

14:00  Complaint        Sunrise Villa
       Plumbing follow-up                       [Open]
```

Controls:

- Previous month.
- Current month.
- Next month.
- Property filter.
- Event-type filter.
- `Requires action` toggle.
- Empty state.
- Loading and retry states.

Color mapping:

| Type | Color |
|---|---|
| Site visit | Blue |
| Rent due | Green |
| Overdue bill | Red |
| Lease date | Purple |
| Vacate date | Gray |
| Complaint follow-up | Orange |

Clicking an event routes to an existing detail screen or dashboard tab.

### Calendar acceptance tests

1. Owner sees events only for properties they own.
2. Property filter excludes events from other properties.
3. Date range filters all event sources consistently.
4. Pending visits are marked as requiring action.
5. Paid bills are omitted by default.
6. Overdue bills are marked as requiring action.
7. Closed complaints do not appear.
8. A rescheduled visit moves to the new calendar date.
9. Unauthorized property IDs return `403 Forbidden`.
10. A range over 93 days returns `400 Bad Request`.

---

## Six-Hour MVP Cut

The first six-hour sprint is calendar and visit scheduling only. Complaint lifecycle work starts after this sprint is stable.

### Must have

- Add owner-managed one-hour visit slots.
- Add bulk slot creation for a selected property and date.
- Show tenants only slots whose status is `Available`.
- Reserve slots atomically when a tenant requests a visit.
- Add owner and tenant rescheduling using `NewSlotId`.
- Add owner/tenant confirmation rules for reschedule proposals.
- Release the old or proposed slot correctly on cancellation, decline, and rejection.
- Add owner calendar endpoint.
- Aggregate site visits, unpaid bills, lease start/expiry, and vacate dates.
- Add a simple owner agenda UI with property and event filters.
- Add owner slot-management mode to the calendar.
- Add backend tests for slot conflicts and authorization.

### Should have

- Calendar `RequiresAction` filtering.
- Visit reschedule reasons and notifications.
- Tenant confirmation of owner-proposed reschedules.
- Tenant calendar endpoint.
- Complaint follow-up events after the complaint enhancement is implemented.

### Defer

- Drag-and-drop calendar.
- Google or Outlook synchronization.
- Recurring custom calendar events.
- Recurring weekly availability templates.
- Vendor assignment.
- Maintenance billing.
- Automatic complaint closure.
- SLA escalation jobs.
- SMS and WhatsApp reminders.
- FullCalendar or another heavy UI dependency.

---

## Suggested Implementation Order

### Sprint 1: Calendar and visits

1. Add `PropertyVisitSlot`, slot status, constraints, and migration.
2. Link `SiteVisit` to a slot and migrate existing `Pending` values.
3. Add owner slot CRUD and bulk-create endpoints.
4. Add tenant available-slot endpoint.
5. Change booking to accept `slotId` instead of arbitrary date/time.
6. Add transactional reservation, approval, decline, cancellation, and completion rules.
7. Add owner and tenant reschedule proposal endpoints.
8. Add reschedule confirmation/rejection endpoints.
9. Add calendar event DTO and aggregation service.
10. Add owner calendar endpoint.
11. Replace hard-coded tenant time options with API availability.
12. Add owner slot-management and agenda UI.
13. Add reschedule actions to owner and tenant visit tabs.
14. Run backend tests, frontend tests, and production build.

### Sprint 2: Complaints

1. Add complaint enum values and lifecycle fields.
2. Add `ComplaintHistory`, EF configuration, and migration.
3. Implement a single complaint transition validator.
4. Add explicit complaint action endpoints.
5. Update complaint DTOs and mapping.
6. Add notifications and backend tests.
7. Update tenant complaint confirmation and reopen actions.
8. Update owner resolution actions and timeline.
9. Add complaint follow-ups to the calendar.

---

## Definition of Done

The enhancement is complete when:

- An owner cannot close a complaint.
- An owner can propose a resolution with notes.
- A tenant can confirm the fix or reopen with a reason.
- Every complaint transition is visible in an append-only timeline.
- The owner calendar shows visits, unpaid/overdue bills, lease dates, and complaint follow-ups.
- Calendar events link back to the correct existing workflow.
- Authorization tests prove that tenants and owners cannot access unrelated records.
- Backend tests pass.
- Frontend build passes.
