# Final Pipeline and Deployment Plan — EstateSync

The complete, ordered plan to go from "manually deployed" to "push-to-main
auto-deploys everything". Every step says WHAT to do, WHERE, and WHY.

---

## The target architecture

```
                         git push to main
                               │
        ┌──────────────────────┼──────────────────────────┐
        │ paths:               │ paths:                   │ paths:
        │ frontend/**          │ backend/**               │ ai-service/**
        ▼                      ▼                          ▼
┌───────────────┐   ┌─────────────────────┐   ┌─────────────────────┐
│ Deploy        │   │ Deploy Backend API  │   │ Deploy AI Service   │
│ Frontend      │   │ 1. docker build     │   │ 1. docker build     │
│ (already      │   │ 2. push → ACR       │   │ 2. push → ACR       │
│  working ✅)  │   │ 3. kubectl set image│   │ 3. kubectl set image│
└──────┬────────┘   └─────────┬───────────┘   └─────────┬───────────┘
       │                      │                          │
       ▼                      ▼                          ▼
 Azure Static           AKS: estatesync namespace (scoped github-deployer SA)
 Web Apps               pods pull image from ACR, read secrets from Key Vault
```

Identities used (least privilege possible with Contributor access):
- **ACR push**: admin user password (only option — see WORKLOAD-IDENTITY-AND-ROLES.md)
- **AKS deploy**: `github-deployer` Kubernetes ServiceAccount — namespace-scoped,
  no delete, no cluster-admin
- **Runtime secrets**: pods use Workload Identity → Key Vault (no change needed)

---

## PHASE 0 — Make the repo safe to push (one time)

> Full reasoning in `SECRETS-AND-GIT-SAFETY.md`. Condensed steps:

**0.1** Edit `.gitignore` — replace the `infra/` line with:
```gitignore
infra/parameters/*.bicepparam
!infra/parameters/prod.bicepparam.example
```

**0.2** Verify the secrets file is STILL ignored:
```bash
git check-ignore infra/parameters/prod.bicepparam && echo SAFE
```

**0.3** Dry-run and eyeball what will be added:
```bash
git add --dry-run infra/
```

**0.4** Enable GitHub push protection:
Repo → Settings → Code security and analysis → enable **Secret scanning** and
**Push protection**.

**0.5** Commit and push the infra folder + workflows.

---

## PHASE 1 — Gather the two CI credentials (one time, ~10 min)

### 1.1 ACR admin credentials (for image push)
```bash
az acr credential show --name estatesyncacr
```
Note down:
- `ACR_LOGIN_SERVER` → `estatesyncacr.azurecr.io`
- `ACR_USERNAME`     → `estatesyncacr`
- `ACR_PASSWORD`     → `passwords[0].value` from the output

### 1.2 Scoped kubeconfig (for deploy) — uses the minimal github-deployer SA

```bash
# Connect with your own (admin) credentials first:
az aks get-credentials --resource-group estatesync-rg --name estatesync-aks --admin

# Create the scoped ServiceAccount + Role + token (idempotent):
kubectl apply -f infra/k8s/github-deployer.yaml

# Extract the pieces:
SERVER=$(kubectl config view --minify -o jsonpath='{.clusters[0].cluster.server}')
CA=$(kubectl config view --raw --minify --flatten -o jsonpath='{.clusters[0].cluster.certificate-authority-data}')
TOKEN=$(kubectl get secret github-deployer-token -n estatesync -o jsonpath='{.data.token}' | base64 -d)

# Build the scoped kubeconfig:
cat > /tmp/github-kubeconfig <<EOF
apiVersion: v1
kind: Config
clusters:
- cluster:
    server: $SERVER
    certificate-authority-data: $CA
  name: estatesync
contexts:
- context:
    cluster: estatesync
    namespace: estatesync
    user: github-deployer
  name: estatesync
current-context: estatesync
users:
- name: github-deployer
  user:
    token: $TOKEN
EOF

cat /tmp/github-kubeconfig    # ← this entire output is the secret value
```

**Why the scoped SA instead of the --admin kubeconfig:** the admin kubeconfig is
cluster-admin (can delete everything, read all secrets, all namespaces). The
github-deployer SA can only update deployments/services in the `estatesync`
namespace and read pods. If the GitHub secret ever leaks, the blast radius is
minimal.

---

## PHASE 2 — Add GitHub secrets (one time, ~5 min)

Repo → **Settings → Secrets and variables → Actions → New repository secret**

| Secret name | Value |
|---|---|
| `ACR_LOGIN_SERVER` | `estatesyncacr.azurecr.io` |
| `ACR_USERNAME` | `estatesyncacr` |
| `ACR_PASSWORD` | from step 1.1 |
| `AKS_DEPLOY_KUBECONFIG` | entire content of `/tmp/github-kubeconfig` |

