using OrderManagement.Domain.Orders;

namespace OrderManagement.Application.Orders;

public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken cancellationToken);
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Order?> GetByIdWithHistoryAsync(Guid id, CancellationToken cancellationToken);
    Task<OrderPage> GetPageAsync(OrderPageQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyList<OrderStatusHistory>> GetStatusHistoryAsync(Guid orderId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
