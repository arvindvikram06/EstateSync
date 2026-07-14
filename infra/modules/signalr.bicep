param location string
param prefix string

resource signalR 'Microsoft.SignalRService/signalR@2023-08-01-preview' = {
  name: '${prefix}-signalr'
  location: location
  sku: { name: 'Free_F1', capacity: 1 }
  properties: {
    features: [
      { flag: 'ServiceMode', value: 'Default' }
    ]
    cors: {
      allowedOrigins: [ '*' ]
    }
  }
}

@secure()
output signalRConnectionString string = signalR.listKeys().primaryConnectionString
