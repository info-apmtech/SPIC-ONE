// SPIC ONE — Azure platform for one environment (staging or prod).
//
// Deployed twice by deploy/azure/provision.ps1:
//   pass 1  deployApps=false   registry, database, storage, vault, logs, environment
//   pass 2  deployApps=true    the two container apps, once images exist in the registry
// deploy/azure/deploy.ps1 re-runs pass 2 with new image tags on every release.
targetScope = 'resourceGroup'

// ---------------------------------------------------------------- parameters
@allowed(['staging', 'prod'])
param envName string

param location string = resourceGroup().location

@description('False deploys only the platform. True also deploys the container apps (needs apiImage/webImage).')
param deployApps bool = false
param apiImage string = ''
param webImage string = ''

param apiMinReplicas int = 1
param apiMaxReplicas int = 2
param webMinReplicas int = 1
param webMaxReplicas int = 2
param apiCpu string = '0.5'
param apiMemory string = '1Gi'
param webCpu string = '0.5'
param webMemory string = '1Gi'
@description('Concurrent HTTP requests per replica before another replica is added.')
param httpConcurrency int = 50

param postgresSku string = 'Standard_B1ms'
@allowed(['Burstable', 'GeneralPurpose', 'MemoryOptimized'])
param postgresTier string = 'Burstable'
param postgresStorageGb int = 32
@allowed(['Disabled', 'ZoneRedundant', 'SameZone'])
param postgresHaMode string = 'Disabled'
param postgresBackupRetentionDays int = 7
param postgresVersion string = '16'

@description('Spread Container Apps replicas across availability zones (production).')
param zoneRedundant bool = false
param logRetentionDays int = 30
param keyVaultPurgeProtection bool = false

@description('Object id of the identity running the deployment. Gets Key Vault Secrets Officer so the scripts can read generated secrets.')
param deployerObjectId string = ''
@description('Optional admin client IP allowed through the PostgreSQL firewall (migrations, data copy).')
param clientIp string = ''

// Secrets. Generated on first deployment and stored in Key Vault; the scripts
// read them back and pass the same values on every later deployment.
@secure()
param postgresAdminPassword string = newGuid()
@secure()
param jwtKey string = newGuid()
param jwtIssuer string = 'SPIC_API'
param jwtAudience string = 'SPIC_API_USERS'
@secure()
param ifmsDeviceKey string = ''
@secure()
param ifmsAutomationKey string = ''
@description('Connection string of the IFMS automation database (stays on the VPS). Empty = API falls back to its own database.')
@secure()
param ifmsConnectionString string = ''
@description('Public URL the browser uses to reach the API. Empty = the API container app FQDN.')
param webApiBaseUrl string = ''

// ---------------------------------------------------------------- names
var envShort = envName == 'prod' ? 'prd' : 'stg'
var suffix = take(uniqueString(resourceGroup().id), 6)
var tags = { app: 'spicone', env: envName, client: 'SPIC' }

var lawName = 'log-spicone-${envShort}'
var vnetName = 'vnet-spicone-${envShort}'
var storageName = 'stspicone${envShort}${suffix}'
var acrName = 'crspicone${envShort}${suffix}'
var uaiName = 'id-spicone-${envShort}'
var kvName = 'kv-spicone-${envShort}-${suffix}'
var pgName = 'pg-spicone-${envShort}-${suffix}'
var caeName = 'cae-spicone-${envShort}'
var apiAppName = 'ca-spicone-api-${envShort}'
var webAppName = 'ca-spicone-web-${envShort}'

var pgAdminLogin = 'spicadmin'
var pgDatabaseName = 'spicone'

// Azure Files shares mounted into the apps.
var shares = [
  'api-uploads'      // SpicAPI/Uploads       (dealer registration documents, sample downloads)
  'api-webuploads'   // SpicAPI/wwwroot/uploads (logistics files)
  'api-keys'         // API data-protection key ring
  'web-keys'         // Web data-protection key ring
]

// ---------------------------------------------------------------- logs
resource law 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: lawName
  location: location
  tags: tags
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: logRetentionDays
  }
}

