# Secrets and Git Safety — What Can Be Pushed, What Must Never Be

This document answers: *"My bicep and yaml files have keys and URIs — how do I push
them to GitHub safely?"* — with an exact file-by-file verdict for THIS repo.

---

## 1. The core concept: SECRET vs IDENTIFIER

This is the single most important distinction. Most things that *look* scary in your
files are not secrets at all.

### A SECRET is something that GRANTS access by itself
If someone has it, they can do damage with no other information:

| Example | Why it's a secret |
|---|---|
| PostgreSQL password | Log into your database |
| Stripe secret key (`sk_live_...`) | Charge cards, move money |
| JWT signing key | Forge login tokens for any user, become Admin |
| ACR admin password | Push malicious images to your registry |
| Kubeconfig token | Deploy anything to your cluster |
| Storage account connection string | Read/delete every uploaded file |

### An IDENTIFIER is just a name/address — it grants NOTHING by itself
Knowing it is useless without a credential to go with it:

| Example | Why it's safe |
|---|---|
| Key Vault URI (`https://estatesync-kv.vault.azure.net/`) | It's an address. Azure AD still rejects anyone without a valid token + access policy. It's like knowing a bank's street address — you still can't open the vault. |
| Backend URL (`estatesync-api.southindia.cloudapp.azure.com`) | It's PUBLIC by definition — every browser using your app sees it. |
| Managed identity client-id (`6c0825d3-...`) | It's a username with no password. The "password" is the OIDC trust chain that ONLY works from inside your AKS cluster. Nobody can "log in" with a client-id from outside. |
| ACR login server name (`estatesyncacr.azurecr.io`) | An address. Pushing needs the admin password. |
| Resource group / cluster / vault NAMES | Public metadata. Visible to anyone in your subscription anyway. |
| Kubernetes namespace, service names | Only meaningful inside your cluster. |

**Rule of thumb:** ask *"if I posted this on Twitter, could someone act on it?"*
- Password/key/token/connection-string → YES → secret → never in git
- URL/URI/name/ID → NO → identifier → fine in git

Real-world confirmation: thousands of open-source projects publish their k8s yaml and
bicep/terraform with URIs and client-ids in plain view (this is standard GitOps
practice). What they never publish is passwords.

---

## 2. File-by-file verdict for THIS repo

### ✅ SAFE to push (contain only identifiers)

| File | "Scary-looking" content | Verdict |
|---|---|---|
| `infra/main.bicep` | Parameter *names* like `jwtSecretKey` | Safe — declares that a secret EXISTS, has `@secure()` so the value is never logged. No value inside. |
| `infra/modules/*.bicep` (all 9) | Resource names, SKUs | Safe — pure infrastructure shape. `keyvault.bicep` writes secrets INTO the vault but receives them as `@secure()` params — no values in the file. |
| `infra/k8s/deployment.yaml` | Key Vault URI, ACR image name | Safe — both are identifiers (see above). |
| `infra/k8s/serviceaccount.yaml` | Workload identity client-id | Safe — a client-id is not a credential (see above). |
| `infra/k8s/service.yaml`, `namespace.yaml`, `redis.yaml`, `ingress.yaml`, `clusterissuer.yaml` | Hostnames, ports | Safe — public addresses and cluster-internal names. |
| `infra/k8s/github-deployer.yaml` | ServiceAccount + Role + Secret *declaration* | Safe — it declares that a token should be GENERATED in the cluster. The token value itself only ever exists inside the cluster, never in this file. |
| `infra/k8s/ai-service.yaml` | Image name, env var NAMES | Safe — verify it references secrets via `secretKeyRef` (names), not inline values. |
| `.github/workflows/*.yml` | `${{ secrets.ACR_PASSWORD }}` | Safe — this is a REFERENCE to a secret stored in GitHub, not the value. That's the whole point of the syntax. |

### ❌ NEVER push (contain real credential values)

| File | What's inside | Protection |
|---|---|---|
| `infra/parameters/prod.bicepparam` | Real Postgres password, JWT key, Stripe secret key | Must stay gitignored FOREVER |
| `**/appsettings.Development.json` | Local dev connection strings, SMTP app password | Already gitignored |
| any exported kubeconfig | Cluster tokens | Never write into the repo; use /tmp and delete |

---

## 3. Current state of YOUR .gitignore — and the exact change to make

Right now `.gitignore` has:
```
# Azure deployment infra (contains secrets)
infra/
```
This ignores **everything** — including the safe bicep and yaml you WANT to push.

