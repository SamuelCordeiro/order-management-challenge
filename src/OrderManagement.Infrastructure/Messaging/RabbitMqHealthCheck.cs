using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace OrderManagement.Infrastructure.Messaging;

public sealed class RabbitMqHealthCheck(IOptions<RabbitMqOptions> options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var factory = RabbitMqConnectionFactory.Create(options.Value, "order-management-healthcheck");
            await using var connection = await factory.CreateConnectionAsync(cancellationToken);
            return HealthCheckResult.Healthy("RabbitMQ connection established.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("RabbitMQ connection failed.", exception);
        }
    }
}
