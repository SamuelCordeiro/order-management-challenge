using OrderManagement.Application.Messaging;
using OrderManagement.Application.Orders;
using OrderManagement.Contracts.Messaging;
using OrderManagement.Domain.Orders;

namespace OrderManagement.Application.Tests;

public sealed class OrderServiceTests
{
    [Fact]
    public async Task CreateAsync_PersistsOrderBeforePublishingOrderCreated()
    {
        var repository = new RecordingOrderRepository();
        var publisher = new RecordingPublisher(() => repository.SaveChangesCalls);
        var now = new DateTimeOffset(2026, 10, 5, 20, 0, 0, TimeSpan.Zero);
        var service = new OrderService(repository, publisher, new FixedTimeProvider(now));

        var order = await service.CreateAsync(new CreateOrderCommand("Ana", "Notebook", 4999.90m), CancellationToken.None);

        Assert.Equal(OrderStatus.Pendente, order.Status);
        Assert.Equal(now, order.DataCriacao);
        Assert.Equal(1, repository.SaveChangesCalls);
        Assert.True(publisher.WasPublishedAfterPersistence);
        Assert.NotNull(publisher.Message);
        Assert.Equal(order.Id, publisher.Message!.OrderId);
        Assert.Equal(order.Id, publisher.Message.CorrelationId);
        Assert.Equal(OrderCreated.EventTypeName, publisher.Message.EventType);
        Assert.Equal(now, publisher.Message.OccurredAt);
    }

    private sealed class RecordingOrderRepository : IOrderRepository
    {
        private readonly List<Order> _orders = [];

        public int SaveChangesCalls { get; private set; }

        public Task AddAsync(Order order, CancellationToken cancellationToken)
        {
            _orders.Add(order);
            return Task.CompletedTask;
        }

        public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(_orders.SingleOrDefault(order => order.Id == id));

        public Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Order>>(_orders);

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCalls++;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingPublisher(Func<int> getSaveChangesCalls) : IOrderCreatedPublisher
    {
        public OrderCreated? Message { get; private set; }
        public bool WasPublishedAfterPersistence { get; private set; }

        public Task PublishAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            Message = message;
            WasPublishedAfterPersistence = getSaveChangesCalls() > 0;
            return Task.CompletedTask;
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
