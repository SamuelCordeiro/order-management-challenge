using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OrderManagement.Infrastructure;
using OrderManagement.Infrastructure.Persistence;
using OrderManagement.Infrastructure.Observability;
using OrderManagement.Worker;

var builder = WebApplication.CreateBuilder(args);

// The worker shares persistence configuration with the API but runs independently.
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddOrderManagementObservability(builder.Configuration, "order-management-worker");
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHostedService<OrderProcessingWorker>();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<OrdersDbContext>("postgresql", failureStatus: HealthStatus.Unhealthy);

var app = builder.Build();
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });

await app.RunAsync();
