@description('Name of the App Service')
@maxLength(60)
param appServiceName string = 'poc-api-appservice'

@description('Name of the App Service Plan')
@maxLength(60)
param appServicePlanName string = 'poc-api-plan'

@description('Name of the SQL Server')
@maxLength(60)
param sqlServerName string = 'poc-api-sqlserver'

@description('Name of the SQL Database')
param sqlDatabaseName string = 'PocDb'

@description('Name of the Cosmos DB Account (MongoDB API)')
@maxLength(44)
param cosmosDbAccountName string = 'poc-api-cosmosdb'

@description('Name of the Service Bus Namespace')
@maxLength(50)
param serviceBusNamespaceName string = 'poc-api-servicebus'

@description('Name of the Application Insights')
@maxLength(260)
param applicationInsightsName string = 'poc-api-insights'

@description('Location for all resources')
param location string = resourceGroup().location

@description('Administrator username for SQL Server')
@maxLength(128)
param administratorLogin string = 'sqladmin'

@description('Administrator password for SQL Server')
@secure()
@minLength(8)
@maxLength(128)
param administratorLoginPassword string = 'P@ssw0rd123!'

var appServicePlanSku = 'B1'
var appServicePlanTier = 'Basic'
var sqlServerVersion = '12.0'

// App Service Plan
resource appServicePlan 'Microsoft.Web/serverfarms@2022-03-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: appServicePlanSku
    tier: appServicePlanTier
  }
  properties: {
    reserved: true
  }
}

// SQL Server
resource sqlServer 'Microsoft.Sql/servers@2021-11-01' = {
  name: sqlServerName
  location: location
  properties: {
    administratorLogin: administratorLogin
    administratorLoginPassword: administratorLoginPassword
    version: sqlServerVersion
  }
}

// SQL Database
resource sqlDatabase 'Microsoft.Sql/servers/databases@2021-11-01' = {
  parent: sqlServer
  name: sqlDatabaseName
  location: location
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
  }
}

// SQL Server Firewall Rule - Allow Azure Services
resource sqlFirewallRule 'Microsoft.Sql/servers/firewallRules@2021-11-01' = {
  parent: sqlServer
  name: 'AllowAllAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// Cosmos DB Account (MongoDB API)
resource cosmosDbAccount 'Microsoft.DocumentDB/databaseAccounts@2021-10-15' = {
  name: cosmosDbAccountName
  location: location
  properties: {
    capabilities: [
      {
        name: 'EnableMongo'
      }
    ]
    databaseAccountOfferType: 'Standard'
    locations: [
      {
        locationName: location
        failoverPriority: 0
      }
    ]
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
  }
}

// Service Bus Namespace
resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2021-06-01-preview' = {
  name: serviceBusNamespaceName
  location: location
  properties: {
    messagingSku: 'Basic'
  }
}

// Application Insights
resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: applicationInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    Request_Source: 'rest'
  }
}

// App Service
resource appService 'Microsoft.Web/sites@2022-03-01' = {
  name: appServiceName
  location: location
  dependsOn: [
    appServicePlan
    sqlDatabase
    cosmosDbAccount
    serviceBusNamespace
    applicationInsights
  ]
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      appSettings: [
        {
          name: 'ConnectionStrings__Sql'
          value: 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Initial Catalog=${sqlDatabaseName};Persist Security Info=False;User ID=${administratorLogin};Password=${administratorLoginPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
        }
        {
          name: 'ConnectionStrings__Mongo'
          value: reference(cosmosDbAccount.id, '2021-10-15').connectionStrings[0].connectionString
        }
        {
          name: 'ServiceBus__ConnectionString'
          value: listKeys(serviceBusNamespace.id, '2021-06-01-preview').primaryConnectionString
        }
        {
          name: 'ApplicationInsights__ConnectionString'
          value: reference(applicationInsights.id).ConnectionString
        }
      ]
    }
  }
}

output appServiceName string = appServiceName
output appServiceUrl string = 'https://${reference(appService.id).defaultHostName}'
output sqlServerName string = sqlServerName
output cosmosDbAccountName string = cosmosDbAccountName
output serviceBusNamespaceName string = serviceBusNamespaceName
output applicationInsightsName string = applicationInsightsName
