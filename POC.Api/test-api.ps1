# API Testing Script for POC API
param(
    [Parameter(Mandatory=$false)]
    [string]$BaseUrl = "https://localhost:5001"
)

Write-Host "Testing POC API endpoints..." -ForegroundColor Green
Write-Host "Base URL: $BaseUrl" -ForegroundColor Yellow

# Test health endpoint
Write-Host "`n1. Testing Health Check..." -ForegroundColor Cyan
try {
    $healthResponse = Invoke-RestMethod -Uri "$BaseUrl/healthz" -Method Get -SkipCertificateCheck
    Write-Host "✓ Health Check: OK" -ForegroundColor Green
    Write-Host "Response: $($healthResponse | ConvertTo-Json -Compress)" -ForegroundColor Gray
} catch {
    Write-Host "✗ Health Check: Failed - $($_.Exception.Message)" -ForegroundColor Red
}

# Test root endpoint
Write-Host "`n2. Testing Root Endpoint..." -ForegroundColor Cyan
try {
    $rootResponse = Invoke-RestMethod -Uri $BaseUrl -Method Get -SkipCertificateCheck
    Write-Host "✓ Root Endpoint: OK" -ForegroundColor Green
    Write-Host "Response: $($rootResponse | ConvertTo-Json -Compress)" -ForegroundColor Gray
} catch {
    Write-Host "✗ Root Endpoint: Failed - $($_.Exception.Message)" -ForegroundColor Red
}

# Test Todo endpoints
Write-Host "`n3. Testing Todo Endpoints (SQL Server)..." -ForegroundColor Cyan

# Create a todo
try {
    $todoData = @{
        title = "Test Todo"
        description = "This is a test todo created by the test script"
        category = "Testing"
        priority = 3
    } | ConvertTo-Json

    $createResponse = Invoke-RestMethod -Uri "$BaseUrl/api/todo" -Method Post -Body $todoData -ContentType "application/json" -SkipCertificateCheck
    Write-Host "✓ Create Todo: OK" -ForegroundColor Green
    Write-Host "Created Todo ID: $($createResponse.id)" -ForegroundColor Gray
    
    $todoId = $createResponse.id
    
    # Get all todos
    $todosResponse = Invoke-RestMethod -Uri "$BaseUrl/api/todo" -Method Get -SkipCertificateCheck
    Write-Host "✓ Get All Todos: OK (Count: $($todosResponse.Count))" -ForegroundColor Green
    
    # Get todo by ID
    $todoByIdResponse = Invoke-RestMethod -Uri "$BaseUrl/api/todo/$todoId" -Method Get -SkipCertificateCheck
    Write-Host "✓ Get Todo by ID: OK" -ForegroundColor Green
    
    # Update todo
    $updateData = @{
        id = $todoId
        title = "Updated Test Todo"
        description = "This todo has been updated"
        category = "Testing"
        priority = 5
        done = $true
    } | ConvertTo-Json
    
    $updateResponse = Invoke-RestMethod -Uri "$BaseUrl/api/todo/$todoId" -Method Put -Body $updateData -ContentType "application/json" -SkipCertificateCheck
    Write-Host "✓ Update Todo: OK" -ForegroundColor Green
    
    # Get pending todos
    $pendingResponse = Invoke-RestMethod -Uri "$BaseUrl/api/todo/pending" -Method Get -SkipCertificateCheck
    Write-Host "✓ Get Pending Todos: OK (Count: $($pendingResponse.Count))" -ForegroundColor Green
    
} catch {
    Write-Host "✗ Todo Endpoints: Failed - $($_.Exception.Message)" -ForegroundColor Red
}

# Test MongoDB endpoints
Write-Host "`n4. Testing MongoDB Endpoints..." -ForegroundColor Cyan

try {
    $documentData = @{
        name = "Test Document"
        content = "This is a test document created by the test script"
        tags = @("test", "document", "api")
        metadata = @{
            author = "test-script"
            version = "1.0"
        }
    } | ConvertTo-Json

    $createDocResponse = Invoke-RestMethod -Uri "$BaseUrl/api/mongo" -Method Post -Body $documentData -ContentType "application/json" -SkipCertificateCheck
    Write-Host "✓ Create Document: OK" -ForegroundColor Green
    Write-Host "Created Document ID: $($createDocResponse.id)" -ForegroundColor Gray
    
    $docId = $createDocResponse.id
    
    # Get all documents
    $docsResponse = Invoke-RestMethod -Uri "$BaseUrl/api/mongo" -Method Get -SkipCertificateCheck
    Write-Host "✓ Get All Documents: OK (Count: $($docsResponse.Count))" -ForegroundColor Green
    
    # Get document by ID
    $docByIdResponse = Invoke-RestMethod -Uri "$BaseUrl/api/mongo/$docId" -Method Get -SkipCertificateCheck
    Write-Host "✓ Get Document by ID: OK" -ForegroundColor Green
    
    # Search documents
    $searchResponse = Invoke-RestMethod -Uri "$BaseUrl/api/mongo/search?term=test" -Method Get -SkipCertificateCheck
    Write-Host "✓ Search Documents: OK (Count: $($searchResponse.Count))" -ForegroundColor Green
    
} catch {
    Write-Host "✗ MongoDB Endpoints: Failed - $($_.Exception.Message)" -ForegroundColor Red
}

# Test Message Queue endpoints
Write-Host "`n5. Testing Message Queue Endpoints (RabbitMQ)..." -ForegroundColor Cyan

try {
    # Test message queue health
    $mqHealthResponse = Invoke-RestMethod -Uri "$BaseUrl/api/messagequeue/health" -Method Get -SkipCertificateCheck
    Write-Host "✓ Message Queue Health: $($mqHealthResponse.healthy)" -ForegroundColor Green
    
    # Publish simple message
    $simpleMessageData = @{
        content = "Hello from test script!"
        queueName = "test-queue"
        messageType = "info"
    } | ConvertTo-Json
    
    $publishResponse = Invoke-RestMethod -Uri "$BaseUrl/api/messagequeue/publish" -Method Post -Body $simpleMessageData -ContentType "application/json" -SkipCertificateCheck
    Write-Host "✓ Publish Simple Message: OK" -ForegroundColor Green
    
    # Publish detailed message
    $detailedMessageData = @{
        content = "Detailed test message"
        type = "test"
        source = "test-script"
        properties = @{
            testId = "12345"
            timestamp = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ssZ")
        }
    } | ConvertTo-Json
    
    $publishDetailedResponse = Invoke-RestMethod -Uri "$BaseUrl/api/messagequeue/publish/detailed?queueName=test-queue" -Method Post -Body $detailedMessageData -ContentType "application/json" -SkipCertificateCheck
    Write-Host "✓ Publish Detailed Message: OK" -ForegroundColor Green
    
} catch {
    Write-Host "✗ Message Queue Endpoints: Failed - $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`nAPI Testing completed!" -ForegroundColor Green
Write-Host "Note: Some tests may fail if the required services (SQL Server, MongoDB, RabbitMQ) are not running locally." -ForegroundColor Yellow
