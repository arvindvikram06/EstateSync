---
title: Lease Creation, Signing, Review, and Activation
version: 1.0
roles: [Tenant, Owner, Admin]
category: leases
action_id: open-leases
---

# Creating a lease

An owner creates a lease from an Approved rental application. Select the correct application and enter the lease terms requested by the form: start date, end date, rent, deposit, payment frequency, and notice period. Dates must form a valid term, amounts must be valid, and the terms should match the agreement between owner and tenant. EstateSync generates a lease agreement document from the saved terms.

# Payment frequency

Payment frequency controls the rent-billing schedule. Supported choices include Monthly, Quarterly, Half Yearly, and Yearly. It does not change the total meaning of the agreed rent field unless the form states that meaning; owners should verify the displayed terms before creation. The frequency later determines when rent bills are generated during an active lease.

# Editing before signatures

The owner can update terms only while the lease is waiting for signatures and neither party has signed. Saving an edit regenerates the agreement so both parties review the same current terms. Once either party signs, terms are locked to prevent one party from changing an agreement after the other accepted it.

If a signed but not yet active lease contains incorrect terms, the supported remedy is to void it where allowed and create a correct lease rather than editing signed content. Active lease terms are not directly edited; an eligible renewal handles extension of the end date.

# Signing order

The tenant signs first and the owner signs after the tenant. Open the lease, review the complete agreement, choose Sign, enter the required name, and provide the signature through the secure signing interface. Do not sign if the displayed terms are incorrect. A typed name or signature submission should be made only by the authorized party.

# Lease statuses before activation

Pending Approval in the lease workflow means the lease has been created and is waiting for required signatures. Partially Signed means one required party has signed. Pending Admin Approval means both parties have signed and the lease is awaiting final platform review. Status naming should be read in the lease context rather than confused with property or application approval.

# Final review

After both signatures, the signed lease goes to authorized final review. The parties are notified of the outcome. Approval changes the lease to Active, changes the property lifecycle appropriately, and generates the initial security-deposit and rent billing obligations described by the billing process. Rejection or refusal at final review notifies the parties with a reason and prevents activation.

# Active, expired, terminated, and voided

Active means the lease is in effect. Expired means its term ended without a continuing approved renewal. Terminated means an active lease was ended early through the supported owner operation. Voided means it was cancelled before activation. These terminal actions cannot be treated as ordinary edits and may have consequences for property availability, applications, bills, and notifications.

# Agreement downloads

The lease page provides the agreement available for the current stage. Before signing, review the draft generated from current terms. After both signatures, the signed copy contains the recorded signatures and can be downloaded for records. Users should obtain lease documents only through their authorized lease view and should not share them publicly.

# Voiding and terminating

Before completion of signing and activation, the owner can void the lease while that action is allowed. After activation, an owner can terminate early through the supported operation. Both notify the tenant and are not reversible ordinary status changes. A tenant who plans to move out should use the vacate-notice workflow rather than assuming that leaving the property automatically terminates the lease.

# Common signing problems

If Sign is unavailable, verify the signing order, current status, and whether the lease is open from the tenant or owner view. The owner cannot sign before the tenant where tenant-first order is enforced. Neither party can sign again after their signature is recorded. If the agreement changed unexpectedly, stop and review the displayed terms; do not sign based only on a previous downloaded draft.
