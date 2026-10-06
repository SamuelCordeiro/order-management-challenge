using System.Net;
using System.Net.Http.Json;
using OrderManagement.Contracts.Messaging;
using RabbitMQ.Client;

namespace OrderManagement.Integration.Tests;

public sealed class OrderEndpointsTests(OrderApiFixture fixture) : IClassFixture<OrderApiFixture>
{
    [Fact]
    public async Task CreateOrder_PersistsPendingOrderAndItsInitialHistory()
    {
        using var api = fixture.CreateApi();
        using var client = api.CreateClient();

        var response = await client.PostAsJsonAsync("/orders", new CreateOrderRequest("Cliente de teste", "Produto de teste", 123.45m));

        response.EnsureSuccessStatusCode();
        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.NotNull(order);
        Assert.Equal("pendente", order.Status);

        var historyResponse = await client.GetAsync($"/orders/{order.Id}/history");

        Assert.Equal(HttpStatusCode.OK, historyResponse.StatusCode);
        var history = await historyResponse.Content.ReadFromJsonAsync<OrderStatusHistoryResponse[]>();
        var initialEntry = Assert.Single(history!);
        Assert.Equal("pendente", initialEntry.Status);
        Assert.Equal("api", initialEntry.Origem);
    }

    [Fact]
    public async Task CreateOrder_PublishesOrderCreatedWithOrderIdAsCorrelationId()
    {
        using var api = fixture.CreateApi();
        using var client = api.CreateClient();

        var response = await client.PostAsJsonAsync("/orders", new CreateOrderRequest("Cliente de teste", "Produto de teste", 123.45m));
        response.EnsureSuccessStatusCode();
        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.NotNull(order);

        var factory = new ConnectionFactory
        {
            HostName = fixture.RabbitMqUri.Host,
            Port = fixture.RabbitMqUri.Port,
            UserName = "integration",
            Password = "integration"
        };
        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        var delivery = await channel.BasicGetAsync("order.created.v1", autoAck: true);
        Assert.NotNull(delivery);

        var message = System.Text.Json.JsonSerializer.Deserialize<OrderCreated>(delivery.Body.Span, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        Assert.NotNull(message);
        Assert.Equal(order.Id, message.OrderId);
        Assert.Equal(order.Id, message.CorrelationId);
        Assert.Equal(OrderCreated.EventTypeName, message.EventType);
    }

    private sealed record CreateOrderRequest(string Cliente, string Produto, decimal Valor);

    private sealed record OrderResponse(Guid Id, string Status);

    private sealed record OrderStatusHistoryResponse(string Status, string Origem);
}
