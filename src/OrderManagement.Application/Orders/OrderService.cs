using OrderManagement.Application.Messaging;
using OrderManagement.Contracts.Messaging;
using OrderManagement.Domain.Orders;

namespace OrderManagement.Application.Orders;

public sealed class OrderService(
    IOrderRepository repository,
    IOrderEventOutbox outbox,
    TimeProvider timeProvider)
{
    public async Task<OrderResponse> CreateAsync(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        var order = new Order(Guid.NewGuid(), command.Cliente, command.Produto, command.Valor, timeProvider.GetUtcNow());
        await repository.AddAsync(order, cancellationToken);

        var orderCreated = new OrderCreated(
            MessageId: Guid.NewGuid(),
            OrderId: order.Id,
            CorrelationId: order.Id,
            EventType: OrderCreated.EventTypeName,
            OccurredAt: timeProvider.GetUtcNow());

        // EF tracks both aggregates in one DbContext, so the order and its event are committed atomically.
        await outbox.EnqueueAsync(orderCreated, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ToResponse(order);
    }

    public async Task<OrderResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await repository.GetByIdAsync(id, cancellationToken);
        return order is null ? null : ToResponse(order);
    }

    public async Task<PagedOrdersResponse> GetPageAsync(OrderPageQuery query, CancellationToken cancellationToken)
    {
        var result = await repository.GetPageAsync(query, cancellationToken);
        return new PagedOrdersResponse(
            result.Items.Select(ToResponse).ToArray(),
            query.Page,
            query.PageSize,
            result.TotalCount,
            new OrderSummaryResponse(result.TotalCount, result.PendingCount, result.ProcessingCount, result.FinalizedCount));
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
