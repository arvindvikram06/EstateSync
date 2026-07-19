# Workload Identity and Azure Roles — Explained for EstateSync

This file explains two things you asked about:
1. **What "workload identity" is in YOUR app** — traced through your real config files
2. **Azure roles** (Owner / Contributor / Admin) — who has what in a company training subscription, and why it limits you

---

# PART 1 — Workload Identity in YOUR app

## The one-sentence version

Your API pod running inside AKS reads its secrets (DB password, Stripe key, JWT key)
from Azure Key Vault **without any password stored anywhere** — because the pod
"becomes" an Azure managed identity through a chain of trust. That mechanism is
called **Workload Identity**.

## The problem it solves

Your API needs secrets: PostgreSQL connection string, Stripe secret key, JWT signing key.

**The bad old way:** put the secrets in the pod as environment variables or a mounted
file. Problem — now the secret exists as plain text inside the cluster, in your YAML,
maybe in git. Anyone with cluster access reads them.

**Workload Identity way:** the pod proves *who it is* to Azure, and Azure hands it the
secrets directly from Key Vault. No password is ever written down. Azure rotates the
underlying credential automatically.

## The chain of trust (your actual resources)

```
┌─────────────────────────────────────────────────────────────────────┐
│  1. POD                                                               │
│     deployment.yaml                                                   │
│     - label: azure.workload.identity/use: "true"                     │
│     - serviceAccountName: estatesync-api-sa                          │
│     - env: Azure__KeyVaultUri = https://estatesync-kv.vault...       │
└───────────────────────────────┬─────────────────────────────────────┘
                                 │ "I run as this ServiceAccount"
                                 ▼
┌─────────────────────────────────────────────────────────────────────┐
│  2. KUBERNETES SERVICE ACCOUNT                                        │
│     serviceaccount.yaml                                               │
│     name: estatesync-api-sa                                          │
│     annotation:                                                      │
│       azure.workload.identity/client-id:                            │
│         6c0825d3-6edd-4786-9896-ba3465714075   ← the managed identity│
└───────────────────────────────┬─────────────────────────────────────┘
                                 │ federated via OIDC (see step 3)
                                 ▼
┌─────────────────────────────────────────────────────────────────────┐
│  3. AKS OIDC ISSUER  +  MANAGED IDENTITY                             │
│     aks.bicep                                                        │
│     - aksIdentity  = "estatesync-aks-identity" (user-assigned MI)   │
│     - oidcIssuerProfile: { enabled: true }   ← issues signed tokens │
│     - securityProfile.workloadIdentity: { enabled: true }           │
│                                                                     │
│     AKS issues a signed OIDC token proving "this pod = that SA".    │
│     Azure AD trusts that token → lets the pod act as the identity.  │
└───────────────────────────────┬─────────────────────────────────────┘
                                 │ "I AM the managed identity"
                                 ▼
┌─────────────────────────────────────────────────────────────────────┐
│  4. KEY VAULT ACCESS POLICY                                          │
│     keyvault.bicep                                                   │
│     enableRbacAuthorization: false   ← uses Access Policies         │
│     accessPolicies:                                                  │
│       - objectId: aksIdentityPrincipalId   ← the managed identity   │
│         permissions: { secrets: [ 'get', 'list' ] }                 │
│                                                                     │
│     Key Vault checks: "is this identity allowed to read secrets?"   │
│     ✅ yes → hands over ConnectionStrings--DefaultConnection, etc.  │
└─────────────────────────────────────────────────────────────────────┘
```

## What flows at runtime (no passwords anywhere)

1. Pod starts. It's labeled `azure.workload.identity/use: "true"`.
2. AKS injects a short-lived **OIDC token** into the pod (a signed JWT saying
   "this pod runs as ServiceAccount `estatesync-api-sa`").
3. The .NET app (via `DefaultAzureCredential`) sends that token to Azure AD.
4. Azure AD checks the **federated credential**: "does SA `estatesync-api-sa` map to
   managed identity `6c0825d3-...`? ✅" → issues an Azure access token.
5. App calls Key Vault at `Azure__KeyVaultUri` with that token.
6. Key Vault checks its **access policy**: "does `6c0825d3-...` have `get` on secrets?
   ✅" → returns the secret values.
7. App boots with real secrets. **Nothing was ever stored as a password.**

## Which "option" is this? — Option 3 (Managed Identity)

This is exactly the **managed identity** method from the ACR discussion. Your app
uses it for **reading Key Vault**. The same identity (`estatesync-aks-identity`) is
*also* what would pull images from ACR (`AcrPull`).

### Why it does NOT solve your CI push problem

- Workload Identity works because the pod **runs inside AKS** — AKS is what issues the
  OIDC token that starts the chain.
- **GitHub Actions does not run inside AKS.** There is no AKS OIDC issuer in a GitHub
  runner, so this chain can't start there.
- CI would need its OWN federated identity credential — and setting that up requires
  **creating a role/federated-credential assignment**, which (see Part 2) you can't do
  with Contributor.