### The fix: un-ignore the folder, keep only the params file ignored

Replace that block with:

```gitignore
# Azure deployment secrets — real credential values live here, never commit
infra/parameters/*.bicepparam
# but DO track the example template
!infra/parameters/prod.bicepparam.example
```

Why this shape:
- `infra/` itself becomes tracked → all bicep + k8s yaml can be committed
- `infra/parameters/*.bicepparam` stays ignored → `prod.bicepparam` (the ONLY file
  with real values) can never be committed, even accidentally, even with `git add .`
- The `!` line re-includes the example template so teammates know what parameters
  they need to supply

### Verify BEFORE the first push (do not skip)

```bash
# 1. Confirm the secrets file is still ignored after the change:
git check-ignore infra/parameters/prod.bicepparam && echo "SAFE - still ignored"

# 2. Dry-run: list everything git WOULD add — read this list with your eyes:
git add --dry-run infra/ | grep -v example

# 3. Search staged content for real values before committing:
git diff --cached | grep -iE "password|sk_live|sk_test|whsec_" && echo "STOP - FOUND SECRET" || echo "clean"
```

---

## 4. Where each secret SHOULD live (the full map)

Every secret has exactly ONE home. Nothing is duplicated, nothing is in git.

| Secret | Lives in | Consumed by | How it gets there |
|---|---|---|---|
| Postgres password | Azure Key Vault (`ConnectionStrings--DefaultConnection`) | API pod via Workload Identity | Bicep deploy reads it from your LOCAL `prod.bicepparam` |
| JWT signing key | Key Vault (`Jwt--SecretKey`) | API pod | same |
| Stripe secret key | Key Vault (`Stripe--SecretKey`) | API pod | same |
| Stripe webhook secret | Key Vault (`Stripe--WebhookSecret`) | API pod | `az keyvault secret set` manually |
| Blob connection string | Key Vault (`Azure--BlobConnectionString`) | API pod | Bicep output → vault (automatic) |
| SignalR connection string | Key Vault (`Azure--SignalRConnectionString`) | API pod | Bicep output → vault (automatic) |
| ACR admin password | GitHub Actions secret `ACR_PASSWORD` | CI build job | You paste it once in repo Settings |
| Scoped kubeconfig token | GitHub Actions secret `AKS_DEPLOY_KUBECONFIG` | CI deploy job | You paste it once in repo Settings |
| Static Web App deploy token | GitHub secret `AZURE_STATIC_WEB_APPS_API_TOKEN` | Frontend CI | Already done |
| AI service keys (OpenAI etc.) | Kubernetes Secret in cluster (`kubectl create secret`) | ai-service pod via `secretKeyRef` | One-time kubectl command |
| Local dev secrets | `appsettings.Development.json` / `ai-service/.env` | Your machine only | Gitignored |

Three storage systems, three jobs:
- **Azure Key Vault** → runtime secrets the APP needs
- **GitHub Actions secrets** → deploy-time credentials the PIPELINE needs
- **Kubernetes Secrets** → runtime secrets for pods that don't use Key Vault (ai-service)

---

## 5. Defense in depth — the layers that catch mistakes

1. **`.gitignore`** — prevents the accident (first line of defense)
2. **GitHub Push Protection** — repo Settings → Code security → enable Secret
   scanning + Push protection. GitHub blocks the push itself if it recognizes a
   Stripe/Azure key pattern. Free for public repos.
3. **Gitleaks CI job** — scans full history on every push, fails the build on leaks.
   Catches patterns GitHub doesn't know.
4. **`@secure()` in bicep** — parameter values never appear in deployment logs or
   the Azure portal deployment history.
5. **Short-lived SAS URLs** — your private blobs already use 1-hour SAS, so even a
   leaked file URL expires.

### If a secret ever DOES leak (the honest playbook)
Committed-then-deleted is still leaked — git history keeps it, and scrapers scan
GitHub within SECONDS of a push. Deleting the commit is not enough. You must ROTATE:
- Postgres → `az postgres flexible-server update --admin-password <new>`
- Stripe → Dashboard → roll the key
- JWT → generate new 32+ char key, update vault, restart pods (all users re-login)
- ACR admin → `az acr credential renew --name estatesyncacr --password-name password`
- Kubeconfig token → delete + recreate the `github-deployer-token` secret in cluster

Rotate FIRST, clean git history second (optional, via `git filter-repo`).
