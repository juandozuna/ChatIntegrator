param location string = resourceGroup().location
param environment string = 'prod'
param appName string = 'mentionsync'
param administratorLogin string = 'mentionsyncadmin'
@secure()
param administratorPassword string
param openAiApiKey string

var appServicePlanName = '${appName}-${environment}-plan'
var webAppName = '${appName}-${environment}-api'
var dbServerName = toLower('${appName}${environment}pg')
var databaseName = 'mentionsync'
var keyVaultName = '${appName}-${environment}-kv'

resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: 'P1v3'
    tier: 'PremiumV3'
    capacity: 1
  }
  properties: {
    reserved: true
  }
}

resource app 'Microsoft.Web/sites@2023-01-01' = {
  name: webAppName
  location: location
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: environment
        }
        {
          name: 'ConnectionStrings__pg'
          value: '@Microsoft.KeyVault(SecretUri=${databaseSecret.properties.secretUriWithVersion})'
        }
        {
          name: 'OpenAI__ApiKey'
          value: '@Microsoft.KeyVault(SecretUri=${openAiSecret.properties.secretUriWithVersion})'
        }
      ]
    }
  }
  identity: {
    type: 'SystemAssigned'
  }
  kind: 'app,linux'
}

resource storage 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: '${appName}${environment}logs'
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
}

resource kv 'Microsoft.KeyVault/vaults@2022-11-01' = {
  name: keyVaultName
  location: location
  properties: {
    tenantId: subscription().tenantId
    sku: {
      name: 'standard'
      family: 'A'
    }
    enablePurgeProtection: true
    enableSoftDelete: true
    accessPolicies: [
      {
        tenantId: subscription().tenantId
        objectId: app.identity.principalId
        permissions: {
          secrets: ['get', 'list']
        }
      }
    ]
  }
}

resource dbServer 'Microsoft.DBforPostgreSQL/flexibleServers@2023-06-01-preview' = {
  name: dbServerName
  location: location
  sku: {
    name: 'Standard_D2ds_v4'
    tier: 'GeneralPurpose'
    capacity: 2
  }
  properties: {
    administratorLogin: administratorLogin
    administratorLoginPassword: administratorPassword
    version: '16'
    storage: {
      storageSizeGB: 64
    }
    availabilityZone: '1'
  }
}

resource firewall 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2023-06-01-preview' = {
  parent: dbServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource database 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2023-06-01-preview' = {
  parent: dbServer
  name: databaseName
  properties: {
    charset: 'UTF8'
    collation: 'en_US.UTF8'
  }
}

resource databaseSecret 'Microsoft.KeyVault/vaults/secrets@2022-11-01' = {
  parent: kv
  name: 'pg-connection-string'
  properties: {
    value: 'Host=${dbServer.properties.fullyQualifiedDomainName};Database=${databaseName};Username=${administratorLogin};Password=${administratorPassword};Ssl Mode=Require;'
  }
}

resource openAiSecret 'Microsoft.KeyVault/vaults/secrets@2022-11-01' = {
  parent: kv
  name: 'openai-api-key'
  properties: {
    value: openAiApiKey
  }
}

resource insights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${appName}-${environment}-appi'
  location: location
  properties: {
    Application_Type: 'web'
    Flow_Type: 'Bluefield'
    Request_Source: 'rest'
  }
}

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${appName}-${environment}-logs'
  location: location
  properties: {
    retentionInDays: 30
    sku: {
      name: 'PerGB2018'
    }
  }
}

resource monitoring 'Microsoft.Insights/Components/CurrentBillingFeatures@2015-05-01' = {
  parent: insights
  name: 'CurrentBillingFeatures'
  properties: {
    CurrentBillingFeatures: 'Basic'
    DataVolumeCap: 'Unlimited'
  }
}

output webAppHostname string = app.properties.defaultHostName
