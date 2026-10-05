using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using OrderManagement.Application.Messaging;
using OrderManagement.Application.Orders;
using OrderManagement.Infrastructure.Messaging;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("OrdersDatabase")
            ?? throw new InvalidOperationException("Connection string 'OrdersDatabase' is required.");

        services.AddDbContext<OrdersDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IOrderRepository, EfOrderRepository>();
        services.AddHealthChecks()
            .AddCheck<RabbitMqHealthCheck>("rabbitmq", failureStatus: HealthStatus.Unhealthy);
        services.AddOptions<RabbitMqOptions>()
            .Configure(options =>
            {
                options.Host = configuration["RabbitMq:Host"] ?? string.Empty;
                options.Port = int.TryParse(configuration["RabbitMq:Port"], out var port) ? port : 5672;
                options.Username = configuration["RabbitMq:Username"] ?? string.Empty;
                options.Password = configuration["RabbitMq:Password"] ?? string.Empty;
                options.Exchange = configuration["RabbitMq:Exchange"] ?? string.Empty;
                options.Queue = configuration["RabbitMq:Queue"] ?? string.Empty;
                options.RoutingKey = configuration["RabbitMq:RoutingKey"] ?? string.Empty;
                options.ErrorExchange = configuration["RabbitMq:ErrorExchange"] ?? string.Empty;
                options.ErrorQueue = configuration["RabbitMq:ErrorQueue"] ?? string.Empty;
                options.ErrorRoutingKey = configuration["RabbitMq:ErrorRoutingKey"] ?? string.Empty;
                options.RetryExchange = configuration["RabbitMq:RetryExchange"] ?? string.Empty;
                options.RetryQueue = configuration["RabbitMq:RetryQueue"] ?? string.Empty;
                options.RetryRoutingKey = configuration["RabbitMq:RetryRoutingKey"] ?? string.Empty;
                options.RetryDelayMilliseconds = int.TryParse(configuration["RabbitMq:RetryDelayMilliseconds"], out var retryDelay) ? retryDelay : 5000;
                options.MaxDeliveryAttempts = int.TryParse(configuration["RabbitMq:MaxDeliveryAttempts"], out var maxAttempts) ? maxAttempts : 3;
            })
            .Validate(options =>
                !string.IsNullOrWhiteSpace(options.Host) &&
                !string.IsNullOrWhiteSpace(options.Username) &&
                !string.IsNullOrWhiteSpace(options.Password) &&
                !string.IsNullOrWhiteSpace(options.Exchange) &&
                !string.IsNullOrWhiteSpace(options.Queue) &&
                !string.IsNullOrWhiteSpace(options.RoutingKey) &&
                !string.IsNullOrWhiteSpace(options.ErrorExchange) &&
                !string.IsNullOrWhiteSpace(options.ErrorQueue) &&
                !string.IsNullOrWhiteSpace(options.ErrorRoutingKey) &&
                !string.IsNullOrWhiteSpace(options.RetryExchange) &&
                !string.IsNullOrWhiteSpace(options.RetryQueue) &&
                !string.IsNullOrWhiteSpace(options.RetryRoutingKey) &&
                options.RetryDelayMilliseconds > 0 &&
                options.MaxDeliveryAttempts > 0,
                "RabbitMq configuration is incomplete.")
            .ValidateOnStart();
        services.AddScoped<IOrderCreatedPublisher, RabbitMqOrderCreatedPublisher>();
        return services;
    }
}
