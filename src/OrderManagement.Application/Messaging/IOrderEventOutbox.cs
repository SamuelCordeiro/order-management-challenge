using OrderManagement.Contracts.Messaging;

namespace OrderManagement.Application.Messaging;

public interface IOrderEventOutbox
{
    Task EnqueueAsync(OrderCreated message, CancellationToken cancellationToken);
    Task EnqueueAsync(OrderStatusChanged message, CancellationToken cancellationToken);
}
