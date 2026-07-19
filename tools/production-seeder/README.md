# EstateSync production test-data seeder

This tool seeds synthetic data through EstateSync's public application APIs. It must
not connect directly to PostgreSQL, Azure Blob Storage, or Stripe. That ensures normal
validation, authorization, storage routing, and webhook behavior are exercised.

## Safety rules

- Use only with Stripe test-mode keys (`sk_test_...`).
- All identity, ownership, address, and property data is synthetic.
- Generated PDFs must visibly say `TEST DOCUMENT - NOT A REAL ID OR LEGAL RECORD`.
- Never commit passwords, API tokens, refresh cookies, Stripe keys, or onboarding URLs.
- Read the shared password from `ESTATESYNC_SEED_PASSWORD` at runtime.
- Keep runtime state in the gitignored local state file.
- Never delete or reset existing production data.
- Every phase must be rerunnable and skip resources already recorded in state.
- Upload files through the API; never write Blob URLs or Stripe IDs directly to the DB.

## Planned phases

1. `prepare`: register users, verify readiness, create KYC drafts, upload four KYC
   PDFs per owner, and submit KYC for manual Admin approval.
2. `owners`: after KYC approval, grant the Owner role and create one-time Stripe test
   onboarding links. The authenticated test owners complete Stripe-hosted onboarding.
3. `properties`: after Stripe webhooks mark onboarding complete, create 12 properties,
   upload six images and one ownership PDF per property, and submit them for manual
   Admin approval.
4. `verify`: query profiles, documents, properties, image counts, and statuses and
   produce a redacted verification report.

The dataset is defined in `seed-manifest.json`. Runtime state and generated assets will
be created in this directory during the later implementation phases and excluded from Git.
