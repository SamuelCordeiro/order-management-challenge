using OrderManagement.Contracts.Messaging;

namespace OrderManagement.Application.Messaging;

public interface IOrderCreatedPublisher
{
    Task PublishAsync(OrderCreated message, CancellationToken cancellationToken);
}
