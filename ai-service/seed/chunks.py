FAQ_CHUNKS: list[dict] = [
    {
        "chunk_id": "kyc-create-draft",
        "topic": "Starting KYC (Identity Verification)",
        "content": (
            "To start your identity verification (KYC), go to the KYC section from your dashboard and click Get Started. "
            "You will need to fill in your personal details to create a draft. "
            "Completing KYC is required before you can apply for any rental property or become a property owner on EstateSync."
        ),
    },
    {
        "chunk_id": "kyc-upload-submit",
        "topic": "Uploading KYC Documents and Submitting",
        "content": (
            "Once your KYC draft is created, upload your identity documents — Aadhaar card (front and back), PAN card, and address proof. "
            "Upload each document in the correct slot on the KYC page. "
            "Once all documents are uploaded, click Submit to send your application for review. "
            "You will receive a notification once a decision is made."
        ),
    },
    {
        "chunk_id": "kyc-review-outcome",
        "topic": "KYC Review and Outcome",
        "content": (
            "After submitting your KYC, it moves to a review state. "
            "An admin will check your documents. "
            "If approved, your account is marked as verified and you can proceed with rental activities. "
            "If rejected, you will be notified with a reason. "
            "You can correct the issue and resubmit your documents."
        ),
    },
    {
        "chunk_id": "owner-become-owner",
        "topic": "Upgrading to Owner Account",
        "content": (
            "If you want to list properties on EstateSync, you need to upgrade your account to Owner. "
            "Go to your profile settings and click Become an Owner. "
            "Your KYC must be approved before you can do this. "
            "Once upgraded, you will have access to property listing and management features on your dashboard."
        ),
    },
    {
        "chunk_id": "owner-payment-setup",
        "topic": "Connecting Payment Account as Owner",
        "content": (
            "After becoming an owner, you need to connect your payment account to receive rental payments from tenants. "
            "Go to your Owner dashboard and click Connect Payment Account. "
            "You will be guided through a secure onboarding process to link your bank account. "
            "Once connected and verified, you can start listing properties and receiving payments."
        ),
    },
    {
        "chunk_id": "property-listing-wizard",
        "topic": "Listing a Property (Step-by-Step Wizard)",
        "content": (
            "To list a property, go to your Owner dashboard and click Add Property. "
            "A step-by-step wizard will guide you through the process. "
            "You will fill in the property title, type, rent amount, address, description, number of bedrooms, and other details. "
            "Complete each step before moving to the next — you cannot skip steps. "
            "Once all steps are done, your listing will be ready to submit."
        ),
    },
    {
        "chunk_id": "property-listing-images",
        "topic": "Uploading Property Images",
        "content": (
            "During the property listing process, you can upload photos of your property in the Images step. "
            "Upload multiple clear photos to give tenants a good view of the property. "
            "You can remove individual images if needed. "
            "Good photos improve your listing's visibility to potential tenants."
        ),
    },
    {
        "chunk_id": "property-listing-submit",
        "topic": "Submitting a Property for Approval",
        "content": (
            "Once you have completed all the steps in the listing wizard including uploading your property documents, click Submit for Approval. "
            "Your listing will be sent for review. "
            "Once approved, it becomes visible to tenants on the properties page. "
            "If rejected, you will receive a notification with the reason and can make corrections before resubmitting."
        ),
    },
    {
        "chunk_id": "rental-application-apply",
        "topic": "Applying for a Rental Property",
        "content": (
            "To apply for a rental property, open the property listing and click Apply. "
            "You must have a verified KYC to apply. "
            "Submit your application and you will receive a notification once the property owner reviews it. "
            "You can track the status of all your applications from your Tenant dashboard."
        ),
    },
    {
        "chunk_id": "rental-application-review",
        "topic": "Reviewing Rental Applications as Owner",
        "content": (
            "As an owner, you will be notified when a tenant applies for your property. "
            "Go to your Owner dashboard and open the Applications tab. "
            "You can view the tenant's details and choose to Approve or Reject the application. "
            "Only one application can be approved per property. "
            "The tenant will be notified of your decision."
        ),
    },
    {
        "chunk_id": "lease-create",
        "topic": "Creating a Lease",
        "content": (
            "After approving a rental application, you can create a lease for the tenant. "
            "Go to the approved application and click Create Lease. "
            "Fill in the lease terms — start date, end date, rent amount, deposit amount, payment frequency, and notice period. "
            "The tenant will be notified that a lease has been created and is waiting for signatures."
        ),
    },
    {
        "chunk_id": "lease-sign",
        "topic": "Signing a Lease",
        "content": (
            "Once a lease is created, both the owner and tenant need to sign it. "
            "Open the lease from your dashboard and click Sign. "
            "You will need to enter your full name and provide your signature. "
            "The tenant signs first, followed by the owner. "
            "After both parties sign, the lease goes for a final review before becoming active."
        ),
    },
    {
        "chunk_id": "lease-void-terminate",
        "topic": "Voiding or Terminating a Lease",
        "content": (
            "An owner can cancel a lease before both parties have signed — open the lease and click Void. "
            "Once a lease is active, it can be ended early by the owner by clicking Terminate on the lease page. "
            "Both actions notify the tenant and cannot be undone."
        ),
    },
    {
        "chunk_id": "lease-renewal-vacate",
        "topic": "Requesting Lease Renewal or Submitting Vacate Notice",
        "content": (
            "Before your lease expires, you can request a renewal from the lease page on your dashboard by clicking Request Renewal. "
            "The owner will be notified and can approve or reject it. "
            "If you plan to move out instead, submit a Vacate Notice from the lease page. "
            "Enter your intended exit date and submit — the owner can accept or reject the notice."
        ),
    },
    {
        "chunk_id": "lease-expiry-warning",
        "topic": "Property Marked as Available Soon Before Lease Ends",
        "content": (
            "If your lease has less than 3 months remaining and you have not requested a renewal, "
            "the property will automatically be marked as Available Soon. "
            "You will receive a notification when this happens. "
            "To avoid this, submit a renewal request before your lease gets close to its end date. "
            "If the lease expires without a renewal, the property becomes available for new tenants."
        ),
    },
    {
        "chunk_id": "vacate-notice-early-exit",
        "topic": "Moving Out Before Your Vacate Notice Date",
        "content": (
            "If you submitted a vacate notice with a specific exit date and you move out before that date, "
            "your lease remains active until the exit date you agreed on. "
            "Rent bills will continue until your notice period ends. "
            "The lease is only closed once your exit date has passed. "
            "Make sure the exit date in your vacate notice matches the actual date you plan to move out."
        ),
    },
    {
        "chunk_id": "vacate-notice-penalty",
        "topic": "Penalty for Early Vacate Notice",
        "content": (
            "When you submit a vacate notice, your lease has a minimum notice period that must be respected. "
            "If the exit date you choose is earlier than the notice period requires, a penalty will be applied. "
            "The penalty amount is based on your security deposit and the specific terms in your lease — "
            "you will see the penalty amount before confirming. "
            "If your landlord accepts the notice, a penalty bill will be added to your account due on your exit date. "
            "Your security deposit may also be partially or fully forfeited depending on your lease terms. "
            "To avoid a penalty, make sure your exit date gives enough advance notice as stated in your lease."
        ),
    },
    {
        "chunk_id": "payments-bills",
        "topic": "Viewing Rent Bills",
        "content": (
            "Once your lease is active, rent bills are generated automatically each month. "
            "Go to your lease page and open the Bills tab to view all your bills. "
            "Each bill shows the amount, due date, and current status — pending, paid, or overdue."
        ),
    },
    {
        "chunk_id": "payments-pay",
        "topic": "Paying a Rent Bill",
        "content": (
            "To pay a rent bill, go to your lease and open the Bills tab. "
            "Click Pay on the bill you want to settle. "
            "You will be redirected to a secure payment page. "
            "Complete the payment using your card. "
            "Once the payment is successful, the bill status updates automatically and you will receive a confirmation notification."
        ),
    },
    {
        "chunk_id": "platform-fees",
        "topic": "Platform Fee",
        "content": (
            "EstateSync charges a 2% platform fee on each rent payment. "
            "This fee is deducted automatically when your tenant pays rent. "
            "The remaining amount is transferred to your connected payment account. "
            "You do not need to do anything — the fee is handled in the background."
        ),
    },
    {
        "chunk_id": "owner-payout-standard",
        "topic": "Requesting a Standard Payout as Owner",
        "content": (
            "To receive your rental earnings, go to your Owner dashboard and open the Payouts section. "
            "Click Request Payout to withdraw your available balance. "
            "Standard payouts take 1 to 2 business days to reach your bank account. "
            "Earnings are not transferred automatically — you need to request a payout manually."
        ),
    },
    {
        "chunk_id": "owner-payout-instant",
        "topic": "Requesting an Instant Payout as Owner",
        "content": (
            "If you need your earnings sooner, you can request an Instant Payout from the Payouts section in your Owner dashboard. "
            "Instant payouts arrive much faster than standard payouts. "
            "This option is available when you have funds ready for instant withdrawal."
        ),
    },
    {
        "chunk_id": "complaints-create",
        "topic": "Raising a Complaint",
        "content": (
            "If you have an issue with your rental property, go to your Tenant dashboard and click Raise a Complaint. "
            "Select the type of issue, set the priority, and describe the problem clearly. "
            "You can also attach photos to support your complaint. "
            "Once submitted, the relevant parties are notified."
        ),
    },
    {
        "chunk_id": "complaints-status",
        "topic": "Tracking Complaint Status",
        "content": (
            "You can track your complaint from your dashboard. "
            "Complaints start as Open, then move to In Progress when the issue is being addressed, and then to Resolved. "
            "As a tenant, you can mark a resolved complaint as Closed once you are satisfied. "
            "You will receive a notification whenever the status of your complaint changes."
        ),
    },
    {
        "chunk_id": "saved-properties",
        "topic": "Saving and Unsaving Properties",
        "content": (
            "You can save properties you are interested in by clicking the Save button on any property listing. "
            "To view your saved properties, go to your dashboard and open the Saved Properties section. "
            "You can remove a saved property at any time by clicking Unsave."
        ),
    },
    {
        "chunk_id": "notifications-overview",
        "topic": "Notifications",
        "content": (
            "EstateSync sends you notifications for all important events — such as when your KYC is reviewed, "
            "when a lease is created or signed, when a rent bill is generated, when a payment is received, "
            "or when a complaint is updated. "
            "Click the bell icon in the top navigation bar to view all your notifications. "
            "You can mark them as read one by one or all at once."
        ),
    },
]
