using OrderManagement.Application.Messaging;
using OrderManagement.Application.Orders;
using OrderManagement.Contracts.Messaging;
using OrderManagement.Domain.Orders;

namespace OrderManagement.Application.Tests;

public sealed class OrderServiceTests
{
    [Fact]
    public async Task CreateAsync_PersistsOrderAndOrderCreatedInTheOutbox()
    {
        var repository = new RecordingOrderRepository();
        var outbox = new RecordingOutbox();
        var now = new DateTimeOffset(2026, 10, 5, 20, 0, 0, TimeSpan.Zero);
        var service = new OrderService(repository, outbox, new FixedTimeProvider(now));

        var order = await service.CreateAsync(new CreateOrderCommand("Ana", "Notebook", 4999.90m), CancellationToken.None);

        Assert.Equal(OrderStatus.Pendente, order.Status);
        Assert.Equal(now, order.DataCriacao);
        Assert.Equal(1, repository.SaveChangesCalls);
        Assert.NotNull(outbox.Message);
        Assert.Equal(order.Id, outbox.Message!.OrderId);
        Assert.Equal(order.Id, outbox.Message.CorrelationId);
        Assert.Equal(OrderCreated.EventTypeName, outbox.Message.EventType);
        Assert.Equal(now, outbox.Message.OccurredAt);
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

        public Task<Order?> GetByIdWithHistoryAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(_orders.SingleOrDefault(order => order.Id == id));

        public Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Order>>(_orders);

        public Task<IReadOnlyList<OrderStatusHistory>> GetStatusHistoryAsync(Guid orderId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<OrderStatusHistory>>([]);

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCalls++;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingOutbox : IOrderEventOutbox
    {
        public OrderCreated? Message { get; private set; }

        public Task EnqueueAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            Message = message;
            return Task.CompletedTask;
        }

        public Task EnqueueAsync(OrderStatusChanged message, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
