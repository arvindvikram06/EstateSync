param location string
param prefix string

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: '${replace(prefix, '-', '')}storage'
  location: location
  sku: { name: 'Standard_LRS' }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: true
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storageAccount
  name: 'default'
}

// Public container — property images, complaint images
resource publicContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: 'estatesync-public'
  properties: {
    publicAccess: 'Blob'
  }
}

// Private container — KYC docs, lease PDFs, signatures (SAS URL required to access)
resource privateContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: 'estatesync-private'
  properties: {
    publicAccess: 'None'
  }
}

output storageAccountName string = storageAccount.name
@secure()
output storageConnectionString string = 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=core.windows.net'