// ---------------------------------------------------------------- network
resource vnet 'Microsoft.Network/virtualNetworks@2024-01-01' = {
  name: vnetName
  location: location
  tags: tags
  properties: {
    addressSpace: { addressPrefixes: ['10.20.0.0/16'] }
    subnets: [
      {
        name: 'aca'
        properties: {
          addressPrefix: '10.20.0.0/23'
          delegations: [
            { name: 'aca', properties: { serviceName: 'Microsoft.App/environments' } }
          ]
        }
      }
    ]
  }
}

// ---------------------------------------------------------------- storage
resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageName
  location: location
  tags: tags
  kind: 'StorageV2'
  sku: { name: 'Standard_LRS' }
  properties: {
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    supportsHttpsTrafficOnly: true
    allowSharedKeyAccess: true // Container Apps environment storage mounts use the account key
  }
}

resource fileService 'Microsoft.Storage/storageAccounts/fileServices@2023-05-01' = {
  parent: storage
  name: 'default'
}

resource fileShares 'Microsoft.Storage/storageAccounts/fileServices/shares@2023-05-01' = [for share in shares: {
  parent: fileService
  name: share
  properties: { shareQuota: 100, accessTier: 'TransactionOptimized' }
}]

// ---------------------------------------------------------------- registry + identity
resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: acrName
  location: location
  tags: tags
  sku: { name: 'Basic' }
  properties: { adminUserEnabled: false }
}

resource uai 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: uaiName
  location: location
  tags: tags
}

var acrPullRoleId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
var kvSecretsUserRoleId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
var kvSecretsOfficerRoleId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'b86a8fe4-44ce-4948-aee5-eccb2c155cd7')

resource acrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(acr.id, uai.id, 'acrpull')
  scope: acr
  properties: {
    roleDefinitionId: acrPullRoleId
    principalId: uai.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

// ---------------------------------------------------------------- key vault
resource kv 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: kvName
  location: location
  tags: tags
  properties: {
    tenantId: subscription().tenantId
    sku: { family: 'A', name: 'standard' }
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    enablePurgeProtection: keyVaultPurgeProtection ? true : null
    publicNetworkAccess: 'Enabled'
  }
}

resource kvSecretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(kv.id, uai.id, 'kv-secrets-user')
  scope: kv
  properties: {
    roleDefinitionId: kvSecretsUserRoleId
    principalId: uai.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource kvSecretsOfficer 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(deployerObjectId)) {
  name: guid(kv.id, deployerObjectId, 'kv-secrets-officer')
  scope: kv
  properties: {
    roleDefinitionId: kvSecretsOfficerRoleId
    principalId: deployerObjectId
    principalType: 'User'
  }
}

// ---------------------------------------------------------------- postgresql
resource pg 'Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01' = {
  name: pgName
  location: location
  tags: tags
  sku: { name: postgresSku, tier: postgresTier }
  properties: {
    version: postgresVersion
    administratorLogin: pgAdminLogin
    administratorLoginPassword: postgresAdminPassword
    storage: { storageSizeGB: postgresStorageGb, autoGrow: 'Enabled' }
    backup: { backupRetentionDays: postgresBackupRetentionDays, geoRedundantBackup: 'Disabled' }
    highAvailability: { mode: postgresHaMode }
    network: { publicNetworkAccess: 'Enabled' }
    authConfig: { passwordAuth: 'Enabled', activeDirectoryAuth: 'Disabled' }
  }
}

resource pgDb 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2024-08-01' = {
  parent: pg
  name: pgDatabaseName
  properties: { charset: 'UTF8', collation: 'en_US.utf8' }
}

// Lets the container apps (Azure-internal traffic) reach the server.
resource pgFwAzure 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2024-08-01' = {
  parent: pg
  name: 'AllowAzureServices'
  properties: { startIpAddress: '0.0.0.0', endIpAddress: '0.0.0.0' }
}

resource pgFwClient 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2024-08-01' = if (!empty(clientIp)) {
  parent: pg
  name: 'AdminClient'
  properties: { startIpAddress: clientIp, endIpAddress: clientIp }
}

var dbConnectionString = 'Host=${pg.properties.fullyQualifiedDomainName};Port=5432;Database=${pgDatabaseName};Username=${pgAdminLogin};Password=${postgresAdminPassword};SSL Mode=Require;Trust Server Certificate=true;Include Error Detail=true'

