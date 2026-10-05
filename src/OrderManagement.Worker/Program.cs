using OrderManagement.Infrastructure;
using OrderManagement.Worker;

var builder = Host.CreateApplicationBuilder(args);

// The worker shares persistence configuration with the API but runs independently.
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<OrderProcessingWorker>();

await builder.Build().RunAsync();
