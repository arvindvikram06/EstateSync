param prefix string

resource staticWebApp 'Microsoft.Web/staticSites@2023-01-01' = {
  name: '${prefix}-frontend'
  location: 'centralus'
  sku: { name: 'Free', tier: 'Free' }
  properties: {}
}

output staticWebAppName string = staticWebApp.name
output staticWebAppUrl string = 'https://${staticWebApp.properties.defaultHostname}'
@secure()
output deploymentToken string = staticWebApp.listSecrets().properties.apiKey
