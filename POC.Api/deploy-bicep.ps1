# Azure Bicep Deployment Script for POC API
param(
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroupName,
    
    [Parameter(Mandatory=$false)]
    [string]$Location = "East US",
    
    [Parameter(Mandatory=$false)]
    [string]$AppServiceName = "poc-api-appservice-$(Get-Random -Minimum 1000 -Maximum 9999)",
    
    [Parameter(Mandatory=$false)]
    [string]$AppServicePlanName = "poc-api-plan-$(Get-Random -Minimum 1000 -Maximum 9999)",
    
    [Parameter(Mandatory=$false)]
    [string]$SqlServerName = "poc-api-sqlserver-$(Get-Random -Minimum 1000 -Maximum 9999)",
    
    [Parameter(Mandatory=$false)]
    [string]$CosmosDbAccountName = "poc-api-cosmosdb-$(Get-Random -Minimum 1000 -Maximum 9999)",
    
    [Parameter(Mandatory=$false)]
    [string]$ServiceBusNamespaceName = "poc-api-servicebus-$(Get-Random -Minimum 1000 -Maximum 9999)",
    
    [Parameter(Mandatory=$false)]
    [string]$ApplicationInsightsName = "poc-api-insights-$(Get-Random -Minimum 1000 -Maximum 9999)",
    
    [Parameter(Mandatory=$false)]
    [string]$SqlAdminPassword = "P@ssw0rd123!"
)

Write-Host "Starting Azure Bicep deployment for POC API..." -ForegroundColor Green

# Check if Azure CLI is installed and user is logged in
try {
    $azAccount = az account show 2>$null | ConvertFrom-Json
    if (-not $azAccount) {
        Write-Error "Please login to Azure CLI first using 'az login'"
        exit 1
    }
    Write-Host "Logged in as: $($azAccount.user.name)" -ForegroundColor Yellow
} catch {
    Write-Error "Azure CLI is not installed or not accessible. Please install Azure CLI first."
    exit 1
}

# Create resource group
Write-Host "Creating resource group: $ResourceGroupName" -ForegroundColor Yellow
az group create --name $ResourceGroupName --location $Location

# Deploy Bicep template
Write-Host "Deploying Azure resources using Bicep..." -ForegroundColor Yellow
$deploymentResult = az deployment group create `
    --resource-group $ResourceGroupName `
    --template-file "azure-deployment.bicep" `
    --parameters `
        appServiceName=$AppServiceName `
        appServicePlanName=$AppServicePlanName `
        sqlServerName=$SqlServerName `
        sqlDatabaseName="PocDb" `
        cosmosDbAccountName=$CosmosDbAccountName `
        serviceBusNamespaceName=$ServiceBusNamespaceName `
        applicationInsightsName=$ApplicationInsightsName `
        administratorLogin="sqladmin" `
        administratorLoginPassword=$SqlAdminPassword `
    --output json

if ($LASTEXITCODE -ne 0) {
    Write-Error "Bicep template deployment failed"
    exit 1
}

$deployment = $deploymentResult | ConvertFrom-Json
Write-Host "Azure resources deployed successfully!" -ForegroundColor Green

# Build and deploy the application
Write-Host "Building application..." -ForegroundColor Yellow
dotnet publish -c Release -o ./publish

if ($LASTEXITCODE -ne 0) {
    Write-Error "Application build failed"
    exit 1
}

# Deploy to App Service using zip deployment
Write-Host "Deploying application to App Service..." -ForegroundColor Yellow
Compress-Archive -Path "./publish/*" -DestinationPath "./publish.zip" -Force

az webapp deployment source config-zip `
    --resource-group $ResourceGroupName `
    --name $AppServiceName `
    --src "./publish.zip"

if ($LASTEXITCODE -ne 0) {
    Write-Error "Application deployment failed"
    exit 1
}

# Clean up local files
Remove-Item "./publish.zip" -Force
Remove-Item "./publish" -Recurse -Force

# Get the App Service URL from deployment outputs
$appServiceUrl = $deployment.properties.outputs.appServiceUrl.value

Write-Host "`nDeployment completed successfully!" -ForegroundColor Green
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "App Service URL: $appServiceUrl" -ForegroundColor White
Write-Host "Resource Group: $ResourceGroupName" -ForegroundColor White
Write-Host "SQL Server: $SqlServerName.database.windows.net" -ForegroundColor White
Write-Host "Cosmos DB: $CosmosDbAccountName" -ForegroundColor White
Write-Host "Service Bus: $ServiceBusNamespaceName" -ForegroundColor White
Write-Host "Application Insights: $ApplicationInsightsName" -ForegroundColor White
Write-Host "===============================================" -ForegroundColor Cyan

Write-Host "`nTesting the API..." -ForegroundColor Yellow
Start-Sleep -Seconds 30  # Wait for the app to start

try {
    $response = Invoke-RestMethod -Uri "$appServiceUrl/healthz" -Method Get -TimeoutSec 30
    Write-Host "Health check response: $($response | ConvertTo-Json)" -ForegroundColor Green
} catch {
    Write-Host "Health check failed. The application might still be starting up." -ForegroundColor Yellow
    Write-Host "You can check the health endpoint manually at: $appServiceUrl/healthz" -ForegroundColor Yellow
}

Write-Host "`nYou can now test your API endpoints:" -ForegroundColor Yellow
Write-Host "GET  $appServiceUrl/api/todo" -ForegroundColor White
Write-Host "POST $appServiceUrl/api/todo" -ForegroundColor White
Write-Host "GET  $appServiceUrl/api/mongo" -ForegroundColor White
Write-Host "POST $appServiceUrl/api/mongo" -ForegroundColor White
Write-Host "POST $appServiceUrl/api/messagequeue/publish" -ForegroundColor White
Write-Host "Note: Service Bus queues need to be created manually in Azure portal or via ARM/Bicep templates" -ForegroundColor Yellow
Write-Host "GET  $appServiceUrl/healthz" -ForegroundColor White
