using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using OrderManagement.Contracts.Messaging;
using RabbitMQ.Client;

namespace OrderManagement.Integration.Tests;

public sealed class OrderEndpointsTests(OrderApiFixture fixture) : IClassFixture<OrderApiFixture>
{
    [Fact]
    public async Task GetOrders_ReturnsServerPaginatedResultWithMinimumPageSizeOfFive()
    {
        using var api = fixture.CreateApi();
        using var client = api.CreateClient();
        var initialPage = await client.GetFromJsonAsync<PagedOrdersResponse>("/orders?pageSize=100");
        Assert.NotNull(initialPage);

        for (var index = 0; index < 11; index++)
        {
            var created = await client.PostAsJsonAsync("/orders", new CreateOrderRequest($"Cliente {index}", "Produto", index + 1));
            created.EnsureSuccessStatusCode();
        }

        var response = await client.GetAsync("/orders?page=1&pageSize=5&sortBy=data_criacao&sortDirection=desc");

        response.EnsureSuccessStatusCode();
        var page = await response.Content.ReadFromJsonAsync<PagedOrdersResponse>();
        Assert.NotNull(page);
        Assert.Equal(5, page.Items.Length);
        Assert.Equal(initialPage.TotalCount + 11, page.TotalCount);
        Assert.Equal(page.TotalCount, page.Summary.Total);

        var invalidPageSize = await client.GetAsync("/orders?pageSize=4");
        Assert.Equal(HttpStatusCode.BadRequest, invalidPageSize.StatusCode);
    }

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

        BasicGetResult? delivery = null;
        var deadline = DateTimeOffset.UtcNow.AddSeconds(5);
        while (delivery is null && DateTimeOffset.UtcNow < deadline)
        {
            delivery = await channel.BasicGetAsync("order.created.v1", autoAck: true);
            if (delivery is null)
                await Task.Delay(TimeSpan.FromMilliseconds(100));
        }

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

    private sealed record PagedOrdersResponse(
        OrderResponse[] Items,
        [property: JsonPropertyName("total_count")] int TotalCount,
        SummaryResponse Summary);

    private sealed record SummaryResponse(int Total, int Pendentes);
}
