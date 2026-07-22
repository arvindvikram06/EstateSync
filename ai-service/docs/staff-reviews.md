---
title: Authorized Staff Review Workflows
version: 1.0
roles: [Admin, Verifier]
category: staff-reviews
action_id: open-review-dashboard
---

# Staff review areas

Staff members use the review dashboard provided for their work. Identity-verification reviews and broader administrative reviews appear in their corresponding dashboard areas. Customer profile upgrades do not provide access to staff review functions.

# Review principles

Open the relevant dashboard queue and select an item that is still awaiting review. Confirm that the item remains Pending before recording a decision because another reviewer or workflow event may have changed it. Review only the information exposed for that task and avoid copying private documents outside the authorized process.

# KYC review

Review the submitted identity information and required document coverage for readability, consistency, and the acceptance criteria represented by the application. Approve when the submission satisfies the requirements. Reject when a correction is required and provide a clear, non-sensitive reason that tells the user what to fix. A useful reason describes the problem without reproducing full identity numbers or unnecessary document contents.

# Property review

Review submitted property details and private ownership evidence. Approval allows the listing to move to its eligible public state. Rejection should identify a correctable issue such as missing, unclear, inconsistent, or unsupported evidence or listing information. Public photos and descriptions should not expose private documents or credentials.

# Lease final review

Final lease review occurs after the required signatures. Confirm that the signed lease is the item being reviewed and that it is still in the pending-final-review state. Approval activates the lease and triggers downstream property and billing behavior. Rejection prevents activation and notifies the parties with the recorded reason. Because the decision has downstream effects, do not approve an item opened from stale data without confirming its current state.

# Rejection reasons

Reasons should be concise, actionable, and safe to show to the affected user. Do not include another user’s information, internal credentials, secret configuration, or unnecessary full identifiers. Avoid vague statements such as “wrong” when a clearer explanation such as “the uploaded address proof is unreadable” is available.

# Duplicate or already-processed actions

If an item is no longer Pending, do not attempt to review it again. Refresh the queue and open the latest state. Approval and rejection operations should be treated as consequential decisions, not toggles. If an incorrect decision cannot be changed through the supported workflow, escalate through the authorized operational process rather than manipulating unrelated records.

# Privacy during review

Staff should use only the records presented for the review being completed. Do not share sign-in access, export private documents to personal channels, or paste sensitive content into the AI assistant. The assistant can explain the visible review flow but must not make approval decisions or reveal private information.
