param location string
param prefix string
param aksIdentityPrincipalId string

// Deployer's object ID — so you can view/edit secrets in the Azure Portal
param deployerObjectId string

// Azure service connection strings — derived from other module outputs in main.bicep
@secure()
param postgresConnectionString string
@secure()
param blobConnectionString string
@secure()
param signalRConnectionString string

// Secrets that must be supplied manually
@secure()
param jwtSecretKey string
@secure()
param stripeSecretKey string

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: '${prefix}-kv'
  location: location
  properties: {
    sku: { family: 'A', name: 'standard' }
    tenantId: subscription().tenantId
    enableRbacAuthorization: false
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    accessPolicies: [
      // AKS managed identity — read secrets at runtime
      {
        tenantId: subscription().tenantId
        objectId: aksIdentityPrincipalId
        permissions: { secrets: [ 'get', 'list' ] }
      }
      // Deployer — full control via Azure Portal
      {
        tenantId: subscription().tenantId
        objectId: deployerObjectId
        permissions: { secrets: [ 'get', 'list', 'set', 'delete' ] }
      }
    ]
  }
}

// ── Azure service secrets (auto-populated from Bicep outputs) ─────────────────

resource secretPostgres 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'ConnectionStrings--DefaultConnection'
  properties: { value: postgresConnectionString }
}

// Redis runs as a pod inside AKS — connection string is the k8s service name
resource secretRedis 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'ConnectionStrings--Redis'
  properties: { value: 'estatesync-redis:6379,abortConnect=false' }
}

resource secretBlob 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'Azure--BlobConnectionString'
  properties: { value: blobConnectionString }
}

resource secretSignalR 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'Azure--SignalRConnectionString'
  properties: { value: signalRConnectionString }
}

// ── Secrets supplied as parameters ────────────────────────────────────────────

resource secretJwt 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'Jwt--SecretKey'
  properties: { value: jwtSecretKey }
}

resource secretStripe 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'Stripe--SecretKey'
  properties: { value: stripeSecretKey }
}

// ── Secrets added manually after deployment (Stripe webhooks, frontend URL) ──
// Set these via Azure Portal or az keyvault secret set after deployment:
//   Stripe--WebhookSecret
//   App--FrontendUrl
//   Stripe--CheckoutSuccessUrl
//   Stripe--CheckoutCancelUrl
//   Stripe--OnboardingReturnUrl
//   Stripe--OnboardingRefreshUrl

output keyVaultName string = keyVault.name
output keyVaultUri string = keyVault.properties.vaultUri
