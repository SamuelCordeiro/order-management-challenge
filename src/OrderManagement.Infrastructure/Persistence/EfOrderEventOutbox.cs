using OrderManagement.Application.Messaging;
using OrderManagement.Contracts.Messaging;

namespace OrderManagement.Infrastructure.Persistence;

public sealed class EfOrderEventOutbox(OrdersDbContext dbContext) : IOrderEventOutbox
{
    public Task EnqueueAsync(OrderCreated message, CancellationToken cancellationToken) =>
        dbContext.OutboxMessages.AddAsync(OutboxMessage.From(message), cancellationToken).AsTask();

    public Task EnqueueAsync(OrderStatusChanged message, CancellationToken cancellationToken) =>
        dbContext.OutboxMessages.AddAsync(OutboxMessage.From(message), cancellationToken).AsTask();
}
