using OrderManagement.Domain.Orders;

namespace OrderManagement.Application.Orders;

public sealed class OrderService(IOrderRepository repository, TimeProvider timeProvider)
{
    public async Task<OrderResponse> CreateAsync(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        var order = new Order(Guid.NewGuid(), command.Cliente, command.Produto, command.Valor, timeProvider.GetUtcNow());
        await repository.AddAsync(order, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
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

    private static OrderResponse ToResponse(Order order) => new(
        order.Id, order.Customer, order.Product, order.Amount, order.Status, order.CreatedAt);
}
