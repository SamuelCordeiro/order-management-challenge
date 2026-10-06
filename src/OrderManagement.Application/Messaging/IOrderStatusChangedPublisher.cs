using OrderManagement.Contracts.Messaging;

namespace OrderManagement.Application.Messaging;

public interface IOrderStatusChangedPublisher
{
    Task PublishAsync(OrderStatusChanged message, CancellationToken cancellationToken);
}
