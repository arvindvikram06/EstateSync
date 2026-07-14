param location string
param prefix string

resource aksIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: '${prefix}-aks-identity'
  location: location
}

resource aks 'Microsoft.ContainerService/managedClusters@2024-02-01' = {
  name: '${prefix}-aks'
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${aksIdentity.id}': {}
    }
  }
  properties: {
    dnsPrefix: '${prefix}-aks'
    agentPoolProfiles: [
      {
        name: 'systempool'
        count: 1
        vmSize: 'Standard_B2as_v2'
        osType: 'Linux'
        mode: 'System'
        enableAutoScaling: false
      }
    ]
    networkProfile: {
      networkPlugin: 'kubenet'
      loadBalancerSku: 'standard'
    }
    oidcIssuerProfile: { enabled: true }
    securityProfile: {
      workloadIdentity: { enabled: true }
    }
  }
}

output aksName string = aks.name
output aksIdentityPrincipalId string = aksIdentity.properties.principalId
output aksIdentityClientId string = aksIdentity.properties.clientId
output oidcIssuerUrl string = aks.properties.oidcIssuerProfile.issuerURL