// ---------------------------------------------------------------- secrets
resource secretPgPassword 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: kv
  name: 'postgres-admin-password'
  properties: { value: postgresAdminPassword }
}

resource secretDbConnection 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: kv
  name: 'db-connection'
  properties: { value: dbConnectionString }
}

resource secretJwtKey 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: kv
  name: 'jwt-key'
  properties: { value: jwtKey }
}

resource secretIfmsDeviceKey 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = if (!empty(ifmsDeviceKey)) {
  parent: kv
  name: 'ifms-device-key'
  properties: { value: ifmsDeviceKey }
}

resource secretIfmsAutomationKey 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = if (!empty(ifmsAutomationKey)) {
  parent: kv
  name: 'ifms-automation-key'
  properties: { value: ifmsAutomationKey }
}

resource secretIfmsConnection 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = if (!empty(ifmsConnectionString)) {
  parent: kv
  name: 'ifms-connection'
  properties: { value: ifmsConnectionString }
}

// ---------------------------------------------------------------- container apps environment
resource cae 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: caeName
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: law.properties.customerId
        sharedKey: law.listKeys().primarySharedKey
      }
    }
    vnetConfiguration: {
      infrastructureSubnetId: vnet.properties.subnets[0].id
      internal: false
    }
    workloadProfiles: [
      { name: 'Consumption', workloadProfileType: 'Consumption' }
    ]
    zoneRedundant: zoneRedundant
  }
}

resource caeStorages 'Microsoft.App/managedEnvironments/storages@2024-03-01' = [for (share, i) in shares: {
  parent: cae
  name: share
  properties: {
    azureFile: {
      accountName: storage.name
      accountKey: storage.listKeys().keys[0].value
      shareName: share
      accessMode: 'ReadWrite'
    }
  }
  dependsOn: [fileShares[i]]
}]

// ---------------------------------------------------------------- container apps
var probes = [
  { type: 'Startup', httpGet: { path: '/health', port: 8080 }, initialDelaySeconds: 5, periodSeconds: 5, failureThreshold: 30 }
  { type: 'Readiness', httpGet: { path: '/health', port: 8080 }, initialDelaySeconds: 5, periodSeconds: 10, failureThreshold: 3 }
  { type: 'Liveness', httpGet: { path: '/health', port: 8080 }, initialDelaySeconds: 15, periodSeconds: 30, failureThreshold: 3 }
]

var apiSecretsBase = [
  { name: 'db-connection', keyVaultUrl: secretDbConnection.properties.secretUri, identity: uai.id }
  { name: 'jwt-key', keyVaultUrl: secretJwtKey.properties.secretUri, identity: uai.id }
]
// Optional secrets are addressed by name under the vault URI so the template never
// dereferences a conditionally-deployed resource.
var apiSecretsIfms = concat(
  empty(ifmsDeviceKey) ? [] : [{ name: 'ifms-device-key', keyVaultUrl: '${kv.properties.vaultUri}secrets/ifms-device-key', identity: uai.id }],
  empty(ifmsAutomationKey) ? [] : [{ name: 'ifms-automation-key', keyVaultUrl: '${kv.properties.vaultUri}secrets/ifms-automation-key', identity: uai.id }],
  empty(ifmsConnectionString) ? [] : [{ name: 'ifms-connection', keyVaultUrl: '${kv.properties.vaultUri}secrets/ifms-connection', identity: uai.id }]
)

