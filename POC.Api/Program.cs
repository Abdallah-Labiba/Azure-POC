using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using Azure.Messaging.ServiceBus;
using POC.Api.Data;
using POC.Api.Models;
using POC.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// OpenAPI (Swagger)
builder.Services.AddOpenApi();

// SQL (EF Core)
builder.Services.AddDbContext<AppDb>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Sql")));

// Mongo (singleton client)
builder.Services.AddSingleton<IMongoClient>(_ =>
    new MongoClient(builder.Configuration.GetConnectionString("Mongo")));

// Azure Service Bus client (singleton)
builder.Services.AddSingleton<ServiceBusClient>(_ =>
{
    var connectionString = builder.Configuration["ServiceBus:ConnectionString"]!;
    return new ServiceBusClient(connectionString);
});

// Register services
builder.Services.AddScoped<ITodoService, TodoService>();
builder.Services.AddScoped<IMongoService, MongoService>();
builder.Services.AddScoped<IMessageQueueService, MessageQueueService>();

// Health checks
builder.Services.AddHealthChecks()
    .AddCheck("sqlserver", () =>
    {
        try
        {
            var serviceProvider = builder.Services.BuildServiceProvider();
            var context = serviceProvider.GetRequiredService<AppDb>();
            context.Database.CanConnect();
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("SQL Server is healthy");
        }
        catch (Exception ex)
        {
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("SQL Server is unhealthy", ex);
        }
    })
    .AddCheck("mongodb", () => 
    {
        try
        {
            var serviceProvider = builder.Services.BuildServiceProvider();
            var client = serviceProvider.GetRequiredService<IMongoClient>();
            client.GetDatabase("admin").RunCommand<MongoDB.Bson.BsonDocument>(new MongoDB.Bson.BsonDocument("ping", 1));
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("MongoDB is healthy");
        }
        catch (Exception ex)
        {
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("MongoDB is unhealthy", ex);
        }
    })
    .AddCheck("servicebus", () =>
    {
        try
        {
            var serviceProvider = builder.Services.BuildServiceProvider();
            var serviceBusClient = serviceProvider.GetRequiredService<ServiceBusClient>();
            return !serviceBusClient.IsClosed
                ? Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Service Bus is healthy")
                : Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("Service Bus is unhealthy");
        }
        catch (Exception ex)
        {
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("Service Bus is unhealthy", ex);
        }
    });

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

// Auto-migrate database on startup (for development)
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<AppDb>();
        context.Database.EnsureCreated();
    }
}

app.UseRouting();
app.MapControllers();

// Health check endpoint
app.MapHealthChecks("/healthz");

// Simple root endpoint
app.MapGet("/", () => new { 
    message = "POC API is running", 
    timestamp = DateTime.UtcNow,
    version = "1.0.0"
});

app.Run();
