param location string
param prefix string
param aksIdentityPrincipalId string

param deployerObjectId string

@secure()
param postgresConnectionString string
param frontendUrl string
@secure()
param blobConnectionString string
@secure()
param signalRConnectionString string

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
      {
        tenantId: subscription().tenantId
        objectId: aksIdentityPrincipalId
        permissions: { secrets: [ 'get', 'list' ] }
      }
      {
        tenantId: subscription().tenantId
        objectId: deployerObjectId
        permissions: { secrets: [ 'get', 'list', 'set', 'delete' ] }
      }
    ]
  }
}

resource secretPostgres 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'ConnectionStrings--DefaultConnection'
  properties: { value: postgresConnectionString }
}

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

resource secretFrontendUrl 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'App--FrontendUrl'
  properties: { value: frontendUrl }
}

output keyVaultName string = keyVault.name
output keyVaultUri string = keyVault.properties.vaultUri
