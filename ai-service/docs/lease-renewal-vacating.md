---
title: Lease Renewal, Vacate Notices, Expiry, and Early Exit
version: 1.0
roles: [Tenant, Owner]
category: lease-lifecycle
action_id: open-leases
---

# Renewal request

Before an Active lease expires, the tenant can request renewal while the lease is eligible. Open the lease and submit the renewal request. The request notifies the owner but does not extend the lease by itself. Submit early enough for the owner to respond before the existing end date and before availability automation reaches later lifecycle stages.

Only the current eligible lease should be renewed. A duplicate pending renewal is unnecessary, and an Expired, Terminated, or Voided lease cannot be extended through the ordinary active-lease renewal flow.

# Owner decision

The owner reviews the request and either approves it with a new end date or rejects it. The new end date must be later than the current end date. Approval extends the same lease; it does not create a second lease and does not require both parties to repeat the original signing flow under the current renewal behavior. Rejection leaves the existing end date unchanged.

# Effects of approval

The tenant is notified of the new end date. Billing continues according to the lease’s payment frequency and extended term. If the property had become Available Soon because the end date was near, successful renewal returns it to the appropriate Leased state. Existing lease history and agreement association remain connected to the same lease.

# Available Soon

When the remaining lease term falls within the configured lifecycle window and no approved continuation prevents it, the property may become Available Soon. This indicates upcoming availability, not that the current tenant must leave immediately. The current lease remains active through its term unless another valid lifecycle action ends it. If renewal is approved, availability returns to Leased.

# Vacate notice

A tenant planning to leave uses Submit Vacate Notice from the Active lease. Enter the intended exit date after reviewing the notice period in the lease. Submitting sends the notice to the owner for a decision; it does not instantly end the lease. The lease continues while the notice is pending and until the accepted effective exit lifecycle completes.

# Notice period and penalties

The lease defines the minimum notice period. If the selected exit date does not provide enough notice, the application can calculate or display the applicable consequence before confirmation. After owner acceptance, an early-exit penalty bill may be created for the exit date and some or all of the security deposit may be forfeited according to the lease terms. The assistant should not invent a penalty amount; users must review the amount displayed for their lease.

# Owner handling

The owner reviews the intended exit date and the information shown with the notice, then Accepts or Rejects while the notice is pending. Acceptance schedules the lease to close according to the accepted date and applies supported billing consequences. Rejection leaves the lease operating under its current terms and notifies the tenant. A decision should be based on the actual lease rather than a different property’s notice period.

# Moving earlier than the accepted date

Physically leaving before the accepted exit date does not automatically close the lease. Rent and other obligations can continue through the agreed date, and the property remains governed by the lease lifecycle until closure. Tenants should select a realistic date and owners should use the recorded notice rather than informal assumptions.

# Expiry and property return

When the lease reaches its end without renewal, it becomes Expired through the lifecycle process and the property can return to an available state. Associated application history can become Completed according to the implemented workflow. Expiry does not erase bills, payments, complaints, or historical documents.

# Deposit expectations

The security deposit is a guarantee governed by the lease. Unpaid dues, supported penalties, damages, or early-exit terms may affect it. EstateSync documentation should not promise a full refund without considering the lease and account obligations. Any remaining handling follows the agreed lease process.

# Common unavailable actions

Renewal may be unavailable when the lease is not Active, a request already exists, or the timing/status is no longer eligible. Vacate Notice may be unavailable for a non-active lease or when another active notice already controls the workflow. Refresh the lease and read its current status before retrying.