var apiEnvBase = [
  { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
  { name: 'ConnectionStrings__DefaultConnection', secretRef: 'db-connection' }
  { name: 'Jwt__Key', secretRef: 'jwt-key' }
  { name: 'Jwt__Issuer', value: jwtIssuer }
  { name: 'Jwt__Audience', value: jwtAudience }
  { name: 'DataProtection__KeysPath', value: '/app/keys' }
]
var apiEnvIfms = concat(
  empty(ifmsDeviceKey) ? [] : [{ name: 'IfmsAutomation__DeviceKey', secretRef: 'ifms-device-key' }],
  empty(ifmsAutomationKey) ? [] : [{ name: 'IfmsAutomation__AutomationKey', secretRef: 'ifms-automation-key' }],
  empty(ifmsConnectionString) ? [] : [{ name: 'ConnectionStrings__IfmsConnection', secretRef: 'ifms-connection' }]
)

resource apiApp 'Microsoft.App/containerApps@2024-03-01' = if (deployApps) {
  name: apiAppName
  location: location
  tags: union(tags, { component: 'api' })
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: { '${uai.id}': {} }
  }
  properties: {
    managedEnvironmentId: cae.id
    workloadProfileName: 'Consumption'
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'
        allowInsecure: false
      }
      registries: [
        { server: acr.properties.loginServer, identity: uai.id }
      ]
      secrets: concat(apiSecretsBase, apiSecretsIfms)
    }
    template: {
      containers: [
        {
          name: 'api'
          image: apiImage
          resources: { cpu: json(apiCpu), memory: apiMemory }
          env: concat(apiEnvBase, apiEnvIfms)
          probes: probes
          volumeMounts: [
            { volumeName: 'uploads', mountPath: '/app/Uploads' }
            { volumeName: 'webuploads', mountPath: '/app/wwwroot/uploads' }
            { volumeName: 'keys', mountPath: '/app/keys' }
          ]
        }
      ]
      scale: {
        minReplicas: apiMinReplicas
        maxReplicas: apiMaxReplicas
        rules: [
          { name: 'http-concurrency', http: { metadata: { concurrentRequests: string(httpConcurrency) } } }
        ]
      }
      volumes: [
        { name: 'uploads', storageType: 'AzureFile', storageName: 'api-uploads' }
        { name: 'webuploads', storageType: 'AzureFile', storageName: 'api-webuploads' }
        { name: 'keys', storageType: 'AzureFile', storageName: 'api-keys' }
      ]
    }
  }
  dependsOn: [acrPull, kvSecretsUser, caeStorages, secretIfmsDeviceKey, secretIfmsAutomationKey, secretIfmsConnection]
}

var apiBaseUrl = !empty(webApiBaseUrl) ? webApiBaseUrl : (deployApps ? 'https://${apiApp.properties.configuration.ingress.fqdn}/' : '')

resource webApp 'Microsoft.App/containerApps@2024-03-01' = if (deployApps) {
  name: webAppName
  location: location
  tags: union(tags, { component: 'web' })
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: { '${uai.id}': {} }
  }
  properties: {
    managedEnvironmentId: cae.id
    workloadProfileName: 'Consumption'
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'
        allowInsecure: false
        // Blazor Server keeps a live circuit per browser tab; the same replica must
        // serve every request of that circuit.
        stickySessions: { affinity: 'sticky' }
      }
      registries: [
        { server: acr.properties.loginServer, identity: uai.id }
      ]
    }
    template: {
      containers: [
        {
          name: 'web'
          image: webImage
          resources: { cpu: json(webCpu), memory: webMemory }
          env: [
            { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
            { name: 'DetailedErrors', value: 'false' }
            { name: 'ApiBaseUrl', value: apiBaseUrl }
            { name: 'DataProtection__KeysPath', value: '/app/keys' }
          ]
          probes: probes
          volumeMounts: [
            { volumeName: 'keys', mountPath: '/app/keys' }
          ]
        }
      ]
      scale: {
        minReplicas: webMinReplicas
        maxReplicas: webMaxReplicas
        rules: [
          { name: 'http-concurrency', http: { metadata: { concurrentRequests: string(httpConcurrency) } } }
        ]
      }
      volumes: [
        { name: 'keys', storageType: 'AzureFile', storageName: 'web-keys' }
      ]
    }
  }
  dependsOn: [acrPull, caeStorages]
}

// ---------------------------------------------------------------- outputs
output resourceGroupName string = resourceGroup().name
output acrName string = acr.name
output acrLoginServer string = acr.properties.loginServer
output keyVaultName string = kv.name
output storageAccountName string = storage.name
output postgresServerName string = pg.name
output postgresFqdn string = pg.properties.fullyQualifiedDomainName
output postgresDatabase string = pgDatabaseName
output postgresAdminLogin string = pgAdminLogin
output environmentName string = cae.name
output logAnalyticsName string = law.name
output apiAppName string = apiAppName
output webAppName string = webAppName
output apiFqdn string = deployApps ? apiApp.properties.configuration.ingress.fqdn : ''
output webFqdn string = deployApps ? webApp.properties.configuration.ingress.fqdn : ''
