---
title: Tenant Bills, Rent Payments, Deposits, and Payment Failures
version: 1.0
roles: [Tenant, Owner, Admin]
category: billing-payments
action_id: view-bills
---

# When bills begin

Lease bills are created only after the lease reaches the stage that activates billing. When final lease approval makes the lease Active, EstateSync creates the initial obligations, including the security-deposit bill and first rent bill described by the lease. Later rent bills follow the payment frequency selected in the lease. A created or partially signed lease does not yet produce the same active billing schedule.

# Bill information and statuses

The tenant dashboard’s Bills area lists obligations with their type, amount, due date, and current status. Pending means payment is still due and the deadline has not produced an overdue state. Paid means a successful payment was recorded. Overdue means the due date passed without successful settlement. A bill remains part of account history after payment.

Security deposit, rent, penalty, or other supported bill types should be read according to their displayed label. Paying one bill does not automatically settle a different bill. Before paying, confirm the property or lease, amount, due date, and bill type.

# Paying a bill

Open Bills from the tenant dashboard, select an eligible unpaid bill, and choose Pay. EstateSync redirects to or opens the secure payment flow. Complete the payment using the options provided there and allow the flow to return to EstateSync. The bill changes to Paid only after successful confirmation. Do not close the payment window prematurely or repeatedly click Pay while the first attempt is still processing.

# Successful payment

After confirmation, EstateSync records the payment, updates the bill, and sends the relevant notification. If the browser remains on a completion screen, return to Bills and refresh the record. Paid bills remain visible for history. A receipt or payment-history entry should be accessed from the authenticated application rather than reconstructed from chat.

# Failed or cancelled payment

If payment fails or is cancelled, the bill remains unpaid. No successful payment should be assumed merely because the secure payment page opened. Return to the same bill and retry when ready. Verify the payment information through the secure provider interface. Do not share full card numbers, security codes, banking passwords, one-time codes, or payment links with the assistant.

If the browser shows an uncertain result, first refresh Bills and Payment History. Avoid immediate repeated attempts until the previous result is known, because duplicate attempts can create confusion. If a charge appears externally but the bill remains unpaid after a reasonable waiting period, contact support with the EstateSync bill reference and payment receipt reference—not sensitive credentials.

# Overdue bills and penalties

When an eligible bill passes its due date unpaid, its status can become Overdue and the configured late consequence may be added. The exact amount must come from the bill and lease; the assistant should not calculate or invent it. Paying before the displayed due date is the reliable way to avoid late consequences. Paying an overdue bill does not erase other outstanding bill types.

# Security deposit

The deposit amount comes from the lease and is billed when the lease activates. It is paid through the same secure flow as other eligible bills. The deposit is not ordinary rent and may later be used according to lease terms for unpaid obligations, supported penalties, or damages. A full refund cannot be promised without considering the final lease obligations.

# Payment history

Payment History records successful payments associated with the authorized user and lease. Entries display supported details such as amount and date. A pending or failed attempt should not appear as a successful settlement. If history and bill status seem inconsistent, refresh both before contacting support.

# Fees and owner receipt

EstateSync can apply the platform fee defined by the product’s payment process. The tenant sees the amount required by the secure checkout, while owner-side earnings reflect the settlement rules. The assistant should describe that fees are handled by the platform without disclosing private settlement configuration or claiming an unverified percentage.

# Common reasons Pay is unavailable

The bill may already be Paid, the lease or bill may not belong to the signed-in tenant, the amount may not yet be eligible, or a payment attempt may still be processing. Use the tenant Bills view rather than an owner earnings view. If the bill is missing, verify that the lease is Active and that the expected billing date has been reached.
