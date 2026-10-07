using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OrderManagement.Application.Orders;
using OrderManagement.Infrastructure;
using OrderManagement.Infrastructure.Observability;
using OrderManagement.Infrastructure.Persistence;
using OrderManagement.Infrastructure.Messaging;
using OrderManagement.Api.Realtime;

var builder = WebApplication.CreateBuilder(args);

#region Service registration

// The public contract is intentionally Portuguese and snake_case, independent of internal names.
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));
});

builder.Services.AddSwaggerGen();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddOrderManagementObservability(builder.Configuration, "order-management-api");
builder.Services.AddScoped<OrderService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<OrderStatusEventHub>();
builder.Services.AddHostedService<OrderStatusEventConsumer>();
builder.Services.AddHostedService<OutboxPublisherWorker>();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<OrdersDbContext>("postgresql", failureStatus: HealthStatus.Unhealthy);

#endregion

var app = builder.Build();

#region Database initialization

// Migrations run before the application accepts traffic, keeping the deployed schema deterministic.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
    await dbContext.Database.MigrateAsync();
}

#endregion

#region HTTP pipeline

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Containers currently expose HTTP directly; production can terminate TLS at the reverse proxy.
if (app.Configuration.GetValue("HttpsRedirection:Enabled", true))
    app.UseHttpsRedirection();

app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.ToDictionary(entry => entry.Key, entry => entry.Value.Status.ToString())
        });
    }
});
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });

#endregion

app.Run();

public partial class Program;
