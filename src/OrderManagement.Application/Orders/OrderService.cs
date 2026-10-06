using OrderManagement.Application.Messaging;
using OrderManagement.Contracts.Messaging;
using OrderManagement.Domain.Orders;

namespace OrderManagement.Application.Orders;

public sealed class OrderService(
    IOrderRepository repository,
    IOrderCreatedPublisher publisher,
    TimeProvider timeProvider)
{
    public async Task<OrderResponse> CreateAsync(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        var order = new Order(Guid.NewGuid(), command.Cliente, command.Produto, command.Valor, timeProvider.GetUtcNow());
        await repository.AddAsync(order, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        var orderCreated = new OrderCreated(
            MessageId: Guid.NewGuid(),
            OrderId: order.Id,
            CorrelationId: order.Id,
            EventType: OrderCreated.EventTypeName,
            OccurredAt: timeProvider.GetUtcNow());

        await publisher.PublishAsync(orderCreated, cancellationToken);
        return ToResponse(order);
    }

    public async Task<OrderResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await repository.GetByIdAsync(id, cancellationToken);
        return order is null ? null : ToResponse(order);
    }

    public async Task<IReadOnlyList<OrderResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var orders = await repository.GetAllAsync(cancellationToken);
        return orders.Select(ToResponse).ToArray();
    }

    public async Task<IReadOnlyList<OrderStatusHistoryResponse>?> GetStatusHistoryAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await repository.GetByIdAsync(id, cancellationToken);
        if (order is null)
            return null;

        var history = await repository.GetStatusHistoryAsync(id, cancellationToken);
        return history.Select(item => new OrderStatusHistoryResponse(item.Status, item.OccurredAt, item.Source)).ToArray();
    }

    private static OrderResponse ToResponse(Order order) => new(
        order.Id, order.Customer, order.Product, order.Amount, order.Status, order.CreatedAt);
}
