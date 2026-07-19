param location string
param prefix string

resource redis 'Microsoft.Cache/redisEnterprise@2025-04-01' = {
  name: '${prefix}-redis'
  location: location
  sku: { name: 'Balanced_B0', capacity: 1 }
}

resource redisDb 'Microsoft.Cache/redisEnterprise/databases@2025-04-01' = {
  parent: redis
  name: 'default'
  properties: {
    clientProtocol: 'Encrypted'
    clusteringPolicy: 'OSSCluster'
    evictionPolicy: 'VolatileLRU'
    port: 10000
  }
}

output redisHost string = redis.properties.hostName
output redisPort int = 10000
@secure()
output redisPrimaryKey string = redisDb.listKeys().primaryKey