Then clean up the local file:
```bash
rm /tmp/github-kubeconfig
```

(`AZURE_STATIC_WEB_APPS_API_TOKEN` for the frontend is already configured.)

---

## PHASE 3 — Test the pipelines manually (before enabling auto-deploy)

Both workflows already support manual runs (`workflow_dispatch`).

**3.1** GitHub → Actions tab → **Deploy Backend API** → Run workflow → main

Watch for:
1. ✅ "Verify deployment configuration" — all 4 secrets found
2. ✅ docker build + push — image lands in ACR tagged with the commit SHA
3. ✅ kubectl apply + set image — uses the scoped kubeconfig
4. ✅ `rollout status` completes within 5 min

**3.2** Same for **Deploy AI Service**.

**3.3** Verify live:
```bash
kubectl get pods -n estatesync          # new pod age = minutes
curl https://estatesync-api.southindia.cloudapp.azure.com/health
```

If a step fails: the fix is almost always a mis-pasted secret. Re-paste, re-run.

---

## PHASE 4 — Enable auto-deploy on push (the final switch)

In BOTH workflow files, uncomment the push trigger:

`.github/workflows/deploy-backend.yml`:
```yaml
on:
  push:
    branches: [main]
    paths:
      - 'backend/**'
      - 'infra/k8s/deployment.yaml'
      - 'infra/k8s/service.yaml'
      - '.github/workflows/deploy-backend.yml'
  workflow_dispatch:
```

`.github/workflows/deploy-ai-service.yml`:
```yaml
on:
  push:
    branches: [main]
    paths:
      - 'ai-service/**'
      - 'infra/k8s/ai-service.yaml'
      - '.github/workflows/deploy-ai-service.yml'
  workflow_dispatch:
```

The `paths:` filter means a frontend-only commit does NOT rebuild the backend, and
vice versa. Each service deploys only when its own files change.

Commit + push these two edits → pipeline is LIVE.

---

## PHASE 5 — Hardening (add within the first week)

In priority order:

**5.1 CI build+test gate (highest value)** — a separate `ci.yml` that runs on every
PR: `dotnet build && dotnet test` for backend, `pip install && pytest` (if tests
exist) for ai-service. Stops broken code BEFORE merge; deploy workflows only run
after merge.

**5.2 Gitleaks secret scan** — `.github/workflows/secret-scan.yml`:
```yaml
name: Secret Scan
on: [push, pull_request]
jobs:
  gitleaks:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
        with: { fetch-depth: 0 }
      - uses: gitleaks/gitleaks-action@v2
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

**5.3 Concurrency guard** — add to each deploy job so two rapid pushes can't deploy
simultaneously:
```yaml
concurrency:
  group: deploy-backend      # deploy-ai for the AI workflow
  cancel-in-progress: false
```

**5.4 Trivy image scan** — after build, before push:
```yaml
- uses: aquasecurity/trivy-action@master
  with:
    image-ref: ${{ env.ACR_LOGIN_SERVER }}/estatesync-api:${{ env.IMAGE_TAG }}
    severity: CRITICAL,HIGH
    exit-code: '1'
```

**5.5 Dependabot** — `.github/dependabot.yml` for nuget + npm + pip + github-actions
ecosystems, weekly.

Skip for now: SonarQube, ArgoCD/GitOps, Terraform — overkill for a single-cluster
training project.

---

## Day-to-day flow once live

```
you (or teammate) opens PR
  → ci.yml builds + tests (5.1)         ← blocks broken code
  → gitleaks scans (5.2)                ← blocks leaked secrets
  → you review + merge to main
      → paths decide which deploy runs
      → image built, tagged with SHA, pushed to ACR
      → scoped SA updates the deployment in AKS
      → rollout status confirms healthy pods
  → done. No manual docker/kubectl ever again.
```

## Rollback (when a deploy goes bad)

Every image is tagged with its commit SHA, so:
```bash
kubectl set image deployment/estatesync-api \
  api=estatesyncacr.azurecr.io/estatesync-api:<previous-good-sha> -n estatesync
```
Or simply: revert the commit on main → pipeline redeploys the old code.

---

## Quick reference — what lives where

| Thing | Location |
|---|---|
| Infra shape (bicep, k8s yaml) | git — `infra/` |
| Pipeline definitions | git — `.github/workflows/` |
| Runtime app secrets | Azure Key Vault |
| Deploy credentials | GitHub Actions secrets |
| Real bicep parameter values | ONLY your laptop — `infra/parameters/prod.bicepparam` (gitignored) |
| Parameter template for teammates | git — `infra/parameters/prod.bicepparam.example` |
