param location string = resourceGroup().location
param prefix string = 'estatesync'
param postgresAdminUser string
@secure()
param postgresAdminPassword string

param deployerObjectId string

@secure()
param jwtSecretKey string
@secure()
param stripeSecretKey string

module acr 'modules/acr.bicep' = {
  name: 'acr'
  params: { location: location, prefix: prefix }
}

module aks 'modules/aks.bicep' = {
  name: 'aks'
  params: { location: location, prefix: prefix }
}

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

module keyVault 'modules/keyvault.bicep' = {
  name: 'keyvault'
  params: {
    location: location
    prefix: prefix
    aksIdentityPrincipalId: aks.outputs.aksIdentityPrincipalId
    deployerObjectId: deployerObjectId
    postgresConnectionString: 'Host=${postgres.outputs.postgresHost};Database=${postgres.outputs.postgresDbName};Username=${postgresAdminUser};Password=${postgresAdminPassword};SslMode=Require;'
    frontendUrl: staticWebApp.outputs.staticWebAppUrl
    blobConnectionString: storage.outputs.storageConnectionString
    signalRConnectionString: signalR.outputs.signalRConnectionString
    jwtSecretKey: jwtSecretKey
    stripeSecretKey: stripeSecretKey
  }
}

module staticWebApp 'modules/staticwebapp.bicep' = {
  name: 'staticwebapp'
  params: { prefix: prefix }
}


output acrLoginServer string = acr.outputs.acrLoginServer
output aksName string = aks.outputs.aksName
output aksIdentityClientId string = aks.outputs.aksIdentityClientId
output oidcIssuerUrl string = aks.outputs.oidcIssuerUrl
output keyVaultUri string = keyVault.outputs.keyVaultUri
output keyVaultName string = keyVault.outputs.keyVaultName
output staticWebAppUrl string = staticWebApp.outputs.staticWebAppUrl
output staticWebAppDeploymentToken string = staticWebApp.outputs.deploymentToken
