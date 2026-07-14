param location string = resourceGroup().location
param prefix string = 'estatesync'
param postgresAdminUser string
@secure()
param postgresAdminPassword string

// Your Azure AD object ID — run: az ad signed-in-user show --query id -o tsv
param deployerObjectId string

// Supplied manually — not derivable from Azure resources
@secure()
param jwtSecretKey string
@secure()
param stripeSecretKey string

// ── ACR — provisioned first, AKS depends on it ───────────────────────────────
module acr 'modules/acr.bicep' = {
  name: 'acr'
  params: { location: location, prefix: prefix }
}

// ── AKS — depends on ACR for pull role assignment ────────────────────────────
module aks 'modules/aks.bicep' = {
  name: 'aks'
  params: { location: location, prefix: prefix }
}

// ── Data & messaging — independent, deploy in parallel ───────────────────────
module postgres 'modules/postgres.bicep' = {
  name: 'postgres'
  params: {
    location: location
    prefix: prefix
    adminUser: postgresAdminUser
    adminPassword: postgresAdminPassword
  }
}

module storage 'modules/storage.bicep' = {
  name: 'storage'
  params: { location: location, prefix: prefix }
}

module signalR 'modules/signalr.bicep' = {
  name: 'signalr'
  params: { location: location, prefix: prefix }
}

// ── Key Vault — depends on AKS identity + all service outputs ────────────────
module keyVault 'modules/keyvault.bicep' = {
  name: 'keyvault'
  params: {
    location: location
    prefix: prefix
    aksIdentityPrincipalId: aks.outputs.aksIdentityPrincipalId
    deployerObjectId: deployerObjectId
    // Azure service connection strings built from module outputs
    postgresConnectionString: 'Host=${postgres.outputs.postgresHost};Database=${postgres.outputs.postgresDbName};Username=${postgresAdminUser};Password=${postgresAdminPassword};SslMode=Require;'
    blobConnectionString: storage.outputs.storageConnectionString
    signalRConnectionString: signalR.outputs.signalRConnectionString
    // Manually supplied secrets
    jwtSecretKey: jwtSecretKey
    stripeSecretKey: stripeSecretKey
  }
}

// ── Frontend ──────────────────────────────────────────────────────────────────
module staticWebApp 'modules/staticwebapp.bicep' = {
  name: 'staticwebapp'
  params: { prefix: prefix }
}

// ── Outputs — needed for Phase 4 (Docker push) and Phase 5 (k8s deploy) ─────
output acrLoginServer string = acr.outputs.acrLoginServer
output aksName string = aks.outputs.aksName
output aksIdentityClientId string = aks.outputs.aksIdentityClientId
output oidcIssuerUrl string = aks.outputs.oidcIssuerUrl
output keyVaultUri string = keyVault.outputs.keyVaultUri
output keyVaultName string = keyVault.outputs.keyVaultName
output staticWebAppUrl string = staticWebApp.outputs.staticWebAppUrl
output staticWebAppDeploymentToken string = staticWebApp.outputs.deploymentToken
