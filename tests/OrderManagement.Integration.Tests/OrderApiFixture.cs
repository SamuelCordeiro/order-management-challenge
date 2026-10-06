using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace OrderManagement.Integration.Tests;

public sealed class OrderApiFixture : IAsyncLifetime
{
    private readonly Dictionary<string, string?> _originalEnvironment = new();
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("orders")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder("rabbitmq:4-management-alpine")
        .WithUsername("integration")
        .WithPassword("integration")
        .Build();

    public string DatabaseConnectionString => _postgres.GetConnectionString();
    public Uri RabbitMqUri => new(_rabbitMq.GetConnectionString());

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await _rabbitMq.StartAsync();
        ConfigureEnvironment();
    }

    public async Task DisposeAsync()
    {
        RestoreEnvironment();
        await _rabbitMq.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    public WebApplicationFactory<Program> CreateApi() => new TestApiFactory();

    private sealed class TestApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
        }
    }

    private void ConfigureEnvironment()
    {
        var values = new Dictionary<string, string?>
        {
            ["HttpsRedirection__Enabled"] = "false",
            ["ConnectionStrings__OrdersDatabase"] = DatabaseConnectionString,
            ["RabbitMq__Host"] = RabbitMqUri.Host,
            ["RabbitMq__Port"] = RabbitMqUri.Port.ToString(),
            ["RabbitMq__Username"] = "integration",
            ["RabbitMq__Password"] = "integration",
            ["RabbitMq__Exchange"] = "order.events",
            ["RabbitMq__Queue"] = "order.created.v1",
            ["RabbitMq__RoutingKey"] = "order.created.v1",
            ["RabbitMq__ErrorExchange"] = "order.events.error",
            ["RabbitMq__ErrorQueue"] = "order.created.v1.error",
            ["RabbitMq__ErrorRoutingKey"] = "order.created.v1.error",
            ["RabbitMq__RetryExchange"] = "order.events.retry",
            ["RabbitMq__RetryQueue"] = "order.created.v1.retry",
            ["RabbitMq__RetryRoutingKey"] = "order.created.v1.retry",
            ["RabbitMq__StatusExchange"] = "order.status.events",
            ["RabbitMq__StatusQueue"] = "order.status.changed.v1",
            ["RabbitMq__StatusRoutingKey"] = "order.status.changed.v1",
            ["RabbitMq__RetryDelayMilliseconds"] = "100",
            ["RabbitMq__MaxDeliveryAttempts"] = "3"
        };

        foreach (var (key, value) in values)
        {
            _originalEnvironment[key] = Environment.GetEnvironmentVariable(key);
            Environment.SetEnvironmentVariable(key, value);
        }
    }

    private void RestoreEnvironment()
    {
        foreach (var (key, value) in _originalEnvironment)
            Environment.SetEnvironmentVariable(key, value);
    }
}
