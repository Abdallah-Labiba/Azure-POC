# POC API - Multi-Database Integration with Azure

This is a .NET 9 Web API project that demonstrates integration with SQL Server, MongoDB, and Azure Service Bus, designed for deployment on Azure App Services.

## Features

- **SQL Server Integration**: Full CRUD operations for Todo entities using Entity Framework Core
- **MongoDB Integration**: Document-based operations using MongoDB Driver
- **Azure Service Bus Integration**: Message publishing and consumption capabilities
- **Health Checks**: Comprehensive health monitoring for all services
- **Azure Ready**: Pre-configured for Azure deployment with ARM templates and Bicep files

## Architecture

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   .NET API      │    │   SQL Server    │    │   MongoDB       │
│                 │◄──►│   (Todos)       │    │   (Documents)   │
│                 │    │                 │    │                 │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │
         ▼
┌─────────────────┐
│ Azure Service   │
│   Bus (Messages)│
└─────────────────┘
```

## API Endpoints

### Todo Controller (SQL Server)
- `GET /api/todo` - Get all todos
- `GET /api/todo/{id}` - Get todo by ID
- `POST /api/todo` - Create new todo
- `PUT /api/todo/{id}` - Update todo
- `DELETE /api/todo/{id}` - Delete todo
- `GET /api/todo/category/{category}` - Get todos by category
- `GET /api/todo/pending` - Get pending todos

### Mongo Controller (MongoDB)
- `GET /api/mongo` - Get all documents
- `GET /api/mongo/{id}` - Get document by ID
- `POST /api/mongo` - Create new document
- `PUT /api/mongo/{id}` - Update document
- `DELETE /api/mongo/{id}` - Delete document
- `GET /api/mongo/search?term={term}` - Search documents
- `GET /api/mongo/tag/{tag}` - Get documents by tag

### Message Queue Controller (Azure Service Bus)
- `POST /api/messagequeue/publish` - Publish simple message
- `POST /api/messagequeue/publish/detailed` - Publish detailed message
- `POST /api/messagequeue/queue/{queueName}` - Create queue
- `GET /api/messagequeue/health` - Check message queue health

### Health Checks
- `GET /healthz` - Overall health status

## Local Development

### Prerequisites
- .NET 9 SDK
- SQL Server (LocalDB or full instance)
- MongoDB
- Azure Service Bus

### Setup

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd POC.Api
   ```

2. **Update connection strings** in `appsettings.Development.json`:
   ```json
   {
     "ConnectionStrings": {
       "Sql": "Server=(localdb)\\mssqllocaldb;Database=PocDb;Trusted_Connection=true;",
       "Mongo": "mongodb://localhost:27017"
     },
     "Rabbit": {
       "AmqpUri": "amqp://guest:guest@localhost:5672/"
     }
   }
   ```

3. **Run the application**
   ```bash
   dotnet run
   ```

4. **Access Swagger UI**
   Navigate to `https://localhost:5001/swagger` (or the port shown in console)

## Azure Deployment

### Option 1: PowerShell Script (ARM Template)

1. **Install Azure CLI**
   ```bash
   # Windows
   winget install Microsoft.AzureCLI
   
   # macOS
   brew install azure-cli
   
   # Linux
   curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash
   ```

2. **Login to Azure**
   ```bash
   az login
   ```

3. **Run deployment script**
   ```powershell
   .\deploy-azure.ps1 -ResourceGroupName "poc-api-rg"
   ```

### Option 2: Bicep Deployment (Recommended)

1. **Install Bicep CLI** (if not already installed)
   ```bash
   az bicep install
   ```

2. **Run Bicep deployment script**
   ```powershell
   .\deploy-bicep.ps1 -ResourceGroupName "poc-api-rg"
   ```

### Option 3: Manual Deployment

1. **Create Azure Resources**
   - App Service Plan (Basic B1)
   - App Service
   - SQL Database
   - Cosmos DB (MongoDB API)
   - Service Bus Namespace
   - Application Insights

2. **Configure Connection Strings** in App Service Configuration:
   - `ConnectionStrings__Sql`: SQL Database connection string
   - `ConnectionStrings__Mongo`: Cosmos DB MongoDB connection string
   - `Rabbit__AmqpUri`: Service Bus connection string
   - `ApplicationInsights__ConnectionString`: Application Insights connection string

3. **Deploy Application**
   ```bash
   dotnet publish -c Release -o ./publish
   # Deploy ./publish folder to App Service
   ```

## Docker Deployment

### Build Docker Image
```bash
docker build -t poc-api .
```

### Run Locally
```bash
docker run -p 8080:8080 poc-api
```

### Deploy to Azure Container Registry
```bash
# Login to Azure Container Registry
az acr login --name <your-registry-name>

# Tag image
docker tag poc-api <your-registry-name>.azurecr.io/poc-api:latest

# Push image
docker push <your-registry-name>.azurecr.io/poc-api:latest
```

## Testing

### Health Check
```bash
curl https://your-app.azurewebsites.net/healthz
```

### Create Todo (SQL Server)
```bash
curl -X POST https://your-app.azurewebsites.net/api/todo \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Test Todo",
    "description": "This is a test todo",
    "category": "Testing",
    "priority": 3
  }'
```

### Create Document (MongoDB)
```bash
curl -X POST https://your-app.azurewebsites.net/api/mongo \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test Document",
    "content": "This is test content",
    "tags": ["test", "document"]
  }'
```

### Publish Message (RabbitMQ)
```bash
curl -X POST https://your-app.azurewebsites.net/api/messagequeue/publish \
  -H "Content-Type: application/json" \
  -d '{
    "content": "Test message",
    "queueName": "test-queue",
    "messageType": "info"
  }'
```

## Configuration

### Environment Variables
- `ConnectionStrings__Sql`: SQL Server connection string
- `ConnectionStrings__Mongo`: MongoDB connection string
- `Rabbit__AmqpUri`: RabbitMQ connection string
- `ApplicationInsights__ConnectionString`: Application Insights connection string

### Logging
The application uses Serilog for structured logging with Application Insights integration for Azure deployments.

## Monitoring

### Health Checks
The application includes health checks for:
- SQL Server database connectivity
- MongoDB connectivity
- Azure Service Bus connectivity
- Application Insights integration

### Application Insights
When deployed to Azure, the application automatically integrates with Application Insights for:
- Request tracking
- Performance monitoring
- Error tracking
- Dependency tracking

## Security Considerations

1. **Connection Strings**: Use Azure Key Vault for production secrets
2. **HTTPS**: Always use HTTPS in production
3. **Authentication**: Consider adding authentication/authorization
4. **Input Validation**: All inputs are validated using data annotations
5. **SQL Injection**: Protected by Entity Framework Core parameterization

## Cost Optimization

For development/testing environments:
- Use Basic App Service Plan (B1)
- Use Basic Service Bus tier
- Use Basic Cosmos DB throughput (400 RU/s)
- Use Basic SQL Database (S0)

## Troubleshooting

### Common Issues

1. **Connection Timeouts**: Check firewall rules and connection strings
2. **Health Check Failures**: Verify all services are running and accessible
3. **Deployment Failures**: Check resource name uniqueness and quotas

### Logs
- Application logs: Available in App Service logs
- Azure Monitor: Use Application Insights for detailed monitoring
- Health check logs: Available in the health check endpoint response

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## License

This project is licensed under the MIT License.