So: Workload Identity = great for the running app. Irrelevant for pushing from CI.
That's why CI falls back to the ACR **admin user** (a plain password, no Azure AD, no
role assignment needed).

## Note: your Key Vault avoids RBAC on purpose

Look at `keyvault.bicep`:
```
enableRbacAuthorization: false
accessPolicies: [ ... ]
```
Key Vault supports TWO permission models:
- **RBAC mode** — grant the `Key Vault Secrets User` role (needs a role assignment)
- **Access Policy mode** — list object IDs directly on the vault (NO role assignment)

Your vault uses **Access Policy mode** for the exact same reason you're stuck on ACR
admin user: **you only have Contributor, and you can't create role assignments.**
Access policies are a property of the vault itself, so Contributor can set them. This
was a deliberate design choice to work within your permission limit.

---

# PART 2 — Azure Roles: who is "Admin", and why you're limited

## First: "Admin" is ambiguous in Azure. There are THREE different things.

| Term | What it controls | Example holder |
|---|---|---|
| **Entra ID (Azure AD) roles** | Identities, users, the directory itself | Global Administrator |
| **Azure RBAC roles** | Resources (VMs, ACR, AKS, Key Vault...) | Owner, Contributor |
| **Classic admins (legacy)** | Old subscription-level control | Account Administrator |

When your company says "the admin" they usually mean whoever holds **Global
Administrator** (directory) and/or **Owner** (resources) on the training subscription.
That's the company's cloud/IT team — not you.

## The Azure RBAC roles that matter here

RBAC = Role-Based Access Control. A role is a bundle of permissions, "assigned" to an
identity at a "scope" (subscription / resource group / single resource).

| Role | Can create/manage resources? | Can grant access to others? | You have it? |
|---|---|---|---|
| **Reader** | ❌ view only | ❌ | — |
| **Contributor** | ✅ yes — full resource management | ❌ **NO** | ✅ **this is you** |
| **User Access Administrator** | ❌ | ✅ yes — only grants access | — |
| **Owner** | ✅ yes | ✅ yes | ❌ (the company holds this) |

### The critical line: Contributor vs Owner

```
Contributor = "manage the resources"     (create ACR, deploy AKS, set up Key Vault)
Owner       = Contributor + "grant access to those resources"
```

The ONE permission that separates them:
```
Microsoft.Authorization/roleAssignments/write
```
This is the "assign a role to someone" permission. It lives in **Owner** and
**User Access Administrator** only. Contributor is *deliberately built without it*.

### Why Microsoft designed it this way

If Contributor could assign roles, a Contributor could simply assign themselves Owner —
and the whole permission system would be pointless. So the wall between "manage
resources" and "grant access" is intentional and cannot be bypassed from your side.

## How this maps to your training subscription

```
COMPANY AZURE TENANT (the directory)
│
├── Global Administrator ......... company IT/cloud team (controls identities)
│
└── Training Subscription
    │
    ├── Owner .................... company cloud team (full control incl. granting roles)
    │
    └── Resource Group: estatesync-rg
        │
        └── Contributor .......... YOU  ← can build everything, cannot grant roles
```

You were given **Contributor**, almost certainly scoped to just `estatesync-rg` (so
trainees can't touch each other's work or the rest of the subscription).

## What you CAN and CANNOT do — the full picture

| Task | Needs role assignment? | You can? |
|---|---|---|
| Create ACR / AKS / Key Vault / Postgres | ❌ | ✅ Yes |
| Enable ACR admin user (a toggle) | ❌ | ✅ Yes |
| Set Key Vault **access policies** | ❌ | ✅ Yes |
| Push to ACR from your laptop (`az acr login`) | ❌ (uses your existing rights) | ✅ Yes |
| Push to ACR from CI via admin password | ❌ | ✅ Yes |
| Create a service principal + assign `AcrPush` | ✅ | ❌ **No** |
| Assign `AcrPull` to the AKS identity | ✅ | ❌ **No** |
| Grant Key Vault via **RBAC** role | ✅ | ❌ **No** |
| Give a teammate any access | ✅ | ❌ **No** |

## The two ways out (both require asking the Owner)

You cannot escalate your own access. Someone with **Owner** or **User Access
Administrator** must do ONE of these:

**Option A — they do the role assignment for you (narrow ask):**
> "Please assign `AcrPush` to this service principal, scoped to `estatesyncacr`."
> Then CI can use the SP instead of the admin password.

**Option B — they grant you User Access Administrator on just the RG (broader, cleaner):**
> "Please grant me `User Access Administrator` scoped to `estatesync-rg`."
> Then YOU can create service principals, assign `AcrPush`/`AcrPull`, switch Key Vault
> to RBAC, and turn the ACR admin user off entirely.

Option B is the better long-term ask — it's scoped to one resource group, and unblocks
every "proper" (non-admin-user) path at once.

## Until then

**ACR admin user is your correct, only option for CI.** It's not a mistake or a
hack — it's the documented fallback for exactly your situation (Contributor without
role-assignment rights). Your Key Vault access-policy setup is the same pattern. Both
are deliberate designs to ship within your permission limit.
